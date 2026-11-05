"""Offline tailoring cleanup. Always start from the saved, tested input cache.

Not training: preserves mesh layout and skinning, then requires posed contact QA.
"""
import os
os.environ.setdefault('OPENBLAS_NUM_THREADS','1')
import json
from pathlib import Path
import sys
import numpy as np
from scipy import sparse
import trimesh
sys.path.insert(0,str(Path(__file__).resolve().parent))
from bake_polo_contact import nearest,front_normals,seat_details,polish_armpits
from check_slow_body_contact import read_mesh

ROOT=Path(__file__).resolve().parent.parent
OUT=ROOT/'Verification/RealismPass'

def smooth(t):
    t=np.clip(t,0,1);return t*t*(3-2*t)

def run():
    old_source=json.loads((ROOT/'Verification/BodySilhouette/rest_fit_source.json').read_text())
    new_source=OUT/'rest_fit_source.json'
    source=json.loads(new_source.read_text()) if new_source.exists() else old_source
    cache=json.loads((OUT/'Before/polo-contact-fit.json').read_text())
    assert all(old_source[k]==cache[k] for k in ('upper_key','detail_key'))
    assert len(source['upper_faces'])==len(old_source['upper_faces'])
    assert source['detail_key']==old_source['detail_key'] and source['body']==old_source['body']
    for k in ('upper_key','detail_key'):cache[k]=source[k]
    raw=np.array(source['upper']).reshape(-1,3)
    previous=np.array(cache['upper']).reshape(-1,3)
    # Changing normal ease can change exact duplicate compaction. Triangles
    # still originate from the same clipped body triangles in the same order.
    # Map by those corner correspondences rather than assuming stable IDs.
    old_ids=np.array(old_source['upper_faces']);new_ids=np.array(source['upper_faces'])
    original=np.zeros_like(raw);counts=np.bincount(new_ids,minlength=len(raw))
    assert counts.min()>0
    np.add.at(original,new_ids,previous[old_ids]);original/=counts[:,None]
    body=trimesh.Trimesh(vertices=np.array(source['body']).reshape(-1,3),faces=np.array(source['body_faces']).reshape(-1,3),process=False)
    # Recover the smooth original underarm instead of fairing it through the
    # narrow gap between arm and torso. Project only the selected local region.
    weight=smooth((abs(raw[:,0])-.14)/.025)*(1-smooth((abs(raw[:,0])-.26)/.035))*smooth((raw[:,1]-1.20)/.04)*(1-smooth((raw[:,1]-1.39)/.045))
    ids=np.flatnonzero(weight>0);underarm_count=len(ids)
    target=raw[ids]
    p=original.copy();p[ids]=original[ids]*(1-weight[ids,None])+target*weight[ids,None]
    # Match the lower bodice to the actual pinned skirt seam, with a covered
    # vertical overlap. No exposed body strip and no floating waist ledge.
    with (ROOT/'Verification/BodySilhouette/Look6_Standing.bin').open('rb') as stream:
        stream.read(4);read_mesh(stream);skirt=read_mesh(stream)
    seam=skirt.vertices[:32]
    centre=np.array([0,seam[:,2].mean()])
    angles=np.arctan2(seam[:,2]-centre[1],seam[:,0]);order=np.argsort(angles)
    radii=np.linalg.norm(seam[:,[0,2]]-centre,axis=1)
    a=np.arctan2(p[:,2]-centre[1],p[:,0])
    radius=np.interp(a,angles[order],radii[order],period=2*np.pi)+.0015
    waist=(1-smooth((p[:,1]-1.112)/.062))
    radial=np.linalg.norm(p[:,[0,2]]-centre,axis=1)
    wanted=np.maximum(radial,radius)
    for axis,c in ((0,0),(2,centre[1])):p[:,axis]=c+(p[:,axis]-c)*np.divide(radial+(wanted-radial)*waist,np.maximum(radial,1e-8))
    faces=np.array(source['upper_faces']).reshape(-1,3)
    # Coincident template vertices must not split after contact projection.
    _,inv=np.unique(np.round(raw,6),axis=0,return_inverse=True)
    for _ in range(5):
        selected=np.flatnonzero(weight>0)
        q,clearance,n=nearest(body,p[selected]);bad=clearance<.0005
        p[selected[bad]]=q[bad]+n[bad]*.0008
        accum=np.zeros((inv.max()+1,3));count=np.bincount(inv)
        np.add.at(accum,inv,p);p=(accum/np.maximum(count,1)[:,None])[inv]
    # Vertex-only clearance is insufficient: a triangle between safe points
    # can still cross the armpit. Resolve its centroids before accepting it.
    _,first=np.unique(inv,return_index=True)
    welded=np.array(polish_armpits(body,p[first].astype(np.float32).ravel().tolist(),inv[faces],threshold=.00002,margin=.00015,iterations=32)).reshape(-1,3)
    p=welded[inv]
    cache['upper']=p.astype(np.float32).ravel().tolist()
    cache['detail']=seat_details(cache['upper'],faces,source['detail'])
    # Seat the collar's outer edge against the shoulder; retain the rolled
    # stand and upper fold so the collar has structure rather than floating.
    detail=np.array(cache['detail']).reshape(-1,3)
    surface=trimesh.Trimesh(vertices=p,faces=faces,process=False)
    start=2*3*97
    for layer in range(2):
        for row in range(7):
            ids=np.arange(start+layer*7*97+row*97,start+layer*7*97+(row+1)*97)
            q,dist,tri=trimesh.proximity.closest_point(surface,detail[ids])
            t=smooth((row/6-.35)/.65)
            target=q+surface.face_normals[tri]*(.0025-layer*.0015)
            detail[ids]=detail[ids]*(1-t)+target*t
    # The collar edge stitch follows that seated edge, with a finer thread.
    begin=1940
    edge=detail[start+6*97:start+7*97]
    rings=detail[begin:2522].reshape(97,6,3)
    offset=rings-rings.mean(axis=1)[:,None]
    rings[:]=edge[:,None]+offset*.55+np.array([0,.0005,0])
    for begin,end in ((2720,3212),(4652,5264)):
        rings=detail[begin:end].reshape(-1,6,3);centers=rings.mean(axis=1)
        rings[:]=centers[:,None]+(rings-centers[:,None])*.55
    cache['detail']=detail.astype(np.float32).ravel().tolist()
    cache['upper_normals']=front_normals(cache['upper'],faces)
    # Smooth the lighting normals at very fine anatomical triangulation while
    # retaining the actual silhouette and cloth fold shape.
    normals=np.array(cache['upper_normals']).reshape(-1,3)
    e=np.unique(np.sort(np.vstack((inv[faces[:,[0,1]]],inv[faces[:,[1,2]]],inv[faces[:,[2,0]]])),axis=1),axis=0)
    e=e[e[:,0]!=e[:,1]]
    adj=sparse.coo_matrix((np.ones(len(e)*2),(e.ravel(),e[:,::-1].ravel())),shape=(inv.max()+1,inv.max()+1)).tocsr()
    degree=np.maximum(1,np.asarray(adj.sum(axis=1)).ravel())
    accum=np.zeros((inv.max()+1,3));np.add.at(accum,inv,normals)
    n=accum/np.maximum(np.linalg.norm(accum,axis=1),1e-12)[:,None]
    for _ in range(4):n=n*.4+(adj@n)/degree[:,None]*.6;n/=np.maximum(np.linalg.norm(n,axis=1),1e-12)[:,None]
    cache['upper_normals']=n[inv].astype(np.float32).ravel().tolist()
    assert np.isfinite(p).all() and np.isfinite(detail).all()
    target=OUT/'polo-contact-fit.json';target.write_text(json.dumps(cache,separators=(',',':')))
    report=dict(method='Local underarm recovery, continuous waist overlap, seated collar, finer topstitch; offline authored fit',vertices=len(p),vertices_before=len(previous),faces=len(faces),underarm_vertices=underarm_count,max_delta_mm=float(np.linalg.norm(p-original,axis=1).max()*1000),body_topology_and_rig_unchanged=True,garment_triangle_count_unchanged=True,simulation_particles_added=0)
    (OUT/'tailoring.json').write_text(json.dumps(report,indent=2));print(report,flush=True)

if __name__=='__main__':run()
