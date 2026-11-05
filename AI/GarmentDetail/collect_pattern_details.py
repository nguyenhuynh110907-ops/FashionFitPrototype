"""Pin AI4C textile-pattern archives and retain per-image rights, not just MIT metadata."""
from collections import Counter
import hashlib
import io
import json
from pathlib import Path
import tarfile
import urllib.request
import sys

from PIL import Image

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT))
from detail_graph import DATA, write_json

FOLDER = DATA / 'Raw/DetailSources'
FILES = [('AI4C_labels.json', 'AI4C_Textile_Pattern_Dataset_2024-06-01_0.json', 701161, '8b76b9c09b62a790187592da288c60af'),
         ('pt1.tar.gz', 'pt1.tar.gz', 25363721, 'ce143aafb34fd8018590b283b50d93fe'),
         ('pt2.tar.gz', 'pt2.tar.gz', 5206012, '01fb67d56bd48e37470ea556dba452e7')]


def collect():
    FOLDER.mkdir(parents=True, exist_ok=True)
    if sum(p.stat().st_size for p in DATA.rglob('*') if p.is_file()) + sum(x[2] for x in FILES) + 10_000_000 > 30_000_000_000:
        raise ValueError('30 GB collection limit')
    for local, remote, size, md5 in FILES:
        path = FOLDER / local
        url = f'https://zenodo.org/api/records/11409357/files/{remote}/content'
        if not path.exists() or hashlib.md5(path.read_bytes()).hexdigest() != md5:
            part = path.with_suffix(path.suffix+'.part')
            with urllib.request.urlopen(url, timeout=120) as response, part.open('wb') as stream:
                count=0
                while True:
                    block=response.read(256*1024)
                    if not block: break
                    count+=len(block)
                    if count>size: raise ValueError('Unexpected size')
                    stream.write(block)
            if part.stat().st_size != size or hashlib.md5(part.read_bytes()).hexdigest() != md5:
                raise ValueError('Zenodo checksum mismatch')
            part.replace(path)
        write_json(path.with_suffix(path.suffix+'.receipt.json'),dict(url=url,bytes=size,
            upstream_md5=md5,sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
            source='https://doi.org/10.5281/zenodo.11409357'))
    labels=json.loads((FOLDER/'AI4C_labels.json').read_text(encoding='utf-8'))
    licenses={x['id']:x['name'] for x in labels['licenses']}
    categories={x['id']:x['name'] for x in labels['categories']}
    annotations={x['image_id']:x['category_id'] for x in labels['annotations']}
    locations={}
    for local,remote,size,md5 in FILES[1:]:
        archive=FOLDER/local
        with tarfile.open(archive,'r:gz') as tar:
            for member in tar:
                if not member.isfile() or not member.name.lower().endswith(('.png','.jpg','.jpeg')): continue
                if member.size>10_000_000: raise ValueError('Unexpected image size')
                name=Path(member.name).name
                if name in locations: raise ValueError('Duplicate archive basename')
                blob=tar.extractfile(member).read()  # Never extract an archive pathname to disk.
                with Image.open(io.BytesIO(blob)) as im:
                    im.load(); width,height=im.size
                locations[name]=dict(archive=str(archive.relative_to(DATA)),member=member.name,
                    sha256=hashlib.sha256(blob).hexdigest(),bytes=len(blob),width=width,height=height)
    items=[]
    for image in labels['images']:
        entry=locations.get(image['file_name'])
        if not entry: raise ValueError('Missing annotated image '+image['file_name'])
        lic=licenses.get(image.get('license'),'unknown')
        eligible=any(token in lic for token in ('/by/4.0/','/zero/1.0/','/mark/1.0/')) and bool(image.get('source') and image.get('landing_page'))
        items.append(dict(id=image['id'],file_name=image['file_name'],**entry,
            category=categories[annotations[image['id']]],source_credit=image.get('source'),
            source_page=image.get('landing_page'),image_license_url=lic,
            training_eligible=eligible,rights_basis='per-image license/source declared by the pinned Zenodo release; individual museum pages not re-reviewed',
            hold_reason=None if eligible else 'missing source/rights or noncommercial/no-derivatives image',
            garment_3d_pair=None,geometry_training_enabled=False))
    manifest=dict(schema_version=1,source_doi='https://doi.org/10.5281/zenodo.11409357',
        publisher='Henk Vanstappen / Datable, AI4Culture',dataset_version='2024-06-01',
        metadata_license='MIT (record); NOT a replacement for image licenses',
        categories=list(categories.values()),items=items,
        annotation_scope='one whole-image pattern category; no embroidery mask, no 3D, no millimetre labels')
    write_json(FOLDER/'pattern_manifest.json',manifest)
    report=dict(images=len(items),eligible_images=sum(x['training_eligible'] for x in items),
        held_images=sum(not x['training_eligible'] for x in items),
        eligible_categories=dict(Counter(x['category'] for x in items if x['training_eligible'])),
        rights_counts=dict(Counter(x['image_license_url'] for x in items)),
        compressed_image_archive_bytes=sum(x[2] for x in FILES[1:]),
        source_record='https://zenodo.org/records/11409357',geometry_pairs=0)
    write_json(ROOT/'pattern_collection_report.json',report);print(json.dumps(report),flush=True)


if __name__=='__main__': collect()
