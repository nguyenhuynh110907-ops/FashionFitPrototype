"""Verify label provenance and two ephemeral optimizer smoke steps on TRAIN only."""
from collections import Counter
import hashlib
import json
from pathlib import Path
import sys

import numpy as np
import torch

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT))
from detail_graph import DATA, read_json, write_json
from surface_detail_data import FOLDER, SurfaceDetailDataset, masked_region_loss


def verify():
    manifest = read_json(FOLDER / 'manifest.json')
    assert manifest['source_pilot_manifest_sha256'] == hashlib.sha256((FOLDER.parent / 'pilot_manifest.json').read_bytes()).hexdigest()
    parts = {}; counts = Counter(); total_seams = total_edges = 0
    for split in ('train', 'validation', 'test'):
        data = SurfaceDetailDataset(split, dresses_only=False)
        parts[split] = set()
        for batch in data:
            a = batch['arrays']; m = batch['metadata']; n = len(a['point_features'])
            parts[split].add(batch['design_group']); counts[batch['category']] += 1
            assert a['point_features'].shape == (n, 11)
            assert a['vertex_targets'].shape == a['vertex_target_valid'].shape == (n, 7)
            assert a['edge_targets'].shape == a['edge_target_valid'].shape == (len(a['edges']), 3)
            for value in a.values():
                assert value.dtype.kind != 'O' and np.isfinite(value).all()
            assert a['edges'].min() >= 0 and a['edges'].max() < n
            assert not any(value['valid'] or value['value'] is not None for value in m['fine_details'].values())
            source = DATA / m['source_graph_npz']
            assert hashlib.sha256(source.read_bytes()).hexdigest() == m['source_graph_sha256']
            annotations = read_json(DATA / m['source_annotations_json'])
            spec = read_json(DATA / m['source_pattern_json'])
            with np.load(source, allow_pickle=False) as original:
                assert np.array_equal(a['point_features'], original['features'][:, list(range(6)) + list(range(22, 27))])
                assert np.array_equal(a['edge_targets'][:, 0], original['seam_edge_mask'])
                assert np.array_equal(a['vertex_targets'][:, 0], original['seam_membership'].any(1))
                assert np.array_equal(a['role_targets'], original['features'][:, 14:22])
                f = read_json(source.with_suffix('.json'))['body_frame']
                basis = np.array(f['basis'])
                expected = (original['target'] * f['height_m']) @ basis.T + np.array(f['origin_m'])
                assert np.allclose(a['reference_positions_m'], expected, atol=2e-7)
            for token, col in [('collar', 2), ('armhole', 3), ('waist', 4), ('lower_interface', 5), ('strapless_top', 6)]:
                matching = [key for key in annotations if token in key.lower()]
                assert np.all(a['vertex_target_valid'][:, col] == bool(matching))
                expected = np.zeros(n)
                for key in matching:
                    expected[annotations[key]] = 1
                assert np.array_equal(a['vertex_targets'][:, col], expected)
            for edge in m['pattern_edges']:
                original = spec['pattern']['panels'][edge['panel']]['edges'][edge['edge_index']]
                assert edge['curvature_source'] == original.get('curvature')
            total_seams += len(a['seam_edges']); total_edges += len(m['pattern_edges'])
    for a, b in [('train', 'validation'), ('train', 'test'), ('validation', 'test')]:
        assert not parts[a] & parts[b], 'Design-group leakage'
    # An all-unknown column must contribute zero gradient, not become a negative label.
    dummy = dict(arrays=dict(vertex_targets=np.zeros((1, 7), dtype=np.float32),
                            vertex_target_valid=np.array([[True, False, False, False, False, False, False]]),
                            edge_targets=np.zeros((1, 3), dtype=np.float32), edge_target_valid=np.zeros((1, 3), dtype=bool)))
    logits = torch.zeros(1, 7, requires_grad=True); edge_logits = torch.zeros(1, 3, requires_grad=True)
    masked_region_loss(logits, edge_logits, dummy).backward()
    assert torch.count_nonzero(logits.grad[:, 1:]) == 0 and torch.count_nonzero(edge_logits.grad) == 0
    torch.manual_seed(27102026); torch.set_num_threads(4)
    train = SurfaceDetailDataset('train')
    assert len(train) >= 2 and len(SurfaceDetailDataset('validation')) > 0 and len(SurfaceDetailDataset('test')) > 0
    encoder = torch.nn.Sequential(torch.nn.Linear(11, 24), torch.nn.SiLU())
    vertex_head = torch.nn.Linear(24, 7); edge_head = torch.nn.Linear(24, 3)
    parameters = list(encoder.parameters()) + list(vertex_head.parameters()) + list(edge_head.parameters())
    optimizer = torch.optim.AdamW(parameters, lr=3e-4)
    before = [p.detach().clone() for p in parameters]; losses = []
    for batch in (train[0], train[1]):
        a = batch['arrays']; features = torch.as_tensor(a['point_features']); edges = torch.as_tensor(a['edges']).long()
        optimizer.zero_grad(); hidden = encoder(features)
        loss = masked_region_loss(vertex_head(hidden), edge_head((hidden[edges[:, 0]] + hidden[edges[:, 1]]) * .5), batch)
        assert torch.isfinite(loss)
        loss.backward(); assert all(p.grad is not None and torch.isfinite(p.grad).all() for p in parameters)
        optimizer.step(); losses.append(float(loss.detach()))
    assert sum(float((p.detach() - b).square().sum()) for p, b in zip(parameters, before)) > 0
    queue = read_json(FOLDER / 'photo_detail_annotation_queue.json')['items']
    project = ROOT.parent.parent
    for image in queue:
        path = project / image['image_path']
        assert hashlib.sha256(path.read_bytes()).hexdigest() == image['image_sha256'].lower()
        assert not image['geometry_training_enabled'] and image['exact_3d_match'] is None
    result = dict(status='passed', graphs=sum(counts.values()), categories=dict(counts),
                  dress_split_counts={part: len(SurfaceDetailDataset(part)) for part in parts},
                  source_pattern_edges=total_edges, source_3d_seam_edges=total_seams,
                  source_curve_parameters_and_annotations_preserved=True, design_split_leakage=0,
                  geometry_inputs_contain_detail_label_bits=False, unknown_target_gradient_zero=True,
                  optimizer_smoke_train_steps=2, optimizer_smoke_losses=losses, test_optimizer_steps=0,
                  saved_detail_model_checkpoint=False, photo_files_verified=len(queue), exact_photo_mesh_pairs=0,
                  fine_button_zipper_hem_material_labels_available=False,
                  total_data_bytes=sum(p.stat().st_size for p in DATA.rglob('*') if p.is_file()))
    assert result['total_data_bytes'] <= 30_000_000_000
    write_json(ROOT / 'surface_detail_verification.json', result); print(json.dumps(result), flush=True)


if __name__ == '__main__':
    verify()
