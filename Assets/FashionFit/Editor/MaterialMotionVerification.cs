#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FashionFit.Editor
{
    public static class MaterialMotionVerification
    {
        [Serializable] sealed class Bench{public string material;public float massKg,windDeflectionM,maxStretch,finalSpeed;}
        [Serializable] sealed class Result{public string status="passed";public int slowSteps,materialStressSteps,proxyViolations;public float maxStretch,secondaryMaxM,broadPhaseMaxDifference;public double averageClothMs,maxClothMs;public double[] clothMsPerLook;public Bench[] bench;}
        static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Verification/MaterialMotion"));
        static void Require(bool ok,string message){if(!ok)throw new InvalidOperationException(message);}
        static Bench Benchmark(ClothMaterialProfile profile,float dt=1f/60,int iterations=7)
        {
            const int segments=32,rings=15;var p=new Vector3[segments*rings];
            for(int r=0;r<rings;r++)for(int s=0;s<segments;s++){float a=s*Mathf.PI*2/segments;p[r*segments+s]=new Vector3(.25f*Mathf.Cos(a),1.3f-r*.05f,.25f*Mathf.Sin(a));}
            var solver=new FittingClothSolver(p,segments,rings,2,false,.012f,iterations,.45f,.07f);solver.SetMaterial(profile);
            var caps=new ClothCapsule[0];var ellips=new ClothEllipsoid[0];solver.AirVelocity=new Vector3(0,0,.32f);
            int steps=Mathf.RoundToInt(4/dt);for(int i=0;i<steps;i++)solver.Step(dt,p,caps,ellips);
            float displacement=0;int count=0;
            for(int i=segments*2;i<p.Length;i++){Vector3 v=solver.Positions[i];Require(!float.IsNaN(v.sqrMagnitude)&&!float.IsInfinity(v.sqrMagnitude),"Nonfinite material benchmark");displacement+=(v-p[i]).sqrMagnitude;count++;}
            for(int i=0;i<segments*2;i++)Require(Vector3.Distance(solver.Positions[i],p[i])<1e-5f,"Pinned seam moved");
            var b=new Bench{material=profile.label,massKg=solver.TotalMass,windDeflectionM=Mathf.Sqrt(displacement/count),maxStretch=solver.MaxStretch()};
            solver.AirVelocity=Vector3.zero;for(int i=0;i<steps;i++)solver.Step(dt,p,caps,ellips);b.finalSpeed=solver.MaxSpeed;return b;
        }
        static float BroadPhaseComparison()
        {
            const int segments=24,rings=12;var p=new Vector3[segments*rings];
            for(int r=0;r<rings;r++)for(int s=0;s<segments;s++){float angle=s*Mathf.PI*2/segments;p[r*segments+s]=new Vector3(.26f*Mathf.Cos(angle),1.25f-.06f*r,.26f*Mathf.Sin(angle));}
            var fast=new FittingClothSolver(p,segments,rings);var reference=new FittingClothSolver(p,segments,rings){UseBroadPhase=false};
            fast.SetMaterial(ClothMaterialProfile.All[1]);reference.SetMaterial(ClothMaterialProfile.All[1]);
            var capsules=new[]{new ClothCapsule(new Vector3(0,.55f,0),new Vector3(0,1.1f,0),.22f)};
            var ellipsoids=new[]{new ClothEllipsoid(new Vector3(0,.9f,0),new Vector3(.20f,.12f,.20f))};float maximum=0;
            for(int i=0;i<180;i++){
                float t=i/60f;var old=capsules[0];float x=.04f*Mathf.Sin(t*2);capsules[0]=new ClothCapsule(new Vector3(x,.55f,0),new Vector3(x,1.1f,0),old.A,old.B,.22f);
                var e=ellipsoids[0];ellipsoids[0]=new ClothEllipsoid(new Vector3(0,.9f,.08f*Mathf.Sin(t)),e.Centre,e.Radius);
                fast.AirVelocity=reference.AirVelocity=new Vector3(0,0,.3f);
                fast.Step(1f/60,p,capsules,ellipsoids);reference.Step(1f/60,p,capsules,ellipsoids);
                for(int k=0;k<p.Length;k++)maximum=Mathf.Max(maximum,Vector3.Distance(fast.Positions[k],reference.Positions[k]));
            }
            Require(maximum<1e-5f,"Collision broad phase changed the result");return maximum;
        }
        static void Export(FashionFitApp app,string name)
        {
            var body=new Mesh();app.Model.Renderer.BakeMesh(body);var p=new List<Vector3>();var indices=new List<int>();
            Action<Mesh,Transform> append=(mesh,t)=>{int offset=p.Count;Matrix4x4 matrix=app.Model.transform.worldToLocalMatrix*t.localToWorldMatrix;foreach(var v in mesh.vertices)p.Add(matrix.MultiplyPoint3x4(v));foreach(int i in mesh.triangles)indices.Add(i+offset);};
            foreach(var f in app.Model.GetComponentsInChildren<MeshFilter>())if(f.sharedMesh)append(f.sharedMesh,f.transform);
            foreach(var skin in app.Model.GetComponentsInChildren<SkinnedMeshRenderer>())if(skin!=app.Model.Renderer){var m=new Mesh();skin.BakeMesh(m);append(m,skin.transform);UnityEngine.Object.DestroyImmediate(m);}
            using(var w=new BinaryWriter(File.Create(Path.Combine(Folder,name+".bin")))){w.Write(0x53434631);Write(w,body.vertices,body.triangles);Write(w,p.ToArray(),indices.ToArray());}
            UnityEngine.Object.DestroyImmediate(body);
        }
        static void Write(BinaryWriter w,Vector3[] p,int[] t){w.Write(p.Length);foreach(var v in p){w.Write(v.x);w.Write(v.y);w.Write(v.z);}w.Write(t.Length);foreach(int i in t)w.Write(i);}
        static void Capture(FashionFitApp app,string name)
        {
            foreach(Transform t in app.Model.GetComponentsInChildren<Transform>())t.gameObject.layer=30;
            var c=app.Player.View;c.enabled=false;c.cullingMask=1<<30;c.rect=new Rect(0,0,1,1);c.fieldOfView=38;c.transform.position=new Vector3(.65f,1.1f,2.3f);c.transform.LookAt(new Vector3(0,1.0f,0));c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.65f,.68f,.70f);
            var rt=new RenderTexture(800,1000,24);var image=new Texture2D(800,1000,TextureFormat.RGB24,false);var old=RenderTexture.active;
            try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,800,1000),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Folder,name+".png"),image.EncodeToPNG());}
            finally{c.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);}
        }
        public static void RunBatch()
        {
            Directory.CreateDirectory(Folder);var result=new Result();
            try{
                var bench=new List<Bench>();foreach(var profile in ClothMaterialProfile.All)bench.Add(Benchmark(profile));result.bench=bench.ToArray();
                result.broadPhaseMaxDifference=BroadPhaseComparison();result.clothMsPerLook=new double[6];
                Require(result.bench[0].massKg<result.bench[1].massKg&&result.bench[1].massKg<result.bench[2].massKg,"Measured mass ordering failed");
                Require(Mathf.Abs(result.bench[0].windDeflectionM-result.bench[2].windDeflectionM)>.002f,"Material responses are indistinguishable");
                var root=new GameObject("Material motion verification");var app=root.AddComponent<FashionFitApp>();typeof(FashionFitApp).GetMethod("Start",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(app,null);Require(app.Ready&&!app.HideCoveredBody,"Need ready app and full body");
                double totalMs=0;int measured=0;
                foreach(int look in new[]{1,2,3,4,5,6}){
                    double lookMs=0;int lookSamples=0;
                    app.SetLook(look);int material=look==6?2:look==5||look==3?0:1;app.Wardrobe.SetPhysicsMaterial(material);app.Wardrobe.SetAirDemo(false);
                    for(int i=0;i<120;i++)app.AdvanceSimulation(1f/60);
                    Export(app,"Look"+look+"_Standing");Capture(app,"Look"+look+"_Standing");app.StartSlowPresentation();
                    for(int i=0;i<540;i++){
                        app.AdvanceSimulation(1f/60);result.slowSteps++;result.proxyViolations+=app.Wardrobe.ProxyPenetrations;
                        var cloth=app.Model.GetComponentInChildren<FittingCloth>();result.maxStretch=Mathf.Max(result.maxStretch,cloth.MaxStretch);result.secondaryMaxM=Mathf.Max(result.secondaryMaxM,app.Wardrobe.SecondaryOffset);
                        Require(cloth.MaxStretch<1.65f,"Cloth over-stretched: "+look);
                        if(i==180||i==420){Export(app,"Look"+look+(i==180?"_WalkingA":"_WalkingB"));Capture(app,"Look"+look+(i==180?"_WalkingA":"_WalkingB"));}
                        if(i>60){totalMs+=app.Wardrobe.ClothMilliseconds;lookMs+=app.Wardrobe.ClothMilliseconds;lookSamples++;measured++;result.maxClothMs=Math.Max(result.maxClothMs,app.Wardrobe.ClothMilliseconds);}
                    }
                    Require(app.Playing,"Motion stopped for contact: "+look);app.TogglePlay();for(int i=0;i<300;i++)app.AdvanceSimulation(1f/60);
                    for(int m=0;m<3;m++){
                        app.Wardrobe.SetPhysicsMaterial(m);app.Wardrobe.SetAirDemo(true);
                        for(int i=0;i<180;i++){app.AdvanceSimulation(1f/60);result.materialStressSteps++;result.proxyViolations+=app.Wardrobe.ProxyPenetrations;result.maxStretch=Mathf.Max(result.maxStretch,app.Model.GetComponentInChildren<FittingCloth>().MaxStretch);result.secondaryMaxM=Mathf.Max(result.secondaryMaxM,app.Wardrobe.SecondaryOffset);}
                        Require(app.Model.GetComponentInChildren<FittingCloth>().MaxStretch<1.65f,"Wind material over-stretched");
                        if(look==5||look==6)Capture(app,"Look"+look+"_Material"+m);
                    }
                    app.Wardrobe.SetAirDemo(false);
                    result.clothMsPerLook[look-1]=lookMs/Math.Max(1,lookSamples);
                }
                Require(result.proxyViolations==0,"Proxy contact regression");Require(result.secondaryMaxM>1e-5f&&result.secondaryMaxM<=.0061f,"Secondary surface inactive or unbounded");
                result.averageClothMs=totalMs/Math.Max(1,measured);File.WriteAllText(Path.Combine(Folder,"verification.json"),JsonUtility.ToJson(result,true));Debug.Log("MATERIAL_MOTION_OK "+JsonUtility.ToJson(result));UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(0);
            }catch(Exception e){result.status="failed";File.WriteAllText(Path.Combine(Folder,"verification.json"),JsonUtility.ToJson(result,true));File.WriteAllText(Path.Combine(Folder,"failure.txt"),e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
#endif
