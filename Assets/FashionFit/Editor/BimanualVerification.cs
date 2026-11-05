#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace FashionFit.Editor
{
    public static class BimanualVerification
    {
        [Serializable] sealed class Result{
            public string status="passed",scope="Injected live hand states, PhysX, two-patch solver and body-mesh exports; not physical HMD testing";
            public int checks,clothCases,meshExports,exceptions;public bool sharedPickup,twoHandTurn,stableJoin,stableLeave,independentRelease,lastHandGravity,baseTranslation,baseRotation,baseGravity,basePosePreserved,baseJointLock,baseDualHold;
            public float joinJumpM,leaveJumpM,maxStretch,baseMovedM,baseLandedY,maxBoneDrift;
        }
        static Result result;static FashionFitApp app;static RunwayPlayer player;static RunwayVRHands hands;static string folder;
        static void Check(bool b,string s){result.checks++;if(!b)throw new InvalidOperationException(s);}
        static void Mode(bool b)=>typeof(RunwayPlayer).GetProperty("InVR").GetSetMethod(true).Invoke(player,new object[]{b});
        static void Tick(int i,Vector3 p,Quaternion q,float g,bool tracked=true)=>hands.Step(i,tracked,p,q,g,0,1f/60);
        static void PhysicsStep(RunwayPickup p,RunwayPoseBase b,int n){for(int i=0;i<n;i++){if(p&&p.IsHeld)p.StepHeld(.02f);if(b&&b.IsHeld)b.StepHeld(.02f);Physics.Simulate(.02f);Physics.SyncTransforms();}}
        static void Log(string m,string s,LogType t){if(t==LogType.Exception||t==LogType.Error)result.exceptions++;}
        public static void RunBatch()
        {
            folder=Path.GetFullPath("Verification/Bimanual");Directory.CreateDirectory(folder);Directory.CreateDirectory(folder+"/Contact");result=new Result();GameObject root=null;var mode=Physics.simulationMode;Application.logMessageReceived+=Log;
            try{
                Physics.simulationMode=SimulationMode.Script;root=new GameObject("Bimanual verification");app=root.AddComponent<FashionFitApp>();typeof(FashionFitApp).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(app,null);Check(app.Ready,app.FatalError);player=app.Player;player.EnsureHands();hands=player.Hands;Mode(true);player.View.transform.position=new Vector3(0,1.7f,3);
                var hat=app.Showroom.Hats[0];hat.ReturnToDisplay();hat.transform.position=new Vector3(0,1.65f,2);hat.GetComponent<Rigidbody>().position=hat.transform.position;Physics.SyncTransforms();
                Vector3 center=hat.GetComponent<Collider>().bounds.center,p0=center+new Vector3(-.12f,.04f,0),p1=center+new Vector3(.12f,.04f,0);var q=Quaternion.identity;
                Tick(0,p0,q,0);Tick(1,p1,q,0);Tick(0,p0,q,1);Check(hat.IsHeld,"First hand does not hold hat");Vector3 before=hat.transform.position;Tick(1,p1,q,1);PhysicsStep(hat,null,1);
                var target=hat.GetComponent<RunwayVRGrabTarget>();result.sharedPickup=hands.HeldBy(0)==target&&hands.HeldBy(1)==target&&target.OwnerCount==2;Check(result.sharedPickup,"Second hand cannot hold same hat");
                result.joinJumpM=Vector3.Distance(before,hat.transform.position);result.stableJoin=result.joinJumpM<.005f;Check(result.stableJoin,"Second-hand join snaps pickup");Check(!target.TryAcquire(new object(),center,q),"Third owner accepted");
                Quaternion initial=hat.transform.rotation;Vector3 scale=hat.transform.localScale;
                for(int i=0;i<80;i++){float a=Mathf.Min(1,i/45f)*60;Quaternion turn=Quaternion.Euler(0,a,0);Tick(0,center+new Vector3(.25f,0,0)+turn*(p0-center),turn,1);Tick(1,center+new Vector3(.25f,0,0)+turn*(p1-center),turn,1);PhysicsStep(hat,null,1);}
                result.twoHandTurn=Quaternion.Angle(initial,hat.transform.rotation)>40&&hat.transform.position.x>.15f&&hat.transform.localScale==scale;Check(result.twoHandTurn,"Two-hand translation/rotation failed or scaled object");
                before=hat.transform.position;hands.Release(0,false);PhysicsStep(hat,null,1);result.leaveJumpM=Vector3.Distance(before,hat.transform.position);result.stableLeave=result.leaveJumpM<.005f;result.independentRelease=hat.IsHeld&&target.OwnerCount==1;Check(result.stableLeave&&result.independentRelease,"Letting one hand go drops/snaps pickup");hands.Release(1,false);result.lastHandGravity=!hat.IsHeld&&hat.GetComponent<Rigidbody>().useGravity;Check(result.lastHandGravity,"Last hand does not restore gravity");app.Showroom.ResetProps();
                // Real shirt and skirt targets use two separately released patches.
                for(int look=1;look<=6;look++){
                    app.SetLook(look);app.StandForFitting();for(int i=0;i<120;i++)app.AdvanceSimulation(1f/60);
                    var skirt=app.Model.GetComponentInChildren<FittingCloth>();ClothCase(skirt.GetComponent<RunwayVRGrabTarget>(),skirt,look,look,"Skirt");
                    if(look==5){var shirt=app.Model.GetComponentInChildren<InteractiveShirt>();ShirtCase(shirt);}
                }
                app.StandForFitting();var stand=app.PoseStudio.Bases[0];var rig=stand.Rig;rig.ResetPose();
                var wrist=rig.Handles.First(x=>x.Bone.name=="LeftHand");Vector3 wp=wrist.transform.position;wrist.BeginGrab(wp,q);for(int i=0;i<60;i++)wrist.Drag(wp+new Vector3(0,.2f,.1f),q,.016f);wrist.EndGrab();
                var positions=rig.Avatar.Bones.Select(x=>x.localPosition).ToArray();var rotations=rig.Avatar.Bones.Select(x=>x.localRotation).ToArray();Vector3 start=stand.transform.position;
                var marker=stand.transform.Find("Grip here to move stand");Vector3 handle=marker.position;Physics.SyncTransforms();Tick(0,handle,q,0);Tick(0,handle,q,1);Check(stand.IsHeld&&hands.HeldBy(0).IsPoseBase,"Grip on front handle does not move base");
                result.baseJointLock=!wrist.CanGrab;Check(result.baseJointLock,"Joint solver remains active while base moving");
                for(int i=0;i<120;i++){Tick(0,handle+new Vector3(-.35f,.3f,.55f),Quaternion.Euler(0,35,0),1);PhysicsStep(null,stand,1);}
                result.baseMovedM=Vector3.Distance(start,stand.transform.position);result.baseTranslation=result.baseMovedM>.35f;result.baseRotation=Mathf.Abs(Mathf.DeltaAngle(stand.transform.eulerAngles.y,0))>20;Check(result.baseTranslation&&result.baseRotation,"Base does not move/turn entire pose mannequin");
                for(int i=0;i<positions.Length;i++){result.maxBoneDrift=Mathf.Max(result.maxBoneDrift,Vector3.Distance(positions[i],rig.Avatar.Bones[i].localPosition));Check(Quaternion.Angle(rotations[i],rig.Avatar.Bones[i].localRotation)<.01f,"Moving base overwrites pose");}result.basePosePreserved=result.maxBoneDrift<.00001f;Check(result.basePosePreserved,"Base movement stretches bones");
                Physics.SyncTransforms();Vector3 second=stand.transform.Find("Grip here to move stand right").position;Tick(1,second,q,0);Tick(1,second,q,1);result.baseDualHold=hands.HeldBy(0)==hands.HeldBy(1)&&hands.HeldBy(1)&&hands.HeldBy(1).OwnerCount==2;Check(result.baseDualHold,"Both hands cannot hold pose base");
                hands.Release(0,true);Check(stand.IsHeld,"Losing one hand drops base while second holds");hands.Release(1,false);Check(stand.GetComponent<Rigidbody>().useGravity&&!stand.GetComponent<Rigidbody>().isKinematic,"Released base has no gravity");PhysicsStep(null,null,250);
                result.baseLandedY=stand.transform.position.y;result.baseGravity=result.baseLandedY>.015f&&result.baseLandedY<.08f;Check(result.baseGravity,"Base did not settle on showroom floor");Check(wrist.CanGrab,"Joint does not re-enable after base release");stand.ReturnHome();
                // Physics colliders must not convert pose marker spheres into rigid-body contacts.
                Check(rig.Handles.All(h=>h.GetComponent<Collider>().isTrigger),"Joint markers are solid on movable base");
                app.Player.PreviewVRMenu();if(!player.VRMenu)typeof(RunwayPlayer).GetMethod("BuildVRControls",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(player,null);
                var menu=player.VRMenu;menu.SelectPage(0);menu.ActionButtons[1].Activate();Check(app.Playing&&app.Speed==.5f,"Default VR walk not .50");app.StandForFitting();menu.SelectPage(4);menu.ActionButtons[2].Activate();Check(app.Speed==.75f,"Faster menu not .75");menu.ActionButtons[3].Activate();Check(app.Speed==1f,"Fast menu not 1.00");app.StandForFitting();Check(result.exceptions==0,"Runtime errors during bimanual checks");
                Mode(false);hands.Hide();player.FocusPoseStudio(0);Capture("Movable_Pose_Base.png");File.WriteAllText(folder+"/verification.json",JsonUtility.ToJson(result,true));Debug.Log("BIMANUAL_OK "+JsonUtility.ToJson(result));UnityEngine.Object.DestroyImmediate(root);root=null;Physics.simulationMode=mode;EditorApplication.Exit(0);
            }catch(Exception e){result.status="failed";File.WriteAllText(folder+"/verification.json",JsonUtility.ToJson(result,true));File.WriteAllText(folder+"/failure.txt",e.ToString());Debug.LogException(e);Physics.simulationMode=mode;EditorApplication.Exit(1);}
            finally{Physics.simulationMode=mode;Application.logMessageReceived-=Log;if(root){if(player)Mode(false);UnityEngine.Object.DestroyImmediate(root);}}
        }
        static void ShirtCase(InteractiveShirt shirt){var surface=shirt.Surface;var driver=shirt.GetComponentInChildren<BodyBoundSurface>();var target=surface.GetComponent<RunwayVRGrabTarget>();Vector3[] v=surface.MeshAt(0).vertices;int left=0,right=0;float dl=float.PositiveInfinity,dr=float.PositiveInfinity;for(int i=0;i<v.Length;i++){float l=(v[i]-new Vector3(-.1f,1.28f,.16f)).sqrMagnitude,r=(v[i]-new Vector3(.1f,1.28f,.16f)).sqrMagnitude;if(l<dl){dl=l;left=i;}if(r<dr){dr=r;right=i;}}Vector3 a=surface.transform.TransformPoint(v[left]),b=surface.transform.TransformPoint(v[right]);object lOwner=new object(),rOwner=new object();Check(target.TryAcquire(lOwner,a,Quaternion.identity)&&target.TryAcquire(rOwner,b,Quaternion.identity),"Repaired shirt two-hand acquire failed");for(int i=0;i<60;i++){target.DragOwned(lOwner,a+Vector3.forward*.10f,Quaternion.identity);target.DragOwned(rOwner,b+Vector3.forward*.10f,Quaternion.identity);driver.Step(1f/60);}Check(surface.MaximumOffset>.025f&&surface.MaximumOffset<.065f,"Repaired shirt does not respond");Export(7,"TwoHands");target.Release(lOwner,false);Check(target.OwnerCount==1&&surface.GrabCount==1,"Repaired shirt lost remaining hand");Export(7,"OneHand");target.Release(rOwner,true);Check(surface.GrabCount==0,"Repaired shirt did not release");for(int i=0;i<240;i++)driver.Step(1f/60);Export(7,"Settled");result.clothCases++;}
        static void ClothCase(RunwayVRGrabTarget target,Component component,int exportId,int look,string name)
        {
            var solver=(FittingClothSolver)component.GetType().GetField("solver",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(component);object a=new object(),b=new object();var tr=component.transform;
            int segments=component is InteractiveShirt?32:(int)typeof(FittingClothSolver).GetField("segments",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(solver);int rings=solver.Positions.Length/segments;
            int row=Mathf.Min(rings-2,Mathf.Max(3,rings*2/3));Vector3 p=tr.TransformPoint(solver.Positions[row*segments+segments/8]),r=tr.TransformPoint(solver.Positions[row*segments+3*segments/8]);
            Check(target.TryAcquire(a,p,Quaternion.identity)&&target.TryAcquire(b,r,Quaternion.identity),"Two cloth patches not acquired "+name+look);Check(solver.GrabCount==2,"Solver does not have two patches");Check(solver.GrabIndex(0)!=solver.GrabIndex(1),"Patches overlap");
            for(int i=0;i<60;i++){target.DragOwned(a,p+new Vector3(-.08f,.10f,-.20f),Quaternion.identity);target.DragOwned(b,r+new Vector3(.08f,.10f,-.20f),Quaternion.identity);app.AdvanceSimulation(1f/60);}
            result.maxStretch=Mathf.Max(result.maxStretch,solver.MaxStretch());Check(solver.LastPenetratingPoints==0,"Grab penetrates collider proxies: "+name+look+" / "+solver.LastPenetratingPoints);Check(solver.MaxStretch()<1.65f,"Two-hand grip overstretches fabric: "+name+look+" / "+solver.MaxStretch());Export(exportId,"TwoHands");
            target.Release(a,false);Check(solver.GrabCount==1&&target.OwnerCount==1,"One-hand release loses both fabric patches");
            for(int i=0;i<30;i++){target.DragOwned(b,r+new Vector3(.06f,.04f,.08f),Quaternion.identity);app.AdvanceSimulation(1f/60);}Export(exportId,"OneHand");target.Release(b,true);Check(solver.GrabCount==0,"Last fabric patch remains grabbed");
            for(int i=0;i<180;i++)app.AdvanceSimulation(1f/60);Check(solver.LastPenetratingPoints==0,"Released cloth penetrates proxies");Export(exportId,"Settled");result.clothCases++;
        }
        static void Export(int look,string state)
        {
            var body=new Mesh();var original=app.Model.Renderer.sharedMesh;app.Model.Renderer.sharedMesh=app.Model.BodyMesh;try{app.Model.Renderer.BakeMesh(body);}finally{app.Model.Renderer.sharedMesh=original;}
            var vertices=new List<Vector3>();var indices=new List<int>();Action<Mesh,Transform> add=(m,tr)=>{int first=vertices.Count;var matrix=app.Model.transform.worldToLocalMatrix*tr.localToWorldMatrix;foreach(var p in m.vertices)vertices.Add(matrix.MultiplyPoint3x4(p));foreach(int n in m.triangles)indices.Add(first+n);};
            foreach(var f in app.Model.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh)add(f.sharedMesh,f.transform);foreach(var s in app.Model.GetComponentsInChildren<SkinnedMeshRenderer>())if(s!=app.Model.Renderer){var m=new Mesh();s.BakeMesh(m);add(m,s.transform);UnityEngine.Object.DestroyImmediate(m);}
            using(var w=new BinaryWriter(File.Create(folder+"/Contact/Look"+look+"_"+state+".bin"))){w.Write(0x53434631);Write(w,body.vertices,body.triangles);Write(w,vertices.ToArray(),indices.ToArray());}UnityEngine.Object.DestroyImmediate(body);result.meshExports++;
        }
        static void Write(BinaryWriter w,Vector3[] p,int[] t){w.Write(p.Length);foreach(var v in p){w.Write(v.x);w.Write(v.y);w.Write(v.z);}w.Write(t.Length);foreach(int i in t)w.Write(i);}
        static void Capture(string name){if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;var c=player.View;c.rect=new Rect(0,0,1,1);var rt=new RenderTexture(1100,900,24);var image=new Texture2D(1100,900,TextureFormat.RGB24,false);var old=RenderTexture.active;try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1100,900),0,0);image.Apply();File.WriteAllBytes(folder+"/"+name,image.EncodeToPNG());}finally{c.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(rt);}}
    }
}
#endif
