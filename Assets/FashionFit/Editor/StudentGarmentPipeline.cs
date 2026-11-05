#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FashionFit.Editor
{
    [Serializable]
    public sealed class StudentGarmentSpec
    {
        public int schema = 1;
        public string displayName = "Student garment";
        public string targetBodyId, referenceBodyKey;
        public string sourceMode = "canonical";
        public string sourceBodyPath = "SourceBody", garmentPath = "Garment";
        public string restPose = "reference-neutral";
        public float unitMeters = 1;
        public Vector3 rotationEuler, offsetMeters;
    }

    public static class StudentGarmentPipeline
    {
        public const float EdgeChangeLimit = .05f;
        public const string OutputFolder = "Assets/Resources/TransferredGarments";

        [Serializable]
        public sealed class PartReport
        {
            public string name, sourceGeometryKey, resultGeometryKey;
            public int vertices, triangles, materialSlots, textureSlots;
            public bool identityAssessed;
            public float identityErrorMeters;
            public TransferMetrics geometry;
        }

        [Serializable]
        public sealed class Report
        {
            public string status, bodyId, referenceBodyKey, sourceMode;
            public string[] reviewReasons;
            public GarmentBodyReference.MatchResult bodyMatch;
            public PartReport[] parts;
            public float edgeChangeLimit = EdgeChangeLimit, penetrationToleranceMeters = .001f;
            public bool detailsPreserved, stationarySamplesPassed;
            public bool motionAssessed, selfCollisionAssessed, physicalMaterialCalibrated;
        }

        public sealed class Result
        {
            public TransferredGarmentAsset package;
            public Report report;
        }

        public static StudentGarmentSpec ReadSpec(string path)
        {
            if (!File.Exists(path)) throw new InvalidDataException("SUBMISSION_SPEC_MISSING " + path);
            var spec = JsonUtility.FromJson<StudentGarmentSpec>(File.ReadAllText(path));
            if (spec == null) throw new InvalidDataException("SUBMISSION_SPEC_INVALID");
            return spec;
        }

        // One entry point for the Editor window and batch submissions. The
        // caller owns the returned meshes until Save makes them persistent.
        public static Result Process(GameObject frame, StudentGarmentSpec spec)
        {
            if (!frame || spec == null) throw new InvalidDataException("SUBMISSION_INPUT_MISSING");
            if (spec.schema != 1 || spec.restPose != "reference-neutral") throw new InvalidDataException("SUBMISSION_SCHEMA_OR_POSE_UNSUPPORTED");
            if (spec.sourceMode != "canonical" && spec.sourceMode != "paired-body") throw new InvalidDataException("SOURCE_MODE_UNSUPPORTED");
            if (!Finite(spec.unitMeters) || spec.unitMeters <= 0 || spec.unitMeters > 10) throw new InvalidDataException("SOURCE_UNIT_INVALID");
            GarmentSurfaceTransfer.Finite(spec.rotationEuler);
            GarmentSurfaceTransfer.Finite(spec.offsetMeters);
            var bodyTransform = frame.transform.Find(spec.sourceBodyPath ?? "");
            var garmentTransform = frame.transform.Find(spec.garmentPath ?? "");
            if (!bodyTransform || !garmentTransform || bodyTransform == garmentTransform || bodyTransform.IsChildOf(garmentTransform)
                || garmentTransform.IsChildOf(bodyTransform)) throw new InvalidDataException("SUBMISSION_HIERARCHY_INVALID. Cần hai nhánh body và trang phục riêng.");
            var bodyRenderer = bodyTransform.GetComponent<Renderer>();
            if (!bodyRenderer) throw new InvalidDataException("SOURCE_BODY_RENDERER_MISSING");
            var target = GarmentBodyReference.Load(spec.targetBodyId);
            Mesh neutral = null, sourceBody = null;
            var inputs = new List<GarmentTransferStudio.Input>();
            TransferredGarmentAsset package = null;
            try
            {
                string key = TailoredPoloStudy.GeometryKey(target.BodyMesh);
                if (string.IsNullOrEmpty(spec.referenceBodyKey) || key != spec.referenceBodyKey)
                    throw new InvalidDataException("BODY_REFERENCE_VERSION_MISMATCH. Dùng submission.json đi cùng body mẫu hiện tại.");
                Matrix4x4 canonical = Matrix4x4.TRS(spec.offsetMeters, Quaternion.Euler(spec.rotationEuler), Vector3.one * spec.unitMeters)
                    * frame.transform.worldToLocalMatrix;
                neutral = GarmentTransferStudio.Snapshot(target.Renderer);
                sourceBody = GarmentTransferStudio.Snapshot(bodyRenderer);
                var sourceMatrix = canonical * bodyRenderer.transform.localToWorldMatrix;
                var report = new Report { bodyId = spec.targetBodyId, referenceBodyKey = key, sourceMode = spec.sourceMode };
                Vector3[] sourcePoints;
                int[] sourceFaces;
                if (spec.sourceMode == "canonical")
                {
                    report.bodyMatch = GarmentBodyReference.Match(sourceBody, sourceMatrix, neutral);
                    // Imported body indexing is irrelevant after its full
                    // geometry has matched the canonical reference.
                    sourcePoints = neutral.vertices;
                    sourceFaces = neutral.triangles;
                }
                else
                {
                    if (sourceMatrix.determinant <= 0) throw new InvalidDataException("MIRRORED_BODY_FRAME");
                    sourcePoints = sourceBody.vertices;
                    for (int i = 0; i < sourcePoints.Length; i++) sourcePoints[i] = sourceMatrix.MultiplyPoint3x4(sourcePoints[i]);
                    sourceFaces = sourceBody.triangles;
                }
                var engine = new GarmentSurfaceTransfer(sourcePoints, sourceFaces, neutral.vertices, neutral.triangles, target.BodyMesh.boneWeights);
                foreach (var renderer in garmentTransform.GetComponentsInChildren<Renderer>(true))
                {
                    if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                        throw new InvalidDataException("HIDDEN_GARMENT_PART " + renderer.name + ". Khai báo rõ các mảnh sẽ nộp.");
                    if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer))
                        throw new InvalidDataException("UNSUPPORTED_GARMENT_RENDERER " + renderer.name);
                    var mesh = GarmentTransferStudio.Snapshot(renderer);
                    inputs.Add(new GarmentTransferStudio.Input { renderer = renderer, mesh = mesh,
                        matrix = canonical * renderer.transform.localToWorldMatrix });
                    ValidateMesh(mesh, renderer);
                }
                if (inputs.Count == 0) throw new InvalidDataException("EMPTY_GARMENT_SUBMISSION");
                package = GarmentTransferStudio.BuildPackage(spec.displayName, engine, target, inputs);
                package.sourceDescription = "Student submission schema 1; " + spec.sourceMode + "; " + spec.targetBodyId + "; original garment geometry and materials retained.";
                var rows = new List<PartReport>();
                var reasons = new List<string>();
                for (int i = 0; i < inputs.Count; i++)
                {
                    var input = inputs[i];
                    var part = package.parts[i];
                    CheckEdgeContacts(part.mesh, engine.target, part.metrics);
                    float error = CheckDetails(input.mesh, input.matrix, part.mesh, spec.sourceMode == "canonical");
                    var row = new PartReport { name = input.renderer.name, sourceGeometryKey = TailoredPoloStudy.GeometryKey(input.mesh),
                        resultGeometryKey = part.fittedGeometryKey, vertices = part.mesh.vertexCount, triangles = part.mesh.triangles.Length / 3,
                        materialSlots = part.materials.Length, geometry = part.metrics, identityErrorMeters = error,
                        identityAssessed = spec.sourceMode == "canonical" };
                    RequireEqual(input.renderer.sharedMaterials, part.materials, "GARMENT_MATERIALS_CHANGED");
                    foreach (var material in part.materials)
                        foreach (string property in material.GetTexturePropertyNames())
                            if (material.GetTexture(property)) row.textureSlots++;
                    rows.Add(row);
                    if (!part.metrics.Passes) reasons.Add(part.name + " cần kiểm tra tiếp xúc hoặc tam giác sụp.");
                    if (part.metrics.maxRelativeEdgeChange > EdgeChangeLimit)
                        reasons.Add(part.name + " thay đổi cạnh vượt 5% so với bài gốc.");
                }
                report.parts = rows.ToArray();
                report.detailsPreserved = true;
                report.reviewReasons = reasons.ToArray();
                report.stationarySamplesPassed = package.stationarySamplesPassed && reasons.Count == 0;
                report.status = report.stationarySamplesPassed ? "STATIC_PREVIEW_READY" : "NEEDS_REVIEW";
                package.stationarySamplesPassed = report.stationarySamplesPassed;
                return new Result { package = package, report = report };
            }
            catch
            {
                if (package) Release(package);
                throw;
            }
            finally
            {
                foreach (var input in inputs) UnityEngine.Object.DestroyImmediate(input.mesh);
                if (neutral) UnityEngine.Object.DestroyImmediate(neutral);
                if (sourceBody) UnityEngine.Object.DestroyImmediate(sourceBody);
                UnityEngine.Object.DestroyImmediate(target.gameObject);
            }
        }

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        static void ValidateMesh(Mesh mesh, Renderer renderer)
        {
            if (mesh.vertexCount == 0 || mesh.triangles.Length == 0) throw new InvalidDataException("EMPTY_GARMENT_MESH " + renderer.name);
            if (mesh.normals.Length != mesh.vertexCount) throw new InvalidDataException("AUTHORED_NORMALS_MISSING " + renderer.name);
            foreach (var material in renderer.sharedMaterials)
                if (!material || !material.shader || !material.shader.isSupported)
                    throw new InvalidDataException("MATERIAL_OR_SHADER_UNSUPPORTED " + renderer.name);
            var points = mesh.vertices;
            foreach (var point in points) GarmentSurfaceTransfer.Finite(point);
            for (int sub = 0; sub < mesh.subMeshCount; sub++)
            {
                if (mesh.GetTopology(sub) != MeshTopology.Triangles) throw new InvalidDataException("TRIANGULATED_GARMENT_REQUIRED " + renderer.name);
                var faces = mesh.GetTriangles(sub);
                for (int i = 0; i < faces.Length; i += 3)
                {
                    var cross = Vector3.Cross(points[faces[i + 1]] - points[faces[i]], points[faces[i + 2]] - points[faces[i]]);
                    if (cross.sqrMagnitude < 1e-24f) throw new InvalidDataException("DEGENERATE_GARMENT_TRIANGLE " + renderer.name + " face " + (i / 3));
                }
            }
        }

        static float CheckDetails(Mesh source, Matrix4x4 matrix, Mesh result, bool identity)
        {
            if (source.vertexCount != result.vertexCount || source.subMeshCount != result.subMeshCount)
                throw new InvalidDataException("GARMENT_TOPOLOGY_CHANGED");
            for (int sub = 0; sub < source.subMeshCount; sub++)
                RequireEqual(source.GetIndices(sub), result.GetIndices(sub), "GARMENT_SUBMESH_CHANGED");
            for (int channel = 0; channel < 8; channel++)
            {
                var a = new List<Vector4>(); var b = new List<Vector4>();
                source.GetUVs(channel, a); result.GetUVs(channel, b);
                RequireEqual(a.ToArray(), b.ToArray(), "GARMENT_UV_CHANGED " + channel);
            }
            RequireEqual(source.colors, result.colors, "GARMENT_VERTEX_COLOURS_CHANGED");
            if (result.normals.Length != source.normals.Length || (source.tangents.Length > 0 && result.tangents.Length != source.tangents.Length))
                throw new InvalidDataException("GARMENT_SHADING_ATTRIBUTES_LOST");
            float error = 0;
            if (identity)
            {
                var a = source.vertices; var b = result.vertices;
                for (int i = 0; i < a.Length; i++) error = Mathf.Max(error, Vector3.Distance(matrix.MultiplyPoint3x4(a[i]), b[i]));
                if (error > .000001f) throw new InvalidDataException("CANONICAL_GARMENT_GEOMETRY_CHANGED");
            }
            return error;
        }

        static void RequireEqual<T>(T[] a, T[] b, string error)
        {
            if (a.Length != b.Length) throw new InvalidDataException(error);
            for (int i = 0; i < a.Length; i++) if (!EqualityComparer<T>.Default.Equals(a[i], b[i])) throw new InvalidDataException(error);
        }

        static void CheckEdgeContacts(Mesh mesh, GarmentSurfaceTransfer.Surface body, TransferMetrics metrics)
        {
            var points = mesh.vertices;
            var faces = mesh.triangles;
            var edges = new HashSet<ulong>();
            for (int i = 0; i < faces.Length; i += 3)
                for (int k = 0; k < 3; k++)
                {
                    int a = faces[i + k], b = faces[i + (k + 1) % 3];
                    ulong key = ((ulong)(uint)Math.Min(a, b) << 32) | (uint)Math.Max(a, b);
                    if (!edges.Add(key)) continue;
                    float clearance = body.LocalSignedClearance((points[a] + points[b]) * .5f);
                    metrics.contactSamples++;
                    metrics.maxLocalPenetration = Mathf.Max(metrics.maxLocalPenetration, -clearance);
                    if (clearance < -.001f) metrics.localPenetrationSamples++;
                }
        }

        public static string Save(Result result, StudentGarmentSpec spec)
        {
            string title = string.IsNullOrWhiteSpace(spec.displayName) ? "StudentGarment" : spec.displayName;
            foreach (char c in Path.GetInvalidFileNameChars()) title = title.Replace(c, '_');
            title = title.Trim().Trim('.');
            if (title.Length == 0) title = "StudentGarment";
            string path = AssetDatabase.GenerateUniqueAssetPath(OutputFolder + "/" + title);
            if (Directory.Exists(path)) throw new IOException("Submission destination already exists.");
            var materials = new Dictionary<Material, Material>();
            var textures = new Dictionary<Texture, Texture2D>();
            var textureFiles = new Dictionary<string, string>();
            try
            {
                Directory.CreateDirectory(path + "/Textures");
                AssetDatabase.Refresh();
                // Own a copy of each material and 2D texture, including embedded
                // glTF images. Removing an import staging folder then leaves the
                // reviewed package's appearance intact.
                foreach (var part in result.package.parts)
                    for (int i = 0; i < part.materials.Length; i++)
                    {
                        var original = part.materials[i];
                        if (!materials.TryGetValue(original, out var copy))
                        {
                            copy = new Material(original);
                            materials.Add(original, copy);
                            foreach (string property in original.GetTexturePropertyNames())
                            {
                                var source = original.GetTexture(property);
                                if (!source) continue;
                                if (!(source is Texture2D)) throw new InvalidDataException("PORTABLE_TEXTURE2D_REQUIRED " + source.name);
                                if (!textures.TryGetValue(source, out var texture))
                                {
                                    texture = CopyTexture((Texture2D)source, path, textureFiles);
                                    textures.Add(source, texture);
                                }
                                copy.SetTexture(property, texture);
                            }
                        }
                        part.materials[i] = copy;
                    }
                GarmentTransferStudio.Save(result.package, path);
                foreach (var texture in textures.Values)
                    if (!EditorUtility.IsPersistent(texture)) AssetDatabase.AddObjectToAsset(texture, result.package);
                foreach (var part in result.package.parts)
                    foreach (var material in part.materials) EditorUtility.SetDirty(material);
                EditorUtility.SetDirty(result.package);
                AssetDatabase.SaveAssets();
                File.WriteAllText(path + "/submission.json", JsonUtility.ToJson(spec, true));
                File.WriteAllText(path + "/submission-report.json", JsonUtility.ToJson(result.report, true));
                AssetDatabase.Refresh();
                return path;
            }
            catch
            {
                // Only this newly allocated submission folder can be rolled back.
                if (AssetDatabase.IsValidFolder(path)) AssetDatabase.DeleteAsset(path);
                throw;
            }
            finally
            {
                foreach (var material in materials.Values) if (material && !EditorUtility.IsPersistent(material)) UnityEngine.Object.DestroyImmediate(material);
                foreach (var texture in textures.Values) if (texture && !EditorUtility.IsPersistent(texture)) UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        static Texture2D CopyTexture(Texture2D source, string folder, Dictionary<string, string> files)
        {
            string sourcePath = AssetDatabase.GetAssetPath(source);
            if (!string.IsNullOrEmpty(sourcePath) && sourcePath.StartsWith("Assets/", StringComparison.Ordinal) && File.Exists(sourcePath))
            {
                if (!files.TryGetValue(sourcePath, out string destination))
                {
                    destination = folder + "/Textures/Source" + files.Count.ToString("00") + Path.GetExtension(sourcePath);
                    if (!AssetDatabase.CopyAsset(sourcePath, destination)) throw new IOException("TEXTURE_SOURCE_COPY_FAILED " + sourcePath);
                    files.Add(sourcePath, destination);
                    AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
                }
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(source, out string _, out long sourceId);
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(destination))
                    if (asset is Texture2D texture)
                    {
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(texture, out string _, out long copiedId);
                        if (copiedId == sourceId) return texture;
                    }
                throw new InvalidDataException("EMBEDDED_TEXTURE_ID_CHANGED " + sourcePath + " " + source.name);
            }
            if (!source.isReadable) throw new InvalidDataException("TEXTURE_SOURCE_ASSET_REQUIRED " + source.name);
            var copy = UnityEngine.Object.Instantiate(source);
            copy.name = source.name;
            return copy;
        }

        public static GameObject CreatePreview(Result result, StudentGarmentSpec spec)
        {
            string prefab = GarmentBodyReference.Export(spec.targetBodyId);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefab));
            root.name = result.report.status + " " + spec.displayName;
            var branch = root.transform.Find("Garment");
            foreach (var part in result.package.parts)
            {
                var child = new GameObject(part.name);
                child.transform.SetParent(branch, false);
                child.AddComponent<MeshFilter>().sharedMesh = part.mesh;
                child.AddComponent<MeshRenderer>().sharedMaterials = part.materials;
            }
            return root;
        }

        public static void Release(TransferredGarmentAsset package)
        {
            if (!package || EditorUtility.IsPersistent(package)) return;
            foreach (var part in package.parts) if (part.mesh && !EditorUtility.IsPersistent(part.mesh)) UnityEngine.Object.DestroyImmediate(part.mesh);
            UnityEngine.Object.DestroyImmediate(package);
        }
    }
}
#endif
