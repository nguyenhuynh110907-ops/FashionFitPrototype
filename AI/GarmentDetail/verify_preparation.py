"""Check source reconstruction, sewing topology, body alignment and saved weights.

Held-out test graphs are checked structurally only; quality metrics use validation.
"""
import hashlib
import json
from pathlib import Path
import sys

import numpy as np
import torch
import yaml
from safetensors.torch import load_file

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT))
from detail_graph import DATA, FEATURE_NAMES, BodyFrame, read_json, write_json, components, ply_geometry
from train_drape import DetailDrapeNet, load


def verify():
    folder = DATA / 'DetailTraining'
    full = read_json(folder / 'full_manifest.json')
    pilot = read_json(folder / 'pilot_manifest.json')
    assert pilot['source_manifest_sha256'] == hashlib.sha256((folder / 'full_manifest.json').read_bytes()).hexdigest()
    sources = {item['id']: item for rows in full['splits'].values() for item in rows}
    assert len(sources) == full['source_candidates']
    groups = {part: {r['design_group'] for r in rows} for part, rows in full['splits'].items()}
    for left, right in (('train', 'validation'), ('train', 'test'), ('validation', 'test')):
        assert not groups[left] & groups[right], 'Design leakage'
    seen = set()
    source_checks = []
    max_reconstruction_error = 0.
    for item in pilot['items']:
        assert item['id'] not in seen
        seen.add(item['id'])
        source = sources[item['id']]
        assert source['official_split'] == item['split'] and source['design_group'] == item['design_group']
        path = DATA / item['npz']
        meta = read_json(DATA / item['metadata'])
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        assert digest == item['sha256'] == meta['npz_sha256']
        spec = read_json(path.with_suffix('.pattern.json'))
        annotation = read_json(path.with_suffix('.annotations.json'))
        with np.load(path, allow_pickle=False) as a:
            n = len(a['rest'])
            assert a['features'].shape == (n, len(FEATURE_NAMES))
            assert np.array_equal(a['features'][:, :3], a['rest'])
            assert a['target'].shape == (n, 3)
            for value in a.values():
                assert value.dtype.kind != 'O' and np.isfinite(value).all()
            for key in ('faces', 'edges', 'render_to_graph'):
                assert a[key].min() >= 0 and a[key].max() < n
            assert a['render_faces'].min() >= 0 and a['render_faces'].max() < len(a['render_to_graph'])
            assert np.array_equal(a['render_to_graph'][a['render_faces']], a['faces'])
            assert a['render_uv'].shape == (len(a['render_to_graph']), 2)
            assert len(a['face_panel']) == len(a['faces'])
            assert a['face_panel'].min() >= -1 and a['face_panel'].max() < len(meta['panel_names'])
            assert a['panel_membership'].shape == (n, len(meta['panel_names']))
            assert a['panel_membership'].any(1).all()
            assert a['seam_membership'].shape == (n, meta['seam_count'])
            assert len(a['seam_edge_mask']) == len(a['edges'])
            f = meta['body_frame']
            basis = np.array(f['basis'])
            frame = BodyFrame(f['origin_m'], basis[:, 1], basis[:, 2], f['height_m'])
            assert np.allclose(frame.local(frame.world(a['rest'])), a['rest'], atol=1e-8)
            # Structural source comparison for one graph in each split, not model scoring.
            if item['split'] not in source_checks:
                raw = components(source)
                for kind, blob in raw.items():
                    assert hashlib.sha256(blob).hexdigest() == source['component_hashes'][kind]
                assert spec == json.loads(raw['specification'])
                assert annotation == yaml.load(raw['vertex_labels'], Loader=yaml.CSafeLoader)
                for key, kind in (('rest', 'box_mesh'), ('target', 'sim_mesh')):
                    vertices, faces, uv = ply_geometry(raw[kind], include_uv=True)
                    error = np.abs(frame.world(a[key])[a['render_to_graph']] - vertices / 100).max()
                    assert error < 5e-7, 'Source garment shape changed during alignment'
                    max_reconstruction_error = max(max_reconstruction_error, float(error))
                    assert np.array_equal(faces, a['render_faces']) and np.array_equal(uv, a['render_uv'])
                source_checks.append(item['split'])
    # Rotation/translation equivariance, with an off-centre asymmetric point cloud.
    points = np.array([[.12, .9, -.2], [-.31, 1.6, .22], [.6, -.5, .1]])
    angle = .73
    rotation = np.array([[np.cos(angle), 0, np.sin(angle)], [0, 1, 0], [-np.sin(angle), 0, np.cos(angle)]])
    translation = np.array([1.7, -.8, .34])
    frame = BodyFrame([.08, 1.1, .02], [0, 1, 0], [0, 0, 1], 1.72)
    transformed = BodyFrame(frame.origin @ rotation.T + translation, rotation @ frame.basis[:, 1], rotation @ frame.basis[:, 2], frame.height)
    assert np.allclose(frame.local(points), transformed.local(points @ rotation.T + translation), atol=1e-12)
    assert np.allclose(np.linalg.norm(points[1:] - points[0], axis=1) / frame.height,
                       np.linalg.norm(frame.local(points)[1:] - frame.local(points)[0], axis=1))
    torch.set_num_threads(4)
    artifacts = ROOT / 'Artifacts'
    report = read_json(artifacts / 'training_report.json')
    assert report['manifest_sha256'] == hashlib.sha256((folder / (report['dataset'] + '_manifest.json')).read_bytes()).hexdigest(), 'Graphs changed since training; train again'
    checkpoint = artifacts / 'detail_drape.safetensors'
    assert hashlib.sha256(checkpoint.read_bytes()).hexdigest() == report['checkpoint_sha256']
    weights = load_file(checkpoint)
    assert all(torch.isfinite(value).all() for value in weights.values())
    model = DetailDrapeNet()
    model.load_state_dict(weights, strict=True)
    model.eval()
    metrics = []
    with torch.inference_mode():
        for item in pilot['items']:
            if item['split'] != 'validation':
                continue
            batch = load(item, 'pilot')
            prediction = model(batch)
            assert torch.isfinite(prediction).all()
            meta = read_json(DATA / item['metadata'])
            height = meta['body_frame']['height_m']
            def rmse(p):
                return float(torch.sqrt((p - batch['target']).square().sum(1).mean())) * height * 1000
            metrics.append(dict(id=item['id'], vertex_rmse_mm=rmse(prediction), rest_baseline_rmse_mm=rmse(batch['rest'])))
    total = sum(path.stat().st_size for path in DATA.rglob('*') if path.is_file())
    assert total <= 30_000_000_000
    result = dict(status='passed', checked_pilot_graphs=len(seen), full_candidate_count=len(sources),
                  full_graph_validation_completed=False, design_split_leakage=0,
                  source_reconstruction_split_examples=source_checks, max_source_reconstruction_error_m=max_reconstruction_error,
                  source_pattern_curves_stitch_flags_and_uv_preserved=True,
                  body_frame_roundtrip_and_rigid_equivariance=True, checkpoint_reload_and_finite_inference=True,
                  validation_samples=len(metrics), validation_metrics=metrics,
                  mean_validation_vertex_rmse_mm=float(np.mean([m['vertex_rmse_mm'] for m in metrics])),
                  mean_validation_rest_baseline_rmse_mm=float(np.mean([m['rest_baseline_rmse_mm'] for m in metrics])),
                  test_model_evaluations=0, contact_loss_enabled=False, nonpenetration_certified=False,
                  total_data_bytes=total, within_30gb_limit=True,
                  note='Passing preparation QA does not mean passing garment quality or collision QA. Validation is a tiny pilot, not an independent test.')
    write_json(ROOT / 'verification.json', result)
    print(json.dumps(result), flush=True)


if __name__ == '__main__':
    verify()
