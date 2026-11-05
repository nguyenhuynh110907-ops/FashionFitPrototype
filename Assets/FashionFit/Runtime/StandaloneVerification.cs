using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace FashionFit
{
    // Opt-in diagnostics for the exported Windows player. No work during ordinary use.
    public sealed class StandaloneVerification:MonoBehaviour
    {
        [Serializable] sealed class Result {public string status="passed";public int checks,exceptions,looks,poseModels,transferPackages;public bool started,standing,canPresent,transferMotionBlocked,rendered,screenshotSaved;public float baseMovedM,basePhysicsTime,pickupDroppedM,baseDropTime,baseFinalY;public bool autoPhysics,baseDroppedToFloor,baseHeldAtCheck,baseReadyAtCheck,baseKinematicAtCheck;public int baseOwnerCount;public Vector3 baseFrom,baseTo;}
        Result result=new Result();string output;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Activate()
        {
            if(Application.isEditor)return;string[] args=Environment.GetCommandLineArgs();string folder=null;for(int i=0;i<args.Length-1;i++)if(args[i]=="-fashionVerify")folder=args[i+1];
            if(folder==null)return;if(!Path.IsPathRooted(folder)){Debug.LogError("-fashionVerify requires an absolute output folder");return;}
            var check=new GameObject("FashionFit standalone opt-in verification").AddComponent<StandaloneVerification>();check.output=folder;DontDestroyOnLoad(check);Application.logMessageReceived+=check.Log;
        }
        void Log(string message,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error)result.exceptions++;}
        void Check(bool ok,string message){result.checks++;if(!ok)throw new InvalidOperationException(message);}
        IEnumerator Start()
        {
            Directory.CreateDirectory(output);float expires=Time.realtimeSinceStartup+90;FashionFitApp app=null;
            while(Time.realtimeSinceStartup<expires){app=FindObjectOfType<FashionFitApp>();if(app&&app.Ready)break;yield return null;}
            if(!app||!app.Ready){Fail("Application did not initialize");yield break;}
            result.started=true;RunwayPoseBase movingBase=null;RunwayPickup fallingProp=null;Vector3 baseStart=Vector3.zero;float basePhysicsStart=0,fallingStartY=0;
            for(int look=1;look<=6;look++){
                try{app.SetLook(look);app.StandForFitting();Check(app.Ready&&app.Wardrobe.HasGarment,"Preset failed: "+look);result.looks++;}catch(Exception e){Fail(e.Message);yield break;}
                for(int frame=0;frame<20;frame++)yield return null;
            }
            try{
                result.autoPhysics=Physics.simulationMode==SimulationMode.FixedUpdate;Check(result.autoPhysics,"Exported player physics is not automatic FixedUpdate");
                result.poseModels=app.PoseStudio.Rigs.Count;Check(result.poseModels==2,"Male/female mannequins missing");foreach(var rig in app.PoseStudio.Rigs)Check(rig.Avatar.Renderer.sharedMesh.vertexCount>1000&&rig.Avatar.Bones.Length==17,"Invalid pose body");
                app.StartSlowPresentation();result.canPresent=app.Playing;Check(result.canPresent,"Existing cloth motion blocked");app.StandForFitting();
                var packages=app.Wardrobe.TransferCatalog;result.transferPackages=packages.Length;Check(packages.Length>0,"Transferred detail package missing");app.Wardrobe.WearTransferred(packages[0]);Check(app.Wardrobe.HasDetailedTransfer,"Transferred detail not wearable: "+app.Status);app.StartPresentationAt(.5f);result.transferMotionBlocked=!app.Playing&&app.FittingPose;Check(result.transferMotionBlocked,"Unverified detail motion enabled");
                app.Player.PreviewVRMenu();Check(app.Player.VRMenu.Buttons.Count==10,"VR menu resources missing");app.Player.VRMenu.SelectPage(2);app.Player.VRMenu.ActionButtons[3].Activate();Check(app.Player.VRMenu.Page==5,"Texture menu unavailable");
                app.SetLook(6);for(int i=0;i<6;i++){app.Player.VRMenu.ActionButtons[i].Activate();Check(app.Wardrobe.HasSurfaceMaps&&app.FabricTrial.SelectedLabel==app.FabricTrial.Samples[i].label,"Exported texture missing "+i);}
                app.Player.VRMenu.Hide();app.Player.EnsureHands();var hands=app.Player.Hands;
                var a=app.Showroom.Hats[0];var b=app.Showroom.Hats[1];a.ReturnToDisplay();b.ReturnToDisplay();a.transform.position=new Vector3(-.5f,1.8f,2);b.transform.position=new Vector3(.5f,1.8f,2);Physics.SyncTransforms();
                Vector3 pa=a.GetComponent<Collider>().bounds.center+Vector3.up*(a.GetComponent<Collider>().bounds.extents.y+.08f),pb=b.GetComponent<Collider>().bounds.center+Vector3.up*(b.GetComponent<Collider>().bounds.extents.y+.08f);Quaternion down=Quaternion.LookRotation(Vector3.down,Vector3.forward);
                Check(!hands.TryBeginNearGrip(0,pa+Vector3.up,down),"Remote grip allowed in exported player");
                hands.Step(0,true,pa,down,0,0,.016f);hands.Step(1,true,pb,down,0,0,.016f);hands.Step(0,true,pa,down,1,0,.016f);hands.Step(1,true,pb,down,1,0,.016f);Check(a.IsHeld&&b.IsHeld&&hands.HeldCount==2,"Exported two-hand grip failed");
                hands.Step(0,false,pa,down,1,0,.016f);Check(!a.IsHeld&&b.IsHeld&&a.GetComponent<Rigidbody>().useGravity,"Exported tracking-loss gravity failed");hands.Hide();app.Showroom.ResetProps();
                // Both owners now hold the SAME pickup, then release independently.
                a.ReturnToDisplay();a.transform.position=new Vector3(0,1.8f,2);Physics.SyncTransforms();Vector3 c=a.GetComponent<Collider>().bounds.center,l=c+new Vector3(-.12f,.04f,0),r=c+new Vector3(.12f,.04f,0);
                hands.Step(0,true,l,Quaternion.identity,0,0,.016f);hands.Step(1,true,r,Quaternion.identity,0,0,.016f);hands.Step(0,true,l,Quaternion.identity,1,0,.016f);hands.Step(1,true,r,Quaternion.identity,1,0,.016f);
                Check(a.IsHeld&&hands.HeldBy(0)==hands.HeldBy(1)&&a.GetComponent<RunwayVRGrabTarget>().OwnerCount==2,"Exported shared pickup failed");hands.Release(0,false);Check(a.IsHeld&&a.GetComponent<RunwayVRGrabTarget>().OwnerCount==1,"Exported one-hand continuation failed");hands.Release(1,true);app.Showroom.ResetProps();
                Check(app.PoseStudio.HandlesVisible,"Exported joint markers hidden");app.SetLook(5);Physics.SyncTransforms();Vector3 chest=app.Model.transform.TransformPoint(new Vector3(0,1.23f,.10f));Check(hands.TryBeginNearGrip(0,chest,Quaternion.identity)&&hands.HeldBy(0).DisplayName=="Áo Ivory","Exported shirt/body selection priority failed");hands.ReleaseAll(true);app.SetLook(6);app.FabricTrial.ApplySample(0);app.StandForFitting();app.Player.FocusTailoring();result.standing=app.FittingPose&&!app.Playing;result.rendered=SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null;Check(result.rendered,"No graphics device");Check(result.exceptions==0,"Player logged runtime errors");
            }catch(Exception e){Fail(e.Message);yield break;}
            try{
                movingBase=app.PoseStudio.Bases[0];baseStart=movingBase.transform.position;basePhysicsStart=Time.fixedTime;Vector3 handle=movingBase.transform.Find("Grip here to move stand").position;
                app.Player.Hands.Step(0,true,handle,Quaternion.identity,0,0,.016f);app.Player.Hands.Step(0,true,handle,Quaternion.identity,1,0,.016f);Check(movingBase.IsHeld,"Exported base grip failed");movingBase.Drag(handle+new Vector3(-.22f,.2f,.24f),Quaternion.Euler(0,20,0));
                fallingProp=app.Showroom.Hats[0];fallingProp.ReturnToDisplay();fallingProp.transform.position=new Vector3(0,1.8f,2);Physics.SyncTransforms();fallingStartY=fallingProp.transform.position.y;fallingProp.BeginGrip(fallingProp.FittingPoint,Quaternion.identity);fallingProp.ReleaseGrip();
            }catch(Exception e){Fail(e.Message);yield break;}
            float moveUntil=Time.fixedTime+1f,moveTimeout=Time.realtimeSinceStartup+10;
            while(Time.fixedTime<moveUntil&&Time.realtimeSinceStartup<moveTimeout)yield return null;
            try{result.baseFrom=baseStart;result.baseTo=movingBase.transform.position;result.baseMovedM=Vector3.Distance(baseStart,movingBase.transform.position);result.basePhysicsTime=Time.fixedTime-basePhysicsStart;result.baseHeldAtCheck=movingBase.IsHeld;result.baseReadyAtCheck=app.Ready;result.baseKinematicAtCheck=movingBase.GetComponent<Rigidbody>().isKinematic;result.baseOwnerCount=movingBase.GetComponent<RunwayVRGrabTarget>().OwnerCount;result.pickupDroppedM=fallingStartY-fallingProp.transform.position.y;Debug.Log("EXPORTED_BASE_CHECK "+JsonUtility.ToJson(result));Check(result.basePhysicsTime>.1f,"FixedUpdate physics did not advance");Check(result.baseMovedM>.15f,"Exported base physics did not move mannequin");Check(result.pickupDroppedM>.15f&&fallingProp.GetComponent<Collider>().bounds.min.y>-.035f,"Exported dropped hat did not fall/collide with floor");app.Player.Hands.ReleaseAll(true);Check(movingBase.GetComponent<Rigidbody>().useGravity,"Exported base gravity missing");app.Player.Hands.Hide();}catch(Exception e){Fail(e.Message);yield break;}
            float dropStart=Time.fixedTime,dropTimeout=Time.realtimeSinceStartup+10;
            while(Time.fixedTime-dropStart<1f&&Time.realtimeSinceStartup<dropTimeout)yield return null;
            try{result.baseDropTime=Time.fixedTime-dropStart;result.baseFinalY=movingBase.transform.position.y;result.baseDroppedToFloor=result.baseFinalY<.06f&&result.baseFinalY>.015f;Check(result.baseDroppedToFloor,"Released base did not descend onto floor: "+result.baseFinalY);movingBase.ReturnHome();app.Showroom.ResetProps();}catch(Exception e){Fail(e.Message);yield break;}
            for(int frame=0;frame<10;frame++)yield return null;
            yield return new WaitForEndOfFrame();string screenshot=Path.Combine(output,"Standalone.png");ScreenCapture.CaptureScreenshot(screenshot);
            float finish=Time.realtimeSinceStartup+10;while(!File.Exists(screenshot)&&Time.realtimeSinceStartup<finish)yield return null;
            result.screenshotSaved=File.Exists(screenshot);if(!result.screenshotSaved||result.exceptions>0){Fail("Screenshot or player exception check failed");yield break;}
            File.WriteAllText(Path.Combine(output,"standalone.json"),JsonUtility.ToJson(result,true));Debug.Log("RECOVERY_STANDALONE_OK "+JsonUtility.ToJson(result));Application.Quit(0);
        }
        void Fail(string message){result.status="failed";File.WriteAllText(Path.Combine(output,"standalone.json"),JsonUtility.ToJson(result,true));File.WriteAllText(Path.Combine(output,"failure.txt"),message);Debug.LogError(message);Application.Quit(1);}
        void OnDestroy(){Application.logMessageReceived-=Log;}
    }
}
