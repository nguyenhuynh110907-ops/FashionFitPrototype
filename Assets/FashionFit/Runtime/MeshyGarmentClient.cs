using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace FashionFit
{
    // Optional prototype bridge: reference image -> Meshy smart-topology OBJ ->
    // the existing measured garment import screen. API keys remain in RAM.
    public sealed class MeshyGarmentClient : MonoBehaviour
    {
        const string Endpoint = "https://api.meshy.ai/openapi/v1/image-to-3d";
        [Serializable] class CreateRequest
        {
            public string image_url, model_type = "smart-topology", ai_model = "meshy-t2.5";
            public bool should_texture = true, enable_pbr = true;
            public int target_polycount = 30000;
            public string[] target_formats = { "obj" };
        }
        [Serializable] class CreateResponse { public string result; }
        [Serializable] class ModelUrls { public string obj; }
        [Serializable] class TaskError { public string message; }
        [Serializable] class TaskResponse { public string id, status; public ModelUrls model_urls; public TaskError task_error; }

        FashionFitApp app;
        string apiKey, imagePath;
        public bool IsBusy { get; private set; }
        public string ImageLabel { get { return string.IsNullOrEmpty(imagePath) ? "Chưa chọn ảnh" : Path.GetFileName(imagePath); } }
        public bool HasApiKey { get { return !string.IsNullOrWhiteSpace(apiKey); } }

        public void Initialize(FashionFitApp owner)
        {
            app = owner; apiKey = Environment.GetEnvironmentVariable("MESHY_API_KEY") ?? string.Empty;
        }
        public void SetApiKey(string value) { apiKey = (value ?? string.Empty).Trim(); }
        public string GetApiKeyForPasswordField() { return apiKey; }

        public void ChooseImage()
        {
            try
            {
                string path = RunwayFiles.Pick("Ảnh trang phục để tạo 3D bằng Meshy", "png,jpg,jpeg"); if (string.IsNullOrEmpty(path)) return;
                var info = new FileInfo(path); if (info.Length > 20 * 1024 * 1024) throw new InvalidDataException("Ảnh Meshy tối đa 20 MB.");
                byte[] bytes = File.ReadAllBytes(path); RunwayFiles.CheckImageSize(bytes); imagePath = path;
                app.Report("Đã chọn ảnh cho Meshy: " + Path.GetFileName(path) + " • ảnh sẽ chỉ được tải lên khi bạn bấm Tạo 3D");
            }
            catch (Exception e) { app.Report("Không chọn được ảnh Meshy: " + e.Message); }
        }

        public void Generate()
        {
            if (IsBusy) return;
            if (!HasApiKey) { app.Report("Meshy: nhập API key hoặc đặt MESHY_API_KEY."); return; }
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath)) { app.Report("Meshy: hãy chọn ảnh PNG/JPG trước."); return; }
            StartCoroutine(CreateAndImport());
        }

        IEnumerator CreateAndImport()
        {
            IsBusy = true;
            byte[] image;
            try { image = File.ReadAllBytes(imagePath); }
            catch (Exception e) { app.Report("Không đọc được ảnh Meshy: " + e.Message); IsBusy = false; yield break; }
            string mime = Path.GetExtension(imagePath).Equals(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
            var body = new CreateRequest { image_url = "data:" + mime + ";base64," + Convert.ToBase64String(image) };
            byte[] json = Encoding.UTF8.GetBytes(JsonUtility.ToJson(body));
            string taskId = null;
            app.Report("Meshy đang tạo smart-topology 3D từ ảnh • tác vụ này dùng credit của tài khoản bạn");
            using (var request = new UnityWebRequest(Endpoint, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(json); request.downloadHandler = new DownloadHandlerBuffer(); request.timeout = 120;
                request.SetRequestHeader("Authorization", "Bearer " + apiKey); request.SetRequestHeader("Content-Type", "application/json");
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    app.Report("Meshy không tạo được tác vụ: " + request.responseCode + " • " + request.error); IsBusy = false; yield break;
                }
                CreateResponse created = JsonUtility.FromJson<CreateResponse>(request.downloadHandler.text); taskId = created != null ? created.result : null;
            }
            if (string.IsNullOrEmpty(taskId)) { app.Report("Meshy không trả task ID."); IsBusy = false; yield break; }

            TaskResponse task = null;
            for (int attempt = 0; attempt < 60; attempt++)
            {
                yield return new WaitForSecondsRealtime(5);
                using (var poll = UnityWebRequest.Get(Endpoint + "/" + taskId))
                {
                    poll.timeout = 60; poll.SetRequestHeader("Authorization", "Bearer " + apiKey); yield return poll.SendWebRequest();
                    if (poll.result != UnityWebRequest.Result.Success) { app.Report("Meshy: đang chờ tác vụ, lần kiểm tra lỗi " + poll.responseCode); continue; }
                    task = JsonUtility.FromJson<TaskResponse>(poll.downloadHandler.text);
                }
                if (task == null) continue;
                if (task.status == "SUCCEEDED") break;
                if (task.status == "FAILED" || task.status == "CANCELED")
                {
                    app.Report("Meshy thất bại: " + (task.task_error != null ? task.task_error.message : task.status)); IsBusy = false; yield break;
                }
                app.Report("Meshy đang xử lý 3D… " + task.status + " • " + (attempt + 1) * 5 + " giây");
            }

            string url = task != null && task.model_urls != null ? task.model_urls.obj : null;
            if (string.IsNullOrEmpty(url)) { app.Report("Meshy chưa trả OBJ sau thời gian chờ. Có thể xem task " + taskId + " trong tài khoản Meshy."); IsBusy = false; yield break; }
            using (var download = UnityWebRequest.Get(url))
            {
                download.timeout = 120; yield return download.SendWebRequest();
                if (download.result != UnityWebRequest.Result.Success) { app.Report("Không tải được OBJ Meshy: " + download.error); IsBusy = false; yield break; }
                try
                {
                    string folder = Path.Combine(Application.persistentDataPath, "MeshyGarments"); Directory.CreateDirectory(folder);
                    string output = Path.Combine(folder, "meshy-" + taskId + ".obj"); File.WriteAllBytes(output, download.downloadHandler.data);
                    app.Wardrobe.LoadOBJ(output);
                    app.Report("Đã tải OBJ Meshy • hãy chọn đơn vị/mốc và xác nhận fitting • AI 3D vẫn cần retopology/rig để dùng sản xuất");
                }
                catch (Exception e) { app.Report("Không nhập được OBJ Meshy: " + e.Message); }
            }
            IsBusy = false;
        }

        void OnDestroy() { apiKey = string.Empty; }
    }
}
