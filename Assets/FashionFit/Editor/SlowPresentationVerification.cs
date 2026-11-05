#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FashionFit.Editor
{
    public static class SlowPresentationVerification
    {
        static bool startupOnly,shirtOnly,closeAfterShirt;
        public static void RunStartupBatch(){startupOnly=true;RunBatch();}
        public static void RunShirtBatch(){shirtOnly=true;RunBatch();}
        public static void RunShirtAndCloseBatch(){shirtOnly=true;closeAfterShirt=true;RunBatch();}
        [Serializable] sealed class LookResult
        {
            public string name;
            public int hiddenBodyTriangles,steps,proxyViolations;
            public float maxStretch,clockTravel,meanClothMilliseconds,maximumClothMilliseconds;
            public bool stopped,returnedOriginalBody,vrBodyRestored,clothSleepingAfterStop;
        }
        [Serializable] sealed class Result
        {
            public string scope="Actual app start, slow gait, settle, stop, coverage and source-body mesh export. Proxy tests are not full triangle CCD or headset certification.";
            public bool startsStanding,defaultSpeedCorrect,warmupHoldsPose,fixedPointsContactSafe,stopDuringWarmupWorks;
            public LookResult[] looks=new LookResult[5];
        }
        static void Check(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        static string Folder {get{return Path.GetFullPath(Path.Combine(Application.dataPath,"../Verification/SlowPresentation"));}}
        static void WriteMesh(BinaryWriter writer,Vector3[] vertices,int[] triangles)
        {
            writer.Write(vertices.Length);foreach(Vector3 p in vertices){writer.Write(p.x);writer.Write(p.y);writer.Write(p.z);}
            writer.Write(triangles.Length);foreach(int i in triangles)writer.Write(i);
        }
        static void Export(FashionFitApp app,int look,string state)
        {
            var bodyMesh=new Mesh();var bodiceMesh=new Mesh();
            try {
                Mesh current=app.Model.Renderer.sharedMesh;
                app.Model.Renderer.sharedMesh=app.Model.BodyMesh;
                try{app.Model.Renderer.BakeMesh(bodyMesh);}finally{app.Model.Renderer.sharedMesh=current;}
                var points=new List<Vector3>();var faces=new List<int>();
                Action<Mesh,Transform> append=(mesh,tr)=>{
                    Matrix4x4 matrix=app.Model.transform.worldToLocalMatrix*tr.localToWorldMatrix;int first=points.Count;
                    foreach(Vector3 p in mesh.vertices)points.Add(matrix.MultiplyPoint3x4(p));
                    foreach(int index in mesh.triangles)faces.Add(first+index);
                };
                FittingCloth cloth=app.Model.GetComponentInChildren<FittingCloth>();
                append(cloth.GetComponent<MeshFilter>().sharedMesh,cloth.transform);
                if(cloth.BodiceSurface!=null){cloth.BodiceSurface.BakeMesh(bodiceMesh);append(bodiceMesh,cloth.BodiceSurface.transform);}
                InteractiveShirt shirt=app.Model.GetComponentInChildren<InteractiveShirt>();
                if(shirt!=null)append(shirt.GetComponent<MeshFilter>().sharedMesh,shirt.transform);
                using(var writer=new BinaryWriter(File.Create(Path.Combine(Folder,"Look"+look+"_"+state+".bin")))){
                    writer.Write(0x53434631);WriteMesh(writer,bodyMesh.vertices,bodyMesh.triangles);WriteMesh(writer,points.ToArray(),faces.ToArray());
                }
            }finally{UnityEngine.Object.DestroyImmediate(bodyMesh);UnityEngine.Object.DestroyImmediate(bodiceMesh);}
        }
        static void Capture(FashionFitApp app,int look,string state,Camera camera)
        {
            foreach(Transform tr in app.Model.GetComponentsInChildren<Transform>())tr.gameObject.layer=30;
            for(int view=0;view<2;view++){
                camera.transform.position=view==0?new Vector3(0,1,3):new Vector3(3,1,0);camera.transform.LookAt(new Vector3(0,.88f,0));
                var target=new RenderTexture(600,900,24);var image=new Texture2D(600,900,TextureFormat.RGB24,false);RenderTexture previous=RenderTexture.active;
                try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,600,900),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Folder,"Look"+look+"_"+state+(view==0?"_Front":"_Side")+".png"),image.EncodeToPNG());}
                finally{camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);UnityEngine.Object.DestroyImmediate(target);}
            }
        }
        public static void RunBatch()
        {
            var root=new GameObject("Temporary slow presentation verification"){hideFlags=HideFlags.HideAndDontSave};var result=new Result();
            try {
                Directory.CreateDirectory(Folder);
                var app=root.AddComponent<FashionFitApp>();
                typeof(FashionFitApp).GetMethod("Start",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(app,null);
                Check(app.Ready&&string.IsNullOrEmpty(app.FatalError),"App initialization failed: "+app.FatalError);
                result.startsStanding=!app.Playing&&app.FittingPose;Check(result.startsStanding,"App must start standing");
                result.defaultSpeedCorrect=Mathf.Abs(app.Speed-FashionFitApp.DefaultMotionSpeed)<1e-6f;Check(result.defaultSpeedCorrect,"Unsafe startup speed");
                app.TogglePlay();app.AdvanceSimulation(1f/60);app.TogglePlay();
                for(int frame=0;frame<5;frame++)app.AdvanceSimulation(1f/60);
                result.stopDuringWarmupWorks=!app.Playing&&!app.Stopping&&app.FittingPose;Check(result.stopDuringWarmupWorks,"Stopping during warmup must not start another step");
                // Pins inside a proxy must be corrected and counted, not ignored.
                var pins=new Vector3[16];for(int r=0;r<2;r++)for(int i=0;i<8;i++)pins[r*8+i]=new Vector3(.02f*Mathf.Cos(i*Mathf.PI/4),.9f+r*.03f,.02f*Mathf.Sin(i*Mathf.PI/4));
                var solver=new FittingClothSolver(pins,8,2,2);var capsule=new[]{new ClothCapsule(new Vector3(0,.8f,0),new Vector3(0,1.1f,0),.1f)};
                solver.Step(1f/60,pins,capsule,new ClothEllipsoid[0]);
                result.fixedPointsContactSafe=solver.LastPenetratingPoints==0&&new Vector2(solver.Positions[0].x,solver.Positions[0].z).magnitude>.11f;Check(result.fixedPointsContactSafe,"Fixed-point collision regression");
                if(startupOnly){Debug.Log("SLOW_STARTUP_OK standing, speed, stop-during-warmup and fixed-point contacts");UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(0);return;}
                var camera=new GameObject("QA camera").AddComponent<Camera>();camera.transform.SetParent(root.transform,false);camera.enabled=false;camera.fieldOfView=38;camera.cullingMask=1<<30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.82f,.83f,.82f);
                for(int look=shirtOnly?5:1;look<=5;look++){
                    app.SetLook(look);var row=new LookResult{name=((DressPreset)(look-1)).ToString()};result.looks[look-1]=row;
                    row.hiddenBodyTriangles=app.Model.HiddenBodyTriangles;Check(row.hiddenBodyTriangles>0,"Missing coverage "+row.name);
                    for(int frame=0;frame<90;frame++)app.AdvanceSimulation(1f/60);
                    Export(app,look,"Standing");Capture(app,look,"Standing",camera);
                    app.TogglePlay();float start=app.ShowTime;
                    for(int frame=0;frame<30;frame++)app.AdvanceSimulation(1f/60);
                    Check(Mathf.Abs(app.ShowTime-start)<1e-7f,"Warmup advanced the gait");result.warmupHoldsPose=true;
                    double totalMs=0;
                    for(int frame=0;frame<540;frame++){
                        app.AdvanceSimulation(1f/60);row.steps++;totalMs+=app.Wardrobe.ClothMilliseconds;
                        row.maximumClothMilliseconds=Mathf.Max(row.maximumClothMilliseconds,app.Wardrobe.ClothMilliseconds);
                        row.proxyViolations+=app.Wardrobe.ProxyPenetrations;
                        foreach(FittingCloth cloth in app.Model.GetComponentsInChildren<FittingCloth>())row.maxStretch=Mathf.Max(row.maxStretch,cloth.MaxStretch);
                        foreach(InteractiveShirt shirt in app.Model.GetComponentsInChildren<InteractiveShirt>())row.maxStretch=Mathf.Max(row.maxStretch,shirt.MaxStretch);
                        if(frame==150){Export(app,look,"WalkingA");Capture(app,look,"WalkingA",camera);}
                        if(frame==390){Export(app,look,"WalkingB");Capture(app,look,"WalkingB",camera);}
                    }
                    row.clockTravel=app.ShowTime-start;row.meanClothMilliseconds=(float)(totalMs/row.steps);
                    Check(row.clockTravel>FashionFitApp.CycleSeconds,"No complete slow gait cycle "+row.name);
                    Check(app.Playing,"Motion safety hold triggered: "+row.name+" "+app.Status);
                    app.TogglePlay();for(int frame=0;frame<360;frame++)app.AdvanceSimulation(1f/60);
                    row.stopped=!app.Playing&&!app.Stopping&&app.FittingPose;row.clothSleepingAfterStop=!app.ClothSettling;Check(row.stopped,"Slow stop did not complete");
                    app.NotifyClothGrab(true);row.vrBodyRestored=app.Model.Renderer.sharedMesh==app.Model.BodyMesh;Check(row.vrBodyRestored,"VR grab must reveal body");app.NotifyClothGrab(false);
                    app.SetLook(0);row.returnedOriginalBody=app.Model.Renderer.sharedMesh==app.Model.BodyMesh;Check(row.returnedOriginalBody,"Removing clothes must restore body");
                    Debug.Log("SLOW_PRESENTATION_LOOK "+JsonUtility.ToJson(row));
                    File.WriteAllText(Path.Combine(Folder,shirtOnly?"shirt_results.json":"results.json"),JsonUtility.ToJson(result,true));
                    Check(row.proxyViolations==0,"Proxy penetration "+row.name);Check(row.maxStretch<1.65f,"Excessive cloth stretch "+row.name);
                }
                Debug.Log("SLOW_PRESENTATION_OK "+JsonUtility.ToJson(result));UnityEngine.Object.DestroyImmediate(root);
                if(closeAfterShirt){CloseViewVerification.After();return;}EditorApplication.Exit(0);
            }catch(Exception e){File.WriteAllText(Path.Combine(Folder,"failure.txt"),e.ToString());Debug.LogException(e);UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(1);}
        }
    }
}
#endif
