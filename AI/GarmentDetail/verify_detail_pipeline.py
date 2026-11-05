"""Verify provenance, masks, split isolation, source UVs, checkpoints and intake audit."""
from collections import Counter
import hashlib
import io
import json
from pathlib import Path
import sys
import struct
import tempfile
import tarfile

import numpy as np
from PIL import Image, ImageOps, ImageDraw

ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT))
from detail_graph import DATA, build, write_json
from prepare_training import candidates
from prepare_detail_regions import geometry_features, TARGETS
from audit_student_asset import audit


def read(path):return json.loads(path.read_text(encoding='utf-8'))
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()


def verify():
    source=DATA/'Raw/DetailSources';folder=DATA/'DetailTraining/DetailRegions'
    material=read(source/'materials_manifest.json');assert not material['errors']
    material_families={part:set() for part in ('train','validation','test')}
    for r in material['items']:
        assert sha(DATA/r['archive'])==r['sha256']
        assert r['license']=='CC0-1.0'
        material_families[r['split']].add(r['family'])
    regions=read(folder/'manifest.json');source_rows={r['id']:r for r in candidates()}
    groups={part:set() for part in ('train','validation','test')};positive=Counter()
    for item in regions['items']:
        groups[item['split']].add(item['design_group'])
        assert item['source_hashes']==source_rows[item['id']]['component_hashes']
        path=DATA/item['npz'];assert sha(path)==item['sha256']
        with np.load(path,allow_pickle=False) as a:
            n=len(a['features']);assert a['features'].shape==(n,13)
            assert a['targets'].shape==a['valid'].shape==(n,len(TARGETS))
            assert np.isfinite(a['features']).all() and np.isfinite(a['render_uv']).all()
            assert set(np.unique(a['targets']))<={0,1}
            assert np.all(a['targets'][~a['valid']]==0)
            assert a['render_to_graph'].max()<n and a['render_to_graph'].min()>=0
            positive.update({k:int(a['targets'][:,i].sum()) for i,k in enumerate(TARGETS)})
    checks=[]
    for part in groups:
        example=next(r for r in regions['items'] if r['split']==part)
        raw,meta,spec,labels=build(source_rows[example['id']])
        with np.load(DATA/example['npz'],allow_pickle=False) as a:
            for key in ('render_uv','render_faces','render_to_graph','faces','edges'):
                assert np.array_equal(raw[key],a[key])
            assert np.array_equal(geometry_features(raw['target'],raw['faces'],raw['edges']),a['features'])
        checks.append(dict(id=example['id'],split=part,exact_uv_faces_and_input_features_match_source=True))
    patterns=read(source/'pattern_manifest.json');prepared=read(DATA/'DetailTraining/PatternDetails/manifest.json')
    source_lookup={r['id']:r for r in patterns['items']}
    hashes={p:set() for p in groups};pattern_groups={p:set() for p in groups}
    archives={}
    for r in prepared['items']:
        original=source_lookup[r['id']];assert original['training_eligible']
        assert original['sha256']==r['sha256'];assert original['image_license_url']!='unknown'
        hashes[r['split']].add(r['sha256']);pattern_groups[r['split']].add(r['group_id'])
    for pairs in (groups,hashes,pattern_groups,material_families):
        assert not (pairs['train']&pairs['validation'] or pairs['train']&pairs['test'] or pairs['validation']&pairs['test'])
    # Check source archive bytes against Zenodo's published checksum receipts.
    for name in ('pt1.tar.gz','pt2.tar.gz'):
        p=source/name;receipt=read(p.with_suffix(p.suffix+'.receipt.json'))
        assert sha(p)==receipt['sha256'] and hashlib.md5(p.read_bytes()).hexdigest()==receipt['upstream_md5']
    # Meaningful intake regression: two material slots, a UV seam, an intentionally missing texture.
    with tempfile.TemporaryDirectory(dir=DATA/'DetailTraining') as tmp:
        tmp=Path(tmp)
        Image.new('RGB',(32,32),(60,120,180)).save(tmp/'cloth.png')
        (tmp/'dress.mtl').write_text('newmtl cloth\nmap_Kd cloth.png\nnewmtl collar\nmap_Kd missing.png\n')
        obj=tmp/'dress.obj'
        obj.write_text('mtllib dress.mtl\nv 0 0 0\nv 1 0 0\nv 0 1 0\nv 1 1 0\nvt 0 0\nvt 1 0\nvt 0 1\nvt 1 1\nvt .5 .5\nvn 0 0 1\nusemtl cloth\nf 1/1/1 2/2/1 3/3/1\nusemtl collar\nf 2/5/1 4/4/1 3/3/1\n')
        before=sha(obj);result=audit(obj)
        assert result['status']=='needs_source_fix' and any('missing.png' in s for s in result['issues'])
        assert result['mesh']['positions_with_multiple_uvs']==1 and result['mesh']['material_names']==['cloth','collar']
        assert sha(obj)==before
        # GLB fixture with real geometry, UV and an embedded PNG dependency.
        png=(tmp/'cloth.png').read_bytes()
        binary=struct.pack('<9f',0,0,0,1,0,0,0,1,0)+struct.pack('<6f',0,0,1,0,0,1)
        doc=dict(asset={'version':'2.0'},buffers=[{'byteLength':len(binary)+len(png)}],
            bufferViews=[{'buffer':0,'byteOffset':0,'byteLength':36},
                         {'buffer':0,'byteOffset':36,'byteLength':24},
                         {'buffer':0,'byteOffset':60,'byteLength':len(png)}],
            accessors=[{'bufferView':0,'componentType':5126,'count':3,'type':'VEC3'},
                       {'bufferView':1,'componentType':5126,'count':3,'type':'VEC2'}],
            images=[{'bufferView':2,'mimeType':'image/png'}],textures=[{'source':0}],
            materials=[{'pbrMetallicRoughness':{'baseColorTexture':{'index':0}}}],
            meshes=[{'name':'test cloth','primitives':[{'attributes':{'POSITION':0,'TEXCOORD_0':1},'material':0}]}])
        js=json.dumps(doc).encode();js+=b' '*((-len(js))%4)
        binary+=png;binary+=b'\0'*((-len(binary))%4)
        glb=tmp/'dress.glb'
        glb.write_bytes(struct.pack('<III',0x46546c67,2,12+8+len(js)+8+len(binary))+
            struct.pack('<II',len(js),0x4e4f534a)+js+struct.pack('<II',len(binary),0x004e4942)+binary)
        result=audit(glb)
        assert result['status']=='dependencies_found_requires_render_review'
        assert result['dependencies'][0]['resolution']==[32,32]
        assert result['dependencies'][0]['sha256']==hashlib.sha256(png).hexdigest()
    reports={}
    for directory,checkpoint in [('DetailRegions','detail_regions.safetensors'),('PatternDetails','pattern_head.safetensors')]:
        p=ROOT/'Artifacts'/directory;report=read(p/'training_report.json')
        assert sha(p/checkpoint)==report['checkpoint_sha256']
        assert report['test_used_for_optimizer_or_selection'] is False
        reports[directory]=dict(checkpoint_sha256=report['checkpoint_sha256'],deployment='offline research only')
    # Contact sheet for human review. The image category is not a localized embroidery label.
    picks=[]
    for category in prepared['categories']:
        picks.extend([r for r in prepared['items'] if r['category']==category][:2])
    wanted={r['member'] for r in picks};image_blobs={}
    for archive in sorted({r['archive'] for r in picks}):
        with tarfile.open(DATA/archive,'r:gz') as tar:
            for member in tar:
                if member.name in wanted:image_blobs[member.name]=tar.extractfile(member).read()
    sheet=Image.new('RGB',(960,3*240),(235,235,235));draw=ImageDraw.Draw(sheet)
    for i,r in enumerate(picks):
        x=(i%4)*240;y=(i//4)*240
        with Image.open(io.BytesIO(image_blobs[r['member']])) as image:
            preview=ImageOps.contain(image.convert('RGB'),(228,205));sheet.paste(preview,(x+(240-preview.width)//2,y))
        draw.text((x+4,y+208),r['category'],fill=(0,0,0));draw.text((x+4,y+224),r['id'][:12],fill=(0,0,0))
    preview=DATA/'Previews/pattern_detail_review.jpg';preview.parent.mkdir(exist_ok=True);sheet.save(preview,quality=92)
    total=sum(p.stat().st_size for p in DATA.rglob('*') if p.is_file());assert total<=30_000_000_000
    result=dict(status='passed',materials=len(material['items']),pbr_maps=sum(len(r['maps']) for r in material['items']),
        source_pattern_images=len(patterns['items']),eligible_pattern_images=sum(r['training_eligible'] for r in patterns['items']),
        actual_pattern_training_pack=len(prepared['items']),pattern_splits=dict(Counter(r['split'] for r in prepared['items'])),
        region_graphs=len(regions['items']),region_splits=dict(Counter(r['split'] for r in regions['items'])),
        source_exact_comparisons=checks,positive_3d_vertices=dict(positive),
        no_split_overlap_for_designs_object_groups_image_hashes_material_families=True,
        unknown_attachment_labels_masked=True,intake_detects_missing_texture_and_preserves_uv_seams=True,
        trained_checkpoints=reports,total_cloth_data_bytes=total,within_30gb_limit=True,
        preview=str(preview.relative_to(DATA)),student_render_equivalence_verified=False,
        microscopic_embroidery_3d_ground_truth_available=False)
    write_json(ROOT/'detail_pipeline_verification.json',result);print(json.dumps(result),flush=True)


if __name__=='__main__':verify()
