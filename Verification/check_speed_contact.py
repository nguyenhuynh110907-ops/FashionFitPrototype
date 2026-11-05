"""Independent full, unmasked body contact samples for a speed trial.

Uses exact nearest-triangle signed distance after a conservative enclosing-hull
broad phase. Finite pose samples do not certify continuous triangle contact.
"""
import os
os.environ.setdefault('OPENBLAS_NUM_THREADS', '1')
os.environ.setdefault('OMP_NUM_THREADS', '1')
import argparse
import concurrent.futures
import hashlib
import json
from pathlib import Path
import struct
import sys
import numpy as np
from scipy.spatial import ConvexHull
import trimesh
sys.path.insert(0, str(Path(__file__).resolve().parent))
from check_slow_body_contact import read_mesh


def inside_hull(points, equations):
    keep = np.ones(len(points), dtype=bool)
    for start in range(0, len(points), 384):
        keep[start:start+384] = (points[start:start+384] @ equations[:, :3].T + equations[:, 3]).max(axis=1) <= .00001
    return keep


def measure(path):
    reference_point = None
    if isinstance(path, tuple):
        path, reference_point = path
    path = Path(path)
    with path.open('rb') as stream:
        assert stream.read(4) == struct.pack('<i', 0x53434631)
        body, cloth = read_mesh(stream), read_mesh(stream)
        assert not stream.read(1)
    assert body.is_watertight and body.is_winding_consistent, 'Ambiguous source body sign'
    hull = ConvexHull(body.vertices)
    assert inside_hull(body.vertices, hull.equations).all()
    points = np.concatenate((cloth.vertices[np.unique(cloth.faces)], cloth.triangles_center))
    unique, inverse = np.unique(points, axis=0, return_inverse=True)
    query = inside_hull(unique, hull.equations)
    depth = np.zeros(len(unique))
    if os.environ.get('FASHIONFIT_QA_DEPS'):
        sys.path.insert(0, os.environ['FASHIONFIT_QA_DEPS'])
    import igl
    from trimesh.ray.ray_pyembree import RayMeshIntersector
    # Preserve trimesh's conservative closest-face sign, including points whose
    # nearest face says inside even when an even/odd ray alone says outside.
    body.ray = RayMeshIntersector(body)
    candidate_ids = np.flatnonzero(query)
    selected = unique[query]
    original_closest = trimesh.proximity.closest_point
    def aabb_closest(mesh, points):
        squared, triangle_id, closest = igl.point_mesh_squared_distance(np.asarray(points,dtype=np.float64),np.asarray(mesh.vertices,dtype=np.float64),np.asarray(mesh.faces,dtype=np.int64))
        return closest, np.sqrt(np.maximum(squared,0)), triangle_id
    try:
        trimesh.proximity.closest_point = aabb_closest
        distance = trimesh.proximity.signed_distance(body,selected)
        assert np.isfinite(distance).all()
        depth[candidate_ids] = np.maximum(distance,0)
        reference_depth = None if reference_point is None else float(max(0,trimesh.proximity.signed_distance(body,np.asarray([reference_point],dtype=np.float64))[0])*1000)
    finally:
        trimesh.proximity.closest_point = original_closest
    measured = depth[inverse]
    return dict(pose=path.stem, samples=len(points), body_watertight=True, masked_body_used=False,
                samples_inside_over_1mm=int((measured > .001).sum()),
                max_sampled_penetration_mm=float(measured.max()*1000),
                interior_unique_samples=int((depth>0).sum()),
                reference_worst_point_depth_mm=reference_depth,
                deepest_point_m=points[np.argmax(measured)].tolist(),
                source_mesh_sha256=hashlib.sha256(path.read_bytes()).hexdigest())


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('folder', type=Path)
    parser.add_argument('--expected', type=int, required=True)
    parser.add_argument('--workers', type=int, default=3)
    parser.add_argument('--qa-deps', type=Path)
    parser.add_argument('--reference', type=Path)
    args = parser.parse_args()
    folder = args.folder.resolve()
    if args.qa_deps:
        os.environ['FASHIONFIT_QA_DEPS'] = str(args.qa_deps.resolve())
    files = sorted(folder.glob('Look*_*.bin'))
    assert len(files) == args.expected, (len(files), args.expected)
    reference = None if not args.reference else {row['pose']: row for row in json.loads(args.reference.read_text(encoding='utf-8'))['poses']}
    jobs = files if reference is None else [(path,reference[path.stem]['deepest_point_m']) for path in files]
    rows = []
    with concurrent.futures.ProcessPoolExecutor(max_workers=args.workers) as pool:
        for row in pool.map(measure, jobs):
            rows.append(row)
            print(json.dumps(row), flush=True)
            (folder/'contact-progress.json').write_text(json.dumps({'completed': len(rows), 'total': len(files), 'rows': rows}, indent=2), encoding='utf-8')
    reference_agrees = False
    if args.reference:
        for row in rows:
            old = reference[row['pose']]
            assert row['source_mesh_sha256'] == old['source_mesh_sha256']
            assert row['samples_inside_over_1mm'] == old['samples_inside_over_1mm']
            assert row['reference_worst_point_depth_mm'] >= old['max_sampled_penetration_mm']-.02, (row,old)
        reference_agrees = True
    result = dict(status='passed' if all(row['samples_inside_over_1mm'] == 0 for row in rows) else 'failed',
                  scope='Referenced render vertices and triangle centroids versus unmasked posed body; libigl exact triangle distance with trimesh closest-face sign and Embree ray fallback; not continuous CCD',
                  threshold_mm=1, masked_body_used=False, continuous_contact_certified=False,
                  reference_threshold_agrees=reference_agrees,
                  reference_max_depth_difference_mm=None if reference is None else max(abs(row['max_sampled_penetration_mm']-reference[row['pose']]['max_sampled_penetration_mm']) for row in rows),
                  poses=rows, samples=sum(row['samples'] for row in rows),
                  samples_inside_over_1mm=sum(row['samples_inside_over_1mm'] for row in rows),
                  max_sampled_penetration_mm=max(row['max_sampled_penetration_mm'] for row in rows))
    (folder/'body_contact.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps({key: value for key, value in result.items() if key != 'poses'}), flush=True)
    if result['status'] != 'passed':
        raise SystemExit(1)


if __name__ == '__main__':
    main()
