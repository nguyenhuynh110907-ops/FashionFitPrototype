"""Local, offline refinement of the generic technical mannequin.

No photo is converted to an unclothed person. Keep the existing rig, UVs,
weights, topology, and all geometry outside the chest transition region.
The original asset is backed up and used as the repeatable source.
"""
import hashlib
import json
from pathlib import Path
import struct
import numpy as np
from scipy import sparse
import trimesh

ROOT=Path(__file__).resolve().parent.parent
OUT=ROOT/'Verification/BodySilhouette'
TARGET=ROOT/'Assets/Resources/Fitting/female-mesh.bytes'


def read(data):
    magic,n=struct.unpack_from('<ii',data)
    assert magic==0x46525633
    p=np.frombuffer(data,dtype='<f4',count=n*3,offset=8).copy().reshape(-1,3)
    normals=np.frombuffer(data,dtype='<f4',count=n*3,offset=8+n*12).copy().reshape(-1,3)
    offset=8+n*64;bind,=struct.unpack_from('<i',data,offset);offset+=4+bind*64
    sub,=struct.unpack_from('<i',data,offset);offset+=4;faces=[]
    for _ in range(sub):
        count,=struct.unpack_from('<i',data,offset);offset+=4
        faces.append(np.frombuffer(data,dtype='<i4',count=count,offset=offset).copy().reshape(-1,3));offset+=count*4
    assert offset==len(data)
    return p,normals,np.vstack(faces)


def smooth(t):
    t=np.clip(t,0,1);return t*t*(3-2*t)


def run():
    before=OUT/'Before';before.mkdir(parents=True,exist_ok=True)
    backup=before/'female-mesh.bytes'
    if not backup.exists():backup.write_bytes(TARGET.read_bytes())
    data=backup.read_bytes();original,old_n,faces=read(data)
    _,index,inverse=np.unique(np.round(original,6),axis=0,return_index=True,return_inverse=True)
    p=original[index].astype(float);source=p.copy();f=inverse[faces]
    edges=np.vstack((f[:,[0,1]],f[:,[1,2]],f[:,[2,0]]));edges=np.unique(np.sort(edges,axis=1),axis=0);edges=edges[edges[:,0]!=edges[:,1]]
    adj=sparse.coo_matrix((np.ones(len(edges)*2),(edges.ravel(),edges[:,::-1].ravel())),shape=(len(p),len(p))).tocsr()
    degree=np.maximum(1,np.asarray(adj.sum(axis=1)).ravel())
    radial=np.sqrt(((abs(source[:,0])-.085)/.088)**2+((source[:,1]-1.255)/.088)**2)
    weight=(1-smooth((radial-.45)/.8))*smooth((source[:,2]-.035)/.045)
    # Fair the actual surface in 3D, with a gentle volume-preserving reverse
    # step. It retains two anatomical lobes rather than a spherical torso cap.
    for _ in range(60):
        p+=(adj@p/degree[:,None]-p)*(.46*weight[:,None])
        p+=(adj@p/degree[:,None]-p)*(-.48*weight[:,None])
    # Remove the small pointed projection without enlarging breast volume.
    peak=np.exp(-((abs(source[:,0])-.09)/.047)**2-((source[:,1]-1.255)/.050)**2)
    p[:,2]-=.0055*peak*weight
    delta=p-source;length=np.linalg.norm(delta,axis=1);delta*=np.minimum(1,.012/np.maximum(length,1e-12))[:,None]
    result=original.astype(float)+delta[inverse]
    # Exact welds stay exact, while vertices outside the region remain bitwise
    # identical (including hands, arms, face, back, waist, hips and legs).
    changed=np.linalg.norm(delta[inverse],axis=1)>1e-9
    result[~changed]=original[~changed]
    mesh=trimesh.Trimesh(vertices=result,faces=faces,process=False)
    cross=np.cross(result[faces[:,1]]-result[faces[:,0]],result[faces[:,2]]-result[faces[:,0]])
    previous=np.cross(original[faces[:,1]]-original[faces[:,0]],original[faces[:,2]]-original[faces[:,0]])
    active=np.maximum(np.linalg.norm(previous,axis=1),np.linalg.norm(cross,axis=1))>1e-9
    flipped=int(((np.einsum('ij,ij->i',cross,previous)<0)&active).sum())
    assert flipped==0, f'{flipped} faces reversed by chest refinement'
    sums=np.zeros_like(p)
    for k in range(3):np.add.at(sums,inverse[faces[:,k]],cross)
    n=sums[inverse];n/=np.maximum(np.linalg.norm(n,axis=1),1e-12)[:,None]
    normal_weight=weight[inverse];normals=old_n*(1-normal_weight[:,None])+n*normal_weight[:,None]
    normals/=np.maximum(np.linalg.norm(normals,axis=1),1e-12)[:,None]
    normals[normal_weight==0]=old_n[normal_weight==0]
    raw=bytearray(data);count=len(original);raw[8:8+count*12]=result.astype('<f4').tobytes();raw[8+count*12:8+count*24]=normals.astype('<f4').tobytes()
    assert raw[8+count*24:]==data[8+count*24:]
    assert np.isfinite(result).all() and np.isfinite(normals).all() and mesh.is_watertight and mesh.is_winding_consistent
    TARGET.write_bytes(raw)
    report=dict(status='passed',source_sha256=hashlib.sha256(data).hexdigest(),result_sha256=hashlib.sha256(raw).hexdigest(),
        vertices=count,changed_vertices=int(changed.sum()),max_displacement_mm=float(np.linalg.norm(result-original,axis=1).max()*1000),
        outside_region_positions_identical=bool(np.array_equal(result[~changed],original[~changed])),rig_uv_weights_topology_bytes_identical=True,
        watertight=bool(mesh.is_watertight),winding_consistent=bool(mesh.is_winding_consistent),flipped_faces=flipped,
        scope='Generic technical mannequin chest only, not reconstruction of the reference person')
    (OUT/'body_refinement.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
    np.savez_compressed(OUT/'body_delta.npz',changed=changed,delta=(result-original).astype(np.float32))
    print(json.dumps(report),flush=True)


if __name__=='__main__':run()
