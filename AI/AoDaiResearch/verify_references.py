"""Offline provenance, file integrity and pattern-connectivity checks. No training."""
from pathlib import Path
import hashlib,json,sqlite3,sys
from html.parser import HTMLParser
from urllib.parse import unquote
from PIL import Image
from pypdf import PdfReader
import pyarrow.parquet as pq

P=Path(__file__).resolve().parents[2]
D=P/'AI/ClothData/Data';O=D/'AoDaiResearch';A=P/'AI/AoDaiResearch';V=P/'Verification/AoDaiResearch'
def read(p):return json.loads(p.read_text(encoding='utf-8'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def total(p):return sum(f.stat().st_size for f in p.rglob('*') if f.is_file())
m=read(O/'manifest.json');support=read(O/'support_patterns/index.json');review=read(A/'source_review.json')
counts=dict(original_sha256=0,photo_sha1=0,photo_dimensions=0,pdf_page_counts=0,extracted_component_sha256=0,
    original_parquet_component_sha256=0,seam_endpoints=0,html_local_targets=0,runtime_audit_files_unchanged=0)
for x in m['items']:
    f=O/x['file'];assert f.is_file() and f.stat().st_size==x['bytes'],str(f)
    assert sha(f)==x['sha256'],str(f);counts['original_sha256']+=1
    if x['kind']=='real_reference_photo':
        assert hashlib.sha1(f.read_bytes()).hexdigest()==x['source_sha1'];counts['photo_sha1']+=1
        with Image.open(f) as im:im.verify()
        with Image.open(f) as im:assert im.size==(x['width'],x['height'])
        counts['photo_dimensions']+=1
        assert x['license'] in ('CC0','Public domain'),x['license']
        assert not x['eligibility']['physical_ground_truth']
    else:
        assert len(PdfReader(f).pages)==x['pages'];counts['pdf_page_counts']+=1
        assert x['eligibility']['model_training'] is False and x['eligibility']['redistribution'] is False

db=sqlite3.connect(D/'Processed/garments.sqlite3');cache={};groups=set()
for x in support:
    row=db.execute('select eligible,split,design_group,detail_json from garments where id=?',(x['id'],)).fetchone()
    assert row and row[0]==1 and row[1]=='train' and row[2]==x['design_group']
    assert row[2] not in groups;groups.add(row[2]);item=json.loads(row[3])
    assert item['source_file']==x['source_file'] and item['row_start']==x['row_start'] and item['row_count']==x['row_count']
    for kind,path in x['files'].items():
        assert sha(O/path)==x['component_sha256'][kind];counts['extracted_component_sha256']+=1
    source_path=D/x['source_file']
    if str(source_path) not in cache:cache[str(source_path)]=pq.ParquetFile(source_path).read_row_group(0).to_pylist()
    rows=cache[str(source_path)][x['row_start']:x['row_start']+x['row_count']]
    assert len(rows)==8
    for r in rows:
        assert hashlib.sha256(r['data']).hexdigest()==x['component_sha256'][r['kind']]
        counts['original_parquet_component_sha256']+=1
    pattern=read(O/x['files']['specification'])['pattern'];panels=pattern['panels']
    assert len(panels)==x['panel_count'] and len(pattern['stitches'])==x['seam_pair_count']
    for pair in pattern['stitches']:
        assert len(pair)==2
        for end in pair:
            assert end['panel'] in panels and 0<=end['edge']<len(panels[end['panel']]['edges'])
            counts['seam_endpoints']+=1
db.close()

class Links(HTMLParser):
    def handle_starttag(self,tag,attrs):
        for name,value in attrs:
            if name not in ('src','href') or not value or value.startswith(('https://','http://','#')):continue
            path=(O/unquote(value)).resolve()
            assert O.resolve() in path.parents and path.is_file(),str(path)
            counts['html_local_targets']+=1
Links().feed((O/'BROWSE.html').read_text(encoding='utf-8'))
for x in review['code_audit']:
    assert sha(P/x['file'])==x['sha256'],'Runtime changed during research: '+x['file']
    counts['runtime_audit_files_unchanged']+=1
assert 'AI/ClothData/Data/' in (P/'.gitignore').read_text(encoding='utf-8')
assert not list(O.rglob('*.partial'))
assert len(m['items'])==10 and len(support)==6
assert sum(x['panel_count'] for x in support)==80
assert sum(x['seam_pair_count'] for x in support)==240
blueprint=read(A/'aodai_blueprint.json')
assert blueprint['training']['run'] is False
assert all(v is None for k,v in blueprint['physical_measurements'].items() if k!='source')
candidate=read(O/'candidate_3d.json');assert not candidate['asset_downloaded'] and candidate['http_status']==401
size=total(D);assert size<30_000_000_000
result=dict(status='PASS',counts=counts,distinct_support_train_designs=len(groups),
    raw_pack_bytes=total(O),dataset_total_bytes=size,remaining_to_cap_bytes=30_000_000_000-size,
    notes=['Integrity and provenance checks, not garment simulation QA','No Ao Dai training or gameplay test run'])
V.mkdir(parents=True,exist_ok=True)
(V/'verification.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(result,indent=2))
