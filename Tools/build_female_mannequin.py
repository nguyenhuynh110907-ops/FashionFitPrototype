"""Build a neutral technical female stand-in from CC0 MakeHuman core asset data.
NOT the original user model; NOT a photo reconstruction. No textures or downloaded code.
Rebuild: pip install numpy scipy; python Tools/build_female_mannequin.py
Source data and CC0 declaration are retained in Tools/SourceAssets.
"""
from pathlib import Path
import numpy as np
from scipy.spatial.transform import Rotation
import json,struct,hashlib
ROOT=Path(__file__).resolve().parents[1];SRC=ROOT/'Tools/SourceAssets';OUT=ROOT/'Assets/Resources/Fitting';OUT.mkdir(parents=True,exist_ok=True)
v=[];groups={};group='';uv=[];faces=[]
for line in (SRC/'base.obj').read_text().splitlines():
 a=line.split()
 if not a:continue
 if a[0]=='v':v.append(list(map(float,a[1:4])))
 elif a[0]=='vt':uv.append(list(map(float,a[1:3])))
 elif a[0]=='g':group=a[1];groups[group]=[]
 elif a[0]=='f':
  polygon=[int(x.split('/')[0])-1 for x in a[1:]];groups[group].append(polygon)
  if group=='body':
   for j in range(1,len(polygon)-1):faces.append([polygon[0],polygon[j],polygon[j+1]])
v=np.array(v,dtype=float)
for line in (SRC/'caucasian-female-young.target').read_text().splitlines():
 a=line.split()
 if a and not a[0].startswith('#'):v[int(a[0])]+=np.array(list(map(float,a[1:])))
f=np.array(faces);used=np.unique(f);scale=1.72/np.ptp(v[used,1]);v[:,1]-=v[used,1].min();v*=scale

def joint(name):return v[np.unique(np.concatenate(groups['joint-'+name]))].mean(0)
def J(x,y,z):return np.array([x,y,z],float)
# Simplified 17-bone research rig, derived from actual skeleton helper positions.
spec=[('Hips',-1,joint('pelvis'),joint('spine-3')),('Spine',0,joint('spine-3'),joint('spine-2')),('Spine2',1,joint('spine-2'),joint('spine-1')),('Neck',2,J(0,1.43,.025),J(0,1.51,.03)),('Head',3,J(0,1.51,.03),joint('head'))]
for side,start,parent in [('l',5,0),('r',8,0)]:
 title='Left' if side=='l' else 'Right'
 spec.extend([(title+'UpLeg',parent,joint(side+'-upper-leg'),joint(side+'-knee')),(title+'Leg',start,joint(side+'-knee'),joint(side+'-ankle')),(title+'Foot',start+1,joint(side+'-ankle'),joint(side+'-foot-1'))])
for side,start in [('l',11),('r',14)]:
 title='Left' if side=='l' else 'Right';hand=joint(side+'-hand');end=hand+J(.075 if side=='l' else -.075,-.055,.02)
 spec.extend([(title+'Arm',2,joint(side+'-shoulder'),joint(side+'-elbow')),(title+'ForeArm',start,joint(side+'-elbow'),hand),(title+'Hand',start+1,hand,end)])
# Keep only body surface, removing joint cubes and all helper clothing geometry.
old_to_new=np.full(len(v),-1);old_to_new[used]=np.arange(len(used));f=old_to_new[f];v=v[used]
d=[]
for name,parent,a,b in spec:
 ab=b-a;t=np.clip(((v-a)@ab)/(ab@ab),0,1);d.append(np.linalg.norm(v-(a+t[:,None]*ab),axis=1))
d=np.stack(d,1)
for i,(name,_,a,b) in enumerate(spec):
 if name.startswith('Left'):d[v[:,0]<-.025,i]+=1
 if name.startswith('Right'):d[v[:,0]>.025,i]+=1
 if 'Arm' in name or 'Hand' in name:d[np.abs(v[:,0])<.19,i]+=.35
ids=np.argsort(d,axis=1)[:,:4];dist=np.take_along_axis(d,ids,axis=1);w=np.exp(-(dist-dist[:,0,None])*75);w/=w.sum(1)[:,None]
# Lower A-pose arms slightly, baking the transform into the new bind geometry.
for sign,first in [(1,11),(-1,14)]:
 shoulder=spec[first][2].copy();rot=Rotation.from_euler('z',-sign*22,degrees=True).as_matrix()
 influence=np.sum(w*((ids>=first)&(ids<first+3)),axis=1)
 v+=((v-shoulder)@rot.T+shoulder-v)*influence[:,None]
 for i in range(first,first+3):
  name,parent,a,b=spec[i];spec[i]=(name,parent,rot@(a-shoulder)+shoulder,rot@(b-shoulder)+shoulder)
# One subdivision pass for smooth silhouette, interpolating the four bone influences.
edge_cache={};verts=list(v);weights=[{int(i):float(a)for i,a in zip(ii,ww)}for ii,ww in zip(ids,w)]
def midpoint(a,b):
 key=tuple(sorted((int(a),int(b))))
 if key not in edge_cache:
  edge_cache[key]=len(verts);verts.append((v[a]+v[b])*.5);mix={}
  for j in [a,b]:
   for k,val in weights[j].items():mix[k]=mix.get(k,0)+val*.5
  weights.append(mix)
 return edge_cache[key]
faces=[]
for a,b,c in f:
 ab=midpoint(a,b);bc=midpoint(b,c);ca=midpoint(c,a);faces.extend([[a,ab,ca],[ab,b,bc],[ca,bc,c],[ab,bc,ca]])
v=np.array(verts,dtype='f4');f=np.array(faces,dtype='i4');ids=np.zeros((len(v),4),dtype='i4');w=np.zeros((len(v),4),dtype='f4')
for j,mix in enumerate(weights):
 top=sorted(mix.items(),key=lambda x:-x[1])[:4]
 for k,(i,a) in enumerate(top):ids[j,k]=i;w[j,k]=a
 w[j]/=w[j].sum()
n=np.zeros_like(v);tri=np.cross(v[f[:,1]]-v[f[:,0]],v[f[:,2]]-v[f[:,0]])
for k in range(3):np.add.at(n,f[:,k],tri)
n/=np.maximum(np.linalg.norm(n,axis=1)[:,None],1e-12)
uv=np.stack([np.arctan2(v[:,2],v[:,0])/(2*np.pi)+.5,v[:,1]/1.72],1).astype('f4')
rig={'nodes':[],'bones':list(range(len(spec))),'rootBone':0}
for name,parent,a,b in spec:
 pos=a-(spec[parent][2] if parent>=0 else 0);rig['nodes'].append(dict(name=name,parent=parent,position=pos.tolist(),rotation=[0,0,0,1],scale=[1,1,1]))
(OUT/'female-rig.json').write_text(json.dumps(rig,indent=2))
with (OUT/'female-mesh.bytes').open('wb') as s:
 s.write(struct.pack('<ii',0x46525633,len(v)))
 for a in [v,n,uv,w,ids]:s.write(a.astype(a.dtype.newbyteorder('<')).tobytes())
 s.write(struct.pack('<i',len(spec)))
 for _,_,a,_ in spec:
  mat=np.eye(4,dtype='f4');mat[:3,3]=-a;s.write(mat.tobytes())
 s.write(struct.pack('<ii',1,len(f)*3));s.write(f.astype('<i4').tobytes())
with (ROOT/'Samples/Technical_Female_Body.obj').open('w') as s:
 s.write('# CC0 MakeHuman-derived technical stand-in, NOT original user character.\n# Metres, Y-up, +Z-front.\n')
 for p in v:s.write('v %.7f %.7f %.7f\n'%tuple(p*[-1,1,1]))
 for face in f[:,::-1]:s.write('f %d %d %d\n'%tuple(face+1))
np.savez(ROOT/'Tools/female-qa.npz',v=v,f=f,n=n,w=w,ids=ids)
report=dict(source='MakeHuman CC0 body + adult female target',not_user_original=True,vertices=len(v),triangles=len(f),height=float(np.ptp(v[:,1])),source_sha256={p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in SRC.iterdir()if p.is_file()})
(ROOT/'Verification/Female_Data_Check.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
