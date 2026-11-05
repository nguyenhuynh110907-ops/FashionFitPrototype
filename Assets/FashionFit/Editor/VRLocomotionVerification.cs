#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FashionFit.Editor
{
    public static class VRLocomotionVerification
    {
        [Serializable] sealed class Result
        {
            public string status="passed";
            public string scope="Injected tracked poses and stick values through the runtime movement method; not a hardware/headset test";
            public int checks,simulatedSteps,panelButtons;
            public float forwardErrorM,diagonalErrorM,smoothTurnErrorDeg,turnHeadDriftM,frameRateDifferenceM;
            public bool headRelative,strafe,deadZone,noModelLock,roomScale,tableBlocked,wardrobeBlocked,wallBlocked,modelBlocked,sliding,snapLatch,trackingLoss,neutralRearm,safeViews,settingsButtons;
        }
        static FashionFitApp app;static RunwayPlayer player;static Result result;
        static void SetState(string name,bool value)=>typeof(RunwayPlayer).GetProperty(name).GetSetMethod(true).Invoke(player,new object[]{value});
        static void Require(bool condition,string message){result.checks++;if(!condition)throw new InvalidOperationException(message);}
        static void Pose(Vector3 position,float headYaw=0,Vector3? localOffset=null)
        {
            SetState("InVR",true);SetState("HeadTracked",false);player.StepVRMovement(Vector2.zero,Vector2.zero,.016f);
            player.transform.SetPositionAndRotation(position,Quaternion.identity);player.View.transform.localPosition=localOffset??new Vector3(0,1.65f,0);player.View.transform.localRotation=Quaternion.Euler(30,headYaw,0);
            SetState("HeadTracked",true);player.VRSnapTurn=false;player.VRWalkSpeed=1.25f;player.StepVRMovement(Vector2.zero,Vector2.zero,.016f);
        }
        static void Step(Vector2 move,Vector2 turn,int frames,float dt){for(int i=0;i<frames;i++){player.StepVRMovement(move,turn,dt);result.simulatedSteps++;}}
        public static void RunBatch()
        {
            string folder=Path.GetFullPath("Verification/VRLocomotion");Directory.CreateDirectory(folder);result=new Result();GameObject root=null;
            try{
                root=new GameObject("VR locomotion verification");app=root.AddComponent<FashionFitApp>();typeof(FashionFitApp).GetMethod("Start",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(app,null);player=app.Player;
                Require(app.Ready,"App startup failed: "+app.FatalError);
                Pose(new Vector3(1,0,1),90,new Vector3(.28f,1.62f,-.15f));Vector3 start=player.View.transform.position;Quaternion localRotation=player.View.transform.localRotation;Vector3 localPosition=player.View.transform.localPosition;
                Step(Vector2.up,Vector2.zero,60,1f/60);result.forwardErrorM=Vector3.Distance(player.View.transform.position-start,new Vector3(1.25f,0,0));result.headRelative=result.forwardErrorM<.0001f;Require(result.headRelative,"Move direction did not follow head yaw");
                Require(player.View.transform.localPosition==localPosition&&Quaternion.Angle(localRotation,player.View.transform.localRotation)<.001f,"Stick input overwrote tracked camera pose");
                player.LookAtModel();result.noModelLock=!player.FollowModel&&Quaternion.Angle(localRotation,player.View.transform.localRotation)<.001f;Require(result.noModelLock,"VR camera locked back to mannequin");
                Pose(new Vector3(1.5f,0,1),90);start=player.View.transform.position;Step(Vector2.right,Vector2.zero,60,1f/60);result.strafe=Vector3.Distance(player.View.transform.position-start,new Vector3(0,0,-1.25f))<.0001f;Require(result.strafe,"Strafe direction wrong");
                Pose(new Vector3(1,0,1));start=player.View.transform.position;Step(Vector2.one,Vector2.zero,60,1f/60);result.diagonalErrorM=Mathf.Abs(Vector3.Distance(start,player.View.transform.position)-1.25f);Require(result.diagonalErrorM<.0001f,"Diagonal movement speed increased");
                Pose(new Vector3(1,0,1));start=player.transform.position;Step(new Vector2(.1f,.1f),new Vector2(.1f,0),180,1f/60);result.deadZone=Vector3.Distance(start,player.transform.position)<.00001f&&Quaternion.Angle(player.transform.rotation,Quaternion.identity)<.001f;Require(result.deadZone,"Stick drift in dead zone");
                Pose(new Vector3(1,0,1),25,new Vector3(.32f,1.65f,-.18f));start=player.View.transform.position;Vector3 initialForward=player.View.transform.forward;player.VRTurnSpeed=75;Step(Vector2.zero,Vector2.right,60,1f/60);
                result.turnHeadDriftM=Vector3.Distance(start,player.View.transform.position);Vector3 expected=Quaternion.Euler(0,75,0)*initialForward;result.smoothTurnErrorDeg=Vector3.Angle(expected,player.View.transform.forward);Require(result.turnHeadDriftM<.0001f&&result.smoothTurnErrorDeg<.03f,"Smooth turn orbited the head or wrong speed");
                Pose(new Vector3(1,0,1));player.VRSnapTurn=true;Step(Vector2.zero,Vector2.right,60,1f/60);float first=player.transform.eulerAngles.y;Step(Vector2.zero,Vector2.zero,1,1f/60);Step(Vector2.zero,Vector2.right,60,1f/60);
                result.snapLatch=Mathf.Abs(Mathf.DeltaAngle(first,45))<.001f&&Mathf.Abs(Mathf.DeltaAngle(player.transform.eulerAngles.y,90))<.001f;Require(result.snapLatch,"Snap repeated while stick held");
                Pose(new Vector3(1,0,1));Vector3 rig=player.transform.position;start=player.View.transform.position;Vector3 physical=new Vector3(.3f,-.15f,.2f);player.View.transform.localPosition+=physical;Step(Vector2.zero,Vector2.zero,1,1f/60);result.roomScale=Vector3.Distance(player.View.transform.position-start,physical)<.0001f&&player.transform.position==rig;Require(result.roomScale,"Physical room-scale motion changed rig origin");
                Pose(new Vector3(-2.05f,0,2.2f),180);Step(Vector2.up,Vector2.zero,300,1f/60);result.tableBlocked=player.View.transform.position.z>=1.5449f;Require(result.tableBlocked,"Walked through hat table");
                Pose(new Vector3(-2.1f,0,-.6f),180);Step(Vector2.up,Vector2.zero,300,1f/60);result.wardrobeBlocked=player.View.transform.position.z>=-1.1651f;Require(result.wardrobeBlocked,"Walked through wardrobe");
                Pose(new Vector3(3.3f,0,1),90);Step(Vector2.up,Vector2.zero,300,1f/60);result.wallBlocked=Mathf.Abs(player.View.transform.position.x-3.6f)<.0001f;Require(result.wallBlocked,"Left room through wall");
                Pose(new Vector3(1.2f,0,0),270);Step(Vector2.up,Vector2.zero,180,1f/60);result.modelBlocked=player.View.transform.position.x>=.4499f&&player.View.transform.position.x<.451f;Require(result.modelBlocked,"Walked into mannequin");
                Pose(new Vector3(-.48f,0,1.8f),225);Step(Vector2.up,Vector2.zero,120,1f/60);result.sliding=player.View.transform.position.x< -1&&player.View.transform.position.z>=1.5449f;Require(result.sliding,"Could not slide along table edge");
                Pose(new Vector3(1,0,1));SetState("HeadTracked",false);start=player.transform.position;Step(Vector2.up,Vector2.right,60,1f/60);result.trackingLoss=player.transform.position==start&&Quaternion.Angle(player.transform.rotation,Quaternion.identity)<.001f;Require(result.trackingLoss,"Moved on lost tracking");
                SetState("HeadTracked",true);Step(Vector2.up,Vector2.zero,60,1f/60);bool blocked=player.transform.position==start;Step(Vector2.zero,Vector2.zero,1,1f/60);Step(Vector2.up,Vector2.zero,1,1f/60);result.neutralRearm=blocked&&player.transform.position!=start;Require(result.neutralRearm,"Did not require neutral sticks after tracking loss");
                Vector3 firstRate=Vector3.zero;foreach(int rate in new[]{30,60,90}){Pose(new Vector3(1,0,1));Step(Vector2.up,Vector2.zero,rate,1f/rate);if(rate==30)firstRate=player.View.transform.position;else result.frameRateDifferenceM=Mathf.Max(result.frameRateDifferenceM,Vector3.Distance(firstRate,player.View.transform.position));}Require(result.frameRateDifferenceM<.0001f,"Frame-rate dependent movement");
                Pose(new Vector3(1,0,1),0,new Vector3(.2f,1.65f,.1f));result.safeViews=true;for(int i=0;i<4;i++){player.GoToView(i);Vector3 p=player.View.transform.position;result.safeViews&=Vector3.Distance(p,app.Showroom.ResolveWalk(p,Vector3.zero,.22f,true))<.0001f;}Require(result.safeViews,"A view station landed inside furniture");
                typeof(RunwayPlayer).GetMethod("BuildVRControls",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(player,null);result.panelButtons=player.VRMenu.Buttons.Count;Require(result.panelButtons==10,"Missing controls in paged VR panel");
                player.VRWalkSpeed=.8f;player.CycleVRSpeed();bool speed=player.VRWalkSpeed==1.25f;player.CycleVRSpeed();speed&=player.VRWalkSpeed==1.8f;player.CycleVRSpeed();speed&=player.VRWalkSpeed==.8f;bool snap=player.VRSnapTurn;player.ToggleVRTurn();result.settingsButtons=speed&&player.VRSnapTurn!=snap;Require(result.settingsButtons,"Movement controls do not change settings");
                SetState("InVR",false);UnityEngine.Object.DestroyImmediate(root);root=null;File.WriteAllText(Path.Combine(folder,"verification.json"),JsonUtility.ToJson(result,true));Debug.Log("VR_LOCOMOTION_OK "+JsonUtility.ToJson(result));EditorApplication.Exit(0);
            }catch(Exception e){result.status="failed";File.WriteAllText(Path.Combine(folder,"verification.json"),JsonUtility.ToJson(result,true));File.WriteAllText(Path.Combine(folder,"failure.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
            finally{if(root){if(player)SetState("InVR",false);UnityEngine.Object.DestroyImmediate(root);}}
        }
    }
}
#endif
