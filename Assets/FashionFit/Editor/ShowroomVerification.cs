#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;

namespace FashionFit.Editor
{
    public static class ShowroomVerification
    {
        [Serializable] sealed class Result
        {
            public string status="passed";
            public int hats,garmentSamples,lookSelections;
            public bool rayGrip,nearGrip,occlusion,gravityTable,gravityFloor,hatAttach,hatReplace,reset,collisionBoundary,openXRLoader,touchProfile;
            public int totalClothSolvers;
            public int vrPanelButtons;
            public int hatMeshes,hatVertices,wearChecks;
            public float capHeadEnvelopeMax;
        }
        static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        public static void RunBatch()
        {
            string folder=Path.GetFullPath("Verification/ReferenceHats");Directory.CreateDirectory(folder);var result=new Result();
            var originalMode=Physics.simulationMode;
            try{
                var root=new GameObject("Showroom verification");var app=root.AddComponent<FashionFitApp>();
                typeof(FashionFitApp).GetMethod("Start",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(app,null);
                Require(app.Ready&&app.Showroom!=null,"App/showroom startup failed: "+app.FatalError);
                foreach(var item in app.Showroom.Items){if(item.Look==0)result.hats++;else result.garmentSamples++;Require(item.GetComponent<Rigidbody>().isKinematic&&item.GetComponent<Collider>()!=null,"Invalid display physics");}
                Require(result.hats==10&&result.garmentSamples==6,"Wrong display count");
                foreach(var item in app.Showroom.Hats)foreach(var f in item.GetComponentsInChildren<MeshFilter>()){
                    result.hatMeshes++;result.hatVertices+=f.sharedMesh.vertexCount;
                    foreach(var p in f.sharedMesh.vertices)Require(!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&!float.IsNaN(p.z)&&p.sqrMagnitude<1,"Invalid hat geometry");
                }
                var hat=app.Showroom.Items[0];Vector3 home=hat.transform.position;Physics.SyncTransforms();
                result.rayGrip=app.Player.TryBeginGrip(new Ray(home+Vector3.up*.5f,Vector3.down),Quaternion.identity)&&hat.IsHeld;
                Require(result.rayGrip,"Ray grip did not select hat");
                app.Player.EndActiveGrab();Physics.simulationMode=SimulationMode.Script;
                for(int i=0;i<200;i++)Physics.Simulate(.01f);
                float tableBottom=hat.GetComponent<Collider>().bounds.min.y;
                result.gravityTable=!hat.GetComponent<Rigidbody>().isKinematic&&tableBottom>.82f&&tableBottom<.85f;
                Require(result.gravityTable,"Released hat fell through table: "+hat.transform.position);
                app.Showroom.ResetProps();Physics.SyncTransforms();
                result.occlusion=!app.Player.TryBeginGrip(new Ray(new Vector3(home.x,.65f,home.z),Vector3.up),Quaternion.identity);
                Require(result.occlusion,"Grip passed through table");
                result.nearGrip=app.Player.TryBeginGrip(new Ray(home+new Vector3(.05f,.07f,0),Vector3.right),Quaternion.identity)&&hat.IsHeld;
                Require(result.nearGrip,"Near-hand grip failed");app.Player.EndActiveGrab();hat.ReturnToDisplay();
                hat.BeginGrip(hat.transform.position,Quaternion.identity);
                for(int i=0;i<100;i++)hat.MoveGrip(app.Showroom.HatPoint,Quaternion.identity,.02f);
                hat.ReleaseGrip();result.hatAttach=hat.IsEquipped&&Vector3.Distance(hat.transform.position,app.Showroom.HatPoint+hat.HatFitOffset)<.001f;
                Require(result.hatAttach,"Hat release did not attach to head");app.Showroom.WearHat(1);
                result.hatReplace=!hat.IsEquipped&&app.Showroom.Items[1].IsEquipped;Require(result.hatReplace,"Hat replacement failed");
                app.Showroom.ResetProps();hat.BeginGrip(hat.transform.position,Quaternion.identity);
                for(int i=0;i<100;i++)hat.MoveGrip(new Vector3(.8f,1,1.5f),Quaternion.identity,.02f);
                hat.ReleaseGrip();Physics.SyncTransforms();for(int i=0;i<250;i++)Physics.Simulate(.01f);
                float floorBottom=hat.GetComponent<Collider>().bounds.min.y;
                result.gravityFloor=floorBottom>-.03f&&floorBottom<.03f;Require(result.gravityFloor,"Hat fell through floor");
                for(int i=0;i<app.Showroom.Items.Count;i++){
                    var item=app.Showroom.Items[i];if(item.Look==0)continue;item.BeginGrip(item.transform.position,Quaternion.identity);
                    Require(root.GetComponentsInChildren<FittingCloth>().Length==1,"Grabbing display duplicated cloth solver");
                    for(int k=0;k<150;k++)item.MoveGrip(app.Showroom.WearPoint-Vector3.up*.3f,Quaternion.identity,.02f);
                    item.ReleaseGrip();Require(app.Look==item.Look&&!item.IsHeld,"Garment release did not select look "+item.Look);result.lookSelections++;
                }
                app.Showroom.ResetProps();result.reset=true;foreach(var item in app.Showroom.Items)result.reset&=!item.IsHeld&&!item.IsEquipped&&item.GetComponent<Rigidbody>().isKinematic;
                Require(result.reset,"Reset left a held/dynamic item");
                Vector3 inside=new Vector3(-1.95f,1.65f,.65f);Vector3 corrected=app.Showroom.ConstrainPosition(inside,.22f);
                result.collisionBoundary=Vector3.Distance(inside,corrected)>.3f;Require(result.collisionBoundary,"Furniture movement boundary failed");
                var settings=XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
                if(settings!=null&&settings.Manager!=null)foreach(var loader in settings.Manager.activeLoaders)if(loader&&loader.GetType().FullName=="UnityEngine.XR.OpenXR.OpenXRLoader")result.openXRLoader=true;
                var xr=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
                result.touchProfile=xr&&xr.GetFeature<OculusTouchControllerProfile>()&&xr.GetFeature<OculusTouchControllerProfile>().enabled;
                Require(result.openXRLoader&&result.touchProfile,"Quest OpenXR configuration missing");
                result.totalClothSolvers=root.GetComponentsInChildren<FittingCloth>().Length;
                Capture(app,Path.Combine(folder,"Showroom.png"),new Vector3(1.7f,2.0f,4.0f),new Vector3(-1.2f,1.05f,-.3f),58);
                Capture(app,Path.Combine(folder,"HatTable.png"),new Vector3(-.25f,2.2f,3.0f),new Vector3(-2.05f,1.0f,.65f),56);
                Capture(app,Path.Combine(folder,"Wardrobe.png"),new Vector3(-2.1f,1.6f,.75f),new Vector3(-2.1f,1.2f,-1.65f),58);
                for(int i=0;i<app.Showroom.Hats.Count;i++){
                    var item=app.Showroom.Hats[i];app.Showroom.WearHat(i);Require(item.IsEquipped,"Hat selection failed: "+i);result.wearChecks++;
                    if(i==0||i==1||i==4||i==6||i==9){
                        var headMesh=new Mesh();app.Model.Renderer.BakeMesh(headMesh);
                        foreach(var p in headMesh.vertices){Vector3 local=item.transform.InverseTransformPoint(app.Model.Renderer.transform.TransformPoint(p));if(local.y<.005f)continue;
                            float q=local.x*local.x/(.094f*.094f)+local.y*local.y/(.126f*.126f)+local.z*local.z/(.110f*.110f);result.capHeadEnvelopeMax=Mathf.Max(result.capHeadEnvelopeMax,q);}
                        UnityEngine.Object.DestroyImmediate(headMesh);Require(result.capHeadEnvelopeMax<=1.001f,"Cap crown does not enclose sampled upper head");
                    }
                    Capture(app,Path.Combine(folder,"Worn_"+(i+1).ToString("00")+".png"),new Vector3(.40f,1.87f,.74f),new Vector3(0,1.74f,0),44);
                }
                app.Showroom.ResetProps();
                for(int i=0;i<app.Showroom.Hats.Count;i++){
                    var item=app.Showroom.Hats[i];foreach(var tr in item.GetComponentsInChildren<Transform>())tr.gameObject.layer=29;
                    var c=app.Player.View;c.cullingMask=1<<29;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.65f,.67f,.69f);
                    Vector3 centre=item.transform.position+item.GetComponent<BoxCollider>().center;
                    Capture(app,Path.Combine(folder,"Hat_"+(i+1).ToString("00")+".png"),centre+new Vector3(-.30f,.17f,.64f),centre,48);
                    foreach(var tr in item.GetComponentsInChildren<Transform>())tr.gameObject.layer=0;item.gameObject.layer=9;
                }
                typeof(RunwayPlayer).GetMethod("BuildVRControls",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(app.Player,null);
                result.vrPanelButtons=app.Player.VRMenu.Buttons.Count;Require(result.vrPanelButtons==10,"VR panel missing paged actions");
                File.WriteAllText(Path.Combine(folder,"verification.json"),JsonUtility.ToJson(result,true));
                UnityEngine.Object.DestroyImmediate(root);Debug.Log("SHOWROOM_OK "+JsonUtility.ToJson(result));EditorApplication.Exit(0);
            }catch(Exception e){result.status="failed";File.WriteAllText(Path.Combine(folder,"verification.json"),JsonUtility.ToJson(result,true));File.WriteAllText(Path.Combine(folder,"failure.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
            finally{Physics.simulationMode=originalMode;}
        }
        static void Capture(FashionFitApp app,string path,Vector3 position,Vector3 target,float fov)
        {
            var c=app.Player.View;c.rect=new Rect(0,0,1,1);c.transform.position=position;c.transform.LookAt(target);c.fieldOfView=fov;
            var rt=new RenderTexture(1400,1000,24);var image=new Texture2D(1400,1000,TextureFormat.RGB24,false);var old=RenderTexture.active;
            try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1400,1000),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally{c.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);}
        }
    }
}
#endif
