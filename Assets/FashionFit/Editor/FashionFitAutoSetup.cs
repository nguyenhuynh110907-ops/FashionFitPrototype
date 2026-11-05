#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;
namespace FashionFit.Editor
{
    [InitializeOnLoad]
    public static class FashionFitAutoSetup
    {
        const string ScenePath="Assets/FashionFit/Scenes/FashionFitPrototype.unity",Started="FashionFit.v052.Started",Pending="FashionFit.v052.Build";
        static double readyAfter;
        static FashionFitAutoSetup()
        {
            readyAfter=EditorApplication.timeSinceStartup+2;
            EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.EnteredPlayMode){SessionState.SetBool(Started,true);FocusGameView();}};
            if(!Application.isBatchMode)EditorApplication.update+=Tick;
        }
        static void Tick()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){readyAfter=EditorApplication.timeSinceStartup+2;return;}
            if(EditorApplication.timeSinceStartup<readyAfter)return;
            EditorApplication.update-=Tick;
            string pending=SessionState.GetString(Pending,"");if(pending!=""){SessionState.EraseString(Pending);BuildNow(pending=="Android"?BuildTarget.Android:BuildTarget.StandaloneWindows64);return;}
            if(!SessionState.GetBool(Started,false))PrepareAndRun();
        }
        [MenuItem("Fashion Fit/Open and Run Prototype")]
        public static void PrepareAndRun()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            try{
                try{ConfigureOpenXR();}catch(Exception xrError){Debug.LogWarning("Desktop remains available; OpenXR configuration: "+xrError.Message);}
                if(SceneManager.GetActiveScene().path!=ScenePath){if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;EditorSceneManager.OpenScene(ScenePath);}
                EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
                Physics.simulationMode=SimulationMode.FixedUpdate;FocusGameView();EditorApplication.isPlaying=true;
            }catch(Exception e){Debug.LogException(e);}
        }
        static void FocusGameView()
        {
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if(type!=null)EditorWindow.FocusWindowIfItsOpen(type);
        }
        [MenuItem("Fashion Fit/Configure OpenXR")]
        public static void ConfigureOpenXR()
        {
            if(EditorApplication.isPlaying)return;
            Directory.CreateDirectory("Assets/XR");AssetDatabase.Refresh();
            if(!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,out XRGeneralSettingsPerBuildTarget perTarget)||perTarget==null){
                const string path="Assets/XR/RunwayXR.asset";perTarget=AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(path);
                if(perTarget==null){perTarget=ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();AssetDatabase.CreateAsset(perTarget,path);}
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey,perTarget,true);
            }
            ConfigureGroup(perTarget,BuildTargetGroup.Standalone);
            if(BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))ConfigureGroup(perTarget,BuildTargetGroup.Android);
            PlayerSettings.companyName="Atelier Prototype";PlayerSettings.productName="Fashion Fitting Room";PlayerSettings.defaultScreenWidth=1440;PlayerSettings.defaultScreenHeight=900;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;
            PlayerSettings.colorSpace=ColorSpace.Linear;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
            EditorUtility.SetDirty(perTarget);AssetDatabase.SaveAssets();
        }
        static void ConfigureGroup(XRGeneralSettingsPerBuildTarget perTarget,BuildTargetGroup group)
        {
            if(!perTarget.HasSettingsForBuildTarget(group))perTarget.CreateDefaultSettingsForBuildTarget(group);
            if(!perTarget.HasManagerSettingsForBuildTarget(group))perTarget.CreateDefaultManagerSettingsForBuildTarget(group);
            var settings=perTarget.SettingsForBuildTarget(group);settings.InitManagerOnStart=false;settings.Manager.automaticLoading=false;settings.Manager.automaticRunning=false;
            if(!XRPackageMetadataStore.AssignLoader(settings.Manager,"UnityEngine.XR.OpenXR.OpenXRLoader",group))throw new InvalidOperationException("Không cấu hình được OpenXR loader.");
            FeatureHelpers.RefreshFeatures(group);
            var xr=OpenXRSettings.GetSettingsForBuildTargetGroup(group);
            var touch=xr.GetFeature<OculusTouchControllerProfile>();if(touch!=null){touch.enabled=true;EditorUtility.SetDirty(touch);}
            if(group==BuildTargetGroup.Android){var quest=xr.GetFeature<MetaQuestFeature>();if(quest!=null){quest.enabled=true;EditorUtility.SetDirty(quest);}}
            xr.renderMode=OpenXRSettings.RenderMode.SinglePassInstanced;
            EditorUtility.SetDirty(xr);EditorUtility.SetDirty(settings);EditorUtility.SetDirty(settings.Manager);
        }
        [MenuItem("Fashion Fit/Build Windows Application")]
        public static void BuildWindowsApplication()=>RequestBuild(BuildTarget.StandaloneWindows64);
        [MenuItem("Fashion Fit/Build Meta Quest APK")]
        public static void BuildQuest()=>RequestBuild(BuildTarget.Android);
        static void RequestBuild(BuildTarget target)
        {
            if(EditorApplication.isPlaying){EditorUtility.DisplayDialog("Atelier","Dừng Play trước khi xuất bản chạy.","OK");return;}
            var group=BuildPipeline.GetBuildTargetGroup(target);
            if(!BuildPipeline.IsBuildTargetSupported(group,target)){EditorUtility.DisplayDialog("Atelier","Cài "+(target==BuildTarget.Android?"Android Build Support + SDK/NDK + OpenJDK":"Windows Build Support")+" cho Unity 2022.3 trong Unity Hub.","OK");return;}
            SessionState.SetBool(Started,true);ConfigureOpenXR();
            if(EditorUserBuildSettings.activeBuildTarget!=target){SessionState.SetString(Pending,target==BuildTarget.Android?"Android":"Windows");if(!EditorUserBuildSettings.SwitchActiveBuildTarget(group,target)){SessionState.EraseString(Pending);throw new InvalidOperationException("Không chuyển được build target.");}readyAfter=EditorApplication.timeSinceStartup+3;EditorApplication.update-=Tick;EditorApplication.update+=Tick;return;}
            BuildNow(target);
        }
        static void BuildNow(BuildTarget target)
        {
            ConfigureOpenXR();
            if(target==BuildTarget.Android){PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel29;PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevelAuto;PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.atelier.fittingroom");PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});}
            string path=target==BuildTarget.Android?"Builds/Quest/FashionFittingRoom.apk":"Builds/Windows/FashionFittingRoom.exe";Directory.CreateDirectory(Path.GetDirectoryName(path));
            Physics.simulationMode=SimulationMode.FixedUpdate;
            BuildReport report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=path,target=target,options=BuildOptions.None});
            if(report.summary.result==BuildResult.Succeeded)EditorUtility.RevealInFinder(path);else Debug.LogError("Build failed: "+report.summary.result);
        }
        [MenuItem("Fashion Fit/Reveal Device Log")]
        static void RevealLog()=>EditorUtility.RevealInFinder(Application.consoleLogPath);
    }
}
#endif
