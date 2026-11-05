#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FashionFit.Editor
{
    public static class FashionFitRecoveryBuild
    {
        public static void RunBatch()
        {
            try{
                string[] arguments=Environment.GetCommandLineArgs();string output=null;for(int i=0;i<arguments.Length-1;i++)if(arguments[i]=="-fashionBuildPath")output=arguments[i+1];
                if(string.IsNullOrEmpty(output)||!Path.IsPathRooted(output))throw new ArgumentException("Explicit absolute -fashionBuildPath required.");Directory.CreateDirectory(Path.GetDirectoryName(output));FashionFitAutoSetup.ConfigureOpenXR();
                // Manual PhysX simulation in Editor QA is project state. A native
                // Editor exit may skip a managed finally; never export that mode.
                Physics.simulationMode=SimulationMode.FixedUpdate;
                if(Physics.simulationMode!=SimulationMode.FixedUpdate)throw new InvalidOperationException("Player requires automatic FixedUpdate physics");
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/FashionFit/Scenes/FashionFitPrototype.unity"},locationPathName=output,target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
                Directory.CreateDirectory("Verification/ProjectRecovery");File.WriteAllText("Verification/ProjectRecovery/build.json","{\"status\":\""+(report.summary.result==BuildResult.Succeeded?"passed":"failed")+"\",\"bytes\":"+report.summary.totalSize+",\"errors\":"+report.summary.totalErrors+",\"warnings\":"+report.summary.totalWarnings+",\"physicsFixedUpdate\":true}");
                if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Windows build failed: "+report.summary.result);Debug.Log("RECOVERY_WINDOWS_BUILD_OK");EditorApplication.Exit(0);
            }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
        }
    }
}
#endif
