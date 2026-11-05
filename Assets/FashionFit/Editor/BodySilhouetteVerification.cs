#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace FashionFit.Editor
{
    public static class BodySilhouetteVerification
    {
        static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Verification/BodySilhouette"));
        static FashionFitApp Start(out GameObject root){Directory.CreateDirectory(Folder);root=new GameObject("Body silhouette verification");var app=root.AddComponent<FashionFitApp>();typeof(FashionFitApp).GetMethod("Start",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(app,null);if(!app.Ready)throw new InvalidOperationException(app.FatalError);return app;}
        static void Capture(FashionFitApp app,string name,int view,bool close=false)
        {
            foreach(Transform t in app.Model.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
            var c=app.Player.View;c.enabled=false;c.cullingMask=1<<30;c.rect=new Rect(0,0,1,1);c.fieldOfView=close?34:40;
            c.transform.position=close?(view==1?new Vector3(.8f,1.30f,.025f):new Vector3(.25f,1.40f,.90f)):(view==1?new Vector3(2.6f,1.08f,.02f):new Vector3(.35f,1.08f,2.6f));
            c.transform.LookAt(close?new Vector3(0,1.27f,.04f):new Vector3(0,.95f,0));c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.74f,.76f,.77f);
            var rt=new RenderTexture(800,1000,24);var tex=new Texture2D(800,1000,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,800,1000),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(Folder,name+".png"),tex.EncodeToPNG());}
            finally{c.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(tex);}
        }
        static void Export(FashionFitApp app,string name)
        {
            var body=new Mesh();app.Model.Renderer.BakeMesh(body);var p=new List<Vector3>();var tri=new List<int>();
            Action<Mesh,Transform> append=(mesh,t)=>{int offset=p.Count;var m=app.Model.transform.worldToLocalMatrix*t.localToWorldMatrix;foreach(var v in mesh.vertices)p.Add(m.MultiplyPoint3x4(v));foreach(int v in mesh.triangles)tri.Add(v+offset);};
            foreach(var f in app.Model.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh)append(f.sharedMesh,f.transform);
            foreach(var s in app.Model.GetComponentsInChildren<SkinnedMeshRenderer>())if(s!=app.Model.Renderer){var mesh=new Mesh();s.BakeMesh(mesh);append(mesh,s.transform);UnityEngine.Object.DestroyImmediate(mesh);}
            using(var w=new BinaryWriter(File.Create(Path.Combine(Folder,name+".bin")))){w.Write(0x53434631);Write(w,body.vertices,body.triangles);Write(w,p.ToArray(),tri.ToArray());}UnityEngine.Object.DestroyImmediate(body);
        }
        static void Write(BinaryWriter w,Vector3[] p,int[] t){w.Write(p.Length);foreach(var v in p){w.Write(v.x);w.Write(v.y);w.Write(v.z);}w.Write(t.Length);foreach(int v in t)w.Write(v);}
        public static void MeasureBodyBatch(){try{GarmentFitStudy.Measure();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
        public static void CaptureBeforeBodyBatch()=>CaptureBodyBatch("Before");
        public static void CaptureAfterBodyBatch()=>CaptureBodyBatch("After");
        static void CaptureBodyBatch(string label)
        {
            try{var app=Start(out GameObject root);app.SetLook(0);Capture(app,label+"_BodyFront",0);Capture(app,label+"_BodySide",1);Capture(app,label+"_BodyClose",0,true);Capture(app,label+"_BodySideClose",1,true);UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(0);}
            catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
        [Serializable] sealed class Result{public string status="passed",scope="Six wardrobes, slow presentation with full body; body-contact QA is separate";public int looks,steps,proxyViolations;public float maxStretch;}
        public static void RunBatch()
        {
            var result=new Result();
            try{var app=Start(out GameObject root);if(app.HideCoveredBody)throw new InvalidOperationException("QA requires unmasked body");app.SetLook(0);Capture(app,"After_BodyClose",0,true);Capture(app,"After_BodySideClose",1,true);
                foreach(int look in new[]{1,2,3,4,5,6}){app.SetLook(look);for(int i=0;i<90;i++)app.AdvanceSimulation(1f/60);result.looks++;
                    if(look==6&&!app.Model.GetComponentInChildren<TailoredPoloStudy>().ContactFitApplied)throw new InvalidOperationException("Rebuild polo cache for the refined body");
                    Capture(app,"Look"+look+"_Standing",0);Capture(app,"Look"+look+"_Side",1);Export(app,"Look"+look+"_Standing");
                    if(look==6){app.Wardrobe.ClearMaps();app.Wardrobe.FabricColor=new Color(.84f,.82f,.78f);app.Wardrobe.Roughness=.86f;app.Wardrobe.UpdateMaterial();Capture(app,"Polo_LightFront",0,true);Capture(app,"Polo_LightSide",1,true);app.Wardrobe.ClearMaps();}
                    app.StartSlowPresentation();for(int i=0;i<540;i++){app.AdvanceSimulation(1f/60);result.steps++;result.proxyViolations+=app.Wardrobe.ProxyPenetrations;result.maxStretch=Mathf.Max(result.maxStretch,app.Model.GetComponentInChildren<FittingCloth>().MaxStretch);if(i==180||i==420)Export(app,"Look"+look+(i==180?"_WalkingA":"_WalkingB"));}
                    if(!app.Playing)throw new InvalidOperationException("Unexpected motion safety hold: "+look);app.TogglePlay();for(int i=0;i<360;i++)app.AdvanceSimulation(1f/60);
                }
                if(result.proxyViolations>0||result.maxStretch>1.65f)throw new InvalidOperationException("Cloth proxy/stretch regression");
                File.WriteAllText(Path.Combine(Folder,"verification.json"),JsonUtility.ToJson(result,true));Debug.Log("BODY_SILHOUETTE_OK "+JsonUtility.ToJson(result));UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(0);
            }catch(Exception e){File.WriteAllText(Path.Combine(Folder,"failure.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
        }
        public static void PreviewPoloBatch()
        {
            try{var app=Start(out GameObject root);app.SetLook(6);for(int i=0;i<90;i++)app.AdvanceSimulation(1f/60);
                Capture(app,"Polo_BlackFront",0,true);Capture(app,"Polo_BlackSide",1,true);app.Wardrobe.ClearMaps();app.Wardrobe.FabricColor=new Color(.84f,.82f,.78f);app.Wardrobe.Roughness=.86f;app.Wardrobe.UpdateMaterial();
                var details=app.Model.GetComponentInChildren<TailoredPoloStudy>().DetailSurface.sharedMaterials;if(details[0].GetColor("_Color").r<.7f||details[1].GetColor("_Color").r<.7f)throw new InvalidOperationException("Neutral fitting preview left dark trim/stitches");
                Capture(app,"Polo_LightFront",0,true);Capture(app,"Polo_LightSide",1,true);UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(0);
            }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
#endif
