"""Offline source-asset review, NOT a Unity screenshot or VR test.
Dependencies: numpy, trimesh, pyrender, Pillow. Run with PYOPENGL_PLATFORM=egl if supported.
"""
from pathlib import Path
import os
os.environ.setdefault('PYOPENGL_PLATFORM','egl')
import numpy as np
# pyrender 0.1.45 retains this NumPy 1.x alias.
np.infty=np.inf
import trimesh,pyrender
from PIL import Image,ImageDraw,ImageFont
ROOT=Path(__file__).resolve().parents[1]
a=np.load(ROOT/'Tools/female-qa.npz');body=trimesh.Trimesh(a['v'],a['f'],process=False)
# The exported OBJ uses right-handed source coordinates; mirror back to internal coordinates.
skirt=trimesh.load(ROOT/'Verification/Settled_Skirt.obj',force='mesh',process=False);skirt.vertices[:,0]*=-1;skirt.faces=skirt.faces[:,::-1]

def pose(eye,target):
 eye=np.array(eye,float);z=eye-np.array(target,float);z/=np.linalg.norm(z);x=np.cross([0,1,0],z);x/=np.linalg.norm(x);y=np.cross(z,x);m=np.eye(4);m[:3,:3]=np.stack([x,y,z],axis=1);m[:3,3]=eye;return m

def render(eye,target,yfov,width,height):
 scene=pyrender.Scene(bg_color=[.69,.70,.71,1],ambient_light=[.28,.29,.3])
 clay=pyrender.MetallicRoughnessMaterial(baseColorFactor=[.6,.57,.54,1],metallicFactor=0,roughnessFactor=.76)
 fabric=pyrender.MetallicRoughnessMaterial(baseColorFactor=[.22,.31,.38,1],metallicFactor=0,roughnessFactor=.78,doubleSided=True)
 floor=pyrender.MetallicRoughnessMaterial(baseColorFactor=[.46,.48,.49,1],metallicFactor=0,roughnessFactor=.85)
 wall=pyrender.MetallicRoughnessMaterial(baseColorFactor=[.69,.70,.71,1],metallicFactor=0,roughnessFactor=.9)
 scene.add(pyrender.Mesh.from_trimesh(body,material=clay,smooth=True));scene.add(pyrender.Mesh.from_trimesh(skirt,material=fabric,smooth=True))
 for ext,centre,mat in [([8,.1,8],[0,-.055,0],floor),([8,3.3,.1],[0,1.65,-4],wall),([.1,3.3,8],[-4,1.65,0],wall),([.1,3.3,8],[4,1.65,0],wall),([8,3.3,.1],[0,1.65,4],wall)]:
  m=trimesh.creation.box(extents=ext);m.apply_translation(centre);scene.add(pyrender.Mesh.from_trimesh(m,material=mat))
 scene.add(pyrender.PerspectiveCamera(yfov=yfov,znear=.05,zfar=15),pose=pose(eye,target))
 scene.add(pyrender.DirectionalLight(color=np.ones(3),intensity=2.0),pose=pose([3,5,4],[0,.9,0]));scene.add(pyrender.DirectionalLight(color=np.ones(3),intensity=.65),pose=pose([-3,4,-3],[0,.9,0]))
 renderer=pyrender.OffscreenRenderer(width,height);color,depth=renderer.render(scene,flags=pyrender.RenderFlags.SHADOWS_DIRECTIONAL);renderer.delete();return Image.fromarray(color)

front=render([1.65,1.6,2.35],[0,.90,0],.72,1024,1000)
detail=render([.8,1.05,1.18],[0,.77,.04],.64,640,1000)
canvas=Image.new('RGB',(1664,1120),'#16212b');canvas.paste(front,(0,100));canvas.paste(detail,(1024,100));draw=ImageDraw.Draw(canvas)
font_path='/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf'
try:font=ImageFont.truetype(font_path,22);small=ImageFont.truetype(font_path,17)
except:font=small=ImageFont.load_default()
draw.text((24,16),'FITTING ROOM / v0.5',font=font,fill='white');draw.text((24,52),'Offline asset render — NOT Unity execution. Temporary CC0 female mannequin; original user model pending.',font=small,fill='#bad0d7')
draw.text((24,1100),'Plain room / fixed origin / authored sample skirt / approximate local cloth',font=small,fill='#bad0d7')
(ROOT/'Preview').mkdir(exist_ok=True);canvas.save(ROOT/'Preview/FittingRoom_Source_Preview.png');print('Saved offline source preview')
