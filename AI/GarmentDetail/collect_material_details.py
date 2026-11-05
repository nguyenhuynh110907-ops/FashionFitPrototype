"""Collect the published 1K fabric PBR bundles, retaining original bytes and rights.

No web scraping of fashion photographs, archive extraction, or image->mesh pairing.
"""
import concurrent.futures
import hashlib
import io
import json
from pathlib import Path
import re
import threading
import time
import urllib.request
import zipfile

from PIL import Image

ROOT = Path(__file__).resolve().parent
DATA = ROOT.parent / 'ClothData/Data'
FOLDER = DATA / 'Raw/DetailSources'
LIMIT = 30_000_000_000
LOCK = threading.Lock()


def write(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    temp = path.with_suffix(path.suffix + '.tmp')
    temp.write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding='utf-8')
    temp.replace(path)


def sha(path):
    h = hashlib.sha256()
    with path.open('rb') as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b''):
            h.update(block)
    return h.hexdigest()


def inspect(path):
    maps = []
    with zipfile.ZipFile(path) as z:
        if sum(x.file_size for x in z.infolist()) > 300_000_000:
            raise ValueError('Unexpected uncompressed bundle size')
        if z.testzip() is not None:
            raise ValueError('ZIP CRC failure')
        for member in z.infolist():
            if not member.filename.lower().endswith(('.jpg', '.png')):
                continue
            with Image.open(io.BytesIO(z.read(member))) as im:
                im.load()
                maps.append(dict(member=member.filename, width=im.width, height=im.height,
                                 channels=im.mode, crc32=f'{member.CRC:08x}'))
    for token in ('_Color.', '_NormalGL.', '_Roughness.'):
        if not any(token.lower() in m['member'].lower() for m in maps):
            raise ValueError('Missing required PBR map: ' + token)
    return maps


def collect():
    FOLDER.mkdir(parents=True, exist_ok=True)
    catalog = FOLDER / 'ambientcg_catalog.json'
    if not catalog.exists():
        url = 'https://ambientcg.com/api/v2/full_json?type=Material&category=Fabric&limit=100&include=downloadData'
        with urllib.request.urlopen(url, timeout=60) as response:
            value = response.read(8_000_000)
        json.loads(value)
        catalog.write_bytes(value)
    assets = json.loads(catalog.read_text(encoding='utf-8'))['foundAssets']
    selected = []
    for asset in assets:
        downloads = asset['downloadFolders']['default']['downloadFiletypeCategories']['zip']['downloads']
        match = [x for x in downloads if x.get('attribute') == '1K-JPG']
        if len(match) != 1:
            raise ValueError('Ambiguous 1K bundle: ' + asset['assetId'])
        selected.append((asset, match[0]))
    existing_bytes = sum(p.stat().st_size for p in DATA.rglob('*') if p.is_file())
    needed = sum(d['size'] for a, d in selected if not (FOLDER / 'ambientCG' / d['fileName']).exists())
    if existing_bytes + needed + 100_000_000 > LIMIT:
        raise ValueError('Not enough of the existing 30 GB allowance for this batch')
    license_url = 'https://docs.ambientcg.com/license/'
    with urllib.request.urlopen(license_url, timeout=60) as response:
        license_bytes = response.read(2_000_000)
    if b'CC0' not in license_bytes:
        raise ValueError('Upstream license page changed; inspect before collecting')
    (FOLDER / 'ambientcg_license.html').write_bytes(license_bytes)
    # Group letter variants together; also join any explicit upstream variation links.
    parent = {a['assetId']: a['assetId'] for a in assets}
    def find(x):
        while parent[x] != x:
            x = parent[x]
        return x
    def join(a, b):
        if b in parent:
            parent[find(b)] = find(a)
    families = {}
    for a in assets:
        key = re.sub(r'[A-Za-z]+$', '', a['assetId'])
        if key in families:
            join(a['assetId'], families[key])
        families[key] = a['assetId']
        for field in ('nextVariationAssetId', 'previousVariationAssetId'):
            if isinstance(a.get(field), str):
                join(a['assetId'], a[field])
    group = {a['assetId']: find(a['assetId']) for a in assets}
    records = []
    errors = []
    def one(pair):
        asset, download = pair
        name = download['fileName']
        if Path(name).name != name or not name.endswith('_1K-JPG.zip'):
            raise ValueError('Unexpected download filename')
        path = FOLDER / 'ambientCG' / name
        path.parent.mkdir(parents=True, exist_ok=True)
        receipt = path.with_suffix('.receipt.json')
        if path.exists() and receipt.exists():
            old = json.loads(receipt.read_text())
            if path.stat().st_size == download['size'] and sha(path) == old['sha256']:
                return old
        request = urllib.request.Request(download['fullDownloadPath'], headers={'User-Agent': 'FashionFit-Research-Collector/1.0'})
        temp = path.with_suffix('.zip.part')
        for attempt in range(4):
            try:
                with urllib.request.urlopen(request, timeout=90) as response, temp.open('wb') as stream:
                    count = 0
                    while True:
                        block = response.read(256 * 1024)
                        if not block:
                            break
                        count += len(block)
                        if count > download['size']:
                            raise ValueError('Upstream file exceeds catalog size')
                        stream.write(block)
                if count != download['size']:
                    raise ValueError('Incomplete download')
                maps = inspect(temp)
                temp.replace(path)
                family = group[asset['assetId']]
                bucket = int(hashlib.sha256(family.encode()).hexdigest()[:8], 16) % 10
                result = dict(id=asset['assetId'], source_page=asset['shortLink'],
                    download_url=download['fullDownloadPath'], archive=str(path.relative_to(DATA)),
                    bytes=count, sha256=sha(path), upstream_checksum_available=False,
                    integrity='local SHA256 + every ZIP member CRC32 + image decode; not an upstream cryptographic signature',
                    license='CC0-1.0', license_url=license_url, creator='ambientCG / Lennart Demes',
                    license_snapshot_sha256=hashlib.sha256(license_bytes).hexdigest(),
                    family=family, split='test' if bucket == 0 else 'validation' if bucket == 1 else 'train',
                    maps=maps, source_tags=asset.get('tags', []), creation_method=asset.get('creationMethod'),
                    physical_dimensions_source={k: asset.get(k) for k in ('dimensionX','dimensionY','dimensionZ')},
                    physical_scale_valid=bool(asset.get('dimensionX', 0) and asset.get('dimensionY', 0)),
                    garment_mesh_pair=None, embroidery_class=None, pattern_class=None,
                    retrieved_utc=time.strftime('%Y-%m-%dT%H:%M:%SZ', time.gmtime()))
                write(receipt, result)
                print('MATERIAL ' + asset['assetId'] + ' ' + str(count), flush=True)
                return result
            except Exception:
                if attempt == 3:
                    raise
                time.sleep(2 ** attempt)
    with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
        future_map = {pool.submit(one, pair): pair[0]['assetId'] for pair in selected}
        for future in concurrent.futures.as_completed(future_map):
            try:
                records.append(future.result())
            except Exception as error:
                errors.append(dict(id=future_map[future], error=str(error)))
            write(FOLDER / 'materials_manifest.json', dict(schema_version=1,
                items=sorted(records, key=lambda x: x['id']), errors=errors,
                catalog_sha256=sha(catalog), source_license=license_url,
                synthetic_garment_pairing=False, source_resolution='1K-JPG'))
    total = sum(p.stat().st_size for p in DATA.rglob('*') if p.is_file())
    summary = dict(assets=len(records), image_maps=sum(len(x['maps']) for x in records),
                   archive_bytes=sum(x['bytes'] for x in records), total_cloth_data_bytes=total,
                   within_30gb_limit=total <= LIMIT, errors=errors)
    write(ROOT / 'material_collection_report.json', summary)
    print(json.dumps(summary), flush=True)
    if errors or total > LIMIT:
        raise RuntimeError('Collection incomplete; rerun to resume verified files')


if __name__ == '__main__':
    collect()
