"""Local back-armpit clearance from controlled Unity visibility rays.

No body hiding, no topology/weight changes, no per-frame work. Final raster and
dense body-contact checks must pass before promoting the generated cache.
"""
import os
os.environ.setdefault('OPENBLAS_NUM_THREADS','1')
import json
from pathlib import Path
import numpy as np
from scipy.spatial import cKDTree
import trimesh
import sys
sys.path.insert(0,str(Path(__file__).resolve().parent))
from check_slow_body_contact import read_mesh

ROOT=Path(__file__).resolve().parent.parent
OUT=ROOT/'Verification/UnderarmFix'

def finish_visible():
    data=json.loads((OUT/'dense-base-fit.json').read_text())
    samples=json.loads((OUT/'visible_remaining_points.json').read_text())['samples']
    assert samples and all(s['view'].endswith('Back') for s in samples)
    hits=np.array([[s['position'][k] for k in ('x','y','z')] for s in samples])
    p=np.array(data['upper']).reshape(-1,3)
    distance,_=cKDTree(hits).query(p);weight=np.exp(-(distance/.016)**2);weight[distance>.035]=0;weight[p[:,2]>.035]=0
    p[:,2]-=.0006*weight
    data['upper']=p.astype(np.float32).ravel().tolist()
    (OUT/'polo-contact-fit.json').write_text(json.dumps(data,separators=(',',':')))
    original=np.array(json.loads((OUT/'Before/polo-contact-fit.json').read_text())['upper']).reshape(-1,3)
    report=dict(method='Local rear clearance, dense barycentric contact constraints, then submillimetre visibility finish',finish_visible_pixels=len(hits),changed_vertices=int((np.linalg.norm(p-original,axis=1)>1e-6).sum()),max_displacement_mm=float(np.linalg.norm(p-original,axis=1).max()*1000),source_upper_key=data['upper_key'],topology_uv_weights_unchanged=True,body_unchanged=True)
    (OUT/'geometry.json').write_text(json.dumps(report,indent=2));print(report,flush=True)

def run():
    before=OUT/'Before';before.mkdir(parents=True,exist_ok=True)
    backup=before/'polo-contact-fit.json'
    if not backup.exists():backup.write_bytes((ROOT/'Assets/Resources/Fitting/polo-contact-fit.json').read_bytes())
    data=json.loads(backup.read_text())
    source=json.loads((ROOT/'Verification/RealismPass/rest_fit_source.json').read_text())
    assert data['upper_key']==source['upper_key'] and data['detail_key']==source['detail_key']
    samples=json.loads((OUT/'visible_body_points_before.json').read_text())['samples']
    assert samples and all(s['view'].endswith('Back') for s in samples)
    hits=np.array([[s['position'][k] for k in ('x','y','z')] for s in samples])
    p=np.array(data['upper']).reshape(-1,3);original=p.copy()
    distance,_=cKDTree(hits).query(p)
    weight=np.exp(-(distance/.012)**2);weight[distance>.03]=0
    # Backward clearance cannot push the rear panel into the neighbouring arm
    # the way unconstrained closest-surface projection can in the axilla.
    weight[p[:,2]>.035]=0
    p[:,2]-=.002*weight
    raw=np.array(source['upper']).reshape(-1,3)
    _,inv=np.unique(np.round(raw,6),axis=0,return_inverse=True)
    accum=np.zeros((inv.max()+1,3));counts=np.bincount(inv)
    np.add.at(accum,inv,p);p=(accum/counts[:,None])[inv]
    faces=np.array(source['upper_faces']).reshape(-1,3)
    # Dense triangle constraints catch protrusions between corners/centroids.
    # Verify candidate signs against the complete closed body, not a collider.
    with (OUT/'Look6_Standing.bin').open('rb') as stream:
        stream.read(4);body,_=read_mesh(stream),read_mesh(stream)
    _,first=np.unique(inv,return_index=True);welded=p[first].copy();f=inv[faces]
    selected=f[(np.linalg.norm(p-original,axis=1)>1e-6)[faces].any(axis=1)]
    bary=np.array([[i/4,j/4,(4-i-j)/4] for i in range(5) for j in range(5-i)]+[[1/3,1/3,1/3]])
    for iteration in range(24):
        probes=np.einsum('bk,fkj->fbj',bary,welded[selected]).reshape(-1,3)
        unique,probe_inv=np.unique(np.round(probes,7),axis=0,return_inverse=True)
        q,dist,tri=trimesh.proximity.closest_point(body,unique)
        approximate=np.einsum('ij,ij->i',q-unique,body.face_normals[tri])
        candidates=np.flatnonzero(approximate>.00001)
        exact=np.zeros(len(unique))
        if len(candidates):exact[candidates]=trimesh.proximity.signed_distance(body,unique[candidates])
        bad=np.flatnonzero(exact[probe_inv]>.00002)
        if not len(bad):
            exact=trimesh.proximity.signed_distance(body,unique)
            bad=np.flatnonzero(exact[probe_inv]>.00002)
        print(dict(dense_iteration=iteration,inside_over_02mm=len(bad),max_signed_mm=float(exact.max()*1000)),flush=True)
        if not len(bad):break
        query=probe_inv[bad];direction=q[query]-unique[query]
        direction/=np.maximum(np.linalg.norm(direction,axis=1),1e-12)[:,None]
        shifts=direction*(exact[query]+.00015)[:,None]
        b=bary[bad%len(bary)];ids=selected[bad//len(bary)]
        factors=b/np.maximum(np.sum(b*b,axis=1),1e-12)[:,None]
        correction=np.zeros_like(welded);count=np.zeros(len(welded))
        for k in range(3):
            active=b[:,k]>0
            np.add.at(correction,ids[active,k],shifts[active]*factors[active,k,None])
            np.add.at(count,ids[active,k],1)
        correction/=np.maximum(count,1)[:,None]
        correction*=np.minimum(1,.001/np.maximum(np.linalg.norm(correction,axis=1),1e-12))[:,None]
        welded+=correction*.9
    p=welded[inv]
    cross=np.cross(p[faces[:,1]]-p[faces[:,0]],p[faces[:,2]]-p[faces[:,0]])
    sums=np.zeros_like(accum)
    for k in range(3):np.add.at(sums,inv[faces[:,k]],cross)
    n=sums[inv];n/=np.maximum(np.linalg.norm(n,axis=1),1e-12)[:,None]
    old=np.array(data['upper_normals']).reshape(-1,3)
    normals=old*(1-weight[:,None])+n*weight[:,None]
    normals/=np.maximum(np.linalg.norm(normals,axis=1),1e-12)[:,None]
    data['upper']=p.astype(np.float32).ravel().tolist();data['upper_normals']=normals.astype(np.float32).ravel().tolist()
    assert np.isfinite(p).all() and np.isfinite(normals).all()
    (OUT/'polo-contact-fit.json').write_text(json.dumps(data,separators=(',',':')))
    report=dict(method='2 mm maximum backward clearance with 12 mm local falloff from observed rear armpit visibility points',input_visible_pixels=len(hits),changed_vertices=int((np.linalg.norm(p-original,axis=1)>1e-6).sum()),max_displacement_mm=float(np.linalg.norm(p-original,axis=1).max()*1000),source_upper_key=data['upper_key'],topology_uv_weights_unchanged=True,body_unchanged=True)
    (OUT/'geometry.json').write_text(json.dumps(report,indent=2));print(report)

if __name__=='__main__':finish_visible() if '--finish-visible' in sys.argv else run()
