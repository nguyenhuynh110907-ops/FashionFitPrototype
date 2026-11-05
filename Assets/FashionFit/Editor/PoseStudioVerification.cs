#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace FashionFit.Editor
{
    public static class PoseStudioVerification
    {
        [Serializable] sealed class Result {
            public string status="passed",scope="Unity mono renders and injected Grip/pose checks; no physical Quest/controller test";
            public int checks,models,handles,gripSelections,nearGrips,posedJoints,deformedVertices,prefabs;
            public float femaleHeight,maleHeight,maxLengthDrift,maxResetError,maxHeldDrift,maxQuaternionError,maxJointAngleExcess;
            public bool jointMenu,handlesToggle,noExtraCloth,walkBounds,invalidInputIgnored;
        }
        static Result result;static readonly string Folder=Path.GetFullPath("Verification/PoseStudio");
        static void Require(bool condition,string message){result.checks++;if(!condition)throw new InvalidOperationException(message);}
        public static void RunBatch()
        {
            Directory.CreateDirectory(Folder);result=new Result();
            try{
                PoseMannequinAssets.Rebuild();
                var root=new GameObject("Pose studio verification");var app=root.AddComponent<FashionFitApp>();typeof(FashionFitApp).GetMethod("Start",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(app,null);
                Require(app.Ready&&app.PoseStudio,"Startup: "+app.FatalError);result.models=app.PoseStudio.Rigs.Count;Require(result.models==2,"Need female and male");
                result.noExtraCloth=root.GetComponentsInChildren<FittingCloth>().Length==1;Require(result.noExtraCloth,"Pose models created extra cloth solvers");
                Require(!app.PoseStudio.HandlesVisible,"Cloth mode should start without joint markers stealing touch");app.PoseStudio.SetHandles(true);
                for(int i=0;i<2;i++){
                    var rig=app.PoseStudio.Rigs[i];var avatar=rig.Avatar;result.handles+=rig.Handles.Count;
                    Require(avatar.Bones.Length==17&&rig.Handles.Count==17,"Missing bones/handles");
                    float height=avatar.BodyMesh.bounds.size.y;if(i==0)result.femaleHeight=height;else result.maleHeight=height;Require(Mathf.Abs(height-(i==0?1.72f:1.82f))<.002f,"Wrong real-world scale");
                    Require(avatar.BodyMesh.vertexCount==13380,"Unexpected mesh budget");
                    var restPositions=avatar.Bones.Select(b=>b.localPosition).ToArray();var restRotations=avatar.Bones.Select(b=>b.localRotation).ToArray();
                    foreach(var handle in rig.Handles){
                        rig.ResetPose();Physics.SyncTransforms();Vector3 point=handle.transform.position;
                        bool grip=app.Player.TryBeginGrip(new Ray(point+rig.transform.forward*.3f,-rig.transform.forward),Quaternion.identity);
                        Require(grip&&handle.IsHeld,"Ray Grip did not select "+rig.Label+" / "+handle.Label);result.gripSelections++;
                        bool leg=handle.Bone.name.Contains("Leg")||handle.Bone.name.Contains("Foot");Vector3 delta=Vector3.up*.20f+rig.transform.forward*(leg?-.22f:.22f);
                        for(int step=0;step<90;step++)handle.Drag(point+delta,Quaternion.AngleAxis(leg?28:32,rig.transform.right),1f/60);
                        app.Player.EndActiveGrab();Require(!handle.IsHeld&&!rig.IsHeld,"Release did not end Grip");
                        bool changed=false;for(int b=0;b<avatar.Bones.Length;b++){
                            float drift=Vector3.Distance(avatar.Bones[b].localPosition,restPositions[b]);result.maxLengthDrift=Mathf.Max(result.maxLengthDrift,drift);
                            Require(drift<.00001f,"Bone translated/stretched");
                            Quaternion q=avatar.Bones[b].localRotation;Require(!float.IsNaN(q.x)&&!float.IsInfinity(q.x),"Invalid rotation");float norm=Mathf.Abs(1-(q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w));result.maxQuaternionError=Mathf.Max(result.maxQuaternionError,norm);
                            string name=avatar.Bones[b].name;float max=name=="Hips"?35:name=="Spine"?22:name=="Spine2"?30:name=="Neck"?35:name=="Head"?65:name.Contains("UpLeg")?100:name.Contains("ForeArm")||name.EndsWith("Leg")?145:name.EndsWith("Arm")?110:name.EndsWith("Hand")?70:35;result.maxJointAngleExcess=Mathf.Max(result.maxJointAngleExcess,Quaternion.Angle(q,restRotations[b])-max);Require(result.maxJointAngleExcess<.1f,"Joint exceeded rotation limit");
                            changed|=Quaternion.Angle(q,restRotations[b])>1;
                        }
                        Require(changed,"Joint did not change pose: "+handle.Label);result.posedJoints++;
                        var heldRotations=avatar.Bones.Select(b=>b.localRotation).ToArray();for(int step=0;step<10;step++)app.AdvanceSimulation(1f/60);
                        for(int b=0;b<avatar.Bones.Length;b++)result.maxHeldDrift=Mathf.Max(result.maxHeldDrift,Quaternion.Angle(heldRotations[b],avatar.Bones[b].localRotation));
                        Require(result.maxHeldDrift<.1f,"Released pose overwritten by animation");
                        rig.ResetPose();for(int b=0;b<avatar.Bones.Length;b++)result.maxResetError=Mathf.Max(result.maxResetError,Quaternion.Angle(avatar.Bones[b].localRotation,restRotations[b]));
                        Require(result.maxResetError<.1f,"Reset did not restore joints");
                    }
                    // Body vertices really follow the skeleton, rather than only moving markers.
                    var before=new Mesh();var after=new Mesh();avatar.Renderer.BakeMesh(before);
                    var wrist=rig.Handles.First(h=>h.Bone.name=="LeftHand");Vector3 wristStart=wrist.transform.position;wrist.BeginGrab(wristStart,Quaternion.identity);for(int step=0;step<90;step++)wrist.Drag(wristStart+Vector3.up*.25f+rig.transform.forward*.15f,Quaternion.identity,1f/60);wrist.EndGrab();avatar.Renderer.BakeMesh(after);
                    var beforeVertices=before.vertices;var afterVertices=after.vertices;bool finite=true;
                    for(int v=0;v<before.vertexCount;v++){finite&=!float.IsNaN(afterVertices[v].x)&&!float.IsInfinity(afterVertices[v].x);if(Vector3.Distance(beforeVertices[v],afterVertices[v])>.002f)result.deformedVertices++;}Require(finite,"Non-finite deformed mesh");
                    Require(result.deformedVertices>100,"Mesh did not deform");ObjectDestroy(before);ObjectDestroy(after);
                    rig.ResetPose();Physics.SyncTransforms();Vector3 nearPoint=wrist.transform.position;Vector3 handPosition=wrist.Bone.position;
                    bool near=app.Player.TryBeginGrip(new Ray(nearPoint,rig.transform.forward),Quaternion.identity);Require(near&&wrist.IsHeld,"Near Grip did not select wrist");result.nearGrips++;
                    wrist.GetComponent<RunwayVRGrabTarget>().Drag(nearPoint,Quaternion.identity);Require(Vector3.Distance(wrist.Bone.position,handPosition)<.02f,"Near Grip jumps on first drag");
                    Quaternion safe=wrist.Bone.localRotation;wrist.Drag(new Vector3(float.NaN,0,0),Quaternion.identity,1f/60);result.invalidInputIgnored=Quaternion.Angle(safe,wrist.Bone.localRotation)<.001f;Require(result.invalidInputIgnored,"Invalid tracking input damaged pose");app.Player.EndActiveGrab();rig.ResetPose();
                    // Leave an intentional arm bend for the before/after picture.
                    Vector3 capturePoint=wrist.transform.position;wrist.BeginGrab(capturePoint,Quaternion.identity);for(int step=0;step<90;step++)wrist.Drag(capturePoint+Vector3.up*.25f+rig.transform.forward*.15f,Quaternion.identity,1f/60);wrist.EndGrab();
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PoseMannequinAssets.Folder+"/"+(i==0?"Female":"Male")+"CC0.prefab");Require(prefab&&prefab.GetComponent<SkinnedMeshRenderer>().bones.Length==17,"Invalid native Unity prefab");result.prefabs++;
                }
                Capture(app,"TwoModels_Posed.png",new Vector3(2.2f,1.35f,.95f),new Vector3(2.2f,1.08f,-1.9f),51);
                app.PoseStudio.ResetPose(0);app.PoseStudio.ResetPose(1);app.PoseStudio.SetHandles(false);Capture(app,"TwoModels_Rest.png",new Vector3(2.2f,1.35f,.95f),new Vector3(2.2f,1.08f,-1.9f),51);
                app.Player.PreviewVRMenu();var menu=app.Player.VRMenu;menu.SelectPage(2);menu.ActionButtons[4].Activate();result.jointMenu=menu.Page==3;Require(result.jointMenu,"Missing VR joint page");
                menu.ActionButtons[2].Activate();bool shown=app.PoseStudio.HandlesVisible;menu.ActionButtons[2].Activate();result.handlesToggle=shown&&!app.PoseStudio.HandlesVisible;Require(result.handlesToggle,"VR marker toggle broken");
                for(int i=0;i<2;i++){Vector3 start=app.PoseStudio.Rigs[i].transform.position;Vector3 resolved=app.Showroom.ResolveWalk(start,Vector3.zero,.22f,true);Vector3 d=resolved-start;d.y=0;Require(d.magnitude>.44f,"Walking passes through pose model");}result.walkBounds=true;
                app.Player.View.cullingMask=1<<8;menu.SelectPage(3);menu.PlaceInFront();Capture(app,"VR_JointMenu.png",app.Player.View.transform.position,app.Player.View.transform.position+app.Player.View.transform.forward,60);
                File.WriteAllText(Path.Combine(Folder,"verification.json"),JsonUtility.ToJson(result,true));Debug.Log("POSE_STUDIO_OK "+JsonUtility.ToJson(result));ObjectDestroy(root);EditorApplication.Exit(0);
            }catch(Exception e){result.status="failed";File.WriteAllText(Path.Combine(Folder,"verification.json"),JsonUtility.ToJson(result,true));File.WriteAllText(Path.Combine(Folder,"failure.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void ObjectDestroy(UnityEngine.Object value){UnityEngine.Object.DestroyImmediate(value);}
        static void Capture(FashionFitApp app,string file,Vector3 position,Vector3 target,float fov)
        {
            var camera=app.Player.View;camera.rect=new Rect(0,0,1,1);camera.transform.position=position;camera.transform.LookAt(target);camera.fieldOfView=fov;var rt=new RenderTexture(1500,1100,24);var texture=new Texture2D(1500,1100,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            // Camera.Render can reuse a skinned buffer within one editor frame.
            // Render fresh CPU-baked copies for deterministic pose screenshots.
            var snapshots=new System.Collections.Generic.List<GameObject>();var meshes=new System.Collections.Generic.List<Mesh>();
            foreach(var rig in app.PoseStudio.Rigs){var skin=rig.Avatar.Renderer;var mesh=new Mesh();skin.BakeMesh(mesh);meshes.Add(mesh);var go=new GameObject("Pose render snapshot");go.transform.SetPositionAndRotation(skin.transform.position,skin.transform.rotation);go.transform.localScale=skin.transform.lossyScale;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=skin.sharedMaterial;skin.enabled=false;snapshots.Add(go);}
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1500,1100),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(Folder,file),texture.EncodeToPNG());}finally{foreach(var go in snapshots)ObjectDestroy(go);foreach(var mesh in meshes)ObjectDestroy(mesh);foreach(var rig in app.PoseStudio.Rigs)rig.Avatar.Renderer.enabled=true;camera.targetTexture=null;RenderTexture.active=previous;ObjectDestroy(rt);ObjectDestroy(texture);}
        }
    }
}
#endif
