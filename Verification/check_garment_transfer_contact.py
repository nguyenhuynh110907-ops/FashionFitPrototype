"""Independent dense, stationary contact checks; never hides body triangles."""
import argparse
import hashlib
import json
import os
from pathlib import Path
import struct
import sys

os.environ.setdefault('OPENBLAS_NUM_THREADS', '1')
os.environ.setdefault('OMP_NUM_THREADS', '1')
parser = argparse.ArgumentParser()
parser.add_argument('project', type=Path)
parser.add_argument('--qa-deps', type=Path, required=True)
args = parser.parse_args()
sys.path.insert(0, str(args.qa_deps.resolve()))
sys.path.insert(0, str(args.project.resolve() / 'Verification'))
import igl
import numpy as np
import trimesh
from trimesh.ray.ray_pyembree import RayMeshIntersector
from check_slow_body_contact import read_mesh

folder = args.project / 'Verification/GarmentTransfer'
rows = []
for path in sorted(folder.glob('Look*.bin')):
    with path.open('rb') as stream:
        assert stream.read(4) == struct.pack('<i', 0x53434631)
        body, cloth = read_mesh(stream), read_mesh(stream)
        assert not stream.read(1)
    assert body.is_watertight and body.is_winding_consistent
    body.ray = RayMeshIntersector(body)
    edges = cloth.vertices[cloth.edges_unique]
    # Quarter/mid/three-quarter edge samples catch thin details missed by centroids.
    points = np.vstack([cloth.vertices, cloth.triangles_center] +
                       [edges[:, 0] * (1 - t) + edges[:, 1] * t for t in (.25, .5, .75)])
    points = np.unique(points, axis=0)
    original_closest = trimesh.proximity.closest_point
    def exact_closest(mesh, query):
        squared, triangle, closest = igl.point_mesh_squared_distance(
            np.asarray(query, dtype=np.float64), np.asarray(mesh.vertices, dtype=np.float64),
            np.asarray(mesh.faces, dtype=np.int64))
        return closest, np.sqrt(np.maximum(squared, 0)), triangle
    try:
        trimesh.proximity.closest_point = exact_closest
        depth = np.maximum(trimesh.proximity.signed_distance(body, points), 0)
        # Positive control: a point close to the torso centre must be classified inside.
        centre = np.array([[0, 1.12, .01]])
        inside_depth = float(trimesh.proximity.signed_distance(body, centre)[0])
        assert inside_depth > .005, ('Inside control failed', inside_depth)
    finally:
        trimesh.proximity.closest_point = original_closest
    assert np.isfinite(depth).all()
    row = dict(file=path.name, sha256=hashlib.sha256(path.read_bytes()).hexdigest(),
               unique_surface_samples=len(points), samples_inside_over_1mm=int((depth > .001).sum()),
               max_sampled_penetration_mm=float(depth.max() * 1000),
               deepest_point_m=points[np.argmax(depth)].tolist(),
               positive_inside_control_mm=inside_depth * 1000, full_body_watertight=True)
    rows.append(row)
    print(json.dumps(row), flush=True)
assert len(rows) == 2
report = dict(status='passed' if all(r['samples_inside_over_1mm'] == 0 for r in rows) else 'failed',
              method='libigl exact triangle distance, trimesh closest-face sign plus Embree fallback; vertices, face centroids and three samples per unique edge',
              threshold_mm=1, body_mask_used=False, stationary_only=True,
              continuous_contact_certified=False, garment_self_collision_checked=False, cases=rows)
(folder / 'independent_contact.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
if report['status'] != 'passed':
    raise SystemExit(1)
