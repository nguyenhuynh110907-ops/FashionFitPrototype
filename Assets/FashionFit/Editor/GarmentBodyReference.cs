#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FashionFit.Editor
{
    public static class GarmentBodyReference
    {
        public static readonly string[] Ids = { "fitting-female", "pose-female", "pose-male" };
        public const float MatchTolerance = .0001f;
        public const string Folder = "Assets/FashionFit/TransferTemplates/StudentReferences";

        public static RunwayAvatar Load(string id)
        {
            string resource;
            switch (id)
            {
                case "fitting-female": resource = "Fitting/female"; break;
                case "pose-female": resource = "PoseModels/female"; break;
                case "pose-male": resource = "PoseModels/male"; break;
                default: throw new InvalidDataException("UNKNOWN_BODY_REFERENCE " + id);
            }
            var root = new GameObject("Submission target " + id);
            try
            {
                var avatar = root.AddComponent<RunwayAvatar>();
                avatar.LoadTechnical(resource, id);
                return avatar;
            }
            catch { UnityEngine.Object.DestroyImmediate(root); throw; }
        }

        [Serializable]
        public sealed class MatchResult
        {
            public int sourceVertices, referenceVertices, triangles;
            public bool reorderedOrSplit;
            public float maxErrorMeters;
        }

        // Match the exported reference even if an importer reorders vertices or
        // splits them at UV seams. Clothing vertices are never reordered here.
        public static MatchResult Match(Mesh source, Matrix4x4 matrix, Mesh reference)
        {
            if (matrix.determinant <= 0) throw new InvalidDataException("MIRRORED_BODY_FRAME");
            var expected = reference.vertices;
            var points = source.vertices;
            var bins = new Dictionary<Vector3Int, List<int>>();
            Vector3Int Cell(Vector3 p) => new Vector3Int(Mathf.FloorToInt(p.x / MatchTolerance),
                Mathf.FloorToInt(p.y / MatchTolerance), Mathf.FloorToInt(p.z / MatchTolerance));
            for (int i = 0; i < expected.Length; i++)
            {
                var key = Cell(expected[i]);
                if (!bins.TryGetValue(key, out var list)) bins.Add(key, list = new List<int>());
                list.Add(i);
            }
            var mapping = new int[points.Length];
            var covered = new bool[expected.Length];
            var result = new MatchResult { sourceVertices = points.Length, referenceVertices = expected.Length,
                reorderedOrSplit = points.Length != expected.Length };
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 p = matrix.MultiplyPoint3x4(points[i]);
                GarmentSurfaceTransfer.Finite(p);
                var cell = Cell(p);
                int best = -1;
                float distance = MatchTolerance * MatchTolerance;
                for (int x = -1; x <= 1; x++)
                    for (int y = -1; y <= 1; y++)
                        for (int z = -1; z <= 1; z++)
                            if (bins.TryGetValue(cell + new Vector3Int(x, y, z), out var list))
                                foreach (int index in list)
                                {
                                    float d = (p - expected[index]).sqrMagnitude;
                                    if (d <= distance && (best < 0 || d < distance || index < best))
                                    { best = index; distance = d; }
                                }
                if (best < 0) throw new InvalidDataException("BODY_REFERENCE_MISMATCH vertex " + i +
                    ". Cơ thể hoặc tư thế khác body mẫu. Cần đăng ký tương ứng trước khi chuyển.");
                mapping[i] = best;
                covered[best] = true;
                result.maxErrorMeters = Mathf.Max(result.maxErrorMeters, Mathf.Sqrt(distance));
                result.reorderedOrSplit |= best != i;
            }
            foreach (bool value in covered)
                if (!value) throw new InvalidDataException("BODY_REFERENCE_INCOMPLETE. Thiếu đỉnh của body mẫu.");
            var counts = new Dictionary<Vector3Int, int>();
            var faces = reference.triangles;
            for (int i = 0; i < faces.Length; i += 3)
            {
                var key = OrientedFace(faces[i], faces[i + 1], faces[i + 2]);
                counts[key] = counts.TryGetValue(key, out int count) ? count + 1 : 1;
            }
            var sourceFaces = source.triangles;
            if (sourceFaces.Length != faces.Length) throw new InvalidDataException("BODY_TRIANGLES_CHANGED");
            for (int i = 0; i < sourceFaces.Length; i += 3)
            {
                var key = OrientedFace(mapping[sourceFaces[i]], mapping[sourceFaces[i + 1]], mapping[sourceFaces[i + 2]]);
                if (!counts.TryGetValue(key, out int count) || count == 0)
                    throw new InvalidDataException("BODY_TRIANGLES_CHANGED. Kiểm tra topology và chiều mặt body.");
                counts[key] = count - 1;
            }
            result.triangles = faces.Length / 3;
            return result;
        }

        static Vector3Int OrientedFace(int a, int b, int c)
        {
            if (a <= b && a <= c) return new Vector3Int(a, b, c);
            return b <= c ? new Vector3Int(b, c, a) : new Vector3Int(c, a, b);
        }

        public static string Export(string id)
        {
            var avatar = Load(id);
            Mesh body = null;
            GameObject root = null;
            try
            {
                body = GarmentTransferStudio.Snapshot(avatar.Renderer);
                string key = TailoredPoloStudy.GeometryKey(avatar.BodyMesh);
                string destination = Folder + "/" + id;
                Directory.CreateDirectory(destination);
                AssetDatabase.Refresh();
                string meshPath = destination + "/Body.asset";
                var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (existing) { EditorUtility.CopySerialized(body, existing); EditorUtility.SetDirty(existing); }
                else { AssetDatabase.CreateAsset(body, meshPath); existing = body; body = null; }
                var material = AssetDatabase.LoadAssetAtPath<Material>(destination + "/Body.mat");
                if (!material)
                {
                    material = new Material(avatar.Original);
                    AssetDatabase.CreateAsset(material, destination + "/Body.mat");
                }
                root = new GameObject(id);
                var source = new GameObject("SourceBody");
                source.transform.SetParent(root.transform, false);
                source.AddComponent<MeshFilter>().sharedMesh = existing;
                source.AddComponent<MeshRenderer>().sharedMaterial = material;
                new GameObject("Garment").transform.SetParent(root.transform, false);
                string prefab = destination + "/Reference.prefab";
                PrefabUtility.SaveAsPrefabAsset(root, prefab);
                AssetDatabase.SaveAssets();
                string export = "Samples/StudentGarments/" + id;
                Directory.CreateDirectory(export);
                using (var writer = new StreamWriter(export + "/BodyReference.obj", false, System.Text.Encoding.UTF8))
                {
                    writer.WriteLine("# FashionFit " + id + "; metres; Y up; Z forward; neutral pose");
                    foreach (var point in existing.vertices)
                        writer.WriteLine(string.Format(CultureInfo.InvariantCulture, "v {0:R} {1:R} {2:R}", point.x, point.y, point.z));
                    var indices = existing.triangles;
                    for (int i = 0; i < indices.Length; i += 3)
                        writer.WriteLine("f " + (indices[i] + 1) + " " + (indices[i + 1] + 1) + " " + (indices[i + 2] + 1));
                }
                var spec = new StudentGarmentSpec { displayName = "Student garment " + id, targetBodyId = id,
                    referenceBodyKey = key };
                File.WriteAllText(export + "/submission.json", JsonUtility.ToJson(spec, true));
                return prefab;
            }
            finally
            {
                if (body) UnityEngine.Object.DestroyImmediate(body);
                if (root) UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(avatar.gameObject);
            }
        }
    }
}
#endif
