"""Local, lazy readers. Variable-size meshes need graph-aware collation."""
from functools import lru_cache
import json
from pathlib import Path
import sqlite3
import struct
import sys

import numpy as np
import pyarrow.parquet as pq
import yaml

ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT))
from collect_data import DATA,RAW,read_json
from prepare_data import ply_geometry,canonical_map,masked_crc,parse_example


@lru_cache(maxsize=2)
def _row_group(path,index): return pq.ParquetFile(path).read_row_group(index,use_threads=False)


def components(item):
    path=DATA/item['source_file']; file=pq.ParquetFile(path)
    start=item['row_start']; end=start+item['row_count']; preceding=0; rows=[]
    for index in range(file.metadata.num_row_groups):
        size=file.metadata.row_group(index).num_rows
        if start<preceding+size and end>preceding:
            left=max(start-preceding,0); right=min(end-preceding,size)
            rows+=_row_group(str(path),index).slice(left,right-left).to_pylist()
        preceding+=size
        if preceding>=end: break
    return {row['kind']:row['data'] for row in rows}


class GarmentPairs:
    def __init__(self,split='train',require_body_mesh=False):
        if split not in ('train','validation','test'): raise ValueError('Invalid split')
        db=sqlite3.connect(DATA/'Processed/garments.sqlite3')
        self.items=[json.loads(row[0]) for row in db.execute('SELECT detail_json FROM garments WHERE split=? AND eligible=1 ORDER BY id',(split,))]
        db.close()
        if require_body_mesh: self.items=[item for item in self.items if item['body_mesh_available']]
    def __len__(self): return len(self.items)
    def __getitem__(self,index):
        item=self.items[index]; payload=components(item)
        box,faces=ply_geometry(payload['box_mesh']); draped,sim_faces=ply_geometry(payload['sim_mesh'])
        if not np.array_equal(faces,sim_faces): raise ValueError('Topology mismatch')
        labels=np.array(payload['segmentation'].decode().splitlines())
        if item['semantic_mapping']=='direct': mapping=np.arange(len(draped),dtype=np.int32)
        elif item['semantic_mapping']=='consecutive_uv_duplicate_map': mapping=canonical_map(draped)
        else: mapping=None
        vocabulary,panel_id=np.unique(labels,return_inverse=True)
        return dict(id=item['id'],design_group=item['design_group'],split=item['official_split'],
             rest_positions_m=box/item['units_in_meter'],draped_positions_m=draped/item['units_in_meter'],faces=faces,
             pattern=json.loads(payload['specification']),body_measurements_source=yaml.load(payload['body_measurements'],Loader=yaml.CSafeLoader)['body'],
             body_mesh_path=str(DATA/item['body_mesh']) if item['body_mesh'] else None,
             semantic_mapping=item['semantic_mapping'],semantic_vertex_id_map=mapping,
             panel_id_per_vertex=panel_id[mapping] if mapping is not None else None,panel_label_vocabulary=vocabulary.tolist(),
             original_units_in_meter=item['units_in_meter'],nonpenetration_certified=False)


class MotionArchiveReader:
    def __init__(self,split='train',for_inspection=False):
        if split not in ('train','validation','test'): raise ValueError('Invalid split')
        all_items=[json.loads(line) for line in (DATA/'Processed/motion_sequences.jsonl').read_text().splitlines() if line]
        self.items=[item for item in all_items if item['official_split']==split]
        if not for_inspection and any(not item['training_eligible'] for item in self.items):
            raise ValueError('Motion data has a dataset-license hold. Inspection permitted; default training reader refuses it.')
        self.metadata=read_json(RAW/'MeshGraphNets/sphere_simple/meta.json')
    def __len__(self): return len(self.items)
    def __getitem__(self,index):
        item=self.items[index]
        with (DATA/item['source_file']).open('rb') as stream:
            stream.seek(item['byte_offset']); length_bytes=stream.read(8); length_crc=stream.read(4)
            if struct.unpack('<I',length_crc)[0]!=masked_crc(length_bytes): raise ValueError('Record header CRC mismatch')
            length=struct.unpack('<Q',length_bytes)[0]; blob=stream.read(length); crc=stream.read(4)
            if struct.unpack('<I',crc)[0]!=masked_crc(blob): raise ValueError('Record payload CRC mismatch')
        arrays=parse_example(blob,self.metadata)
        return dict(id=item['id'],split=item['official_split'],arrays=arrays,dt=self.metadata['dt'],training_eligible=item['training_eligible'])


class ContactPoints:
    def __init__(self,split='train'):
        code={'train':0,'validation':1,'test':2}[split]
        with np.load(DATA/'Processed/Contact/neutral_body_contact.npz',allow_pickle=False) as source:
            mask=source['split']==code
            self.arrays={key:source[key][mask] for key in source.files}
    def __len__(self): return len(self.arrays['query_m'])
    def __getitem__(self,index): return {key:value[index] for key,value in self.arrays.items()}
