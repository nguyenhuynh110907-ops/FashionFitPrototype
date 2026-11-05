#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
namespace FashionFit.Editor
{
    public static class BimanualSpeedControls
    {
        public static void RunBatch(){
            string folder=Path.GetFullPath("Verification/Bimanual");Directory.CreateDirectory(folder);int checks=0;
            Action<bool,string> check=(b,s)=>{checks++;if(!b)throw new InvalidOperationException(s);};GameObject root=null;
            try{
                root=new GameObject("Faster motion controls QA");var app=root.AddComponent<FashionFitApp>();typeof(FashionFitApp).GetMethod("Start",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(app,null);check(app.Ready&&!app.Playing&&app.FittingPose&&app.Speed==.5f,"Default must start standing, selected .50");app.Player.PreviewVRMenu();var panel=app.Player.VRMenu;
                panel.SelectPage(0);panel.ActionButtons[1].Activate();for(int i=0;i<240;i++)app.AdvanceSimulation(1f/60);check(app.Playing&&Mathf.Abs(app.CurrentMotionSpeed-.5f)<.002f,"Default .50 not reached");
                panel.SelectPage(4);panel.ActionButtons[2].Activate();for(int i=0;i<100;i++)app.AdvanceSimulation(1f/60);check(app.Playing&&Mathf.Abs(app.CurrentMotionSpeed-.75f)<.002f,".75 not reached");
                panel.ActionButtons[3].Activate();for(int i=0;i<100;i++)app.AdvanceSimulation(1f/60);check(app.Playing&&Mathf.Abs(app.CurrentMotionSpeed-1)<.002f,"1.00 not reached");
                panel.ActionButtons[4].Activate();for(int i=0;i<600&&app.Stopping;i++)app.AdvanceSimulation(1f/60);check(!app.Playing&&!app.Stopping&&app.FittingPose,"Stop failed");
                panel.ActionButtons[1].Activate();check(app.Speed==.18f,"Slow .18 removed");app.StartPresentationAt(100);check(app.Speed==1,"Max speed clamp failed");app.StartPresentationAt(-1);check(app.Speed==.05f,"Min clamp failed");app.StandForFitting();
                File.WriteAllText(folder+"/speed-controls.json","{\"status\":\"passed\",\"checks\":"+checks+",\"startsStanding\":true,\"slow\":0.18,\"default\":0.50,\"faster\":0.75,\"maximum\":1.00,\"hardwareTested\":false}");Debug.Log("BIMANUAL_SPEED_CONTROLS_OK");UnityEngine.Object.DestroyImmediate(root);root=null;EditorApplication.Exit(0);
            }catch(Exception e){File.WriteAllText(folder+"/speed-controls.json","{\"status\":\"failed\",\"checks\":"+checks+"}");Debug.LogException(e);EditorApplication.Exit(1);}finally{if(root)UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
#endif
