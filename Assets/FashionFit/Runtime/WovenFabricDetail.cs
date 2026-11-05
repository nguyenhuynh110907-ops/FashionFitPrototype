using UnityEngine;

namespace FashionFit
{
    // CC0 ambientCG Fabric061: texture response only, not a trained cloth model.
    // Loaded once; never read files or perform inference inside the frame loop.
    public static class WovenFabricDetail
    {
        static Texture2D normal,roughness;
        static bool loaded;
        public static void Apply(Material material,bool enabled)
        {
            if(!material)return;
            if(enabled&&!loaded){normal=Resources.Load<Texture2D>("Fitting/WovenDetail/normal");roughness=Resources.Load<Texture2D>("Fitting/WovenDetail/roughness");loaded=true;}
            material.SetFloat("_UseClothDetail",enabled&&normal&&roughness?1:0);
            if(enabled){material.SetTexture("_ClothNormal",normal);material.SetTexture("_ClothRoughness",roughness);}
        }
    }
}
