#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FashionFit.Editor
{
    public static class PoseMannequinAssets
    {
        public const string Folder="Assets/FashionFit/Models/PoseMannequins";
        [MenuItem("Fashion Fit/Rebuild Male and Female Pose Prefabs")]
        public static void Rebuild()
        {
            if(EditorApplication.isPlaying)throw new System.InvalidOperationException("Stop Play before rebuilding pose assets.");
            System.IO.Directory.CreateDirectory(Folder);AssetDatabase.Refresh();
            for(int i=0;i<2;i++){
                string gender=i==0?"Female":"Male";var temp=new GameObject("Temporary CC0 mannequin").AddComponent<RunwayAvatar>();temp.LoadTechnical("PoseModels/"+gender.ToLowerInvariant(),gender+" CC0 mesh");
                var export=new GameObject(gender+" CC0 mannequin — generic 17-bone rig");
                try{
                    string meshPath=Folder+"/"+gender+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if(mesh)EditorUtility.CopySerialized(temp.BodyMesh,mesh);else{mesh=Object.Instantiate(temp.BodyMesh);mesh.name=gender+" CC0 mesh";AssetDatabase.CreateAsset(mesh,meshPath);}
                    string materialPath=Folder+"/"+gender+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if(!mat){mat=RunwayStage.Matte(gender+" satin charcoal",new Color(.075f,.083f,.095f),.42f);AssetDatabase.CreateAsset(mat,materialPath);}
                    var map=new Dictionary<Transform,Transform>();foreach(var bone in temp.Bones){var tr=new GameObject(bone.name).transform;tr.SetParent(bone.parent==temp.transform?export.transform:map[bone.parent],false);tr.localPosition=bone.localPosition;tr.localRotation=bone.localRotation;tr.localScale=bone.localScale;map[bone]=tr;}
                    var renderer=export.AddComponent<SkinnedMeshRenderer>();renderer.sharedMesh=mesh;renderer.sharedMaterial=mat;renderer.bones=System.Array.ConvertAll(temp.Bones,b=>map[b]);renderer.rootBone=map[temp.RootBone];renderer.quality=SkinQuality.Bone4;
                    SkinnedBounds.SetFromModel(renderer,export.transform,new Bounds(new Vector3(0,1,0),new Vector3(2.6f,2.9f,2.4f)),0);
                    PrefabUtility.SaveAsPrefabAsset(export,Folder+"/"+gender+"CC0.prefab");
                }finally{Object.DestroyImmediate(export);Object.DestroyImmediate(temp.gameObject);}
            }
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        }
    }
}
#endif
