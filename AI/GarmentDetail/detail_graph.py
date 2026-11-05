"""Pattern-aware graph preparation. Source UVs/panels remain intact for rendering."""
from pathlib import Path
import hashlib
import json
import sys

import numpy as np
import yaml

ROOT=Path(__file__).resolve().parent
CLOTH=ROOT.parent/'ClothData'
sys.path.insert(0,str(CLOTH))
from collect_data import DATA,RAW,read_json,write_json
from datasets import components
from prepare_data import ply_geometry,canonical_map

ROLE_NAMES=['torso','leg_or_skirt','sleeve','collar','waistband','cuff','strap','other']
FEATURE_NAMES=['rest_x','rest_y','rest_z','normal_x','normal_y','normal_z','near_waist','near_hip','near_upper_reference',
               'seam','free_boundary','collar_annotation','waist_annotation','armhole_annotation',
               'role_torso','role_leg_or_skirt','role_sleeve','role_collar','role_waistband','role_cuff','role_strap','role_other',
               'body_height','waist_ratio','hips_ratio','bust_ratio','log_bending']


class BodyFrame:
    """A proper rigid frame plus ONE uniform body scale, never garment centering."""
    def __init__(self,origin,up,front,height):
        self.origin=np.asarray(origin,dtype=np.float64); up=np.asarray(up,dtype=np.float64); front=np.asarray(front,dtype=np.float64)
        if height<=0 or not np.isfinite(height): raise ValueError('Invalid body scale')
        if np.linalg.norm(up)<1e-9: raise ValueError('Missing up direction')
        up=up/np.linalg.norm(up); front=front-up*np.dot(front,up)
        if np.linalg.norm(front)<1e-9: raise ValueError('Front/up directions are collinear')
        front/=np.linalg.norm(front); right=np.cross(up,front)
        self.basis=np.column_stack((right,up,front)); self.height=float(height)
        if not np.allclose(self.basis.T@self.basis,np.eye(3),atol=1e-8) or np.linalg.det(self.basis)<.999: raise ValueError('Invalid alignment frame')
    def local(self,points): return ((np.asarray(points)-self.origin)@self.basis/self.height)
    def world(self,points): return (np.asarray(points)*self.height)@self.basis.T+self.origin
    def metadata(self): return dict(origin_m=self.origin.tolist(),basis=self.basis.tolist(),height_m=self.height,up='+Y',front='+Z',method='body waist section; proper rigid frame and uniform height scale')


def role(name,label):
    text=(name+' '+str(label)).lower()
    if 'collar' in text: return 3
    if 'cuff' in text or 'wrist' in text: return 5
    if 'strap' in text: return 6
    if 'waistband' in text or text.startswith('wb') or label=='band': return 4
    if 'sleeve' in text or label=='arm': return 2
    if any(token in text for token in ('skirt','pant','leg','godet')): return 1
    if any(token in text for token in ('torso','body','bodice','shirt')): return 0
    return 7


def body_frame(measurements):
    waist=float(measurements['_waist_level'])/100
    body=[]
    with (RAW/'GarmentCodeData_v2/upstream/neutral_body/mean_all.obj').open() as stream:
        for line in stream:
            if line.startswith('v '): body.append([float(v) for v in line.split()[1:4]])
    body=np.array(body)
    section=body[np.abs(body[:,1]-waist)<.012]
    if len(section)<6: raise ValueError('Insufficient body waist section')
    centre=(section.min(0)+section.max(0))*.5; centre[1]=waist
    return BodyFrame(centre,[0,1,0],[0,0,1],float(measurements['height'])/100)


def build(item):
    if not item['body_mesh_available']: raise ValueError('This preparation phase requires the actual reference body mesh')
    raw=components(item)
    for kind,blob in raw.items():
        if hashlib.sha256(blob).hexdigest()!=item['component_hashes'][kind]: raise ValueError('Source component changed')
    rest,render_faces,uv=ply_geometry(raw['box_mesh'],include_uv=True)
    target,target_faces,target_uv=ply_geometry(raw['sim_mesh'],include_uv=True)
    if not np.array_equal(render_faces,target_faces) or not np.array_equal(uv,target_uv): raise ValueError('Rest/target render correspondence changed')
    mapping=canonical_map(target)
    first=np.unique(mapping,return_index=True)[1]
    if not np.array_equal(canonical_map(rest),mapping): raise ValueError('UV alias mapping differs between states')
    if np.abs(rest-rest[first][mapping]).max()>1e-5 or np.abs(target-target[first][mapping]).max()>1e-5: raise ValueError('UV aliases disagree geometrically')
    vertices=rest[first]/100; reference=target[first]/100; faces=mapping[render_faces]
    if np.any(np.diff(np.sort(faces,axis=1),axis=1)==0): raise ValueError('Collapsed physics face')
    spec=json.loads(raw['specification']); pattern=spec['pattern']; panels=pattern['panels']; names=pattern.get('panel_order',list(panels))
    if set(names)!=set(panels): raise ValueError('Panel order does not match specification')
    name_index={name:i for i,name in enumerate(names)}; stitches=pattern['stitches']; n=len(vertices)
    memberships=np.zeros((n,len(names)),dtype=bool); seam_membership=np.zeros((n,len(stitches)),dtype=bool)
    labels=raw['segmentation'].decode().splitlines()
    if len(labels)!=n: raise ValueError('Canonical semantic labels do not align')
    for i,text in enumerate(labels):
        for token in text.split(','):
            if token in name_index: memberships[i,name_index[token]]=True
            elif token.startswith('stitch_'):
                seam=int(token[7:])
                if seam<0 or seam>=len(stitches): raise ValueError('Invalid sewing seam index')
                seam_membership[i,seam]=True
                for endpoint in stitches[seam][:2]:
                    if endpoint['panel'] not in name_index: raise ValueError('Unknown seam panel')
                    panel=panels[endpoint['panel']]
                    if not 0<=endpoint['edge']<len(panel['edges']): raise ValueError('Invalid pattern edge')
                    memberships[i,name_index[endpoint['panel']]]=True
            else: raise ValueError('Unrecognized source segmentation token '+token)
    if not memberships.any(1).all(): raise ValueError('Vertex without panel ownership')
    all_edges=np.sort(np.concatenate((faces[:,[0,1]],faces[:,[1,2]],faces[:,[2,0]])),axis=1)
    edges,counts=np.unique(all_edges,axis=0,return_counts=True)
    # Multiple panels can meet at a sewn junction; keep and report these edges.
    nonmanifold=int(np.count_nonzero(counts>2))
    boundary=np.zeros(n,dtype=np.float32); boundary[edges[counts==1].ravel()]=1
    seam_mask=seam_membership.any(1)
    seam_edges=np.zeros(len(edges),dtype=bool)
    for seam in range(len(stitches)):
        seam_edges|=seam_membership[edges[:,0],seam]&seam_membership[edges[:,1],seam]
    common=memberships[faces[:,0]]&memberships[faces[:,1]]&memberships[faces[:,2]]
    face_panel=np.where(common.sum(1)==1,common.argmax(1),-1).astype(np.int32)
    annotation=yaml.load(raw['vertex_labels'],Loader=yaml.CSafeLoader) or {}
    masks={name:np.zeros(n,dtype=np.float32) for name in ('collar','waist','armhole')}
    for key,value in annotation.items():
        ids=np.array(value,dtype=np.int32)
        if len(ids) and (ids.min()<0 or ids.max()>=n): raise ValueError('Source attachment index out of range')
        low=key.lower()
        if 'collar' in low or 'strapless_top' in low: masks['collar'][ids]=1
        if 'waist' in low: masks['waist'][ids]=1
        if 'armhole' in low: masks['armhole'][ids]=1
    roles=np.zeros((n,len(ROLE_NAMES)),dtype=np.float32)
    for j,name in enumerate(names): roles[memberships[:,j],role(name,panels[name].get('label'))]=1
    body=yaml.load(raw['body_measurements'],Loader=yaml.CSafeLoader)['body']; frame=body_frame(body)
    x=frame.local(vertices).astype(np.float32); y=frame.local(reference).astype(np.float32)
    hip=(float(body['_waist_level'])-float(body['hips_line']))/100
    upper=(float(body['height'])-float(body['head_l']))/100
    normal=np.zeros_like(vertices)
    triangle_normal=np.cross(vertices[faces[:,1]]-vertices[faces[:,0]],vertices[faces[:,2]]-vertices[faces[:,0]])
    for corner in range(3): np.add.at(normal,faces[:,corner],triangle_normal)
    normal/=np.maximum(np.linalg.norm(normal,axis=1,keepdims=True),1e-9)
    global_values=np.array([frame.height,float(body['waist'])/(100*frame.height),float(body['hips'])/(100*frame.height),float(body['bust'])/(100*frame.height),np.log1p(item['upstream_bending'])/np.log(501)],dtype=np.float32)
    features=np.column_stack((x,normal,np.abs(vertices[:,1]-frame.origin[1])/frame.height,
            np.abs(vertices[:,1]-hip)/frame.height,np.abs(vertices[:,1]-upper)/frame.height,
            seam_mask,boundary,masks['collar'],masks['waist'],masks['armhole'],roles,np.tile(global_values,(n,1)))).astype(np.float32)
    if features.shape[1]!=len(FEATURE_NAMES): raise ValueError('Feature schema drift')
    if not np.isfinite(features).all() or not np.isfinite(y).all(): raise ValueError('Nonfinite training features')
    arrays=dict(features=features,rest=x,target=y,faces=faces.astype(np.int32),edges=edges.astype(np.int32),
        seam_edge_mask=seam_edges,anchor_mask=np.maximum(masks['collar'],masks['waist']),
        boundary_mask=boundary,render_to_graph=mapping.astype(np.int32),render_faces=render_faces.astype(np.int32),render_uv=uv,
        face_panel=face_panel,panel_membership=memberships,seam_membership=seam_membership)
    # Observation only: no transform ever uses the garment centre to reposition it.
    tri=reference[faces]; area=np.linalg.norm(np.cross(tri[:,1]-tri[:,0],tri[:,2]-tri[:,0]),axis=1)
    centre=np.average(tri.mean(1),weights=area,axis=0)
    metadata=dict(id=item['id'],design_group=item['design_group'],split=item['official_split'],body_frame=frame.metadata(),
        graph_vertices=n,render_vertices=len(rest),faces=len(faces),edges=len(edges),panel_names=names,
        seam_count=len(stitches),seam_edges=int(seam_edges.sum()),boundary_vertices=int(boundary.sum()),
        support_annotation_vertices=int(np.count_nonzero(arrays['anchor_mask'])),source_vertex_annotation_names=list(annotation),
        face_panel_ambiguous=int(np.count_nonzero(face_panel<0)),nonmanifold_edges=nonmanifold,source_units_in_meter=100,
        garment_area_centre_m=centre.tolist(),centering_rule='waist/body frame; preserve garment asymmetry and train length',
        feature_names=FEATURE_NAMES,component_roles=ROLE_NAMES,pattern_preserved=True,source_uv_preserved=True,
        source_component_hashes=item['component_hashes'],stitch_orientation_flags_preserved=True,
        reference_is_simulation=True,paired_real_photo=False,contact_loss_enabled=False,nonpenetration_certified=False)
    return arrays,metadata,spec,annotation


def save(item,folder):
    folder=Path(folder); folder.mkdir(parents=True,exist_ok=True)
    arrays,metadata,spec,annotation=build(item)
    path=folder/(item['id']+'.npz'); np.savez_compressed(path,**arrays)
    metadata['npz_sha256']=hashlib.sha256(path.read_bytes()).hexdigest()
    write_json(folder/(item['id']+'.json'),metadata)
    write_json(folder/(item['id']+'.pattern.json'),spec)
    write_json(folder/(item['id']+'.annotations.json'),annotation)
    if sum(p.stat().st_size for p in DATA.rglob('*') if p.is_file())>30_000_000_000: raise RuntimeError('30 GB data limit reached')
    return metadata
