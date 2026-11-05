"""Offline surface fairing and contact projection for this technical avatar only.

Not ML, reconstruction, or a runtime collision solver. UVs, topology, and skin
weights are retained. Unity accepts the cache only for exact source mesh hashes.
Independent posed-body QA is still required after baking.
"""
import json
from pathlib import Path
import sys
import numpy as np
from scipy import sparse
from scipy.special import logsumexp
from scipy.ndimage import distance_transform_edt, gaussian_filter
from scipy.interpolate import RegularGridInterpolator
import trimesh


def nearest(body, p):
    out=[]
    for i in range(0,len(p),384):
        q,d,t=trimesh.proximity.closest_point(body,p[i:i+384])
        n=body.face_normals[t]
        # Local closest-face sign for projection; independent QA uses the full
        # trimesh signed-distance/ray test, including edge ambiguity.
        sign=np.sign(np.einsum('ij,ij->i',p[i:i+384]-q,n))
        out.append((q,d*sign,n))
    return tuple(np.concatenate([r[k] for r in out]) for k in range(3))


def bake(body, points, faces, smooth=False):
    original=points.copy()
    _,index,inverse=np.unique(np.round(points,6),axis=0,return_index=True,return_inverse=True)
    p=points[index].copy(); f=inverse[faces]
    edge=np.vstack((f[:,[0,1]],f[:,[1,2]],f[:,[2,0]]))
    edge=np.sort(edge,axis=1);edge=edge[edge[:,0]!=edge[:,1]]
    edge,count=np.unique(edge,axis=0,return_counts=True)
    boundary=np.zeros(len(p),bool);boundary[edge[count==1].ravel()]=True
    adjacency=sparse.coo_matrix((np.ones(len(edge)*2),(edge.ravel(),edge[:,::-1].ravel())),shape=(len(p),len(p))).tocsr()
    degree=np.maximum(1,np.asarray(adjacency.sum(axis=1)).ravel())
    if smooth:
        for _ in range(75):
            delta=adjacency@p/degree[:,None]-p
            delta[boundary]=0
            p+=delta*.42
    def margin(p,outer,inner):
        underarm=(np.abs(p[:,0])>.15)&(np.abs(p[:,0])<.25)&(p[:,1]>1.23)&(p[:,1]<1.40)
        return np.where(underarm,inner,outer)
    for iteration in range(30):
        q,clearance,n=nearest(body,p)
        target=margin(p,.005,.0018)
        bad=clearance<target*.8
        if bad.any():p[bad]=q[bad]+n[bad]*target[bad,None]
        # A face can cut into the body even when all its corners are outside.
        centers=p[f].mean(axis=1)
        q,clearance,n=nearest(body,centers)
        target=margin(centers,.004,.0012)
        bad=clearance<target*.6
        print(json.dumps(dict(iteration=iteration,vertices=int(len(p)),face_corrections=int(bad.sum()),min_centroid_clearance_mm=float(clearance.min()*1000))),flush=True)
        if not bad.any():break
        ids=f[bad].ravel();shift=np.repeat((n[bad]*(target[bad]-clearance[bad])[:,None]),3,axis=0)
        correction=np.zeros_like(p);weights=np.zeros(len(p))
        np.add.at(correction,ids,shift);np.add.at(weights,ids,1)
        correction/=np.maximum(1,weights)[:,None]
        p+=correction
    # Finish on a vertex projection: the last face correction can otherwise
    # move a shared corner back into a neighbouring body surface.
    for _ in range(8):
        q,clearance,n=nearest(body,p)
        target=margin(p,.005,.0018);bad=clearance<target*.6
        if not bad.any():break
        p[bad]=q[bad]+n[bad]*target[bad,None]
    result=p[inverse]
    print(json.dumps(dict(max_displacement_mm=float(np.linalg.norm(result-original,axis=1).max()*1000),mean_displacement_mm=float(np.linalg.norm(result-original,axis=1).mean()*1000))),flush=True)
    return result.astype(np.float32).ravel().tolist()


def polish_armpits(body,values,faces,threshold=.0003,margin=.0004,iterations=16):
    p=np.array(values).reshape(-1,3)
    for iteration in range(iterations):
        centers=p[faces].mean(axis=1);samples=np.vstack((p,centers))
        selected=np.flatnonzero((abs(samples[:,0])>.15)&(abs(samples[:,0])<.25)&(samples[:,1]>1.23)&(samples[:,1]<1.40))
        d=np.concatenate([trimesh.proximity.signed_distance(body,samples[selected[i:i+384]]) for i in range(0,len(selected),384)])
        bad=selected[d>threshold]
        print(json.dumps(dict(polish_iteration=iteration,inside_over_tolerance=int(len(bad)),tolerance_mm=threshold*1000,max_signed_mm=float(d.max()*1000))),flush=True)
        if not len(bad):break
        q,dist,n=nearest(body,samples[bad]);shift=n*(margin-dist)[:,None]
        correction=np.zeros_like(p);weight=np.zeros(len(p))
        for id,delta in zip(bad,shift):
            ids=[id] if id<len(p) else faces[id-len(p)]
            for vertex in ids:correction[vertex]+=delta;weight[vertex]+=1
        p+=correction/np.maximum(1,weight)[:,None]
    return p.astype(np.float32).ravel().tolist()


def round_front(values,faces):
    """Give woven cloth a broad, rounded front envelope instead of copying
    anatomical peaks. Only expands the front; neck, cuffs and back are retained.
    The parabolic support radius bounds the sharpness of each bust peak, while
    soft-max joins the two sides without a centre ridge. This is offline fit.
    """
    p=np.array(values).reshape(-1,3);original=p.copy()
    surface=trimesh.Trimesh(vertices=p,faces=faces,process=False)
    x,y=np.meshgrid(np.arange(-.16,.1601,.004),np.arange(1.14,1.4101,.004))
    xy=np.column_stack((x.ravel(),y.ravel()))
    starts=np.column_stack((xy,np.ones(len(xy))))
    hits,ids,_=surface.ray.intersects_location(starts,np.tile([0.,0.,-1.],(len(starts),1)),multiple_hits=True)
    z=np.full(len(xy),-np.inf);np.maximum.at(z,ids,hits[:,2])
    valid=np.isfinite(z)&(z>.05);xy,z=xy[valid],z[valid]
    def smooth(t):
        t=np.clip(t,0,1);return t*t*(3-2*t)
    weight=(1-smooth((abs(p[:,0])-.115)/.06))*smooth((p[:,1]-1.145)/.04)*(1-smooth((p[:,1]-1.375)/.045))*smooth((p[:,2]-.055)/.035)
    selected=np.flatnonzero(weight>0)
    for begin in range(0,len(selected),256):
        group=selected[begin:begin+256];q=p[group]
        # Horizontal radius bridges the bust; vertical radius rounds the
        # transition above/below it. No spherical "cups" are added.
        score=z[None,:]-(q[:,None,0]-xy[None,:,0])**2/(2*.16)-(q[:,None,1]-xy[None,:,1])**2/(2*.075)
        envelope=.00045*logsumexp(score/.00045,axis=1)
        p[group,2]+=np.maximum(0,envelope-p[group,2])*weight[group]
    # Small geometric tension ridges near the side/underarm transition. They
    # are authored fitting detail, not a claim of simulated material response.
    lateral=smooth((abs(p[:,0])-.065)/.035)*(1-smooth((abs(p[:,0])-.145)/.025))
    line=1.205+.75*(abs(p[:,0])-.07)
    ridges=.0009*(.5+.5*np.sin((p[:,1]-line)*2*np.pi/.030))**2*np.exp(-((p[:,1]-line)/.045)**2)
    p[:,2]+=ridges*lateral*weight
    report=dict(method='outward-only local rounded fit with subtle authored tension ridges',horizontal_support_radius_m=.16,vertical_support_radius_m=.075,
        changed_vertices=int((p[:,2]-original[:,2]>.00001).sum()),max_added_ease_mm=float((p[:,2]-original[:,2]).max()*1000),
        vertices=len(p),topology_uv_and_weights_changed=False)
    print(json.dumps(report),flush=True)
    return p.astype(np.float32).ravel().tolist(),report


def seat_details(upper,faces,values):
    """Seat the authored placket, threads and hardware on the actual shirt,
    rather than the coarse body radius profile (which can leave floating seams).
    Layout ranges follow the explicit construction order in TailoredPoloStudy.
    """
    surface=trimesh.Trimesh(vertices=np.array(upper).reshape(-1,3),faces=faces,process=False)
    p=np.array(values).reshape(-1,3)
    def front(xy):
        starts=np.column_stack((xy,np.full(len(xy),1.0)))
        hits,ids,_=surface.ray.intersects_location(starts,np.tile([0,0,-1.0],(len(xy),1)),multiple_hits=True)
        z=np.full(len(xy),-np.inf);np.maximum.at(z,ids,hits[:,2])
        if not np.isfinite(z).all():raise ValueError('Detail ray missed shirt surface')
        return z
    # Collar: 2,522 points. Placket: two 33x3 layers.
    start=2522;layer=99
    z=front(p[start:start+2*layer,:2]);p[start:start+layer,2]=z[:layer]+.003
    p[start+layer:start+2*layer,2]=z[layer:]+.001
    # Two vertical topstitch tubes (41 rings with six corners each).
    for begin,end,relief in ((2720,3212,.004),(4652,5264,.0015)):
        rings=p[begin:end].reshape(-1,6,3);center=rings.mean(axis=1);offset=rings[:,:,2]-center[:,None,2]
        rings[:,:,2]=front(center[:,:2])[:,None]+relief+offset
    # Keep each button's tiny tube cross-sections and its shape intact.
    for button in range(4):
        a=3212+button*360;z=front(np.array([[0,1.397-button*.061]]))[0]
        # The first 162 points are the first G stroke; its tube-ring centres
        # are exactly 8 mm above the original authored front profile.
        anchor=p[a:a+162,2].mean();p[a:a+360,2]+=z+.008-anchor
    print('Seated placket, button hardware and topstitch on fitted upper',flush=True)
    return p.astype(np.float32).ravel().tolist()


def front_normals(values,faces):
    """Normals of the fitted outer cloth surface, independent of anatomical
    undercuts retained in the body-derived template's triangle connectivity.
    UVs/positions stay unchanged. The front height field is filtered at 6 mm.
    """
    p=np.array(values).reshape(-1,3)
    _,inverse=np.unique(np.rint(p*100000).astype(np.int64),axis=0,return_inverse=True)
    accum=np.zeros((inverse.max()+1,3));cross=np.cross(p[faces[:,1]]-p[faces[:,0]],p[faces[:,2]]-p[faces[:,0]])
    for k in range(3):np.add.at(accum,inverse[faces[:,k]],cross)
    n=accum[inverse];length=np.linalg.norm(n,axis=1);n/=np.maximum(length,1e-12)[:,None];n[length<1e-8]=[0,1,0]
    xs=np.arange(-.185,.1851,.003);ys=np.arange(1.12,1.4401,.003);x,y=np.meshgrid(xs,ys);xy=np.column_stack((x.ravel(),y.ravel()))
    surface=trimesh.Trimesh(vertices=p,faces=faces,process=False)
    hits,ids,_=surface.ray.intersects_location(np.column_stack((xy,np.ones(len(xy)))),np.tile([0.,0.,-1.],(len(xy),1)),multiple_hits=True)
    z=np.full(len(xy),-np.inf);np.maximum.at(z,ids,hits[:,2]);z=z.reshape(x.shape)
    missing=~np.isfinite(z);nearest_id=distance_transform_edt(missing,return_distances=False,return_indices=True);z[missing]=z[tuple(nearest_id[:,missing])]
    z=gaussian_filter(z,sigma=2);dy,dx=np.gradient(z,ys,xs)
    query=np.column_stack((p[:,1],p[:,0]))
    gx=RegularGridInterpolator((ys,xs),dx,bounds_error=False,fill_value=0)(query)
    gy=RegularGridInterpolator((ys,xs),dy,bounds_error=False,fill_value=0)(query)
    outer=np.column_stack((-gx,-gy,np.ones(len(p))));outer/=np.linalg.norm(outer,axis=1)[:,None]
    def smooth(t):t=np.clip(t,0,1);return t*t*(3-2*t)
    weight=(1-smooth((abs(p[:,0])-.15)/.03))*smooth((p[:,1]-1.13)/.025)*(1-smooth((p[:,1]-1.395)/.035))*smooth((p[:,2]-.055)/.035)
    n=n*(1-weight[:,None])+outer*weight[:,None];length=np.linalg.norm(n,axis=1);n[length<1e-8]=outer[length<1e-8];n/=np.maximum(np.linalg.norm(n,axis=1),1e-12)[:,None]
    assert np.isfinite(n).all()
    print('Fitted front normals:',int((weight>0).sum()),'vertices',flush=True)
    return n.astype(np.float32).ravel().tolist()


if __name__=='__main__':
    source=Path(sys.argv[1]);target=Path(sys.argv[2]);data=json.loads(source.read_text())
    body=trimesh.Trimesh(vertices=np.array(data['body']).reshape(-1,3),faces=np.array(data['body_faces']).reshape(-1,3),process=False)
    assert body.is_watertight
    if any(flag in sys.argv for flag in ('--polish-only','--align-details-only','--round-front-only','--normals-only')):result=json.loads(target.read_text())
    else:
        result={k:data[k] for k in ('upper_key','detail_key')}
        for label in ('upper','detail'):
            print(label,flush=True)
            result[label]=bake(body,np.array(data[label]).reshape(-1,3),np.array(data[label+'_faces']).reshape(-1,3),smooth=label=='upper')
    if '--polish-only' not in sys.argv and '--align-details-only' not in sys.argv and '--normals-only' not in sys.argv:
        result['upper'],roundness=round_front(result['upper'],np.array(data['upper_faces']).reshape(-1,3))
        (source.parent/'roundness_fit.json').write_text(json.dumps(roundness,indent=2),encoding='utf-8')
    if '--align-details-only' not in sys.argv and '--round-front-only' not in sys.argv and '--normals-only' not in sys.argv:result['upper']=polish_armpits(body,result['upper'],np.array(data['upper_faces']).reshape(-1,3))
    if '--normals-only' not in sys.argv:result['detail']=seat_details(result['upper'],np.array(data['upper_faces']).reshape(-1,3),data['detail'])
    result['upper_normals']=front_normals(result['upper'],np.array(data['upper_faces']).reshape(-1,3))
    target.parent.mkdir(parents=True,exist_ok=True)
    target.write_text(json.dumps(result,separators=(',',':')),encoding='utf-8')
    print(str(target),flush=True)
