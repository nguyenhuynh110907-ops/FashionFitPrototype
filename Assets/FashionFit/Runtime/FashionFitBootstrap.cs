using UnityEngine;

namespace FashionFit
{
    public static class FashionFitBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartPrototype()
        {
            if (Object.FindObjectOfType<FashionFitApp>() != null) return;
            GameObject root = new GameObject("FashionFitPrototype");
            root.AddComponent<FashionFitApp>();
        }
    }
}
