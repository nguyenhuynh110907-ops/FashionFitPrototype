#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FashionFit.Editor
{
    public static class CloseViewVerification
    {
        [Serializable] sealed class Item { public int look,view,pinkPixels,verticesOutsideBounds; public string bounds,expected; }
        [Serializable] sealed class Report { public string label; public List<Item> items=new List<Item>(); }
        static void Initialize(FashionFitApp app) { typeof(FashionFitApp).GetMethod("Start",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(app,null); }
        public static void Before(){Run("Before",false);}
        public static void After(){Run("After",true);}
        static void Run(string label,bool enforce)
        {
            var root=new GameObject("Close view verification");var report=new Report{label=label};
            string output=Path.GetFullPath(Path.Combine(Application.dataPath,"../Verification/CloseView",label));Directory.CreateDirectory(output);
            try {
                foreach(float angle in new[]{-1e-9f,0,1e-9f,-Mathf.PI*2,Mathf.PI*2,Mathf.PI*4}){
                    float radius=BodyFitProfile.Radius(1.46f,angle);
                    if(float.IsNaN(radius)||radius<=0||Mathf.Abs(radius-BodyFitProfile.Radius(1.46f,angle+Mathf.PI*2))>.00001f)throw new InvalidOperationException("Body profile angle-wrap regression");
                }
                var app=root.AddComponent<FashionFitApp>();Initialize(app);
                if(!app.Ready)throw new InvalidOperationException(app.FatalError);
                Camera camera=app.Player.View;camera.enabled=false;camera.rect=new Rect(0,0,1,1);camera.fieldOfView=50;camera.cullingMask=1<<30;
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.82f,.83f,.82f);
                foreach(int look in new[]{2,3,4,5}) {
                    app.SetLook(look);for(int f=0;f<90;f++)app.AdvanceSimulation(1f/60);
                    foreach(Transform tr in app.Model.GetComponentsInChildren<Transform>())tr.gameObject.layer=30;
                    FittingCloth cloth=app.Model.GetComponentInChildren<FittingCloth>();
                    Renderer upper=look==5?(Renderer)app.Model.GetComponentInChildren<InteractiveShirt>().GetComponent<MeshRenderer>():cloth.BodiceSurface;
                    Mesh source=look==5?upper.GetComponent<MeshFilter>().sharedMesh:cloth.BodiceSurface.sharedMesh;
                    Vector3[] points=source.vertices;
                    if(look!=5){var matrices=app.Model.SkinMatrices();var weights=source.boneWeights;
                        for(int i=0;i<points.Length;i++){Vector3 p=points[i];BoneWeight w=weights[i];points[i]=matrices[w.boneIndex0].MultiplyPoint3x4(p)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(p)*w.weight1;}}
                    Matrix4x4 toWorld=look==5?upper.transform.localToWorldMatrix:app.Model.transform.localToWorldMatrix;
                    Bounds expected=new Bounds(toWorld.MultiplyPoint3x4(points[0]),Vector3.zero);int outside=0;
                    foreach(Vector3 p in points){Vector3 world=toWorld.MultiplyPoint3x4(p);expected.Encapsulate(world);if(!upper.bounds.Contains(world))outside++;}
                    for(int view=0;view<4;view++){
                        float angle=(25+view*90)*Mathf.Deg2Rad;
                        camera.transform.position=app.Model.transform.position+new Vector3(Mathf.Sin(angle)*.80f,1.65f,Mathf.Cos(angle)*.80f);
                        camera.transform.LookAt(app.Model.transform.position+Vector3.up*.99f);
                        Capture(camera,Path.Combine(output,"Look"+look+"_View"+view+".png"));
                        Material saved=upper.sharedMaterial;bool bodyEnabled=app.Model.Renderer.enabled,skirtEnabled=cloth.Surface.enabled;
                        var id=new Material(Shader.Find("Unlit/Color")){color=Color.magenta};
                        int pixels;
                        try{upper.sharedMaterial=id;app.Model.Renderer.enabled=false;cloth.Surface.enabled=false;pixels=Capture(camera,Path.Combine(output,"Look"+look+"_View"+view+"_UpperID.png"),true);}
                        finally{upper.sharedMaterial=saved;app.Model.Renderer.enabled=bodyEnabled;cloth.Surface.enabled=skirtEnabled;UnityEngine.Object.DestroyImmediate(id);}
                        report.items.Add(new Item{look=look,view=view,pinkPixels=pixels,verticesOutsideBounds=outside,bounds=upper.bounds.ToString(),expected=expected.ToString()});
                    }
                    if(enforce&&look==5){
                        InteractiveShirt shirt=app.Model.GetComponentInChildren<InteractiveShirt>();
                        Vector3[] initial=source.vertices;Vector3 collar=Vector3.zero;for(int i=0;i<32;i++)collar+=initial[i]/32;
                        for(int fit=0;fit<2;fit++){
                            shirt.SetFit(fit==0?.94f:1.16f,fit==0?.94f:1.12f);
                            for(int f=0;f<30;f++)app.AdvanceSimulation(1f/60);
                            Vector3[] adjusted=source.vertices;Vector3 current=Vector3.zero;for(int i=0;i<32;i++)current+=adjusted[i]/32;
                            if(Vector3.Distance(current,collar)>.002f||shirt.transform.localScale!=Vector3.one)throw new InvalidOperationException("Shirt fit moved the collar off its anchor");
                            camera.transform.position=app.Model.transform.position+new Vector3(.45f,1.65f,.66f);camera.transform.LookAt(app.Model.transform.position+Vector3.up*.99f);
                            Capture(camera,Path.Combine(output,fit==0?"ShirtFit_Small.png":"ShirtFit_Large.png"));
                        }
                        shirt.ResetAdjustment();Debug.Log("SHIRT_FIT_ANCHOR_OK min and max dimensions");
                    }
                }
                File.WriteAllText(Path.Combine(output,"results.json"),JsonUtility.ToJson(report,true));
                if(enforce)foreach(Item item in report.items)if(item.verticesOutsideBounds>0||item.pinkPixels<200)throw new InvalidOperationException("Upper garment culled or unsafe bounds: look "+item.look+" view "+item.view);
                Debug.Log("CLOSE_VIEW_"+label.ToUpperInvariant()+"_OK "+JsonUtility.ToJson(report));
                UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(0);
            }catch(Exception e){Debug.LogException(e);UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(1);}
        }
        static int Capture(Camera camera,string path,bool count=false)
        {
            var rt=new RenderTexture(640,800,24);var texture=new Texture2D(640,800,TextureFormat.RGB24,false);RenderTexture old=RenderTexture.active;
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,640,800),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());int pixels=0;
                if(count)foreach(Color32 color in texture.GetPixels32())if(color.r>180&&color.b>180&&color.g<80)pixels++;return pixels;}
            finally{camera.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(rt);}
        }
    }
}
#endif
