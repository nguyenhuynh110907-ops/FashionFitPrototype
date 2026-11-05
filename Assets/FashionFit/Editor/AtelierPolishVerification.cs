#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FashionFit.Editor
{
    public static class AtelierPolishVerification
    {
        [Serializable] sealed class Result
        {
            public string status="passed";public int checks,previews,previewVertices,panelButtons,wardrobeChoices,shaderErrors;
            public bool worldAnchored,menuEdges,recenter,hover,buttonRaycasts,previewDoesNotStartXR,gripHidesMenu,closePreview,settings;
            public float panelDistanceM,actionTargetAngleDeg;public string scope="Unity mono renders, software input/collider checks, shader compile; not a headset test";
        }
        static Result result;static string Folder=>Path.GetFullPath("Verification/AtelierPolish");
        static void Require(bool condition,string message){result.checks++;if(!condition)throw new InvalidOperationException(message);}
        public static void RunBatch()
        {
            Directory.CreateDirectory(Folder);result=new Result();
            try{
                var root=new GameObject("Atelier verification");var app=root.AddComponent<FashionFitApp>();typeof(FashionFitApp).GetMethod("Start",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(app,null);
                Require(app.Ready,"Startup failed: "+app.FatalError);result.previews=app.Showroom.BakedPreviewCount;Require(result.previews==6,"Missing baked display meshes");
                foreach(var f in app.Showroom.GetComponentsInChildren<MeshFilter>())if(f.gameObject.name.StartsWith("Baked garment preview"))result.previewVertices+=f.sharedMesh.vertexCount;
                Require(result.previewVertices>10000&&result.previewVertices<40000,"Display mesh budget unexpected");
                Require(root.GetComponentsInChildren<FittingCloth>().Length==1,"Display generated extra cloth solvers");
                for(int i=0;i<6;i++){var mesh=Resources.Load<Mesh>("Showroom/Look"+(i+1).ToString("00"));Require(mesh&&mesh.bounds.size.x<.495f&&mesh.bounds.size.y>.15f,"Preview does not fit its slot "+i);}
                Capture(app,Path.Combine(Folder,"Wardrobe.png"),new Vector3(-2.05f,1.47f,1.35f),new Vector3(-2.05f,1.12f,-1.7f),58);
                Capture(app,Path.Combine(Folder,"Atelier.png"),new Vector3(1.7f,2.15f,4),new Vector3(-1.2f,1.1f,-.3f),64);
                app.Player.PreviewVRMenu();var panel=app.Player.VRMenu;result.previewDoesNotStartXR=!app.Player.InVR&&!app.Player.StartingVR;Require(result.previewDoesNotStartXR,"Preview started XR");
                result.panelButtons=panel.Buttons.Count;Require(result.panelButtons==10&&panel.ActionButtons.Count==6,"Wrong page density");result.panelDistanceM=Vector3.Distance(app.Player.View.transform.position,panel.transform.position);Require(result.panelDistanceM>.8f&&result.panelDistanceM<1.6f,"Panel not at comfortable ray distance");
                result.actionTargetAngleDeg=2*Mathf.Atan(.13f/(2*RunwayVRPanel.Distance))*Mathf.Rad2Deg;Require(result.actionTargetAngleDeg>5,"VR button too small");
                Vector3 anchor=panel.transform.position;app.Player.View.transform.localPosition+=new Vector3(.15f,-.08f,.12f);app.Player.View.transform.localRotation*=Quaternion.Euler(20,25,15);
                result.worldAnchored=panel.transform.parent!=app.Player.View.transform&&!panel.transform.IsChildOf(app.Player.transform)&&panel.transform.position==anchor;Require(result.worldAnchored,"Menu follows head pitch/roll");
                app.Player.HandleMenuButtons(true,false);bool hidden=!panel.Visible;app.Player.HandleMenuButtons(true,false);bool noRepeat=!panel.Visible;app.Player.HandleMenuButtons(false,false);app.Player.HandleMenuButtons(true,false);result.menuEdges=hidden&&noRepeat&&panel.Visible;Require(result.menuEdges,"Y menu toggle repeats or fails");
                app.Player.HandleMenuButtons(false,false);panel.transform.position+=Vector3.left;app.Player.HandleMenuButtons(false,true);result.recenter=Vector3.Distance(panel.transform.position,anchor)>.05f&&Vector3.Distance(panel.transform.position,app.Player.View.transform.position)<1.6f;Require(result.recenter,"X did not recenter menu");app.Player.HandleMenuButtons(false,false);
                panel.SelectPage(1);for(int i=0;i<6;i++){panel.ActionButtons[i].Activate();panel.Refresh();Require(app.Look==i+1&&panel.ActionButtons[i].Label.Contains("ĐANG MẶC"),"Wrong wardrobe selection/feedback");result.wardrobeChoices++;}
                var button=panel.ActionButtons[0];button.SetHovered(true);result.hover=button.IsHovered;button.SetHovered(false);result.hover&=!button.IsHovered;Require(result.hover,"Hover feedback not reset");
                Physics.SyncTransforms();result.buttonRaycasts=true;foreach(var b in panel.ActionButtons){var origin=app.Player.View.transform.position;var ray=new Ray(origin,(b.transform.position-origin).normalized);result.buttonRaycasts&=Physics.Raycast(ray,out RaycastHit hit,3,1<<8,QueryTriggerInteraction.Ignore)&&hit.collider.GetComponent<RunwayVRButton>()==b;}Require(result.buttonRaycasts,"Panel backing or another hitbox blocks buttons");
                panel.SelectPage(2);bool turn=app.Player.VRSnapTurn;panel.ActionButtons[0].Activate();float speed=app.Player.VRWalkSpeed;panel.ActionButtons[1].Activate();bool quality=app.Player.LowQuality;panel.ActionButtons[2].Activate();result.settings=app.Player.VRSnapTurn!=turn&&app.Player.VRWalkSpeed!=speed&&app.Player.LowQuality!=quality;Require(result.settings,"Settings actions not connected");
                // All pages use the same fixed layout; a mono preview is kept for visual QA.
                app.Player.View.transform.SetPositionAndRotation(new Vector3(.2f,1.65f,2.5f),Quaternion.Euler(0,180,0));app.Player.View.cullingMask=1<<8;app.Player.View.clearFlags=CameraClearFlags.SolidColor;app.Player.View.backgroundColor=new Color(.20f,.24f,.29f);app.Player.View.fieldOfView=60;
                for(int page=0;page<3;page++){panel.SelectPage(page);panel.PlaceInFront();Capture(app,Path.Combine(Folder,"VR_Page"+page+".png"),app.Player.View.transform.position,app.Player.View.transform.position+app.Player.View.transform.forward,60);}
                panel.SelectPage(2);panel.ActionButtons[5].Activate();result.closePreview=!panel.Visible&&!app.Player.InVR;Require(result.closePreview,"Preview close affects XR state");panel.Show();
                var hat=app.Showroom.Hats[0];Physics.SyncTransforms();bool grabbed=app.Player.TryBeginGrip(new Ray(hat.transform.position+Vector3.up*.5f,Vector3.down),Quaternion.identity);result.gripHidesMenu=grabbed&&!panel.Visible&&hat.IsHeld;Require(result.gripHidesMenu,"Grip does not clear menu");app.Player.EndActiveGrab();app.Showroom.ResetProps();
                foreach(string shaderName in new[]{"Runway/VRPanel","Runway/WorldText"}){var shader=Shader.Find(shaderName);Require(shader!=null,"Missing UI shader");foreach(var message in ShaderUtil.GetShaderMessages(shader))if(message.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)result.shaderErrors++;}Require(result.shaderErrors==0,"UI shader compilation error");
                File.WriteAllText(Path.Combine(Folder,"verification.json"),JsonUtility.ToJson(result,true));Debug.Log("ATELIER_POLISH_OK "+JsonUtility.ToJson(result));UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(0);
            }catch(Exception e){result.status="failed";File.WriteAllText(Path.Combine(Folder,"verification.json"),JsonUtility.ToJson(result,true));File.WriteAllText(Path.Combine(Folder,"failure.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void Capture(FashionFitApp app,string path,Vector3 position,Vector3 target,float fov)
        {
            var c=app.Player.View;c.rect=new Rect(0,0,1,1);c.transform.position=position;c.transform.LookAt(target);c.fieldOfView=fov;var rt=new RenderTexture(1400,1000,24);var image=new Texture2D(1400,1000,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,1400,1000),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}finally{c.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);}
        }
    }
}
#endif
