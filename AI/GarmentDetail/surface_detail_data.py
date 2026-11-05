"""Variable-size graph reader for construction-detail supervision."""
import hashlib
from pathlib import Path
import sys

import numpy as np

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT))
from detail_graph import DATA, read_json

FOLDER = DATA / 'DetailTraining/SurfaceDetails'


class SurfaceDetailDataset:
    def __init__(self, split='train', dresses_only=True):
        if split not in ('train', 'validation', 'test'):
            raise ValueError('Invalid split')
        manifest = read_json(FOLDER / 'manifest.json')
        self.schema = manifest
        self.items = [item for item in manifest['items'] if item['split'] == split and
                      (not dresses_only or item['category'] == 'sewn_dress')]

    def __len__(self):
        return len(self.items)

    def __getitem__(self, index):
        item = self.items[index]
        path = DATA / item['npz']
        if hashlib.sha256(path.read_bytes()).hexdigest() != item['sha256']:
            raise ValueError('Detail cache changed')
        with np.load(path, allow_pickle=False) as archive:
            arrays = {key: np.array(value, copy=True) for key, value in archive.items()}
        return dict(id=item['id'], design_group=item['design_group'], split=item['split'],
                    category=item['category'], arrays=arrays, metadata=read_json(path.with_suffix('.json')))


def masked_region_loss(vertex_logits, edge_logits, batch):
    import torch
    import torch.nn.functional as F
    a = batch['arrays']
    def term(logits, target_key, valid_key):
        target = torch.as_tensor(a[target_key], dtype=logits.dtype, device=logits.device)
        valid = torch.as_tensor(a[valid_key], dtype=logits.dtype, device=logits.device)
        return (F.binary_cross_entropy_with_logits(logits, target, reduction='none') * valid).sum() / valid.sum().clamp_min(1)
    return term(vertex_logits, 'vertex_targets', 'vertex_target_valid') + term(edge_logits, 'edge_targets', 'edge_target_valid')
