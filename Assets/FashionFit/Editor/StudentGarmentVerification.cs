#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FashionFit.Editor
{
    public static class StudentGarmentVerification
    {
        [Serializable] sealed class Case
        {
            public string name, expected, actual;
            public StudentGarmentPipeline.Report report;
        }
        [Serializable] sealed class Evidence
        {
            public string status = "RUNNING", scope = "Authored fixtures and current project garments; no real student submissions supplied.";
            public int checks, exceptions, contactExports;
            public int texturePairs;
            public float maxTextureSampleError;
            public Case[] cases;
            public string failure;
        }
        static Evidence evidence;
        static readonly List<Case> cases = new List<Case>();
        static string output;
        static void Check(bool condition, string message)
        {
            evidence.checks++;
            if (!condition) throw new InvalidOperationException(message);
        }

        public static void RunBatch()
        {
            output = StudioTelemetry.Argument("-fashionSubmissionQA");
            if (string.IsNullOrEmpty(output) || !Path.IsPathRooted(output)) throw new InvalidDataException("Explicit absolute QA output required.");
            Directory.CreateDirectory(output);
            Directory.CreateDirectory(Path.Combine(output, "Contact"));
            evidence = new Evidence();
            cases.Clear();
            GameObject appRoot = null;
            try
            {
                appRoot = new GameObject("Student garment verification");
                var app = appRoot.AddComponent<FashionFitApp>();
                typeof(FashionFitApp).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(app, null);
                Check(app.Ready && !app.HideCoveredBody, "Need full body in live project.");
                app.SetLook(6);
                app.StandForFitting();
                for (int i = 0; i < 180; i++) app.AdvanceSimulation(1f / 60);
                foreach (string id in GarmentBodyReference.Ids)
                {
                    string prefab = GarmentBodyReference.Export(id);
                    Check(AssetDatabase.LoadAssetAtPath<GameObject>(prefab) != null, "Reference prefab missing " + id);
                    var frame = MakeFrame(app, id);
                    try
                    {
                        var spec = ReadReference(id);
                        Ready(frame, spec, id + "_Identity", true);
                        if (id == "fitting-female") MainVariants(frame, spec);
                        else
                        {
                            SplitAndReorderBody(frame);
                            Ready(frame, spec, id + "_UVSplit", false);
                        }
                    }
                    finally { DestroyFrame(frame); }
                }
                Formats();
                evidence.status = "passed";
                SaveEvidence();
                UnityEngine.Object.DestroyImmediate(appRoot);
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                evidence.status = "failed";
                evidence.exceptions++;
                evidence.failure = error.ToString();
                SaveEvidence();
                Debug.LogException(error);
                if (appRoot) UnityEngine.Object.DestroyImmediate(appRoot);
                EditorApplication.Exit(1);
            }
        }

        static StudentGarmentSpec ReadReference(string id) => StudentGarmentPipeline.ReadSpec("Samples/StudentGarments/" + id + "/submission.json");

        static GameObject MakeFrame(FashionFitApp app, string id)
        {
            RunwayAvatar avatar;
            Mesh garment = null;
            Material[] materials = null;
            if (id == "fitting-female") avatar = app.Model;
            else
            {
                int gender = id == "pose-female" ? 0 : 1;
                app.PoseStudio.ResetPose(gender);
                app.PoseStudio.Wear(gender, gender == 0 ? 5 : 8);
                var wear = app.PoseStudio.Wearables[gender];
                wear.Step(1f / 30);
                avatar = wear.Rig.Avatar;
                garment = UnityEngine.Object.Instantiate(wear.Surface.MeshAt(0));
                garment.vertices = wear.Surface.CurrentPoints;
                materials = wear.Surface.GetComponent<MeshRenderer>().sharedMaterials;
            }
            var frame = new GameObject(id + " submission");
            var body = new GameObject("SourceBody");
            body.transform.SetParent(frame.transform, false);
            body.AddComponent<MeshFilter>().sharedMesh = GarmentTransferStudio.Snapshot(avatar.Renderer);
            body.AddComponent<MeshRenderer>().sharedMaterial = avatar.Original;
            var branch = new GameObject("Garment");
            branch.transform.SetParent(frame.transform, false);
            if (garment) AddPart(branch, "Outfit", garment, materials);
            else
                foreach (var renderer in avatar.GetComponentsInChildren<Renderer>())
                    if (renderer != avatar.Renderer && (renderer is MeshRenderer || renderer is SkinnedMeshRenderer))
                        AddPart(branch, renderer.name, GarmentTransferStudio.Snapshot(renderer), renderer.sharedMaterials);
            return frame;
        }

        static void AddPart(GameObject branch, string name, Mesh mesh, Material[] materials)
        {
            // Extra non-empty UV channels and colours catch attribute loss.
            var colors = new Color32[mesh.vertexCount];
            var secondary = new List<Vector4>();
            for (int i = 0; i < mesh.vertexCount; i++)
            {
                colors[i] = new Color32((byte)(i % 255), 127, 223, 255);
                secondary.Add(new Vector4(i % 31 / 31f, i % 13 / 13f, .25f, .75f));
            }
            mesh.colors32 = colors;
            mesh.SetUVs(7, secondary);
            var part = new GameObject(name);
            part.transform.SetParent(branch.transform, false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.AddComponent<MeshRenderer>().sharedMaterials = materials;
        }

        static void Ready(GameObject frame, StudentGarmentSpec spec, string name, bool photo)
        {
            var original = new Dictionary<Mesh, string>();
            foreach (var filter in frame.GetComponentsInChildren<MeshFilter>()) original.Add(filter.sharedMesh, TailoredPoloStudy.GeometryKey(filter.sharedMesh));
            var result = StudentGarmentPipeline.Process(frame, spec);
            try
            {
                cases.Add(new Case { name = name, expected = "STATIC_PREVIEW_READY", actual = result.report.status, report = result.report });
                Check(result.report.status == "STATIC_PREVIEW_READY", name + " failed static gate " + JsonUtility.ToJson(result.report));
                Check(result.report.detailsPreserved && !result.report.motionAssessed, "Incorrect review scope " + name);
                foreach (var row in result.report.parts)
                {
                    Check(row.geometry.localPenetrationSamples == 0, "Contact failure " + name);
                    if (row.identityAssessed) Check(row.identityErrorMeters <= .000001f && row.geometry.maxRelativeEdgeChange == 0, "Identity altered detail " + name);
                }
                foreach (var pair in original) Check(TailoredPoloStudy.GeometryKey(pair.Key) == pair.Value, "Source mutated " + name);
                Export(spec.targetBodyId, result.package, name);
                if (photo) Capture(spec.targetBodyId, result.package, name);
                if (name == "fitting-female_Identity" || name == "Native_glb") Persistence(result, spec);
            }
            finally { StudentGarmentPipeline.Release(result.package); }
        }

        static void MainVariants(GameObject frame, StudentGarmentSpec spec)
        {
            string key = spec.referenceBodyKey;
            spec.referenceBodyKey = "stale-reference";
            Reject(frame, spec, "StaleReference", "BODY_REFERENCE_VERSION_MISMATCH");
            spec.referenceBodyKey = key;
            var body = frame.transform.Find("SourceBody").GetComponent<MeshFilter>().sharedMesh;
            var points = body.vertices;
            var changed = (Vector3[])points.Clone(); changed[0] += Vector3.right * .01f; body.vertices = changed;
            Reject(frame, spec, "WrongBodyShape", "BODY_REFERENCE_MISMATCH"); body.vertices = points;
            var faces = body.triangles; var inverted = (int[])faces.Clone(); int swap = inverted[0]; inverted[0] = inverted[1]; inverted[1] = swap; body.triangles = inverted;
            Reject(frame, spec, "WrongBodyWinding", "BODY_TRIANGLES_CHANGED"); body.triangles = faces;
            var renderer = frame.transform.Find("Garment").GetComponentInChildren<MeshRenderer>();
            var slots = renderer.sharedMaterials; var missing = (Material[])slots.Clone(); missing[0] = null; renderer.sharedMaterials = missing;
            Reject(frame, spec, "MissingMaterial", "MATERIAL_OR_SHADER_UNSUPPORTED"); renderer.sharedMaterials = slots;
            var part = renderer.transform; part.localPosition = Vector3.right * 10;
            Reject(frame, spec, "DistantGarment", "over 50 cm"); part.localPosition = Vector3.zero;
            renderer.enabled = false; Reject(frame, spec, "HiddenGarmentPart", "HIDDEN_GARMENT_PART"); renderer.enabled = true;
            spec.unitMeters = 100; Reject(frame, spec, "WrongUnits", "SOURCE_UNIT_INVALID"); spec.unitMeters = 1;
            spec.targetBodyId = "pose-male"; Reject(frame, spec, "WrongBodyId", "BODY_REFERENCE_VERSION_MISMATCH"); spec.targetBodyId = "fitting-female";

            frame.transform.SetPositionAndRotation(new Vector3(3, 2, -4), Quaternion.Euler(0, 37, 0));
            frame.transform.localScale = Vector3.one * 1.2f;
            Ready(frame, spec, "FittingMovedSceneRoot", false);
            frame.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); frame.transform.localScale = Vector3.one;
            foreach (Transform child in frame.transform) child.localScale = Vector3.one * 100;
            spec.unitMeters = .01f;
            Ready(frame, spec, "FittingCentimetres", false);
            foreach (Transform child in frame.transform) child.localScale = Vector3.one;
            spec.unitMeters = 1;

            spec.sourceMode = "paired-body";
            BodyScale(frame, new Vector3(.98f, 1, .98f));
            Ready(frame, spec, "RegisteredBodyTwoPercent", false);
            BodyScale(frame, new Vector3(.90f / .98f, 1, .90f / .98f));
            var review = StudentGarmentPipeline.Process(frame, spec);
            try
            {
                cases.Add(new Case { name = "RegisteredBodyTenPercent", expected = "NEEDS_REVIEW", actual = review.report.status, report = review.report });
                Check(review.report.status == "NEEDS_REVIEW" && !review.package.CanPreview, "Large strain was silently accepted.");
            }
            finally { StudentGarmentPipeline.Release(review.package); }
            BodyScale(frame, new Vector3(1 / .9f, 1, 1 / .9f));
            spec.sourceMode = "canonical";
            SplitAndReorderBody(frame);
            Ready(frame, spec, "FittingUVSplitAndReorder", false);
        }

        static void BodyScale(GameObject frame, Vector3 scale)
        {
            var body = frame.transform.Find("SourceBody").GetComponent<MeshFilter>().sharedMesh;
            var points = body.vertices;
            for (int i = 0; i < points.Length; i++) points[i] = Vector3.Scale(points[i], scale);
            body.vertices = points;
            frame.transform.Find("Garment").localScale = Vector3.Scale(frame.transform.Find("Garment").localScale, scale);
        }

        static void SplitAndReorderBody(GameObject frame)
        {
            var filter = frame.transform.Find("SourceBody").GetComponent<MeshFilter>();
            var old = filter.sharedMesh; var points = old.vertices; var faces = old.triangles;
            var split = new Vector3[faces.Length]; var indices = new int[faces.Length];
            for (int i = 0; i < faces.Length; i++) { split[faces.Length - 1 - i] = points[faces[i]]; indices[i] = faces.Length - 1 - i; }
            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32, vertices = split, triangles = indices };
            mesh.RecalculateNormals(); filter.sharedMesh = mesh; UnityEngine.Object.DestroyImmediate(old);
        }

        static void Reject(GameObject frame, StudentGarmentSpec spec, string name, string expectedReason)
        {
            string actual = "ACCEPTED";
            StudentGarmentPipeline.Result result = null;
            try { result = StudentGarmentPipeline.Process(frame, spec); }
            catch (InvalidDataException error) { actual = error.Message; }
            finally { if (result != null) StudentGarmentPipeline.Release(result.package); }
            cases.Add(new Case { name = name, expected = expectedReason, actual = actual });
            Check(actual.Contains(expectedReason), name + " rejection mismatch " + actual);
        }

        static void Persistence(StudentGarmentPipeline.Result result, StudentGarmentSpec spec)
        {
            var samples = new Dictionary<string, Color[]>();
            var signatures = new Dictionary<string, string>();
            for (int p = 0; p < result.package.parts.Length; p++)
                for (int m = 0; m < result.package.parts[p].materials.Length; m++)
                {
                    var material = result.package.parts[p].materials[m];
                    foreach (string property in material.GetTexturePropertyNames())
                        if (material.GetTexture(property) is Texture2D texture)
                        {
                            string key = p + "/" + m + "/" + property;
                            samples.Add(key, TextureSamples(texture));
                            signatures.Add(key, TextureSignature(texture));
                        }
                }
            spec.displayName = "Submission_QA_" + Guid.NewGuid().ToString("N");
            string folder = StudentGarmentPipeline.Save(result, spec);
            try
            {
                AssetDatabase.ImportAsset(folder + "/Garment.asset", ImportAssetOptions.ForceSynchronousImport);
                var asset = AssetDatabase.LoadAssetAtPath<TransferredGarmentAsset>(folder + "/Garment.asset");
                var target = GarmentBodyReference.Load(spec.targetBodyId);
                try { Check(asset && asset.Check(target) == null && asset.CheckNeutralRig(target) == null, "Persisted package cannot be previewed."); }
                finally { UnityEngine.Object.DestroyImmediate(target.gameObject); }
                for (int p = 0; p < asset.parts.Length; p++)
                    for (int m = 0; m < asset.parts[p].materials.Length; m++)
                    {
                        var material = asset.parts[p].materials[m];
                        Check(AssetDatabase.GetAssetPath(material) == folder + "/Garment.asset", "Material still depends on import staging.");
                        foreach (string property in material.GetTexturePropertyNames())
                        {
                            var texture = material.GetTexture(property);
                            string key = p + "/" + m + "/" + property;
                            if (!samples.TryGetValue(key, out var before)) continue;
                            Check(texture && AssetDatabase.GetAssetPath(texture).StartsWith(folder + "/", StringComparison.Ordinal), "Texture still depends on import staging or was lost.");
                            var after = TextureSamples((Texture2D)texture);
                            Check(signatures[key] == TextureSignature((Texture2D)texture), "Texture format/sampling changed.");
                            float error = 0;
                            for (int i = 0; i < before.Length; i++) error = Mathf.Max(error, ((Vector4)before[i] - (Vector4)after[i]).magnitude);
                            evidence.texturePairs++;
                            evidence.maxTextureSampleError = Mathf.Max(evidence.maxTextureSampleError, error);
                            Check(error < .000001f, "Texture GPU sample mismatch " + key + " " + error);
                        }
                    }
                var preview = StudentGarmentPipeline.CreatePreview(result, spec);
                try
                {
                    Check(preview.transform.Find("Garment").childCount == asset.parts.Length, "Scene preview lost garment pieces.");
                    foreach (var filter in preview.transform.Find("Garment").GetComponentsInChildren<MeshFilter>())
                        Check(EditorUtility.IsPersistent(filter.sharedMesh), "Preview references a temporary mesh.");
                }
                finally { UnityEngine.Object.DestroyImmediate(preview); }
                Check(File.Exists(folder + "/submission-report.json"), "Submission report missing.");
            }
            finally { AssetDatabase.DeleteAsset(folder); }
            spec.displayName = "Student garment fitting-female";
        }

        static string TextureSignature(Texture2D texture) => texture.width + "/" + texture.height + "/" + texture.mipmapCount
            + "/" + texture.graphicsFormat + "/" + texture.filterMode + "/" + texture.wrapModeU + "/" + texture.wrapModeV + "/" + texture.anisoLevel;

        static Color[] TextureSamples(Texture2D source)
        {
            var rt = RenderTexture.GetTemporary(32, 32, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            var image = new Texture2D(32, 32, TextureFormat.RGBAFloat, false, true);
            var previous = RenderTexture.active;
            try
            {
                Graphics.Blit(source, rt); RenderTexture.active = rt;
                image.ReadPixels(new Rect(0, 0, 32, 32), 0, 0); image.Apply();
                return image.GetPixels();
            }
            finally { RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); UnityEngine.Object.DestroyImmediate(image); }
        }

        static void Formats()
        {
            foreach (string extension in new[] { "fbx", "obj", "glb" })
            {
                string path = "Assets/FashionFit/TransferSamples/DetailFixture." + extension;
                byte[] before = File.ReadAllBytes(path);
                if (extension != "glb") GarmentImportPreparation.Prepare(path);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Check(prefab, "Format fixture missing " + extension);
                var frame = new GameObject("Format submission " + extension);
                var target = GarmentBodyReference.Load("fitting-female");
                try
                {
                    var body = new GameObject("SourceBody"); body.transform.SetParent(frame.transform, false);
                    body.AddComponent<MeshFilter>().sharedMesh = GarmentTransferStudio.Snapshot(target.Renderer);
                    body.AddComponent<MeshRenderer>().sharedMaterial = target.Original;
                    var garment = new GameObject("Garment"); garment.transform.SetParent(frame.transform, false);
                    var source = (GameObject)PrefabUtility.InstantiatePrefab(prefab); source.transform.SetParent(garment.transform, false);
                    var bounds = new Bounds(); bool first = true;
                    foreach (var renderer in source.GetComponentsInChildren<Renderer>())
                    {
                        var mesh = GarmentTransferStudio.Snapshot(renderer);
                        foreach (var point in mesh.vertices)
                        {
                            Vector3 p = renderer.transform.TransformPoint(point);
                            if (first) { bounds = new Bounds(p, Vector3.zero); first = false; } else bounds.Encapsulate(p);
                        }
                        UnityEngine.Object.DestroyImmediate(mesh);
                    }
                    garment.transform.localPosition = new Vector3(0, 1.15f, .30f) - bounds.center;
                    Ready(frame, ReadReference("fitting-female"), "Native_" + extension, false);
                    Check(Convert.ToBase64String(before) == Convert.ToBase64String(File.ReadAllBytes(path)), "Native format source changed.");
                }
                finally
                {
                    // Imported fixture meshes are assets and remain owned by the importer.
                    var filter = frame.transform.Find("SourceBody").GetComponent<MeshFilter>();
                    UnityEngine.Object.DestroyImmediate(filter.sharedMesh);
                    UnityEngine.Object.DestroyImmediate(frame);
                    UnityEngine.Object.DestroyImmediate(target.gameObject);
                }
            }
        }

        static void Export(string id, TransferredGarmentAsset package, string name)
        {
            var target = GarmentBodyReference.Load(id);
            var body = GarmentTransferStudio.Snapshot(target.Renderer);
            try
            {
                var points = new List<Vector3>(); var faces = new List<int>();
                foreach (var part in package.parts)
                {
                    int offset = points.Count; points.AddRange(part.mesh.vertices);
                    foreach (int index in part.mesh.triangles) faces.Add(offset + index);
                }
                using (var writer = new BinaryWriter(File.Create(Path.Combine(output, "Contact", name + ".bin"))))
                {
                    writer.Write(0x53434631); Write(writer, body.vertices, body.triangles); Write(writer, points.ToArray(), faces.ToArray());
                }
                evidence.contactExports++;
            }
            finally { UnityEngine.Object.DestroyImmediate(body); UnityEngine.Object.DestroyImmediate(target.gameObject); }
        }

        static void Write(BinaryWriter writer, Vector3[] points, int[] faces)
        {
            writer.Write(points.Length); foreach (var point in points) { writer.Write(point.x); writer.Write(point.y); writer.Write(point.z); }
            writer.Write(faces.Length); foreach (int index in faces) writer.Write(index);
        }

        static void Capture(string id, TransferredGarmentAsset package, string name)
        {
            var target = GarmentBodyReference.Load(id);
            target.transform.position = Vector3.right * 20;
            foreach (var part in package.parts)
            {
                var child = new GameObject(part.name); child.transform.SetParent(target.transform, false);
                child.AddComponent<MeshFilter>().sharedMesh = part.mesh;
                child.AddComponent<MeshRenderer>().sharedMaterials = part.materials;
            }
            var cameraRoot = new GameObject("Submission review camera");
            var camera = cameraRoot.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.18f, .18f, .19f);
            camera.transform.position = target.transform.position + new Vector3(.60f, 1.18f, 2.2f);
            camera.transform.LookAt(target.transform.position + Vector3.up * 1.0f); camera.fieldOfView = 48;
            var texture = new Texture2D(1000, 1100, TextureFormat.RGB24, false); var rt = new RenderTexture(1000, 1100, 24); var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                texture.ReadPixels(new Rect(0, 0, 1000, 1100), 0, 0); texture.Apply();
                File.WriteAllBytes(Path.Combine(output, name + ".png"), texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(texture);
                UnityEngine.Object.DestroyImmediate(cameraRoot); UnityEngine.Object.DestroyImmediate(target.gameObject);
            }
        }

        static void DestroyFrame(GameObject frame)
        {
            foreach (var filter in frame.GetComponentsInChildren<MeshFilter>())
                if (!EditorUtility.IsPersistent(filter.sharedMesh)) UnityEngine.Object.DestroyImmediate(filter.sharedMesh);
            UnityEngine.Object.DestroyImmediate(frame);
        }

        static void SaveEvidence()
        {
            evidence.cases = cases.ToArray();
            File.WriteAllText(Path.Combine(output, "verification.json"), JsonUtility.ToJson(evidence, true));
        }
    }
}
#endif
