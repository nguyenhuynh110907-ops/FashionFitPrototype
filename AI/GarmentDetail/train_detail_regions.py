"""CPU baseline for recognizing construction regions on registered 3D garments.

This does not synthesize fine geometry, transfer a garment, or regenerate textures.
"""
import argparse
import copy
import hashlib
import json
from pathlib import Path
import time
import sys

import numpy as np
import torch
from safetensors.torch import save_file, load_file

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT))
from detail_graph import DATA, write_json
from prepare_detail_regions import FOLDER, FEATURES, TARGETS

ARTIFACTS = ROOT / 'Artifacts/DetailRegions'


class DetailRegionNet(torch.nn.Module):
    def __init__(self):
        super().__init__()
        self.net = torch.nn.Sequential(torch.nn.Linear(len(FEATURES), 64), torch.nn.SiLU(),
            torch.nn.Linear(64, 64), torch.nn.SiLU(), torch.nn.Linear(64, 32), torch.nn.SiLU(),
            torch.nn.Linear(32, len(TARGETS)))
        self.register_buffer('mean', torch.zeros(len(FEATURES)))
        self.register_buffer('std', torch.ones(len(FEATURES)))
    def forward(self, x):
        return self.net(((x-self.mean)/self.std).clamp(-10, 10))


def load(item):
    path = DATA / item['npz']
    if hashlib.sha256(path.read_bytes()).hexdigest() != item['sha256']:
        raise ValueError('Changed input ' + item['id'])
    with np.load(path, allow_pickle=False) as a:
        return tuple(torch.from_numpy(a[k].copy()) for k in ('features', 'targets', 'valid'))


def statistics(model, items):
    model.eval(); tp=np.zeros(len(TARGETS)); fp=tp.copy(); fn=tp.copy()
    positives=tp.copy(); valid_counts=tp.copy(); bce=0.; count=0
    with torch.inference_mode():
        for item in items:
            x,y,v=load(item); logit=model(x); prediction=logit >= 0
            truth=y.bool()
            tp += ((prediction & truth) & v).sum(0).numpy()
            fp += ((prediction & ~truth) & v).sum(0).numpy()
            fn += ((~prediction & truth) & v).sum(0).numpy()
            positives += (truth & v).sum(0).numpy(); valid_counts += v.sum(0).numpy()
            bce += float((torch.nn.functional.binary_cross_entropy_with_logits(logit,y,reduction='none')*v).sum())
            count += int(v.sum())
    f1=np.divide(2*tp,2*tp+fp+fn,out=np.zeros_like(tp),where=(2*tp+fp+fn)>0)
    supported=positives>0
    return dict(unweighted_bce=bce/max(count,1), macro_f1_supported=float(f1[supported].mean()),
        threshold=.5, labels={name: dict(f1=float(f1[i]),
            precision=float(tp[i]/max(tp[i]+fp[i],1)), recall=float(tp[i]/max(tp[i]+fn[i],1)),
            positives=int(positives[i]), valid_vertices=int(valid_counts[i]),
            always_positive_f1=float(2*positives[i]/max(valid_counts[i]+positives[i],1))) for i,name in enumerate(TARGETS)})


def train(epochs=30):
    if epochs < 1:
        raise ValueError('epochs must be positive')
    torch.manual_seed(28102026); torch.set_num_threads(4); rng=np.random.default_rng(28102026)
    manifest=json.loads((FOLDER/'manifest.json').read_text(encoding='utf-8'))
    parts={p: [r for r in manifest['items'] if r['split']==p] for p in ('train','validation','test')}
    if not all(parts.values()):
        raise ValueError('Missing split')
    # Cache train only; no validation/test statistics enter standardization or class weights.
    training=[load(item) for item in parts['train']]
    feature_sample=torch.cat([x[::max(len(x)//256,1)] for x,y,v in training])
    model=DetailRegionNet(); model.mean.copy_(feature_sample.mean(0)); model.std.copy_(feature_sample.std(0).clamp_min(1e-4))
    pos=sum((y*v).sum(0) for x,y,v in training)
    valid=sum(v.sum(0) for x,y,v in training)
    if torch.any(pos==0):
        raise ValueError('Unsupported target in training set; do not train an empty class')
    weight=((valid-pos)/pos).clamp(1,100)
    optimizer=torch.optim.AdamW(model.parameters(),lr=1e-3,weight_decay=1e-4)
    best=-1.; best_state=None; best_epoch=None; history=[]; started=time.perf_counter()
    ARTIFACTS.mkdir(parents=True,exist_ok=True)
    initial={k:v.clone() for k,v in model.state_dict().items()}
    # Uniform vertex sampling preserves prevalence; weights are fitted from full train counts.
    for epoch in range(1,epochs+1):
        model.train(); losses=[]
        for idx in rng.permutation(len(training)):
            x,y,v=training[idx]
            sample=rng.choice(len(x),size=min(4096,len(x)),replace=False)
            optimizer.zero_grad(); logit=model(x[sample]); mask=v[sample]
            loss=(torch.nn.functional.binary_cross_entropy_with_logits(logit,y[sample],reduction='none',pos_weight=weight)*mask).sum()/mask.sum().clamp_min(1)
            if not torch.isfinite(loss): raise ValueError('Nonfinite loss')
            loss.backward(); torch.nn.utils.clip_grad_norm_(model.parameters(),1.); optimizer.step()
            losses.append(float(loss.detach()))
        validation=statistics(model,parts['validation'])
        row=dict(epoch=epoch,train_weighted_loss=float(np.mean(losses)),validation=validation)
        history.append(row)
        if validation['macro_f1_supported'] > best:
            best=validation['macro_f1_supported'];best_state=copy.deepcopy(model.state_dict());best_epoch=epoch
        print(json.dumps(dict(epoch=epoch,train_loss=row['train_weighted_loss'],validation_f1=validation['macro_f1_supported'])),flush=True)
    model.load_state_dict(best_state)
    path=ARTIFACTS/'detail_regions.safetensors';save_file({k:v.contiguous() for k,v in model.state_dict().items()},path)
    # Evaluate held-out test once, after freezing model selection and threshold.
    final_validation=statistics(model,parts['validation']); test=statistics(model,parts['test'])
    report=dict(status='research_baseline_trained',task='3d_construction_region_recognition',
        split_counts={p:len(v) for p,v in parts.items()},epochs=epochs,best_epoch=best_epoch,
        seed=28102026,parameters=sum(p.numel() for p in model.parameters()),
        optimizer_steps=epochs*len(training),training_vertices=sum(len(x) for x,y,v in training),
        validation=final_validation,test=test,history=history,
        checkpoint_sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
        manifest_sha256=hashlib.sha256((FOLDER/'manifest.json').read_bytes()).hexdigest(),
        training_ids=[r['id'] for r in parts['train']],validation_ids=[r['id'] for r in parts['validation']],
        test_ids=[r['id'] for r in parts['test']],features=FEATURES,targets=TARGETS,
        train_only_positive_weights=weight.tolist(),test_used_for_optimizer_or_selection=False,
        parameter_update_squared_norm=sum(float((model.state_dict()[k]-v).square().sum()) for k,v in initial.items()),
        elapsed_seconds=time.perf_counter()-started,torch_version=torch.__version__,device='cpu',
        source_license='CC-BY-4.0',source='GarmentCodeData v2, existing verified Parquet archives',
        synthetic_geometry=True,student_file_generalization_validated=False,
        generates_embroidery=False,generates_new_geometry=False,retargets_to_mannequin=False,
        deployment='offline research only; no Unity runtime change')
    write_json(ARTIFACTS/'training_report.json',report)
    # Safe checkpoint reload: never pickle untrusted model files.
    restored=DetailRegionNet();restored.load_state_dict(load_file(path))
    with torch.inference_mode():
        assert torch.equal(restored(training[0][0][:32]),model(training[0][0][:32]))
    print(json.dumps({k:v for k,v in report.items() if k not in ('history','training_ids','validation_ids','test_ids')}),flush=True)


if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--epochs',type=int,default=30);a=p.parse_args();train(a.epochs)
