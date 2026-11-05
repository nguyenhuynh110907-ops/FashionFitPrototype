"""Verify collection totals, independent source readers and sampled contact labels."""
import argparse
import hashlib
import json
from pathlib import Path
import sqlite3
import sys

import numpy as np
import pyarrow.parquet as pq
import trimesh

ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT))
from collect_data import DATA,RAW,read_json,write_json,now,checksums
from datasets import GarmentPairs,MotionArchiveReader,ContactPoints


def verify(full_raw=False):
    status=read_json(ROOT/'collection_status.json'); plan=read_json(ROOT/'download_plan.json')
    if status['status']!='complete' or status['errors']: raise ValueError('Download collection incomplete')
    assert status['verified_files']==len(plan['items'])==17
    files=[path for path in DATA.rglob('*') if path.is_file()]
    total=sum(path.stat().st_size for path in files)
    assert total<=30_000_000_000
    assert not list(DATA.rglob('*.part'))
    for item in plan['items']:
        path=DATA/item['local']; receipt=read_json(path.with_suffix(path.suffix+'.receipt.json'))
        assert path.stat().st_size==item['size']==receipt['size']
        if item.get('sha256'): assert receipt['sha256']==item['sha256']
        if item.get('crc32c'): assert receipt['crc32c_computed']==item['crc32c']
        if full_raw:
            sha,crc=checksums(path)
            assert sha==receipt['sha256'] and crc==receipt['crc32c_computed']
    db=sqlite3.connect(DATA/'Processed/garments.sqlite3')
    assert db.execute("SELECT COUNT(*) FROM (SELECT design_group FROM garments WHERE split!='unassigned' GROUP BY design_group HAVING COUNT(DISTINCT split)>1)").fetchone()[0]==0
    summary=read_json(DATA/'Processed/garment_summary.json')
    assert db.execute('SELECT COUNT(*) FROM garments').fetchone()[0]==summary['indexed_garment_variants']
    sources=db.execute('SELECT path,rows,garments FROM sources').fetchall()
    assert len(sources)==3
    assert db.execute("SELECT COUNT(*) FROM garments WHERE eligible=1 AND json_extract(detail_json,'$.body_variant')='random'").fetchone()[0]>0
    for path,rows,count in sources:
        assert pq.ParquetFile(DATA/path).metadata.num_rows==rows
        failed=sum(1 for item in summary['structural_failures'] if item['source']==path)
        assert rows==8*(count+failed)
    samples=[]
    for split in ('train','validation','test'):
        dataset=GarmentPairs(split)
        assert len(dataset)>0
        sample=dataset[0]
        assert sample['rest_positions_m'].shape==sample['draped_positions_m'].shape
        assert np.isfinite(sample['draped_positions_m']).all()
        assert sample['original_units_in_meter']==100
        assert sample['panel_id_per_vertex'] is None or len(sample['panel_id_per_vertex'])==len(sample['draped_positions_m'])
        samples.append(sample['id'])
    motion=read_json(DATA/'Processed/motion_summary.json')
    assert motion['sequences']==1200 and motion['frames']==601200 and motion['next_step_pairs']==598800
    for split,count in (('train',1000),('validation',100),('test',100)):
        reader=MotionArchiveReader(split,for_inspection=True)
        assert len(reader)==count
        sample=reader[0]
        assert sample['arrays']['world_pos'].shape==(501,1731,3)
        assert np.isfinite(sample['arrays']['world_pos']).all()
        try: MotionArchiveReader(split)
        except ValueError: pass
        else: raise AssertionError('Motion licensing hold ignored')
    with np.load(DATA/'Processed/Contact/neutral_body_contact.npz',allow_pickle=False) as contact:
        counts={}
        cells=[]
        for code,name in enumerate(('train','validation','test')):
            selected=contact['split']==code
            counts[name]=int(selected.sum())
            cells.append(set(map(tuple,contact['surface_cell'][selected])))
        assert not cells[0]&cells[1] and not cells[0]&cells[2] and not cells[1]&cells[2]
        assert np.all(np.isfinite(contact['query_m']))
        assert np.all(contact['projected_clearance_m']>=.003-1.01e-5)
        mesh=trimesh.load_mesh(RAW/'GarmentCodeData_v2/upstream/neutral_body/mean_all.obj',process=False)
        indices=np.random.default_rng(27102026).choice(len(contact['query_m']),size=512,replace=False)
        actual=-trimesh.proximity.signed_distance(mesh,contact['query_m'][indices]+contact['correction_m'][indices])
        assert np.all(actual>=.003-1.2e-5)
        contact_count=len(contact['query_m'])
    result=dict(status='passed',verified_utc=now(),raw_files=17,raw_bytes=status['verified_bytes'],total_data_bytes=total,
        within_30gb_limit=True,static_variants=summary['indexed_garment_variants'],unique_designs=summary['unique_designs'],
        eligible_pattern_drape_variants=summary['pattern_drape_training_eligible'],garment_sources=len(sources),
        design_split_leakage=0,lazy_garment_reader_examples=samples,motion_sequences=1200,motion_frames=601200,
        tfrecord_checks='All records checked during indexing: header CRC, payload CRC, finite arrays, shapes and face indices',
        motion_training_hold_enforced=True,contact_points=contact_count,contact_split_counts=counts,
        contact_spatial_split_overlap=0,independent_contact_checks=512,min_sampled_projected_clearance_m=float(actual.min()),
        nonpenetration_certified=False,
        full_raw_hash_recheck=full_raw,
        note='Download checksums verified at acquisition. Contact tests apply to points on one body, not cloth triangles or CCD.')
    db.close(); write_json(ROOT/'verification.json',result); print(json.dumps(result,indent=2),flush=True)


if __name__=='__main__':
    parser=argparse.ArgumentParser(); parser.add_argument('--full-raw',action='store_true'); args=parser.parse_args(); verify(args.full_raw)
