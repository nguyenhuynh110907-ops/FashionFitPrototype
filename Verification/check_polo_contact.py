"""Independent source-body sample QA for the new closed skirts and polo study."""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import sys

import numpy as np
import trimesh

sys.path.insert(0,str(Path(__file__).resolve().parent))
from check_slow_body_contact import read_mesh


def run(folder,reuse=None):
    files=sorted(folder.glob('Look*_*.bin'))
    if len(files)!=6:raise ValueError('Expected three newly exported poses for each of two looks')
    previous=json.loads(reuse.read_text()) if reuse else {}
    cached={r['source_mesh_sha256']:r for r in previous.get('poses',[])} if previous.get('masked_body_used') is False else {}
    rows=[]
    for path in files:
        digest=hashlib.sha256(path.read_bytes()).hexdigest()
        if digest in cached:
            row=dict(cached[digest]);row['reused_identical_binary_measurement']=True
            rows.append(row);print(json.dumps(row),flush=True);continue
        with path.open('rb') as stream:
            assert stream.read(4)==struct.pack('<i',0x53434631)
            body,cloth=read_mesh(stream),read_mesh(stream);assert not stream.read(1)
        used=np.unique(cloth.faces);points=np.concatenate((cloth.vertices[used],cloth.triangles_center))
        unique,inverse=np.unique(points,axis=0,return_inverse=True)
        measured=np.concatenate([trimesh.proximity.signed_distance(body,unique[start:start+384]) for start in range(0,len(unique),384)])
        distance=measured[inverse]
        depth=np.maximum(distance,0)
        row=dict(pose=path.stem,samples=len(points),body_watertight=bool(body.is_watertight),
            source_mesh_sha256=digest,
            samples_inside_over_1mm=int((depth>.001).sum()),max_sampled_penetration_mm=float(depth.max()*1000),
            deepest_point_m=points[np.argmax(depth)].tolist())
        rows.append(row);print(json.dumps(row),flush=True)
    result=dict(status='passed' if all(r['samples_inside_over_1mm']==0 for r in rows) else 'failed',
        poses=rows,samples=sum(r['samples'] for r in rows),masked_body_used=False,
        samples_inside_over_1mm=sum(r['samples_inside_over_1mm'] for r in rows),
        max_sampled_penetration_mm=max(r['max_sampled_penetration_mm'] for r in rows),
        scope='Referenced garment vertices and triangle centroids against original body at six sampled poses; not full triangle CCD',
        continuous_contact_certified=False)
    (folder/'source_body_contact.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    print(json.dumps({k:v for k,v in result.items() if k!='poses'}),flush=True)
    if result['status']!='passed':raise SystemExit(1)


if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('folder',type=Path);p.add_argument('--reuse',type=Path);a=p.parse_args();run(a.folder,a.reuse)
