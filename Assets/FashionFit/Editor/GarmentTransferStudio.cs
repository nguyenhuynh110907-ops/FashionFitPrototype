#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FashionFit.Editor
{
    public sealed class GarmentTransferStudio:EditorWindow
    {
        GameObject frame,garment;Renderer sourceBody;float unit=1;Vector3 offset,euler;bool confirmed;Vector2 scroll;string status="";
        [MenuItem("Fashion Fit/3D to 3D/Transfer Garment")]
        public static void Open()=>GetWindow<GarmentTransferStudio>("Chuyển đồ 3D → 3D");
        void OnGUI()
        {
            scroll=EditorGUILayout.BeginScrollView(scroll);EditorGUILayout.LabelField("GIỮ CHI TIẾT TRANG PHỤC GỐC",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Đích: mannequin nữ fitting hiện tại. Nhập FBX/OBJ bằng Unity, đặt body và bộ đồ nguồn cùng một frame trong Scene. Cơ thể nguồn phải giữ đúng thứ tự đỉnh và tam giác của body mẫu; khác topology cần đăng ký tương ứng trước.",MessageType.Info);
            if(GUILayout.Button("Nh?n b?i sinh vi?n theo body m?u")) StudentGarmentWindow.Open();
            frame=(GameObject)EditorGUILayout.ObjectField("Frame nguồn",frame,typeof(GameObject),true);
            sourceBody=(Renderer)EditorGUILayout.ObjectField("Renderer cơ thể nguồn",sourceBody,typeof(Renderer),true);
            garment=(GameObject)EditorGUILayout.ObjectField("Nhánh trang phục",garment,typeof(GameObject),true);
            unit=EditorGUILayout.FloatField("Mét / đơn vị nguồn",unit);euler=EditorGUILayout.Vector3Field("Quay sang Y lên, Z trước",euler);offset=EditorGUILayout.Vector3Field("Dịch sang frame đích (m)",offset);
            confirmed=EditorGUILayout.ToggleLeft("Đã kiểm tra cùng tư thế nghỉ, đơn vị và tương ứng body",confirmed);
            using(new EditorGUI.DisabledScope(EditorApplication.isPlaying||!confirmed||!frame||!sourceBody||!garment))if(GUILayout.Button("Tạo bản chuyển và báo cáo"))Build();
            EditorGUILayout.Space();if(GUILayout.Button("Xuất body mẫu + outfit thử trong Scene"))CreateTemplateScene();
            if(GUILayout.Button("Chuẩn bị importer FBX/OBJ đang chọn"))try{GarmentImportPreparation.PrepareSelected();}catch(Exception e){status=e.Message;}
            EditorGUILayout.HelpBox("FBX/OBJ: importer Unity; GLB: Unity glTFast 6.12.1. Giữ số đỉnh sau import, UV0–UV7, màu đỉnh, submesh, tham chiếu vật liệu/texture. Chuyển trọng số xương theo barycentric. Blend shape, extension chưa kiểm chứng, renderer phản chiếu và vật liệu thiếu sẽ dừng để kiểm tra.",MessageType.None);
            EditorGUILayout.HelpBox("Cổng kiểm tra: không có điểm xuyên sâu hơn 1 mm theo pháp tuyến mặt gần nhất; biến dạng cạnh tối đa 25%; không tam giác sụp. Lấy mẫu đỉnh + tâm mặt chưa chứng minh không xuyên mọi nơi hoặc không tự xuyên. Chỉ xem đứng yên; chưa bật đi/VR kéo vải cho đồ chuyển.",MessageType.Warning);
            if(!string.IsNullOrEmpty(status))EditorGUILayout.TextArea(status,GUILayout.MinHeight(80));EditorGUILayout.EndScrollView();
        }
        void Build()
        {
            GameObject target=null;
            try{
                if(EditorUtility.IsPersistent(frame)||EditorUtility.IsPersistent(sourceBody)||EditorUtility.IsPersistent(garment))throw new InvalidDataException("Kéo prefab vào Scene trước để kiểm tra pose và frame.");
                if(!sourceBody.transform.IsChildOf(frame.transform)||!garment.transform.IsChildOf(frame.transform)||sourceBody.transform.IsChildOf(garment.transform))throw new InvalidDataException("Frame cần chứa body và nhánh đồ riêng biệt.");
                if(float.IsNaN(unit)||float.IsInfinity(unit)||unit<=0||unit>10)throw new InvalidDataException("Đơn vị không hợp lệ.");
                target=new GameObject("Temporary target body");var avatar=target.AddComponent<RunwayAvatar>();avatar.LoadTechnical("Fitting/female","Target body");
                Matrix4x4 canonical=Matrix4x4.TRS(offset,Quaternion.Euler(euler),Vector3.one*unit)*frame.transform.worldToLocalMatrix;
                Mesh body=Snapshot(sourceBody);TransferredGarmentAsset package=null;
                try{
                    var positions=body.vertices;Matrix4x4 m=canonical*sourceBody.transform.localToWorldMatrix;foreach(var row in new[]{m})if(row.determinant<=0)throw new InvalidDataException("Apply mirrored transforms before transfer.");for(int i=0;i<positions.Length;i++)positions[i]=m.MultiplyPoint3x4(positions[i]);
                    Mesh neutral=Snapshot(avatar.Renderer);GarmentSurfaceTransfer engine;try{engine=new GarmentSurfaceTransfer(positions,body.triangles,neutral.vertices,neutral.triangles,avatar.BodyMesh.boneWeights);}finally{UnityEngine.Object.DestroyImmediate(neutral);}
                    var inputs=new List<Input>();foreach(var r in garment.GetComponentsInChildren<Renderer>(false)){if(!r.enabled)continue;if(!(r is MeshRenderer)&&!(r is SkinnedMeshRenderer))throw new InvalidDataException("Unsupported renderer: "+r.name);inputs.Add(new Input{renderer=r,mesh=Snapshot(r),matrix=canonical*r.transform.localToWorldMatrix});}
                    if(inputs.Count==0)throw new InvalidDataException("Không có renderer trang phục đang bật.");
                    try{package=BuildPackage(garment.name,engine,avatar,inputs);string folder=AssetDatabase.GenerateUniqueAssetPath("Assets/Resources/TransferredGarments/"+SafeName(garment.name));Save(package,folder);status="Đã lưu: "+folder+"\n"+(package.stationarySamplesPassed?"Qua kiểm tra lấy mẫu tĩnh. Chọn gói này ở tab TRANG PHỤC khi Play.":"Cần kiểm tra lại: có điểm tiếp xúc hoặc méo quá giới hạn. Gói này bị chặn mặc thử.");Selection.activeObject=package;}
                    finally{foreach(var input in inputs)UnityEngine.Object.DestroyImmediate(input.mesh);}
                }finally{UnityEngine.Object.DestroyImmediate(body);}
            }catch(Exception e){status="Chưa chuyển: "+e.Message;Debug.LogWarning(status);}finally{if(target)UnityEngine.Object.DestroyImmediate(target);}
        }
        public sealed class Input {public Renderer renderer;public Mesh mesh;public Matrix4x4 matrix=Matrix4x4.identity;}
        public static TransferredGarmentAsset BuildPackage(string title,GarmentSurfaceTransfer engine,RunwayAvatar avatar,List<Input> inputs)
        {
            engine.source.RequireClosedBody();engine.target.RequireClosedBody();
            var package=CreateInstance<TransferredGarmentAsset>();package.displayName=title;package.sourceDescription="Original Unity meshes and material references; paired-body affine fit and barycentric residuals; source assets unchanged.";package.targetBodyKey=TailoredPoloStudy.GeometryKey(avatar.BodyMesh);package.targetBones=Array.ConvertAll(avatar.Bones,b=>b.name);var parts=new List<TransferredGarmentAsset.Part>();package.stationarySamplesPassed=true;
            try{
                int total=0;foreach(var input in inputs){
                    total+=input.mesh.vertexCount;if(total>300000)throw new InvalidDataException("Đợt thử tối đa 300.000 đỉnh; dùng LOD xem VR riêng, giữ bản gốc để đối chiếu.");
                    var materials=input.renderer.sharedMaterials;if(materials.Length!=input.mesh.subMeshCount)throw new InvalidDataException("Material slots must match submeshes: "+input.renderer.name);foreach(var material in materials)if(!material)throw new InvalidDataException("Missing material: "+input.renderer.name);
                    var bind=new Matrix4x4[avatar.Bones.Length];for(int i=0;i<bind.Length;i++)bind[i]=avatar.Bones[i].worldToLocalMatrix*avatar.transform.localToWorldMatrix;
                    Mesh result=engine.Transfer(input.mesh,input.matrix,bind,out TransferMetrics metrics);parts.Add(new TransferredGarmentAsset.Part{name=input.renderer.name,mesh=result,fittedGeometryKey=TailoredPoloStudy.GeometryKey(result),materials=(Material[])materials.Clone(),metrics=metrics});package.stationarySamplesPassed&=metrics.Passes;
                }
                package.parts=parts.ToArray();return package;
            }catch{foreach(var part in parts)UnityEngine.Object.DestroyImmediate(part.mesh);UnityEngine.Object.DestroyImmediate(package);throw;}
        }
        public static Mesh Snapshot(Renderer renderer)
        {
            GarmentImportPreparation.Check(renderer);
            Mesh original=renderer is SkinnedMeshRenderer skin?skin.sharedMesh:renderer.GetComponent<MeshFilter>()?.sharedMesh;
            if(!original||!original.isReadable)throw new InvalidDataException("Bật Read/Write cho mesh: "+renderer.name);
            if(original.blendShapeCount!=0)throw new InvalidDataException("Bake blend shapes ra mesh trung tính trước: "+renderer.name);
            Mesh snapshot=UnityEngine.Object.Instantiate(original);
            if(renderer is SkinnedMeshRenderer s){var baked=new Mesh();try{s.BakeMesh(baked,false);snapshot.vertices=baked.vertices;snapshot.normals=baked.normals;snapshot.tangents=baked.tangents;}finally{UnityEngine.Object.DestroyImmediate(baked);}}
            snapshot.name=original.name;return snapshot;
        }
        [Serializable] sealed class PackageReport {public string algorithm,displayName,targetBodyKey,sourceDescription;public bool stationarySamplesPassed,continuousCollisionTested,selfCollisionTested;public TransferMetrics[] parts;public string contactMethod="Closed outward body; exact nearest triangle; interior-face normal sign, three-ray parity at edges; vertices and triangle centroids; 1 mm tolerance. Finite samples, not CCD.";}
        public static void Save(TransferredGarmentAsset package,string folder)
        {
            Directory.CreateDirectory(folder);AssetDatabase.Refresh();AssetDatabase.CreateAsset(package,folder+"/Garment.asset");
            foreach(var part in package.parts)AssetDatabase.AddObjectToAsset(part.mesh,package);
            // Demo materials may be transient; create private copies so the asset is self-contained.
            var owned=new Dictionary<Material,Material>();foreach(var part in package.parts)for(int i=0;i<part.materials.Length;i++)if(!EditorUtility.IsPersistent(part.materials[i])){var original=part.materials[i];if(!owned.TryGetValue(original,out Material saved)){saved=new Material(original);owned.Add(original,saved);AssetDatabase.AddObjectToAsset(saved,package);}part.materials[i]=saved;}
            EditorUtility.SetDirty(package);AssetDatabase.SaveAssets();
            var report=new PackageReport{algorithm=package.algorithm,displayName=package.displayName,targetBodyKey=package.targetBodyKey,sourceDescription=package.sourceDescription,stationarySamplesPassed=package.stationarySamplesPassed,parts=Array.ConvertAll(package.parts,p=>p.metrics)};File.WriteAllText(folder+"/transfer-report.json",JsonUtility.ToJson(report,true));AssetDatabase.Refresh();
        }
        static string SafeName(string title){foreach(char c in Path.GetInvalidFileNameChars())title=title.Replace(c,'_');return string.IsNullOrWhiteSpace(title)?"Garment":title;}
        [MenuItem("Fashion Fit/3D to 3D/Create Body and Garment Template")]
        public static void CreateTemplateScene()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before creating the source template.");
            var root=(GameObject)PrefabUtility.InstantiatePrefab(EnsureTemplateAssets());Undo.RegisterCreatedObjectUndo(root,"Create garment source template");
            var window=GetWindow<GarmentTransferStudio>("Chuyển đồ 3D → 3D");window.frame=root;window.garment=root.transform.Find("Garment").gameObject;window.sourceBody=root.transform.Find("SourceBody").GetComponent<Renderer>();window.unit=1;window.euler=window.offset=Vector3.zero;window.confirmed=false;Selection.activeGameObject=root;
        }
        public const string TemplateFolder="Assets/FashionFit/TransferTemplates";
        public static GameObject EnsureTemplateAssets()
        {
            string prefabPath=TemplateFolder+"/FittingReference.prefab";var existing=AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);if(existing)return existing;
            Directory.CreateDirectory(TemplateFolder);AssetDatabase.Refresh();var temporary=new GameObject("Temporary template generator");var avatar=temporary.AddComponent<RunwayAvatar>();avatar.LoadTechnical("Fitting/female","Fitting reference");var outfit=new GameObject("Temporary study");outfit.transform.SetParent(temporary.transform,false);var study=outfit.AddComponent<TailoredPoloStudy>();var fabric=new Material(Resources.Load<Shader>("Runway/DressFabric"));fabric.SetColor("_Color",new Color(.30f,.42f,.45f));study.Initialize(avatar,fabric);var export=new GameObject("FittingReference — metres, Y up, Z forward");var garment=new GameObject("Garment");garment.transform.SetParent(export.transform,false);var materials=new Dictionary<Material,Material>();
            try{
                int index=0;foreach(var renderer in temporary.GetComponentsInChildren<Renderer>()){
                    if(!(renderer is MeshRenderer)&&!(renderer is SkinnedMeshRenderer))continue;Mesh mesh=Snapshot(renderer);mesh.name=renderer==avatar.Renderer?"Canonical neutral source body":renderer.name;AssetDatabase.CreateAsset(mesh,TemplateFolder+"/Mesh"+(index++).ToString("00")+".asset");
                    var child=new GameObject(renderer==avatar.Renderer?"SourceBody":renderer.name);child.transform.SetParent(renderer==avatar.Renderer?export.transform:garment.transform,false);child.AddComponent<MeshFilter>().sharedMesh=mesh;var slots=renderer.sharedMaterials;
                    for(int i=0;i<slots.Length;i++){if(!materials.TryGetValue(slots[i],out Material copy)){copy=new Material(slots[i]);AssetDatabase.CreateAsset(copy,TemplateFolder+"/Material"+materials.Count.ToString("00")+".mat");materials.Add(slots[i],copy);}slots[i]=copy;}child.AddComponent<MeshRenderer>().sharedMaterials=slots;
                    if(renderer==avatar.Renderer)WriteTemplateOBJ(mesh);
                }
                var prefab=PrefabUtility.SaveAsPrefabAsset(export,prefabPath);AssetDatabase.SaveAssets();return prefab;
            }finally{UnityEngine.Object.DestroyImmediate(export);UnityEngine.Object.DestroyImmediate(temporary);UnityEngine.Object.DestroyImmediate(fabric);}
        }
        static void WriteTemplateOBJ(Mesh mesh)
        {
            string folder="Samples/3DTransfer";Directory.CreateDirectory(folder);using(var writer=new StreamWriter(folder+"/FittingBodyReference.obj",false,System.Text.Encoding.UTF8)){writer.WriteLine("# Neutral fitting body; metres; raw Unity X/Y/Z coordinates; no skeleton. Keep canonical Unity body for correspondence.");foreach(var p in mesh.vertices)writer.WriteLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,"v {0:R} {1:R} {2:R}",p.x,p.y,p.z));int[] indices=mesh.triangles;for(int i=0;i<indices.Length;i+=3)writer.WriteLine("f "+(indices[i]+1)+" "+(indices[i+1]+1)+" "+(indices[i+2]+1));}
        }
    }
}
#endif
