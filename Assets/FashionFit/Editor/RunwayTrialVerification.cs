#if UNITY_EDITOR
using System;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;

namespace FashionFit.Editor
{
    public static class RunwayTrialVerification
    {
        [Serializable] sealed class Result
        {
            public string scope="Unity engine: actual rig IK, five render meshes, blouse, moving rig. No headset or full triangle contact certification.";
            public int checks,looks,physicsSteps,proxyViolations;
            public float maxStretch,maxAnkleHeightError;
            public double milliseconds;
        }
        static void Check(bool ok,string message,Result result){if(!ok)throw new InvalidOperationException(message);result.checks++;}

        [MenuItem("Fashion Fit/Verify Runway Trial")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before verification.");
            var result=new Result();var watch=Stopwatch.StartNew();
            var root=new GameObject("Runway verification - temporary"){hideFlags=HideFlags.HideAndDontSave};
            try
            {
                RunwayAvatar avatar=root.AddComponent<RunwayAvatar>();avatar.Load(null,null,"");
                avatar.Animate(0,0,1);float leftY=avatar.Joint("LeftFoot").y,rightY=avatar.Joint("RightFoot").y;
                for(int look=0;look<5;look++)
                {
                    root.transform.position=Vector3.zero;root.transform.rotation=Quaternion.identity;
                    avatar.Animate(0,0,1);
                    var garment=new GameObject("Verification garment");garment.transform.SetParent(root.transform,false);
                    var cloth=garment.AddComponent<FittingCloth>();cloth.Initialize(avatar,(DressPreset)look);
                    InteractiveShirt shirt=null;
                    if(look==4){var top=new GameObject("Verification blouse");top.transform.SetParent(garment.transform,false);shirt=top.AddComponent<InteractiveShirt>();shirt.Initialize(null,avatar,avatar.Original);}
                    Mesh mesh=garment.GetComponent<MeshFilter>().sharedMesh;
                    Check(mesh.vertexCount>SampleSkirtGeometry.Segments*SampleSkirtGeometry.Rings,"Display cage was not subdivided",result);
                    float lookStretch=1;int lookViolations=0;double physicsMs=0,displayMs=0;var lookWatch=Stopwatch.StartNew();
                    for(int frame=1;frame<=90;frame++)
                    {
                        float time=frame/60f;
                        avatar.Animate(time,Mathf.Clamp01(frame/15f),1);
                        // Test world-space inertia path as well as articulated legs.
                        root.transform.position=new Vector3(Mathf.Sin(time)*.08f,0,0);
                        root.transform.rotation=Quaternion.Euler(0,Mathf.Sin(time)*12,0);
                        cloth.Step(1f/60f);if(shirt!=null)shirt.Step(1f/60f);result.physicsSteps++;
                        physicsMs+=cloth.PhysicsMilliseconds;displayMs+=cloth.DisplayMilliseconds;
                        int violations=cloth.ProxyPenetrations+(shirt!=null?shirt.ProxyPenetrations:0);
                        result.proxyViolations+=violations;lookViolations+=violations;
                        lookStretch=Mathf.Max(lookStretch,cloth.MaxStretch);
                        if(shirt!=null)lookStretch=Mathf.Max(lookStretch,shirt.MaxStretch);
                        result.maxStretch=Mathf.Max(result.maxStretch,cloth.MaxStretch);
                        if(shirt!=null)result.maxStretch=Mathf.Max(result.maxStretch,shirt.MaxStretch);
                        float phase=time/FashionFitApp.CycleSeconds;
                        RunwayGait.Sample(phase,out float offset,out float lift,out float roll);
                        if(lift<.0001f)result.maxAnkleHeightError=Mathf.Max(result.maxAnkleHeightError,Mathf.Abs(avatar.Joint("LeftFoot").y-leftY));
                        RunwayGait.Sample(phase+.5f,out offset,out lift,out roll);
                        if(lift<.0001f)result.maxAnkleHeightError=Mathf.Max(result.maxAnkleHeightError,Mathf.Abs(avatar.Joint("RightFoot").y-rightY));
                        if(frame%15==0)foreach(Vector3 v in mesh.vertices)Check(!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.x)&&!float.IsInfinity(v.y)&&!float.IsInfinity(v.z),"Nonfinite display mesh",result);
                    }
                    lookWatch.Stop();UnityEngine.Debug.Log("RUNWAY_LOOK "+(look+1)+" stretch="+lookStretch+" violations="+lookViolations+" ms="+lookWatch.Elapsed.TotalMilliseconds+" physicsMs/step="+physicsMs/90+" displayMs/step="+displayMs/90);
                    if(shirt!=null)UnityEngine.Debug.Log("SHIRT_EDGE "+shirt.StretchDiagnostic);
                    result.looks++;UnityEngine.Object.DestroyImmediate(garment);
                }
                watch.Stop();result.milliseconds=watch.Elapsed.TotalMilliseconds;
                string output=Path.Combine(Application.dataPath,"../Verification/Unity_Runway_Trial.json");
                File.WriteAllText(output,JsonUtility.ToJson(result,true));
                Check(result.maxAnkleHeightError<.015f,"Stance ankle floor error exceeds 15 mm",result);
                Check(result.maxStretch<1.65f,"Actual rig cloth stretch regression",result);
                Check(result.proxyViolations==0,"Actual rig proxy contact violation (see JSON)",result);
                UnityEngine.Debug.Log("UNITY_RUNWAY_TRIAL_OK "+JsonUtility.ToJson(result));
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }

        public static void RunBatch()
        {
            try{Run();EditorApplication.Exit(0);}
            catch(Exception e){UnityEngine.Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
#endif
