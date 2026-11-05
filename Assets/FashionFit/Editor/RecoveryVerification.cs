#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FashionFit.Editor
{
    [InitializeOnLoad]
    public static class RecoveryVerification
    {
        const string Key="FashionFit.RecoveryTest.";
        static int checks;
        static RecoveryVerification(){if(SessionState.GetInt(Key+"Stage",0)>0){EditorApplication.update+=Tick;Application.logMessageReceived+=Log;}}
        static void Check(bool ok,string text){checks++;if(!ok)throw new InvalidOperationException(text);}
        static void Set(object owner,string field,object value)=>owner.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(owner,value);
        static object Get(object owner,string field)=>owner.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(owner);
        public static void RunGuardsBatch()
        {
            Directory.CreateDirectory("Verification/ProjectRecovery");GameObject root=null;
            try{
                root=new GameObject("Reload guards test");var app=root.AddComponent<FashionFitApp>();typeof(FashionFitApp).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(app,null);Check(app.Ready,"Startup readiness broken");
                var cloth=app.Model.GetComponentInChildren<FittingCloth>();object solver=Get(cloth,"solver");Set(cloth,"solver",null);Check(!cloth.SimulationReady&&!app.Ready,"Lost solver remains ready");for(int i=0;i<120;i++){app.AdvanceSimulation(1f/60);cloth.Step(1f/60);}cloth.BeginVRGrab(Vector3.zero);cloth.MoveVRGrab(Vector3.zero);cloth.EndVRGrab();Check(!app.Ready,"Lost solver guard skipped");Set(cloth,"solver",solver);Check(app.Ready,"Restored solver not ready");
                Set(app,"runtimeInitialized",false);Check(!app.Ready,"Reload generation marker restored incorrectly");typeof(RunwayPlayer).GetMethod("Update",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(app.Player,null);app.AdvanceSimulation(1f/60);Set(app,"runtimeInitialized",true);Check(app.Ready,"Generation readiness not restored");
                app.SetLook(5);var shirt=app.Model.GetComponentInChildren<InteractiveShirt>();Check(shirt&&shirt.SimulationReady,"Shirt not ready");object topSolver=Get(shirt,"solver");Set(shirt,"solver",null);Check(!app.Ready,"Lost shirt solver remains ready");shirt.Step(1f/60);shirt.BeginVRGrab(Vector3.zero,Quaternion.identity);shirt.MoveVRGrab(Vector3.zero,Quaternion.identity);shirt.EndVRGrab();Set(shirt,"solver",topSolver);Check(app.Ready,"Shirt failed to recover");
                for(int look=1;look<=6;look++){app.SetLook(look);app.StandForFitting();for(int i=0;i<90;i++)app.AdvanceSimulation(1f/60);Check(app.Ready&&app.Wardrobe.HasGarment&&app.Wardrobe.ProxyPenetrations==0,"Normal cloth flow regressed: "+look);}
                File.WriteAllText("Verification/ProjectRecovery/guards.json","{\"status\":\"passed\",\"checks\":"+checks+",\"lost_solver_guard\":true,\"lost_generation_guard\":true,\"shirt_guard\":true,\"normal_looks\":6}");Debug.Log("RECOVERY_GUARDS_OK "+checks);UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(0);
            }catch(Exception e){Debug.LogException(e);if(root)UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(1);}
        }
        public static void RunReloadBatch()
        {
            SessionState.SetInt(Key+"Stage",1);SessionState.SetInt(Key+"Errors",0);SessionState.SetFloat(Key+"Deadline",(float)EditorApplication.timeSinceStartup+150);EditorApplication.update-=Tick;EditorApplication.update+=Tick;Application.logMessageReceived+=Log;FashionFitAutoSetup.PrepareAndRun();
        }
        static void Log(string condition,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error)SessionState.SetInt(Key+"Errors",SessionState.GetInt(Key+"Errors",0)+1);}
        static void Tick()
        {
            try{
                int stage=SessionState.GetInt(Key+"Stage",0);if(stage==0){EditorApplication.update-=Tick;return;}
                if(EditorApplication.timeSinceStartup>SessionState.GetFloat(Key+"Deadline",0))throw new TimeoutException("Script reload recovery did not rebuild Play state.");
                var app=UnityEngine.Object.FindObjectOfType<FashionFitApp>();
                if(stage==1&&EditorApplication.isPlaying&&app&&app.Ready){SessionState.SetInt(Key+"OriginalID",app.Model.GetComponentInChildren<FittingCloth>().GetInstanceID());SessionState.SetInt(Key+"Stage",2);Debug.Log("RECOVERY_TEST_REQUESTING_SCRIPT_RELOAD");EditorUtility.RequestScriptReload();return;}
                // Scene components can retain their instance ID across Enter Play. The
                // generated cloth must be newly constructed, so compare that instance.
                if(stage==2&&EditorApplication.isPlaying&&app&&app.Ready&&app.Model.GetComponentInChildren<FittingCloth>().GetInstanceID()!=SessionState.GetInt(Key+"OriginalID",0)){
                    Check(app.FittingPose&&!app.Playing&&app.Wardrobe.SimulationReady,"Reload did not restore stationary fitting");for(int i=0;i<120;i++)app.AdvanceSimulation(1f/60);Check(SessionState.GetInt(Key+"Errors",0)==0,"Exception during script reload recovery");SessionState.SetInt(Key+"Stage",3);EditorApplication.isPlaying=false;return;
                }
                if(stage==3&&!EditorApplication.isPlayingOrWillChangePlaymode){Directory.CreateDirectory("Verification/ProjectRecovery");File.WriteAllText("Verification/ProjectRecovery/reload.json","{\"status\":\"passed\",\"actual_script_reload\":true,\"rebuilt_play_session\":true,\"returned_to_standing\":true,\"exceptions\":0}");SessionState.SetInt(Key+"Stage",0);Debug.Log("RECOVERY_RELOAD_OK");EditorApplication.Exit(0);}
            }catch(Exception e){SessionState.SetInt(Key+"Stage",0);Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
#endif
