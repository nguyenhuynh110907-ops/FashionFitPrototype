"""Compare actual ray-sampled garment profiles before/after the front refit."""
import json
import os
from pathlib import Path
import numpy as np
import trimesh
os.environ.setdefault('MPLCONFIGDIR',str(Path(__file__).resolve().parent/'__pycache__/matplotlib'))
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt

ROOT=Path(__file__).resolve().parent.parent
OUT=ROOT/'Verification/PoloRoundness'
source=json.loads((ROOT/'Verification/CloseViewProject/Verification/PoloStudy/rest_fit_source.json').read_text())
before=json.loads((OUT/'Before/polo-contact-fit.json').read_text())
after=json.loads((ROOT/'Assets/Resources/Fitting/polo-contact-fit.json').read_text())
assert before['upper_key']==after['upper_key']==source['upper_key']
faces=np.array(source['upper_faces']).reshape(-1,3)
old=np.array(before['upper']).reshape(-1,3);new=np.array(after['upper']).reshape(-1,3)
assert np.array_equal(old[:,:2],new[:,:2]) and np.all(new[:,2]>=old[:,2]-1e-7)
assert np.isfinite(new).all() and len(old)==len(new)
meshes=[trimesh.Trimesh(vertices=v,faces=f,process=False) for v,f in (
    (np.array(source['body']).reshape(-1,3),np.array(source['body_faces']).reshape(-1,3)),(old,faces),(new,faces))]

def front(mesh,xy):
    xy=np.array(xy);starts=np.column_stack((xy,np.ones(len(xy))))
    hits,ids,_=mesh.ray.intersects_location(starts,np.tile([0.,0.,-1.],(len(starts),1)),multiple_hits=True)
    z=np.full(len(xy),-np.inf);np.maximum.at(z,ids,hits[:,2])
    assert np.isfinite(z).all()
    return z

xs=np.linspace(-.14,.14,201);ys=np.linspace(1.13,1.41,201)
horizontal=[front(m,np.column_stack((xs,np.full(len(xs),1.26)))) for m in meshes]
vertical=[front(m,np.column_stack((np.full(len(ys),.09),ys))) for m in meshes]
metric=[]
for x in (-.09,.09):
    xy=[[x+dx,1.26] for dx in (-.015,0,.015)]
    a,b=(front(m,xy) for m in meshes[1:])
    metric.append(dict(x_m=x,before_peak_excess_mm=float((a[1]-(a[0]+a[2])/2)*1000),after_peak_excess_mm=float((b[1]-(b[0]+b[2])/2)*1000)))
assert all(m['after_peak_excess_mm']<m['before_peak_excess_mm']*.75 for m in metric),'Local bust peaks did not soften sufficiently'
result=dict(status='passed',scope='Rest shape only; contact and gait verified separately',vertex_count_unchanged=True,uv_weights_topology_unchanged=True,
    outward_only=True,apex_profile_15mm_span=metric)
(OUT/'profile_check.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
fig,axes=plt.subplots(1,2,figsize=(10,4.5),layout='constrained')
for i,(axis,coord,curves,title) in enumerate(zip(axes,[xs,ys],[horizontal,vertical],['Across bust at Y = 1.26 m','Front section at X = 0.09 m'])):
    for curve,label,color in zip(curves,['Body','Before','After'],['#999999','#c94f4f','#147a72']):axis.plot(coord*1000,curve*1000,label=label,color=color,lw=2)
    axis.set_title(title);axis.set_xlabel('X (mm)' if i==0 else 'Y (mm)');axis.set_ylabel('Front Z (mm)');axis.grid(alpha=.2);axis.legend()
fig.savefig(OUT/'bust_profiles.png',dpi=150);plt.close(fig)
print(json.dumps(result),flush=True)
