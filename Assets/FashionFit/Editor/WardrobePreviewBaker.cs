#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FashionFit.Editor
{
    public static class WardrobePreviewBaker
    {
        [Serializable] sealed class Report {public string status="passed";public int[] vertices=new int[6],triangles=new int[6];public string method="Static meshes baked from the six fitted runtime garments; 0.66 scale; vertex clustering only on large surfaces";}
        sealed class Cluster {public Vector3 sum;public Vector2 uv;public int count;}
        [MenuItem("Fashion Fit/Rebuild Wardrobe Display Previews")]
        public static void Bake()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before baking wardrobe previews");
            string folder="Assets/Resources/Showroom";Directory.CreateDirectory(folder);AssetDatabase.Refresh();
            var root=new GameObject("Temporary preview baker");var app=root.AddComponent<FashionFitApp>();
            try{
                typeof(FashionFitApp).GetMethod("Start",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(app,null);
                if(!app.Ready)throw new InvalidOperationException(app.FatalError);
                var catalog=new ShowroomPreviewCatalog.Catalog{schema=1,entries=new ShowroomPreviewCatalog.Entry[6]};var report=new Report();
                for(int look=1;look<=6;look++){
                    app.SetLook(look);for(int i=0;i<90;i++)app.AdvanceSimulation(1f/60);
                    var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<List<int>>();var surfaces=new List<ShowroomPreviewCatalog.Surface>();
                    foreach(var filter in app.Model.GetComponentsInChildren<MeshFilter>())if(filter.sharedMesh){var renderer=filter.GetComponent<MeshRenderer>();if(renderer)Append(filter.sharedMesh,app.Model.transform.worldToLocalMatrix*filter.transform.localToWorldMatrix,renderer.sharedMaterials,vertices,uv,triangles,surfaces);}
                    foreach(var renderer in app.Model.GetComponentsInChildren<SkinnedMeshRenderer>())if(renderer!=app.Model.Renderer){var mesh=new Mesh();renderer.BakeMesh(mesh);Append(mesh,app.Model.transform.worldToLocalMatrix*renderer.transform.localToWorldMatrix,renderer.sharedMaterials,vertices,uv,triangles,surfaces);UnityEngine.Object.DestroyImmediate(mesh);}
                    if(vertices.Count<100||triangles.Count==0)throw new InvalidOperationException("Missing wardrobe geometry "+look);
                    var bounds=new Bounds(vertices[0],Vector3.zero);foreach(var p in vertices)bounds.Encapsulate(p);
                    Vector3 origin=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
                    for(int i=0;i<vertices.Count;i++)vertices[i]=(vertices[i]-origin)*.66f;
                    var output=new Mesh{name="Look "+look+" static atelier display"};if(vertices.Count>65000)output.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
                    output.SetVertices(vertices);output.SetUVs(0,uv);output.subMeshCount=triangles.Count;int faces=0;for(int i=0;i<triangles.Count;i++){output.SetTriangles(triangles[i],i);faces+=triangles[i].Count/3;}
                    output.RecalculateNormals();output.RecalculateBounds();output.RecalculateTangents();
                    string path=folder+"/Look"+look.ToString("00")+".asset";var previous=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                    if(previous){EditorUtility.CopySerialized(output,previous);UnityEngine.Object.DestroyImmediate(output);EditorUtility.SetDirty(previous);}else AssetDatabase.CreateAsset(output,path);
                    catalog.entries[look-1]=new ShowroomPreviewCatalog.Entry{look=look,vertices=vertices.Count,surfaces=surfaces.ToArray()};report.vertices[look-1]=vertices.Count;report.triangles[look-1]=faces;
                }
                File.WriteAllText(folder+"/preview-catalog.json",JsonUtility.ToJson(catalog,true));Directory.CreateDirectory("Verification/AtelierPolish");File.WriteAllText("Verification/AtelierPolish/preview_bake.json",JsonUtility.ToJson(report,true));AssetDatabase.SaveAssets();AssetDatabase.Refresh();Debug.Log("WARDROBE_PREVIEWS_OK "+JsonUtility.ToJson(report));
            }finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        public static void RunBatch(){try{Bake();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
        static void Append(Mesh mesh,Matrix4x4 matrix,Material[] materials,List<Vector3> output,List<Vector2> outputUv,List<List<int>> triangles,List<ShowroomPreviewCatalog.Surface> surfaces)
        {
            var p=mesh.vertices;var uv=mesh.uv;bool simplify=p.Length>6500;
            for(int sub=0;sub<mesh.subMeshCount;sub++){
                Material material=materials[Mathf.Min(sub,materials.Length-1)];Color color=material.HasProperty("_Color")?material.GetColor("_Color"):Color.white;
                float smooth=material.HasProperty("_Glossiness")?material.GetFloat("_Glossiness"):.25f,metal=material.HasProperty("_Metallic")?material.GetFloat("_Metallic"):0;
                var surface=new ShowroomPreviewCatalog.Surface{color=new[]{color.r,color.g,color.b},smoothness=smooth,metallic=metal};surfaces.Add(surface);var faces=new List<int>();triangles.Add(faces);
                var map=new Dictionary<Vector3Int,int>();var clusters=new List<Cluster>();var sourceToCluster=new Dictionary<int,int>();var indices=mesh.GetTriangles(sub);
                foreach(int index in indices)if(!sourceToCluster.ContainsKey(index)){
                    Vector3 point=matrix.MultiplyPoint3x4(p[index]);Vector3Int key=simplify?new Vector3Int(Mathf.RoundToInt(point.x/.009f),Mathf.RoundToInt(point.y/.009f),Mathf.RoundToInt(point.z/.009f)):new Vector3Int(index,0,0);
                    if(!map.TryGetValue(key,out int id)){id=clusters.Count;map.Add(key,id);clusters.Add(new Cluster());}
                    clusters[id].sum+=point;clusters[id].uv+=uv.Length==p.Length?uv[index]:Vector2.zero;clusters[id].count++;sourceToCluster.Add(index,id);
                }
                int offset=output.Count;foreach(var c in clusters){output.Add(c.sum/c.count);outputUv.Add(c.uv/c.count);}
                for(int i=0;i<indices.Length;i+=3){int a=sourceToCluster[indices[i]],b=sourceToCluster[indices[i+1]],c=sourceToCluster[indices[i+2]];if(a==b||b==c||a==c)continue;faces.Add(offset+a);faces.Add(offset+b);faces.Add(offset+c);}
            }
        }
    }
}
#endif
