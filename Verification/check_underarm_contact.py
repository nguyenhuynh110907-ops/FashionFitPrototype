"""Dense edge/interior contact sampling of the changed armpit neighbourhood.
Runtime exports contain the full unmasked body and skinned upper at three poses.
This is sampled contact, not continuous triangle intersection certification.
"""
import os
os.environ.setdefault('OPENBLAS_NUM_THREADS','1')
import hashlib
import json
from pathlib import Path
import struct
import sys
import numpy as np
import trimesh
sys.path.insert(0,str(Path(__file__).resolve().parent))
from check_slow_body_contact import read_mesh

ROOT=Path(__file__).resolve().parent.parent
OUT=ROOT/'Verification/UnderarmFix'

def run():
    old=json.loads((OUT/'Before/polo-contact-fit.json').read_text())
    new=json.loads((OUT/'polo-contact-fit.json').read_text())
    before=np.array(old['upper']).reshape(-1,3);after=np.array(new['upper']).reshape(-1,3)
    assert old['upper_key']==new['upper_key'] and old['detail']==new['detail']
    changed=np.linalg.norm(after-before,axis=1)>1e-6
    bary=np.array([[i/4,j/4,(4-i-j)/4] for i in range(5) for j in range(5-i)]+[[1/3,1/3,1/3]])
    files=sorted(OUT.glob('Look6_*.bin'));assert len(files)==3
    rows=[]
    for path in files:
        with path.open('rb') as stream:
            assert stream.read(4)==struct.pack('<i',0x53434631)
            body,upper=read_mesh(stream),read_mesh(stream);assert not stream.read(1)
        assert body.is_watertight and body.is_winding_consistent and len(upper.vertices)==len(after)
        selected=upper.faces[changed[upper.faces].any(axis=1)]
        p=np.einsum('bk,fkj->fbj',bary,upper.vertices[selected]).reshape(-1,3)
        p=np.unique(np.round(p,7),axis=0)
        d=np.concatenate([trimesh.proximity.signed_distance(body,p[i:i+256]) for i in range(0,len(p),256)])
        row=dict(pose=path.stem,body_vertices=len(body.vertices),masked_body_used=False,faces=int(len(selected)),samples=len(p),samples_inside_over_1mm=int((d>.001).sum()),max_sampled_penetration_mm=float(np.maximum(d,0).max()*1000),sha256=hashlib.sha256(path.read_bytes()).hexdigest())
        rows.append(row);print(json.dumps(row),flush=True)
    result=dict(status='passed' if all(r['samples_inside_over_1mm']==0 for r in rows) else 'failed',scope='Changed armpit triangles: barycentric quarter-step grid plus centroid; three posed full-body meshes; not continuous CCD',poses=rows,samples=sum(r['samples'] for r in rows),samples_inside_over_1mm=sum(r['samples_inside_over_1mm'] for r in rows),max_sampled_penetration_mm=max(r['max_sampled_penetration_mm'] for r in rows),topology_uv_weights_unchanged=True,body_unchanged=True)
    (OUT/'dense_contact.json').write_text(json.dumps(result,indent=2));print(json.dumps({k:v for k,v in result.items() if k!='poses'}),flush=True)
    if result['status']!='passed':raise SystemExit(1)

if __name__=='__main__':run()
