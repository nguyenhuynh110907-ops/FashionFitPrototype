#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace FashionFit.Editor
{
    public static class PoseBaseCollisionVerification
    {
        public static void RunBatch()
        {
            string folder=Path.GetFullPath("Verification/Bimanual");Directory.CreateDirectory(folder);int checks=0;GameObject root=null;var mode=Physics.simulationMode;
            Action<bool,string> check=(b,s)=>{checks++;if(!b)throw new InvalidOperationException(s);};
            try{
                Physics.simulationMode=SimulationMode.Script;root=new GameObject("Pose base collision QA");var app=root.AddComponent<FashionFitApp>();typeof(FashionFitApp).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(app,null);check(app.Ready,"Startup failed");app.Player.View.transform.position=new Vector3(0,1.6f,3);
                var b=app.PoseStudio.Bases[0];var target=b.GetComponent<RunwayVRGrabTarget>();object owner=new object();Vector3 start=b.transform.position,p=start+new Vector3(0,.87f,.43f);var q=Quaternion.identity;check(target.TryAcquire(owner,p,q),"Base acquisition failed");
                for(int i=0;i<100;i++){target.DragOwned(owner,p+Vector3.forward*.5f,q);b.StepHeld(.02f);Physics.Simulate(.02f);Physics.SyncTransforms();}
                check(b.transform.position.z-start.z>.45f,"Floor stops horizontal base dragging");check(b.transform.position.y<.05f,"Horizontal drag lifts stand off floor");target.Release(owner,true);b.ReturnHome();Physics.SyncTransforms();
                var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.SetParent(root.transform);wall.layer=10;wall.transform.position=start+new Vector3(0,.8f,.65f);wall.transform.localScale=new Vector3(2.4f,1.7f,.025f);Physics.SyncTransforms();check(target.TryAcquire(owner,p,q),"Wall test acquisition failed");
                for(int i=0;i<150;i++){target.DragOwned(owner,p+Vector3.forward*1.2f,q);b.StepHeld(.02f);Physics.Simulate(.02f);Physics.SyncTransforms();}
                var box=b.GetComponent<BoxCollider>();check(box.bounds.max.z<=wall.GetComponent<Collider>().bounds.min.z+.006f,"Base passed through wall fixture");target.Release(owner,true);UnityEngine.Object.DestroyImmediate(wall);b.ReturnHome();Physics.SyncTransforms();
                check(target.TryAcquire(owner,p,q),"Bounds acquisition failed");for(int i=0;i<600;i++){target.DragOwned(owner,p+new Vector3(9,.4f,9),q);b.StepHeld(.02f);Physics.Simulate(.02f);Physics.SyncTransforms();}check(Mathf.Abs(b.transform.position.x)<=3.401f&&Mathf.Abs(b.transform.position.z)<=3.401f,"Stand left room bounds");
                target.Release(owner,true);app.PoseStudio.ResetPose(0);check(Vector3.Distance(start,b.transform.position)<.0001f,"Reset pose does not reset base position");
                File.WriteAllText(folder+"/base-collision.json","{\"status\":\"passed\",\"checks\":"+checks+",\"floorSlide\":true,\"thinWallBlocked\":true,\"roomBounds\":true,\"resetHome\":true}");Debug.Log("POSE_BASE_COLLISION_OK");UnityEngine.Object.DestroyImmediate(root);root=null;Physics.simulationMode=mode;EditorApplication.Exit(0);
            }catch(Exception e){File.WriteAllText(folder+"/base-collision.json","{\"status\":\"failed\",\"checks\":"+checks+"}");File.WriteAllText(folder+"/base-collision-failure.txt",e.ToString());Debug.LogException(e);Physics.simulationMode=mode;EditorApplication.Exit(1);}finally{Physics.simulationMode=mode;if(root)UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
#endif
