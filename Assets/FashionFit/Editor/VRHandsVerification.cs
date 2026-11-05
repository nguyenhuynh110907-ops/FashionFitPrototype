#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FashionFit.Editor
{
    public static class VRHandsVerification
    {
        [Serializable] sealed class Result
        {
            public string status="passed",scope="Injected controller poses through live two-hand interaction, PhysX simulation and Unity renders; no physical HMD test";
            public int checks,joints,textureSamples,exceptions;public bool distantBlocked,wallBlocked,twoPickups,singleOwner,oneHandLoss,neutralRearm,noFirstFrameJump,heldCollision,gravity,settled,dualWristPose,jointConflict,poseHeld,twoMenuRays;
            public float firstFrameJumpM,fallM,restFloorPenetrationM,maxBoneLengthErrorM;
        }
        static Result result;static FashionFitApp app;static RunwayPlayer player;static RunwayVRHands hands;static string folder;
        static void Check(bool ok,string label){result.checks++;if(!ok)throw new InvalidOperationException(label);}
        static void InVR(bool b)=>typeof(RunwayPlayer).GetProperty("InVR").GetSetMethod(true).Invoke(player,new object[]{b});
        static readonly Quaternion Down=Quaternion.LookRotation(Vector3.down,Vector3.forward);
        static void Place(RunwayPickup item,Vector3 p){item.ReturnToDisplay();item.transform.position=p;item.GetComponent<Rigidbody>().position=p;Physics.SyncTransforms();}
        static Vector3 HandPoint(RunwayPickup p)=>p.GetComponent<Collider>().bounds.center+Vector3.up*(p.GetComponent<Collider>().bounds.extents.y+.08f);
        static void Tick(int side,Vector3 p,Quaternion q,float grip=0,float trigger=0,bool tracked=true)=>hands.Step(side,tracked,p,q,grip,trigger,1f/60);
        static void PhysicsStep(RunwayPickup item,int count){for(int i=0;i<count;i++){if(item&&item.IsHeld)item.StepHeld(.02f);Physics.Simulate(.02f);Physics.SyncTransforms();}}
        static void Log(string m,string s,LogType t){if(t==LogType.Exception||t==LogType.Error)result.exceptions++;}
        public static void RunBatch()
        {
            folder=Path.GetFullPath("Verification/VRHands");Directory.CreateDirectory(folder);result=new Result();GameObject root=null;var mode=Physics.simulationMode;Application.logMessageReceived+=Log;
            try{
                Physics.simulationMode=SimulationMode.Script;root=new GameObject("VR hands verification");app=root.AddComponent<FashionFitApp>();typeof(FashionFitApp).GetMethod("Start",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(app,null);Check(app.Ready,"Startup: "+app.FatalError);player=app.Player;
                player.PrepareForVRTrial();Check(app.PoseStudio.HandlesVisible,"VR preset hides joint markers");player.EnsureHands();hands=player.Hands;InVR(true);
                var a=app.Showroom.Hats[0];var b=app.Showroom.Hats[1];Place(a,new Vector3(-.5f,1.7f,2));Place(b,new Vector3(.5f,1.7f,2));Vector3 pa=HandPoint(a),pb=HandPoint(b);
                result.distantBlocked=!hands.TryBeginNearGrip(0,pa+Vector3.up,Down)&&!hands.TryBeginNearGrip(1,pa+Vector3.up*3,Down);Check(result.distantBlocked,"Remote Grip still picks up props");
                var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.name="Occlusion fixture";wall.transform.SetParent(root.transform);wall.layer=10;wall.transform.position=pa-Vector3.up*.04f;wall.transform.localScale=new Vector3(.4f,.02f,.4f);Physics.SyncTransforms();
                result.wallBlocked=!hands.TryBeginNearGrip(0,pa,Down);Check(result.wallBlocked,"Near overlap selects through solid furniture");UnityEngine.Object.DestroyImmediate(wall);Physics.SyncTransforms();
                Tick(0,pa,Down);Tick(1,pb,Down);Tick(0,pa,Down,1);Tick(1,pb,Down,1);result.twoPickups=a.IsHeld&&b.IsHeld&&hands.HeldCount==2;Check(result.twoPickups,"Both hands cannot hold separate props");Check(hands.Visible(0)&&hands.Visible(1),"Tracked hands not visible");
                var shared=a.GetComponent<RunwayVRGrabTarget>();object secondOwner=new object();Check(shared.TryAcquire(secondOwner,pa,Down),"Second owner cannot join pickup");result.singleOwner=!shared.TryAcquire(new object(),pa,Down);Check(result.singleOwner,"Third owner accepted");shared.Release(secondOwner,false);
                Vector3 before=a.transform.position;PhysicsStep(a,1);result.firstFrameJumpM=Vector3.Distance(before,a.transform.position);result.noFirstFrameJump=result.firstFrameJumpM<.005f;Check(result.noFirstFrameJump,"Grab snaps item into hand");
                Tick(0,pa,Down,1,0,false);result.oneHandLoss=!a.IsHeld&&b.IsHeld&&hands.HeldCount==1&&a.GetComponent<Rigidbody>().velocity.sqrMagnitude<.000001f;Check(result.oneHandLoss,"Lost left tracking releases right hand / throws left item");
                Tick(0,pa,Down,1);bool blocked=!a.IsHeld;Tick(0,pa,Down,0);Tick(0,pa,Down,1);result.neutralRearm=blocked&&a.IsHeld;Check(result.neutralRearm,"Grip did not require neutral release after tracking recovery");
                Tick(0,pa,Down,0);Check(!a.IsHeld&&b.IsHeld,"Releasing one hand affected other hand");hands.ReleaseAll(true);app.Showroom.ResetProps();
                // Move a real hat into a thin wall using the held-body path.
                Place(a,new Vector3(0,1.8f,2));pa=HandPoint(a);Tick(0,pa,Down,0);Tick(0,pa,Down,1);
                wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(root.transform);wall.layer=10;wall.transform.position=new Vector3(.35f,1.8f,2);wall.transform.localScale=new Vector3(.025f,.9f,.9f);Physics.SyncTransforms();
                for(int i=0;i<100;i++){Tick(0,pa+Vector3.right*.8f,Down,1);PhysicsStep(a,1);}
                result.heldCollision=a.GetComponent<Collider>().bounds.max.x<wall.GetComponent<Collider>().bounds.min.x+.005f;Check(result.heldCollision,"Held item passed through thin solid");hands.ReleaseAll(true);UnityEngine.Object.DestroyImmediate(wall);
                // Released body must fall and settle on the showroom floor.
                Place(a,new Vector3(0,1.8f,2));pa=HandPoint(a);Tick(0,pa,Down,0);Tick(0,pa,Down,1);Tick(0,pa,Down,0);
                var rb=a.GetComponent<Rigidbody>();float y=a.transform.position.y;PhysicsStep(null,12);result.fallM=y-a.transform.position.y;result.gravity=rb.useGravity&&!rb.isKinematic&&result.fallM>.1f;Check(result.gravity,"Released pickup has no gravity");PhysicsStep(null,240);
                float bottom=a.GetComponent<Collider>().bounds.min.y;result.restFloorPenetrationM=Mathf.Max(0,-.01f-bottom);result.settled=bottom>-.025f&&bottom<.04f&&rb.velocity.magnitude<.02f;Check(result.settled,"Pickup fell through / did not rest on floor");
                app.Showroom.ResetProps();app.PoseStudio.SetHandles(true);var rig=app.PoseStudio.Rigs[0];rig.ResetPose();Physics.SyncTransforms();
                var left=rig.Handles.First(h=>h.Bone.name=="LeftHand");var right=rig.Handles.First(h=>h.Bone.name=="RightHand");Vector3 lp=left.transform.position,rp=right.transform.position;
                var lengths=rig.Avatar.Bones.Select(x=>x.localPosition).ToArray();Tick(0,lp,Quaternion.identity,0);Tick(1,rp,Quaternion.identity,0);Tick(0,lp,Quaternion.identity,1);Tick(1,rp,Quaternion.identity,1);
                Check(left.IsHeld&&right.IsHeld&&rig.IsHeld,"Two wrist handles cannot be gripped together");
                result.jointConflict=!rig.Handles.First(h=>h.Bone.name=="LeftArm").CanGrab&&!rig.Handles.First(h=>h.Bone.name=="Hips").CanGrab;Check(result.jointConflict,"Conflicting IK / parent chain can be overwritten");
                Quaternion lrest=left.Bone.parent.parent.localRotation,rrest=right.Bone.parent.parent.localRotation;
                for(int i=0;i<100;i++){Tick(0,lp+Vector3.up*.25f+Vector3.forward*.12f,Quaternion.Euler(15,0,0),1);Tick(1,rp+Vector3.up*.20f+Vector3.forward*.10f,Quaternion.Euler(10,0,0),1);}
                result.dualWristPose=Quaternion.Angle(lrest,left.Bone.parent.parent.localRotation)>2&&Quaternion.Angle(rrest,right.Bone.parent.parent.localRotation)>2;Check(result.dualWristPose,"Actual bones do not follow both hands");
                Tick(0,lp,Quaternion.identity,0);Check(!left.IsHeld&&right.IsHeld&&rig.IsHeld,"Rig held state ignores second hand");Tick(1,rp,Quaternion.identity,0);Quaternion posed=left.Bone.rotation;for(int i=0;i<20;i++)app.AdvanceSimulation(1f/60);result.poseHeld=Quaternion.Angle(posed,left.Bone.rotation)<.01f;Check(result.poseHeld,"Pose lost after releasing joint");
                for(int i=0;i<rig.Avatar.Bones.Length;i++)result.maxBoneLengthErrorM=Mathf.Max(result.maxBoneLengthErrorM,Vector3.Distance(lengths[i],rig.Avatar.Bones[i].localPosition));Check(result.maxBoneLengthErrorM<.00001f,"Pose interaction stretches skeleton");
                result.joints=0;foreach(var model in app.PoseStudio.Rigs){app.PoseStudio.ResetPose(model==app.PoseStudio.Rigs[0]?0:1);foreach(var h in model.Handles){Physics.SyncTransforms();Check(hands.TryBeginNearGrip(0,h.transform.position,Quaternion.identity)&&h.IsHeld,"Unreachable handle: "+h.Label);hands.Release(0,false);result.joints++;}}
                typeof(RunwayPlayer).GetMethod("BuildVRControls",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(player,null);var menu=player.VRMenu;Check(menu&&menu.Buttons.Count==10,"Menu missing");menu.SelectPage(2);menu.ActionButtons[3].Activate();Check(menu.Page==5,"Texture page not reachable");
                app.SetLook(6);app.StandForFitting();float speed=app.Speed;int material=app.Wardrobe.PhysicsMaterialIndex;
                for(int i=0;i<6;i++){menu.ActionButtons[i].Activate();Check(app.Wardrobe.HasSurfaceMaps&&app.FabricTrial.SelectedLabel==app.FabricTrial.Samples[i].label,"Texture action failed "+i);result.textureSamples++;}
                Check(app.Speed==speed&&material==app.Wardrobe.PhysicsMaterialIndex,"Appearance selection changes mechanics");
                menu.SelectPage(2);menu.PlaceInFront();Physics.SyncTransforms();var button=menu.ActionButtons[0];Vector3 origin=player.View.transform.position;Quaternion aim=Quaternion.LookRotation(button.transform.position-origin);bool initial=player.VRSnapTurn;
                Tick(0,origin,aim,0,0);Tick(0,origin,aim,0,1);bool leftWorks=player.VRSnapTurn!=initial;Tick(0,origin,aim,0,0);Tick(1,origin,aim,0,0);Tick(1,origin,aim,0,1);result.twoMenuRays=leftWorks&&player.VRSnapTurn==initial;Check(result.twoMenuRays,"Menu trigger is not functional on both hands");
                Tick(0,Vector3.zero,Quaternion.identity,1,0,false);Tick(1,Vector3.zero,Quaternion.identity,1,0,false);Check(hands.HeldCount==0&&!hands.Visible(0)&&!hands.Visible(1),"Tracking loss leaves ghost hands");
                Check(!RunwayVRHands.ValidPose(new Vector3(float.NaN,0,0),Quaternion.identity),"Non-finite tracking pose allowed");Check(!RunwayVRHands.Button(.5f,false)&&RunwayVRHands.Button(.5f,true),"Analog Grip hysteresis missing");Check(result.exceptions==0,"Runtime errors recorded");
                // Shirt and mannequin broad-phase volumes overlap at the torso.
                // A hand inside both must pull the shirt instead of the whole body.
                app.SetLook(5);Physics.SyncTransforms();Vector3 chest=app.Model.transform.TransformPoint(new Vector3(0,1.23f,.10f));
                Check(hands.TryBeginNearGrip(0,chest,Quaternion.identity)&&hands.HeldBy(0).DisplayName=="Áo Ivory","Overlapping body volume steals shirt Grip");hands.ReleaseAll(true);
                InVR(false);hands.Hide();app.SetLook(6);app.FabricTrial.ApplySample(0);app.StandForFitting();player.FocusShowroom();Capture("Textured_Atelier.png");
                player.FocusPoseStudio(0);hands.Step(0,true,rig.transform.position+new Vector3(-.35f,1.4f,.6f),Quaternion.Euler(0,180,0),.2f,.1f,.016f);hands.Step(1,true,rig.transform.position+new Vector3(.35f,1.4f,.6f),Quaternion.Euler(0,180,0),.2f,.1f,.016f);Capture("Pose_Hands.png");
                File.WriteAllText(Path.Combine(folder,"verification.json"),JsonUtility.ToJson(result,true));Debug.Log("VR_HANDS_OK "+JsonUtility.ToJson(result));UnityEngine.Object.DestroyImmediate(root);root=null;Physics.simulationMode=mode;EditorApplication.Exit(0);
            }catch(Exception e){result.status="failed";File.WriteAllText(Path.Combine(folder,"verification.json"),JsonUtility.ToJson(result,true));File.WriteAllText(Path.Combine(folder,"failure.txt"),e.ToString());Debug.LogException(e);Physics.simulationMode=mode;EditorApplication.Exit(1);}
            finally{Physics.simulationMode=mode;Application.logMessageReceived-=Log;if(root){if(player)InVR(false);UnityEngine.Object.DestroyImmediate(root);}}
        }
        static void Capture(string name)
        {
            if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
            var c=player.View;Rect rect=c.rect;c.rect=new Rect(0,0,1,1);var rt=new RenderTexture(1280,900,24);var texture=new Texture2D(1280,900,TextureFormat.RGB24,false);var old=RenderTexture.active;
            try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1280,900),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(folder,name),texture.EncodeToPNG());}finally{c.targetTexture=null;c.rect=rect;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
        }
    }
}
#endif
