"""Independent sampled contact check. No runtime solver or body masking."""
from pathlib import Path
import argparse, hashlib, json, os, struct, sys
for key in ('OMP_NUM_THREADS', 'OPENBLAS_NUM_THREADS'): os.environ[key] = '1'

def main():
    p = argparse.ArgumentParser()
    p.add_argument('folder', type=Path); p.add_argument('--expected', type=int, required=True)
    p.add_argument('--layers', action='store_true'); p.add_argument('--qa-deps', type=Path)
    a = p.parse_args(); out = a.folder / 'independent_contact.json'
    result = {'status': 'NOT_ASSESSED', 'method': 'Full original body; libigl triangle distance + Embree ray parity; vertices, face centres and edge midpoints; 1 mm tolerance',
              'continuous_collision_certified': False, 'self_collision_certified': False, 'cases': []}
    try:
        if not a.folder.is_dir(): raise ValueError('Missing export folder')
        if a.qa_deps: sys.path.insert(0, str(a.qa_deps.resolve()))
        import numpy as np
        import trimesh
        import igl
        from trimesh.ray.ray_pyembree import RayMeshIntersector

        def read_mesh(f):
            raw = f.read(4)
            if len(raw) != 4: raise ValueError('Truncated vertex count')
            n, = struct.unpack('<i', raw)
            if not 3 <= n <= 10000000: raise ValueError('Invalid vertex count')
            v = np.frombuffer(f.read(n * 12), dtype='<f4').copy().reshape(n, 3)
            nfaces, = struct.unpack('<i', f.read(4))
            if not 3 <= nfaces <= 60000000 or nfaces % 3: raise ValueError('Invalid triangle index count')
            faces = np.frombuffer(f.read(nfaces * 4), dtype='<i4').copy().reshape(-1, 3)
            if not np.isfinite(v).all() or faces.min() < 0 or faces.max() >= n: raise ValueError('Invalid mesh values')
            return trimesh.Trimesh(vertices=v, faces=faces, process=False)

        def body_contact(body, mesh):
            points = np.unique(np.vstack([mesh.vertices[np.unique(mesh.faces)], mesh.triangles_center,
                                           mesh.vertices[mesh.edges_unique].mean(axis=1)]), axis=0)
            sq, _, _ = igl.point_mesh_squared_distance(points, body.vertices, body.faces)
            inside = body.contains(points); depth = np.where(inside, np.sqrt(np.maximum(sq, 0)), 0)
            if not np.isfinite(depth).all(): raise ValueError('Nonfinite distance')
            return dict(samples=len(points), inside_over_1mm=int((depth > .001).sum()), max_depth_mm=float(depth.max() * 1000))

        def crossings(first, second):
            edges = first.vertices[first.edges_unique]; delta = edges[:, 1] - edges[:, 0]
            length = np.linalg.norm(delta, axis=1); good = length > 1e-6
            edges, delta, length = edges[good], delta[good], length[good]
            locations, ids, _ = RayMeshIntersector(second).intersects_location(edges[:, 0], delta / length[:, None], multiple_hits=True)
            if not len(ids): return 0
            along = np.einsum('ij,ij->i', locations - edges[ids, 0], delta[ids] / length[ids, None])
            return int(((along > 1e-4) & (along < length[ids] - 1e-4)).sum())

        files = sorted(a.folder.glob('*.bin'))
        if len(files) != a.expected or not files: raise ValueError(f'Expected {a.expected} fresh exports, found {len(files)}')
        for path in files:
            with path.open('rb') as f:
                magic, = struct.unpack('<i', f.read(4))
                if magic != (0x4c415931 if a.layers else 0x53434631): raise ValueError('Wrong mesh export format')
                body = read_mesh(f); parts = [read_mesh(f) for _ in range(4 if a.layers else 1)]
                if f.read(1): raise ValueError('Trailing export data')
            if not body.is_watertight or not body.is_winding_consistent: raise ValueError('Original body is not closed/consistently wound')
            body.ray = RayMeshIntersector(body)
            checks = [body_contact(body, part) for part in parts]; pairs = []
            if a.layers:
                for i, j in ((1, 2), (1, 3), (2, 3)):
                    pairs.append(dict(parts=[i, j], crossings=crossings(parts[i], parts[j]) + crossings(parts[j], parts[i])))
            row = dict(file=path.name, sha256=hashlib.sha256(path.read_bytes()).hexdigest(), body=checks, layer_pairs=pairs)
            result['cases'].append(row)
            print(path.name, 'body>1mm', [v['inside_over_1mm'] for v in checks], 'crossings', [v['crossings'] for v in pairs], flush=True)
        result['status'] = 'passed' if all(all(c['inside_over_1mm'] == 0 for c in r['body']) and all(c['crossings'] == 0 for c in r['layer_pairs']) for r in result['cases']) else 'failed'
    except (ImportError, ModuleNotFoundError) as e: result['reason'] = 'QA dependencies unavailable: ' + str(e)
    except Exception as e: result.update(status='failed', reason=str(e))
    a.folder.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(result, indent=2), encoding='utf-8')
    return 0 if result['status'] == 'passed' else 2 if result['status'] == 'NOT_ASSESSED' else 1

if __name__ == '__main__': sys.exit(main())
