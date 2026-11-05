#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FashionFit.Editor
{
    public static class ContactPoseVerification
    {
        [Serializable] sealed class Result
        {
            public string status = "passed", scope = "All pose handles via actual near-grip acquisition and controller drag, injected input, no HMD";
            public int checks, joints, exceptions;
        }
        public static void RunBatch()
        {
            var result = new Result(); GameObject root = null;
            string folder = Path.GetFullPath("Verification/ContactPose");
            Directory.CreateDirectory(folder);
            File.Delete(folder + "/failure.txt");
            Application.LogCallback logger = (m, s, t) => { if (t == LogType.Error || t == LogType.Exception) result.exceptions++; };
            Application.logMessageReceived += logger; int exit = 0;
            try
            {
                root = new GameObject("Pose handle verification");
                var app = root.AddComponent<FashionFitApp>();
                typeof(FashionFitApp).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(app, null);
                if (!app.Ready) throw new Exception(app.FatalError);
                app.Player.PrepareForVRTrial(); app.Player.EnsureHands();
                app.PoseStudio.SetHandles(true);
                foreach (var rig in app.PoseStudio.Rigs)
                    foreach (var handle in rig.Handles)
                    {
                        rig.ResetPose();
                        Physics.SyncTransforms();
                        var hands = app.Player.Hands;
                        if (!hands.TryBeginNearGrip(0, handle.transform.position, Quaternion.identity))
                            throw new Exception("Cannot acquire " + rig.Label + "/" + handle.Label);
                        result.checks++;
                        if (hands.HeldBy(0).gameObject != handle.gameObject)
                            throw new Exception("Wrong target at " + rig.Label + "/" + handle.Label + " picked " + hands.HeldBy(0).name);
                        var beforePosition = handle.Bone.position;
                        var beforeRotation = handle.Bone.rotation;
                        Vector3 target = handle.transform.position + new Vector3(.01f, .02f, .015f);
                        hands.Step(0, true, handle.transform.position, Quaternion.identity, 1, 0, 1f / 60);
                        for (int i = 0; i < 100; i++)
                            hands.Step(0, true, target, Quaternion.Euler(12, 8, 3), 1, 0, 1f / 60);
                        if (Quaternion.Angle(beforeRotation, handle.Bone.rotation) < .1f && Vector3.Distance(beforePosition, handle.Bone.position) < .001f)
                            for (int i = 0; i < 100; i++)
                                hands.Step(0, true, handle.transform.position - new Vector3(.01f, .02f, .015f), Quaternion.Euler(-12, -8, -3), 1, 0, 1f / 60);
                        // Acquisition is checked independently of rotation limits.
                        // For IK hands/feet, direct controller delta also proves translation.
                        if (Quaternion.Angle(beforeRotation, handle.Bone.rotation) < .1f && Vector3.Distance(beforePosition, handle.Bone.position) < .001f)
                            throw new Exception("No joint response for " + rig.Label + "/" + handle.Label);
                        result.checks++; result.joints++;
                        hands.ReleaseAll(false);
                    }
                if (result.exceptions != 0) throw new Exception("Unity exceptions");
            }
            catch (Exception error) { result.status = "failed"; File.WriteAllText(folder + "/failure.txt", error.ToString()); exit = 1; }
            finally
            {
                if (root) UnityEngine.Object.DestroyImmediate(root);
                Application.logMessageReceived -= logger;
                File.WriteAllText(folder + "/verification.json", JsonUtility.ToJson(result, true));
            }
            EditorApplication.Exit(exit);
        }
    }
}
#endif
