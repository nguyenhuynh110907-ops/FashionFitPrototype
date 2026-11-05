#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace FashionFit.Editor
{
    public static class PoloStudyVerification
    {
        [Serializable] sealed class Result {public string status="passed",scope="Six-look closure, authored 2D-reference geometry, sampled Unity poses. Not full CCD/headset certification.";public int closedSkirts,detailVertices,upperVertices,steps,proxyViolations,checks;public float maxStretch;public bool contactFitApplied;}
        [Serializable] sealed class FitSource {public string upper_key,detail_key;public float[] upper,detail,body;public int[] upper_faces,detail_faces,body_faces;}
        static float[] Flat(Vector3[] p){var a=new float[p.Length*3];for(int i=0;i<p.Length;i++){a[i*3]=p[i].x;a[i*3+1]=p[i].y;a[i*3+2]=p[i].z;}return a;}
        static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Verification/PoloStudy"));
        static Result result;
        static void Check(bool ok,string message){result.checks++;if(!ok)throw new InvalidOperationException(message);}
        static void Capture(FashionFitApp app,string name,bool close=false,int side=0)
        {
            foreach(Transform t in app.Model.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
            Camera c=app.Player.View;c.enabled=false;c.rect=new Rect(0,0,1,1);c.cullingMask=1<<30;c.fieldOfView=close?34:38;
            c.transform.position=close?new Vector3(.18f,1.52f,.70f):side==0?new Vector3(.60f,1.17f,2.50f):side==1?new Vector3(2.5f,1.17f,.2f):new Vector3(0,1.17f,-2.5f);
            c.transform.LookAt(close?new Vector3(0,1.33f,.05f):new Vector3(0,1.02f,0));c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.76f,.78f,.79f);
            var rt=new RenderTexture(800,1000,24);var image=new Texture2D(800,1000,TextureFormat.RGB24,false);var old=RenderTexture.active;
            try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,800,1000),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Folder,name+".png"),image.EncodeToPNG());}
            finally{c.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);}
        }
        static void WriteMesh(BinaryWriter w,Vector3[] p,int[] t){w.Write(p.Length);foreach(Vector3 v in p){w.Write(v.x);w.Write(v.y);w.Write(v.z);}w.Write(t.Length);foreach(int v in t)w.Write(v);}
        static void Export(FashionFitApp app,string name)
        {
            var body=new Mesh();var p=new List<Vector3>();var tri=new List<int>();
            Action<Mesh,Transform> append=(mesh,t)=>{int offset=p.Count;Matrix4x4 m=app.Model.transform.worldToLocalMatrix*t.localToWorldMatrix;foreach(Vector3 v in mesh.vertices)p.Add(m.MultiplyPoint3x4(v));foreach(int index in mesh.triangles)tri.Add(offset+index);};
            app.Model.Renderer.BakeMesh(body);
            foreach(MeshFilter f in app.Model.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh)append(f.sharedMesh,f.transform);
            foreach(SkinnedMeshRenderer skin in app.Model.GetComponentsInChildren<SkinnedMeshRenderer>())if(skin!=app.Model.Renderer){var mesh=new Mesh();skin.BakeMesh(mesh);append(mesh,skin.transform);UnityEngine.Object.DestroyImmediate(mesh);}
            using(var w=new BinaryWriter(File.Create(Path.Combine(Folder,name+".bin")))){w.Write(0x53434631);WriteMesh(w,body.vertices,body.triangles);WriteMesh(w,p.ToArray(),tri.ToArray());}
            UnityEngine.Object.DestroyImmediate(body);
        }
        public static void RunBatch()
        {
            GameObject root=null;result=new Result();Directory.CreateDirectory(Folder);
            try{
                foreach(DressPreset preset in Enum.GetValues(typeof(DressPreset))){
                    SampleSkirtGeometry.Build(preset,out Vector3[] p,out Vector2[] uv,out int[] t);Check(t.Length==32*20*6,"Missing centre skirt faces: "+preset);
                    var edges=new Dictionary<long,int>();for(int i=0;i<t.Length;i+=3)for(int side=0;side<3;side++){int a=t[i+side],b=t[i+(side+1)%3];long key=((long)Mathf.Min(a,b)<<32)|(uint)Mathf.Max(a,b);edges.TryGetValue(key,out int n);edges[key]=n+1;}
                    int boundary=0;foreach(int n in edges.Values){Check(n<=2,"Nonmanifold skirt");if(n==1)boundary++;}Check(boundary==64,"Skirt has a boundary beyond waist and hem");result.closedSkirts++;
                }
                root=new GameObject("Polo study verification");var app=root.AddComponent<FashionFitApp>();typeof(FashionFitApp).GetMethod("Start",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(app,null);
                Check(app.Ready,"App not ready: "+app.FatalError);Check(app.Look==6,"Reference study should be selected at startup");Check(!app.HideCoveredBody,"QA must use full body");
                var tailored=app.Model.GetComponentInChildren<TailoredPoloStudy>();Check(tailored!=null,"Missing tailored study");result.detailVertices=tailored.DetailSurface.sharedMesh.vertexCount;result.upperVertices=tailored.UpperSurface.sharedMesh.vertexCount;
                result.contactFitApplied=tailored.ContactFitApplied;
                if(!tailored.ContactFitApplied){var upper=tailored.UpperSurface.sharedMesh;var details=tailored.DetailSurface.sharedMesh;File.WriteAllText(Path.Combine(Folder,"rest_fit_source.json"),JsonUtility.ToJson(new FitSource{upper_key=TailoredPoloStudy.GeometryKey(upper),detail_key=TailoredPoloStudy.GeometryKey(details),upper=Flat(upper.vertices),upper_faces=upper.triangles,detail=Flat(details.vertices),detail_faces=details.triangles,body=Flat(app.Model.BodyMesh.vertices),body_faces=app.Model.BodyMesh.triangles}));}
                Check(tailored.ContactFitApplied,"Contact-fit cache absent or mismatched; export and bake before accepting this study");
                Check(tailored.DetailSurface.sharedMesh.subMeshCount==3&&TailoredPoloStudy.ButtonCount==4,"Missing separate fabric/stitch/hardware geometry");
                Color trialColor=new Color(.18f,.24f,.31f);app.Wardrobe.FabricColor=trialColor;app.Wardrobe.UpdateMaterial();Check(tailored.UpperSurface.sharedMaterial.GetColor("_Color")==trialColor,"Upper-shell material did not follow wardrobe changes");app.Wardrobe.ClearMaps();
                foreach(Vector3 n in tailored.UpperSurface.sharedMesh.normals)Check(!float.IsNaN(n.sqrMagnitude)&&!float.IsInfinity(n.sqrMagnitude)&&Mathf.Abs(n.magnitude-1)<.002f,"Invalid cloth normal");
                foreach(var mesh in new[]{tailored.UpperSurface.sharedMesh,tailored.DetailSurface.sharedMesh}){
                    foreach(Vector3 v in mesh.vertices)Check(!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsNaN(v.z),"Nonfinite garment point");
                    foreach(BoneWeight w in mesh.boneWeights)Check(Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)<.001f,"Unnormalized skin weight");
                }
                foreach(int look in new[]{6,2}){
                    app.SetLook(look);Check(app.Model.HoldUpperBodyForFitting==(look==6),"Upper-body fit hold leaked between outfits");for(int i=0;i<90;i++)app.AdvanceSimulation(1f/60);
                    Capture(app,"Look"+look+"_Standing");Export(app,"Look"+look+"_Standing");
                    if(look==6){Capture(app,"CollarClose",true);Capture(app,"Side",false,1);Capture(app,"Back",false,2);}
                    app.StartSlowPresentation();
                    for(int i=0;i<540;i++){
                        app.AdvanceSimulation(1f/60);result.steps++;result.proxyViolations+=app.Wardrobe.ProxyPenetrations;
                        var cloth=app.Model.GetComponentInChildren<FittingCloth>();result.maxStretch=Mathf.Max(result.maxStretch,cloth.MaxStretch);
                        if(i==180||i==420){string name="Look"+look+(i==180?"_WalkingA":"_WalkingB");Capture(app,name);Export(app,name);}
                    }
                    Check(app.Playing,"Safety hold during slow gait: look "+look);app.TogglePlay();for(int i=0;i<360;i++)app.AdvanceSimulation(1f/60);Check(!app.Playing&&!app.Stopping,"Stop failed");
                }
                Check(result.proxyViolations==0,"Proxy collision violations");Check(result.maxStretch<1.65f,"Excessive stretch");
                File.WriteAllText(Path.Combine(Folder,"verification.json"),JsonUtility.ToJson(result,true));Debug.Log("POLO_STUDY_OK "+JsonUtility.ToJson(result));UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(0);
            }catch(Exception error){result.status="failed";File.WriteAllText(Path.Combine(Folder,"failure.txt"),error.ToString());Debug.LogException(error);if(root)UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(1);}
        }
        public static void ExportRestBatch()
        {
            try {TailoredPoloStudy.ExportUnfittedGeometry=true;Directory.CreateDirectory(Folder);var root=new GameObject("Rest contact fit export");var app=root.AddComponent<FashionFitApp>();typeof(FashionFitApp).GetMethod("Start",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(app,null);
                if(!app.Ready)throw new InvalidOperationException(app.FatalError);var tailored=app.Model.GetComponentInChildren<TailoredPoloStudy>();var upper=tailored.UpperSurface.sharedMesh;var details=tailored.DetailSurface.sharedMesh;
                File.WriteAllText(Path.Combine(Folder,"rest_fit_source.json"),JsonUtility.ToJson(new FitSource{upper_key=TailoredPoloStudy.GeometryKey(upper),detail_key=TailoredPoloStudy.GeometryKey(details),upper=Flat(upper.vertices),upper_faces=upper.triangles,detail=Flat(details.vertices),detail_faces=details.triangles,body=Flat(app.Model.BodyMesh.vertices),body_faces=app.Model.BodyMesh.triangles}));
                UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(0);
            }catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}finally{TailoredPoloStudy.ExportUnfittedGeometry=false;}
        }
        public static void CompareLightingBatch()
        {
            try{Directory.CreateDirectory(Folder);var root=new GameObject("Lighting comparison");var app=root.AddComponent<FashionFitApp>();typeof(FashionFitApp).GetMethod("Start",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(app,null);if(!app.Ready)throw new InvalidOperationException(app.FatalError);
                for(int i=0;i<90;i++)app.AdvanceSimulation(1f/60);Capture(app,"BiasOriginal",true);
                foreach(var light in root.GetComponentsInChildren<Light>()){light.shadowBias=.05f;light.shadowNormalBias=.15f;}
                Capture(app,"BiasAdjusted",true);
                foreach(var light in root.GetComponentsInChildren<Light>())light.shadows=LightShadows.None;
                Capture(app,"ShadowDisabled",true);
                var tailored=app.Model.GetComponentInChildren<TailoredPoloStudy>();var unlit=new Material(Shader.Find("Unlit/Color")){color=Color.white};tailored.UpperSurface.sharedMaterial=unlit;tailored.DetailSurface.enabled=false;Capture(app,"UnlitShell",true);
                var standard=new Material(Shader.Find("Standard")){color=new Color(.025f,.026f,.028f)};standard.SetFloat("_Glossiness",.12f);tailored.UpperSurface.sharedMaterial=standard;Capture(app,"StandardShell",true);
                int bad=0;foreach(var t in tailored.UpperSurface.sharedMesh.tangents)if(float.IsNaN(t.x)||float.IsNaN(t.y)||float.IsNaN(t.z)||float.IsNaN(t.w)||float.IsInfinity(t.x)||float.IsInfinity(t.y)||float.IsInfinity(t.z)||t.x*t.x+t.y*t.y+t.z*t.z<1e-10)bad++;
                File.WriteAllText(Path.Combine(Folder,"tangent_diagnostic.json"),"{\"invalid_or_zero_tangents\":"+bad+"}");UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(unlit);UnityEngine.Object.DestroyImmediate(standard);EditorApplication.Exit(0);
            }catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
        }
    }
}
#endif
