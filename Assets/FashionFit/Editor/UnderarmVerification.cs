#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FashionFit.Editor
{
    public static class UnderarmVerification
    {
        [Serializable] sealed class ViewResult{public string view;public int exposedPixels;}
        [Serializable] sealed class VisiblePoint{public string view;public Vector3 position,normal;}
        [Serializable] sealed class VisiblePoints{public VisiblePoint[] samples;}
        [Serializable] sealed class Result{public string status;public string scope="Six 512px orthographic views of covered armpit band; full unmasked body; not all viewing angles/continuous contact";public ViewResult[] views;public int exposedPixels,bodyOnlyControlPixels,slowSteps,proxyViolations;public bool fittedCache,cacheRefreshPreservedScene;}
        static int CountRed(Texture2D image){int red=0;foreach(Color32 pixel in image.GetPixels32())if(pixel.r>180&&pixel.g<40&&pixel.b<40)red++;return red;}
        static void ReadWorldPoints(Camera camera,Material mask,string view,List<VisiblePoint> points)
        {
            var rt=new RenderTexture(512,512,24,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);var tex=new Texture2D(512,512,TextureFormat.RGBAFloat,false,true);var old=RenderTexture.active;
            try{mask.SetFloat("_Positions",1);camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,512,512),0,0);tex.Apply();
                foreach(var pixel in tex.GetPixels())if(Mathf.Abs(pixel.r)>.15f&&Mathf.Abs(pixel.r)<.255f&&pixel.g>1.275f&&pixel.g<1.42f)points.Add(new VisiblePoint{view=view,position=new Vector3(pixel.r,pixel.g,pixel.b),normal=Vector3.back});
            }finally{mask.SetFloat("_Positions",0);camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
        }
        static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Verification/UnderarmFix"));
        static void Export(FashionFitApp app,TailoredPoloStudy study,string name)
        {
            var body=new Mesh();var upper=new Mesh();app.Model.Renderer.BakeMesh(body);study.UpperSurface.BakeMesh(upper);
            var p=upper.vertices;var matrix=app.Model.transform.worldToLocalMatrix*study.UpperSurface.transform.localToWorldMatrix;
            for(int i=0;i<p.Length;i++)p[i]=matrix.MultiplyPoint3x4(p[i]);
            using(var writer=new BinaryWriter(File.Create(Path.Combine(Folder,"Look6_"+name+".bin")))){writer.Write(0x53434631);Write(writer,body.vertices,body.triangles);Write(writer,p,upper.triangles);}
            UnityEngine.Object.DestroyImmediate(body);UnityEngine.Object.DestroyImmediate(upper);
        }
        static void Write(BinaryWriter writer,Vector3[] p,int[] triangles){writer.Write(p.Length);foreach(var v in p){writer.Write(v.x);writer.Write(v.y);writer.Write(v.z);}writer.Write(triangles.Length);foreach(int v in triangles)writer.Write(v);}
        static Texture2D Render(Camera camera,string name)
        {
            var rt=new RenderTexture(512,512,24);var t=new Texture2D(512,512,TextureFormat.RGB24,false);var old=RenderTexture.active;
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;t.ReadPixels(new Rect(0,0,512,512),0,0);t.Apply();File.WriteAllBytes(Path.Combine(Folder,name+".png"),t.EncodeToPNG());return t;}
            finally{camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(rt);}
        }
        public static void RunBatch()
        {
            Directory.CreateDirectory(Folder);var result=new Result();
            try{
                var root=new GameObject("Underarm visibility verification");var app=root.AddComponent<FashionFitApp>();typeof(FashionFitApp).GetMethod("Start",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(app,null);
                if(!app.Ready||app.HideCoveredBody)throw new InvalidOperationException("Need ready app with unmasked body");
                app.SetLook(6);for(int i=0;i<90;i++)app.AdvanceSimulation(1f/60);
                var tailored=app.Model.GetComponentInChildren<TailoredPoloStudy>();result.fittedCache=tailored.ContactFitApplied;
                if(!result.fittedCache)throw new InvalidOperationException("Missing fitted cache");
                var upper=tailored.UpperSurface.sharedMesh;string expected=TailoredPoloStudy.GeometryKey(upper),rawKey=tailored.RawUpperKey;
                var cloth=app.Model.GetComponentInChildren<MeshFilter>().sharedMesh;string clothKey=TailoredPoloStudy.GeometryKey(cloth);
                var cameraPosition=app.Player.View.transform.position;var cameraRotation=app.Player.View.transform.rotation;var fabric=tailored.UpperSurface.sharedMaterial;
                // Simulate a stale/corrupted runtime surface, then exercise the
                // same refresh entry point used by the asset-import callback.
                var stale=upper.vertices;stale[stale.Length/2]+=Vector3.right*.02f;upper.vertices=stale;
                PoloFitCacheRefresh.ReloadActive();
                result.cacheRefreshPreservedScene=TailoredPoloStudy.GeometryKey(upper)==expected&&tailored.RawUpperKey==rawKey&&TailoredPoloStudy.GeometryKey(cloth)==clothKey&&app.Player.View.transform.position==cameraPosition&&app.Player.View.transform.rotation==cameraRotation&&tailored.UpperSurface.sharedMaterial==fabric&&app.Look==6;
                if(!result.cacheRefreshPreservedScene)throw new InvalidOperationException("Fit refresh failed or reset the camera/material/cloth state");
                foreach(Transform t in app.Model.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
                var camera=app.Player.View;camera.enabled=false;camera.cullingMask=1<<30;camera.rect=new Rect(0,0,1,1);camera.orthographic=true;camera.orthographicSize=.105f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.allowMSAA=false;
                var renderers=app.Model.GetComponentsInChildren<Renderer>();var saved=new Material[renderers.Length][];for(int i=0;i<saved.Length;i++)saved[i]=renderers[i].sharedMaterials;
                var mask=new Material(Shader.Find("Hidden/FashionFit/UnderarmVisibility"));var black=new Material(Shader.Find("Unlit/Color")){color=Color.black};
                if(!mask.shader.isSupported)throw new InvalidOperationException("Visibility shader unsupported");
                var rows=new List<ViewResult>();var visiblePoints=new List<VisiblePoint>();
                foreach(int side in new[]{-1,1})foreach(int view in new[]{0,1,2}){
                    string name=(side<0?"Left":"Right")+(view==0?"Front":view==1?"Side":"Back");
                    camera.transform.position=new Vector3(side*.75f,1.34f,view==0?.65f:view==1?.025f:-.65f);camera.transform.LookAt(new Vector3(side*.19f,1.32f,.025f));
                    for(int i=0;i<saved.Length;i++)renderers[i].sharedMaterials=saved[i];
                    UnityEngine.Object.DestroyImmediate(Render(camera,name+"_Fabric"));
                    for(int i=0;i<saved.Length;i++){var materials=new Material[saved[i].Length];for(int k=0;k<materials.Length;k++)materials[k]=renderers[i]==app.Model.Renderer?mask:black;renderers[i].sharedMaterials=materials;}
                    if(view==0){
                        foreach(var renderer in renderers)if(renderer!=app.Model.Renderer)renderer.enabled=false;
                        var control=Render(camera,name+"_BodyOnlyControl");int controlPixels=CountRed(control);UnityEngine.Object.DestroyImmediate(control);
                        if(controlPixels<100)throw new InvalidOperationException("Visibility control failed to reveal covered body pixels: "+name);
                        result.bodyOnlyControlPixels+=controlPixels;
                        foreach(var renderer in renderers)renderer.enabled=true;
                    }
                    var image=Render(camera,name+"_Visibility");int red=CountRed(image);
                    int pointCount=visiblePoints.Count;ReadWorldPoints(camera,mask,name,visiblePoints);
                    if(visiblePoints.Count-pointCount!=red)throw new InvalidOperationException("Float-position readback disagrees with visible body mask");
                    UnityEngine.Object.DestroyImmediate(image);rows.Add(new ViewResult{view=name,exposedPixels=red});result.exposedPixels+=red;
                }
                for(int i=0;i<saved.Length;i++)renderers[i].sharedMaterials=saved[i];
                Export(app,tailored,"Standing");app.StartSlowPresentation();
                for(int i=0;i<540;i++){app.AdvanceSimulation(1f/60);result.slowSteps++;result.proxyViolations+=app.Wardrobe.ProxyPenetrations;if(i==180||i==420)Export(app,tailored,i==180?"WalkingA":"WalkingB");}
                if(!app.Playing||result.proxyViolations>0)throw new InvalidOperationException("Slow presentation regression");
                result.views=rows.ToArray();result.status=result.exposedPixels==0?"passed":"failed";File.WriteAllText(Path.Combine(Folder,"visibility.json"),JsonUtility.ToJson(result,true));
                File.WriteAllText(Path.Combine(Folder,"visible_body_points.json"),JsonUtility.ToJson(new VisiblePoints{samples=visiblePoints.ToArray()},true));
                for(int i=0;i<saved.Length;i++)renderers[i].sharedMaterials=saved[i];UnityEngine.Object.DestroyImmediate(mask);UnityEngine.Object.DestroyImmediate(black);UnityEngine.Object.DestroyImmediate(root);
                Debug.Log("UNDERARM_VISIBILITY "+JsonUtility.ToJson(result));EditorApplication.Exit(result.exposedPixels==0?0:1);
            }catch(Exception e){File.WriteAllText(Path.Combine(Folder,"failure.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
#endif
