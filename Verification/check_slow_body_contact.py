"""Independent sampled garment vs ORIGINAL posed body check, never masked body.

Uses closest-triangle signed distances from trimesh, not the runtime proxies.
Open/nonmanifold body regions can make the sign ambiguous. This is sampled
static-pose QA, not triangle intersection certification or continuous CCD.
"""
import json
from pathlib import Path
import struct

import numpy as np
import trimesh

ROOT = Path(__file__).resolve().parent / 'SlowPresentation'


def read_mesh(stream):
    n, = struct.unpack('<i', stream.read(4))
    vertices = np.frombuffer(stream.read(n * 12), dtype='<f4').copy().reshape(-1, 3)
    count, = struct.unpack('<i', stream.read(4))
    faces = np.frombuffer(stream.read(count * 4), dtype='<i4').copy().reshape(-1, 3)
    assert np.isfinite(vertices).all() and faces.min() >= 0 and faces.max() < n
    return trimesh.Trimesh(vertices=vertices, faces=faces, process=False)


def run(look=None):
    files = sorted(ROOT.glob('Look*_*.bin'))
    if look is not None:
        files = [path for path in files if path.name.startswith(f'Look{look}_')]
    assert len(files) == (3 if look is not None else 15), 'Need standing plus two walking poses for each selected outfit'
    rows = []
    for path in files:
        with path.open('rb') as stream:
            assert stream.read(4) == struct.pack('<i', 0x53434631)
            body, cloth = read_mesh(stream), read_mesh(stream)
            assert not stream.read(1)
        # Ignore unused cage points at authored arm openings: they emit no face.
        used = np.unique(cloth.faces)
        points = np.concatenate((cloth.vertices[used], cloth.triangles_center))
        distances = np.concatenate([trimesh.proximity.signed_distance(body, points[i:i + 512]) for i in range(0, len(points), 512)])
        assert np.isfinite(distances).all()
        depth = np.maximum(distances, 0)
        row = dict(pose=path.stem, source_body_watertight=bool(body.is_watertight),
                   source_body_winding_consistent=bool(body.is_winding_consistent), samples=len(points),
                   vertices=len(cloth.vertices), referenced_vertices=len(used), unused_vertices_omitted=len(cloth.vertices)-len(used), faces=len(cloth.faces),
                   samples_inside_over_1mm=int(np.count_nonzero(depth > .001)),
                   samples_inside_over_3mm=int(np.count_nonzero(depth > .003)),
                   max_sampled_penetration_mm=float(depth.max() * 1000),
                   median_signed_clearance_mm=float(-np.median(distances) * 1000))
        rows.append(row)
        print(json.dumps(row), flush=True)
    result = dict(scope='All referenced render vertices plus triangle centroids against original posed body; static sampled QA only',
                  status='passed' if all(row['samples_inside_over_1mm']==0 for row in rows) else 'failed',
                  masked_body_used=False, poses=rows,
                  samples_inside_over_1mm=sum(row['samples_inside_over_1mm'] for row in rows),
                  samples_inside_over_3mm=sum(row['samples_inside_over_3mm'] for row in rows),
                  max_sampled_penetration_mm=max(row['max_sampled_penetration_mm'] for row in rows),
                  continuous_triangle_nonpenetration_certified=False)
    name = 'source_body_contact.json' if look is None else f'source_body_contact_look{look}.json'
    (ROOT / name).write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps({k: v for k, v in result.items() if k != 'poses'}), flush=True)
    if result['status'] != 'passed':
        raise SystemExit('Original body contact QA failed (>1 mm sampled penetration)')


if __name__ == '__main__':
    import argparse
    parser = argparse.ArgumentParser()
    parser.add_argument('--folder', type=Path, default=ROOT)
    parser.add_argument('--look', type=int, choices=range(1, 6))
    args = parser.parse_args()
    ROOT = args.folder.resolve()
    run(args.look)
