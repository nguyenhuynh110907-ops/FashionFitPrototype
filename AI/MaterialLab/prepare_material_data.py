"""Audit label provenance and prepare references without inventing strength labels."""
import argparse
import csv
import hashlib
import io
import json
import os
from pathlib import Path
import zipfile
from collections import Counter
import numpy as np
from PIL import Image

parser=argparse.ArgumentParser();parser.add_argument('--project',type=Path,required=True);args=parser.parse_args()
project=args.project.resolve();root=project/'AI/ClothData/Data/MaterialLab';output=root/'Prepared';output.mkdir(parents=True,exist_ok=True)
os.environ['MPLCONFIGDIR']=str(output/'matplotlib-cache')
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt
def dump(name,obj):(output/name).write_text(json.dumps(obj,ensure_ascii=False,indent=2),encoding='utf-8')
profile=project/'Assets/Resources/Fitting/cloth-materials.json';profile_before=hashlib.sha256(profile.read_bytes()).hexdigest()
archive=root/'Raw/EG2024/dataset_for_eg2024.zip'
samples=[];missing=[];groups={};image_hashes={};verified_images=0;gt={};counts={}
with zipfile.ZipFile(archive) as z:
    names=set(z.namelist());base='dataset_for_eg2024/'
    for name in sorted(names):
        if '/GT/' in name and name.endswith('.json'):
            obj=json.loads(z.read(name));ref=Path(name).stem.split('_GT_',1)[1];params=obj['params']
            assert all(np.isfinite(v) for v in params.values())
            gt[ref]=dict(values=params,archive_member=name,label_type='upstream_GT_simulation_reference',units='upstream simulator convention; not an XPBD compliance or strength-to-failure measurement')
    train=list(csv.DictReader(z.read(base+'dataset_train.csv').decode('utf-8-sig').splitlines()))
    reader=list(csv.reader(z.read(base+'dataset_test.csv').decode('utf-8-sig').splitlines()))
    header=[]
    for a,b in zip(reader[0],reader[1]):header.append((a+'.'+b).strip('.') if b else a)
    test=[]
    for row in reader[3:]:
        if not row or not row[0]:continue
        record=dict(zip(header,row));record['ref']=row[0];test.append(record)
    groups['train']={r['ref'] for r in train};groups['test']={r['ref'] for r in test}
    assert len(groups['train'])==len(train) and len(groups['test'])==len(test)
    assert groups['train'].isdisjoint(groups['test'])
    for split,rows in [('train',train),('test',test)]:
        for row in rows:
            ref=row['ref'];images={};issues=[]
            for side in ['front','back']:
                name=base+row['img_path_'+side].removeprefix('./')
                if name not in names:
                    missing.append(dict(id=ref,split=split,side=side,expected_member=name));issues.append('missing_'+side);continue
                blob=z.read(name)
                with Image.open(io.BytesIO(blob)) as image:
                    image.verify();resolution=list(image.size)
                digest=hashlib.sha256(blob).hexdigest();image_hashes.setdefault(digest,[]).append((ref,split,side));verified_images+=1
                images[side]=dict(archive='Raw/EG2024/dataset_for_eg2024.zip',member=name,sha256=digest,resolution=resolution)
            predictions={k:v for k,v in row.items() if 'MechMet' in k or 'MechIm' in k}
            metadata={k:v for k,v in row.items() if k not in predictions and not k.startswith('img_path_') and k not in {'ref',''}}
            for name in predictions:
                if predictions[name] and not np.isfinite(float(predictions[name])):issues.append('nonfinite_prediction')
            samples.append(dict(id=ref,official_split=split,images=images,metadata_raw=metadata,
                model_outputs_not_measured_labels=predictions,simulation_reference=gt.get(ref),
                approved_supervised_training=False,issues=issues,
                measured_failure=dict(breaking_force_N=None,elongation_at_break=None,tear_force_N=None,abrasion_cycles=None)))
    cross_duplicates=[dict(sha256=h,occurrences=rows) for h,rows in image_hashes.items() if len({r[1] for r in rows})>1]
    with (output/'material_samples.jsonl').open('w',encoding='utf-8') as stream:
        for sample in samples:stream.write(json.dumps(sample,ensure_ascii=False)+'\n')
    dump('simulation_GT_reference.json',gt)
    dump('eg2024_audit.json',dict(source='https://henardoel.github.io/henardoel/index_eg2024.html',official_train_rows=len(train),official_test_rows=len(test),
        paper_described_fabrics=1565,archive_csv_rows=len(samples),image_references_verified=verified_images,
        missing_images=missing,reference_GT_scenes=len(gt),train_rows_with_GT=sum(s['official_split']=='train' and s['simulation_reference'] is not None for s in samples),
        test_rows_with_GT=sum(s['official_split']=='test' and s['simulation_reference'] is not None for s in samples),
        GT_not_in_CSV=sorted(set(gt)-groups['train']-groups['test']),cross_split_identical_images=cross_duplicates,
        findings=['MechMet/MechIm columns are predictions; NMAE columns are errors, not material measurements.',
                  'GT simulation references belong to the official held-out set; do not train on them.',
                  'Raw thickness/density conventions need verification before SI conversion or solver calibration.',
                  'No paired breaking/tear/abrasion labels found; texture assets are not matched to these mechanics.']))

curve_rows=[];fig,axes=plt.subplots(1,2,figsize=(12,4.5),constrained_layout=True)
for index,condition in enumerate(['clamped','sliding']):
    raw=root/'Raw/Knitted2018'/f'{condition}_force_data.txt'
    array=np.loadtxt(raw,skiprows=1);assert array.ndim==2 and array.shape[1]==3 and np.isfinite(array).all()
    assert (np.diff(array[:,0])>=0).all();converted=array.copy();converted[:,1]*=.001
    with (output/f'{condition}_force_SI.csv').open('w',newline='',encoding='utf-8') as stream:
        writer=csv.writer(stream);writer.writerow(['time_s','total_vertical_size_m','force_N']);writer.writerows(converted)
    stride=max(1,len(converted)//10000);axes[index].plot(converted[::stride,1]*1000,converted[::stride,2],linewidth=.65,color='#226786');axes[index].set(xlabel='Total vertical size (mm)',ylabel='Measured force (N)',title=condition+' boundary');axes[index].grid(alpha=.2)
    curve_rows.append(dict(boundary_condition=condition,rows=len(array),time_s=[float(array[:,0].min()),float(array[:,0].max())],total_vertical_size_m=[float(converted[:,1].min()),float(converted[:,1].max())],measured_force_N=[float(array[:,2].min()),float(array[:,2].max())],breaking_force_N=None,measured_area_or_gauge_length_for_stress=None,negative_force_offsets_preserved=True))
fig.suptitle('Measured knit response — force maxima are NOT breaking forces');fig.savefig(output/'knit_force_response.png',dpi=170);plt.close(fig)
dump('knit_response_summary.json',dict(source_record='https://zenodo.org/records/17075127',license='CC-BY-4.0',material_systems=1,boundary_conditions=2,curves=curve_rows,force_unit='N',source_length_unit='mm',prepared_length_unit='m',note='Header says extension; author README specifies total vertical fabric size. No strain/Young modulus/strength inferred without natural gauge length and geometry.'))

catalog=json.loads((root/'texture_catalog.json').read_text(encoding='utf-8'));fig,axes=plt.subplots(2,3,figsize=(12,8),constrained_layout=True)
for axis,asset in zip(axes.flat,catalog):
    color=next(m for m in asset['maps'] if '_Color.' in m['path'])
    with Image.open(root/color['path']) as image:axis.imshow(image)
    axis.set_title(asset['id']+' · CC0');axis.axis('off')
fig.suptitle('Independent PBR appearance references — no measured strength pairing');fig.savefig(output/'texture_reference_sheet.png',dpi=150);plt.close(fig)
after=hashlib.sha256(profile.read_bytes()).hexdigest();assert profile_before==after
total=sum((Path(base)/name).stat().st_size for base,dirs,files in os.walk(project/'AI/ClothData/Data') for name in files);assert total<30_000_000_000
report=dict(status='audited_reference_data_prepared',sample_rows=len(samples),verified_image_references=verified_images,
    missing_image_references=len(missing),cross_split_identical_images=len(cross_duplicates),texture_assets=len(catalog),
    texture_maps=sum(len(x['maps']) for x in catalog),experimental_curve_rows=sum(x['rows'] for x in curve_rows),
    runtime_profile_sha256_before=profile_before,runtime_profile_sha256_after=after,runtime_changed=False,
    ground_truth_strength_labels_created=0,trained_model=False,final_data_bytes=total,budget_bytes=30_000_000_000)
dump('verification.json',report);print(json.dumps(report),flush=True)
