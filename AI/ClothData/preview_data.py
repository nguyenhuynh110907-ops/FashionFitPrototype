import json
import os
from pathlib import Path
import sqlite3
import sys

ROOT=Path(__file__).resolve().parent
os.environ.setdefault('MPLCONFIGDIR',str(ROOT.parent.parent/'.ml-runtime/matplotlib'))

import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
from matplotlib.collections import PolyCollection
import numpy as np
import trimesh

sys.path.insert(0,str(ROOT))
from collect_data import DATA,RAW
from datasets import components
from prepare_data import ply_geometry


def render():
    db=sqlite3.connect(DATA/'Processed/garments.sqlite3')
    rows=[json.loads(row[0]) for row in db.execute("SELECT detail_json FROM garments WHERE eligible=1 AND json_extract(detail_json,'$.body_variant')='default' ORDER BY id LIMIT 3")]
    db.close()
    body=trimesh.load_mesh(RAW/'GarmentCodeData_v2/upstream/neutral_body/mean_all.obj',process=False)
    fig,axes=plt.subplots(1,6,figsize=(15,5),layout='constrained')
    for index,item in enumerate(rows):
        data=components(item); vertices,faces=ply_geometry(data['sim_mesh']); vertices=vertices/item['units_in_meter']
        triangles=np.concatenate((body.vertices[body.faces],vertices[faces]))
        colors=np.tile([.68,.65,.60,1.],(len(triangles),1)); colors[len(body.faces):]=[.15,.36,.57,1.]
        normals=np.cross(triangles[:,1]-triangles[:,0],triangles[:,2]-triangles[:,0])
        normals/=np.maximum(np.linalg.norm(normals,axis=1,keepdims=True),1e-12)
        shade=.6+.4*np.abs(normals@np.array([.3,.4,.866]))
        colors[:,:3]*=shade[:,None]
        for side in (0,1):
            ax=axes[index*2+side]; horizontal=0 if side==0 else 2; depth=2 if side==0 else 0
            order=np.argsort(triangles[:,:,depth].mean(1))
            xy=triangles[:,:,[horizontal,1]]
            ax.add_collection(PolyCollection(xy[order],facecolors=colors[order],edgecolors='none',antialiased=False))
            ax.autoscale(); ax.set_aspect('equal'); ax.set_ylim(-.03,1.9); ax.axis('off')
            ax.set_title(('Front' if side==0 else 'Side')+' · '+str(item['panels'])+' panels',fontsize=10)
            if side==0: ax.text(.5,-.04,item['element'],transform=ax.transAxes,ha='center',fontsize=8)
    fig.suptitle('GarmentCodeData v2 · source drapes + neutral body · coordinates converted to meters\nVisual examples only; no nonintersection certificate',fontsize=12)
    folder=DATA/'Previews'; folder.mkdir(parents=True,exist_ok=True)
    fig.savefig(folder/'static_drapes.png',dpi=120); plt.close(fig)


if __name__=='__main__': render()
