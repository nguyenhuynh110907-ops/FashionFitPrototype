#if !FASHIONFIT_NUMERICAL
using System;
using System.IO;
using UnityEngine;

namespace FashionFit
{
    [Serializable] public sealed class ClothMaterialProfile
    {
        public string label;
        public int mit_fabric_id;
        public float pullTravelM=.05f;
        public float youngWarpPa,youngWeftPa,thicknessM,compressionRatio,recoverySeconds;
        public float arealMass,bendCompliance,damping,friction,roughness,stretchWarp,stretchWeft,shear,airDrag;
        [Serializable] sealed class Catalog{public int schema_version;public ClothMaterialProfile[] profiles;}
        static ClothMaterialProfile[] profiles;
        public static ClothMaterialProfile[] All
        {
            get{
                if(profiles!=null)return profiles;
                var asset=Resources.Load<TextAsset>("Fitting/cloth-materials");if(!asset)throw new FileNotFoundException("Missing cloth-materials.json");
                var catalog=JsonUtility.FromJson<Catalog>(asset.text);
                if(catalog==null||catalog.schema_version!=1||catalog.profiles==null||catalog.profiles.Length!=3)throw new InvalidDataException("Invalid cloth material catalog");
                foreach(var p in catalog.profiles)if(p.arealMass<=0||p.bendCompliance<=0||p.stretchWarp<=0||p.stretchWeft<=0||p.shear<=0||p.damping<0||p.friction<0||p.airDrag<0)throw new InvalidDataException("Invalid cloth material parameters");
                profiles=catalog.profiles;return profiles;
            }
        }
    }
}
#endif
