"""Prepare source-backed detail labels, sewing curves and 3D seam placement.

No photo-to-mesh pairs or microscopic material/trim labels are invented.
"""
from collections import Counter
import hashlib
import json
from pathlib import Path
import sys

import numpy as np

ROOT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT))
from detail_graph import DATA, FEATURE_NAMES, ROLE_NAMES, BodyFrame, read_json, write_json, role
from prepare_training import candidates

BASE = DATA / 'DetailTraining'
OUTPUT = BASE / 'SurfaceDetails'
VERTEX_TARGETS = ['sewn_vertex', 'open_boundary', 'neckline', 'armhole', 'waist', 'lower_interface', 'strapless_edge']
EDGE_TARGETS = ['sewn_edge', 'open_boundary_edge', 'same_panel_sewing']
INPUT_NAMES = FEATURE_NAMES[:6] + FEATURE_NAMES[-5:]
MISSING_FINE_DETAILS = ['thread_spacing_m', 'thread_radius_m', 'seam_allowance_m', 'hem_width_m',
                        'fabric_thickness_m', 'lining_mesh', 'buttons_3d', 'zipper_3d', 'pockets_3d', 'embroidery_uv']


def connectivity(spec, meta, item):
    names = meta['panel_names']
    graph = {name: set() for name in names}
    for stitch in spec['pattern']['stitches']:
        a, b = stitch[:2]
        graph[a['panel']].add(b['panel']); graph[b['panel']].add(a['panel'])
    seen = set(); groups = []
    for start in names:
        if start in seen:
            continue
        todo = [start]; group = set()
        while todo:
            node = todo.pop()
            if node in group:
                continue
            group.add(node); todo.extend(graph[node] - group)
        seen |= group; groups.append(group)
    panel_roles = {name: role(name, spec['pattern']['panels'][name].get('label')) for name in names}
    skirt = 'Skirt' in str(item['design_meta'].get('bottom'))
    joined = skirt and any(0 in {panel_roles[n] for n in group} and 1 in {panel_roles[n] for n in group} for group in groups)
    category = 'sewn_dress' if joined else 'separate_upper_and_skirt' if skirt and item['design_meta'].get('upper') else 'other'
    return category, [sorted(group) for group in groups]


def prepare():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    rows = candidates(); lookup = {row['id']: row for row in rows}
    pilot = read_json(BASE / 'pilot_manifest.json')
    eligible = [row for row in rows if row['design_meta'].get('upper') and 'Skirt' in str(row['design_meta'].get('bottom'))]
    write_json(OUTPUT / 'dress_candidates.json', dict(
        status='candidates_only_sewing_connectivity_not_checked_for_all', count=len(eligible),
        items=[{key: row[key] for key in ('id', 'design_group', 'official_split', 'source_file', 'design_meta')} for row in eligible]))
    items = []; counts = Counter(); curves = Counter()
    for source in pilot['items']:
        path = DATA / source['npz']
        assert hashlib.sha256(path.read_bytes()).hexdigest() == source['sha256']
        meta = read_json(DATA / source['metadata'])
        spec = read_json(path.with_suffix('.pattern.json'))
        annotations = read_json(path.with_suffix('.annotations.json'))
        category, connected = connectivity(spec, meta, lookup[source['id']])
        with np.load(path, allow_pickle=False) as a:
            n = len(a['rest']); edges = a['edges']
            labels = np.zeros((n, len(VERTEX_TARGETS)), dtype=np.float32)
            valid = np.zeros_like(labels, dtype=bool)
            labels[:, 0] = a['seam_membership'].any(1); labels[:, 1] = a['boundary_mask']
            valid[:, :2] = True
            rules = [('collar', 2), ('armhole', 3), ('waist', 4), ('lower_interface', 5), ('strapless_top', 6)]
            for token, column in rules:
                matching = [key for key in annotations if token in key.lower()]
                if matching:
                    valid[:, column] = True
                    for key in matching:
                        labels[np.asarray(annotations[key], dtype=np.int32), column] = 1
            all_edges = np.sort(np.concatenate((a['faces'][:, [0, 1]], a['faces'][:, [1, 2]], a['faces'][:, [2, 0]])), axis=1)
            unique, multiplicity = np.unique(all_edges, axis=0, return_counts=True)
            assert np.array_equal(unique, edges)
            same_panel = np.zeros(len(edges), dtype=bool)
            for index, stitch in enumerate(spec['pattern']['stitches']):
                if stitch[0]['panel'] == stitch[1]['panel']:
                    membership = a['seam_membership'][:, index]
                    same_panel |= membership[edges[:, 0]] & membership[edges[:, 1]]
            edge_labels = np.column_stack((a['seam_edge_mask'], multiplicity == 1, same_panel)).astype(np.float32)
            f = meta['body_frame']; basis = np.array(f['basis'])
            frame = BodyFrame(f['origin_m'], basis[:, 1], basis[:, 2], f['height_m'])
            # Inputs deliberately exclude the labels, panel roles and target pose.
            features = a['features'][:, list(range(6)) + list(range(22, 27))]
            values = dict(point_features=features, edges=edges, faces=a['faces'],
                          vertex_targets=labels, vertex_target_valid=valid,
                          edge_targets=edge_labels, edge_target_valid=np.ones_like(edge_labels, dtype=bool),
                          role_targets=a['features'][:, 14:22],
                          reference_positions_m=frame.world(a['target']).astype(np.float32),
                          seam_edges=edges[a['seam_edge_mask']],
                          same_panel_sewing_edges=edges[same_panel],
                          open_boundary_edges=edges[multiplicity == 1])
            target = OUTPUT / (source['id'] + '.npz')
            np.savez_compressed(target, **values)
            size = target.stat().st_size
            edge_records = []
            stitched = {}
            for index, stitch in enumerate(spec['pattern']['stitches']):
                for side in stitch[:2]:
                    stitched.setdefault((side['panel'], side['edge']), []).append(index)
            for panel_name, panel in spec['pattern']['panels'].items():
                for index, edge in enumerate(panel['edges']):
                    curve_type = edge.get('curvature', {}).get('type', 'straight')
                    curves[curve_type] += 1
                    edge_records.append(dict(panel=panel_name, edge_index=index,
                        endpoints_2d_source_cm=[panel['vertices'][i] for i in edge['endpoints']],
                        curvature_source=edge.get('curvature'), source_label=edge.get('label'),
                        sewing_ids=stitched.get((panel_name, index), []),
                        microscopic_dimensions=None))
            record = dict(id=source['id'], design_group=source['design_group'], split=source['split'],
                          category=category, connected_panel_groups=connected,
                          npz=str(target.relative_to(DATA)), sha256=hashlib.sha256(target.read_bytes()).hexdigest(),
                          source_graph_npz=source['npz'], source_graph_sha256=source['sha256'],
                          source_pattern_json=str(path.with_suffix('.pattern.json').relative_to(DATA)),
                          source_annotations_json=str(path.with_suffix('.annotations.json').relative_to(DATA)),
                          source_component_hashes=meta['source_component_hashes'],
                          graph_vertices=n, labeled_seam_edges=int(a['seam_edge_mask'].sum()),
                          fine_details={name: dict(value=None, valid=False) for name in MISSING_FINE_DETAILS},
                          pattern_edges=edge_records, stored_bytes=size,
                          pattern_curve_units='Source cm and original curve parameter conventions; not flattened or approximated',
                          same_panel_sewing_note='May represent a dart or fold; not automatically labeled as a dart',
                          lower_interface_note='Literal source label; not assumed to mean hem or natural waist')
            write_json(target.with_suffix('.json'), record)
            items.append({key: record[key] for key in ('id', 'design_group', 'split', 'category', 'npz', 'sha256')})
            counts[category] += 1
    # Existing photos are queued for review; no automatic match to a 3D garment.
    photos_root = ROOT.parent.parent / 'References/ModelWear100'
    photos = read_json(photos_root / 'manifest.json')
    queue = []
    for photo in photos:
        queue.append(dict(image_id=photo['id'], image_path=str((photos_root / photo['file']).relative_to(ROOT.parent.parent)),
                          image_sha256=photo['download_sha256'], category=photo['category'],
                          creator=photo['creator'], license=photo['license'], license_url=photo['license_url'], source_page=photo['source_page'],
                          annotation_status='pending_detail_review', exact_3d_match=None,
                          regions=[], neckline=None, hem=None, closures=None, pockets=None, lining=None,
                          geometry_training_enabled=False))
    # Never replace annotations a user may already have edited.
    queue_path = OUTPUT / 'photo_detail_annotation_queue.json'
    if not queue_path.exists():
        write_json(queue_path, dict(coordinates='normalized image XY in [0,1]; per-region visible/occluded/unknown', items=queue))
    write_json(OUTPUT / 'manifest.json', dict(schema_version=1, items=items,
               input_features=INPUT_NAMES, vertex_targets=VERTEX_TARGETS, edge_targets=EDGE_TARGETS,
               source_pilot_manifest_sha256=hashlib.sha256((BASE / 'pilot_manifest.json').read_bytes()).hexdigest(),
               role_targets=ROLE_NAMES, geometry_units='reference positions: metres; input positions: body-height normalized',
               unknown_labels_must_be_masked=True, missing_fine_detail_targets=MISSING_FINE_DETAILS,
               real_photo_mesh_pairs=0, model_trained_on_this_pack=False))
    total = sum(p.stat().st_size for p in DATA.rglob('*') if p.is_file())
    if total > 30_000_000_000:
        raise RuntimeError('30 GB collection budget exceeded')
    summary = dict(status='prepared', source_graphs=len(items), categories=dict(counts),
                   split_counts=dict(Counter(item['split'] for item in items)), dress_candidate_count=len(eligible),
                   pattern_edge_curve_types=dict(curves), photo_review_queue=len(photos), photo_mesh_pairs=0,
                   microscopic_material_targets_available=False, total_data_bytes=total, within_30gb_limit=True,
                   note='Source-backed construction labels and seam placement; not yet a trained button/zipper/embroidery generator')
    write_json(ROOT / 'surface_detail_preparation.json', summary)
    print(json.dumps(summary), flush=True)


if __name__ == '__main__':
    prepare()
