import argparse
from collections import Counter,defaultdict
import hashlib
import json
from pathlib import Path
import sqlite3
import sys

import numpy as np
import trimesh

ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT))
from detail_graph import DATA,RAW,FEATURE_NAMES,BodyFrame,save,read_json,write_json

FOLDER=DATA/'DetailTraining'


def candidates():
    db=sqlite3.connect(DATA/'Processed/garments.sqlite3')
    rows=[json.loads(row[0]) for row in db.execute("SELECT detail_json FROM garments WHERE eligible=1 AND json_extract(detail_json,'$.body_variant')='default'")]
    db.close()
    return rows


def ordered(rows):
    strata=defaultdict(list)
    for item in rows:
        key=(str(item['design_meta'].get('upper')),str(item['design_meta'].get('bottom')),item['upstream_bending'])
        strata[key].append(item)
    for group in strata.values(): group.sort(key=lambda r:hashlib.sha256(r['id'].encode()).hexdigest())
    keys=sorted(strata)
    while keys:
        for key in keys: yield strata[key].pop(0)
        keys=[key for key in keys if strata[key]]


def export_preview(metadata):
    path=FOLDER/'Cache'/(metadata['id']+'.npz')
    with np.load(path,allow_pickle=False) as a:
        frame_info=metadata['body_frame']; frame=BodyFrame(frame_info['origin_m'],[0,1,0],[0,0,1],frame_info['height_m'])
        rest=frame.world(a['rest'])[a['render_to_graph']]; reference=frame.world(a['target'])[a['render_to_graph']]
        first=np.unique(a['render_to_graph'],return_index=True)[1]
        seam=a['edges'][a['seam_edge_mask']]
        support=first[np.flatnonzero(a['anchor_mask']>0)]
        if len(support)>256: support=support[np.linspace(0,len(support)-1,256,dtype=int)]
        body=trimesh.load_mesh(RAW/'GarmentCodeData_v2/upstream/neutral_body/mean_all.obj',process=False)
        data=dict(schemaVersion=1,id=metadata['id'],source='GarmentCodeData v2 simulation reference; not AI-generated',
            panelNames=metadata['panel_names'],facePanels=a['face_panel'].tolist(),
            restVertices=rest.astype(np.float32).ravel().tolist(),referenceVertices=reference.astype(np.float32).ravel().tolist(),
            triangles=a['render_faces'].ravel().tolist(),uv=a['render_uv'].ravel().tolist(),
            seamEdges=first[seam].ravel().tolist(),anchorIndices=support.tolist(),
            bodyVertices=body.vertices.astype(np.float32).ravel().tolist(),bodyTriangles=body.faces.ravel().tolist(),
            waistOrigin=metadata['body_frame']['origin_m'],garmentCentre=metadata['garment_area_centre_m'],
            nonpenetrationCertified=False,coordinateSystem='right-handed source; Unity viewer mirrors X and winding exactly once')
        write_json(FOLDER/'detail_preview.json',data)


def prepare(train_count=16,validation_count=4,test_count=4):
    if min(train_count,validation_count,test_count)<1: raise ValueError('Each split needs at least one graph')
    rows=candidates(); FOLDER.mkdir(parents=True,exist_ok=True)
    manifest=dict(schema_version=1,task='pattern_aware_rest_to_static_drape',source_candidates=len(rows),
        splits={part:[r for r in rows if r['official_split']==part] for part in ('train','validation','test')},
        feature_names=FEATURE_NAMES,body_required=True,real_photo_pairs=False,
        contact_loss_enabled=False,full_graph_validation_completed=False,
        note='Full manifest is a source-candidate manifest; graphs are validated on conversion. Do not treat it as a trained model.')
    write_json(FOLDER/'full_manifest.json',manifest)
    pilot=[]; rejected=[]
    for part,wanted in (('train',train_count),('validation',validation_count),('test',test_count)):
        accepted=0
        for item in ordered(manifest['splits'][part]):
            if item['canonical_label_count']>30000: continue
            try:
                metadata=save(item,FOLDER/'Cache')
                pilot.append(dict(id=item['id'],design_group=item['design_group'],split=part,
                    npz=str((FOLDER/'Cache'/(item['id']+'.npz')).relative_to(DATA)),
                    metadata=str((FOLDER/'Cache'/(item['id']+'.json')).relative_to(DATA)),
                    sha256=metadata['npz_sha256']))
                accepted+=1; print(f'PREPARED {part} {accepted}/{wanted} {item["id"]} · {metadata["graph_vertices"]} nodes',flush=True)
            except (ValueError,KeyError) as error: rejected.append(dict(id=item['id'],error=str(error)))
            if accepted==wanted: break
        if accepted<wanted: raise RuntimeError('Not enough valid pilot samples')
    split_groups={part:{r['design_group'] for r in pilot if r['split']==part} for part in ('train','validation','test')}
    assert not split_groups['train']&split_groups['validation'] and not split_groups['train']&split_groups['test'] and not split_groups['test']&split_groups['validation']
    write_json(FOLDER/'pilot_manifest.json',dict(schema_version=1,features=FEATURE_NAMES,items=pilot,rejected=rejected,
        source_manifest_sha256=hashlib.sha256((FOLDER/'full_manifest.json').read_bytes()).hexdigest(),
        purpose='CPU pipeline trial; not production validation',contact_loss_enabled=False))
    # Prefer a complete upper + skirt outfit for the Unity detail inspector.
    example=next((r for r in pilot if r['split']=='train' and
        next(x for x in rows if x['id']==r['id'])['design_meta'].get('upper') and
        'Skirt' in str(next(x for x in rows if x['id']==r['id'])['design_meta'].get('bottom'))),pilot[0])
    export_preview(read_json(DATA/example['metadata']))
    summary=dict(candidate_count=len(rows),candidate_split_counts={part:len(group) for part,group in manifest['splits'].items()},
        pilot_split_counts=dict(Counter(r['split'] for r in pilot)),pilot_samples=len(pilot),rejected=rejected,
        preview_id=example['id'],data_bytes=sum(p.stat().st_size for p in DATA.rglob('*') if p.is_file()),
        source_photos_used_for_supervision=False,source_patterns_preserved=True,uv_preserved=True)
    write_json(ROOT/'preparation_summary.json',summary); print(json.dumps(summary),flush=True)


if __name__=='__main__':
    p=argparse.ArgumentParser(); p.add_argument('--train',type=int,default=16); p.add_argument('--validation',type=int,default=4); p.add_argument('--test',type=int,default=4)
    a=p.parse_args(); prepare(a.train,a.validation,a.test)
