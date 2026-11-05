"""Read-only print recognition on a texture/photo; never rewrites the design."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import sys

ROOT=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT));sys.path.insert(0,str(ROOT.parent/'FitVision'))
os.environ['HF_HUB_OFFLINE']='1'
import torch
from safetensors.torch import load_file
from fitvision import load_encoder, image_tensor, ENCODER_DIR


def predict(path):
    folder=ROOT/'Artifacts/PatternDetails'
    report=json.loads((folder/'training_report.json').read_text(encoding='utf-8'))
    checkpoint=folder/'pattern_head.safetensors'
    if hashlib.sha256(checkpoint.read_bytes()).hexdigest()!=report['checkpoint_sha256']:
        raise ValueError('Changed classifier checkpoint')
    if hashlib.sha256((ENCODER_DIR/'model.safetensors').read_bytes()).hexdigest()!=report['encoder_sha256']:
        raise ValueError('Changed frozen encoder')
    torch.set_num_threads(2);processor,encoder=load_encoder(False)
    categories=report['categories'];model=torch.nn.Linear(384,len(categories));model.load_state_dict(load_file(checkpoint));model.eval()
    with torch.inference_mode():
        pixels=image_tensor(path,processor).unsqueeze(0)
        feature=torch.nn.functional.normalize(encoder(pixel_values=pixels).last_hidden_state[:,0],dim=1)
        probability=torch.softmax(model(feature),dim=1)[0].tolist()
    ranking=sorted(zip(categories,probability),key=lambda row:-row[1])
    return dict(schema_version=1,file=str(Path(path).resolve()),
        image_sha256=hashlib.sha256(Path(path).read_bytes()).hexdigest(),predicted_pattern=ranking[0][0],
        scores=[dict(pattern=name,softmax_score=value) for name,value in ranking],
        calibrated_confidence=False,source_modified=False,
        scope='whole-image pattern recognition, six known classes; no geometry or UV changes',
        student_domain_validated=False)


if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('image',type=Path)
    p.add_argument('--output',type=Path);p.add_argument('--request-id',default='')
    a=p.parse_args();result=predict(a.image);result['request_id']=a.request_id
    text=json.dumps(result,ensure_ascii=False,indent=2)
    if a.output:
        a.output.parent.mkdir(parents=True,exist_ok=True)
        temporary=a.output.with_suffix(a.output.suffix+'.tmp')
        temporary.write_text(text,encoding='utf-8');temporary.replace(a.output)
    print(text)
