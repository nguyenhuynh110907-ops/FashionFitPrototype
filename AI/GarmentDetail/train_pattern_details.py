"""Train an offline print-category head; frozen local DINOv2, no generative claims."""
from collections import Counter
import copy
import hashlib
import io
import json
import os
from pathlib import Path
import sys
import tarfile
import time

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT))
sys.path.insert(0, str(ROOT.parent/'FitVision'))
os.environ['HF_HUB_OFFLINE']='1'
import numpy as np
import torch
from PIL import Image, ImageOps
from scipy.fft import dctn
from safetensors.torch import save_file
from sklearn.metrics import f1_score, accuracy_score, confusion_matrix
from sklearn.model_selection import StratifiedGroupKFold
from detail_graph import DATA, write_json
from fitvision import load_encoder, ENCODER_DIR

SOURCE = DATA/'Raw/DetailSources/pattern_manifest.json'
OUTPUT = DATA/'DetailTraining/PatternDetails'
ARTIFACTS = ROOT/'Artifacts/PatternDetails'


def train():
    started=time.perf_counter(); torch.manual_seed(28102026);torch.set_num_threads(2)
    OUTPUT.mkdir(parents=True,exist_ok=True);ARTIFACTS.mkdir(parents=True,exist_ok=True)
    source=json.loads(SOURCE.read_text(encoding='utf-8'))
    rows=[r for r in source['items'] if r['training_eligible']]
    images={}
    for archive in sorted({r['archive'] for r in rows}):
        wanted={r['member'] for r in rows if r['archive']==archive}
        with tarfile.open(DATA/archive,'r:gz') as tar:
            for member in tar:
                if member.name in wanted:
                    images[(archive,member.name)]=tar.extractfile(member).read()
    parent=list(range(len(rows)))
    def find(i):
        while parent[i]!=i:
            parent[i]=parent[parent[i]];i=parent[i]
        return i
    def union(a,b): parent[find(b)]=find(a)
    seen_hash={};seen_page={};phashes=[];unique=[];conflicts=[]
    for i,row in enumerate(rows):
        blob=images[(row['archive'],row['member'])]
        if hashlib.sha256(blob).hexdigest()!=row['sha256']: raise ValueError('Changed source image')
        if row['sha256'] in seen_hash:
            other=seen_hash[row['sha256']];union(other,i)
            if row['category']!=rows[other]['category']: conflicts.extend((i,other))
        else: seen_hash[row['sha256']]=i;unique.append(i)
        page=row['source_page']
        if page in seen_page:union(seen_page[page],i)
        else:seen_page[page]=i
        with Image.open(io.BytesIO(blob)) as image:
            grey=np.asarray(ImageOps.exif_transpose(image).convert('L').resize((32,32)),dtype=np.float32)
        low=dctn(grey,norm='ortho')[:8,:8].ravel()[1:]
        phashes.append(low>np.median(low))
    phashes=np.asarray(phashes)
    near_pairs=0
    for i in range(len(rows)):
        matches=np.flatnonzero(np.count_nonzero(phashes[i+1:]!=phashes[i],axis=1)<=4)+i+1
        for j in matches:union(i,int(j));near_pairs+=1
    # Remove byte-identical repetitions and conflicting-label copies from training.
    selected=[i for i in unique if i not in set(conflicts)]
    categories=sorted(source['categories']);labels=np.array([categories.index(rows[i]['category']) for i in selected])
    groups=np.array([find(i) for i in selected]);chosen=[dict(rows[i],group_id=str(find(i))) for i in selected]
    folds=StratifiedGroupKFold(n_splits=10,shuffle=True,random_state=28102026)
    assignment=np.full(len(chosen),-1)
    for fold,(_,indices) in enumerate(folds.split(np.zeros(len(chosen)),labels,groups)):
        assignment[indices]=fold
    split=np.where(assignment==0,'test',np.where(assignment==1,'validation','train'))
    for i,row in enumerate(chosen):row['split']=str(split[i])
    for part in ('train','validation','test'):
        if set(labels[split==part])!=set(range(len(categories))):raise ValueError('A category is missing in '+part)
    for a,b in [('train','validation'),('train','test'),('validation','test')]:
        assert not set(groups[split==a]) & set(groups[split==b])
    write_json(OUTPUT/'manifest.json',dict(items=chosen,categories=categories,
        source_manifest_sha256=hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
        splits='10-fold stratified group split: fold 0 test, fold 1 validation; other 8 train',
        group_basis='same museum object URL, exact SHA256, or 63-bit perceptual hash distance <=4',
        near_duplicate_pairs=near_pairs,byte_duplicate_images_removed=len(rows)-len(unique),
        conflicting_label_indices=sorted(set(conflicts)),real_garment_identity_verified=False,
        geometry_pairing=False,annotation_scope='whole image; not a localized print mask'))
    signature=hashlib.sha256((SOURCE.read_bytes()+ (ENCODER_DIR/'model.safetensors').read_bytes())).hexdigest()
    cache=OUTPUT/'features.npz'
    use_cache=False
    if cache.exists():
        with np.load(cache,allow_pickle=False) as old:
            use_cache=str(old['signature'].item())==signature and np.array_equal(old['ids'],np.array([r['id'] for r in chosen]))
            if use_cache:features=old['features'].copy()
    if not use_cache:
        processor,encoder=load_encoder(False);features=[]
        with torch.inference_mode():
            for start in range(0,len(chosen),8):
                batch=[]
                for row in chosen[start:start+8]:
                    with Image.open(io.BytesIO(images[(row['archive'],row['member'])])) as image:
                        image=ImageOps.pad(ImageOps.exif_transpose(image).convert('RGB'),(224,224),method=Image.Resampling.BICUBIC,color=(127,127,127))
                        batch.append(processor(images=image,do_resize=False,do_center_crop=False,return_tensors='pt')['pixel_values'][0])
                value=encoder(pixel_values=torch.stack(batch)).last_hidden_state[:,0]
                features.append(torch.nn.functional.normalize(value,dim=1).numpy())
                if start%80==0: print(f'PATTERN_FEATURES {min(start+8,len(chosen))}/{len(chosen)}',flush=True)
        features=np.concatenate(features).astype(np.float32)
        np.savez_compressed(cache,features=features,ids=np.array([r['id'] for r in chosen]),signature=np.array(signature))
    x=torch.from_numpy(features);y=torch.from_numpy(labels).long()
    train_idx=np.flatnonzero(split=='train');val_idx=np.flatnonzero(split=='validation');test_idx=np.flatnonzero(split=='test')
    model=torch.nn.Linear(x.shape[1],len(categories));optimizer=torch.optim.AdamW(model.parameters(),lr=.01,weight_decay=.05)
    class_counts=np.bincount(labels[train_idx],minlength=len(categories));weights=torch.tensor(len(train_idx)/(len(categories)*class_counts),dtype=torch.float32)
    def evaluate(indices):
        model.eval()
        with torch.inference_mode():pred=model(x[indices]).argmax(1).numpy()
        truth=labels[indices]
        return dict(accuracy=float(accuracy_score(truth,pred)),macro_f1=float(f1_score(truth,pred,labels=np.arange(len(categories)),average='macro',zero_division=0)),
            per_category_f1=dict(zip(categories,f1_score(truth,pred,labels=np.arange(len(categories)),average=None,zero_division=0).tolist())),
            confusion_matrix=confusion_matrix(truth,pred,labels=np.arange(len(categories))).tolist(),
            class_counts=np.bincount(truth,minlength=len(categories)).tolist())
    best=-1.;history=[];best_state=None
    for epoch in range(1,201):
        model.train();optimizer.zero_grad();loss=torch.nn.functional.cross_entropy(model(x[train_idx]),y[train_idx],weight=weights)
        loss.backward();optimizer.step();val=evaluate(val_idx)
        history.append(dict(epoch=epoch,train_loss=float(loss.detach()),validation_macro_f1=val['macro_f1']))
        if val['macro_f1']>best:best=val['macro_f1'];best_state=copy.deepcopy(model.state_dict());best_epoch=epoch
    model.load_state_dict(best_state);checkpoint=ARTIFACTS/'pattern_head.safetensors';save_file(model.state_dict(),checkpoint)
    report=dict(status='frozen_encoder_linear_head_trained',task='six whole-image textile-pattern categories',
        images=len(chosen),split_counts=dict(Counter(split.tolist())),categories=categories,best_epoch=best_epoch,epochs=200,
        validation=evaluate(val_idx),test=evaluate(test_idx),history=history,
        majority_train_class=categories[int(class_counts.argmax())],
        majority_baseline_test_accuracy=float(np.mean(labels[test_idx]==class_counts.argmax())),
        checkpoint_sha256=hashlib.sha256(checkpoint.read_bytes()).hexdigest(),
        prepared_manifest_sha256=hashlib.sha256((OUTPUT/'manifest.json').read_bytes()).hexdigest(),
        encoder='facebook/dinov2-small, existing local frozen checkpoint; not retrained',
        encoder_sha256=hashlib.sha256((ENCODER_DIR/'model.safetensors').read_bytes()).hexdigest(),
        preprocessing='EXIF transpose, RGB, bicubic pad 224, DINO normalization, normalized CLS',
        head_parameters=sum(p.numel() for p in model.parameters()),seed=28102026,elapsed_seconds=time.perf_counter()-started,
        test_used_for_optimizer_or_selection=False,rights_held_images_used=0,
        embroidery_geometry_generation=False,uv_transfer=False,student_domain_validated=False,
        limitation='Small museum-source image classifier; recognition does not reconstruct thread or copy a 3D garment',
        deployment='offline research only')
    write_json(ARTIFACTS/'training_report.json',report)
    print(json.dumps({k:v for k,v in report.items() if k!='history'}),flush=True)


if __name__=='__main__':train()
