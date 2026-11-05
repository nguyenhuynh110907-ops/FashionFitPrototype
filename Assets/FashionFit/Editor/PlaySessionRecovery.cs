#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace FashionFit.Editor
{
    // Restart generated runtime state after an actual script reload in an established Play session.
    // Ordinary Enter Play domain reloads must not trigger another restart.
    [InitializeOnLoad]
    public static class PlaySessionRecovery
    {
        const string Entered="FashionFit.Recovery.Entered",Pending="FashionFit.Recovery.Pending";
        static PlaySessionRecovery(){EditorApplication.playModeStateChanged+=Changed;}
        static void Changed(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.ExitingEditMode)SessionState.SetBool(Entered,false);
            if(state==PlayModeStateChange.EnteredPlayMode)SessionState.SetBool(Entered,true);
            if(state==PlayModeStateChange.ExitingPlayMode)SessionState.SetBool(Entered,false);
            if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(Pending,false))EditorApplication.update+=Restart;
        }
        [DidReloadScripts(100)]
        static void Reloaded()
        {
            if(!EditorApplication.isPlaying||!SessionState.GetBool(Entered,false)||!Object.FindObjectOfType<FashionFitApp>())return;
            SessionState.SetBool(Pending,true);EditorApplication.delayCall+=Stop;
        }
        static void Stop()
        {
            if(!SessionState.GetBool(Pending,false))return;
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=Stop;return;}
            Debug.Log("FASHIONFIT_SCRIPT_RELOAD_RECOVERY • rebuilding generated runtime state; returning to stationary fitting. Re-enter VR manually if needed.");
            if(EditorApplication.isPlaying)EditorApplication.isPlaying=false;else EditorApplication.update+=Restart;
        }
        static void Restart()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            EditorApplication.update-=Restart;if(!SessionState.GetBool(Pending,false))return;
            SessionState.SetBool(Pending,false);FashionFitAutoSetup.PrepareAndRun();
        }
    }
}
#endif
