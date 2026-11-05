"""Full-body sampled contact with a conservative convex-hull broad phase.

A point strictly outside a convex hull enclosing every body vertex cannot be
inside any body triangle. Remaining points use the same trimesh signed-distance
test as previous QA. This avoids expensive queries for loose skirts far away.
"""
import os
os.environ.setdefault('OPENBLAS_NUM_THREADS','1')
import argparse
import hashlib
import json
from pathlib import Path
import struct
import sys
import numpy as np
from scipy.spatial import ConvexHull
import trimesh
sys.path.insert(0,str(Path(__file__).resolve().parent))
from check_slow_body_contact import read_mesh


def inside_hull(points,equations,tolerance=.00001):
    keep=np.ones(len(points),dtype=bool)
    for i in range(0,len(points),512):keep[i:i+512]=(points[i:i+512]@equations[:,:3].T+equations[:,3]).max(axis=1)<=tolerance
    return keep


def run(folder,expected,compare=None):
    files=sorted(folder.glob('Look*_*.bin'));assert len(files)==expected,(len(files),expected)
    rows=[]
    for path in files:
        with path.open('rb') as stream:
            assert stream.read(4)==struct.pack('<i',0x53434631)
            body,cloth=read_mesh(stream),read_mesh(stream);assert not stream.read(1)
        assert body.is_watertight and body.is_winding_consistent
        hull=ConvexHull(body.vertices);eq=hull.equations
        assert inside_hull(body.vertices,eq,tolerance=1e-7).all(),'Broad phase must contain every body vertex'
        points=np.concatenate((cloth.vertices[np.unique(cloth.faces)],cloth.triangles_center))
        unique,inverse=np.unique(points,axis=0,return_inverse=True);query=inside_hull(unique,eq)
        depth=np.zeros(len(unique));selected=unique[query]
        if len(selected):
            distance=np.concatenate([trimesh.proximity.signed_distance(body,selected[i:i+384]) for i in range(0,len(selected),384)])
            depth[query]=np.maximum(distance,0)
        measured=depth[inverse]
        row=dict(pose=path.stem,samples=len(points),body_watertight=True,masked_body_used=False,
            exact_signed_queries=int(query.sum()),outside_enclosing_hull=int((~query).sum()),
            samples_inside_over_1mm=int((measured>.001).sum()),max_sampled_penetration_mm=float(measured.max()*1000),
            deepest_point_m=points[np.argmax(measured)].tolist(),source_mesh_sha256=hashlib.sha256(path.read_bytes()).hexdigest())
        rows.append(row);print(json.dumps(row),flush=True)
    if compare:
        old={r['pose']:r for r in json.loads(compare.read_text())['poses']}
        for r in rows:
            prior=old[r['pose']];assert r['source_mesh_sha256']==prior['source_mesh_sha256']
            assert r['samples_inside_over_1mm']==prior['samples_inside_over_1mm'] and abs(r['max_sampled_penetration_mm']-prior['max_sampled_penetration_mm'])<1e-5
    result=dict(status='passed' if all(r['samples_inside_over_1mm']==0 for r in rows) else 'failed',
        scope='All referenced vertices and triangle centroids against full body; convex-hull broad phase with exact signed-distance narrow phase; not continuous triangle CCD',
        poses=rows,samples=sum(r['samples'] for r in rows),samples_inside_over_1mm=sum(r['samples_inside_over_1mm'] for r in rows),
        max_sampled_penetration_mm=max(r['max_sampled_penetration_mm'] for r in rows),masked_body_used=False,continuous_contact_certified=False,
        agrees_with_previous_exact_test=bool(compare))
    (folder/'body_contact_broadphase.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
    print(json.dumps({k:v for k,v in result.items() if k!='poses'}),flush=True)
    if result['status']!='passed':raise SystemExit(1)


if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('folder',type=Path);p.add_argument('--expected',type=int,default=18);p.add_argument('--compare',type=Path);a=p.parse_args();run(a.folder,a.expected,a.compare)
