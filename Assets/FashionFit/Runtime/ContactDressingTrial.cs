using System;
using System.IO;
using System.Collections;
using UnityEngine;

namespace FashionFit
{
    public sealed class ContactDressingTrial : MonoBehaviour
    {
        [Serializable] sealed class Result
        {
            public string status = "passed", scope = "Rendered native cloth prototype, injected Touch input, no physical HMD";
            public int checks, exceptions, particles;
            public float poseTravel, clothTravel, releaseTravel, clothMinY, maxStrain;
            public bool twoHands, mannequinHold, retainedAfterVRRelease, handReleased;
            public string materialValidation = "NOT_ASSESSED", physicalHeadset = "NOT_ASSESSED";
        }
        FashionFitApp app;
        RunwayVRHands hands;
        string folder;
        Result result = new Result();
        ContactGarment observed;
        void Update() { if (observed && observed.Active) result.maxStrain = Mathf.Max(result.maxStrain, observed.MaximumStrain); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            string output = StudioTelemetry.Argument("-fashionContactTrial");
            if (output == null) return;
            var trial = new GameObject("Contact dressing trial").AddComponent<ContactDressingTrial>();
            trial.folder = Path.GetFullPath(output);
        }
        void Check(bool value, string message) { result.checks++; if (!value) throw new InvalidOperationException(message); }
        void Log(string message, string stack, LogType type) { if (type == LogType.Error || type == LogType.Exception) result.exceptions++; }
        IEnumerator Start()
        {
            Application.runInBackground = true;
            Directory.CreateDirectory(folder + "/Contact");
            Directory.CreateDirectory(folder + "/Cage");
            Application.logMessageReceived += Log;
            var scenario = Scenario(); int exit = 0;
            while (true)
            {
                object next = null; bool more = false; Exception error = null;
                try { more = scenario.MoveNext(); if (more) next = scenario.Current; } catch (Exception e) { error = e; }
                if (error != null) { result.status = "failed"; File.WriteAllText(folder + "/failure.txt", error.ToString()); exit = 1; break; }
                if (!more) break;
                yield return next;
            }
            if (observed) result.materialValidation = result.maxStrain <= observed.GetComponent<ElasticSurface>().Material.maxStrain ? "within_preset_range" : "FAILED_strain_exceeds_preset";
            hands?.ReleaseAll(false);
            File.WriteAllText(folder + "/trial.json", JsonUtility.ToJson(result, true));
            StudioTelemetry.Active?.Finish(); Application.Quit(exit);
        }
        IEnumerator Scenario()
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 45;
            while (!app || !app.Ready) { app = FindObjectOfType<FashionFitApp>(); Check(Time.realtimeSinceStartupAsDouble < deadline, "Startup timeout"); yield return null; }
            app.Player.PrepareForVRTrial(); app.Player.EnsureHands(); hands = app.Player.Hands;
            app.Player.enabled = false;
            if (app.Player.VRMenu) app.Player.VRMenu.Hide();
            GarmentContactWorld.Get(app).SetManualDressing(true);
            app.Showroom.SelectOutfitTarget(1); app.PoseStudio.Wear(0, 5);
            var wear = app.PoseStudio.Wearables[0]; var surface = wear.Surface; observed = surface.ContactMotion;
            var camera = app.Player.View; camera.rect = new Rect(0, 0, 1, 1);
            camera.transform.position = wear.Rig.Avatar.transform.TransformPoint(new Vector3(.7f, 1.4f, 2.1f));
            camera.transform.LookAt(wear.Rig.Avatar.transform.TransformPoint(new Vector3(0, 1.1f, 0)));
            app.PoseStudio.SetHandles(true);
            yield return new WaitForSecondsRealtime(1);
            RunwayPoseHandle handle = null;
            foreach (var h in wear.Rig.Handles) if (h.Bone.name == "LeftHand") handle = h;
            Check(handle, "Missing wrist handle");
            Vector3 start = handle.Bone.position, p = handle.transform.position;
            Grip(0, p); Check(hands.HeldBy(0) && hands.HeldBy(0).IsJoint, "Pose joint acquisition failed");
            double end = Time.realtimeSinceStartupAsDouble + 1;
            while (Time.realtimeSinceStartupAsDouble < end) { Tick(0, p + Vector3.up * .12f); yield return null; }
            result.poseTravel = Vector3.Distance(start, handle.Bone.position);
            Check(result.poseTravel > .04f, "Pose wrist did not move");
            hands.ReleaseAll(false); app.PoseStudio.ResetPose(0); app.PoseStudio.SetHandles(false);
            yield return new WaitForSecondsRealtime(.5f);
            Capture("initial"); Export(wear, "initial");
            int index = Closest(surface.CurrentPoints, new Vector3(-.10f, 1.15f, .20f));
            Vector3 initial = surface.CurrentPoints[index]; p = surface.transform.TransformPoint(initial);
            Grip(0, p); Check(hands.HeldBy(0) && hands.HeldBy(0).Elastic == surface, "Near cloth Grip failed");
            result.particles = surface.ContactMotion.ParticleCount;
            end = Time.realtimeSinceStartupAsDouble + 3;
            while (Time.realtimeSinceStartupAsDouble < end) { Tick(0, p + Vector3.up * .22f + Vector3.forward * .22f); yield return null; }
            result.clothTravel = Vector3.Distance(initial, surface.CurrentPoints[index]);
            Capture("pulled"); Export(wear, "pulled");
            hands.ReleaseAll(false); yield return new WaitForSecondsRealtime(4);
            result.releaseTravel = Vector3.Distance(initial, surface.CurrentPoints[index]);
            result.clothMinY = float.PositiveInfinity;
            foreach (var point in surface.CurrentPoints) result.clothMinY = Mathf.Min(result.clothMinY, point.y);
            Capture("released"); Export(wear, "released");
            result.maxStrain = Mathf.Max(result.maxStrain, surface.ContactMotion.MaximumStrain);
            result.materialValidation = result.maxStrain <= surface.Material.maxStrain ? "within_preset_range" : "FAILED_strain_exceeds_preset";
            // Acquire two distinct near patches through the ordinary Touch route.
            var vertices = surface.CurrentPoints;
            int left = Closest(vertices, new Vector3(-.15f, 1.3f, .3f));
            int right = Closest(vertices, new Vector3(.15f, 1.3f, .3f));
            Vector3 lp = surface.transform.TransformPoint(vertices[left]), rp = surface.transform.TransformPoint(vertices[right]);
            Grip(0, lp); Grip(1, rp);
            result.twoHands = hands.HeldCount == 2; Check(result.twoHands, "Two cloth grips failed");
            end = Time.realtimeSinceStartupAsDouble + .8;
            while (Time.realtimeSinceStartupAsDouble < end) { Tick(0, lp + Vector3.up * .04f); Tick(1, rp + Vector3.up * .04f); yield return null; }
            Export(wear, "two_hands");
            hands.Release(0, false);
            Check(hands.HeldBy(1) && surface.GrabCount == 1, "First release removed second grip");
            hands.ReleaseAll(false);
            yield return new WaitForSecondsRealtime(.3f);
            // Explicit fixture placement brings the other mannequin's hand close
            // to cloth. The subsequent hold uses the same Trigger route as VR.
            var other = app.PoseStudio.Rigs[1];
            Vector3 handPoint = other.Avatar.FindJoint("LeftHand").position;
            surface.Nearest(surface.transform.TransformPoint(surface.CurrentPoints[left]), .1f, out Vector3 clothPoint);
            var stand = app.PoseStudio.Bases[1]; var rb = stand.GetComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.None;
            rb.position += clothPoint - handPoint;
            stand.transform.position = rb.position;
            Physics.SyncTransforms(); yield return null;
            handPoint = other.Avatar.FindJoint("LeftHand").position;
            hands.Step(0, true, handPoint, Quaternion.identity, 0, 0, Time.deltaTime);
            hands.Step(0, true, handPoint, Quaternion.identity, 0, 1, Time.deltaTime);
            result.mannequinHold = surface.ContactMotion.SupportCount == 1;
            Check(result.mannequinHold, "Trigger did not create mannequin hand support");
            yield return new WaitForSecondsRealtime(.5f);
            Export(wear, "mannequin_hold");
            Export(wear, "foreign_hand_body", other.Avatar);
            result.retainedAfterVRRelease = surface.GrabCount == 0 && surface.ContactMotion.SupportCount == 1;
            Check(result.retainedAfterVRRelease, "Mannequin support vanished after VR release");
            hands.Step(0, true, handPoint, Quaternion.identity, 0, 0, Time.deltaTime);
            hands.Step(0, true, handPoint, Quaternion.identity, 0, 1, Time.deltaTime);
            result.handReleased = surface.ContactMotion.SupportCount == 0;
            Check(result.handReleased, "Second Trigger did not release mannequin support");
            Check(result.clothTravel > .08f, "Cloth remained body locked");
            Check(result.exceptions == 0, "Unity runtime errors");
        }
        void Grip(int side, Vector3 p) { Physics.SyncTransforms(); hands.Step(side, true, p, Quaternion.identity, 0, 0, Time.deltaTime); Tick(side, p); }
        void Tick(int side, Vector3 p) { hands.Step(side, true, p, Quaternion.identity, 1, 0, Time.deltaTime); }
        static int Closest(Vector3[] points, Vector3 target)
        { int best = 0; for (int i = 1; i < points.Length; i++) if ((points[i] - target).sqrMagnitude < (points[best] - target).sqrMagnitude) best = i; return best; }
        void Capture(string label) { ScreenCapture.CaptureScreenshot(folder + "/" + label + ".png"); }
        void Export(PoseWearable wear, string label, RunwayAvatar contactBody = null)
        {
            var body = new Mesh(); var avatar = contactBody ? contactBody : wear.Rig.Avatar; var previous = avatar.Renderer.sharedMesh;
            avatar.Renderer.sharedMesh = avatar.BodyMesh; avatar.Renderer.BakeMesh(body); avatar.Renderer.sharedMesh = previous;
            using (var writer = new BinaryWriter(File.Create(folder + "/Contact/" + label + ".bin")))
            { writer.Write(0x53434631); var vertices = body.vertices;
                var matrix = wear.Surface.transform.worldToLocalMatrix * avatar.Renderer.transform.localToWorldMatrix;
                for (int i = 0; i < vertices.Length; i++) vertices[i] = matrix.MultiplyPoint3x4(vertices[i]);
                Write(writer, vertices, body.triangles); Write(writer, wear.Surface.CurrentPoints, wear.Surface.MeshAt(0).triangles); }
            var parts = (System.Collections.IEnumerable)typeof(ContactGarment).GetField("parts", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(wear.Surface.ContactMotion);
            int partIndex = 0;
            foreach (ContactClothPart part in parts)
            {
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var type = typeof(ContactClothPart);
                var rest = (Vector3[])type.GetField("rest", flags).GetValue(part);
                var current = (Vector3[])type.GetField("positions", flags).GetValue(part);
                var triangles = (int[])type.GetField("triangles", flags).GetValue(part);
                using (var writer = new BinaryWriter(File.Create(folder + "/Cage/" + label + "_" + partIndex++ + ".bin")))
                { writer.Write(0x43414745); Write(writer, rest, triangles); Write(writer, current, triangles); }
            }
            Destroy(body);
        }
        static void Write(BinaryWriter writer, Vector3[] points, int[] indices)
        { writer.Write(points.Length); foreach (var p in points) { writer.Write(p.x); writer.Write(p.y); writer.Write(p.z); } writer.Write(indices.Length); foreach (int index in indices) writer.Write(index); }
        void OnDestroy() { Application.logMessageReceived -= Log; }
    }
}
