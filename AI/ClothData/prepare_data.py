"""Build bounded-memory indexes over the raw archives; do not duplicate them.

Static drapes and motion trajectories are different tasks. Structural checks do
not certify nonintersection. Licensing holds remain separate from geometry QA.
"""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import sqlite3
import struct
import sys
import time

import google_crc32c
import numpy as np
import pyarrow.parquet as pq
import yaml

ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT))
from collect_data import DATA, RAW, read_json, write_json, now
PROCESSED=DATA/'Processed'


def ply_geometry(blob,include_uv=False):
    """Read original binary PLY vertex IDs, including UV seam duplicates."""
    marker=b'end_header\n'; end=blob.index(marker)+len(marker)
    lines=blob[:end].decode('ascii').splitlines()
    if lines[1]!='format binary_little_endian 1.0': raise ValueError('Unsupported PLY format')
    types={'float':'<f4','double':'<f8','uchar':'u1','int':'<i4','uint':'<u4','short':'<i2','ushort':'<u2'}
    fields=[]; current=None; n=f=0; face_schema=[]
    for line in lines[2:]:
        p=line.split()
        if not p: continue
        if p[0]=='element':
            current=p[1]
            if current=='vertex': n=int(p[2])
            elif current=='face': f=int(p[2])
            else: raise ValueError('Unsupported PLY element')
        elif p[0]=='property':
            if current=='vertex':
                if len(p)!=3 or p[1] not in types: raise ValueError('Unsupported vertex property')
                fields.append((p[2],types[p[1]]))
            elif current=='face': face_schema.append(p[1:])
    if face_schema != [['list','uchar','int','vertex_indices']]: raise ValueError('Unsupported face properties')
    dtype=np.dtype(fields)
    raw_vertices=np.frombuffer(blob,dtype=dtype,count=n,offset=end)
    vertices=np.column_stack([raw_vertices[k] for k in ('x','y','z')]).astype(np.float32)
    face_dtype=np.dtype([('count','u1'),('indices','<i4',(3,))])
    offset=end+n*dtype.itemsize
    if len(blob)!=offset+f*face_dtype.itemsize: raise ValueError('Unexpected PLY byte length')
    raw_faces=np.frombuffer(blob,dtype=face_dtype,count=f,offset=offset)
    if np.any(raw_faces['count']!=3): raise ValueError('Nontriangle face')
    faces=raw_faces['indices'].copy()
    if include_uv:
        if not {'s','t'}.issubset(raw_vertices.dtype.names): raise ValueError('PLY has no source UV coordinates')
        return vertices,faces,np.column_stack((raw_vertices['s'],raw_vertices['t'])).astype(np.float32)
    return vertices,faces


def canonical_map(vertices):
    return np.concatenate(([0],np.cumsum(np.any(vertices[1:]!=vertices[:-1],axis=1)))).astype(np.int32)


def mesh_checks(vertices,faces,units):
    if not np.isfinite(vertices).all() or not len(vertices) or not len(faces): raise ValueError('Empty/nonfinite geometry')
    if faces.min()<0 or faces.max()>=len(vertices): raise ValueError('Face index outside vertices')
    v=vertices/units
    if np.abs(v).max()>10: raise ValueError('Geometry outside 10m sanity envelope')
    twice_area=np.linalg.norm(np.cross(v[faces[:,1]]-v[faces[:,0]],v[faces[:,2]]-v[faces[:,0]]),axis=1)
    return dict(vertices=len(v),faces=len(faces),bbox_m=[v.min(0).tolist(),v.max(0).tolist()],
                degenerate_faces=int(np.count_nonzero(twice_area<2e-14)))


def official_splits():
    source=read_json(RAW/'GarmentCodeData_v2/upstream/GarmentCodeData_v2_official_train_valid_test_data_split.json')
    exact={}; grouped={}
    for part,paths in source.items():
        part='train' if part=='training' else part
        for path in paths:
            exact[path]=part
            batch,_,element=path.split('/')
            group=batch+'/'+element
            if group in grouped and grouped[group]!=part: raise ValueError('Official split crosses design group')
            grouped[group]=part
    return exact,grouped


def garment_info(rows,source,row_start,split_lookup,design_splits):
    group={r['kind']:r for r in rows}
    expected={'body_measurements','box_mesh','design_params','pattern_svg','sim_mesh','segmentation','specification','vertex_labels'}
    if len(rows)!=8 or set(group)!=expected: raise ValueError('Incomplete/duplicate garment components')
    for row in rows:
        if hashlib.sha256(row['data']).hexdigest()!=row['sha256']: raise ValueError('Embedded component hash mismatch')
    spec=json.loads(group['specification']['data'])
    units=float(spec['properties']['units_in_meter'])
    if units!=100: raise ValueError('Unknown pattern units; do not assume cm')
    box,faces=ply_geometry(group['box_mesh']['data'])
    draped,sim_faces=ply_geometry(group['sim_mesh']['data'])
    if box.shape!=draped.shape or not np.array_equal(faces,sim_faces): raise ValueError('Box/drape topology mismatch')
    box_checks=mesh_checks(box,faces,units); sim_checks=mesh_checks(draped,faces,units)
    labels=group['segmentation']['data'].decode().splitlines()
    if len(labels)==len(draped): semantic_status='direct'
    else:
        mapping=canonical_map(draped); box_mapping=canonical_map(box)
        semantic_status='consecutive_uv_duplicate_map' if int(mapping[-1])+1==len(labels) and np.array_equal(mapping,box_mapping) else 'mapping_unverified'
    body=yaml.load(group['body_measurements']['data'],Loader=yaml.CSafeLoader)
    design=yaml.load(group['design_params']['data'],Loader=yaml.CSafeLoader)
    meta=design.get('design',{}).get('meta',{})
    batch=int(rows[0]['batch']); variant=rows[0]['body']; element=rows[0]['element']
    official=f'garments_5000_{batch}/{variant}_body/{element}'
    design_group=f'garments_5000_{batch}/{element}'
    split=split_lookup.get(official,design_splits.get(design_group,'unassigned'))
    split_origin='official_path' if official in split_lookup else 'official_design_inherited' if design_group in design_splits else 'not_in_official_split'
    geometry_ok=box_checks['degenerate_faces']==0 and sim_checks['degenerate_faces']==0
    return dict(id=f'gcd_b{batch:02d}_{variant}_{element}',design_group=design_group,split_origin=split_origin,
       batch=batch,body_variant=variant,element=element,official_split=split,source_file=str(source.relative_to(DATA)).replace('\\','/'),row_start=row_start,row_count=8,
       units_in_meter=units,output_length_unit='meter',body_measurements_length_unit='centimeter',
       body_height_m=float(body['body']['height'])/100,body_mesh_available=(variant=='default'),
       body_mesh='Raw/GarmentCodeData_v2/upstream/neutral_body/mean_all.obj' if variant=='default' else None,
       raw_vertex_count=sim_checks['vertices'],canonical_label_count=len(labels),semantic_mapping=semantic_status,
       box_mesh=box_checks,sim_mesh=sim_checks,panels=len(spec['pattern']['panels']),stitches=len(spec['pattern'].get('stitches',[])),
       design_meta={k:v.get('v') if isinstance(v,dict) else v for k,v in meta.items()},
       upstream_bending=1 if batch in list(range(0,7))+list(range(21,26)) else 100 if batch in list(range(7,14))+list(range(26,31)) else 500,
       component_hashes={k:v['sha256'] for k,v in group.items()},
       structural_qa='passed' if geometry_ok else 'degenerate_triangles',
       pattern_drape_training_eligible=geometry_ok and split!='unassigned',
       semantic_training_eligible=geometry_ok and split!='unassigned' and semantic_status!='mapping_unverified',
       body_triangle_intersection_qa='not_run',self_intersection_qa='not_run',
       nonpenetration_certified=False)


def prepare_garments():
    PROCESSED.mkdir(parents=True,exist_ok=True)
    exact,design_splits=official_splits()
    database=sqlite3.connect(PROCESSED/'garments.sqlite3')
    database.execute('CREATE TABLE IF NOT EXISTS garments (id TEXT PRIMARY KEY, design_group TEXT, split TEXT, source TEXT, eligible INTEGER, detail_json TEXT)')
    database.execute('CREATE TABLE IF NOT EXISTS sources (path TEXT PRIMARY KEY, sha256 TEXT, rows INTEGER, garments INTEGER)')
    if 'index_version' not in {row[1] for row in database.execute('PRAGMA table_info(sources)')}:
        database.execute('ALTER TABLE sources ADD COLUMN index_version INTEGER DEFAULT 1')
    database.execute('CREATE INDEX IF NOT EXISTS by_split ON garments(split,eligible)')
    database.execute('CREATE INDEX IF NOT EXISTS by_design ON garments(design_group)')
    prior_summary=PROCESSED/'garment_summary.json'
    errors=read_json(prior_summary).get('structural_failures',[]) if prior_summary.exists() else []
    for source in sorted((RAW/'GarmentCodeData_v2').glob('*_body/*.parquet')):
        receipt=source.with_suffix('.parquet.receipt.json')
        if not receipt.exists(): continue
        source_hash=read_json(receipt)['sha256']; source_key=str(source.relative_to(DATA)).replace('\\','/')
        previous=database.execute('SELECT sha256,index_version FROM sources WHERE path=?',(source_key,)).fetchone()
        if previous and previous[0]==source_hash and previous[1]>=2: print('INDEX_CACHED '+source_key,flush=True); continue
        errors=[error for error in errors if error['source']!=source_key]
        database.execute('DELETE FROM garments WHERE source=?',(source_key,))
        q=pq.ParquetFile(source); pending=[]; key=None; start=0; offset=0; count=0
        def finish(rows,row_start):
            nonlocal count
            try:
                item=garment_info(rows,source,row_start,exact,design_splits)
                database.execute('INSERT OR REPLACE INTO garments VALUES (?,?,?,?,?,?)',(item['id'],item['design_group'],item['official_split'],source_key,int(item['pattern_drape_training_eligible']),json.dumps(item)))
                count+=1
                if count%250==0:
                    database.commit(); print(f'GARMENTS {source.name} {count}',flush=True)
            except Exception as error:
                errors.append(dict(source=source_key,row_start=row_start,element=rows[0]['element'],error=str(error)))
        for batch in q.iter_batches(batch_size=16,use_threads=False):
            for row in batch.to_pylist():
                newkey=(row['batch'],row['body'],row['element'])
                if key is not None and key!=newkey: finish(pending,start); pending=[]; start=offset
                key=newkey; pending.append(row); offset+=1
        if pending: finish(pending,start)
        database.execute('INSERT OR REPLACE INTO sources VALUES (?,?,?,?,?)',(source_key,source_hash,offset,count,2)); database.commit()
    # Cross-body variants of one design must stay in one official split.
    leakage=database.execute("SELECT design_group FROM garments WHERE split!='unassigned' GROUP BY design_group HAVING COUNT(DISTINCT split)>1").fetchall()
    if leakage: raise ValueError('Design leakage in prepared data')
    summary=dict(updated_utc=now(),indexed_garment_variants=database.execute('SELECT COUNT(*) FROM garments').fetchone()[0],
        unique_designs=database.execute('SELECT COUNT(DISTINCT design_group) FROM garments').fetchone()[0],
        pattern_drape_training_eligible=database.execute('SELECT COUNT(*) FROM garments WHERE eligible=1').fetchone()[0],
        split_counts=dict(database.execute('SELECT split,COUNT(*) FROM garments GROUP BY split').fetchall()),
        variant_counts=dict(database.execute('SELECT json_extract(detail_json,\'$.body_variant\'),COUNT(*) FROM garments GROUP BY 1').fetchall()),
        structural_failures=errors,design_group_leakage=0,
        note='Static pattern/drape pairs; structural QA does not certify body/self nonintersection. Random body meshes are absent from this repack.')
    database.close(); write_json(PROCESSED/'garment_summary.json',summary); print(json.dumps(summary),flush=True)


def protobuf_fields(blob):
    pos=0; fields={}
    def varint():
        nonlocal pos
        value=shift=0
        while True:
            if pos>=len(blob) or shift>63: raise ValueError('Invalid protobuf varint')
            b=blob[pos]; pos+=1; value|=(b&127)<<shift
            if b<128: return value
            shift+=7
    while pos<len(blob):
        tag=varint(); number,wire=tag>>3,tag&7
        if wire==2:
            length=varint(); value=blob[pos:pos+length]; pos+=length
            if len(value)!=length: raise ValueError('Truncated protobuf')
        elif wire==0: value=varint()
        elif wire in (1,5): length=8 if wire==1 else 4; value=blob[pos:pos+length]; pos+=length
        else: raise ValueError('Unsupported protobuf wire type')
        fields.setdefault(number,[]).append(value)
    return fields


def parse_example(blob,metadata):
    features=protobuf_fields(protobuf_fields(blob)[1][0]); result={}
    for entry in features[1]:
        record=protobuf_fields(entry); key=record[1][0].decode()
        feature=protobuf_fields(record[2][0]); chunks=protobuf_fields(feature[1][0])[1]
        field=metadata['features'][key]
        if field['type'] not in ('static','dynamic'): raise ValueError('Ragged fields need a separate adapter')
        result[key]=np.frombuffer(b''.join(chunks),dtype=np.dtype(field['dtype']).newbyteorder('<')).reshape(field['shape'])
    if set(result)!=set(metadata['field_names']): raise ValueError('Missing TF Example field')
    return result


def masked_crc(data):
    value=google_crc32c.value(data)
    return (((value>>15)|(value<<17))+0xa282ead8)&0xffffffff


def tfrecords(path):
    with path.open('rb') as stream:
        while True:
            offset=stream.tell(); raw_length=stream.read(8)
            if not raw_length: return
            if len(raw_length)!=8: raise ValueError('Truncated TFRecord length')
            length_crc=stream.read(4)
            if len(length_crc)!=4 or struct.unpack('<I',length_crc)[0]!=masked_crc(raw_length): raise ValueError('TFRecord length CRC mismatch')
            length=struct.unpack('<Q',raw_length)[0]
            if length>200_000_000: raise ValueError('Oversized TFRecord')
            blob=stream.read(length); data_crc=stream.read(4)
            if len(blob)!=length or len(data_crc)!=4: raise ValueError('Truncated TFRecord payload')
            if struct.unpack('<I',data_crc)[0]!=masked_crc(blob): raise ValueError('TFRecord data CRC mismatch')
            yield offset,length+16,blob


def prepare_motion():
    folder=RAW/'MeshGraphNets/sphere_simple'; meta_path=folder/'meta.json'
    if not meta_path.exists(): return
    signatures={name:read_json(folder/(name+'.tfrecord.receipt.json'))['sha256'] for name in ('train','valid','test') if (folder/(name+'.tfrecord.receipt.json')).exists()}
    summary_path=PROCESSED/'motion_summary.json'
    if summary_path.exists() and (PROCESSED/'motion_sequences.jsonl').exists() and read_json(summary_path).get('source_archive_sha256')==signatures:
        print('MOTION_INDEX_CACHED',flush=True); return
    metadata=read_json(meta_path); output=[]
    for split in ('train','valid','test'):
        path=folder/(split+'.tfrecord')
        if not path.with_suffix('.tfrecord.receipt.json').exists(): continue
        for index,(offset,length,blob) in enumerate(tfrecords(path)):
            arrays=parse_example(blob,metadata); world=arrays['world_pos']; cells=arrays['cells'][0]; types=arrays['node_type']
            if not all(np.isfinite(array).all() for array in arrays.values()): raise ValueError('Nonfinite motion data')
            if cells.min()<0 or cells.max()>=world.shape[1]: raise ValueError('Motion face index out of range')
            if world.shape[0]!=metadata['trajectory_length']: raise ValueError('Unexpected trajectory length')
            if types.min()<0 or types.max()>8: raise ValueError('Unknown node type')
            velocity=np.diff(world,axis=0)/metadata['dt']
            counts=np.bincount(types[0,:,0],minlength=9)
            item=dict(id=f'mgn_sphere_simple_{split}_{index:05d}',official_split='validation' if split=='valid' else split,
                 source_file=str(path.relative_to(DATA)).replace('\\','/'),byte_offset=offset,record_bytes=length,
                 frames=world.shape[0],vertices=world.shape[1],faces=len(cells),dt=metadata['dt'],
                 next_step_pairs=world.shape[0]-2,max_speed_source_units_per_second=float(np.linalg.norm(velocity,axis=2).max()),
                 initial_node_type_counts=counts.tolist(),structural_qa='passed',nonpenetration_certified=False,
                 training_eligible=False,training_hold_reason='Dataset-specific license not explicit in upstream README; do not infer data rights from code license')
            output.append(item)
            if (index+1)%100==0: print(f'MOTION {split} {index+1} trajectories',flush=True)
    PROCESSED.mkdir(parents=True,exist_ok=True)
    with (PROCESSED/'motion_sequences.jsonl').open('w',encoding='utf-8') as stream:
        for item in output: stream.write(json.dumps(item)+'\n')
    summary=dict(updated_utc=now(),sequences=len(output),frames=sum(r['frames'] for r in output),
                 next_step_pairs=sum(r['next_step_pairs'] for r in output),split_counts=dict(Counter(r['official_split'] for r in output)),
                 structural_qa_passed=len(output),training_eligible=0,training_hold='Dataset-specific license confirmation',
                 source_archive_sha256=signatures,
                 note='Generic cloth/obstacle sequences, not human runway or paired human garments. Adjacent frames are correlated.')
    write_json(PROCESSED/'motion_summary.json',summary); print(json.dumps(summary),flush=True)


if __name__=='__main__':
    parser=argparse.ArgumentParser(); parser.add_argument('--task',choices=('garments','motion','all'),default='all'); args=parser.parse_args()
    if args.task in ('garments','all'): prepare_garments()
    if args.task in ('motion','all'): prepare_motion()
