#if UNITY_EDITOR
using UnityEditor;

namespace FashionFit.Editor
{
    public sealed class WovenDetailImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/Resources/Fitting/WovenDetail/"))return;
            var importer=(TextureImporter)assetImporter;
            // Shader decodes OpenGL RGB explicitly. Do not import as Unity's
            // packed normal format, and never gamma-correct normal/roughness.
            importer.textureType=TextureImporterType.Default;
            importer.sRGBTexture=false;importer.mipmapEnabled=true;
            importer.wrapMode=UnityEngine.TextureWrapMode.Repeat;
            importer.filterMode=UnityEngine.FilterMode.Trilinear;
            importer.anisoLevel=4;importer.maxTextureSize=1024;
            importer.textureCompression=TextureImporterCompression.CompressedHQ;
        }
    }
}
#endif
