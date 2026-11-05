#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FashionFit.Editor
{
    [InitializeOnLoad]
    public static class QuestLinkTrialLauncher
    {
        const string Pending="FashionFit.QuestLinkTrial.Pending";
        static double expires;
        static QuestLinkTrialLauncher(){if(!Application.isBatchMode&&SessionState.GetBool(Pending,false))Queue();}
        [MenuItem("Fashion Fit/Prepare Quest Link Trial")]
        public static void OpenAndRun()
        {
            SessionState.SetBool(Pending,true);Queue();FashionFitAutoSetup.PrepareAndRun();
        }
        static void Queue(){expires=EditorApplication.timeSinceStartup+90;EditorApplication.update-=Finish;EditorApplication.update+=Finish;}
        static void Finish()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            if(EditorApplication.timeSinceStartup>expires){EditorApplication.update-=Finish;SessionState.EraseBool(Pending);Debug.LogWarning("Quest Link trial preparation timed out; use Fashion Fit > Prepare Quest Link Trial again.");return;}
            if(!EditorApplication.isPlaying)return;
            var app=Object.FindObjectOfType<FashionFitApp>();if(!app||!app.Ready)return;
            EditorApplication.update-=Finish;SessionState.EraseBool(Pending);app.Player.PrepareForVRTrial();app.GetComponent<RunwayUI>().OpenVRPage();Debug.Log("QUEST_LINK_TRIAL_PREPARED • waiting for headset Link; XR session has not been started");
        }
    }
}
#endif
