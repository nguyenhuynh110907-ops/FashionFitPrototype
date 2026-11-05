from pathlib import Path
import argparse, json, os, struct, sys
os.environ['OMP_NUM_THREADS'] = '1'
os.environ['OPENBLAS_NUM_THREADS'] = '1'
parser = argparse.ArgumentParser()
parser.add_argument('folder', type=Path)
parser.add_argument('--qa-deps', type=Path, required=True)
args = parser.parse_args()
sys.path.insert(0, str(args.qa_deps.resolve()))
import numpy as np
import igl
import trimesh
from trimesh.ray.ray_pyembree import RayMeshIntersector

def mesh(f):
    count, = struct.unpack('<i', f.read(4))
    vertices = np.frombuffer(f.read(count * 12), '<f4').reshape(-1, 3).astype(float)
    count, = struct.unpack('<i', f.read(4))
    faces = np.frombuffer(f.read(count * 4), '<i4').reshape(-1, 3).copy()
    return trimesh.Trimesh(vertices=vertices, faces=faces, process=False)

folder = args.folder
rows = []
for path in sorted(folder.glob('*.bin')):
    with path.open('rb') as f:
        assert struct.unpack('<i', f.read(4))[0] == 0x46495432
        body, authored, posed, fitted = [mesh(f) for _ in range(4)]
    assert posed.is_watertight and posed.is_winding_consistent
    posed.ray = RayMeshIntersector(posed)
    points = fitted.vertices[np.unique(fitted.faces)]
    h = body.vertices[:, 1].max()
    torso = points[(np.abs(points[:, 0]) < .22) & (points[:, 1] > h * .62) & (points[:, 1] < h * .81)]
    distance = np.sqrt(igl.point_mesh_squared_distance(torso, posed.vertices, posed.faces)[0]) * 1000
    samples = np.unique(np.vstack([points, fitted.triangles_center, fitted.vertices[fitted.edges_unique].mean(axis=1)]), axis=0)
    depth = np.sqrt(igl.point_mesh_squared_distance(samples, posed.vertices, posed.faces)[0])
    depth[~posed.contains(samples)] = 0
    row = dict(name=path.stem, torso_gap_p50_mm=float(np.median(distance)), torso_gap_p95_mm=float(np.percentile(distance, 95)),
               torso_gap_max_mm=float(distance.max()), contact_samples=len(samples), inside_over_1mm=int((depth > .001).sum()), max_depth_mm=float(depth.max()*1000))
    rows.append(row)
    print(row, flush=True)
result = dict(status='passed' if len(rows) == 7 and all(r['inside_over_1mm'] == 0 and r['torso_gap_p95_mm'] < 50 for r in rows) else 'failed',
              checks=len(rows), exceptions=0, note='Neutral pose sampled shirt check. 50 mm torso p95 regression limit. No continuous collision certification.', cases=rows)
(folder/'fit_metrics.json').write_text(json.dumps(result, indent=2))
sys.exit(0 if result['status'] == 'passed' else 1)
