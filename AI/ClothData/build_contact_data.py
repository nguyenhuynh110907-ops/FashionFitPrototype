"""Generate point/body contact labels from a verified watertight body mesh.

These are signed-distance/projection examples for ONE static body. They are not
cloth trajectories, pattern labels, or a proof of triangle-level cloth CCD.
"""
import argparse
import hashlib
from pathlib import Path
import sys
import time

import numpy as np
import trimesh

ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT))
from collect_data import DATA,RAW,write_json,now


def build(count=200000,max_seconds=180):
    source=RAW/'GarmentCodeData_v2/upstream/neutral_body/mean_all.obj'
    mesh=trimesh.load_mesh(source,process=False)
    if not mesh.is_watertight or not mesh.is_winding_consistent or mesh.volume<=0:
        raise ValueError('Signed labels require a closed consistently oriented mesh')
    if not 1.4<float(mesh.extents[1])<2.2: raise ValueError('Body not in expected meter scale')
    # Trimesh's signed distance is positive inside. Our clearance is positive outside.
    sentinel=np.array([[0,1.2,0],[0,2.5,0]],dtype=np.float64)
    signed=trimesh.proximity.signed_distance(mesh,sentinel)
    if not (signed[0]>0 and signed[1]<0): raise ValueError('Body sign convention check failed')
    rng=np.random.default_rng(27102026)
    surface,face=trimesh.sample.sample_surface(mesh,count,seed=27102026)
    query=surface+mesh.face_normals[face]*rng.uniform(-.04,.06,size=(count,1))
    chunks=[]; rejected=0; started=time.perf_counter(); margin=.003
    for start in range(0,count,256):
        q=query[start:start+256]
        near,distance,triangle=trimesh.proximity.closest_point(mesh,q)
        clearance=-trimesh.proximity.signed_distance(mesh,q)
        outward=mesh.face_normals[triangle]
        needs=clearance<margin
        corrected=q.copy(); corrected[needs]=near[needs]+outward[needs]*margin
        after=clearance.copy()
        if np.any(needs): after[needs]=-trimesh.proximity.signed_distance(mesh,corrected[needs])
        valid=np.isfinite(clearance)&np.isfinite(corrected).all(1)&(after>=margin-1e-5)
        rejected+=int(np.count_nonzero(~valid))
        # Separate spatial surface cells, not random copies of identical points.
        cell=np.floor(near/.15).astype(np.int32)
        split=[]
        for key in cell:
            bucket=int(hashlib.sha256(','.join(map(str,key)).encode()).hexdigest()[:8],16)%10
            split.append(0 if bucket<8 else 1 if bucket==8 else 2)
        chunks.append(dict(query_m=q[valid].astype(np.float32),clearance_m=clearance[valid].astype(np.float32),
            normal=outward[valid].astype(np.float32),correction_m=(corrected[valid]-q[valid]).astype(np.float32),
            projected_clearance_m=after[valid].astype(np.float32),surface_cell=cell[valid],split=np.array(split,dtype=np.uint8)[valid]))
        if (start+256)%4096==0: print(f'CONTACT {min(start+256,count)}/{count}',flush=True)
        if time.perf_counter()-started>max_seconds: break
    arrays={name:np.concatenate([chunk[name] for chunk in chunks]) for name in chunks[0]}
    folder=DATA/'Processed/Contact'; folder.mkdir(parents=True,exist_ok=True)
    np.savez_compressed(folder/'neutral_body_contact.npz',**arrays)
    total=len(arrays['query_m'])
    summary=dict(created_utc=now(),source_mesh=str(source.relative_to(DATA)).replace('\\','/'),
        source_sha256=hashlib.sha256(source.read_bytes()).hexdigest(),source_license='CC-BY-4.0 upstream GarmentCodeData',
        body_meshes=1,static_body=True,body_watertight=True,source_length_unit='meter',requested_points=count,accepted_points=total,
        rejected_points=rejected,split_counts={name:int(np.count_nonzero(arrays['split']==i)) for i,name in enumerate(('train','validation','test'))},
        spatial_cell_size_m=.15,collision_margin_m=margin,min_projected_clearance_m=float(arrays['projected_clearance_m'].min()),
        inside_input_points=int(np.count_nonzero(arrays['clearance_m']<0)),elapsed_seconds=time.perf_counter()-started,
        training_eligible=True,task='point_signed_distance_and_contact_projection',
        labels='Computed geometry on a single neutral body; split by surface voxels; no held-out-body generalization claim',
        does_not_include=['cloth material','cloth triangle collision','self collision','continuous collision','human motion'],
        point_projection_check='Accepted points passed recomputed signed-distance margin; not a cloth nonintersection certificate')
    write_json(folder/'contact_summary.json',summary)
    print(summary,flush=True)


if __name__=='__main__':
    parser=argparse.ArgumentParser(); parser.add_argument('--points',type=int,default=200000); parser.add_argument('--max-seconds',type=int,default=180)
    args=parser.parse_args(); build(args.points,args.max_seconds)
