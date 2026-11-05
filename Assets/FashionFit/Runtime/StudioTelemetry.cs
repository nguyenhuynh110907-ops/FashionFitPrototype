using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.XR;

namespace FashionFit
{
    // Opt-in observation only. Never changes solver cadence, quality or input.
    [DefaultExecutionOrder(2000)]
    public sealed class StudioTelemetry : MonoBehaviour
    {
        [Serializable] public sealed class Metric
        {
            public string status; public int samples;
            public double mean, p50, p95, p99, max;
            public static Metric From(List<double> values)
            {
                var m = new Metric { samples = values.Count, status = values.Count == 0 ? "NOT_ASSESSED" : "RECORDED" };
                if (values.Count == 0) return m;
                values.Sort(); double sum = 0; foreach (double v in values) sum += v;
                m.mean = sum / values.Count; m.max = values[values.Count - 1];
                m.p50 = values[(int)Math.Ceiling(values.Count * .50) - 1];
                m.p95 = values[(int)Math.Ceiling(values.Count * .95) - 1];
                m.p99 = values[(int)Math.Ceiling(values.Count * .99) - 1]; return m;
            }
        }
        [Serializable] public sealed class Summary
        {
            public string status, startedUtc, endedUtc, scope, unity, applicationVersion, device, gpu;
            public string headsetAssessment = "NOT_ASSESSED", compositorDroppedFrames = "NOT_ASSESSED";
            public string note = "App timings only; wall frame includes pacing/wait. No Link/compositor latency or physical-controller acceptance verdict. Solver time is last executed pose step, not a per-frame sum. Missing counters are NOT_ASSESSED. GC includes recorder/test/UI overhead.";
            public int frames, renderedFrames, focusedFrames, xrActiveFrames, headTrackedFrames, leftTrackedFrames, rightTrackedFrames, overBudgetFrames, exceptions, width, height, vSyncCount, targetFrameRate;
            public bool syntheticInput, frameTimingEnabled, truncated, durationCompleted, physicalInteractionVerified;
            public double budgetMs = 1000.0 / 72, requestedSeconds, capturedSeconds, warmupSeconds;
            public Metric uniquePoseSolveMs, uniquePosePublishMs;
            public Metric wallFrameMs, renderedWallFrameMs, cpuFrameIncludingWaitMs, cpuMainThreadMs, gpuFrameMs, gcAllocatedBytes, lastPoseStepMs;
        }
        struct Sample
        {
            public double time, wall, cpu, main, gpu, pose, solve, publish; public long gc;
            public bool xr, head, left, right, rendered, focused; public int phase;
        }
        const int Capacity = 100000;
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        readonly Sample[] samples = new Sample[Capacity];
        readonly FrameTiming[] timings = new FrameTiming[1];
        readonly List<string> phases = new List<string> { "interactive" };
        ProfilerRecorder gcRecorder;
        FashionFitApp app; string folder, started; double seconds = 600, warmup = 5, begin = -1, lastTime;
        uint lastSolverVersion; ulong lastStamp; int count, phase, errors, lastRenderedFrame = -10; bool saved, synthetic, durationCompleted;
        public static StudioTelemetry Active { get; private set; }
        public int Frames => count;
        public int RenderedFrames { get; private set; }
        public static string Argument(string key)
        {
            var a = Environment.GetCommandLineArgs();
            for (int i = 0; i < a.Length - 1; i++) if (a[i] == key) return a[i + 1]; return null;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            string output = Argument("-fashionPerformance"); if (output == null) return;
            var t = new GameObject("FashionFit opt-in performance capture").AddComponent<StudioTelemetry>();
            t.folder = Path.GetFullPath(output); t.synthetic = Argument("-fashionStudioTrial") != null || Argument("-fashionContactTrial") != null;
            if (double.TryParse(Argument("-fashionPerformanceSeconds"), NumberStyles.Float, Inv, out double s) && !double.IsNaN(s) && !double.IsInfinity(s)) t.seconds = Math.Max(10, Math.Min(1800, s));
            if (double.TryParse(Argument("-fashionPerformanceWarmup"), NumberStyles.Float, Inv, out double w) && !double.IsNaN(w) && !double.IsInfinity(w)) t.warmup = Math.Max(0, Math.Min(120, w));
            Directory.CreateDirectory(t.folder); t.started = DateTime.UtcNow.ToString("O"); Active = t;
            t.gcRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
            Application.logMessageReceived += t.Log;
            Camera.onPostRender += t.CameraRendered;
        }
        void CameraRendered(Camera camera) { if (app && app.Player && camera == app.Player.View) lastRenderedFrame = Time.frameCount; }
        void Log(string message, string trace, LogType type) { if (type == LogType.Error || type == LogType.Exception) errors++; }
        public void SetPhase(string value)
        {
            int i = phases.IndexOf(value); if (i < 0) { phases.Add(value); i = phases.Count - 1; } phase = i;
        }
        static bool Tracked(XRNode node)
        {
            var d = InputDevices.GetDeviceAtXRNode(node); return d.isValid && d.TryGetFeatureValue(CommonUsages.isTracked, out bool b) && b;
        }
        void LateUpdate()
        {
            if (saved || folder == null) return;
            if (!app) app = FindObjectOfType<FashionFitApp>();
            if (!app || !app.Ready) return;
            double now = Time.realtimeSinceStartupAsDouble;
            if (begin < 0) { begin = now; lastTime = now; }
            if (now - begin < warmup) { lastTime = now; return; }
            if (now - begin - warmup >= seconds) { durationCompleted = true; Finish(); return; }
            if (count == Capacity) { Finish(); return; }
            var s = new Sample { time = now - begin - warmup, wall = (now - lastTime) * 1000, cpu = -1, main = -1, gpu = -1, gc = -1, pose = -1, solve = -1, publish = -1, phase = phase };
            lastTime = now;
            s.rendered = lastRenderedFrame == Time.frameCount - 1; s.focused = Application.isFocused;
            if (s.rendered) RenderedFrames++;
            if (FrameTimingManager.IsFeatureEnabled())
            {
                if (FrameTimingManager.GetLatestTimings(1, timings) > 0 && timings[0].frameStartTimestamp != lastStamp)
                {
                    lastStamp = timings[0].frameStartTimestamp;
                    s.cpu = timings[0].cpuFrameTime; s.main = timings[0].cpuMainThreadFrameTime; s.gpu = timings[0].gpuFrameTime;
                }
                FrameTimingManager.CaptureFrameTimings();
            }
            if (gcRecorder.Valid && gcRecorder.Count > 0) s.gc = gcRecorder.LastValue;
            if (app.PoseStudio && app.PoseStudio.Wearables.Count > 0) {var wear=app.PoseStudio.Wearables[0];s.pose=wear.Milliseconds;if(wear.SolverVersion!=lastSolverVersion){lastSolverVersion=wear.SolverVersion;s.solve=wear.SolveMilliseconds;s.publish=wear.PublishMilliseconds;}}
            s.xr = XRSettings.isDeviceActive; s.head = Tracked(XRNode.Head); s.left = Tracked(XRNode.LeftHand); s.right = Tracked(XRNode.RightHand);
            samples[count++] = s;
        }
        public void Finish()
        {
            if (saved || folder == null) return; saved = true;
            try
            {
                var r = new Summary { status = count > 0 && errors == 0 ? "RECORDED" : "INCOMPLETE", startedUtc = started, endedUtc = DateTime.UtcNow.ToString("O"),
                    unity = Application.unityVersion, applicationVersion = Application.version, device = SystemInfo.deviceModel, gpu = SystemInfo.graphicsDeviceName,
                    scope = synthetic ? "Desktop rendered trial; injected controller poses; automatic LateUpdate and PhysX" : "User-driven capture; observe actual XR/tracking counts below",
                    frames = count, syntheticInput = synthetic, frameTimingEnabled = FrameTimingManager.IsFeatureEnabled(), requestedSeconds = seconds, warmupSeconds = warmup,
                    truncated = count == Capacity, durationCompleted = durationCompleted, exceptions = errors, width = Screen.width, height = Screen.height, vSyncCount = QualitySettings.vSyncCount, targetFrameRate = Application.targetFrameRate };
                var solve = new List<double>(count); var publish = new List<double>(count); var wall = new List<double>(count); var renderedWall = new List<double>(count); var cpu = new List<double>(count); var main = new List<double>(count); var gpu = new List<double>(count); var gc = new List<double>(count); var pose = new List<double>(count);
                using (var w = new StreamWriter(Path.Combine(folder, "frames.csv"), false, new System.Text.UTF8Encoding(false)))
                {
                    w.WriteLine("seconds,phase,wall_frame_ms,cpu_frame_including_wait_ms,cpu_main_thread_ms,gpu_ms,gc_allocated_bytes,last_pose_step_ms,xr_active,head_tracked,left_tracked,right_tracked,camera_rendered,focused,unique_pose_solve_ms,unique_pose_publish_ms");
                    for (int i = 0; i < count; i++)
                    {
                        var s = samples[i]; if (s.wall > 0) wall.Add(s.wall); if (s.rendered && s.wall > 0) renderedWall.Add(s.wall); if (s.cpu > 0) cpu.Add(s.cpu); if (s.main > 0) main.Add(s.main); if (s.gpu > 0) gpu.Add(s.gpu); if (s.gc >= 0) gc.Add(s.gc); if (s.pose > 0) pose.Add(s.pose);
                        if(s.solve>=0)solve.Add(s.solve);if(s.publish>=0)publish.Add(s.publish);if (s.rendered) r.renderedFrames++; if (s.focused) r.focusedFrames++;
                        if (s.xr) r.xrActiveFrames++; if (s.head) r.headTrackedFrames++; if (s.left) r.leftTrackedFrames++; if (s.right) r.rightTrackedFrames++; if (s.wall > r.budgetMs) r.overBudgetFrames++;
                        w.WriteLine(string.Format(Inv, "{0:F6},{1},{2:F4},{3:F4},{4:F4},{5:F4},{6},{7:F4},{8},{9},{10},{11},{12},{13},{14:F4},{15:F4}", s.time, phases[s.phase], s.wall, s.cpu, s.main, s.gpu, s.gc, s.pose, s.xr ? 1 : 0, s.head ? 1 : 0, s.left ? 1 : 0, s.right ? 1 : 0, s.rendered ? 1 : 0, s.focused ? 1 : 0, s.solve, s.publish));
                    }
                }
                r.uniquePoseSolveMs=Metric.From(solve);r.uniquePosePublishMs=Metric.From(publish);r.wallFrameMs = Metric.From(wall); r.renderedWallFrameMs = Metric.From(renderedWall); r.cpuFrameIncludingWaitMs = Metric.From(cpu); r.cpuMainThreadMs = Metric.From(main); r.gpuFrameMs = Metric.From(gpu); r.gcAllocatedBytes = Metric.From(gc); r.lastPoseStepMs = Metric.From(pose);
                r.capturedSeconds = count > 1 ? samples[count - 1].time - samples[0].time : 0;
                if (r.xrActiveFrames > 0) r.headsetAssessment = synthetic ? "SYNTHETIC_INPUT_WITH_XR_PRESENT" : "XR_PRESENCE_RECORDED_MANUAL_ACCEPTANCE_PENDING";
                File.WriteAllText(Path.Combine(folder, "performance.json"), JsonUtility.ToJson(r, true));
            }
            catch (Exception e) { Debug.LogException(e); }
            finally { gcRecorder.Dispose(); Application.logMessageReceived -= Log; Camera.onPostRender -= CameraRendered; }
        }
        void OnApplicationQuit() { Finish(); }
        void OnDestroy() { Finish(); if (Active == this) Active = null; }
    }
}
