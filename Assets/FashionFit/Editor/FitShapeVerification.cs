#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FashionFit.Editor
{
    public static class FitShapeVerification
    {
        static string output;
        static FashionFitApp app;
        public static void PrepareRelease()
        {
            PlayerSettings.bundleVersion = "0.9.2";
            foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new[] { "Assets/Resources/ContinuousShirts" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/V4_CloseFit_") || path.Contains("/V5_CloseFit_") || path.Contains("/V6_CloseFit_")) AssetDatabase.DeleteAsset(path);
            }
            AssetDatabase.SaveAssets();
            EditorApplication.Exit(0);
        }
        public static void RunBatch()
        {
            GameObject root = null;
            try
            {
                output = StudioTelemetry.Argument("-fashionFitOutput") ?? "Verification/FitShape";
                output = Path.GetFullPath(output); Directory.CreateDirectory(output);
                root = new GameObject("Clothing fit verification");
                app = root.AddComponent<FashionFitApp>();
                typeof(FashionFitApp).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(app, null);
                if (!app.Ready) throw new InvalidOperationException(app.FatalError);
                app.PoseStudio.SetHandles(false);
                foreach (var renderer in app.Showroom.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
                Capture(0, 5, "FemaleIvory");
                Capture(1, 6, "MaleDefaultPolo");
                Capture(1, 7, "MaleTee");
                Capture(1, 8, "MalePolo");
                Capture(1, 9, "MaleRugby");
                Capture(1, 10, "MaleHenley");
                Capture(1, 11, "MaleOvershirt");
                File.WriteAllText(Path.Combine(output, "verification.json"), "{\"status\":\"passed\",\"checks\":7,\"exceptions\":0}");
                UnityEngine.Object.DestroyImmediate(root);
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                File.WriteAllText(Path.Combine(output, "failure.txt"), error.ToString());
                if (root) UnityEngine.Object.DestroyImmediate(root);
                EditorApplication.Exit(1);
            }
        }
        static void Capture(int gender, int look, string label)
        {
            foreach(var rig in app.PoseStudio.Rigs)foreach(var renderer in rig.GetComponentsInChildren<Renderer>())renderer.enabled=rig==app.PoseStudio.Rigs[gender];
            app.PoseStudio.ResetPose(gender);
            app.PoseStudio.Wear(gender, look);
            var wear = app.PoseStudio.Wearables[gender];
            var avatar = wear.Rig.Avatar;
            wear.Step(1f / 30);
            var asset = MensReferenceCollection.Contains(look) ? MensReferenceCollection.Asset(look)
                : Resources.Load<PoseOutfitAsset>("PoseWardrobe/" + (gender == 0 ? "Female" : "Male") + look.ToString("00"));
            int submesh = look == 5 ? 1 : 0;
            var baked = new Mesh();
            var old = avatar.Renderer.sharedMesh;
            avatar.Renderer.sharedMesh = avatar.BodyMesh;
            avatar.Renderer.BakeMesh(baked);
            avatar.Renderer.sharedMesh = old;
            using (var writer = new BinaryWriter(File.Create(Path.Combine(output, label + ".bin"))))
            {
                writer.Write(0x46495432);
                Write(writer, avatar.BodyVertices, avatar.BodyMesh.triangles);
                Write(writer, asset.mesh.vertices, asset.mesh.GetTriangles(submesh));
                Write(writer, baked.vertices, baked.triangles);
                Write(writer, wear.Surface.CurrentPoints, wear.Surface.MeshAt(0).GetTriangles(submesh));
            }
            if (gender == 1 && look >= 6)
            {
                float detailMotion = 0;
                var rest = asset.mesh.vertices;
                foreach (int index in asset.mesh.GetTriangles(2))
                    detailMotion = Mathf.Max(detailMotion, Vector3.Distance(rest[index], wear.Surface.CurrentPoints[index]));
                File.WriteAllText(Path.Combine(output, label + "_details.txt"), "maxNeutralDetailMotion " + detailMotion);
                if (detailMotion > .04f) throw new InvalidOperationException(label + " collar correction exceeds 40 mm: " + detailMotion);
            }
            int unsorted = 0;
            foreach (var weight in avatar.BodyMesh.boneWeights)
                if (weight.weight1 > weight.weight0 || weight.weight2 > weight.weight0 || weight.weight3 > weight.weight0) unsorted++;
            File.WriteAllText(Path.Combine(output, label + "_weights.txt"), "nonDominantIndex0 " + unsorted + " / " + avatar.BodyVertices.Length);
            CaptureImage(avatar, label, new Vector3(.55f, 1.1f, 1.15f), 1.0f);
            CaptureImage(avatar, label + "_Side", new Vector3(.95f, 1.1f, .01f), 1.0f);
            UnityEngine.Object.DestroyImmediate(baked);
        }
        static void Write(BinaryWriter writer, Vector3[] vertices, int[] triangles)
        {
            writer.Write(vertices.Length);
            foreach (var p in vertices) { writer.Write(p.x); writer.Write(p.y); writer.Write(p.z); }
            writer.Write(triangles.Length); foreach (int index in triangles) writer.Write(index);
        }
        static void CaptureImage(RunwayAvatar avatar, string label, Vector3 offset, float height)
        {
            var camera = app.Player.View;
            camera.rect = new Rect(0, 0, 1, 1);
            camera.orthographic = true; camera.orthographicSize = 1.05f;
            camera.transform.position = avatar.transform.TransformPoint(offset);
            camera.transform.LookAt(avatar.transform.TransformPoint(new Vector3(0, height, 0)));
            var target = new RenderTexture(1000, 1100, 24);
            var texture = new Texture2D(1000, 1100, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                texture.ReadPixels(new Rect(0, 0, 1000, 1100), 0, 0); texture.Apply();
                File.WriteAllBytes(Path.Combine(output, label + ".png"), texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
#endif
