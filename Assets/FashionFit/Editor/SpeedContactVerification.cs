#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FashionFit.Editor
{
    public static class SpeedContactVerification
    {
        [Serializable] sealed class Row{public int look,steps,proxyViolations,exports;public float requestedSpeed,reachedSpeed,clockTravel,maxStretch;public double meanClothMs,maxClothMs;public bool completedCycles,stoppedCleanly;}
        [Serializable] sealed class Result{public string status="exported_for_independent_mesh_check",stage;public float frameDt;public bool fullBody=true;public Row[] looks=new Row[6];}
        static string folder;static Result result;
        static float Arg(string name,float fallback){string[] args=Environment.GetCommandLineArgs();for(int i=0;i<args.Length-1;i++)if(args[i]==name)return float.Parse(args[i+1],System.Globalization.CultureInfo.InvariantCulture);return fallback;}
        public static void RunStandingBatch()=>Run(true);
        public static void RunMovingBatch()=>Run(false);
        public static void RunControlsBatch()
        {
            string path=Path.GetFullPath("Verification/SpeedContact/controls.json");Directory.CreateDirectory(Path.GetDirectoryName(path));
            try{
                var root=new GameObject("Speed menu controls verification");var app=root.AddComponent<FashionFitApp>();typeof(FashionFitApp).GetMethod("Start",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(app,null);
                Check(app.Ready&&!app.Playing&&app.FittingPose&&Mathf.Abs(app.Speed-FashionFitApp.DefaultMotionSpeed)<.001f,"Startup must stand at the selected default speed");
                app.Player.PreviewVRMenu();var menu=app.Player.VRMenu;menu.SelectPage(0);menu.ActionButtons[5].Activate();Check(menu.Page==4,"Missing model-speed page");
                menu.ActionButtons[2].Activate();for(int i=0;i<240;i++)app.AdvanceSimulation(1f/60);Check(app.Playing&&Mathf.Abs(app.Speed-FashionFitApp.FasterMotionSpeed)<.001f&&Mathf.Abs(app.CurrentMotionSpeed-FashionFitApp.FasterMotionSpeed)<.002f,"Faster button did not reach .75");
                menu.ActionButtons[3].Activate();for(int i=0;i<180;i++)app.AdvanceSimulation(1f/60);Check(app.Playing&&Mathf.Abs(app.Speed-FashionFitApp.MaximumMotionSpeed)<.001f&&Mathf.Abs(app.CurrentMotionSpeed-FashionFitApp.MaximumMotionSpeed)<.002f,"Fast button did not reach 1.00");
                menu.ActionButtons[4].Activate();for(int i=0;i<600&&app.Stopping;i++)app.AdvanceSimulation(1f/60);Check(!app.Playing&&!app.Stopping,"Stop button did not finish step");
                app.StartPresentationAt(10);Check(Mathf.Abs(app.Speed-FashionFitApp.MaximumMotionSpeed)<.001f,"Upper clamp failed");app.StartPresentationAt(-10);Check(Mathf.Abs(app.Speed-FashionFitApp.MinimumMotionSpeed)<.001f,"Lower clamp failed");
                menu.ActionButtons[0].Activate();Check(!app.Playing&&app.FittingPose,"Standing button failed");
                File.WriteAllText(path,"{\"status\":\"passed\",\"startsStanding\":true,\"defaultSpeed\":0.50,\"slowOption\":0.18,\"fasterButton\":0.75,\"fastButton\":1.00,\"maximumClamp\":1.00,\"stopCompletes\":true,\"vrMenuButtons\":10,\"headsetTested\":false}");Debug.Log("SPEED_CONTROLS_OK");UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(0);
            }catch(Exception e){File.WriteAllText(path,"{\"status\":\"failed\"}");Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        static void Run(bool standing)
        {
            float speed=standing?0:Arg("-fashionSpeed",.35f);float frameDt=Arg("-fashionFrameDt",1f/60);
            string stage=standing?"Standing":"Speed"+Mathf.RoundToInt(speed*100).ToString("000")+"_"+Mathf.RoundToInt(1/frameDt)+"fps";
            folder=Path.GetFullPath("Verification/SpeedContact/"+stage);Directory.CreateDirectory(folder);result=new Result{stage=stage,frameDt=frameDt};
            try{
                var root=new GameObject("Stationary then faster contact verification");var app=root.AddComponent<FashionFitApp>();typeof(FashionFitApp).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(app,null);
                Check(app.Ready&&!app.HideCoveredBody,"Need complete visible body");Check(standing||speed<=FashionFitApp.MaximumMotionSpeed,"Requested test speed is clamped by runtime");
                var speedField=typeof(FashionFitApp).GetField("currentMotionSpeed",BindingFlags.Instance|BindingFlags.NonPublic);
                for(int look=1;look<=6;look++){
                    app.SetLook(look);app.Wardrobe.SetAirDemo(false);app.Wardrobe.SetPhysicsMaterial(look==6?2:look==3||look==5?0:1);
                    var row=new Row{look=look,requestedSpeed=speed};result.looks[look-1]=row;
                    if(standing){
                        Export(app,look,"Initial");row.exports++;
                        for(int step=0;step<240;step++)Step(app,row,1f/60);
                        Export(app,look,"Settled");row.exports++;row.completedCycles=true;row.stoppedCleanly=!app.Playing;
                        Capture(app,"Look"+look+"_Standing.png");
                    }else{
                        for(int step=0;step<120;step++)Step(app,row,1f/60);
                        app.Speed=speed;app.TogglePlay();
                        int warmup=0;while((float)speedField.GetValue(app)<speed-.0001f&&warmup++<1200){Step(app,row,frameDt);Check(app.Playing,"Safety hold during acceleration: "+look);}
                        row.reachedSpeed=(float)speedField.GetValue(app);Check(Mathf.Abs(row.reachedSpeed-speed)<.002f,"Did not reach requested speed");
                        float start=app.ShowTime;int next=0,frames=0;
                        while(app.ShowTime-start<2*FashionFitApp.CycleSeconds&&frames++<2400){
                            Step(app,row,frameDt);Check(app.Playing,"Safety hold during faster gait: "+look);
                            if((app.ShowTime-start)/FashionFitApp.CycleSeconds>=next*.25f&&next<8){Export(app,look,"Moving"+next.ToString("00"));row.exports++;if(next==3)Capture(app,"Look"+look+"_Moving.png");next++;}
                        }
                        row.clockTravel=app.ShowTime-start;row.completedCycles=row.clockTravel>=2*FashionFitApp.CycleSeconds&&next==8;Check(row.completedCycles,"Incomplete gait coverage");
                        app.TogglePlay();int stop=0;while((app.Stopping||!app.FittingPose)&&stop++<1500)Step(app,row,frameDt);
                        row.stoppedCleanly=!app.Playing&&!app.Stopping&&app.FittingPose;Check(row.stoppedCleanly,"Stopping did not complete");
                        for(int step=0;step<120;step++)Step(app,row,1f/60);Export(app,look,"AfterStop");row.exports++;
                    }
                    row.meanClothMs/=Math.Max(1,row.steps);Check(row.proxyViolations==0,"Runtime contact violation: "+look);Check(row.maxStretch<1.65f,"Excessive stretch: "+look);
                    File.WriteAllText(Path.Combine(folder,"runtime.json"),JsonUtility.ToJson(result,true));Debug.Log("SPEED_CONTACT_LOOK "+stage+" "+JsonUtility.ToJson(row));
                }
                File.WriteAllText(Path.Combine(folder,"runtime.json"),JsonUtility.ToJson(result,true));Debug.Log("SPEED_CONTACT_EXPORTED "+stage);UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(0);
            }catch(Exception e){result.status="failed";File.WriteAllText(Path.Combine(folder,"runtime.json"),JsonUtility.ToJson(result,true));File.WriteAllText(Path.Combine(folder,"failure.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void Step(FashionFitApp app,Row row,float dt){app.AdvanceSimulation(dt);row.steps++;row.proxyViolations+=app.Wardrobe.ProxyPenetrations;row.meanClothMs+=app.Wardrobe.ClothMilliseconds;row.maxClothMs=Math.Max(row.maxClothMs,app.Wardrobe.ClothMilliseconds);foreach(var c in app.Model.GetComponentsInChildren<FittingCloth>())row.maxStretch=Mathf.Max(row.maxStretch,c.MaxStretch);foreach(var c in app.Model.GetComponentsInChildren<InteractiveShirt>())row.maxStretch=Mathf.Max(row.maxStretch,c.MaxStretch);}
        static void Export(FashionFitApp app,int look,string state)
        {
            var body=new Mesh();Mesh original=app.Model.Renderer.sharedMesh;app.Model.Renderer.sharedMesh=app.Model.BodyMesh;try{app.Model.Renderer.BakeMesh(body);}finally{app.Model.Renderer.sharedMesh=original;}
            var vertices=new List<Vector3>();var faces=new List<int>();
            Action<Mesh,Transform> append=(mesh,tr)=>{int first=vertices.Count;Matrix4x4 m=app.Model.transform.worldToLocalMatrix*tr.localToWorldMatrix;foreach(var p in mesh.vertices)vertices.Add(m.MultiplyPoint3x4(p));foreach(int index in mesh.triangles)faces.Add(first+index);};
            foreach(var filter in app.Model.GetComponentsInChildren<MeshFilter>())if(filter.sharedMesh)append(filter.sharedMesh,filter.transform);
            foreach(var skin in app.Model.GetComponentsInChildren<SkinnedMeshRenderer>())if(skin!=app.Model.Renderer){var baked=new Mesh();skin.BakeMesh(baked);append(baked,skin.transform);UnityEngine.Object.DestroyImmediate(baked);}
            using(var writer=new BinaryWriter(File.Create(Path.Combine(folder,"Look"+look+"_"+state+".bin")))){writer.Write(0x53434631);Write(writer,body.vertices,body.triangles);Write(writer,vertices.ToArray(),faces.ToArray());}UnityEngine.Object.DestroyImmediate(body);
        }
        static void Write(BinaryWriter w,Vector3[] p,int[] t){w.Write(p.Length);foreach(var v in p){w.Write(v.x);w.Write(v.y);w.Write(v.z);}w.Write(t.Length);foreach(int i in t)w.Write(i);}
        static void Capture(FashionFitApp app,string name)
        {
            var c=app.Player.View;c.rect=new Rect(0,0,1,1);c.fieldOfView=38;c.transform.position=new Vector3(.65f,1.1f,2.3f);c.transform.LookAt(new Vector3(0,1,0));
            var rt=new RenderTexture(700,1000,24);var image=new Texture2D(700,1000,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            var skins=app.Model.GetComponentsInChildren<SkinnedMeshRenderer>();var snapshots=new List<GameObject>();var meshes=new List<Mesh>();
            foreach(var skin in skins){var mesh=new Mesh();skin.BakeMesh(mesh);meshes.Add(mesh);var snapshot=new GameObject("Fresh posed render snapshot");snapshot.transform.SetPositionAndRotation(skin.transform.position,skin.transform.rotation);snapshot.transform.localScale=skin.transform.lossyScale;snapshot.AddComponent<MeshFilter>().sharedMesh=mesh;snapshot.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;snapshots.Add(snapshot);}
            try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,700,1000),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,name),image.EncodeToPNG());}finally{foreach(var snapshot in snapshots)UnityEngine.Object.DestroyImmediate(snapshot);foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);foreach(var skin in skins)skin.enabled=true;c.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);}
        }
    }
}
#endif
