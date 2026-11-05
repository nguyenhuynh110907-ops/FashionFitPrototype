"""Re-evaluate frozen checkpoints, without any optimizer or model selection.

Pattern evaluation decodes the 101 original test images and recomputes DINO
features; it does not merely repeat metrics from an earlier JSON report.
"""
import hashlib
import io
import json
import os
from pathlib import Path
import sys
import tarfile
import time

HERE=Path(__file__).resolve().parent
PROJECT=HERE.parent.parent
sys.path.insert(0,str(PROJECT/'AI/GarmentDetail'))
sys.path.insert(0,str(PROJECT/'AI/FitVision'))
os.environ['HF_HUB_OFFLINE']='1'

import numpy as np
import torch
from PIL import Image,ImageOps
from safetensors.torch import load_file
from sklearn.metrics import accuracy_score,confusion_matrix,f1_score
from detail_graph import DATA,write_json
from prepare_detail_regions import FOLDER
from train_detail_regions import DetailRegionNet,statistics
from fitvision import load_encoder,ENCODER_DIR


def digest(path):return hashlib.sha256(path.read_bytes()).hexdigest()


def run():
    started=time.perf_counter();torch.set_num_threads(2)
    region_dir=PROJECT/'AI/GarmentDetail/Artifacts/DetailRegions'
    region_report=json.loads((region_dir/'training_report.json').read_text(encoding='utf-8'))
    region_checkpoint=region_dir/'detail_regions.safetensors'
    assert digest(region_checkpoint)==region_report['checkpoint_sha256']
    manifest=json.loads((FOLDER/'manifest.json').read_text(encoding='utf-8'))
    assert digest(FOLDER/'manifest.json')==region_report['manifest_sha256']
    region_model=DetailRegionNet();region_model.load_state_dict(load_file(region_checkpoint))
    region_metrics=statistics(region_model,[r for r in manifest['items'] if r['split']=='test'])
    assert abs(region_metrics['macro_f1_supported']-region_report['test']['macro_f1_supported'])<1e-8
    print('REGION_MODEL_REEVALUATED '+str(region_metrics['macro_f1_supported']),flush=True)
    pattern_dir=PROJECT/'AI/GarmentDetail/Artifacts/PatternDetails'
    pattern_report=json.loads((pattern_dir/'training_report.json').read_text(encoding='utf-8'))
    pattern_checkpoint=pattern_dir/'pattern_head.safetensors'
    assert digest(pattern_checkpoint)==pattern_report['checkpoint_sha256']
    assert digest(ENCODER_DIR/'model.safetensors')==pattern_report['encoder_sha256']
    pattern_manifest=DATA/'DetailTraining/PatternDetails/manifest.json'
    assert digest(pattern_manifest)==pattern_report['prepared_manifest_sha256']
    all_rows=json.loads(pattern_manifest.read_text(encoding='utf-8'))['items']
    test_rows=[r for r in all_rows if r['split']=='test'];categories=pattern_report['categories']
    blobs={}
    for archive in sorted({r['archive'] for r in test_rows}):
        wanted={r['member'] for r in test_rows if r['archive']==archive}
        with tarfile.open(DATA/archive,'r:gz') as tar:
            for member in tar:
                if member.name in wanted:blobs[(archive,member.name)]=tar.extractfile(member).read()
    processor,encoder=load_encoder(False);features=[]
    with torch.inference_mode():
        for start in range(0,len(test_rows),8):
            batch=[]
            for row in test_rows[start:start+8]:
                blob=blobs[(row['archive'],row['member'])]
                assert hashlib.sha256(blob).hexdigest()==row['sha256']
                with Image.open(io.BytesIO(blob)) as image:
                    image=ImageOps.pad(ImageOps.exif_transpose(image).convert('RGB'),(224,224),method=Image.Resampling.BICUBIC,color=(127,127,127))
                    batch.append(processor(images=image,do_resize=False,do_center_crop=False,return_tensors='pt')['pixel_values'][0])
            hidden=encoder(pixel_values=torch.stack(batch)).last_hidden_state[:,0]
            features.append(torch.nn.functional.normalize(hidden,dim=1))
            print(f'FRESH_TEST_IMAGES {min(start+8,len(test_rows))}/{len(test_rows)}',flush=True)
    features=torch.cat(features)
    model=torch.nn.Linear(384,len(categories));model.load_state_dict(load_file(pattern_checkpoint));model.eval()
    with torch.inference_mode():pred=model(features).argmax(1).numpy()
    truth=np.array([categories.index(r['category']) for r in test_rows])
    indices=[i for i,r in enumerate(all_rows) if r['split']=='test']
    with np.load(DATA/'DetailTraining/PatternDetails/features.npz',allow_pickle=False) as cached:
        assert np.array_equal(cached['ids'],np.array([r['id'] for r in all_rows]))
        max_feature_difference=float(np.abs(features.numpy()-cached['features'][indices]).max())
    accuracy=float(accuracy_score(truth,pred));macro_f1=float(f1_score(truth,pred,average='macro',zero_division=0))
    result=dict(status='passed' if abs(accuracy-pattern_report['test']['accuracy'])<1e-10 else 'prediction_changed',
        frozen_weights_unchanged=True,optimizer_steps=0,new_holdout=False,
        scope='Reproduction on the existing held-out test sets; not an independent student-domain evaluation',
        region_test=region_metrics,region_test_designs=28,
        region_test_designs_with_collar=sum(r['positive_vertices']['collar_panel']>0 for r in manifest['items'] if r['split']=='test'),
        pattern_test=dict(images=len(test_rows),fresh_source_image_inference=True,accuracy=accuracy,macro_f1=macro_f1,
            confusion_matrix=confusion_matrix(truth,pred,labels=np.arange(len(categories))).tolist(),
            per_class_f1=dict(zip(categories,f1_score(truth,pred,labels=np.arange(len(categories)),average=None,zero_division=0).tolist())),
            largest_absolute_feature_difference_from_training_cache=max_feature_difference),
        elapsed_seconds=time.perf_counter()-started)
    write_json(HERE/'model_reevaluation.json',result)
    print(json.dumps(result),flush=True)


if __name__=='__main__':run()
