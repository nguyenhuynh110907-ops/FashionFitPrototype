#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FashionFit.Editor
{
    public static class GarmentFormatVerification
    {
        [Serializable] sealed class Row {public string format,importer;public int renderers,vertices,triangles,materialSlots,uv1Vertices;public bool sourceUnchanged,hasTexture,shadersSupported,transferPassed;public float width,height;public Vector3 importedCentre,explicitFrameOffset;}
        [Serializable] sealed class Report {public string status="passed",scope="Tiny authored fixtures through native FBX/OBJ import and Unity glTFast GLB; not a certification of arbitrary student assets";public Row[] cases;}
        public static void RunBatch()
        {
            string folder="Verification/GarmentTransfer";Directory.CreateDirectory(folder);var rows=new List<Row>();GameObject target=null;
            try{
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);target=new GameObject("Import target");var avatar=target.AddComponent<RunwayAvatar>();avatar.LoadTechnical("Fitting/female","Target body");var neutral=GarmentTransferStudio.Snapshot(avatar.Renderer);var engine=new GarmentSurfaceTransfer(neutral.vertices,neutral.triangles,neutral.vertices,neutral.triangles,avatar.BodyMesh.boneWeights);
                foreach(string extension in new[]{"fbx","obj","glb"}){
                    string path="Assets/FashionFit/TransferSamples/DetailFixture."+extension;byte[] original=File.ReadAllBytes(path);if(extension!="glb")GarmentImportPreparation.Prepare(path);else AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!prefab)throw new InvalidDataException("No imported prefab: "+path);var root=(GameObject)PrefabUtility.InstantiatePrefab(prefab);var row=new Row{format=extension,importer=AssetImporter.GetAtPath(path).GetType().FullName,shadersSupported=true};var inputs=new List<GarmentTransferStudio.Input>();Bounds bounds=new Bounds();bool first=true;
                    try{
                        foreach(var renderer in root.GetComponentsInChildren<Renderer>()){
                            var mesh=GarmentTransferStudio.Snapshot(renderer);inputs.Add(new GarmentTransferStudio.Input{renderer=renderer,mesh=mesh,matrix=renderer.transform.localToWorldMatrix});row.renderers++;row.vertices+=mesh.vertexCount;row.triangles+=mesh.triangles.Length/3;row.materialSlots+=renderer.sharedMaterials.Length;row.uv1Vertices+=mesh.uv2.Length;
                            if(mesh.uv.Length!=mesh.vertexCount||mesh.normals.Length!=mesh.vertexCount)throw new InvalidDataException("Missing UV/normals: "+extension);
                            foreach(var point in mesh.vertices){Vector3 p=renderer.transform.TransformPoint(point);if(first){bounds=new Bounds(p,Vector3.zero);first=false;}else bounds.Encapsulate(p);}
                            foreach(var material in renderer.sharedMaterials){if(!material)throw new InvalidDataException("Missing material: "+extension);row.shadersSupported&=material.shader&&material.shader.isSupported;foreach(var property in material.GetTexturePropertyNames())row.hasTexture|=material.GetTexture(property)!=null;}
                        }
                        row.width=bounds.size.x;row.height=bounds.size.y;row.importedCentre=bounds.center;row.sourceUnchanged=Convert.ToBase64String(original)==Convert.ToBase64String(File.ReadAllBytes(path));Debug.Log("FORMAT_IMPORT "+JsonUtility.ToJson(row));
                        if(row.triangles!=2||row.materialSlots!=2||!row.sourceUnchanged||!row.shadersSupported||Mathf.Abs(row.width-.24f)>.0001f||Mathf.Abs(row.height-.1f)>.0001f)throw new InvalidDataException("Geometry/material/unit fixture mismatch: "+JsonUtility.ToJson(row));
                        if(extension=="glb"&&(!row.hasTexture||row.uv1Vertices!=row.vertices))throw new InvalidDataException("GLB lost embedded texture or UV1");if(extension=="obj"&&!row.hasTexture)throw new InvalidDataException("OBJ lost MTL texture");
                        // Authored format fixture has no source body: explicitly position the test patch,
                        // just as the studio's frame offset does. This is not an automatic garment fit.
                        row.explicitFrameOffset=new Vector3(0,1.15f,.30f)-bounds.center;foreach(var input in inputs)input.matrix=Matrix4x4.Translate(row.explicitFrameOffset)*input.matrix;
                        var package=GarmentTransferStudio.BuildPackage("Format test",engine,avatar,inputs);row.transferPassed=package.Check(avatar)==null;if(!row.transferPassed)throw new InvalidDataException("Format transfer failed: "+extension);foreach(var part in package.parts)UnityEngine.Object.DestroyImmediate(part.mesh);UnityEngine.Object.DestroyImmediate(package);rows.Add(row);
                    }finally{foreach(var input in inputs)UnityEngine.Object.DestroyImmediate(input.mesh);UnityEngine.Object.DestroyImmediate(root);}
                }
                File.WriteAllText(folder+"/formats.json",JsonUtility.ToJson(new Report{cases=rows.ToArray()},true));Debug.Log("GARMENT_FORMATS_OK");UnityEngine.Object.DestroyImmediate(target);EditorApplication.Exit(0);
            }catch(Exception e){File.WriteAllText(folder+"/formats.json",JsonUtility.ToJson(new Report{status="failed",cases=rows.ToArray()},true));Debug.LogException(e);if(target)UnityEngine.Object.DestroyImmediate(target);EditorApplication.Exit(1);}
        }
    }
}
#endif
