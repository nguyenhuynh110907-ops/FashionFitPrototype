#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace FashionFit.Editor
{
    // Runs only on request in the Editor. No model is loaded into the VR game.
    public sealed class FitVisionWindow : EditorWindow
    {
        // Assigned by Unity JsonUtility, not by C# source assignments.
#pragma warning disable 0649
        [Serializable] sealed class Ranked { public string label; public float probability; }
        [Serializable] sealed class Prediction { public string task, label; public float probability; public Ranked[] top3; }
        [Serializable] sealed class Result { public int schema_version; public string image, guidance, warning, deployment_status; public bool requires_review; public Prediction[] predictions; }
        [Serializable] sealed class TaskScore { public string name; public float accuracy, macro_f1; public int test_n; }
        [Serializable] sealed class Summary { public int dataset_size, train_count, validation_count, test_count, best_epoch; public TaskScore[] tasks; }
#pragma warning restore 0649

        string imagePath, status;
        Result result;
        Summary summary;
        Process worker;
        Task<string> stdout, stderr;
        Vector2 scroll;
        double started;
        string Root { get { return Path.GetFullPath(Path.Combine(Application.dataPath, "..")); } }
        string Study { get { return Path.Combine(Root, "AI", "FitVision"); } }
        string ResultPath { get { return Path.Combine(Study, "Artifacts", "editor_prediction.json"); } }

        [MenuItem("Fashion Fit/AI/Analyze Garment Photo")]
        static void Open() { GetWindow<FitVisionWindow>("FitVision AI").Show(); }

        void OnEnable()
        {
            minSize = new Vector2(440, 420);
            EditorApplication.update += Poll;
            try {
                string path = Path.Combine(Study, "Artifacts", "editor_summary.json");
                if (File.Exists(path)) summary = JsonUtility.FromJson<Summary>(File.ReadAllText(path));
            } catch (Exception error) { status = error.Message; }
        }
        void OnDisable() { EditorApplication.update -= Poll; Stop(); }

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            GUILayout.Label("AI nhận diện trang phục và phom", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Mô hình nghiên cứu từ 100 ảnh. Phân tích ảnh chạy cục bộ; kết quả cần kiểm tra bằng mắt. AI này chưa học chuyển động vải hoặc tạo mesh 3D.", MessageType.Info);
            if (summary != null) {
                GUILayout.Label("Train " + summary.train_count + " · Validation " + summary.validation_count + " · Test " + summary.test_count);
                if (summary.tasks != null) foreach (TaskScore task in summary.tasks)
                    GUILayout.Label((task.name == "category" ? "Loại trang phục" : "Nhóm phom") + ": test accuracy " + task.accuracy.ToString("P1") + " · macro F1 " + task.macro_f1.ToString("F2") + " · n=" + task.test_n);
            }
            GUILayout.Space(10);
            using (new EditorGUI.DisabledScope(worker != null)) {
                if (GUILayout.Button("1 · Chọn ảnh JPG / PNG", GUILayout.Height(32))) {
                    string selected = EditorUtility.OpenFilePanel("Ảnh trang phục · chỉ xử lý trên máy", Root, "jpg,jpeg,png");
                    if (!string.IsNullOrEmpty(selected)) { imagePath = selected; result = null; status = null; }
                }
                EditorGUILayout.LabelField("Ảnh", string.IsNullOrEmpty(imagePath) ? "Chưa chọn" : Path.GetFileName(imagePath));
                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(imagePath)))
                    if (GUILayout.Button("2 · Phân tích bằng mô hình đã huấn luyện", GUILayout.Height(36))) Analyze();
            }
            if (worker != null) {
                GUILayout.Label("Đang phân tích · " + (EditorApplication.timeSinceStartup - started).ToString("F0") + " giây");
                if (GUILayout.Button("Hủy phân tích")) { Stop(); status = "Đã hủy."; }
            }
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, result == null ? MessageType.Info : MessageType.None);
            if (result != null && result.predictions != null) {
                foreach (Prediction prediction in result.predictions) {
                    GUILayout.Space(8);
                    GUILayout.Label(prediction.task == "category" ? "Loại trang phục" : "Nhóm phom quan sát", EditorStyles.boldLabel);
                    if (prediction.top3 != null) foreach (Ranked item in prediction.top3)
                        GUILayout.Label(Label(item.label) + " · " + item.probability.ToString("P1"));
                }
                GUILayout.Space(10);
                EditorGUILayout.HelpBox("Xác suất mô hình không bảo đảm đúng với ảnh mới. Chưa tự áp kết quả lên trang phục trong game.", MessageType.Warning);
                EditorGUILayout.HelpBox(Guidance(result.predictions), MessageType.Info);
            }
            GUILayout.Space(10);
            if (GUILayout.Button("Mở báo cáo huấn luyện")) EditorUtility.RevealInFinder(Path.Combine(Study, "TRAINING_REPORT.md"));
            EditorGUILayout.EndScrollView();
        }

        void Analyze()
        {
            try {
                string python = Path.Combine(Root, ".ml-runtime", "python", "python.exe");
                if (!File.Exists(python) || !File.Exists(Path.Combine(Study, "Artifacts", "heads.safetensors"))) throw new InvalidOperationException("Thiếu môi trường hoặc trọng số. Đọc AI/FitVision/README.md.");
                if (!File.Exists(imagePath) || new FileInfo(imagePath).Length > 20 * 1024 * 1024) throw new InvalidOperationException("Chọn ảnh có dung lượng tối đa 20 MB.");
                var info = new ProcessStartInfo {
                    FileName = python,
                    Arguments = Quote(Path.Combine(Study, "fitvision.py")) + " predict --image " + Quote(imagePath) + " --output " + Quote(ResultPath),
                    WorkingDirectory = Root, UseShellExecute = false, CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden, RedirectStandardOutput = true, RedirectStandardError = true
                };
                info.EnvironmentVariables["PYTHONUTF8"] = "1";
                info.EnvironmentVariables["HF_HUB_OFFLINE"] = "1";
                info.EnvironmentVariables["HF_HUB_DISABLE_TELEMETRY"] = "1";
                worker = new Process { StartInfo = info };
                worker.Start(); stdout = worker.StandardOutput.ReadToEndAsync(); stderr = worker.StandardError.ReadToEndAsync();
                started = EditorApplication.timeSinceStartup; result = null; status = null;
            } catch (Exception error) { Stop(); status = error.Message; }
        }

        void Poll()
        {
            if (worker == null) return;
            try {
                if (EditorApplication.timeSinceStartup - started > 180) { Stop(); status = "Phân tích quá 180 giây; đã dừng. Kiểm tra môi trường AI."; Repaint(); return; }
                if (!worker.HasExited || !stdout.IsCompleted || !stderr.IsCompleted) { Repaint(); return; }
                if (worker.ExitCode != 0) {
                    string error = stderr.Result;
                    throw new InvalidOperationException(error.Length > 4000 ? error.Substring(error.Length - 4000) : error);
                }
                result = JsonUtility.FromJson<Result>(File.ReadAllText(ResultPath));
                if (result == null || result.schema_version != 1 || result.predictions == null || result.predictions.Length != 2) throw new InvalidDataException("Kết quả AI không đúng định dạng.");
                status = "Đã phân tích trên máy · cần kiểm tra kết quả trước khi dựng trang phục.";
                Stop(); Repaint();
            } catch (Exception error) { Stop(); status = error.Message; Repaint(); }
        }
        void Stop()
        {
            if (worker == null) return;
            try { if (!worker.HasExited) worker.Kill(); } catch (InvalidOperationException) { }
            finally { worker.Dispose(); worker = null; stdout = null; stderr = null; }
        }
        static string Quote(string value) { return "\"" + value.Replace("\"", "\\\"") + "\""; }
        static string Label(string code)
        {
            switch (code) {
                case "dress": return "Đầm"; case "skirt": return "Chân váy"; case "trousers": return "Quần";
                case "top": return "Áo"; case "outerwear": return "Áo khoác / choàng"; case "jumpsuit": return "Jumpsuit"; case "separates": return "Bộ phối";
                case "close": return "Ôm / dáng cột"; case "volume": return "Xòe / thể tích lớn"; case "straight_relaxed": return "Suông / rộng";
                case "draped_layered": return "Quấn / xếp rủ / nhiều lớp"; case "tailored": return "Có cấu trúc may";
                default: return code;
            }
        }
        static string Guidance(Prediction[] predictions)
        {
            foreach (Prediction prediction in predictions) if (prediction.task == "appearance") {
                switch (prediction.label) {
                    case "close": return "Gợi ý dựng phom: fit riêng ngực–eo–hông, giữ độ dư và khoảng tiếp xúc riêng.";
                    case "volume": return "Gợi ý dựng phom: tạo diện tích tùng/panel và điểm giữ eo; để gấu có độ tự do.";
                    case "straight_relaxed": return "Gợi ý dựng phom: giữ độ dư theo thiết kế; không ép vải sát mọi vùng lõm của body.";
                    case "draped_layered": return "Gợi ý dựng phom: cần panel quấn/xếp rủ và khoảng hở giữa các lớp.";
                    case "tailored": return "Gợi ý dựng phom: cần cấu trúc vai, ve, cạp và đường may riêng.";
                }
            }
            return "Kiểm tra ảnh và phom thủ công.";
        }
    }
}
#endif
