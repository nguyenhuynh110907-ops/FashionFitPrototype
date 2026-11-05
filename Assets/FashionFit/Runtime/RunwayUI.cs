using UnityEngine;

namespace FashionFit
{
    public sealed class RunwayUI : MonoBehaviour
    {
        FashionFitApp app;
        GUIStyle title, label, small, button, tab, activeTab, section, textArea;
        Texture2D panel,referencePhoto;
        Vector2 scroll;
        bool styled;
        bool showMeshy,showImport,showReference;
        int page=1;
        float fps, elapsed;
        int frames;
        static readonly string[] FabricLabels={"Lụa nhẹ","Cotton","Len có cấu trúc"};

        public void Initialize(FashionFitApp owner) { app = owner; }
        public void ShowDoffingTrial(){page=1;scroll=Vector2.zero;}
        public void OpenVRPage(){page=3;scroll=Vector2.zero;}
        void Update() { frames++; elapsed += Time.unscaledDeltaTime; if (elapsed > .5f) { fps = frames / elapsed; frames = 0; elapsed = 0; } }

        void Styles()
        {
            if (styled) return; styled = true;
            panel = new Texture2D(1, 1); panel.SetPixel(0, 0, new Color(.048f, .054f, .059f, .985f)); panel.Apply();
            title = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold, normal = { textColor = new Color(.96f, .91f, .80f) } };
            label = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true, normal = { textColor = new Color(.92f, .92f, .89f) } };
            small = new GUIStyle(label) { fontSize = 12, normal = { textColor = new Color(.69f, .72f, .71f) } };
            section = new GUIStyle(label) { fontSize = 12, fontStyle = FontStyle.Bold, normal = { textColor = new Color(.85f, .72f, .48f) } };
            button = new GUIStyle(GUI.skin.button) { fontSize = 13, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(12, 9, 8, 8), wordWrap = true };
            tab = new GUIStyle(GUI.skin.button) { fontSize = 11, alignment = TextAnchor.MiddleCenter, padding = new RectOffset(5, 5, 8, 8), wordWrap = true };
            activeTab = new GUIStyle(tab); activeTab.normal.textColor = new Color(.96f, .82f, .52f); activeTab.fontStyle = FontStyle.Bold;
            textArea = new GUIStyle(GUI.skin.textArea) { fontSize = 12, wordWrap = true, padding = new RectOffset(8, 8, 7, 7) };
        }

        bool Button(string text) { return GUILayout.Button(text, button, GUILayout.MinHeight(38)); }
        float Slider(string text, float value, float min, float max, string format = "0.000")
        {
            GUILayout.Label(text + "  " + value.ToString(format), small); return GUILayout.HorizontalSlider(value, min, max);
        }

        void TabButton(int index, string text)
        {
            if (GUILayout.Button(text, page == index ? activeTab : tab, GUILayout.MinHeight(38))) { page = index; scroll = Vector2.zero; }
        }

        void OnGUI()
        {
            if (app == null || !app.Ready || app.Player.InVR) return;
            Styles();
            float scale = Mathf.Max(.58f, Mathf.Min(Screen.width / 1280f, Screen.height / 800f));
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);
            float w = Screen.width / scale, h = Screen.height / scale, side = w * .30f;
            if (!app.Player.PanelVisible)
            {
                if (GUI.Button(new Rect(16, 16, 220, 38), "TAB  ·  Mở điều khiển", button)) app.Player.PanelVisible = true;
                GUI.matrix = Matrix4x4.identity; return;
            }

            GUI.DrawTexture(new Rect(0, 0, side, h), panel); GUI.DrawTexture(new Rect(side, h - 68, w - side, 68), panel);
            GUILayout.BeginArea(new Rect(18, 14, side - 36, h - 28));
            GUILayout.Label("FASHION UNITY", title);
            GUILayout.Label(app.ModelLabel + "  ·  " + fps.ToString("0") + " FPS", small);
            GUILayout.Label("TRÌNH DIỄN ×"+app.Speed.ToString("0.00")+" · Vải "+app.Wardrobe.ClothMilliseconds.ToString("0.0")+" ms",small);
            GUILayout.Space(8);
            GUILayout.BeginHorizontal(); TabButton(0, "BẮT ĐẦU"); TabButton(1, "TRANG PHỤC"); GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(); TabButton(2, "VẢI / AI"); TabButton(3, "VR / CAMERA"); GUILayout.EndHorizontal();
            GUILayout.Space(10);
            scroll = GUILayout.BeginScrollView(scroll, false, false);
            if (page == 0) DrawMotion();
            else if (page == 1) DrawWardrobe();
            else if (page == 2) DrawMaterial();
            else DrawCameraVR();
            GUILayout.Space(22); GUILayout.EndScrollView(); GUILayout.EndArea();

            GUI.Label(new Rect(side + 20, 18, w - side - 40, 28), app.MotionWarmingUp ? "ỔN ĐỊNH VẢI TRƯỚC KHI BƯỚC" : app.Stopping ? "HOÀN TẤT BƯỚC CHẬM" : app.ClothSettling ? "VẢI ĐANG SETTLE" : app.FittingPose ? "ĐỨNG YÊN · FIT TRANG PHỤC" : app.Playing ? "TRÌNH DIỄN ×"+app.Speed.ToString("0.00") : "ĐÃ ỔN ĐỊNH", section);
            GUI.Label(new Rect(side + 20, h - 62, w - side - 40, 30), app.Status, label);
            GUI.Label(new Rect(side + 20, h - 30, w - side - 40, 22), "W/S: tiến–lùi · A/D: đi quanh mannequin · Chuột phải: nhìn tự do · F: khóa nhìn · R: reset", small);
            GUI.matrix = Matrix4x4.identity;
        }

        void DrawMotion()
        {
            GUILayout.Label("BẮT ĐẦU NHANH", section);GUILayout.Space(5);
            GUILayout.Label("1",title);if(Button("Chọn một trang phục")){page=1;scroll=Vector2.zero;}
            if(Button("Thử mẫu vải và AI hoa văn")){page=2;scroll=Vector2.zero;}
            GUILayout.Label("2",title);if(Button(app.Playing?"Dừng motion và xem vải settle":"Chạy motion thử trang phục"))app.TogglePlay();
            GUILayout.Label("3",title);if(Button("Chuẩn bị và kiểm tra VR")){page=3;scroll=Vector2.zero;}
            GUILayout.Space(16);GUILayout.Label("MOTION VÀ ỔN ĐỊNH VẢI",section); GUILayout.Space(6);
            if(Button("▶  Đi bộ mặc định · 0,50×"))app.StartPresentationAt(FashionFitApp.DefaultMotionSpeed);
            if(Button("▶  Chậm để xem vải · 0,18×"))app.StartSlowPresentation();
            if(Button("▶  Nhanh hơn · 0,75×"))app.StartPresentationAt(FashionFitApp.FasterMotionSpeed);
            if(Button("▶  Đi bình thường · 1,00×"))app.StartPresentationAt(FashionFitApp.MaximumMotionSpeed);
            if (Button(app.Playing ? "■  Dừng motion — vải vẫn chịu trọng lực" : "▶  Tiếp tục motion")) app.TogglePlay();
            if (Button("Tư thế fitting — đứng yên và settle")) app.StandForFitting();
            if (Button("Khởi động lại chu kỳ")) app.RestartShow();
            if (Button("Đưa mannequin về giữa  ·  R")) app.ResetPresentation();
            GUILayout.Space(10);
            GUILayout.Label(app.ClothSettling ? "Vải đang rơi và giảm dao động…" : app.FittingPose ? "Mannequin đang ở tư thế fitting." : "Chu kỳ: " + Mathf.RoundToInt(app.CyclePhase * 100) + "%", label);
            float phase = GUILayout.HorizontalSlider(app.CyclePhase, 0, 1);
            if (!app.FittingPose && Mathf.Abs(phase - app.CyclePhase) > .002f) app.Seek(phase);
            if (Button("Đi một khung hình")) app.StepFrame();
            app.Speed = Slider("Nhịp người mẫu ×", app.Speed, FashionFitApp.MinimumMotionSpeed, FashionFitApp.MaximumMotionSpeed, "0.00");
            bool coverage=GUILayout.Toggle(app.HideCoveredBody,"Ẩn da bên dưới vùng trang phục che kín",label);
            if(coverage!=app.HideCoveredBody){app.HideCoveredBody=coverage;app.RefreshBodyCoverage();}
            if(app.Wardrobe.ProxyPenetrations>0)GUILayout.Label("Cần kiểm tra tiếp xúc: "+app.Wardrobe.ProxyPenetrations+" điểm còn nằm trong proxy cơ thể.",small);
            GUILayout.Space(14);
            GUILayout.Label("MẸO", section);
            GUILayout.Label("Dừng motion không đóng băng vải ngay. Solver tiếp tục chạy để áo/váy rơi xuống và ổn định theo trọng lực.", small);
        }

        void DrawWardrobe()
        {
            GUILayout.Label("TRANG PHỤC · BẢN DỰNG THỬ", section);
            if(Button(app.PoseStudio.HandlesVisible?"POSE — đổi sang kéo VẢI":"VẢI — đổi sang chỉnh POSE"))app.PoseStudio.ToggleHandles();
            if(Button(GarmentContactWorld.Get(app).ManualDressing?"Về phom ban đầu · kết thúc thử vật lý":"Mặc/cởi bằng Grip · thử vật lý")){app.PoseStudio.SetHandles(false);GarmentContactWorld.Get(app).ToggleManualDressing();}
            GUILayout.Label(app.Wardrobe.GarmentName, label); GUILayout.Space(5);
            GUILayout.Space(8);GUILayout.Label("ÁO CÀI NÚT · BẢN THỬ VR",section);
            GUILayout.Label("HOLLISTER NAM · 5 BẢN THAM CHIẾU",section);for(int i=0;i<5;i++){int index=i;if(Button((i+1).ToString("00")+" · "+MensReferenceCollection.Names[i]))MensReferenceCollection.Wear(app,index);}
            GUILayout.Label("NHÂN VẬT NHẬN TRANG PHỤC",section);GUILayout.BeginHorizontal();for(int target=0;target<3;target++){int selected=target;if(Button((app.Showroom.OutfitTarget==target?"✓ ":"")+(target==0?"Mẫu đi bộ":target==1?"Pose nữ":"Pose nam")))app.Showroom.SelectOutfitTarget(selected);}GUILayout.EndHorizontal();
            GUILayout.Label("Chọn đích rồi chọn đồ trong tủ/menu. Thả nón/đồ gần mẫu sẽ chọn mẫu gần nhất.",small);
            if(Button(app.MaterialLab.HatMode?"Đối tượng vật liệu: NÓN — đổi sang đồ":"Đối tượng vật liệu: TRANG PHỤC — đổi sang nón"))app.MaterialLab.ToggleFocus();
            if(Button("Đổi vật liệu: "+app.MaterialLab.ProfileLabel))app.MaterialLab.CycleProfile();
            if(Button("Đổi cỡ nón đang đội: "+app.MaterialLab.SizeLabel))app.MaterialLab.CycleHatSize();
            GUILayout.Label(app.MaterialLab.Summary,small);if(Button("Bảng thông số kéo / nén rõ ràng")){app.Player.PreviewVRMenu();app.Player.VRMenu.SelectPage(12);}GUILayout.Label(app.MaterialLab.DetailedReadout,small);
            GUILayout.Label("ÁO DÀI · CÁC TÀ VÀ LỚP VẢI",section);
            if(Button("Mặc áo dài cho nhân vật đã chọn"))app.LayerLab.WearSelected();
            if(app.LayerLab.Current){var ao=app.LayerLab.Current;if(Button("Lớp đang chọn: "+ao.LayerLabel))app.LayerLab.CycleLayer();if(Button("Đổi chiều dài tà: "+ao.LengthScale.ToString("F2")))app.LayerLab.CycleLength();if(Button("Đổi rộng tà: "+ao.WidthScale.ToString("F2")))app.LayerLab.CycleWidth();if(Button(ao.ArrangementMode?"Ghim điểm tà — đổi sang rủ tự nhiên":"Rủ tự nhiên — đổi sang ghim điểm"))ao.ToggleArrangement();if(Button("Đặt lại tà và bỏ điểm ghim"))ao.ResetDrape();GUILayout.Label(ao.Summary,small);}
            GUILayout.Label("Grip gần để kéo áo/nón đang đội. Trigger + Grip để tháo nón. Ẩn chấm khớp để dễ chỉnh vải.",small);
            if(Button("Mặc áo blouse cài 4 nút"))app.Wardrobe.WearDoffableBlouse();
            var blouse=app.Wardrobe.Blouse;
            if(blouse){
                GUILayout.Label(blouse.OpenCount+"/4 nút đã mở · "+blouse.Hint,small);
                if(!app.Player.InVR){GUILayout.BeginHorizontal();for(int i=0;i<4;i++){int k=i;if(Button((blouse.Solver.Closed[k]?"Mở ":"Cài ")+(k+1)))blouse.ToggleFastener(k);}GUILayout.EndHorizontal();}
                if(Button("Bắt đầu cởi · cần mở đủ 4 nút"))blouse.BeginDoff();
                if(Button("Đặt lại áo lên mẫu · cài 4 nút"))blouse.ResetOnBody();
            }
            if(Button("Dọn các áo đã cởi và thả"))app.Wardrobe.ClearFreeBlouses();
            var detailed=app.Wardrobe.TransferCatalog;
            if(detailed.Length>0){GUILayout.Label("TRANG PHỤC 3D · GIỮ CHI TIẾT GỐC",section);foreach(var package in detailed)if(Button("Mặc thử 3D · "+package.displayName))app.Wardrobe.WearTransferred(package);if(app.Wardrobe.HasDetailedTransfer)GUILayout.Label("Kiểm tra đứng yên: giữ UV, vật liệu và các phần mesh. Chưa cho đi hoặc kéo vải VR.",small);}
            if(app.Look>0)GUILayout.Label(BodyFitProfile.Description((DressPreset)(app.Look-1)),small);
            if (Button((app.Showroom.TargetLook==1?"✓  ":"")+"01  ·  Atelier Midi")) app.Showroom.WearLook(1);
            if (Button((app.Showroom.TargetLook==2?"✓  ":"")+"02  ·  Emerald Column — váy kín")) app.Showroom.WearLook(2);
            if (Button((app.Showroom.TargetLook==3?"✓  ":"")+"03  ·  Rose Couture — dáng xòe")) app.Showroom.WearLook(3);
            if (Button((app.Showroom.TargetLook==4?"✓  ":"")+"04  ·  Sapphire Cocktail")) app.Showroom.WearLook(4);
            if (Button((app.Showroom.TargetLook==5?"✓  ":"")+"05  ·  Ivory Blouse Set — áo VR")) app.Showroom.WearLook(5);
            if (Button((app.Showroom.TargetLook==6?"✓  ":"")+"06  ·  Gucci 864361 — thử từ ảnh 2D")){app.Wardrobe.ClearMaps();app.Showroom.WearLook(6);}
            if(app.Look==6){GUILayout.Label("Tham chiếu Gucci: cổ gập, tay ngắn, nẹp và bốn nút. Bản dựng thử, không phải mesh gốc của hãng.",small);
                GUILayout.Label("Thử phom: giữ tay và thân trên ổn định; chân và váy vẫn trình diễn chậm.",small);
                if(Button("Nhìn gần cổ áo và nẹp nút"))app.Player.FocusTailoring();
                if(Button("Xem phom · vải sáng")){app.Wardrobe.ClearMaps();app.Wardrobe.FabricColor=new Color(.84f,.82f,.78f);app.Wardrobe.Roughness=.86f;app.Wardrobe.UpdateMaterial();}
                if(Button("Bỏ đồ · xem phom mannequin"))app.SetLook(0);
                if(Button(showReference?"Ẩn ảnh tham chiếu 2D":"Xem ảnh tham chiếu 2D"))showReference=!showReference;
                if(showReference){if(!referencePhoto)referencePhoto=Resources.Load<Texture2D>("Fitting/gucci864361-reference");if(referencePhoto){Rect photo=GUILayoutUtility.GetRect(120,210,GUILayout.ExpandWidth(true));GUI.DrawTexture(photo,referencePhoto,ScaleMode.ScaleToFit);}GUILayout.Label("Ảnh sản phẩm Gucci · nguồn lưu trong hướng dẫn. Phần khuất của bản 3D được ước lượng.",small);}
                if(Button(app.Wardrobe.TailoredDetailsVisible?"Ẩn cổ/nẹp/nút để so sánh":"Hiện cổ/nẹp/nút 3D"))app.Wardrobe.ToggleTailoredDetails();
                if(Button("Khôi phục màu đen theo ảnh"))app.Wardrobe.ClearMaps();}

            if (app.Wardrobe.HasInteractiveShirt)
            {
                GUILayout.Space(14); GUILayout.Label("CHỈNH ÁO", section);
                GUILayout.Label("Áo dùng lưới vật lý nhẹ và bề mặt hiển thị riêng. Trong VR: giữ Grip trái / phải rồi kéo; thả ra để áo rơi và settle.", small);
                float loose = Slider("Độ rộng áo", app.Wardrobe.ShirtLooseness, .94f, 1.16f, "0.00");
                float length = Slider("Chiều dài áo", app.Wardrobe.ShirtLength, .94f, 1.12f, "0.00");
                app.Wardrobe.SetShirtFit(loose, length);
                if (Button("Đặt lại áo")) app.Wardrobe.ResetShirt();
            }

            GUILayout.Space(14);
            if(Button((showMeshy?"▼  ":"▶  ")+"Nâng cao: tạo 3D từ ảnh bằng Meshy"))showMeshy=!showMeshy;
            if(showMeshy)
            {
                GUILayout.Label("Ảnh chỉ được tải lên khi bấm Tạo 3D; thao tác dùng credit Meshy.",small);
                MeshyGarmentClient meshy=app.Meshy;GUILayout.Label("Meshy API key — chỉ giữ trong RAM",small);
                meshy.SetApiKey(GUILayout.PasswordField(meshy.GetApiKeyForPasswordField(),'*',96,GUI.skin.textField));
                if(Button("Chọn ảnh  ·  "+meshy.ImageLabel))meshy.ChooseImage();GUI.enabled=!meshy.IsBusy;
                if(Button(meshy.IsBusy?"Meshy đang tạo 3D…":"Tạo smart-topology OBJ và nhập"))meshy.Generate();GUI.enabled=true;
            }
            if(Button((showImport?"▼  ":"▶  ")+"Nâng cao: nhập OBJ thủ công"))showImport=!showImport;
            if(showImport||app.Wardrobe.HasPending){if(Button("Chọn tệp OBJ"))app.Wardrobe.ChooseOBJ();DrawPendingGarment();}
            if (Button("Bỏ trang phục đang mặc")) app.Wardrobe.ClearGarment();
            GUILayout.Label("Mẫu 01–05 là thiết kế procedural. Mẫu 06 dựng nghiên cứu từ ảnh Gucci 864361; phần khuất ước lượng. OBJ nhập ngoài vẫn là bản xem trước hình học.", small);
        }

        void DrawMaterial()
        {
            GUILayout.Label("CHẤT LIỆU VÀ ĐỘ RỦ",section);
            bool enabled=GUI.enabled;GUI.enabled=enabled&&app.Wardrobe.IsSample;
            GUILayout.BeginHorizontal();for(int i=0;i<FabricLabels.Length;i++)if(Button((app.Wardrobe.PhysicsMaterialIndex==i?"✓ ":"")+FabricLabels[i]))app.Wardrobe.SetPhysicsMaterial(i);GUILayout.EndHorizontal();
            GUILayout.Label("Đổi chất liệu để so sánh độ rủ, độ nảy và ma sát. Màu, hoa văn đang dùng được giữ lại.",small);
            if(Button(app.Wardrobe.AirDemo?"Tắt quạt nhẹ":"Bật quạt nhẹ · xem vải chuyển động"))app.Wardrobe.SetAirDemo(!app.Wardrobe.AirDemo);
            GUI.enabled=enabled;
            GUILayout.Space(12);
            DrawFabricTrial();
            RunwayWardrobe wardrobe = app.Wardrobe;
            GUILayout.Label("BỀ MẶT VẢI", section);
            float rough = Slider("Độ nhám", wardrobe.Roughness, .2f, .95f, "0.00");
            if (Mathf.Abs(rough - wardrobe.Roughness) > .001f) { wardrobe.Roughness = rough; wardrobe.UpdateMaterial(); }
            if (Button("Chọn base color  ·  PNG/JPG")) wardrobe.ChooseMap(false);
            if (Button("Chọn normal map RGB +Y")) wardrobe.ChooseMap(true);
            float tiling = Slider("Số lần lặp texture", wardrobe.MapScale, .25f, 10, "0.00");
            if (Mathf.Abs(tiling - wardrobe.MapScale) > .001f) { wardrobe.MapScale = tiling; wardrobe.UpdateMaterial(); }
            if (Button("Bỏ texture đã nhập / AI")) wardrobe.ClearMaps();

            GUILayout.Space(16); GUILayout.Label("AI FABRIC STUDIO", section);
            GUILayout.Label("Nano Banana chỉ tạo texture 2D. Mesh, fitting và vật lý vẫn chạy trong Unity.", small);
            NanoBananaClient ai = app.AI;
            GUILayout.Label("API key — chỉ giữ trong RAM", small);
            ai.SetApiKey(GUILayout.PasswordField(ai.GetApiKeyForPasswordField(), '*', 96, GUI.skin.textField));
            GUILayout.Label("Mô tả chất liệu / hoa văn", small);
            ai.Prompt = GUILayout.TextArea(ai.Prompt, textArea, GUILayout.MinHeight(68));
            GUI.enabled = !ai.IsBusy;
            if (Button(ai.IsBusy ? "Đang tạo mẫu vải…" : "Tạo và áp mẫu vải  ·  Nano Banana")) ai.GenerateFabric();
            GUI.enabled = true; GUILayout.Label(ai.ModelLabel + "  ·  cần Internet và API quota", small);
        }

        void DrawFabricTrial()
        {
            FabricDetailTrial trial=app.FabricTrial;if(trial==null)return;
            GUILayout.Label("DÙNG THỬ · CHI TIẾT VẢI",section);
            GUILayout.Label("Chọn mẫu → Áp lên trang phục. Màu, vân nổi và độ nhám dùng đúng UV hiện có.",small);
            for(int i=0;i<trial.Samples.Length;i+=2){
                GUILayout.BeginHorizontal();
                for(int j=i;j<Mathf.Min(i+2,trial.Samples.Length);j++)if(Button(trial.Samples[j].label))trial.SelectSample(j);
                GUILayout.EndHorizontal();
            }
            if(Button("Chọn ảnh texture của bạn"))trial.ChooseTexture();
            if(trial.Preview){GUILayout.Label(trial.SelectedLabel,label);Rect rect=GUILayoutUtility.GetRect(120,120,GUILayout.ExpandWidth(true));GUI.DrawTexture(rect,trial.Preview,ScaleMode.ScaleToFit);}
            GUI.enabled=trial.HasSelection;
            if(Button("Áp mẫu đã chọn lên trang phục"))trial.ApplySelected();
            GUI.enabled=app.Wardrobe.HasSurfaceMaps;
            if(Button(app.Wardrobe.MapsVisible?"So sánh · xem vật liệu mặc định":"So sánh · xem mẫu vải đã áp"))app.Wardrobe.ToggleMapComparison();
            GUI.enabled=trial.HasSelection&&trial.CanAnalyze&&!trial.Busy;
            if(Button(trial.Busy?"Đang nhận diện…":"AI · nhận diện hoa văn đã chọn"))trial.Analyze();
            GUI.enabled=true;
            if(trial.Busy){GUILayout.Label("Đang phân tích · "+trial.RunningSeconds.ToString("0")+" giây",small);if(Button("Hủy nhận diện"))trial.Cancel();}
            GUILayout.Label(trial.Status,small);
            if(trial.Result!=null){
                for(int i=0;i<Mathf.Min(3,trial.Result.scores.Length);i++){var score=trial.Result.scores[i];GUILayout.Label(FabricDetailTrial.PatternLabel(score.pattern)+" · điểm "+score.softmax_score.ToString("P1"),label);}
            }
            GUILayout.Label("AI gợi ý 6 nhóm hoa văn; có thể nhầm với vải trơn. Điểm số không phải độ tin cậy đã hiệu chuẩn. AI không tự sửa cổ áo hay hình dáng.",small);
            if(!trial.CanAnalyze)GUILayout.Label("Nhận diện AI dùng môi trường cục bộ của dự án trong Unity Editor. Có thể thử các mẫu vải mà không chạy AI.",small);
            GUILayout.Label("Mẫu vải: ambientCG · CC0",small);GUILayout.Space(16);
        }

        void DrawCameraVR()
        {
            GUILayout.Label("BẮT ĐẦU VR · QUEST LINK",section);
            GUILayout.Label("1. Cắm kính và Launch Link trong kính.\n2. Chuẩn bị phiên thử bên dưới.\n3. Bấm Vào VR khi Link đã hoạt động.",small);
            if(Button("Chuẩn bị phiên VR · đứng yên / đồ họa nhẹ"))app.Player.PrepareForVRTrial();
            GUILayout.Label(app.Player.VRReadiness,app.Player.OpenXRConfigured?label:small);
            if(Button("Kiểm tra cấu hình OpenXR"))app.Player.CheckVRSetup();
            GUI.enabled=!app.Player.StartingVR;
            if(Button(app.Player.StartingVR?"Đang kết nối…":app.Player.InVR?"Thoát VR · về desktop":"Vào VR · OpenXR / Quest Link"))app.Player.ToggleVR();GUI.enabled=true;
            if(Button("Xem thử giao diện VR trên màn hình"))app.Player.PreviewVRMenu();
            GUILayout.Label("Lần đầu: đứng yên, thả các nút/cần về giữa; sau đó thử quay đầu, Y mở menu và Trigger chọn. Grip là nút bên hông tay cầm.",small);GUILayout.Space(14);
            GUILayout.Label("MẪU NAM / NỮ · CHỈNH KHỚP",section);
            if(Button("Mẫu nữ 3D · nhìn gần và chỉnh khớp"))app.PoseStudio.Focus(0);
            if(Button("Mẫu nam 3D · nhìn gần và chỉnh khớp"))app.PoseStudio.Focus(1);
            if(Button(app.PoseStudio.HandlesVisible?"Ẩn điểm khớp · giữ tư thế":"Hiện điểm khớp · Grip để chỉnh"))app.PoseStudio.ToggleHandles();
            GUILayout.BeginHorizontal();if(Button("Đặt lại mẫu nữ"))app.PoseStudio.ResetPose(0);if(Button("Đặt lại mẫu nam"))app.PoseStudio.ResetPose(1);GUILayout.EndHorizontal();
            GUILayout.Label("Điểm cổ tay/cổ chân: kéo để gập tay/chân. Vai, khuỷu, gối, thân, cổ/đầu: kéo nhẹ hoặc xoay tay. Desktop: giữ E; VR: Grip trái / phải. Hai mẫu thử độc lập với mannequin đang mặc đầm.",small);GUILayout.Space(14);
            GUILayout.Label("THỬ ĐIỀU KHIỂN",section);
            GUILayout.Label("DI CHUYỂN TỰ DO TRONG KÍNH",section);
            GUILayout.Label("Cần trái: tiến/lùi/ngang theo hướng nhìn, không khóa quanh mannequin. Quay đầu hoặc bước thật trong vùng chơi để đổi góc nhìn.",small);
            app.Player.VRWalkSpeed=Slider("Tốc độ đi VR · m/s",app.Player.VRWalkSpeed,.5f,2.2f,"0.00");
            if(Button(app.Player.VRSnapTurn?"Đang quay 45° · đổi sang quay mượt":"Đang quay mượt · đổi sang quay 45°"))app.Player.ToggleVRTurn();
            GUILayout.Label("Trong kính: Y trái mở/ẩn menu; X trái đưa menu về trước. Tab CÀI ĐẶT đổi cách quay, tốc độ và đồ họa. Khi mất tracking, thả hai cần về giữa trước khi tiếp tục.",small);
            if(app.Player.InVR)GUILayout.Label(app.Player.ControllerStatus,small);
            GUILayout.Label("Cần trái: đi\nCần phải: quay\nTrigger phải: bấm bảng\nGrip trái / phải: kéo mannequin, váy hoặc áo",label);
            GUILayout.Label("Grip trái / phải: lấy nón/mẫu đồ từ bàn/tủ. Đưa gần đầu/thân mannequin rồi thả để đội/mặc. Thả xa mannequin: vật rơi xuống bàn/sàn.",small);
            GUILayout.Label("Menu có 3 tab: THỬ ĐỒ, TRANG PHỤC, CÀI ĐẶT. Grip để lấy vật sẽ ẩn menu; Y mở lại. Vật ở xa được kéo về tay khi giữ Grip.",small);
            GUILayout.Space(12);GUILayout.Label("BÀN NÓN / TỦ TRANG PHỤC",section);
            if(Button("Xem bàn nón và tủ đồ"))app.Player.FocusShowroom();
            GUILayout.Label("Desktop: trỏ chuột vào vật trong Game, giữ E để giả lập Grip. Thả E để đặt. Có thể đội/chọn đồ nhanh bên dưới.",small);
            GUILayout.Label("10 NÓN THEO ẢNH THAM CHIẾU",section);
            if(Button("Nhìn gần nón đang đội · desktop"))app.Player.FocusHat();
            for(int i=0;i<ReferenceHatCatalog.Labels.Length;i++)if(Button(ReferenceHatCatalog.Labels[i]))app.Showroom.WearHat(i);
            for(int i=0;i<6;i++)if(Button((i+1).ToString("00")+" · Lấy "+RunwayShowroom.LookNames[i]+" từ tủ"))app.Showroom.WearLook(i+1);
            if(Button("Trả nón / mẫu đồ về bàn và tủ")){app.Player.EndActiveGrab();app.Showroom.ResetProps();}
            if(Button(app.Player.LowQuality?"Đồ họa đang nhẹ · chuyển chi tiết":"Đồ họa đang chi tiết · chuyển nhẹ"))app.Player.ToggleQuality();

            GUILayout.Space(18);GUILayout.Label("CAMERA DESKTOP", section);
            GUILayout.Label("W/S tiến–lùi, A/D di chuyển vòng quanh mannequin. Camera tự nhìn lại mannequin và có vùng chặn để không đi xuyên cơ thể.", small);
            GUILayout.BeginHorizontal(); if (Button("↶ Trái")) app.Player.Orbit(-15); if (Button("Phải ↷")) app.Player.Orbit(15); GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal(); if (Button("Tiến gần")) app.Player.Dolly(.25f); if (Button("Lùi xa")) app.Player.Dolly(-.25f); GUILayout.EndHorizontal();
            if (Button("Đổi góc trước / trái / sau / phải")) app.Player.NextView();
            if (Button(app.Player.FollowModel ? "Đang khóa nhìn mannequin  ·  tắt" : "Khóa nhìn vào mannequin")) { app.Player.FollowModel = !app.Player.FollowModel; if (app.Player.FollowModel) app.Player.LookAtModel(); }

            GUILayout.Label(SystemInfo.graphicsDeviceName + "\n" + SystemInfo.graphicsMemorySize + " MB  ·  " + fps.ToString("0") + " FPS", small);
        }

        void DrawPendingGarment()
        {
            RunwayWardrobe wardrobe = app.Wardrobe; if (!wardrobe.HasPending) return;
            GUILayout.Space(8); GUILayout.Label(wardrobe.ImportSummary, small); GUILayout.Label("Đơn vị tệp gốc", label);
            GUILayout.BeginHorizontal(); if (Button("m")) wardrobe.SetUnit(1); if (Button("cm")) wardrobe.SetUnit(.01f); if (Button("mm")) wardrobe.SetUnit(.001f); GUILayout.EndHorizontal();
            Vector3 size = wardrobe.PendingSize; GUILayout.Label("Rộng / cao / sâu: " + size.x.ToString("0.000") + " / " + size.y.ToString("0.000") + " / " + size.z.ToString("0.000") + " m", small);
            wardrobe.ShowPending = GUILayout.Toggle(wardrobe.ShowPending, "Hiện bản gốc để kiểm tra"); wardrobe.RefreshPreview();
            bool plausible = size.y >= .05f && size.y <= 1.7f && size.x <= 3 && size.z <= 3;
            if (plausible)
            {
                wardrobe.WaistY = Slider("Mốc eo Y (m)", wardrobe.WaistY, -.5f, 2); wardrobe.HipY = Slider("Mốc hông Y (m)", wardrobe.HipY, -.5f, 2);
                wardrobe.CenterX = Slider("Tâm X (m)", wardrobe.CenterX, -.5f, .5f); wardrobe.CenterZ = Slider("Tâm Z (m)", wardrobe.CenterZ, -.5f, .5f);
                wardrobe.AdaptToBody = GUILayout.Toggle(wardrobe.AdaptToBody, "Căn phom theo eo/hông"); if (wardrobe.AdaptToBody) wardrobe.Ease = Slider("Khoảng rộng (m)", wardrobe.Ease, .005f, .04f);
                wardrobe.FitX = Slider("Dịch X (m)", wardrobe.FitX, -.15f, .15f); wardrobe.FitY = Slider("Dịch Y (m)", wardrobe.FitY, -.15f, .15f); wardrobe.FitZ = Slider("Dịch Z (m)", wardrobe.FitZ, -.15f, .15f);
                wardrobe.LandmarksConfirmed = GUILayout.Toggle(wardrobe.LandmarksConfirmed, "Đã kiểm tra đơn vị và mốc váy");
                if (Button("Mặc bản sao đã căn")) wardrobe.ApplyPending();
            }
            else GUILayout.Label("Kích thước chưa hợp lý; hãy chọn lại m / cm / mm.", small);
            if (Button("Bỏ bản nhập đang chờ")) wardrobe.DiscardPending();
        }

        void OnDestroy() { if (panel) Destroy(panel); }
    }
}
