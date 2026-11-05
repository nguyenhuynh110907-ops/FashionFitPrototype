"""Collect cited texture/experimental references, keeping them outside Unity Assets.

No visual texture is labelled as measured strength. No downloaded code is run.
"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import time
import urllib.request
import zipfile
from datetime import datetime, timezone
from PIL import Image

parser=argparse.ArgumentParser()
parser.add_argument('--project',type=Path,required=True)
args=parser.parse_args()
project=args.project.resolve();data=project/'AI/ClothData/Data';root=data/'MaterialLab'
root.mkdir(parents=True,exist_ok=True)
MAX=30_000_000_000
def size():
    return sum((Path(base)/name).stat().st_size for base,dirs,files in os.walk(data) for name in files)
initial=sum((Path(base)/name).stat().st_size for base,dirs,files in os.walk(data) if not Path(base).is_relative_to(root) for name in files);records=[]
def save(path,content):
    path.parent.mkdir(parents=True,exist_ok=True)
    if size()+len(content)-(path.stat().st_size if path.exists() else 0)>MAX:raise RuntimeError('30 GB data budget would be exceeded')
    path.write_bytes(content)
def dump(path,value):save(path,json.dumps(value,ensure_ascii=False,indent=2).encode('utf-8'))
def request(url):return urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'FashionFit-MaterialResearch/1.0'}),timeout=90)
def metadata(url,path):
    with request(url) as stream:blob=stream.read()
    save(path,blob);return json.loads(blob)
def download(url,path,expected_bytes,expected_md5=None):
    def valid():return path.exists() and path.stat().st_size==expected_bytes and (expected_md5 is None or hashlib.md5(path.read_bytes()).hexdigest()==expected_md5)
    path.parent.mkdir(parents=True,exist_ok=True)
    if not valid():
        partial=path.with_suffix(path.suffix+'.part')
        if size()+expected_bytes-(partial.stat().st_size if partial.exists() else 0)>MAX:raise RuntimeError('30 GB download budget exceeded')
        last=0;received=0
        with request(url) as source,partial.open('wb') as output:
            while True:
                block=source.read(1024*1024)
                if not block:break
                received+=len(block)
                if received>expected_bytes:raise RuntimeError('Download exceeds declared source size')
                output.write(block)
                if received-last>=64*1024*1024:print(json.dumps(dict(file=path.name,received=received,expected=expected_bytes)),flush=True);last=received
        if received!=expected_bytes:raise RuntimeError('Incomplete download: '+path.name)
        if expected_md5 and hashlib.md5(partial.read_bytes()).hexdigest()!=expected_md5:raise RuntimeError('Source checksum mismatch: '+path.name)
        partial.replace(path)
    digest=hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda:stream.read(1024*1024),b''):digest.update(block)
    record=dict(path=path.relative_to(root).as_posix(),url=url,bytes=path.stat().st_size,sha256=digest.hexdigest(),source_md5=expected_md5)
    records.append(record);print(json.dumps(dict(downloaded=path.name,bytes=record['bytes'])),flush=True)
    return record

# Paired images/metadata/physics from the original Eurographics repository.
eg_url='https://diglib.eg.org/server/api/core/items/41871095-8ae2-453f-a210-49e4e5febc5f'
eg=metadata(eg_url,root/'Sources/eg2024-item.json')
bundles=metadata(eg_url+'/bundles?size=20',root/'Sources/eg2024-bundles.json')
original=next(x for x in bundles['_embedded']['bundles'] if x['name']=='ORIGINAL')
bits=metadata(original['_links']['bitstreams']['href']+'?size=20',root/'Sources/eg2024-files.json')
archive=next(x for x in bits['_embedded']['bitstreams'] if x['name']=='dataset_for_eg2024.zip')
eg_path=root/'Raw/EG2024/dataset_for_eg2024.zip'
download(archive['_links']['content']['href'],eg_path,archive['sizeBytes'],archive['checkSum']['value'])
with zipfile.ZipFile(eg_path) as z:
    members=[]
    for info in z.infolist():
        if info.is_dir():continue
        record=dict(name=info.filename,bytes=info.file_size,compressed_bytes=info.compress_size,crc32=f'{info.CRC:08x}')
        members.append(record)
        if Path(info.filename).suffix.lower() in {'.csv','.txt','.md'} and info.file_size<5_000_000:
            target=(root/'Metadata/EG2024'/Path(info.filename).name).resolve()
            if not target.is_relative_to((root/'Metadata/EG2024').resolve()):raise ValueError('Unsafe ZIP member')
            save(target,z.read(info))
    dump(root/'Sources/eg2024-zip-index.json',members)
print('EG2024 metadata extracted; images stay in ZIP for streaming.',flush=True)

# One knit system, two boundary conditions: do not count time samples as fabrics.
knit=metadata('https://zenodo.org/api/records/17075127',root/'Sources/knitted2018-record.json')
assert knit['metadata']['license']['id']=='cc-by-4.0'
for entry in knit['files']:
    if entry['key']=='sliding_pictures.zip' or entry['key'] in {'clamped_stitch_posx.mat','clamped_stitch_posy.mat'}:continue
    algorithm,checksum=entry['checksum'].split(':');assert algorithm=='md5'
    download(entry['links']['self'],root/'Raw/Knitted2018'/entry['key'],entry['size'],checksum)

# Independent appearance sources: CC0 PBR only, with no invented mechanical match.
ambient=metadata('https://ambientcg.com/api/v2/full_json?category=Fabric&limit=100&include=downloadData',root/'Sources/ambientcg-metadata.json')
with request('https://docs.ambientcg.com/license/') as stream:save(root/'Sources/ambientcg-license.html',stream.read())
selected=['Fabric086','Fabric085','Fabric084','Fabric083','Fabric081C','Fabric082A'];textures=[]
for asset_id in selected:
    asset=next(x for x in ambient['foundAssets'] if x['assetId']==asset_id)
    files=asset['downloadFolders']['default']['downloadFiletypeCategories']['zip']['downloads'];chosen=next(x for x in files if x['attribute']=='1K-PNG')
    path=root/'Raw/ambientCG'/chosen['fileName'];download(chosen['downloadLink'],path,chosen['size'])
    maps=[]
    with zipfile.ZipFile(path) as z:
        for info in z.infolist():
            if info.is_dir() or Path(info.filename).suffix.lower()!='.png':continue
            if info.file_size>100_000_000:raise ValueError('Unexpected large texture')
            target=(root/'Textures'/asset_id/Path(info.filename).name).resolve()
            save(target,z.read(info))
            with Image.open(target) as image:
                assert max(image.size)<=2048
                dimensions=list(image.size);mode=image.mode;image.verify()
            maps.append(dict(path=target.relative_to(root).as_posix(),sha256=hashlib.sha256(target.read_bytes()).hexdigest(),resolution=dimensions,mode=mode))
    textures.append(dict(id=asset_id,source='https://ambientcg.com/view?id='+asset_id,license='CC0-1.0',creation_method=asset.get('creationMethod'),physical_size_source=asset.get('dimensionX'),maps=maps,measured_strength=None,paired_mechanics_sample_id=None))
    dump(root/'texture_catalog.json',textures)
final=size()
report=dict(status='downloaded_and_checksums_verified',retrieved_utc=datetime.now(timezone.utc).isoformat(),
    project=str(project),initial_data_bytes=initial,final_data_bytes=final,budget_bytes=MAX,
    additions_bytes=final-initial,files=records,texture_assets=len(textures),
    licenses=dict(ambientCG='CC0-1.0',knitted2018='CC-BY-4.0',EG2024='Academic use per publication; inspect included terms; not approved for commercial runtime redistribution'),
    not_downloaded=['MIT video archive already documented; not duplicated','Zenodo sliding photographs to avoid duplication/budget pressure'],
    mechanics_runtime_changed=False,trained_model=False)
dump(root/'collection_manifest.json',report)
print(json.dumps({k:v for k,v in report.items() if k!='files'},ensure_ascii=False),flush=True)
