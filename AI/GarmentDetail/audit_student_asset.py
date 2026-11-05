"""Read-only source audit for student OBJ/MTL and glTF/GLB submissions.

Checks detail dependencies before any fitting. Never weld, simplify, unwrap, or
rewrite source meshes. It is not a cloth solver or a complete glTF validator.
"""
import argparse
import hashlib
import io
import json
from pathlib import Path
import shlex
import struct
import sys
from urllib.parse import unquote

from PIL import Image

ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT))
from detail_graph import DATA, write_json


def digest(path):
    h=hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda:stream.read(1024*1024),b''):h.update(block)
    return h.hexdigest()


def audit(path):
    path=Path(path).resolve(strict=True);base=path.parent
    report=dict(file=str(path),sha256=digest(path),format=path.suffix.lower(),
        source_modified=False,dependencies=[],issues=[],notes=[],
        preserves_source_bytes=True,retargeting_performed=False,simulation_ready=False)
    def dependency(name,kind):
        candidate=(base/unquote(name.replace('\\','/'))).resolve()
        if not candidate.is_relative_to(base):
            report['issues'].append('Reference outside submission folder: '+name)
            return None
        if not candidate.is_file():
            report['issues'].append('Missing '+kind+': '+name);return None
        record=dict(file=str(candidate.relative_to(base)),kind=kind,bytes=candidate.stat().st_size,sha256=digest(candidate))
        report['dependencies'].append(record)
        return candidate,record
    def image_record(blob,record):
        with Image.open(io.BytesIO(blob)) as image:
            image.verify();record['resolution']=list(image.size);record['mode']=image.mode
    if path.suffix.lower()=='.obj':
        positions=0;uv=0;normals=0;faces=0;corners=0;missing_uv=0;missing_normals=0
        materials=set();libraries=[];current=None;position_uv={};uv_alias=0;bounds=[]
        for line in path.open(encoding='utf-8-sig',errors='strict'):
            fields=line.split('#',1)[0].split()
            if not fields:continue
            if fields[0]=='v':positions+=1;bounds.append([float(v) for v in fields[1:4]])
            elif fields[0]=='vt':uv+=1
            elif fields[0]=='vn':normals+=1
            elif fields[0]=='usemtl':current=' '.join(fields[1:]);materials.add(current)
            elif fields[0]=='mtllib':
                # Quoted filenames supported; ambiguous unquoted spaces are reported.
                libraries.extend(shlex.split(line.strip()[7:],posix=True))
            elif fields[0]=='f':
                faces+=1
                for corner in fields[1:]:
                    corners+=1;values=corner.split('/')
                    pi=int(values[0]);pi=pi-1 if pi>0 else positions+pi
                    if not 0<=pi<positions:raise ValueError('Invalid OBJ position index')
                    ti=None
                    if len(values)>1 and values[1]:
                        ti=int(values[1]);ti=ti-1 if ti>0 else uv+ti
                        if not 0<=ti<uv:raise ValueError('Invalid OBJ UV index')
                    else:missing_uv+=1
                    if len(values)<=2 or not values[2]:missing_normals+=1
                    else:
                        ni=int(values[2]);ni=ni-1 if ni>0 else normals+ni
                        if not 0<=ni<normals:raise ValueError('Invalid OBJ normal index')
                    position_uv.setdefault(pi,set()).add(ti)
        defined=set()
        for library in libraries:
            found=dependency(library,'material_library')
            if not found:continue
            mtl,record=found;active=None
            for line in mtl.read_text(encoding='utf-8-sig').splitlines():
                f=line.split('#',1)[0].strip().split(maxsplit=1)
                if len(f)!=2:continue
                if f[0]=='newmtl':active=f[1];defined.add(active)
                elif f[0].lower().startswith(('map_','bump','norm','disp')):
                    raw=f[1].strip().strip('"')
                    if raw.startswith('-'):
                        report['issues'].append('MTL map options require manual path review: '+raw);continue
                    # MTL image paths are relative to the MTL file, not necessarily OBJ.
                    target=dependency(str((mtl.parent/raw).relative_to(base)),'texture')
                    if target:
                        texture,rec=target;rec['material']=active;rec['slot']=f[0]
                        try:image_record(texture.read_bytes(),rec)
                        except Exception as error:report['issues'].append('Invalid texture '+raw+': '+str(error))
        if not faces:report['issues'].append('No faces')
        if missing_uv:report['issues'].append(f'{missing_uv}/{corners} corners lack source UV')
        if missing_normals:report['notes'].append(f'{missing_normals}/{corners} corners lack source normals; an importer must calculate them')
        if materials-defined:report['issues'].append('Undefined materials: '+', '.join(sorted(materials-defined)))
        report['mesh']=dict(positions=positions,texture_coordinates=uv,normals=normals,polygons=faces,
            material_names=sorted(materials),positions_with_multiple_uvs=sum(len(v)>1 for v in position_uv.values()),
            missing_uv_corners=missing_uv,missing_normal_corners=missing_normals)
        if bounds:
            report['source_bounds']={key:[fn(v[i] for v in bounds) for i in range(3)] for key,fn in [('min',min),('max',max)]}
        report['notes'].append('OBJ stores no standard unit or rig; require unit/axis/body-reference metadata separately.')
    elif path.suffix.lower() in ('.glb','.gltf'):
        binary=None
        if path.suffix.lower()=='.glb':
            blob=path.read_bytes()
            magic,version,length=struct.unpack_from('<III',blob)
            if magic!=0x46546c67 or version!=2 or length!=len(blob):raise ValueError('Invalid GLB header')
            offset=12;document=None
            while offset<len(blob):
                size,kind=struct.unpack_from('<II',blob,offset);offset+=8
                if offset+size>len(blob):raise ValueError('Truncated GLB chunk')
                content=blob[offset:offset+size];offset+=size
                if kind==0x4e4f534a:document=json.loads(content)
                elif kind==0x004e4942:binary=content
            if document is None:raise ValueError('Missing GLB JSON')
        else:document=json.loads(path.read_text(encoding='utf-8-sig'))
        if document.get('asset',{}).get('version')!='2.0':raise ValueError('Only glTF 2.0 supported')
        buffers=[]
        for buffer in document.get('buffers',[]):
            if 'uri' not in buffer:buffers.append(binary)
            elif buffer['uri'].startswith('data:'):
                import base64
                buffers.append(base64.b64decode(buffer['uri'].split(',',1)[1],validate=True))
            else:
                found=dependency(buffer['uri'],'buffer');buffers.append(found[0].read_bytes() if found else None)
        for i,image in enumerate(document.get('images',[])):
            record=dict(kind='embedded_image',image_index=i)
            try:
                if 'uri' in image:
                    if image['uri'].startswith('data:'):
                        import base64
                        raw=base64.b64decode(image['uri'].split(',',1)[1],validate=True)
                        record['sha256']=hashlib.sha256(raw).hexdigest();report['dependencies'].append(record)
                    else:
                        found=dependency(image['uri'],'texture')
                        if not found:continue
                        raw=found[0].read_bytes();record=found[1]
                else:
                    view=document['bufferViews'][image['bufferView']];b=buffers[view['buffer']]
                    if b is None:raise ValueError('Missing image buffer')
                    start=view.get('byteOffset',0);raw=b[start:start+view['byteLength']]
                    if len(raw)!=view['byteLength']:raise ValueError('Truncated embedded image')
                    record['sha256']=hashlib.sha256(raw).hexdigest();report['dependencies'].append(record)
                image_record(raw,record)
            except Exception as error:report['issues'].append('Image '+str(i)+': '+str(error))
        primitives=[]
        for mesh in document.get('meshes',[]):
            for primitive in mesh.get('primitives',[]):
                attr=primitive.get('attributes',{});material=primitive.get('material')
                primitives.append(dict(mesh=mesh.get('name'),attributes=sorted(attr),material=material,
                    position_count=document['accessors'][attr['POSITION']]['count'] if 'POSITION' in attr else 0,
                    uses_draco='KHR_draco_mesh_compression' in primitive.get('extensions',{})))
                if 'TEXCOORD_0' not in attr:report['issues'].append('Primitive lacks TEXCOORD_0: '+str(mesh.get('name')))
                if 'NORMAL' not in attr:report['notes'].append('Primitive has no source NORMAL')
        report['primitives']=primitives;report['materials_source']=document.get('materials',[])
        report['required_extensions']=document.get('extensionsRequired',[])
        report['notes'].append('Accessor values, texture coordinate transforms, material extensions and render equivalence require a full glTF/Unity validation pass.')
    else:
        raise ValueError('Offline audit accepts OBJ+MTL or glTF/GLB. Export FBX via Unity Editor; screenshots are not a 3D asset.')
    assert digest(path)==report['sha256']
    report['status']='dependencies_found_requires_render_review' if not report['issues'] else 'needs_source_fix'
    report['notes'].append('Training consent/asset rights must be recorded separately from permission to display a student submission.')
    return report


if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('file',type=Path);a=p.parse_args()
    result=audit(a.file)
    out=DATA/'DetailTraining/StudentIntake'/('audit_'+result['sha256'][:16]+'.json')
    write_json(out,result);print(json.dumps(dict(report=str(out),status=result['status'],issues=result['issues']),ensure_ascii=False))
