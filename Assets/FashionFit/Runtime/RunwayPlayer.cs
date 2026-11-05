using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using XRCommonUsages = UnityEngine.XR.CommonUsages;
using UnityEngine.XR.Management;

namespace FashionFit
{
    public sealed class RunwayPlayer : MonoBehaviour
    {
        FashionFitApp app;
        public RunwayVRHands Hands {get;private set;}
        public void EnsureHands(){if(!Hands){Hands=new GameObject("Two Touch controller hands").AddComponent<RunwayVRHands>();Hands.transform.SetParent(transform,false);Hands.Initialize(app,this);}}
        public Camera View { get; private set; }
        public bool InVR { get; private set; }
        public bool StartingVR { get; private set; }
        public bool PanelVisible = true;
        public bool FollowModel = true;
        public bool LowQuality;
        [Range(.5f,2.2f)] public float VRWalkSpeed=1.25f;
        [Range(30,120)] public float VRTurnSpeed=75;
        public bool VRSnapTurn;
        public bool HeadTracked {get;private set;}
        bool movementArmed,turnReady=true;
        bool menuWasDown,recenterWasDown,desktopMenuPreview;
        bool initialVRMenuPending;
        RunwayVRButton hoveredButton;
        TextMesh interactionHint;
        RunwayVRGrabTarget hintedTarget;
        bool hintedHeld;
        public RunwayVRPanel VRMenu {get;private set;}
        float yaw, pitch;
        int station;
        bool triggerWasDown, gripWasDown, rightWasHeld;
        Vector2 previousMouse;
        Transform vrPanel;
        LineRenderer pointer;
        RunwayVRGrabTarget activeGrab;
        float grabDistance;
        float trackingHeight;
        readonly Collider[] nearbyGrabs=new Collider[24];
        public string ControllerStatus {get;private set;}="Chưa vào VR";
        readonly Vector3[] stations = { new Vector3(1.65f, 0, 2.35f), new Vector3(-2.25f, 0, -.68f), new Vector3(0, 0, -2.6f), new Vector3(2.25f, 0, .1f) };

        public bool OpenXRConfigured
        {
            get { XRGeneralSettings settings=XRGeneralSettings.Instance;if(settings==null||settings.Manager==null)return false;foreach(var loader in settings.Manager.activeLoaders)if(loader&&loader.GetType().FullName=="UnityEngine.XR.OpenXR.OpenXRLoader")return true;return false; }
        }
        public string VRReadiness
        {
            get
            {
                if(InVR)return HeadTracked?"VR đang hoạt động • đã nhận tracking đầu":"VR đã mở • chờ tracking đầu; tương tác đang tạm ngừng";
                if(!OpenXRConfigured)return "Chưa có XR Plug-in Manager/OpenXR trong project";
                if(XRSettings.isDeviceActive)return "Headset đã được OpenXR nhận • có thể vào VR";
                return "Project đã có OpenXR • hãy bật Meta Horizon Link/SteamVR và kết nối headset";
            }
        }
        public void CheckVRSetup(){app.Report(VRReadiness);}
        public void PrepareForVRTrial()
        {
            EndActiveGrab();app.StandForFitting();app.Speed=FashionFitApp.DefaultMotionSpeed;
            app.Wardrobe.SetAirDemo(false);app.PoseStudio.SetHandles(false);
            if(!LowQuality)ToggleQuality();VRWalkSpeed=.8f;VRSnapTurn=true;RequireNeutralInput();
            if(VRMenu)VRMenu.Hide();
            app.Report("Sẵn sàng thử VR: đứng yên, đồ họa nhẹ, đi 0,80 m/s, quay 45° • Mở Link trong kính rồi bấm Vào VR");
        }

        public void Initialize(FashionFitApp owner)
        {
            app = owner;
            View = new GameObject("Fitting room camera").AddComponent<Camera>();
            View.transform.SetParent(transform, false); View.transform.localPosition = new Vector3(0, 1.65f, 0); View.tag = "MainCamera";
            View.clearFlags = CameraClearFlags.SolidColor; View.backgroundColor = new Color(.16f, .15f, .14f); View.nearClipPlane = .05f; View.farClipPlane = 15; View.fieldOfView = 50; View.allowHDR = true;
            View.gameObject.AddComponent<AudioListener>();
            GoToView(0); FollowModel = true; LookAtModel();
            Application.onBeforeRender += TrackHead;
            #if UNITY_ANDROID && !UNITY_EDITOR
            StartCoroutine(StartVR());
            #endif
        }

        public void GoToView(int index)
        {
            station = ((index % stations.Length) + stations.Length) % stations.Length;
            Vector3 destination = stations[station];
            if (InVR)
            {
                EndActiveGrab();Vector3 desired=app.Showroom!=null?app.Showroom.ResolveWalk(destination,Vector3.zero,.22f,true):destination;
                Vector3 headOffset = View.transform.position - transform.position; headOffset.y = 0; transform.position = desired - headOffset;
            }
            else { transform.position = destination; LookAtModel(); }
        }

        public void NextView() { GoToView(station + 1); }
        public void FocusShowroom()
        {
            if(InVR){EndActiveGrab();Vector3 offset=View.transform.position-transform.position;offset.y=0;transform.position=new Vector3(-.1f,0,2.45f)-offset;FaceVRTarget(new Vector3(-1.95f,1,-.65f));return;}
            FollowModel=false;transform.position=new Vector3(0,0,3.5f);View.transform.localPosition=new Vector3(0,1.85f,0);View.fieldOfView=68;
            Quaternion q=Quaternion.LookRotation(new Vector3(-2.05f,1.0f,.65f)-View.transform.position);
            yaw=q.eulerAngles.y;pitch=q.eulerAngles.x;if(pitch>180)pitch-=360;transform.rotation=Quaternion.Euler(0,yaw,0);View.transform.localRotation=Quaternion.Euler(pitch,0,0);
            app.Report("Bàn nón và tủ đồ • trỏ chuột vào vật, giữ E để giả lập Grip, thả E để đặt/thử đồ");
        }
        public void FocusPoseStudio(int index)
        {
            EndActiveGrab();FollowModel=false;Vector3 target=app.PoseStudio.Rigs[Mathf.Clamp(index,0,1)].transform.position;Vector3 destination=new Vector3(target.x,0,target.z+2.1f);
            if(InVR){Vector3 offset=View.transform.position-transform.position;offset.y=0;transform.position=destination-offset;FaceVRTarget(target+Vector3.up*1.1f);return;}
            transform.position=destination;View.transform.localPosition=new Vector3(0,1.5f,0);View.fieldOfView=50;Quaternion q=Quaternion.LookRotation(target+Vector3.up*.95f-View.transform.position);yaw=q.eulerAngles.y;pitch=q.eulerAngles.x;if(pitch>180)pitch-=360;transform.rotation=Quaternion.Euler(0,yaw,0);View.transform.localRotation=Quaternion.Euler(pitch,0,0);
            app.Report("Mẫu nam/nữ • trỏ điểm xanh, giữ E để giả lập Grip • VR dùng Grip trái hoặc phải ở gần điểm khớp • thả để giữ tư thế");
        }
        public void FocusTailoring()
        {
            if(InVR)return;FollowModel=false;
            transform.position=app.Model.transform.TransformPoint(new Vector3(.28f,0,.78f));View.transform.localPosition=new Vector3(0,1.52f,0);
            Quaternion q=Quaternion.LookRotation(app.Model.transform.TransformPoint(new Vector3(0,1.34f,.06f))-View.transform.position);
            yaw=q.eulerAngles.y;pitch=q.eulerAngles.x;if(pitch>180)pitch-=360;
            transform.rotation=Quaternion.Euler(0,yaw,0);View.transform.localRotation=Quaternion.Euler(pitch,0,0);
            app.Report("Góc gần cổ, nẹp và nút • Vào VR/CAMERA để đổi lại góc toàn thân");
        }
        public void FocusHat()
        {
            if(InVR)return;FollowModel=false;transform.position=app.Model.transform.TransformPoint(new Vector3(.35f,0,.85f));View.transform.localPosition=new Vector3(0,1.82f,0);View.fieldOfView=44;
            Quaternion q=Quaternion.LookRotation(app.Showroom.HatPoint+Vector3.up*.045f-View.transform.position);yaw=q.eulerAngles.y;pitch=q.eulerAngles.x;if(pitch>180)pitch-=360;
            transform.rotation=Quaternion.Euler(0,yaw,0);View.transform.localRotation=Quaternion.Euler(pitch,0,0);app.Report("Góc gần nón đang đội • chọn số 01–10 để so sánh các mẫu");
        }

        public void LookAtModel()
        {
            if(InVR)return;
            Vector3 target = app.Model.transform.position + Vector3.up * .95f;
            Quaternion q = Quaternion.LookRotation(target - View.transform.position);
            yaw = q.eulerAngles.y; pitch = q.eulerAngles.x; if (pitch > 180) pitch -= 360;
            transform.rotation = Quaternion.Euler(0, yaw, 0); View.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
        void FaceVRTarget(Vector3 target)
        {
            Vector3 forward=View.transform.forward,desired=target-View.transform.position;forward.y=desired.y=0;
            if(forward.sqrMagnitude>.001f&&desired.sqrMagnitude>.001f)transform.RotateAround(View.transform.position,Vector3.up,Vector3.SignedAngle(forward,desired,Vector3.up));
        }

        void Update()
        {
            if (View == null || app == null || !app.Ready) return;
            if (InVR) { UpdateVR(); return; }
            if (StartingVR) return;
            View.rect = PanelVisible ? new Rect(.30f, .075f, .70f, .925f) : new Rect(0, 0, 1, 1);
            var keys = Keyboard.current; var mouse = Mouse.current;
            if (Down(KeyCode.Tab, keys != null && keys.tabKey.wasPressedThisFrame)) PanelVisible = !PanelVisible;
            if (Down(KeyCode.F12, keys != null && keys.f12Key.wasPressedThisFrame))
            {
                string path = System.IO.Path.Combine(Application.persistentDataPath, "Fitting-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".png");
                ScreenCapture.CaptureScreenshot(path); app.Report("Ảnh chụp: " + path);
            }
            if (Down(KeyCode.Space, keys != null && keys.spaceKey.wasPressedThisFrame)) app.TogglePlay();
            if (Down(KeyCode.F, keys != null && keys.fKey.wasPressedThisFrame)) { FollowModel = !FollowModel; if (FollowModel) LookAtModel(); }
            if (Down(KeyCode.R, keys != null && keys.rKey.wasPressedThisFrame)) app.ResetPresentation();
            if (Down(KeyCode.Alpha1, keys != null && keys.digit1Key.wasPressedThisFrame)) GoToView(0);
            if (Down(KeyCode.Alpha2, keys != null && keys.digit2Key.wasPressedThisFrame)) GoToView(1);
            if (Down(KeyCode.Alpha3, keys != null && keys.digit3Key.wasPressedThisFrame)) GoToView(2);
            if (Down(KeyCode.Alpha4, keys != null && keys.digit4Key.wasPressedThisFrame)) GoToView(3);

            Vector2 move = new Vector2((Held(KeyCode.D, keys != null && keys.dKey.isPressed) ? 1 : 0) - (Held(KeyCode.A, keys != null && keys.aKey.isPressed) ? 1 : 0), (Held(KeyCode.W, keys != null && keys.wKey.isPressed) ? 1 : 0) - (Held(KeyCode.S, keys != null && keys.sKey.isPressed) ? 1 : 0));
            if (move.sqrMagnitude > .02f)
            {
                Move(move, Held(KeyCode.LeftShift, keys != null && keys.leftShiftKey.isPressed) ? 2.2f : 1.15f);
                FollowModel = true; LookAtModel();
            }
            if (Down(KeyCode.Escape, keys != null && keys.escapeKey.wasPressedThisFrame)) { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; PanelVisible = true; }

            Vector2 position, delta; float scroll; bool right;
            #if ENABLE_LEGACY_INPUT_MANAGER
            position=UnityEngine.Input.mousePosition;right=UnityEngine.Input.GetMouseButton(1);
            delta=rightWasHeld&&right?position-previousMouse:Vector2.zero;scroll=UnityEngine.Input.mouseScrollDelta.y*120;
            #else
            position=mouse!=null?mouse.position.ReadValue():Vector2.zero;right=mouse!=null&&mouse.rightButton.isPressed;
            delta=mouse!=null?mouse.delta.ReadValue():Vector2.zero;scroll=mouse!=null?mouse.scroll.ReadValue().y:0;
            #endif
            previousMouse = position; rightWasHeld = right;
            bool overView = !PanelVisible || position.x > Screen.width * .30f;
            if (right && overView)
            {
                yaw += delta.x * .12f; pitch = Mathf.Clamp(pitch - delta.y * .12f, -65, 65);
                transform.rotation = Quaternion.Euler(0, yaw, 0); View.transform.localRotation = Quaternion.Euler(pitch, 0, 0); FollowModel = false;
            }
            if (FollowModel) LookAtModel();
            if (overView) View.fieldOfView = Mathf.Clamp(View.fieldOfView - scroll * .025f, 30, 80);
            bool desktopGrip=overView&&Held(KeyCode.E,keys!=null&&keys.eKey.isPressed);
            Ray desktopRay=View.ScreenPointToRay(position);
            if(desktopGrip&&!gripWasDown)TryBeginGrip(desktopRay,View.transform.rotation);
            if(desktopGrip&&activeGrab!=null)activeGrab.Drag(desktopRay.GetPoint(grabDistance),View.transform.rotation);
            if(!desktopGrip&&gripWasDown)EndActiveGrab();gripWasDown=desktopGrip;
            if(desktopMenuPreview&&VRMenu){
                if(Down(KeyCode.Y,keys!=null&&keys.yKey.wasPressedThisFrame))VRMenu.Toggle();
                if(Down(KeyCode.X,keys!=null&&keys.xKey.wasPressedThisFrame))VRMenu.Show();
                RunwayVRButton hovered=null;if(overView&&Physics.Raycast(desktopRay,out RaycastHit uiHit,5,(1<<8)|(1<<9)|(1<<10),QueryTriggerInteraction.Ignore))hovered=uiHit.collider.GetComponent<RunwayVRButton>();
                SetHoveredButton(hovered);
                bool click=Down(KeyCode.Mouse0,mouse!=null&&mouse.leftButton.wasPressedThisFrame);if(click&&hovered!=null)hovered.Activate();
            }
        }

        static bool Down(KeyCode key, bool newValue)
        {
            #if ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetKeyDown(key);
            #else
            return newValue;
            #endif
        }

        static bool Held(KeyCode key, bool newValue)
        {
            #if ENABLE_LEGACY_INPUT_MANAGER
            return UnityEngine.Input.GetKey(key);
            #else
            return newValue;
            #endif
        }

        void RequireNeutralInput(){gripWasDown=triggerWasDown=menuWasDown=recenterWasDown=true;movementArmed=false;turnReady=false;if(Hands)Hands.Rearm();}
        void OnApplicationFocus(bool focused) { if (!focused) {rightWasHeld = false;EndActiveGrab();RequireNeutralInput();if(Hands)Hands.Hide();} }

        static Vector2 DeadZone(Vector2 input,float deadZone)
        {
            if(float.IsNaN(input.x)||float.IsNaN(input.y)||float.IsInfinity(input.x)||float.IsInfinity(input.y))return Vector2.zero;
            input=Vector2.ClampMagnitude(input,1);float magnitude=input.magnitude;
            return magnitude<=deadZone?Vector2.zero:input/magnitude*((magnitude-deadZone)/(1-deadZone));
        }
        // Shared by live XR input and the Editor verification, without requiring
        // a hardware runtime for testing the movement math.
        public void StepVRMovement(Vector2 move,Vector2 turn,float dt)
        {
            if(!InVR||!HeadTracked){movementArmed=false;turnReady=false;return;}
            FollowModel=false;dt=Mathf.Clamp(dt,0,.05f);
            Vector2 walk=DeadZone(move,.18f),rotation=DeadZone(turn,.20f);
            if(!movementArmed){if(walk==Vector2.zero&&rotation==Vector2.zero){movementArmed=true;turnReady=true;}else return;}
            float degrees=0;
            if(VRSnapTurn){if(Mathf.Abs(turn.x)<.25f)turnReady=true;if(turnReady&&Mathf.Abs(turn.x)>.65f){degrees=Mathf.Sign(turn.x)*45;turnReady=false;}}
            else degrees=rotation.x*Mathf.Clamp(VRTurnSpeed,30,120)*dt;
            if(degrees!=0)transform.RotateAround(View.transform.position,Vector3.up,degrees);
            Vector3 forward=View.transform.forward;forward.y=0;if(forward.sqrMagnitude<.001f)forward=transform.forward;forward.Normalize();
            Vector3 delta=(forward*walk.y+Vector3.Cross(Vector3.up,forward)*walk.x)*Mathf.Clamp(VRWalkSpeed,.5f,2.2f)*dt;
            if(delta.sqrMagnitude>0){Vector3 head=View.transform.position;Vector3 target=app.Showroom.ResolveWalk(head,delta,.22f,true);transform.position+=target-head;}
        }
        public void ToggleVRTurn(){VRSnapTurn=!VRSnapTurn;turnReady=false;RefreshMovementLabels();app.Report(VRSnapTurn?"VR quay từng nấc 45° • thả cần phải về giữa rồi gạt lại":"VR quay mượt • cần phải để quay tự do");}
        public void CycleVRSpeed(){VRWalkSpeed=VRWalkSpeed<1?1.25f:VRWalkSpeed<1.5f?1.8f:.8f;RefreshMovementLabels();app.Report("Tốc độ đi trong VR: "+VRWalkSpeed.ToString("0.00")+" m/s");}
        void RefreshMovementLabels(){if(VRMenu)VRMenu.Refresh();}

        void Move(Vector2 stick, float speed)
        {
            if (stick.magnitude < .15f) return;
            stick = Vector2.ClampMagnitude(stick, 1);
            Vector3 forward = View.transform.forward; forward.y = 0;
            if (forward.sqrMagnitude < .001f) forward = transform.forward; forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            ApplyDisplacement((forward * stick.y + right * stick.x) * speed * Time.unscaledDeltaTime);
        }

        void ApplyDisplacement(Vector3 delta)
        {
            Vector3 head = InVR ? View.transform.position : transform.position;
            Vector3 candidate = head + delta;
            candidate.x = Mathf.Clamp(candidate.x, -3.6f, 3.6f); candidate.z = Mathf.Clamp(candidate.z, -3.6f, 3.6f); candidate.y = head.y;
            if (!InVR && app != null && app.Model != null)
            {
                Vector3 centre = app.Model.transform.position; Vector3 radial = candidate - centre; radial.y = 0;
                const float minimumRadius = .78f;
                if (radial.sqrMagnitude < minimumRadius * minimumRadius)
                {
                    if (radial.sqrMagnitude < .0001f) radial = -View.transform.forward;
                    radial.y = 0; radial.Normalize(); candidate.x = centre.x + radial.x * minimumRadius; candidate.z = centre.z + radial.z * minimumRadius;
                }
            }
            transform.position += candidate - head;
            if(app.Showroom!=null){Vector3 corrected=app.Showroom.ConstrainPosition(InVR?View.transform.position:transform.position,.22f);transform.position+=corrected-(InVR?View.transform.position:transform.position);}
        }

        public void Orbit(float degrees)
        {
            if (InVR || View == null) return;
            transform.RotateAround(app.Model.transform.position, Vector3.up, degrees); FollowModel = true; LookAtModel();
        }

        public void Dolly(float metres)
        {
            if (InVR || View == null) return;
            Vector3 direction = app.Model.transform.position - transform.position; direction.y = 0;
            ApplyDisplacement(direction.normalized * metres); FollowModel = true; LookAtModel();
        }

        public void ToggleVR() { if (StartingVR) return; if (InVR) StopVR(); else StartCoroutine(StartVR()); }

        IEnumerator StartVR()
        {
            EndActiveGrab();RequireNeutralInput();
            StartingVR = true; app.Report("Đang mở OpenXR… Hãy bật Quest Link và chọn OpenXR làm runtime.");
            XRGeneralSettings settings = XRGeneralSettings.Instance;
            if (settings == null || settings.Manager == null) { StartingVR = false; app.Report("Chưa có OpenXR. Trong Unity chọn Fashion Fit > Configure OpenXR."); yield break; }
            XRManagerSettings manager = settings.Manager;
            yield return manager.InitializeLoader();
            if (manager.activeLoader == null) { StartingVR = false; app.Report("Không khởi tạo được OpenXR. Desktop vẫn dùng bình thường."); yield break; }
            manager.StartSubsystems(); float until = Time.realtimeSinceStartup + 6;
            while (!XRSettings.isDeviceActive && Time.realtimeSinceStartup < until) yield return null;
            if (!XRSettings.isDeviceActive) { manager.StopSubsystems(); manager.DeinitializeLoader(); StartingVR = false; app.Report("Kính chưa sẵn sàng. Kiểm tra Quest Link/OpenXR runtime."); yield break; }
            var inputs = new List<XRInputSubsystem>(); SubsystemManager.GetInstances(inputs);
            trackingHeight=1.65f;
            foreach (var input in inputs)if(input.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor)||input.GetTrackingOriginMode()==TrackingOriginModeFlags.Floor)trackingHeight=0;
            InVR = true; StartingVR = false; FollowModel = false;
            RequireNeutralInput();
            View.rect = new Rect(0, 0, 1, 1); View.transform.localPosition = Vector3.zero; View.transform.localRotation = Quaternion.identity;
            transform.rotation = Quaternion.identity; transform.position = stations[station];
            XRSettings.eyeTextureResolutionScale = LowQuality ? .8f : 1f; Application.targetFrameRate = 72; QualitySettings.vSyncCount = 0;
            TrackHead();InitializeVRInteractionUI();
            app.Report("VR LAB • Cần trái: đi tự do • Cần phải: quay mượt • Trigger: nút • Grip hai tay: cầm gần / chỉnh khớp");
        }

        public void StopVR()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            Application.Quit();return;
            #else
            EndActiveGrab();if(Hands)Hands.Hide(); InVR = false;StartingVR=false;initialVRMenuPending=false;HeadTracked=false;movementArmed=false; triggerWasDown = gripWasDown = false;
            if (vrPanel) Destroy(vrPanel.gameObject);VRMenu=null;vrPanel=null;desktopMenuPreview=false;SetHoveredButton(null);
            if (pointer) { Destroy(pointer.sharedMaterial); Destroy(pointer.gameObject); }
            var manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            if (manager != null && manager.activeLoader != null) { manager.StopSubsystems(); manager.DeinitializeLoader(); }
            View.transform.localPosition = new Vector3(0, 1.65f, 0); View.fieldOfView = 50; Application.targetFrameRate = 60; QualitySettings.vSyncCount = 1;
            GoToView(station); FollowModel = true; LookAtModel(); app.Report("Đã về desktop • WASD di chuyển quanh mannequin như chế độ cũ");
            #endif
        }

        void TrackHead()
        {
            if (!InVR) return;
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            bool hasPosition=head.TryGetFeatureValue(XRCommonUsages.devicePosition,out Vector3 position),hasRotation=head.TryGetFeatureValue(XRCommonUsages.deviceRotation,out Quaternion rotation);
            HeadTracked=hasPosition&&hasRotation;if(head.TryGetFeatureValue(XRCommonUsages.isTracked,out bool tracked))HeadTracked&=tracked;
            if(!HeadTracked){movementArmed=false;turnReady=false;return;}
            View.transform.localPosition=position+Vector3.up*trackingHeight;View.transform.localRotation=rotation;
        }

        void UpdateVR()
        {
            TrackHead();
            FinishPendingVRMenu();
            var left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand); var right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            Vector2 move=Vector2.zero,turn=Vector2.zero;
            bool leftTracked=left.isValid;if(left.TryGetFeatureValue(XRCommonUsages.isTracked,out bool lt))leftTracked&=lt;
            bool rightTracked=right.isValid;if(right.TryGetFeatureValue(XRCommonUsages.isTracked,out bool rt))rightTracked&=rt;
            if(leftTracked)left.TryGetFeatureValue(XRCommonUsages.primary2DAxis,out move);
            if(rightTracked)right.TryGetFeatureValue(XRCommonUsages.primary2DAxis,out turn);
            StepVRMovement(move,turn,Time.unscaledDeltaTime);
            if(leftTracked&&HeadTracked){left.TryGetFeatureValue(XRCommonUsages.secondaryButton,out bool menu);left.TryGetFeatureValue(XRCommonUsages.primaryButton,out bool recenter);HandleMenuButtons(menu,recenter);}else{menuWasDown=recenterWasDown=true;}

            EnsureHands();
            bool leftPose=StepController(0,left);if(!InVR)return;
            bool rightPose=StepController(1,right);
            ControllerStatus="Tay trái: "+(leftPose?"đang tracking":"mất tracking")+" • tay phải: "+(rightPose?"đang tracking":"mất tracking")+" • Grip gần ≤25 cm";
        }
        bool StepController(int side,UnityEngine.XR.InputDevice device)
        {
            bool position=device.TryGetFeatureValue(XRCommonUsages.devicePosition,out Vector3 p);
            bool rotation=device.TryGetFeatureValue(XRCommonUsages.deviceRotation,out Quaternion q);
            bool tracked=device.isValid&&position&&rotation;
            if(device.TryGetFeatureValue(XRCommonUsages.isTracked,out bool valid))tracked&=valid;
            if(!device.TryGetFeatureValue(XRCommonUsages.grip,out float grip))grip=device.TryGetFeatureValue(XRCommonUsages.gripButton,out bool g)&&g?1:0;
            if(!device.TryGetFeatureValue(XRCommonUsages.trigger,out float trigger))trigger=device.TryGetFeatureValue(XRCommonUsages.triggerButton,out bool v)&&v?1:0;
            Hands.Step(side,HeadTracked&&tracked,transform.TransformPoint(p+Vector3.up*trackingHeight),transform.rotation*q,grip,trigger,Time.unscaledDeltaTime);
            return HeadTracked&&tracked;
        }

        public bool TryBeginGrip(Ray ray,Quaternion rotation)
        {
            if(InVR){EnsureHands();return Hands.TryBeginNearGrip(1,ray.origin,rotation);}
            if(activeGrab!=null)return false;
            RunwayVRGrabTarget target=null;Vector3 point=Vector3.zero;float distance=0;
            int count=Physics.OverlapSphereNonAlloc(ray.origin,.13f,nearbyGrabs,1<<9,QueryTriggerInteraction.Collide);float nearest=float.PositiveInfinity;
            for(int i=0;i<count;i++){var candidate=nearbyGrabs[i].GetComponentInParent<RunwayVRGrabTarget>();if(!candidate)continue;Vector3 p=nearbyGrabs[i].ClosestPoint(ray.origin);float d=(p-ray.origin).sqrMagnitude;if(d<nearest){nearest=d;target=candidate;point=p;distance=Mathf.Sqrt(d);}}
            if(!target&&Physics.Raycast(ray,out RaycastHit hit,4,(1<<8)|(1<<9)|(1<<10),QueryTriggerInteraction.Collide)){target=hit.collider.GetComponentInParent<RunwayVRGrabTarget>();point=hit.point;distance=hit.distance;}
            if(!target)return false;
            if((target.IsPoseBase||target.Elastic)&&target.TryPoseJointRay(ray,4,out RaycastHit jointHit)){target=jointHit.collider.GetComponentInParent<RunwayVRGrabTarget>();point=jointHit.point;distance=jointHit.distance;}
            activeGrab=target;grabDistance=target.IsPickup?.22f:target.IsJoint?Mathf.Clamp(distance,0,3.5f):Mathf.Clamp(distance,.25f,3.5f);target.BeginGrab(point,rotation);if(VRMenu&&VRMenu.Page!=12)VRMenu.Hide();SetHoveredButton(null);return true;
        }

        public void EndActiveGrab()
        {
            if(Hands)Hands.ReleaseAll(true);
            if (activeGrab != null) activeGrab.EndGrab(); activeGrab = null;if(interactionHint)interactionHint.gameObject.SetActive(false);hintedTarget=null;
        }

        void InitializeVRInteractionUI()
        {
            EndActiveGrab();RequireNeutralInput();initialVRMenuPending=!HeadTracked;
            if(HeadTracked)FaceVRTarget(app.Model.transform.position);
            EnsureHands();BuildVRControls();if(initialVRMenuPending&&VRMenu)VRMenu.Hide();
        }
        void FinishPendingVRMenu()
        {
            if(!initialVRMenuPending||!HeadTracked)return;
            FaceVRTarget(app.Model.transform.position);if(VRMenu)VRMenu.Show();initialVRMenuPending=false;
        }
        public bool EnsureVRInteractionTracking(bool headAvailable,bool handAvailable)
        {
            if(headAvailable&&handAvailable)return true;
            if(pointer)pointer.enabled=false;if(Hands)Hands.Hide();EndActiveGrab();SetHoveredButton(null);gripWasDown=triggerWasDown=true;
            if(!headAvailable){movementArmed=false;turnReady=false;menuWasDown=recenterWasDown=true;}
            return false;
        }

        void BuildVRControls()
        {
            if(vrPanel){if(Application.isPlaying)Destroy(vrPanel.gameObject);else DestroyImmediate(vrPanel.gameObject);}VRMenu=new GameObject("Atelier VR menu").AddComponent<RunwayVRPanel>();VRMenu.Initialize(app,this);vrPanel=VRMenu.transform;
            if(pointer){if(Application.isPlaying){Destroy(pointer.sharedMaterial);Destroy(pointer.gameObject);}else{DestroyImmediate(pointer.sharedMaterial);DestroyImmediate(pointer.gameObject);}}
            pointer = new GameObject("Right hand pointer").AddComponent<LineRenderer>();
            pointer.positionCount = 2; pointer.startWidth = .003f; pointer.endWidth = .0015f; pointer.useWorldSpace = true;
            pointer.sharedMaterial = RunwayStage.Matte("Pointer", new Color(.84f, .75f, .54f)); pointer.sharedMaterial.SetColor("_EmissionColor", new Color(.7f, .6f, .4f));
            pointer.enabled=false;
            RefreshMovementLabels();
        }
        public void PreviewVRMenu(){if(InVR){if(VRMenu)VRMenu.Show();return;}FollowModel=false;View.fieldOfView=60;desktopMenuPreview=true;if(!VRMenu)BuildVRControls();else VRMenu.Show();if(pointer)pointer.enabled=false;app.Report("Xem thử menu VR • chuột trái chọn • Y mở/ẩn • X đưa menu về trước • chưa khởi tạo headset");}
        public void HandleMenuButtons(bool menu,bool recenter){if(VRMenu){if(menu&&!menuWasDown)VRMenu.Toggle();if(recenter&&!recenterWasDown)VRMenu.Show();}menuWasDown=menu;recenterWasDown=recenter;}
        void SetHoveredButton(RunwayVRButton button){if(hoveredButton==button)return;if(hoveredButton)hoveredButton.SetHovered(false);hoveredButton=button;if(hoveredButton)hoveredButton.SetHovered(true);}
        void UpdateInteractionHint(RunwayVRGrabTarget target,Vector3 point)
        {
            if(!target){if(interactionHint)interactionHint.gameObject.SetActive(false);hintedTarget=null;return;}
            if(!interactionHint)interactionHint=RunwayStage.Text(app.transform,"",Vector3.zero,.027f,new Color(1,.89f,.65f));
            bool held=activeGrab==target;if(hintedTarget!=target||hintedHeld!=held){interactionHint.text=target.DisplayName+"\n"+(held?target.ReleaseHint:"Giữ Grip để lấy / kéo");hintedTarget=target;hintedHeld=held;}
            interactionHint.gameObject.SetActive(true);Vector3 position=point+Vector3.up*.13f;interactionHint.transform.position=position;interactionHint.transform.rotation=Quaternion.LookRotation(position-View.transform.position);interactionHint.transform.localScale=Vector3.one*Mathf.Clamp(Vector3.Distance(View.transform.position,position),.6f,3);
        }

        public void ToggleQuality()
        {
            LowQuality = !LowQuality; QualitySettings.shadows = LowQuality ? ShadowQuality.HardOnly : ShadowQuality.All;
            QualitySettings.shadowResolution = LowQuality ? ShadowResolution.Low : ShadowResolution.Medium; QualitySettings.antiAliasing = LowQuality ? 2 : 4;
            if (InVR) XRSettings.eyeTextureResolutionScale = LowQuality ? .8f : 1f;
        }

        void OnDestroy()
        {
            Application.onBeforeRender -= TrackHead; EndActiveGrab();
            var manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
            if ((InVR||StartingVR) && manager != null && manager.activeLoader != null) { manager.StopSubsystems(); manager.DeinitializeLoader(); }
            if (pointer) { Destroy(pointer.sharedMaterial); Destroy(pointer.gameObject); }
            if(vrPanel){if(Application.isPlaying)Destroy(vrPanel.gameObject);else DestroyImmediate(vrPanel.gameObject);}if(interactionHint)Destroy(interactionHint.gameObject);
        }
    }
}
