#if UNITY_EDITOR
using UnityEditor;
namespace FashionFit.Editor
{
    public static class StudioBuild
    {
        public static void RunBatch()
        {
            PlayerSettings.enableFrameTimingStats = true;
            FashionFitRecoveryBuild.RunBatch();
        }
    }
}
#endif
