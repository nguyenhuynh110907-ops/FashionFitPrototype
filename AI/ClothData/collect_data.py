"""Bounded, resumable collection of public garment and cloth-motion data.

Raw bytes stay outside Unity Assets. Each file is verified against upstream
hash/CRC metadata before it is marked complete. No downloaded code is run.
"""
import argparse
import base64
from concurrent.futures import ThreadPoolExecutor, as_completed
from datetime import datetime, timezone
import hashlib
import json
import os
from pathlib import Path
import shutil
import threading
import time
import urllib.error
import urllib.parse
import urllib.request

import google_crc32c

ROOT = Path(__file__).resolve().parent
DATA = ROOT / 'Data'
RAW = DATA / 'Raw'
lock = threading.Lock()


def now(): return datetime.now(timezone.utc).isoformat()


def read_json(path): return json.loads(Path(path).read_text(encoding='utf-8-sig'))


def write_json(path, value):
    path = Path(path); path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix+'.tmp')
    temporary.write_text(json.dumps(value, indent=2, ensure_ascii=False), encoding='utf-8')
    temporary.replace(path)


def safe_path(relative):
    path = (DATA / relative).resolve()
    if not path.is_relative_to(DATA.resolve()): raise ValueError('Path outside data root')
    return path


def checksums(path):
    sha, crc = hashlib.sha256(), google_crc32c.Checksum()
    with path.open('rb') as stream:
        while block := stream.read(8*1024*1024): sha.update(block); crc.update(block)
    return sha.hexdigest(), base64.b64encode(crc.digest()).decode()


def download(item):
    target = safe_path(item['local']); target.parent.mkdir(parents=True,exist_ok=True)
    receipt = target.with_suffix(target.suffix+'.receipt.json')
    if target.exists() and receipt.exists():
        old = read_json(receipt)
        if target.stat().st_size == item['size'] and old.get('expected_sha256')==item.get('sha256') and old.get('url')==item['url']:
            return dict(old, status='cached')
    part = target.with_suffix(target.suffix+'.part')
    if target.exists() and not part.exists(): target.replace(part)
    if part.exists() and part.stat().st_size > item['size']: raise ValueError('Partial file larger than expected')
    for attempt in range(5):
        try:
            offset = part.stat().st_size if part.exists() else 0
            if offset < item['size']:
                headers={'User-Agent':'FashionFitPrototype-ClothData/0.1','Accept-Encoding':'identity'}
                if offset: headers['Range']=f'bytes={offset}-'
                request=urllib.request.Request(item['url'],headers=headers)
                with urllib.request.urlopen(request,timeout=60) as response:
                    if offset and response.status != 206: offset=0
                    if offset and not response.headers.get('Content-Range','').startswith(f'bytes {offset}-'): raise ValueError('Invalid range response')
                    if response.headers.get('Content-Type','').startswith('text/html'): raise ValueError('Unexpected HTML instead of dataset')
                    with part.open('ab' if offset else 'wb') as stream:
                        while block := response.read(4*1024*1024):
                            stream.write(block); offset+=len(block)
                            if offset>item['size']: raise ValueError('Response exceeded expected size')
                            if shutil.disk_usage(DATA).free < 80_000_000_000: raise RuntimeError('Free disk reserve reached')
            if part.stat().st_size != item['size']: raise ValueError('Truncated download')
            sha,crc=checksums(part)
            if item.get('sha256') and sha != item['sha256']: raise ValueError('SHA256 mismatch')
            if item.get('crc32c') and crc != item['crc32c']: raise ValueError('CRC32C mismatch')
            if item.get('git_blob'):
                git=hashlib.sha1()
                git.update(f'blob {item["size"]}\0'.encode())
                with part.open('rb') as stream:
                    while block:=stream.read(4*1024*1024): git.update(block)
                if git.hexdigest()!=item['git_blob']: raise ValueError('Git blob checksum mismatch')
            part.replace(target)
            result=dict(item,status='verified',downloaded_utc=now(),sha256=sha,crc32c_computed=crc,expected_sha256=item.get('sha256'))
            write_json(receipt,result)
            print(f'VERIFIED {item["local"]} {item["size"]:,} bytes',flush=True)
            return result
        except urllib.error.HTTPError as error:
            if error.code in (401,403,404): raise
            if attempt==4: raise
            time.sleep(min(60,int(error.headers.get('Retry-After','15'))))
        except (OSError,ValueError) as error:
            # Checksum failures are not appended to or silently accepted.
            if 'mismatch' in str(error) or attempt==4: raise
            time.sleep(3*(attempt+1))


def make_plan():
    inventory=read_json(ROOT/'source_inventory.json')
    revision=inventory['sha']; bypath={row['path']:row for row in inventory['files']}
    selected=['README.md','CITATION.cff','upstream/Dataset_documentation_v2.pdf',
       'upstream/GarmentCodeData_v2_official_train_valid_test_data_split.json',
       'upstream/neutral_body/mean_all.obj','upstream/neutral_body/mean_all.yaml',
       'default_body/batch-00.parquet','random_body/batch-00.parquet','default_body/batch-14.parquet']
    # Keep matching upstream simulation properties (material, units, quality flags).
    selected += [p for p in bypath if p.startswith('upstream/batch_properties/') and
                 any(f'/garments_5000_{batch}/' in p for batch in (0,14))]
    items=[]
    for name in selected:
        row=bypath[name]
        items.append(dict(source='GarmentCodeData_v2_repack',url=f'https://huggingface.co/datasets/{inventory["id"]}/resolve/{revision}/{urllib.parse.quote(name,safe="/")}?download=true',
           local='Raw/GarmentCodeData_v2/'+name,size=row['size'],sha256=row['lfs_sha256'],
           git_blob=row['blob_id'] if not row['lfs_sha256'] else None,revision=revision,
           license='CC-BY-4.0 (upstream and repack dataset cards)',source_page=f'https://huggingface.co/datasets/{inventory["id"]}',
           purpose='sewing_pattern_to_static_drape',training_license_status='documented'))
    for row in read_json(ROOT/'meshgraph_inventory.json'):
        if row.get('domain')!='sphere_simple' or row.get('error'): continue
        crc=next((value.split('=',1)[1] for value in (row.get('checksum') or '').split(',') if value.startswith('crc32c=')),None)
        items.append(dict(source='MeshGraphNets_sphere_simple',url=row['url'],local='Raw/MeshGraphNets/sphere_simple/'+row['file'],size=row['size'],crc32c=crc,
          license='Repository code Apache-2.0; separate dataset terms not explicit in README',
          source_page='https://github.com/google-deepmind/deepmind-research/tree/master/meshgraphnets',
          purpose='cloth_obstacle_motion',training_license_status='needs_dataset_license_confirmation'))
    total=sum(item['size'] for item in items)
    if total>28_000_000_000: raise ValueError(f'Raw plan exceeds 28 GB reserve: {total}')
    return dict(schema_version=1,created_utc=now(),max_total_bytes=30_000_000_000,
                raw_bytes=total,processed_reserve_bytes=30_000_000_000-total,items=items,
                notes=['Pinned public repack, not direct ETH original archives; per-file byte hashes checked',
                       'Do not equate file rows with garment designs or frames with independent sequences',
                       'MeshGraphNets excluded from default training until dataset-specific license confirmation'])


def collect(workers=2):
    DATA.mkdir(parents=True,exist_ok=True)
    plan=make_plan(); write_json(ROOT/'download_plan.json',plan)
    print(f'PLAN {len(plan["items"])} files; {plan["raw_bytes"]/1e9:.3f} GB raw; limit 30.000 GB including processed',flush=True)
    results=[]; errors=[]
    write_json(ROOT/'collection_status.json',dict(status='running',started_utc=now(),verified_files=0,verified_bytes=0))
    with ThreadPoolExecutor(max_workers=workers) as pool:
        futures={pool.submit(download,item):item for item in plan['items']}
        for future in as_completed(futures):
            item=futures[future]
            try: results.append(future.result())
            except Exception as error:
                errors.append(dict(local=item['local'],error=str(error))); print(f'ERROR {item["local"]}: {error}',flush=True)
            write_json(ROOT/'download_manifest.json',results)
            write_json(ROOT/'collection_status.json',dict(status='running',updated_utc=now(),verified_files=len(results),
               total_files=len(plan['items']),verified_bytes=sum(r['size'] for r in results),errors=errors))
    state=dict(status='complete' if not errors else 'partial',updated_utc=now(),verified_files=len(results),total_files=len(plan['items']),
               verified_bytes=sum(r['size'] for r in results),errors=errors)
    write_json(ROOT/'collection_status.json',state)
    print(json.dumps(state),flush=True)
    if errors: raise SystemExit(1)


if __name__=='__main__':
    parser=argparse.ArgumentParser(); parser.add_argument('--workers',type=int,default=2)
    args=parser.parse_args(); collect(max(1,min(2,args.workers)))
