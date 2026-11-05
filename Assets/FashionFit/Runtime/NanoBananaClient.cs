using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace FashionFit
{
    // Optional bridge from Gemini/Nano Banana to a 2D fabric swatch. It never sends body
    // geometry or photographs, and it never stores the API key in project assets or PlayerPrefs.
    public sealed class NanoBananaClient : MonoBehaviour
    {
        const string Model = "gemini-nano-banana-2.1";
        const string Endpoint = "https://generativelanguage.googleapis.com/v1/models/" + Model + ":generateContent";

        [Serializable] class Part { public string text; }
        [Serializable] class Content { public Part[] parts; }
        [Serializable] class ImageConfig { public string aspectRatio = "1:1"; public string imageSize = "1K"; }
        [Serializable] class GenerationConfig { public string[] responseModalities = { "IMAGE" }; public ImageConfig imageConfig = new ImageConfig(); }
        [Serializable] class RequestBody { public Content[] contents; public GenerationConfig generationConfig = new GenerationConfig(); }
        [Serializable] class InlineData { public string mimeType; public string data; }
        [Serializable] class ResponsePart { public string text; public InlineData inlineData; }
        [Serializable] class ResponseContent { public ResponsePart[] parts; }
        [Serializable] class Candidate { public ResponseContent content; }
        [Serializable] class ApiError { public string message; }
        [Serializable] class ResponseBody { public Candidate[] candidates; public ApiError error; }

        FashionFitApp app;
        string apiKey;
        public string Prompt = "lụa jacquard xanh ngọc, họa tiết hình học tối giản, sợi dệt tinh tế";
        public bool IsBusy { get; private set; }
        public string LastOutputPath { get; private set; }
        public bool HasApiKey { get { return !string.IsNullOrWhiteSpace(apiKey); } }
        public string ModelLabel { get { return Model; } }

        public void Initialize(FashionFitApp owner)
        {
            app = owner;
            apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? string.Empty;
        }

        public void SetApiKey(string value) { apiKey = (value ?? string.Empty).Trim(); }
        public string GetApiKeyForPasswordField() { return apiKey; }

        public void GenerateFabric()
        {
            if (IsBusy) return;
            if (!HasApiKey) { app.Report("Nano Banana: nhập API key hoặc đặt biến môi trường GEMINI_API_KEY."); return; }
            if (string.IsNullOrWhiteSpace(Prompt) || Prompt.Trim().Length < 4) { app.Report("Nano Banana: mô tả chất liệu cần ít nhất 4 ký tự."); return; }
            StartCoroutine(Generate());
        }

        IEnumerator Generate()
        {
            IsBusy = true;
            app.Report("Nano Banana đang tạo mẫu vải 1K... Có thể mất khoảng một phút.");
            string safePrompt = Prompt.Trim();
            string instruction = "Create a production-ready seamless square fabric texture tile for a 3D fashion garment. " +
                "Flat orthographic material scan, even neutral lighting, edge-to-edge repeat, physically plausible weave, high detail. " +
                "No person, no garment silhouette, no folds, no objects, no text, no logo, no border, no watermark-like decoration. Design brief: " + safePrompt;
            var body = new RequestBody { contents = new[] { new Content { parts = new[] { new Part { text = instruction } } } } };
            byte[] json = Encoding.UTF8.GetBytes(JsonUtility.ToJson(body));

            using (var request = new UnityWebRequest(Endpoint, UnityWebRequest.kHttpVerbPOST))
            {
                request.uploadHandler = new UploadHandlerRaw(json);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("x-goog-api-key", apiKey);
                request.timeout = 120;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    string detail = ApiMessage(request.downloadHandler != null ? request.downloadHandler.text : null);
                    app.Report("Nano Banana không trả ảnh: " + request.responseCode + " - " + (string.IsNullOrEmpty(detail) ? request.error : detail));
                    IsBusy = false;
                    yield break;
                }

                try
                {
                    ResponseBody response = JsonUtility.FromJson<ResponseBody>(request.downloadHandler.text);
                    InlineData image = FindImage(response);
                    if (image == null || string.IsNullOrEmpty(image.data)) throw new InvalidDataException("Phản hồi không chứa ảnh. Hãy thử mô tả khác hoặc kiểm tra quyền của model.");
                    if (image.data.Length > 40 * 1024 * 1024) throw new InvalidDataException("Ảnh trả về quá lớn để nạp an toàn.");
                    byte[] bytes = Convert.FromBase64String(image.data);
                    app.Wardrobe.ApplyGeneratedTexture(bytes, safePrompt);

                    string folder = Path.Combine(Application.persistentDataPath, "NanoBananaFabrics");
                    Directory.CreateDirectory(folder);
                    string ext = image.mimeType != null && image.mimeType.IndexOf("jpeg", StringComparison.OrdinalIgnoreCase) >= 0 ? ".jpg" : ".png";
                    LastOutputPath = Path.Combine(folder, "fabric-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ext);
                    File.WriteAllBytes(LastOutputPath, bytes);
                    app.Report("Đã áp mẫu vải Nano Banana lên trang phục - bản gốc lưu tại " + LastOutputPath);
                }
                catch (Exception e) { app.Report("Không đọc được ảnh Nano Banana: " + e.Message); }
            }
            IsBusy = false;
        }

        static InlineData FindImage(ResponseBody response)
        {
            if (response == null || response.candidates == null) return null;
            for (int i = 0; i < response.candidates.Length; i++)
            {
                ResponseContent content = response.candidates[i].content;
                if (content == null || content.parts == null) continue;
                for (int p = 0; p < content.parts.Length; p++) if (content.parts[p].inlineData != null) return content.parts[p].inlineData;
            }
            return null;
        }

        static string ApiMessage(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try { ResponseBody response = JsonUtility.FromJson<ResponseBody>(json); return response != null && response.error != null ? response.error.message : null; }
            catch { return null; }
        }

        void OnDestroy() { apiKey = string.Empty; }
    }
}
