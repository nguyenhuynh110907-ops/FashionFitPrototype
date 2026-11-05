using System;
using System.Collections.Generic;
using UnityEngine;

namespace FashionFit
{
    public static class ShowroomPreviewCatalog
    {
        [Serializable] public sealed class Surface {public float[] color;public float smoothness,metallic;}
        [Serializable] public sealed class Entry {public int look,vertices;public Surface[] surfaces;}
        [Serializable] public sealed class Catalog {public int schema;public Entry[] entries;}
        static Catalog catalog;
        public static bool Create(int look,Transform parent,List<Material> owned,out Bounds bounds)
        {
            bounds=new Bounds();
            if(catalog==null){var data=Resources.Load<TextAsset>("Showroom/preview-catalog");if(!data)return false;catalog=JsonUtility.FromJson<Catalog>(data.text);}
            if(catalog==null||catalog.schema!=1||catalog.entries==null||catalog.entries.Length!=6)return false;
            var entry=catalog.entries[look-1];var mesh=Resources.Load<Mesh>("Showroom/Look"+look.ToString("00"));if(!mesh||entry.look!=look||mesh.subMeshCount!=entry.surfaces.Length)return false;
            var materials=new Material[entry.surfaces.Length];
            for(int i=0;i<materials.Length;i++){
                var s=entry.surfaces[i];var color=new Color(s.color[0],s.color[1],s.color[2]);
                if(s.metallic<.1f){
                    materials[i]=new Material(Resources.Load<Shader>("Runway/DressFabric")){name="Woven showroom preview "+look+" surface "+i};
                    materials[i].SetColor("_Color",color);materials[i].SetFloat("_Glossiness",s.smoothness);WovenFabricDetail.Apply(materials[i],true);
                }else materials[i]=RunwayStage.Matte("Showroom hardware "+look+" surface "+i,color,s.smoothness,s.metallic);
                owned.Add(materials[i]);
            }
            var go=new GameObject("Baked garment preview "+look);go.transform.SetParent(parent,false);go.layer=11;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=materials;bounds=mesh.bounds;return true;
        }
    }
}
