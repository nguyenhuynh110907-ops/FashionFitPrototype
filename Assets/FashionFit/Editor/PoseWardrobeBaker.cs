#if UNITY_EDITOR
using System;using System.IO;using System.Collections.Generic;using UnityEngine;using UnityEditor;
namespace FashionFit.Editor {
 public static class PoseWardrobeBaker {
  static string folder="Assets/Resources/PoseWardrobe";
  public static void RunBatch(){GameObject root=null;try{
   Directory.CreateDirectory(folder);AssetDatabase.Refresh();root=new GameObject("Pose wardrobe authoring");var app=root.AddComponent<FashionFitApp>();typeof(FashionFitApp).GetMethod("Start",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(app,null);if(!app.Ready)throw new Exception(app.FatalError);
   var source=app.PoseStudio.Rigs[0].Avatar;int[] faces=source.BodyMesh.triangles;var from=new GarmentSurfaceTransfer.Surface(source.BodyVertices,faces);
   var targets=new RunwayAvatar[2];var contacts=new GarmentSurfaceTransfer.Surface[2];
   for(int g=0;g<2;g++){var target=app.PoseStudio.Rigs[g].Avatar;targets[g]=target;int[] targetFaces=target.BodyMesh.triangles;if(targetFaces.Length!=faces.Length||target.BodyVertices.Length!=source.BodyVertices.Length)throw new Exception("Pose body correspondence missing");for(int i=0;i<faces.Length;i++)if(faces[i]!=targetFaces[i])throw new Exception("Pose body topology differs");contacts[g]=new GarmentSurfaceTransfer.Surface(target.BodyVertices,targetFaces);}

   for(int look=1;look<=6;look++){
    app.SetLook(look);for(int k=0;k<80;k++)app.AdvanceSimulation(1f/60);
    var input=new List<Mesh>();var materials=new List<Material[]>();var transforms=new List<Matrix4x4>();
    foreach(var f in app.Model.GetComponentsInChildren<MeshFilter>()){var r=f.GetComponent<MeshRenderer>();if(r&&f.sharedMesh){input.Add(f.sharedMesh);materials.Add(r.sharedMaterials);transforms.Add(app.Model.transform.worldToLocalMatrix*f.transform.localToWorldMatrix);}}
    var owned=new List<Mesh>();foreach(var r in app.Model.GetComponentsInChildren<SkinnedMeshRenderer>())if(r!=app.Model.Renderer){var b=new Mesh();r.BakeMesh(b);input.Add(b);owned.Add(b);materials.Add(r.sharedMaterials);transforms.Add(app.Model.transform.worldToLocalMatrix*r.transform.localToWorldMatrix);}
    for(int g=0;g<2;g++){
     var target=app.PoseStudio.Rigs[g].Avatar;var positions=new List<Vector3>();var uvs=new List<Vector2>();var skin=new List<BoneWeight>();var triangles=new List<int[]>();var mats=new List<Material>();
     for(int part=0;part<input.Count;part++){
      var m=input[part];Vector3[] p=m.vertices;Vector2[] uv=m.uv;int offset=positions.Count;
      for(int i=0;i<p.Length;i++){
       Vector3 original=transforms[part].MultiplyPoint3x4(p[i]);var binding=from.Nearest(original);int tri=binding.triangle*3;
       Vector3 a0=source.BodyVertices[faces[tri]],a1=source.BodyVertices[faces[tri+1]],a2=source.BodyVertices[faces[tri+2]],b0=target.BodyVertices[faces[tri]],b1=target.BodyVertices[faces[tri+1]],b2=target.BodyVertices[faces[tri+2]];
       Vector3 normalA=Vector3.Cross(a1-a0,a2-a0).normalized,normalB=Vector3.Cross(b1-b0,b2-b0).normalized;
       Vector3 uA=(a1-a0).normalized,vA=Vector3.Cross(normalA,uA),uB=(b1-b0).normalized,vB=Vector3.Cross(normalB,uB),delta=original-binding.point;
       // Transfer the smooth corresponding body displacement, rather than rotating
       // every offset by an unrelated tiny triangle frame (which creates spikes).
       float ratio=target.BodyMesh.bounds.size.y/source.BodyMesh.bounds.size.y;
       Vector3 bodyB=b0*binding.barycentric.x+b1*binding.barycentric.y+b2*binding.barycentric.z;
       Vector3 mapped=original*ratio+(bodyB-binding.point*ratio);
       mapped=contacts[g].ProjectOutside(mapped,.018f);positions.Add(mapped);uvs.Add(uv.Length==p.Length?uv[i]:Vector2.zero);var weight=SkinWeights(target.BodyMesh.boneWeights,faces,tri,binding.barycentric);
       // A skirt hangs from the pelvis; its hem is not sewn to the ankle/foot.
       float hanging=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(target.BodyMesh.bounds.max.y*.48f,target.BodyMesh.bounds.max.y*.61f,mapped.y));
       if(hanging>.001f){int hips=target.BoneIndex("Hips");weight=MixHip(weight,hips,hanging*.98f);}skin.Add(weight);
      }
      for(int sub=0;sub<m.subMeshCount;sub++){int[] t=m.GetTriangles(sub);for(int i=0;i<t.Length;i++)t[i]+=offset;triangles.Add(t);var mat=UnityEngine.Object.Instantiate(materials[part][Mathf.Min(sub,materials[part].Length-1)]);string matPath=folder+"/"+(g==0?"Female":"Male")+look.ToString("00")+"_Material"+mats.Count+".mat";Save(mat,matPath);mats.Add(AssetDatabase.LoadAssetAtPath<Material>(matPath));}
     }
     string stem=folder+"/"+(g==0?"Female":"Male")+look.ToString("00");var mesh=new Mesh{name="Full fitted pose garment "+look,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.SetVertices(positions);mesh.SetUVs(0,uvs);mesh.boneWeights=skin.ToArray();mesh.bindposes=target.GarmentBindposes;mesh.subMeshCount=triangles.Count;for(int i=0;i<triangles.Count;i++)mesh.SetTriangles(triangles[i],i);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateTangents();Save(mesh,stem+"_Mesh.asset");
     var asset=ScriptableObject.CreateInstance<PoseOutfitAsset>();asset.look=look;asset.label=RunwayShowroom.LookNames[look-1]+" / pose "+(g==0?"nữ":"nam");asset.mesh=AssetDatabase.LoadAssetAtPath<Mesh>(stem+"_Mesh.asset");asset.materials=mats.ToArray();Save(asset,stem+".asset");Debug.Log("POSE_BAKE "+stem+" vertices="+positions.Count);
    }
    foreach(var m in owned)UnityEngine.Object.DestroyImmediate(m);
   }
   AssetDatabase.SaveAssets();Directory.CreateDirectory("Verification/PoseMaterial");File.WriteAllText("Verification/PoseMaterial/bake.json","{\"status\":\"passed\",\"outfits\":12,\"method\":\"Full runtime garments, matched CC0 body correspondence, original UV and submaterials\"}");UnityEngine.Object.DestroyImmediate(root);root=null;EditorApplication.Exit(0);
  }catch(Exception e){Debug.LogException(e);if(root)UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(1);}}
  static BoneWeight MixHip(BoneWeight w,int hips,float amount){var entries=new Dictionary<int,float>();int[] ids={w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};float[] vals={w.weight0,w.weight1,w.weight2,w.weight3};for(int i=0;i<4;i++)if(vals[i]>0)entries[ids[i]]=(entries.TryGetValue(ids[i],out float v)?v:0)+vals[i]*(1-amount);entries[hips]=(entries.TryGetValue(hips,out float old)?old:0)+amount;var list=new List<KeyValuePair<int,float>>(entries);list.Sort((a,b)=>b.Value.CompareTo(a.Value));float total=0;for(int i=0;i<4&&i<list.Count;i++)total+=list[i].Value;var result=new BoneWeight();for(int i=0;i<4&&i<list.Count;i++){int id=list[i].Key;float value=list[i].Value/total;if(i==0){result.boneIndex0=id;result.weight0=value;}if(i==1){result.boneIndex1=id;result.weight1=value;}if(i==2){result.boneIndex2=id;result.weight2=value;}if(i==3){result.boneIndex3=id;result.weight3=value;}}return result;}
  static BoneWeight SkinWeights(BoneWeight[] weights,int[] triangles,int tri,Vector3 bary){var sum=new Dictionary<int,float>();for(int j=0;j<3;j++){var w=weights[triangles[tri+j]];int[] ids={w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};float[] values={w.weight0,w.weight1,w.weight2,w.weight3};for(int k=0;k<4;k++)if(values[k]>0)sum[ids[k]]=(sum.TryGetValue(ids[k],out float old)?old:0)+values[k]*bary[j];}var sorted=new List<KeyValuePair<int,float>>(sum);sorted.Sort((a,b)=>b.Value.CompareTo(a.Value));int[] index=new int[4];float[] value=new float[4];float total=0;for(int i=0;i<4&&i<sorted.Count;i++){index[i]=sorted[i].Key;value[i]=sorted[i].Value;total+=value[i];}if(total<=0)throw new Exception("Zero skin weight");return new BoneWeight{boneIndex0=index[0],boneIndex1=index[1],boneIndex2=index[2],boneIndex3=index[3],weight0=value[0]/total,weight1=value[1]/total,weight2=value[2]/total,weight3=value[3]/total};}
  static void Save(UnityEngine.Object obj,string path){var previous=AssetDatabase.LoadMainAssetAtPath(path);if(previous){EditorUtility.CopySerialized(obj,previous);UnityEngine.Object.DestroyImmediate(obj);EditorUtility.SetDirty(previous);}else AssetDatabase.CreateAsset(obj,path);}
 }
}
#endif
