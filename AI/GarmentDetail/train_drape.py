"""Static, pattern-aware graph drape baseline; no runtime collision guarantee."""
import argparse
import copy
import hashlib
import json
import os
from pathlib import Path
import sys
import time

os.environ.setdefault('OMP_NUM_THREADS','4')
import numpy as np
import torch
from safetensors.torch import save_file

ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT))
from detail_graph import DATA,FEATURE_NAMES,build,read_json,write_json
FOLDER=DATA/'DetailTraining'


class DetailDrapeNet(torch.nn.Module):
    def __init__(self,hidden=32,steps=2):
        super().__init__()
        self.encoder=torch.nn.Sequential(torch.nn.Linear(len(FEATURE_NAMES),hidden),torch.nn.SiLU(),torch.nn.Linear(hidden,hidden))
        self.message=torch.nn.ModuleList([torch.nn.Sequential(torch.nn.Linear(hidden*2+4,hidden),torch.nn.SiLU(),torch.nn.Linear(hidden,hidden)) for _ in range(steps)])
        self.update=torch.nn.ModuleList([torch.nn.Sequential(torch.nn.Linear(hidden*2,hidden),torch.nn.SiLU(),torch.nn.Linear(hidden,hidden)) for _ in range(steps)])
        self.decoder=torch.nn.Sequential(torch.nn.Linear(hidden,hidden),torch.nn.SiLU(),torch.nn.Linear(hidden,3))
        torch.nn.init.zeros_(self.decoder[-1].weight); torch.nn.init.zeros_(self.decoder[-1].bias)
    def forward(self,batch):
        rest=batch['rest']; edges=batch['edges']
        sender=torch.cat((edges[:,0],edges[:,1])); receiver=torch.cat((edges[:,1],edges[:,0]))
        relative=rest[sender]-rest[receiver]; length=torch.linalg.vector_norm(relative,dim=1,keepdim=True)
        edge_features=torch.cat((relative,length),dim=1)
        degree=torch.zeros(len(rest),1,device=rest.device); degree.index_add_(0,receiver,torch.ones(len(receiver),1,device=rest.device)); degree.clamp_min_(1)
        hidden=self.encoder(batch['features'])
        for message,update in zip(self.message,self.update):
            values=message(torch.cat((hidden[sender],hidden[receiver],edge_features),dim=1))
            aggregate=torch.zeros_like(hidden); aggregate.index_add_(0,receiver,values)
            hidden=hidden+update(torch.cat((hidden,aggregate/degree),dim=1))
        return rest+self.decoder(hidden)


def tensors(arrays):
    keys=('features','rest','target','faces','edges','seam_edge_mask','anchor_mask')
    result={key:torch.from_numpy(np.array(arrays[key],copy=True)) for key in keys}
    result['faces']=result['faces'].long(); result['edges']=result['edges'].long()
    return result


def loss_terms(predicted,batch):
    target=batch['target']; edges=batch['edges']; faces=batch['faces']
    error=(predicted-target).square().mean(1)
    pred_edge=predicted[edges[:,0]]-predicted[edges[:,1]]
    target_edge=target[edges[:,0]]-target[edges[:,1]]
    lengths=(torch.linalg.vector_norm(pred_edge,dim=1)-torch.linalg.vector_norm(target_edge,dim=1)).square()
    seam=batch['seam_edge_mask']; anchor=batch['anchor_mask']>0
    def normals(p):
        value=torch.linalg.cross(p[faces[:,1]]-p[faces[:,0]],p[faces[:,2]]-p[faces[:,0]])
        return torch.nn.functional.normalize(value,dim=1,eps=1e-9)
    values=dict(vertex=error.mean(),edge=lengths.mean(),
        seam=lengths[seam].mean() if seam.any() else lengths.sum()*0,
        normal=(1-(normals(predicted)*normals(target)).sum(1)).mean(),
        attachment=error[anchor].mean() if anchor.any() else error.sum()*0)
    total=values['vertex']+.5*values['edge']+.3*values['seam']+.001*values['normal']+2*values['attachment']
    return total,values


def items(which,part):
    manifest=read_json(FOLDER/(which+'_manifest.json'))
    return [item for item in manifest['items'] if item['split']==part] if which=='pilot' else manifest['splits'][part]


def load(item,which):
    if which=='pilot':
        path=DATA/item['npz']
        if hashlib.sha256(path.read_bytes()).hexdigest()!=item['sha256']: raise ValueError('Prepared graph changed')
        with np.load(path,allow_pickle=False) as arrays: return tensors(arrays)
    return tensors(build(item)[0])


def train(which='pilot',epochs=8,smoke=False):
    if epochs<1: raise ValueError('Epoch count must be positive')
    torch.manual_seed(27102026); np.random.seed(27102026); torch.set_num_threads(4)
    model=DetailDrapeNet(); optimizer=torch.optim.AdamW(model.parameters(),lr=3e-4,weight_decay=1e-4)
    folder=ROOT/'Artifacts'; folder.mkdir(parents=True,exist_ok=True)
    train_items=items(which,'train'); validation=items(which,'validation')
    if not train_items or not validation: raise ValueError('Training/validation missing')
    history=[]; best=float('inf'); best_state=None; started=time.perf_counter()
    initial={key:value.clone() for key,value in model.state_dict().items()}
    if smoke: train_items=train_items[:2]; epochs=1
    for epoch in range(1,epochs+1):
        model.train(); running=[]; last_terms={}; steps=0
        for item in train_items:
            batch=load(item,which); optimizer.zero_grad(); predicted=model(batch)
            loss,terms=loss_terms(predicted,batch)
            if not torch.isfinite(loss): raise RuntimeError('Nonfinite loss')
            loss.backward(); norm=torch.nn.utils.clip_grad_norm_(model.parameters(),1.)
            if not torch.isfinite(norm): raise RuntimeError('Nonfinite gradient')
            optimizer.step(); running.append(float(loss.detach())); steps+=1
            last_terms={key:float(value.detach()) for key,value in terms.items()}
        row=dict(epoch=epoch,train_loss=float(np.mean(running)),optimizer_steps=steps,loss_terms_last_sample=last_terms)
        if not smoke:
            model.eval(); scores=[]
            with torch.inference_mode():
                for item in validation:
                    batch=load(item,which); scores.append(float(loss_terms(model(batch),batch)[0]))
            row['validation_loss']=float(np.mean(scores))
            if row['validation_loss']<best: best=row['validation_loss']; best_state=copy.deepcopy(model.state_dict())
        else: best_state=copy.deepcopy(model.state_dict())
        history.append(row); print(json.dumps(row),flush=True)
    model.load_state_dict(best_state)
    change=sum(float((model.state_dict()[key]-value).square().sum()) for key,value in initial.items())
    if change<=0: raise RuntimeError('No parameter update')
    path=folder/('smoke_graph.safetensors' if smoke else 'detail_drape.safetensors')
    save_file({key:value.contiguous() for key,value in model.state_dict().items()},path)
    report=dict(status='smoke_passed' if smoke else 'pilot_trained' if which=='pilot' else 'full_trained',
        dataset=which,epochs=epochs,training_samples=len(train_items),validation_samples=0 if smoke else len(validation),
        manifest_sha256=hashlib.sha256((FOLDER/(which+'_manifest.json')).read_bytes()).hexdigest(),
        training_item_ids=[item['id'] for item in train_items],validation_item_ids=[] if smoke else [item['id'] for item in validation],
        model_config=dict(hidden=32,message_steps=2),seed=27102026,torch_version=torch.__version__,
        test_samples_used=0,parameters=sum(p.numel() for p in model.parameters()),parameter_update_squared_norm=change,
        history=history,elapsed_seconds=time.perf_counter()-started,checkpoint_sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
        feature_names=FEATURE_NAMES,loss_weights=dict(vertex=1,edge=.5,seam=.3,normal=.001,attachment=2),
        supports_source_panels=True,source_uv_reconstruction=True,contact_loss_enabled=False,
        deployment='research_only',nonpenetration_certified=False,
        warning='Static pattern-graph baseline. Smoke mode checks the training path only; it is not a quality evaluation or an image-to-3D generator.')
    write_json(folder/('smoke_report.json' if smoke else 'training_report.json'),report)
    print(json.dumps({key:value for key,value in report.items() if key!='history'}),flush=True)


if __name__=='__main__':
    p=argparse.ArgumentParser(); p.add_argument('--dataset',choices=('pilot','full'),default='pilot'); p.add_argument('--epochs',type=int,default=8); p.add_argument('--smoke',action='store_true')
    a=p.parse_args(); train(a.dataset,a.epochs,a.smoke)
