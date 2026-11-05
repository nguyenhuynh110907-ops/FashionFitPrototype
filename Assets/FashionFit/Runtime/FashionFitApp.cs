using System;
using UnityEngine;

namespace FashionFit
{
    [DefaultExecutionOrder(-100)]
    public sealed class FashionFitApp : MonoBehaviour
    {
        [Header("Optional original female model (not the reference photograph)")]
        public GameObject FemaleAvatarPrefab;
        public RuntimeAnimatorController WalkController;
        public string WalkState = "Base Layer.Walk";

        public RunwayAvatar Model { get; private set; }
        public RunwayPlayer Player { get; private set; }
        public RunwayWardrobe Wardrobe { get; private set; }
        public RunwayShowroom Showroom { get; private set; }
        public LayeredWardrobe LayerLab {get;private set;}
        public RunwayMaterialLab MaterialLab {get;private set;}
        public RunwayPoseStudio PoseStudio { get; private set; }
        public NanoBananaClient AI { get; private set; }
        public MeshyGarmentClient Meshy { get; private set; }
        public FabricDetailTrial FabricTrial { get; private set; }
        public bool Playing = false;
        public bool FittingPose { get; private set; }
        public bool ClothSettling { get { return !Playing && (settleRemaining > 0 || stopping); } }
        public bool Stopping { get { return stopping; } }
        public const float SlowMotionSpeed = .18f, DefaultMotionSpeed = .50f, MinimumMotionSpeed = .05f, FasterMotionSpeed = .75f, MaximumMotionSpeed = 1.00f, PresentationWalkBlend = .35f;
        public float Speed = DefaultMotionSpeed;
        public float CurrentMotionSpeed => currentMotionSpeed;
        public bool HideCoveredBody = false;
        public bool MotionWarmingUp { get { return Playing && motionWarmup > 0; } }
        public int Look;
        public string Status = "Đang chuẩn bị phòng thử đồ…";
        public string FatalError { get; private set; }

        double clock;
        const float Step = 1f / 60f;
        float accumulator, inspectionPhase, settleRemaining;
        float walkBlend,quietTime,motionWarmup,currentMotionSpeed;
        int contactFrames;
        double stopAt;
        bool stopping;
        Vector3 modelLocalPosition, modelGrabOffset;
        float modelYaw, modelVerticalVelocity, grabStartHandYaw, grabStartModelYaw;
        bool modelGrabbed,coverageSuspended,legacyWakeDuringBlouse;
        // Custom solvers/dictionaries do not survive an Editor script reload.
        // Unity may restore component references; those alone do not mean the session is ready.
        [NonSerialized] bool runtimeInitialized;

        public const float CycleSeconds = 1.2f;
        public float ShowTime { get { return (float)clock; } }
        public float CyclePhase { get { return !Playing && !FittingPose ? inspectionPhase : Mathf.Repeat(ShowTime / CycleSeconds, 1); } }
        public bool Ready { get { return runtimeInitialized && Model != null && Player != null && Wardrobe != null && Wardrobe.SimulationReady; } }
        public string ModelLabel { get { return Model != null && Model.IsOriginal ? "NHÂN VẬT NỮ CỦA BẠN" : "MA-NƠ-CANH NỮ TẠM"; } }

        void Start()
        {
            try
            {
                Application.targetFrameRate = 60; QualitySettings.vSyncCount = 1; QualitySettings.antiAliasing = 4;
                QualitySettings.shadowDistance = 10; QualitySettings.shadows = ShadowQuality.All; QualitySettings.shadowResolution = ShadowResolution.Medium; QualitySettings.pixelLightCount = 4;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = new Color(.58f, .55f, .50f); RenderSettings.ambientEquatorColor = new Color(.36f, .37f, .37f); RenderSettings.ambientGroundColor = new Color(.18f, .17f, .16f); RenderSettings.reflectionIntensity = .72f; RenderSettings.fog = false;
                RunwayStage.Build(transform);

                Model = new GameObject("Female fitting model - movable VR anchor").AddComponent<RunwayAvatar>();
                Model.transform.SetParent(transform, false); Model.Load(FemaleAvatarPrefab, WalkController, WalkState);
                BuildMannequinGrabVolume();

                Wardrobe = gameObject.AddComponent<RunwayWardrobe>(); Wardrobe.Initialize(this);
                Showroom=new GameObject("Atelier hat table and wardrobe").AddComponent<RunwayShowroom>();Showroom.transform.SetParent(transform,false);Showroom.Initialize(this);
                PoseStudio=new GameObject("Male and female pose studio").AddComponent<RunwayPoseStudio>();PoseStudio.transform.SetParent(transform,false);PoseStudio.Initialize(this);
                AI = gameObject.AddComponent<NanoBananaClient>(); AI.Initialize(this);
                Meshy = gameObject.AddComponent<MeshyGarmentClient>(); Meshy.Initialize(this);
                FabricTrial = gameObject.AddComponent<FabricDetailTrial>(); FabricTrial.Initialize(this);
                Player = new GameObject("Observer rig").AddComponent<RunwayPlayer>(); Player.transform.SetParent(transform, false); Player.Initialize(this);
                LayerLab=gameObject.AddComponent<LayeredWardrobe>();LayerLab.Initialize(this);MaterialLab=gameObject.AddComponent<RunwayMaterialLab>();MaterialLab.Initialize(this);gameObject.AddComponent<RunwayUI>().Initialize(this);

                runtimeInitialized=true;
                Speed=DefaultMotionSpeed; SetPose(); SetLook(6); StandForFitting();PoseStudio.DressDefaults();
                Report("Mặc định đứng yên • Bấm Đi bộ để trình diễn ở nhịp 0,50×");
                Debug.Log("FITTING_READY_V092_CLOSE_FIT | GPU: " + SystemInfo.graphicsDeviceName + " | " + SystemInfo.graphicsMemorySize + " MB | " + SystemInfo.operatingSystem);
            }
            catch (Exception e) { runtimeInitialized=false;FatalError = e.Message; Debug.LogException(e); }
        }

        void BuildMannequinGrabVolume()
        {
            CapsuleCollider capsule = Model.gameObject.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0, .88f, 0); capsule.height = 1.72f; capsule.radius = .23f; capsule.direction = 1;
            Model.gameObject.layer = 9;
            Model.gameObject.AddComponent<RunwayVRGrabTarget>().ConfigureMannequin(this);
        }

        void Update()
        {
            AdvanceSimulation(Time.deltaTime);
        }

        public void AdvanceSimulation(float deltaTime)
        {
            if (!Ready) return;
            if(DoffableBlouse.Live.Count>0&&(!FittingPose||Playing||stopping)){StandForFitting();Report("Áo cài nút đang dùng tư thế fit đã kiểm tra · dọn áo thử để bật motion");}
            float frame = Mathf.Clamp(deltaTime,0,.05f);
            UpdatePresentationGravity(frame);

            if(DoffableBlouse.Live.Count>0&&FittingPose){
                // The independent blouse must not keep the large legacy skirt solver awake.
                // A real skirt grip / air demo still wakes it, including its settling tail.
                if(Wardrobe.LegacyClothGrabbed||Wardrobe.AirDemo)legacyWakeDuringBlouse=true;
                if(!Wardrobe.LegacyClothGrabbed&&!Wardrobe.AirDemo&&settleRemaining<=0)legacyWakeDuringBlouse=false;
                if(!legacyWakeDuringBlouse){accumulator=0;return;}
            }else legacyWakeDuringBlouse=false;
            bool simulate = Playing || stopping || settleRemaining > 0 || modelGrabbed || Wardrobe.LegacyClothGrabbed || Wardrobe.AirDemo;
            if (!simulate) return;
            // Gravity follows real seconds. Speed changes only the gait clock.
            accumulator = Mathf.Min(accumulator + frame, Step * 4);
            int steps = 0;
            while (accumulator >= Step && steps++ < 4)
            {
                accumulator -= Step;
                if(Playing&&!FittingPose)
                {
                    Speed=Mathf.Clamp(Speed,MinimumMotionSpeed,MaximumMotionSpeed);
                    if(motionWarmup>0)motionWarmup=Mathf.Max(0,motionWarmup-Step);
                    else {
                        currentMotionSpeed=Mathf.MoveTowards(currentMotionSpeed,Speed,Step*.30f);
                        clock+=Step*currentMotionSpeed;
                        walkBlend=Mathf.MoveTowards(walkBlend,PresentationWalkBlend,Step*.18f);
                        SetPose();
                    }
                }
                else if(stopping)
                {
                    if(clock<stopAt)clock=Math.Min(stopAt,clock+Step*Mathf.Clamp(currentMotionSpeed,MinimumMotionSpeed,MaximumMotionSpeed));
                    else walkBlend=Mathf.MoveTowards(walkBlend,0,Step*.18f);
                    SetPose();inspectionPhase=Mathf.Repeat(ShowTime/CycleSeconds,1);
                    if(walkBlend<=0){stopping=false;FittingPose=true;settleRemaining=1.5f;}
                }
                Wardrobe.StepCloth(Step);
                contactFrames=Wardrobe.ProxyPenetrations>0?contactFrames+1:0;
                if(contactFrames>=3&&(Playing||stopping))
                {
                    // Hold the current pose; snapping to rest would sweep the legs through cloth.
                    Playing=false;stopping=false;FittingPose=false;motionWarmup=0;settleRemaining=3;
                    inspectionPhase=Mathf.Repeat(ShowTime/CycleSeconds,1);
                    Report("Tạm giữ tư thế để xử lý tiếp xúc • Vải tiếp tục ổn định; có thể chọn Đứng yên để căn lại");
                }
                if(!Playing&&!stopping&&!modelGrabbed&&!Wardrobe.LegacyClothGrabbed)
                {
                    quietTime=Wardrobe.ClothSpeed<.018f?quietTime+Step:0;
                    settleRemaining=Mathf.Max(0,settleRemaining-Step);
                    if(settleRemaining<=0&&quietTime<.4f)settleRemaining=Step;
                }
            }
        }

        void UpdatePresentationGravity(float dt)
        {
            if (modelGrabbed || modelLocalPosition.y <= 0) return;
            modelVerticalVelocity = Mathf.Max(-.25f,modelVerticalVelocity-9.81f*dt);
            modelLocalPosition.y += modelVerticalVelocity * dt;
            if (modelLocalPosition.y <= 0) { modelLocalPosition.y = 0; modelVerticalVelocity = 0; }
            ApplyPresentation();
            settleRemaining = Mathf.Max(settleRemaining, 1.5f);
        }

        void ApplyPresentation()
        {
            if (Model == null) return;
            Model.transform.localPosition = modelLocalPosition;
            Model.transform.localRotation = Quaternion.Euler(0, modelYaw, 0);
        }

        void SetPose()
        {
            ApplyPresentation();
            Model.Animate(ShowTime, FittingPose ? 0 : walkBlend, 1);
        }

        public void TogglePlay()
        {
            if(LayerLab&&LayerLab.ForTarget(0)){StandForFitting();Report("Áo dài thử: giữ tư thế đứng để kiểm tra nhiều lớp trước khi bật bước đi");return;}if(BlockUnverifiedTransferMotion())return;
            if (Playing)
            {
                inspectionPhase = Mathf.Repeat(ShowTime / CycleSeconds, 1);
                Playing=false;stopping=true;quietTime=0;settleRemaining=2.5f;
                stopAt=motionWarmup>0?clock:(Math.Floor(clock/(CycleSeconds*.5))+1)*(CycleSeconds*.5);motionWarmup=0;
                Report("Đang kết thúc bước • Vải tiếp tục ổn định theo trọng lực");
                return;
            }
            if (FittingPose) FittingPose = false;
            Playing = true; stopping=false;quietTime=0;settleRemaining = 0; accumulator = 0;motionWarmup=.8f;contactFrames=0;currentMotionSpeed=0;
            Report("Ổn định vải trước khi bước • Nhịp trình diễn "+Speed.ToString("0.00")+"×");
        }

        public void StandForFitting()
        {
            if(Player)Player.EndActiveGrab();
            Playing = false;stopping=false;walkBlend=0;quietTime=0;motionWarmup=0;currentMotionSpeed=0;contactFrames=0; FittingPose = true; clock = 0; accumulator = 0; settleRemaining = 1.8f;
            SetPose(); Wardrobe.ResetCloth();coverageSuspended=false;RefreshBodyCoverage();
            Report("Tư thế fit • Cơ thể đứng yên, vải vẫn được thả rơi để ổn định");
        }

        public void RestartShow()
        {
            if(LayerLab&&LayerLab.ForTarget(0)){StandForFitting();Report("Áo dài thử: giữ tư thế đứng để kiểm tra nhiều lớp trước khi bật bước đi");return;}if(BlockUnverifiedTransferMotion())return;
            Playing = true;stopping=false;walkBlend=0;quietTime=0;motionWarmup=.8f;currentMotionSpeed=0;contactFrames=0; FittingPose = false; clock = 0; accumulator = 0; settleRemaining = 0;
            SetPose(); Wardrobe.ResetCloth();
        }

        public void Seek(float phase)
        {
            if(LayerLab&&LayerLab.ForTarget(0)){StandForFitting();Report("Áo dài thử: giữ tư thế đứng để kiểm tra nhiều lớp trước khi bật bước đi");return;}if(BlockUnverifiedTransferMotion())return;
            Playing = false;stopping=false;walkBlend=PresentationWalkBlend;quietTime=0;motionWarmup=0; FittingPose = false; inspectionPhase = Mathf.Clamp01(phase); clock = 0; accumulator = 0;
            SetPose(); Wardrobe.ResetCloth();
            int steps = Mathf.RoundToInt(Mathf.Clamp01(phase) * CycleSeconds / Step);
            for (int i = 0; i < steps; i++) { clock += Step; SetPose(); Wardrobe.StepCloth(Step); }
            settleRemaining = .8f;
            Report("Dừng motion tại " + Mathf.RoundToInt(phase * 100) + "% • Vải đang ổn định");
        }

        public void StepFrame() { Seek(Mathf.Min(1, CyclePhase + Step / CycleSeconds)); }
        public void SetLook(int index)
        {
            if(LayerLab)LayerLab.Clear(0);Look=Mathf.Clamp(index,0,6);
            if(Look==0)Wardrobe.ClearGarment();else Wardrobe.WearPreset((DressPreset)(Look-1));
            if(Showroom)Showroom.RefreshSelectedLook();
        }
        public void CycleLook(int direction)
        {
            int current=Look<=0?1:Look;current=((current-1+direction)%6+6)%6+1;SetLook(current);
        }

        public void BeginMannequinGrab(Vector3 worldHit, Quaternion handRotation)
        {
            if (Playing) inspectionPhase = Mathf.Repeat(ShowTime / CycleSeconds, 1);
            Playing = false;stopping=false;quietTime=0; FittingPose = false; modelGrabbed = true; modelVerticalVelocity = 0;
            Vector3 localHit = transform.InverseTransformPoint(worldHit);
            modelGrabOffset = modelLocalPosition - localHit;
            grabStartHandYaw = handRotation.eulerAngles.y; grabStartModelYaw = modelYaw;
            settleRemaining = 3f;
            Report("VR đang giữ mannequin • Di chuyển tay để kéo, xoay cổ tay để quay");
        }

        public void UpdateMannequinGrab(Vector3 worldPoint, Quaternion handRotation)
        {
            if (!modelGrabbed) return;
            Vector3 target = transform.InverseTransformPoint(worldPoint) + modelGrabOffset;
            target.x = Mathf.Clamp(target.x, -2.8f, 2.8f); target.z = Mathf.Clamp(target.z, -2.8f, 2.8f); target.y = Mathf.Clamp(target.y, 0, 1.2f);
            modelLocalPosition=Vector3.MoveTowards(modelLocalPosition,target,.25f*Mathf.Min(Time.unscaledDeltaTime,.05f));
            modelYaw=Mathf.MoveTowardsAngle(modelYaw,grabStartModelYaw+Mathf.DeltaAngle(grabStartHandYaw,handRotation.eulerAngles.y),25*Mathf.Min(Time.unscaledDeltaTime,.05f));
            ApplyPresentation(); settleRemaining = 3f;
        }

        public void EndMannequinGrab()
        {
            if (!modelGrabbed) return;
            modelGrabbed = false; modelVerticalVelocity = 0; settleRemaining = 3f;
            Report("Đã thả mannequin • Trọng lực đưa mannequin về sàn, vải tiếp tục ổn định");
        }

        public void NotifyClothGrab(bool grabbing)
        {
            if (grabbing && (Playing||stopping)) { inspectionPhase = Mathf.Repeat(ShowTime / CycleSeconds, 1); Playing = false;stopping=false; FittingPose = false; }
            quietTime=0;
            settleRemaining = grabbing ? 5f : 3f;
            if(grabbing)coverageSuspended=true;
            RefreshBodyCoverage();
            Report(grabbing ? "VR đang kéo vải • Va chạm cơ thể vẫn hoạt động" : "Đã thả vải • Đang rơi và ổn định theo trọng lực");
        }
        public void RequestClothSettle(){settleRemaining=Mathf.Max(settleRemaining,4);quietTime=0;}

        public void ResetPresentation()
        {
            if(DoffableBlouse.Live.Count>0){Report("Dùng Đặt lại áo lên mẫu hoặc Dọn áo rời trong menu áo cài nút");return;}
            modelGrabbed = false; modelLocalPosition = Vector3.zero; modelYaw = 0; modelVerticalVelocity = 0; ApplyPresentation();
            Wardrobe.ResetCloth(); settleRemaining = 1.5f;
            Report("Đã đưa mannequin và trang phục về vị trí trung tâm");
        }

        public void Report(string message) { Status = message; }
        public void RefreshBodyCoverage() { if(Model!=null)Model.SetCoverageEnabled(HideCoveredBody&&!coverageSuspended&&Wardrobe!=null&&!Wardrobe.ClothGrabbed); }
        public void StartSlowPresentation() { StartPresentationAt(SlowMotionSpeed); }
        public void StartPresentationAt(float speed)
        {
            if(LayerLab&&LayerLab.ForTarget(0)){StandForFitting();Report("Áo dài thử: giữ tư thế đứng để kiểm tra nhiều lớp trước khi bật bước đi");return;}if(BlockUnverifiedTransferMotion())return;
            Speed=Mathf.Clamp(speed,MinimumMotionSpeed,MaximumMotionSpeed);
            if(!Playing)TogglePlay();
            Report("Đổi dần nhịp trình diễn đến "+Speed.ToString("0.00")+"× • trọng lực và kiểm tra tiếp xúc vẫn hoạt động");
        }
        bool BlockUnverifiedTransferMotion()
        {
            if(DoffableBlouse.Live.Count>0){Report("Áo cài nút đang thử cởi · giữ mannequin đứng yên để kiểm tra va chạm");return true;}
            if(Wardrobe==null||!Wardrobe.HasDetailedTransfer)return false;
            StandForFitting();Report("Đồ 3D đang kiểm tra phom tĩnh. Cần mô phỏng vải và kiểm định chuyển động trước khi bật bước đi.");return true;
        }

        void OnGUI()
        {
            if (string.IsNullOrEmpty(FatalError)) return;
            GUI.matrix = Matrix4x4.identity;
            GUI.Box(new Rect(20, 20, Screen.width - 40, 230), "Không thể mở phòng thử đồ");
            GUI.Label(new Rect(40, 65, Screen.width - 80, 160), FatalError + "\nXem Console hoặc Logs/FashionFit-Editor.log.");
        }
    }
}
