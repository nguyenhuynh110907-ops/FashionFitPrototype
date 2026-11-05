#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace FashionFit.Editor
{
    public static class VRReadyVerification
    {
        [Serializable] sealed class Result {
            public string status="passed",scope="Project configuration and injected head/controller states; no physical Quest/controller session";
            public int checks,panelButtons;public bool openXRLoader,touchProfile,manualXRStartup,stereoInstancing,d3d11,trialPreset,idempotentPreset,menuWaitsForHead,menuAtHeadHeight,menuFacingView,menuWorldAnchored,headLossReleasesGrip,handLossReleasesGrip,trackingRearmsButtons,focusLossReleasesGrip,validTrackingKeepsGrip,poseMenu,speedMenu;
            public float panelDistanceM,panelHeightErrorM;
        }
        static Result result;static RunwayPlayer player;static string Folder=>Path.GetFullPath("Verification/VRReady");
        static void Require(bool ok,string message){result.checks++;if(!ok)throw new InvalidOperationException(message);}
        static void SetState(string name,bool value)=>typeof(RunwayPlayer).GetProperty(name).GetSetMethod(true).Invoke(player,new object[]{value});
        static bool Flag(string name)=>(bool)typeof(RunwayPlayer).GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).GetValue(player);
        static void Invoke(string name,params object[] args)=>typeof(RunwayPlayer).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance).Invoke(player,args);
        static void BeginHat(FashionFitApp app){var hat=app.Showroom.Hats[0];hat.ReturnToDisplay();Physics.SyncTransforms();Require(player.TryBeginGrip(new Ray(hat.GetComponent<Collider>().bounds.center+Vector3.up*(hat.GetComponent<Collider>().bounds.extents.y+.08f),Vector3.down),Quaternion.identity)&&hat.IsHeld,"Grip setup failed");}
        public static void RunBatch()
        {
            Directory.CreateDirectory(Folder);result=new Result();GameObject root=null;
            try{
                var config=XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);Require(config&&config.Manager,"Missing XR manager");
                result.openXRLoader=config.Manager.activeLoaders.Any(loader=>loader is UnityEngine.XR.OpenXR.OpenXRLoader);Require(result.openXRLoader,"OpenXR loader missing");
                result.manualXRStartup=!config.InitManagerOnStart&&!config.Manager.automaticLoading&&!config.Manager.automaticRunning;Require(result.manualXRStartup,"Project must use explicit Enter VR");
                var xr=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);result.touchProfile=xr&&xr.GetFeature<OculusTouchControllerProfile>()&&xr.GetFeature<OculusTouchControllerProfile>().enabled;Require(result.touchProfile,"Touch interaction profile disabled");
                result.stereoInstancing=xr.renderMode==OpenXRSettings.RenderMode.SinglePassInstanced;Require(result.stereoInstancing,"Unexpected stereo mode");result.d3d11=PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64).Contains(GraphicsDeviceType.Direct3D11);Require(result.d3d11,"D3D11 unavailable");
                root=new GameObject("VR readiness verification");var app=root.AddComponent<FashionFitApp>();typeof(FashionFitApp).GetMethod("Start",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(app,null);Require(app.Ready,"Startup failed: "+app.FatalError);player=app.Player;
                app.StartPresentationAt(.5f);app.Wardrobe.SetAirDemo(true);app.PoseStudio.SetHandles(true);player.PrepareForVRTrial();
                app.GetComponent<RunwayUI>().OpenVRPage();Require((int)typeof(RunwayUI).GetField("page",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(app.GetComponent<RunwayUI>())==3,"Trial launcher cannot open VR page");
                result.trialPreset=!app.Playing&&app.FittingPose&&Mathf.Abs(app.Speed-FashionFitApp.DefaultMotionSpeed)<.001f&&!app.Wardrobe.AirDemo&&!app.PoseStudio.HandlesVisible&&player.LowQuality&&player.VRSnapTurn&&Mathf.Abs(player.VRWalkSpeed-.8f)<.001f;Require(result.trialPreset,"Trial preset not applied");
                player.PrepareForVRTrial();result.idempotentPreset=player.LowQuality&&player.VRSnapTurn;Require(result.idempotentPreset,"Repeated prepare changed quality back");
                SetState("InVR",true);SetState("HeadTracked",false);player.transform.SetPositionAndRotation(new Vector3(1.65f,0,2.35f),Quaternion.identity);player.View.transform.localPosition=Vector3.zero;player.View.transform.localRotation=Quaternion.identity;
                Invoke("InitializeVRInteractionUI");result.menuWaitsForHead=player.VRMenu&&!player.VRMenu.Visible&&Flag("initialVRMenuPending");Require(result.menuWaitsForHead,"Menu appeared at untracked origin");
                result.trackingRearmsButtons=Flag("gripWasDown")&&Flag("triggerWasDown")&&Flag("menuWasDown")&&Flag("recenterWasDown");Require(result.trackingRearmsButtons,"Entering VR did not require button release");
                player.View.transform.localPosition=new Vector3(.12f,1.63f,.08f);player.View.transform.localRotation=Quaternion.Euler(0,17,0);SetState("HeadTracked",true);Vector3 head=player.View.transform.position;Invoke("FinishPendingVRMenu");
                result.panelHeightErrorM=Mathf.Abs(player.VRMenu.transform.position.y-(head.y-.18f));result.panelDistanceM=Vector3.Distance(player.VRMenu.transform.position,player.View.transform.position);result.menuAtHeadHeight=player.VRMenu.Visible&&!Flag("initialVRMenuPending")&&result.panelHeightErrorM<.0001f&&Vector3.Distance(head,player.View.transform.position)<.0001f;Require(result.menuAtHeadHeight,"Menu not placed from tracked eye height");
                Vector3 direction=player.VRMenu.transform.position-player.View.transform.position;direction.y=0;Vector3 forward=player.View.transform.forward;forward.y=0;result.menuFacingView=Vector3.Angle(direction,forward)<.01f;Require(result.menuFacingView,"Menu behind user after rig yaw");
                Vector3 anchor=player.VRMenu.transform.position;player.View.transform.localPosition+=new Vector3(.15f,.03f,.10f);Invoke("FinishPendingVRMenu");result.menuWorldAnchored=player.VRMenu.transform.position==anchor;Require(result.menuWorldAnchored,"Late placement repeated every frame");
                result.panelButtons=player.VRMenu.Buttons.Count;Require(result.panelButtons==14,"VR menu layout changed");
                BeginHat(app);result.validTrackingKeepsGrip=player.EnsureVRInteractionTracking(true,true)&&app.Showroom.Hats[0].IsHeld;Require(result.validTrackingKeepsGrip,"Valid tracked Grip released");
                result.headLossReleasesGrip=!player.EnsureVRInteractionTracking(false,true)&&!app.Showroom.Hats[0].IsHeld;Require(result.headLossReleasesGrip,"Head tracking loss did not release Grip");
                BeginHat(app);result.handLossReleasesGrip=!player.EnsureVRInteractionTracking(true,false)&&!app.Showroom.Hats[0].IsHeld;Require(result.handLossReleasesGrip,"Hand tracking loss did not release Grip");Require(Flag("gripWasDown")&&Flag("triggerWasDown"),"Tracking recovery did not latch inputs");
                BeginHat(app);Invoke("OnApplicationFocus",false);result.focusLossReleasesGrip=!app.Showroom.Hats[0].IsHeld&&Flag("gripWasDown")&&Flag("triggerWasDown");Require(result.focusLossReleasesGrip,"Focus loss left a held object / active input");
                player.VRMenu.SelectPage(2);player.VRMenu.ActionButtons[4].Activate();result.poseMenu=player.VRMenu.Page==3;Require(result.poseMenu,"Missing mannequin pose controls");player.VRMenu.SelectPage(0);player.VRMenu.ActionButtons[5].Activate();result.speedMenu=player.VRMenu.Page==4;Require(result.speedMenu,"Missing garment speed controls");
                player.VRMenu.Buttons[10].Activate();Require(player.VRMenu.Page==9,"Layered garment entry strip does not open its page");player.VRMenu.ActionButtons[5].Activate();Require(player.VRMenu.Page==6,"Layered page cannot reach blouse");player.VRMenu.ActionButtons[5].Activate();Require(player.VRMenu.Page==1,"Blouse page cannot return to wardrobe");
                player.VRMenu.Buttons[11].Activate();Require(player.VRMenu.Page==7,"Target/material header missing");
                player.VRMenu.ActionButtons[1].Activate();Require(player.VRMenu.Page==1&&app.Showroom.OutfitTarget==1,"Female target route failed");
                int mainLook=app.Look;player.VRMenu.ActionButtons[2].Activate();Require(app.PoseStudio.Wearables[0].Look==3&&app.Look==mainLook,"VR look changed wrong avatar");
                player.VRMenu.SelectPage(7);player.VRMenu.ActionButtons[2].Activate();Require(player.VRMenu.Page==1&&app.Showroom.OutfitTarget==2,"Male target route failed");
                player.VRMenu.SelectPage(7);player.VRMenu.ActionButtons[3].Activate();Require(player.VRMenu.Page==8,"Material route failed");
                bool hatMode=app.MaterialLab.HatMode;player.VRMenu.ActionButtons[4].Activate();Require(app.MaterialLab.HatMode!=hatMode,"Object focus did not change");
                player.VRMenu.ActionButtons[4].Activate();var oldProfile=app.PoseStudio.Wearables[1].Surface.Material;player.VRMenu.ActionButtons[0].Activate();Require(app.PoseStudio.Wearables[1].Surface.Material!=oldProfile,"Material button did not change selected outfit");
                player.VRMenu.ActionButtons[5].Activate();Require(player.VRMenu.Page==1,"Material page has no way back");
                File.WriteAllText(Path.Combine(Folder,"verification.json"),JsonUtility.ToJson(result,true));Debug.Log("VR_READY_SOFTWARE_OK "+JsonUtility.ToJson(result));SetState("InVR",false);UnityEngine.Object.DestroyImmediate(root);root=null;EditorApplication.Exit(0);
            }catch(Exception e){result.status="failed";File.WriteAllText(Path.Combine(Folder,"verification.json"),JsonUtility.ToJson(result,true));File.WriteAllText(Path.Combine(Folder,"failure.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
            finally{if(root){if(player)SetState("InVR",false);UnityEngine.Object.DestroyImmediate(root);}}
        }
    }
}
#endif
