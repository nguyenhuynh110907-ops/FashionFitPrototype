using System;
using System.Collections.Generic;
using UnityEngine;

namespace FashionFit
{
    // Separate sewn components before reducing the simulation mesh. A shirt
    // touching the waistband must never weld itself to a skirt or trousers.
    public sealed class ContactClothMesh
    {
        public Vector3[] Points;
        public int[] Faces, ParticleForRender;
        struct Binding { public int a, b, c; public Vector3 bary; public float offset; }
        Binding[] bindings;
        bool[] included;
        public ContactClothMesh(Vector3[] source, int[] faces, int picked)
        {
            var parent = new int[source.Length];
            int Root(int i) { while (parent[i] != i) { parent[i] = parent[parent[i]]; i = parent[i]; } return i; }
            void Union(int a, int b) { parent[Root(b)] = Root(a); }
            var welded = new Dictionary<Vector3Int, int>();
            for (int i = 0; i < source.Length; i++)
            {
                parent[i] = i;
                var key = Vector3Int.RoundToInt(source[i] * 100000);
                if (welded.TryGetValue(key, out int j)) Union(i, j); else welded.Add(key, i);
            }
            for (int i = 0; i < faces.Length; i += 3) { Union(faces[i], faces[i + 1]); Union(faces[i], faces[i + 2]); }
            int selected = Root(Mathf.Clamp(picked, 0, source.Length - 1));
            included = new bool[source.Length];
            ParticleForRender = new int[source.Length];
            var ids = new Dictionary<Vector3Int, int>();
            var vertices = new List<Vector3>();
            for (int i = 0; i < source.Length; i++)
            {
                ParticleForRender[i] = -1;
                if (Root(i) != selected) continue;
                included[i] = true;
                var key = Vector3Int.RoundToInt(source[i] * 100000);
                if (!ids.TryGetValue(key, out int index)) { index = vertices.Count; ids.Add(key, index); vertices.Add(source[i]); }
                ParticleForRender[i] = index;
            }
            var output = new List<int>(); var visited = new HashSet<Vector3Int>();
            for (int i = 0; i < faces.Length; i += 3)
            {
                int a = ParticleForRender[faces[i]], b = ParticleForRender[faces[i + 1]], c = ParticleForRender[faces[i + 2]];
                if (a < 0 || b < 0 || c < 0 || a == b || b == c || c == a) continue;
                int low = Mathf.Min(a, Mathf.Min(b, c)), high = Mathf.Max(a, Mathf.Max(b, c));
                if (visited.Add(new Vector3Int(low, a + b + c - low - high, high))) { output.Add(a); output.Add(b); output.Add(c); }
            }
            var original = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            original.SetVertices(vertices); original.SetTriangles(output, 0);
            var simplifier = new UnityMeshSimplifier.MeshSimplifier(original);
            var options = UnityMeshSimplifier.SimplificationOptions.Default;
            options.PreserveBorderEdges = true;
            options.PreserveSurfaceCurvature = true;
            simplifier.SimplificationOptions = options;
            simplifier.SimplifyMesh(Mathf.Min(1, 1600f / (output.Count / 3)));
            var reduced = simplifier.ToMesh();
            Points = reduced.vertices; Faces = reduced.triangles;
            if (Application.isPlaying) { UnityEngine.Object.Destroy(original); UnityEngine.Object.Destroy(reduced); }
            else { UnityEngine.Object.DestroyImmediate(original); UnityEngine.Object.DestroyImmediate(reduced); }
            for (int i = 0; i < source.Length; i++)
                if (included[i])
                {
                    int best = 0;
                    for (int j = 1; j < Points.Length; j++) if ((Points[j] - source[i]).sqrMagnitude < (Points[best] - source[i]).sqrMagnitude) best = j;
                    ParticleForRender[i] = best;
                }
            if (Faces.Length == 0) throw new InvalidOperationException("Picked component has no usable cloth faces");
            var surface = new GarmentSurfaceTransfer.Surface(Points, Faces);
            bindings = new Binding[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                if (!included[i]) continue;
                var hit = surface.Nearest(source[i]); int f = hit.triangle * 3;
                int a = Faces[f], b = Faces[f + 1], c = Faces[f + 2];
                Vector3 normal = Vector3.Cross(Points[b] - Points[a], Points[c] - Points[a]).normalized;
                bindings[i] = new Binding { a = a, b = b, c = c, bary = hit.barycentric, offset = Vector3.Dot(source[i] - hit.point, normal) };
            }
        }
        public void Publish(Vector3[] positions, Vector3[] output, Matrix4x4 worldToLocal)
        {
            for (int i = 0; i < bindings.Length; i++)
            {
                if (!included[i]) continue;
                var b = bindings[i];
                Vector3 normal = Vector3.Cross(positions[b.b] - positions[b.a], positions[b.c] - positions[b.a]).normalized;
                output[i] = worldToLocal.MultiplyPoint3x4(positions[b.a] * b.bary.x + positions[b.b] * b.bary.y + positions[b.c] * b.bary.z + normal * b.offset);
            }
        }
    }
}
