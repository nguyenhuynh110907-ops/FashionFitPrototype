"""Expand source-backed 3D construction labels without changing the older pilots."""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import sys

import numpy as np

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT))
from detail_graph import DATA, build, write_json
from prepare_training import candidates, ordered

FOLDER = DATA / 'DetailTraining/DetailRegions'
TARGETS = ['sewn_vertex', 'collar_panel', 'neck_attachment', 'armhole_attachment']
FEATURES = [axis for group in ('position', 'normal', 'neighbor_mean', 'neighbor_std') for axis in (group+'_x', group+'_y', group+'_z')] + ['log_degree']


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def geometry_features(positions, faces, edges):
    normal = np.zeros_like(positions)
    face_normal = np.cross(positions[faces[:, 1]]-positions[faces[:, 0]], positions[faces[:, 2]]-positions[faces[:, 0]])
    for c in range(3):
        np.add.at(normal, faces[:, c], face_normal)
    normal /= np.maximum(np.linalg.norm(normal, axis=1, keepdims=True), 1e-9)
    src = np.concatenate((edges[:, 0], edges[:, 1]))
    dst = np.concatenate((edges[:, 1], edges[:, 0]))
    rel = positions[src]-positions[dst]
    degree = np.bincount(dst, minlength=len(positions)).astype(np.float32)[:, None]
    mean = np.zeros_like(positions); second = np.zeros_like(positions)
    np.add.at(mean, dst, rel); np.add.at(second, dst, rel*rel)
    mean /= np.maximum(degree, 1); second /= np.maximum(degree, 1)
    std = np.sqrt(np.maximum(second-mean*mean, 0))
    return np.column_stack((positions, normal, mean, std, np.log1p(degree))).astype(np.float32)


def prepare(counts):
    FOLDER.mkdir(parents=True, exist_ok=True)
    rows = [r for r in candidates() if r['design_meta'].get('upper') and r['canonical_label_count'] <= 25000]
    selected = []
    for split, wanted in counts.items():
        seen = set()
        for row in ordered([r for r in rows if r['official_split'] == split]):
            if row['design_group'] in seen:
                continue
            seen.add(row['design_group']); selected.append(row)
            if len(seen) == wanted:
                break
        if len(seen) != wanted:
            raise ValueError('Insufficient distinct designs in ' + split)
    groups = {p: {r['design_group'] for r in selected if r['official_split'] == p} for p in counts}
    assert not (groups['train'] & groups['validation'] or groups['train'] & groups['test'] or groups['validation'] & groups['test'])
    records = []; rejected = []
    # Order by Parquet location to avoid re-reading large row groups for every graph.
    selected.sort(key=lambda r: (r['source_file'], r['row_start']))
    for row in selected:
        path = FOLDER / (row['id']+'.npz')
        metadata_path = path.with_suffix('.json')
        if path.exists() and metadata_path.exists():
            record = json.loads(metadata_path.read_text(encoding='utf-8'))
            if record['sha256'] == sha(path) and record['source_hashes'] == row['component_hashes']:
                # Retire the earlier unsupported waist head, retaining the source labels as unknown.
                if 'waist_attachment' in record['positive_vertices']:
                    with np.load(path, allow_pickle=False) as old:
                        arrays = {key: old[key].copy() for key in old.files}
                    arrays['targets'] = arrays['targets'][:, :len(TARGETS)]
                    arrays['valid'] = arrays['valid'][:, :len(TARGETS)]
                    np.savez_compressed(path, **arrays)
                    record['positive_vertices'].pop('waist_attachment')
                    record['valid_vertices'].pop('waist_attachment')
                    record['sha256'] = sha(path)
                    write_json(metadata_path, record)
                records.append(record); continue
        try:
            a, meta, spec, annotations = build(row)
            x = geometry_features(a['target'], a['faces'], a['edges'])
            y = np.zeros((len(x), len(TARGETS)), np.float32)
            valid = np.zeros_like(y, dtype=bool)
            y[:, 0] = a['seam_membership'].any(1); valid[:, 0] = True
            # Derived from literal collar panel names/labels, separate from neck support.
            y[:, 1] = a['features'][:, 17]; valid[:, 1] = True
            for token, col in [('collar', 2), ('armhole', 3)]:
                matching = [key for key in annotations if token in key.lower()]
                if matching:
                    valid[:, col] = True
                    for key in matching:
                        y[np.asarray(annotations[key], np.int32), col] = 1
            if not np.isfinite(x).all():
                raise ValueError('Nonfinite features')
            # Preserve exact render UV, split vertices and faces for a future retargeter.
            # Model inputs do not include these labels, panel memberships or annotation bits.
            np.savez_compressed(path, features=x, targets=y, valid=valid,
                reference_positions=a['target'], faces=a['faces'], edges=a['edges'],
                render_to_graph=a['render_to_graph'], render_faces=a['render_faces'], render_uv=a['render_uv'])
            record = dict(id=row['id'], split=row['official_split'], design_group=row['design_group'],
                npz=str(path.relative_to(DATA)), sha256=sha(path), source_hashes=row['component_hashes'],
                source_file=row['source_file'], source_row_start=row['row_start'], source_row_count=row['row_count'],
                license='CC-BY-4.0', source_page='https://doi.org/10.3929/ethz-b-000690432',
                body_frame=meta['body_frame'], vertices=len(x), panel_names=meta['panel_names'],
                annotation_names=list(annotations), design_meta=row['design_meta'],
                positive_vertices={name: int(y[:, i].sum()) for i, name in enumerate(TARGETS)},
                valid_vertices={name: int(valid[:, i].sum()) for i, name in enumerate(TARGETS)},
                labels_origin='sewing memberships + literal panel collar role + source attachment vertex lists',
                texture_maps_available=False, buttons_embroidery_targets_available=False,
                real_photo_pair=None, nonpenetration_certified=False)
            write_json(metadata_path, record); records.append(record)
            print(f'REGIONS {len(records)}/{len(selected)} {row["id"]} {len(x)} vertices', flush=True)
        except (ValueError, KeyError) as error:
            rejected.append(dict(id=row['id'], error=str(error)))
    manifest = dict(schema_version=1, items=records, rejected=rejected, features=FEATURES, targets=TARGETS,
        task='source_3d_construction_region_recognition', input_state='draped mesh registered to source body waist/height frame',
        collar_panel_definition='Derived from collar panel name/label; NOT neck attachment and NOT expert-reviewed collar style',
        absent_attachment_labels='unknown; excluded by valid mask', split_policy='upstream official split; one example per design_group',
        photo_to_3d=False, missing_targets=['embroidery','buttons','zipper','thread_dimensions','print_uv'],
        upstream_revision='0e900868c0f7728b0522a13f917203082e8395d4')
    write_json(FOLDER / 'manifest.json', manifest)
    total = sum(p.stat().st_size for p in DATA.rglob('*') if p.is_file())
    summary = dict(graphs=len(records), split_counts=dict(Counter(r['split'] for r in records)),
        vertices=sum(r['vertices'] for r in records), distinct_designs=len({r['design_group'] for r in records}),
        positive_graph_counts={name: sum(r['positive_vertices'][name] > 0 for r in records) for name in TARGETS},
        positive_vertex_counts={name: sum(r['positive_vertices'][name] for r in records) for name in TARGETS},
        total_cloth_data_bytes=total, within_30gb_limit=total <= 30_000_000_000,
        manifest_sha256=sha(FOLDER / 'manifest.json'), rejected=rejected,
        source_geometry_license='CC-BY-4.0', new_3d_downloads=0, uses_existing_verified_3d_archives=True)
    write_json(ROOT / 'detail_regions_preparation.json', summary)
    print(json.dumps(summary), flush=True)
    if rejected or total > 30_000_000_000:
        raise RuntimeError('Preparation has rejections or exceeded budget; inspect report')


if __name__ == '__main__':
    p = argparse.ArgumentParser(); p.add_argument('--train', type=int, default=144)
    p.add_argument('--validation', type=int, default=28); p.add_argument('--test', type=int, default=28)
    args = p.parse_args()
    if min(args.train, args.validation, args.test) < 1:
        p.error('Positive split counts required')
    prepare(dict(train=args.train, validation=args.validation, test=args.test))
