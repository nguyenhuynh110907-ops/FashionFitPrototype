#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FashionFit.Editor
{
    public static class GarmentImportPreparation
    {
        [MenuItem("Fashion Fit/3D to 3D/Prepare Selected FBX or OBJ")]
        public static void PrepareSelected()
        {
            string path=AssetDatabase.GetAssetPath(Selection.activeObject);Prepare(path);Debug.Log("Garment import prepared: "+path);
        }
        public static void Prepare(string path)
        {
            var importer=AssetImporter.GetAtPath(path) as ModelImporter;if(!importer)throw new InvalidDataException("Chọn asset FBX/OBJ trong Project. GLB dùng Unity glTFast đã cài.");
            importer.isReadable=true;importer.meshCompression=ModelImporterMeshCompression.Off;importer.weldVertices=false;importer.optimizeMeshPolygons=false;importer.optimizeMeshVertices=false;importer.importNormals=ModelImporterNormals.Import;importer.SaveAndReimport();
        }
        [Serializable] sealed class GltfHeader {public string[] extensionsUsed,extensionsRequired;}
        public static void Check(Renderer renderer)
        {
            Mesh original=renderer is SkinnedMeshRenderer skin?skin.sharedMesh:renderer.GetComponent<MeshFilter>()?.sharedMesh;string path=AssetDatabase.GetAssetPath(original);if(string.IsNullOrEmpty(path))return;
            if(AssetImporter.GetAtPath(path) is ModelImporter model){if(model.meshCompression!=ModelImporterMeshCompression.Off)throw new InvalidDataException("Tắt Mesh Compression trước khi chuyển: "+path);return;}
            string json=null;
            if(path.EndsWith(".glb",StringComparison.OrdinalIgnoreCase))using(var reader=new BinaryReader(File.OpenRead(path))){if(reader.ReadUInt32()!=0x46546c67||reader.ReadUInt32()!=2)throw new InvalidDataException("GLB 2.0 required.");reader.ReadUInt32();int length=reader.ReadInt32();if(reader.ReadUInt32()!=0x4e4f534a||length<2||length>16*1024*1024)throw new InvalidDataException("Invalid GLB JSON chunk.");json=System.Text.Encoding.UTF8.GetString(reader.ReadBytes(length));}
            else if(path.EndsWith(".gltf",StringComparison.OrdinalIgnoreCase))json=File.ReadAllText(path);
            if(json==null)return;var header=JsonUtility.FromJson<GltfHeader>(json);var allowed=new HashSet<string>{"KHR_texture_transform","KHR_materials_unlit","KHR_mesh_quantization"};foreach(var list in new[]{header.extensionsUsed,header.extensionsRequired})if(list!=null)foreach(var extension in list)if(!allowed.Contains(extension))throw new InvalidDataException("glTF extension cần kiểm chứng để giữ chi tiết: "+extension+". Chưa chấp nhận bản chuyển này.");
        }
    }
}
#endif
