using System;
using System.IO;
using System.Collections;
using UnityEngine;

namespace FashionFit
{
    // Explicit test flag only; normal player input is never replaced in play.
    public sealed class StudioRenderedTrial : MonoBehaviour
    {
        [Serializable] sealed class Result
        {
            public string status = "passed", scope = "Ivory female + red top hat; actual rendered frames/automatic physics, injected near-grip input; no physical HMD";
            public int checks, exceptions,clothFrames,clothSolverSteps,poseFrames,poseSolverSteps;
            public float shirtTravelM, shirtRecoveredM, jointTravelM, hatExtensionM, hatCompressionM, hatDropM;
            public bool twoHands, balancedForces, firstReleaseRetainsSecond, headDetach, gravity;
        }
        FashionFitApp app; RunwayVRHands hands; string folder; Result result = new Result();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            string p = StudioTelemetry.Argument("-fashionStudioTrial"); if (p == null) return;
            var t = new GameObject("Explicit rendered studio trial").AddComponent<StudioRenderedTrial>(); t.folder = Path.GetFullPath(p);
        }
        void Check(bool condition, string message) { result.checks++; if (!condition) throw new InvalidOperationException(message); }
        void Log(string m, string s, LogType type) { if (type == LogType.Error || type == LogType.Exception) result.exceptions++; }
        IEnumerator Start()
        {
            Application.runInBackground = true; Directory.CreateDirectory(folder); Application.logMessageReceived += Log;
            var run = Scenario(); int code = 0;
            while (true)
            {
                object next = null; bool more = false; Exception error = null;
                try { more = run.MoveNext(); if (more) next = run.Current; } catch (Exception e) { error = e; }
                if (error != null) { result.status = "failed"; File.WriteAllText(Path.Combine(folder, "failure.txt"), error.ToString()); code = 1; break; }
                if (!more) break; yield return next;
            }
            hands?.ReleaseAll(false); StudioTelemetry.Active?.Finish();
            File.WriteAllText(Path.Combine(folder, "trial.json"), JsonUtility.ToJson(result, true));
            Application.Quit(code);
        }
        void Phase(string name) { StudioTelemetry.Active?.SetPhase(name); }
        IEnumerator Scenario()
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 45;
            while (!app || !app.Ready) { app = FindObjectOfType<FashionFitApp>(); if (Time.realtimeSinceStartupAsDouble > deadline) throw new Exception("Startup timeout"); yield return null; }
            app.Player.PrepareForVRTrial(); app.Player.EnsureHands(); hands = app.Player.Hands;
            app.Player.enabled = false; // prevents physical or desktop input mixing with the explicit test
            if (app.Player.VRMenu) app.Player.VRMenu.Hide(); app.PoseStudio.SetHandles(false);
            Check(Physics.simulationMode == SimulationMode.FixedUpdate, "Automatic PhysX not enabled");
            app.Showroom.SelectOutfitTarget(1); app.PoseStudio.Wear(0, 5);
            var wear = app.PoseStudio.Wearables[0]; var surface = wear.Surface; surface.SetMaterial(MembraneMaterial.Presets[0]);
            var c = app.Player.View; c.rect = new Rect(0, 0, 1, 1); c.transform.position = wear.Rig.Avatar.transform.TransformPoint(new Vector3(.7f, 1.4f, 2.1f));
            c.transform.LookAt(wear.Rig.Avatar.transform.TransformPoint(new Vector3(0, 1.1f, 0)));
            Phase("ivory_neutral"); yield return new WaitForSecondsRealtime(3);
            var points = surface.MeshAt(0).vertices; int index = Closest(points, new Vector3(-.10f, 1.28f, .23f)); var initial = points[index];
            Vector3 p = surface.transform.TransformPoint(initial); Grip(0, p);
            Check(hands.HeldBy(0) && hands.HeldBy(0).Elastic == surface, "Ivory near Grip failed");
            uint clothVersion=wear.SolverVersion;Phase("ivory_pull"); double end = Time.realtimeSinceStartupAsDouble + 1.5;
            while (Time.realtimeSinceStartupAsDouble < end) { Tick(0, p + Vector3.forward * .30f); yield return null;result.clothFrames++; }
            result.clothSolverSteps=(int)(wear.SolverVersion-clothVersion);Check(result.clothSolverSteps>=result.clothFrames-1&&result.clothFrames>10,"Active cloth still waits for 30 Hz tick");
            result.shirtTravelM = Vector3.Distance(initial, surface.MeshAt(0).vertices[index]);
            Check(result.shirtTravelM > .02f && result.shirtTravelM < .075f, "Ivory pull response outside trial tolerance");
            hands.ReleaseAll(false); Phase("ivory_recovery"); yield return new WaitForSecondsRealtime(3.2f);
            result.shirtRecoveredM = Vector3.Distance(initial, surface.MeshAt(0).vertices[index]);
            Check(result.shirtRecoveredM < result.shirtTravelM * .6f, "Ivory recovery failed in automatic frames");
            Phase("ivory_pose"); app.PoseStudio.SetHandles(true); RunwayPoseHandle handle = null;
            foreach (var h in wear.Rig.Handles) if (h.Bone.name == "LeftHand") { handle = h; break; }
            Check(handle, "Missing wrist handle"); Vector3 joint = handle.Bone.position; p = handle.transform.position; Grip(0, p);
            Check(hands.HeldBy(0) && hands.HeldBy(0).IsJoint, "Pose mode did not acquire joint");
            uint poseVersion=wear.SolverVersion;end = Time.realtimeSinceStartupAsDouble + 1;
            while (Time.realtimeSinceStartupAsDouble < end) { Tick(0, p + Vector3.up * .12f); yield return null;result.poseFrames++; }
            result.poseSolverSteps=(int)(wear.SolverVersion-poseVersion);Check(result.poseSolverSteps>=result.poseFrames-1&&result.poseFrames>10,"Active joint still waits for 30 Hz tick");
            result.jointTravelM = Vector3.Distance(joint, handle.Bone.position); Check(result.jointTravelM > .03f, "Pose joint did not move");
            hands.ReleaseAll(false); app.PoseStudio.ResetPose(0); app.PoseStudio.SetHandles(false);

            var hat = app.Showroom.Hats[2]; hat.ReturnToDisplay(); var rb = hat.GetComponent<Rigidbody>();
            // This fixture teleports a display item, not a user carry operation.
            // Flush Rigidbody interpolation before the next real rendered frame.
            var interpolation = rb.interpolation; rb.interpolation = RigidbodyInterpolation.None;
            hat.transform.position = new Vector3(0, 1.8f, 1.2f); rb.position = hat.transform.position; Physics.SyncTransforms();
            hat.Elastic.SetMaterial(MembraneMaterial.Presets[0]); hat.Elastic.ResetShape(); yield return null;
            rb.interpolation = interpolation;
            var v = hat.Elastic.MeshAt(0).vertices; int left = 0, right = 0;
            for (int i = 1; i < v.Length; i++) { if (v[i].x < v[left].x) left = i; if (v[i].x > v[right].x) right = i; }
            Vector3 a = hat.transform.TransformPoint(v[left]), b = hat.transform.TransformPoint(v[right]);
            float span = Vector3.Distance(v[left], v[right]); Grip(0, a); Grip(1, b);
            result.twoHands = hands.HeldCount == 2 && hat.Elastic.GrabCount == 2;
            Check(result.twoHands, "Red hat two-hand Grip failed: count=" + hands.HeldCount + " patches=" + hat.Elastic.GrabCount + " position=" + hat.transform.position + " bounds=" + hat.GetComponent<Collider>().bounds + " a=" + a + " b=" + b);
            Phase("red_hat_stretch"); end = Time.realtimeSinceStartupAsDouble + 1.5;
            while (Time.realtimeSinceStartupAsDouble < end) { Tick(0, a - Vector3.right * .15f); Tick(1, b + Vector3.right * .15f); yield return null; }
            var pulled = hat.Elastic.MeshAt(0).vertices; result.hatExtensionM = Vector3.Distance(pulled[left], pulled[right]) - span;
            Check(result.hatExtensionM > .025f && result.hatExtensionM < .065f, "Red hat stretch not bounded near 5 cm span cap");
            result.balancedForces = (hat.Elastic.HandForce0 + hat.Elastic.HandForce1).magnitude < .00001f && hat.Elastic.HandForce0.magnitude > 1;
            Check(result.balancedForces, "Hat command forces are not balanced");
            Phase("red_hat_compression"); end = Time.realtimeSinceStartupAsDouble + 1.5;
            while (Time.realtimeSinceStartupAsDouble < end) { Tick(0, a + Vector3.right * .15f); Tick(1, b - Vector3.right * .15f); yield return null; }
            var compressed = hat.Elastic.MeshAt(0).vertices; result.hatCompressionM = span - Vector3.Distance(compressed[left], compressed[right]);
            Check(result.hatCompressionM > .025f && result.hatCompressionM < .065f, "Red hat compression outside trial tolerance");
            hands.Release(0, false); result.firstReleaseRetainsSecond = hat.IsHeld && hands.HeldBy(1);
            Check(result.firstReleaseRetainsSecond, "First release drops other hand"); hands.ReleaseAll(true); hat.ReturnToDisplay();

            Phase("red_hat_equip_detach"); app.Showroom.WearHat(2); yield return new WaitForSecondsRealtime(.3f);
            Check(hat.IsEquipped, "Red hat not equipped"); v = hat.Elastic.MeshAt(0).vertices; p = hat.transform.TransformPoint(v[right]); Grip(0, p);
            result.headDetach = hat.IsHeld && !hat.IsEquipped; Check(result.headDetach, "Normal Grip cannot remove red hat");
            end = Time.realtimeSinceStartupAsDouble + 1;
            while (Time.realtimeSinceStartupAsDouble < end) { Tick(0, p + Vector3.forward * .7f + Vector3.up * .1f); yield return null; }
            float startY = hat.transform.position.y; hands.ReleaseAll(true); Phase("red_hat_gravity"); yield return new WaitForSecondsRealtime(.9f);
            result.hatDropM = startY - hat.transform.position.y; result.gravity = rb.useGravity && !rb.isKinematic && result.hatDropM > .1f;
            Check(result.gravity, "Red hat has no gravity after detach/release"); hat.ReturnToDisplay();
            Check(StudioTelemetry.Active && StudioTelemetry.Active.RenderedFrames > 60, "No usable camera-rendered frame capture (keep trial window visible)");
            Check(result.exceptions == 0, "Unity exceptions in rendered trial");
        }
        static int Closest(Vector3[] points, Vector3 target)
        {
            int best = 0; float d = float.PositiveInfinity;
            for (int i = 0; i < points.Length; i++) { float n = (points[i] - target).sqrMagnitude; if (n < d) { d = n; best = i; } } return best;
        }
        void Grip(int side, Vector3 p)
        {
            Physics.SyncTransforms(); hands.Step(side, true, p, Quaternion.identity, 0, 0, Time.unscaledDeltaTime); Tick(side, p);
        }
        void Tick(int side, Vector3 p) { hands.Step(side, true, p, Quaternion.identity, 1, 0, Time.unscaledDeltaTime); }
        void OnDestroy() { Application.logMessageReceived -= Log; }
    }
}
