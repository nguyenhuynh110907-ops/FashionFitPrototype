#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace FashionFit.Editor
{
    // An imported TextAsset does not rebuild an existing runtime mesh. Refresh
    // the fitted surfaces once after importing, preserving camera/materials,
    // cloth state and the current mannequin pose.
    public sealed class PoloFitCacheRefresh : AssetPostprocessor
    {
        const string Cache="Assets/Resources/Fitting/polo-contact-fit.json";
        static bool queued;
        static void OnPostprocessAllAssets(string[] imported,string[] deleted,string[] moved,string[] movedFrom)
        {
            if(Array.IndexOf(imported,Cache)<0||queued)return;
            queued=true;EditorApplication.delayCall+=ApplyPending;
        }
        static void ApplyPending()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=ApplyPending;return;}
            queued=false;
            if(EditorApplication.isPlaying)ReloadActive();
        }
        [MenuItem("Fashion Fit/Refresh Active Garment Fit")]
        public static void ReloadActive()
        {
            foreach(var study in UnityEngine.Object.FindObjectsOfType<TailoredPoloStudy>()){
                if(study.RefreshContactFit())Debug.Log("POLO_FIT_REFRESHED: fitted garment updated without resetting the camera or cloth simulation",study);
                else Debug.LogWarning("Polo fit cache does not match the active garment template; stop and start Play to rebuild the garment.",study);
            }
        }
    }
}
#endif
