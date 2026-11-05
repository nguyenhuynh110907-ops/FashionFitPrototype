using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UnityEngine;

namespace FashionFit
{
    // User-requested desktop inference. No Python/model work occurs in the cloth loop.
    public sealed class FabricDetailTrial : MonoBehaviour
    {
#pragma warning disable 0649
        [Serializable] public sealed class Sample { public string id,label,color,normal,roughness,source,license; }
        [Serializable] sealed class Catalog { public int schema_version; public Sample[] samples; }
        [Serializable] public sealed class Score { public string pattern; public float softmax_score; }
        [Serializable] public sealed class Prediction { public int schema_version; public string request_id,image_sha256,predicted_pattern; public Score[] scores; }
#pragma warning restore 0649
        FashionFitApp app;
        Sample selectedSample;
        string selectedImage,requestId,requestHash,requestFolder;
        Process worker;
        Task<string> stdout,stderr;
        float started;
        public Sample[] Samples {get;private set;}=new Sample[0];
        public Prediction Result {get;private set;}
        public Texture2D Preview {get;private set;}
        public string Status {get;private set;}="Chọn mẫu vải hoặc ảnh texture để dùng thử.";
        public string SelectedLabel {get;private set;}="Chưa chọn";
        public string SelectedImage=>selectedImage;
        public bool Busy=>worker!=null;
        public bool HasSelection=>!string.IsNullOrEmpty(selectedImage);
        public float RunningSeconds=>Busy?Time.realtimeSinceStartup-started:0;
        public string CatalogRoot=>Path.Combine(Application.streamingAssetsPath,"FashionFit","FabricTrial");
        public string ProjectRoot=>Path.GetFullPath(Path.Combine(Application.dataPath,".."));
        public bool CanAnalyze=>File.Exists(Path.Combine(ProjectRoot,".ml-runtime","python","python.exe"))&&File.Exists(Path.Combine(ProjectRoot,"AI","GarmentDetail","Artifacts","PatternDetails","pattern_head.safetensors"));

        public void Initialize(FashionFitApp owner)
        {
            app=owner;
            try{var catalog=JsonUtility.FromJson<Catalog>(File.ReadAllText(Path.Combine(CatalogRoot,"manifest.json")));if(catalog==null||catalog.schema_version!=1||catalog.samples==null)throw new InvalidDataException("Danh sách mẫu vải không đúng định dạng.");Samples=catalog.samples;}
            catch(Exception error){Status="Không nạp được mẫu vải: "+error.Message;}
        }
        string SamplePath(string name)
        {
            if(string.IsNullOrEmpty(name)||Path.GetFileName(name)!=name)throw new InvalidDataException("Tên texture trong mẫu không hợp lệ.");
            return Path.Combine(CatalogRoot,name);
        }
        public void SelectSample(int index)
        {
            try{if(index<0||index>=Samples.Length)throw new ArgumentOutOfRangeException(nameof(index));SelectImage(SamplePath(Samples[index].color),Samples[index].label);selectedSample=Samples[index];}
            catch(Exception error){Status=error.Message;}
        }
        public void ApplySample(int index)
        {
            if(index<0||index>=Samples.Length){app.Report("Không tìm thấy mẫu vải.");return;}
            if(app.Wardrobe.HasDetailedTransfer){app.Report("Bản chuyển 3D giữ nguyên vật liệu gốc. Chọn trang phục mẫu để thử texture.");return;}
            if(!app.Wardrobe.HasGarment){app.Report("Chọn trang phục trước khi áp texture.");return;}
            SelectSample(index);ApplySelected();
        }
        public void ChooseTexture()
        {
            try{string path=RunwayFiles.Pick("Chọn ảnh texture hoa văn","png,jpg,jpeg");if(!string.IsNullOrEmpty(path))SelectImage(path,Path.GetFileName(path));}
            catch(Exception error){Status=error.Message;}
        }
        public void SelectImage(string path,string label)
        {
            byte[] bytes=ReadImage(path);var preview=new Texture2D(2,2,TextureFormat.RGBA32,true,false){name="Selected fabric preview"};
            if(!ImageConversion.LoadImage(preview,bytes)){ReleasePreview(preview);throw new InvalidDataException("Không đọc được texture.");}
            preview.filterMode=FilterMode.Trilinear;Cancel();if(Preview)ReleasePreview(Preview);Preview=preview;
            selectedImage=Path.GetFullPath(path);SelectedLabel=label;selectedSample=null;Result=null;Status="Đã chọn "+label+" • Bấm Áp lên trang phục để xem.";
        }
        static byte[] ReadImage(string path)
        {
            if(!File.Exists(path)||new FileInfo(path).Length>20*1024*1024)throw new InvalidDataException("Chọn PNG/JPEG tối đa 20 MB.");
            byte[] bytes=File.ReadAllBytes(path);RunwayFiles.CheckImageSize(bytes);return bytes;
        }
        public void ApplySelected()
        {
            try{if(!HasSelection)throw new InvalidOperationException("Chọn một mẫu vải trước.");
                app.Wardrobe.ApplySurfaceFiles(selectedImage,selectedSample!=null?SamplePath(selectedSample.normal):null,selectedSample!=null?SamplePath(selectedSample.roughness):null);
                Status="Đang xem: "+SelectedLabel+" • Có thể đổi trang phục hoặc bật/tắt mẫu để so sánh.";
            }catch(Exception error){Status=error.Message;app.Report(Status);}
        }
        public void Analyze()
        {
            if(Busy)return;
            try{
                if(!CanAnalyze)throw new InvalidOperationException("AI cục bộ hiện dùng trong Unity Editor của dự án này. Các mẫu vải có sẵn vẫn dùng được.");
                byte[] input=ReadImage(selectedImage);requestId=Guid.NewGuid().ToString("N");requestHash=Hash(input);
                requestFolder=Path.Combine(Application.temporaryCachePath,"FashionFitPattern",requestId);Directory.CreateDirectory(requestFolder);
                string snapshot=Path.Combine(requestFolder,"input"+Path.GetExtension(selectedImage));File.WriteAllBytes(snapshot,input);
                string output=Path.Combine(requestFolder,"result.json");
                var info=new ProcessStartInfo{
                    FileName=Path.Combine(ProjectRoot,".ml-runtime","python","python.exe"),
                    Arguments=Quote(Path.Combine(ProjectRoot,"AI","GarmentDetail","predict_pattern_details.py"))+" "+Quote(snapshot)+" --output "+Quote(output)+" --request-id "+Quote(requestId),
                    WorkingDirectory=ProjectRoot,UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,
                    RedirectStandardOutput=true,RedirectStandardError=true};
                info.EnvironmentVariables["PYTHONUTF8"]="1";info.EnvironmentVariables["HF_HUB_OFFLINE"]="1";
                info.EnvironmentVariables["HF_HUB_DISABLE_TELEMETRY"]="1";info.EnvironmentVariables["HF_HUB_DISABLE_IMPLICIT_TOKEN"]="1";
                worker=new Process{StartInfo=info};worker.Start();stdout=worker.StandardOutput.ReadToEndAsync();stderr=worker.StandardError.ReadToEndAsync();
                started=Time.realtimeSinceStartup;Result=null;Status="AI đang nhận diện hoa văn trên máy…";
            }catch(Exception error){StopWorker();Status=error.Message;}
        }
        void Update(){Poll();}
        public void Poll()
        {
            if(!Busy)return;
            try{
                if(RunningSeconds>180)throw new TimeoutException("Phân tích quá 180 giây. Có thể thử lại khi máy rảnh.");
                if(!worker.HasExited||!stdout.IsCompleted||!stderr.IsCompleted)return;
                if(worker.ExitCode!=0){UnityEngine.Debug.LogWarning("Pattern AI: "+stderr.Result);throw new InvalidOperationException("Chưa phân tích được ảnh. Xem Console hoặc thử ảnh texture khác.");}
                string output=Path.Combine(requestFolder,"result.json");
                if(!File.Exists(output)||new FileInfo(output).Length>32768)throw new InvalidDataException("Kết quả AI không hợp lệ.");
                var result=JsonUtility.FromJson<Prediction>(File.ReadAllText(output));
                if(result==null||result.schema_version!=1||result.request_id!=requestId||result.image_sha256!=requestHash||result.scores==null||result.scores.Length!=6)throw new InvalidDataException("Kết quả không khớp ảnh vừa chọn.");
                float total=0;foreach(var score in result.scores){if(score==null||float.IsNaN(score.softmax_score)||score.softmax_score<0||score.softmax_score>1)throw new InvalidDataException("Điểm phân loại không hợp lệ.");total+=score.softmax_score;}
                if(Mathf.Abs(total-1)>.01f||Hash(ReadImage(selectedImage))!=requestHash)throw new InvalidDataException("Ảnh nguồn đã đổi. Chọn và phân tích lại.");
                Result=result;Status="Gợi ý: "+PatternLabel(result.predicted_pattern)+" • Giữ nguyên texture gốc khi áp lên trang phục.";StopWorker();
            }catch(Exception error){Result=null;StopWorker();Status=error.Message;}
        }
        public void Cancel(){if(Busy)Status="Đã hủy phân tích.";StopWorker();}
        void StopWorker()
        {
            if(worker!=null){try{if(!worker.HasExited){worker.Kill();worker.WaitForExit(1000);}}catch(InvalidOperationException){}catch(System.ComponentModel.Win32Exception){}finally{worker.Dispose();worker=null;stdout=stderr=null;}}
            // Remove only our known request files; never recursively delete a selected source directory.
            if(!string.IsNullOrEmpty(requestFolder))try{foreach(string file in Directory.GetFiles(requestFolder))File.Delete(file);Directory.Delete(requestFolder);}catch(IOException){}catch(UnauthorizedAccessException){}
            requestFolder=null;
        }
        static string Quote(string path){if(path.Contains("\""))throw new InvalidDataException("Đường dẫn có ký tự không hợp lệ.");return "\""+path+"\"";}
        static string Hash(byte[] value){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(value)).Replace("-","").ToLowerInvariant();}
        public static string PatternLabel(string value){switch(value){case "checkered pattern":return "Ô caro";case "dots pattern":return "Chấm bi";case "floral/plant pattern":return "Hoa / lá";case "geometric pattern":return "Hình học";case "plaid pattern":return "Kẻ plaid";case "striped pattern":return "Kẻ sọc";default:return value;}}
        static void ReleasePreview(Texture2D texture){if(Application.isPlaying)Destroy(texture);else DestroyImmediate(texture);}
        void OnDestroy(){StopWorker();if(Preview)ReleasePreview(Preview);}
    }
}
