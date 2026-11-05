#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace FashionFit.Editor
{
    public static class GarmentTransferVerification
    {
        const string Folder="Verification/GarmentTransfer";
        [Serializable] sealed class Result {public string status="passed";public int checks,parts,vertices,triangles,materialSlots;public float identityMaxError,changedShapeMaxError;public bool sourceUnchanged,changedShapeSampleGate,bodyMaskUsed=false,motionEnabled=false,trainedNetwork=false;public double transferSeconds;public TransferMetrics[] identity,changedShape;}
        static Result result;
        static void Check(bool pass,string message){result.checks++;if(!pass)throw new InvalidOperationException(message);}
        static void Reject(Action action,string message){bool rejected=false;try{action();}catch(InvalidDataException){rejected=true;}Check(rejected,message);}
        public static void RunBatch()
        {
            result=new Result();Directory.CreateDirectory(Folder);GameObject root=null;
            try{
                Analytic();root=new GameObject("3D transfer verification");var app=root.AddComponent<FashionFitApp>();typeof(FashionFitApp).GetMethod("Start",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(app,null);Check(app.Ready&&!app.HideCoveredBody,"Need live full body");app.SetLook(6);app.StandForFitting();for(int i=0;i<180;i++)app.AdvanceSimulation(1f/60);
                var inputs=new List<GarmentTransferStudio.Input>();foreach(var r in app.Model.GetComponentsInChildren<Renderer>())if(r!=app.Model.Renderer&&(r is MeshRenderer||r is SkinnedMeshRenderer))inputs.Add(new GarmentTransferStudio.Input{renderer=r,mesh=GarmentTransferStudio.Snapshot(r),matrix=app.Model.transform.worldToLocalMatrix*r.transform.localToWorldMatrix});
                Check(inputs.Count>=3,"Missing skirt, upper, collar/button detail meshes");var sourceKeys=new List<string>();foreach(var input in inputs)sourceKeys.Add(TailoredPoloStudy.GeometryKey(input.mesh));
                var neutral=GarmentTransferStudio.Snapshot(app.Model.Renderer);var targetVertices=neutral.vertices;
                var engine=new GarmentSurfaceTransfer(targetVertices,neutral.triangles,targetVertices,neutral.triangles,app.Model.BodyMesh.boneWeights);var timer=System.Diagnostics.Stopwatch.StartNew();
                var package=GarmentTransferStudio.BuildPackage("3D transfer · bản nghiên cứu có cổ và nút",engine,app.Model,inputs);timer.Stop();result.transferSeconds=timer.Elapsed.TotalSeconds;result.parts=package.parts.Length;result.identity=Array.ConvertAll(package.parts,p=>p.metrics);
                for(int p=0;p<package.parts.Length;p++){
                    var original=inputs[p].mesh;var copied=package.parts[p].mesh;var before=original.vertices;var after=copied.vertices;result.vertices+=after.Length;result.triangles+=copied.triangles.Length/3;result.materialSlots+=package.parts[p].materials.Length;
                    Check(original.vertexCount==copied.vertexCount&&Equal(original.triangles,copied.triangles),"Topology changed");Check(original.subMeshCount==copied.subMeshCount,"Material partition changed");
                    for(int sub=0;sub<copied.subMeshCount;sub++)Check(Equal(original.GetIndices(sub),copied.GetIndices(sub)),"Submesh order changed");
                    for(int channel=0;channel<8;channel++){var a=new List<Vector4>();var b=new List<Vector4>();original.GetUVs(channel,a);copied.GetUVs(channel,b);Check(a.Count==b.Count,"UV channel lost");for(int i=0;i<a.Count;i++)if(a[i]!=b[i])throw new InvalidOperationException("UV changed");}
                    Check(Equal(original.colors32,copied.colors32),"Vertex colors changed");
                    for(int i=0;i<after.Length;i++)result.identityMaxError=Mathf.Max(result.identityMaxError,Vector3.Distance(inputs[p].matrix.MultiplyPoint3x4(before[i]),after[i]));
                    for(int i=0;i<package.parts[p].materials.Length;i++)Check(package.parts[p].materials[i]==inputs[p].renderer.sharedMaterials[i],"Original material reference changed");
                    foreach(var w in copied.boneWeights)if(Mathf.Abs(w.weight0+w.weight1+w.weight2+w.weight3-1)>.00001f)throw new InvalidOperationException("Weights not normalized");Check(copied.boneWeights.Length==copied.vertexCount,"Skin transfer missing");
                }
                Check(result.identityMaxError<.000001f,"Identity transfer moved details");Check(package.stationarySamplesPassed,"Identity sample contact gate failed");
                // A real body-shape change: original body and outfit narrowed by 2%, then transferred back.
                var from=(Vector3[])targetVertices.Clone();for(int i=0;i<from.Length;i++)from[i]=Vector3.Scale(from[i],new Vector3(.98f,1,.98f));
                var changed=new GarmentSurfaceTransfer(from,neutral.triangles,targetVertices,neutral.triangles,app.Model.BodyMesh.boneWeights);var smallInputs=new List<GarmentTransferStudio.Input>();foreach(var input in inputs)smallInputs.Add(new GarmentTransferStudio.Input{renderer=input.renderer,mesh=input.mesh,matrix=Matrix4x4.Scale(new Vector3(.98f,1,.98f))*input.matrix});
                var changedPackage=GarmentTransferStudio.BuildPackage("Changed shape validation",changed,app.Model,smallInputs);result.changedShapeSampleGate=changedPackage.stationarySamplesPassed;result.changedShape=Array.ConvertAll(changedPackage.parts,p=>p.metrics);
                for(int p=0;p<package.parts.Length;p++){var a=package.parts[p].mesh.vertices;var b=changedPackage.parts[p].mesh.vertices;for(int i=0;i<a.Length;i++)result.changedShapeMaxError=Mathf.Max(result.changedShapeMaxError,Vector3.Distance(a[i],b[i]));}
                Check(result.changedShapeSampleGate&&result.changedShapeMaxError<.00001f,"Size-change transfer lost detail or failed geometry gate");
                Export(neutral,package,"Look1_Identity.bin");Export(neutral,changedPackage,"Look2_ChangedShape.bin");
                Check(package.Check(app.Model)==null&&package.CheckNeutralRig(app.Model)==null,"Package target validation failed");var savedKey=package.targetBodyKey;package.targetBodyKey="different";Check(package.Check(app.Model)!=null,"Changed body allowed");package.targetBodyKey=savedKey;
                var oldBind=package.parts[0].mesh.bindposes;var badBind=(Matrix4x4[])oldBind.Clone();badBind[0]=Matrix4x4.Translate(Vector3.up);package.parts[0].mesh.bindposes=badBind;Check(package.CheckNeutralRig(app.Model)!=null,"Changed neutral bind pose accepted");package.parts[0].mesh.bindposes=oldBind;
                var key=package.parts[0].fittedGeometryKey;package.parts[0].fittedGeometryKey="different";Check(package.Check(app.Model)!=null,"Changed garment allowed");package.parts[0].fittedGeometryKey=key;
                bool unchanged=true;for(int i=0;i<inputs.Count;i++)unchanged&=sourceKeys[i]==TailoredPoloStudy.GeometryKey(inputs[i].mesh);result.sourceUnchanged=unchanged;Check(unchanged,"Source geometry modified");
                string destination="Assets/Resources/TransferredGarments/DetailDemo";if(AssetDatabase.IsValidFolder(destination))AssetDatabase.DeleteAsset(destination);GarmentTransferStudio.Save(package,destination);
                app.Wardrobe.WearTransferred(package);Check(app.Wardrobe.HasDetailedTransfer&&app.FittingPose,"Runtime transfer preview failed");Check(!app.HideCoveredBody&&app.Model.HiddenBodyTriangles==0,"Body hidden to pass QA");
                app.StartPresentationAt(.5f);Check(!app.Playing&&app.FittingPose,"Unverified walking enabled");app.TogglePlay();Check(!app.Playing,"Toggle bypassed motion gate");app.Seek(.5f);Check(app.FittingPose,"Seek bypassed motion gate");app.RestartShow();Check(!app.Playing,"Restart bypassed motion gate");
                for(int i=0;i<120;i++)app.AdvanceSimulation(1f/60);Check(!app.Playing&&app.FittingPose,"Static review drifted into walking");
                Capture(app,"Transferred_Detail.png");app.SetLook(1);Check(!app.Wardrobe.HasDetailedTransfer,"Transfer state remained on preset");app.StartSlowPresentation();Check(app.Playing,"Existing cloth motion regressed");app.StandForFitting();
                var template=GarmentTransferStudio.EnsureTemplateAssets();Check(template&&template.GetComponentsInChildren<MonoBehaviour>().Length==0,"Template depends on transient runtime generators");var bodyRenderer=template.transform.Find("SourceBody").GetComponent<Renderer>();var bodyReference=GarmentTransferStudio.Snapshot(bodyRenderer);var templateEngine=new GarmentSurfaceTransfer(bodyReference.vertices,bodyReference.triangles,targetVertices,neutral.triangles,app.Model.BodyMesh.boneWeights);var templateInputs=new List<GarmentTransferStudio.Input>();foreach(var renderer in template.transform.Find("Garment").GetComponentsInChildren<Renderer>())templateInputs.Add(new GarmentTransferStudio.Input{renderer=renderer,mesh=GarmentTransferStudio.Snapshot(renderer),matrix=Matrix4x4.identity});var templatePackage=GarmentTransferStudio.BuildPackage("Template round trip",templateEngine,app.Model,templateInputs);Check(templatePackage.Check(app.Model)==null,"Native template/import flow failed");Check(File.Exists("Samples/3DTransfer/FittingBodyReference.obj"),"Artist body reference not exported");
                File.WriteAllText(Folder+"/verification.json",JsonUtility.ToJson(result,true));Debug.Log("GARMENT_TRANSFER_OK "+JsonUtility.ToJson(result));UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(0);
            }catch(Exception e){result.status="failed";File.WriteAllText(Folder+"/verification.json",JsonUtility.ToJson(result,true));File.WriteAllText(Folder+"/failure.txt",e.ToString());Debug.LogException(e);if(root)UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(1);}
        }
        static bool Equal<T>(T[] a,T[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(!EqualityComparer<T>.Default.Equals(a[i],b[i]))return false;return true;}
        static void Analytic()
        {
            var from=new[]{new Vector3(0,0,0),new Vector3(.2f,0,0),new Vector3(0,.2f,0)};int[] faces={0,1,2};var same=new GarmentSurfaceTransfer(from,faces,from,faces);Vector3 p=new Vector3(.05f,.06f,.01f);Check(Vector3.Distance(same.Map(p,out var h,out _),p)<1e-7,"Identity map wrong");Check(Vector3.Distance(h.barycentric,new Vector3(.45f,.25f,.30f))<1e-5,"Barycentric wrong");
            Vector3 delta=new Vector3(.03f,.02f,.01f);var moved=Array.ConvertAll(from,v=>v+delta);var translated=new GarmentSurfaceTransfer(from,faces,moved,faces);Check(Vector3.Distance(translated.Map(p,out _,out _),p+delta)<1e-6,"Translation wrong");
            Quaternion rotation=Quaternion.Euler(0,0,15);var rotated=Array.ConvertAll(from,v=>rotation*v);var turning=new GarmentSurfaceTransfer(from,faces,rotated,faces);Check(Vector3.Distance(turning.Map(p,out _,out _),rotation*p)<1e-6,"Rigid rotation loses detail");
            var broad=Array.ConvertAll(from,v=>new Vector3(v.x*1.2f,v.y,v.z));var widen=new GarmentSurfaceTransfer(from,faces,broad,faces);Check(Vector3.Distance(widen.Map(p,out _,out _),new Vector3(p.x*1.2f,p.y,p.z))<1e-6,"Triangle affine mapping wrong");
            Reject(()=>new GarmentSurfaceTransfer(from,faces,from,new[]{0,2,1}),"Wrong topology accepted");Reject(()=>new GarmentSurfaceTransfer(from,faces,new Vector3[4],faces),"Wrong vertex count accepted");Reject(()=>same.Map(new Vector3(float.NaN,0,0),out _,out _),"NaN accepted");Reject(()=>same.Map(new Vector3(10,0,0),out _,out _),"Distant garment accepted");
            Check(same.target.LocalSignedClearance(new Vector3(.05f,.05f,-.01f))<-.009,"Deep penetration not detected");
            Reject(()=>same.source.RequireClosedBody(),"Open body accepted for contact qualification");
            // Non-empty secondary UV channels, authored normals, tangent sign and colours.
            var weights=new[]{new BoneWeight{weight0=1},new BoneWeight{weight0=1},new BoneWeight{weight0=1}};var skinEngine=new GarmentSurfaceTransfer(from,faces,from,faces,weights);var detail=new Mesh{name="Attribute preservation fixture",vertices=new[]{new Vector3(.02f,.02f,.01f),new Vector3(.08f,.02f,.01f),new Vector3(.02f,.08f,.01f)},triangles=faces};
            detail.normals=new[]{Vector3.forward,Vector3.forward,Vector3.forward};detail.tangents=new[]{new Vector4(1,0,0,-1),new Vector4(1,0,0,-1),new Vector4(1,0,0,-1)};detail.colors32=new[]{new Color32(20,30,40,255),new Color32(90,80,70,255),new Color32(100,110,120,128)};
            for(int channel=0;channel<8;channel++)detail.SetUVs(channel,new List<Vector4>{new Vector4(.1f,.2f,channel,.5f),new Vector4(.7f,.2f,channel,.5f),new Vector4(.1f,.9f,channel,.5f)});
            var copied=skinEngine.Transfer(detail,Matrix4x4.identity,new[]{Matrix4x4.identity},out _);Check(Equal(detail.colors32,copied.colors32)&&Equal(detail.normals,copied.normals)&&Equal(detail.tangents,copied.tangents),"Authored shading or colours changed");for(int channel=0;channel<8;channel++){var a=new List<Vector4>();var b=new List<Vector4>();detail.GetUVs(channel,a);copied.GetUVs(channel,b);Check(Equal(a.ToArray(),b.ToArray()),"Secondary UV changed");}
            Reject(()=>skinEngine.Transfer(detail,Matrix4x4.Scale(new Vector3(-1,1,1)),new[]{Matrix4x4.identity},out _),"Mirrored garment accepted");detail.AddBlendShapeFrame("Unsupported shape",100,new Vector3[3],new Vector3[3],new Vector3[3]);Reject(()=>skinEngine.Transfer(detail,Matrix4x4.identity,new[]{Matrix4x4.identity},out _),"Unsupported blend shape silently discarded");UnityEngine.Object.DestroyImmediate(detail);UnityEngine.Object.DestroyImmediate(copied);
            // Compare BVH with exhaustive triangle queries, independent of tree traversal.
            var random=new System.Random(73);var points=new Vector3[180];var indices=new int[180];for(int i=0;i<points.Length;i++){points[i]=new Vector3((float)random.NextDouble(),(float)random.NextDouble(),(float)random.NextDouble());indices[i]=i;}var surface=new GarmentSurfaceTransfer.Surface(points,indices);
            for(int i=0;i<60;i++){Vector3 q=new Vector3((float)random.NextDouble(),(float)random.NextDouble(),(float)random.NextDouble());float expected=float.PositiveInfinity;for(int j=0;j<indices.Length;j+=3)expected=Mathf.Min(expected,(q-GarmentSurfaceTransfer.Closest(q,points[j],points[j+1],points[j+2],out _)).sqrMagnitude);Check(Mathf.Abs(surface.Nearest(q).distanceSquared-expected)<1e-7,"BVH missed closest triangle");}
        }
        static void Export(Mesh neutral,TransferredGarmentAsset package,string filename)
        {
            var points=new List<Vector3>();var triangles=new List<int>();foreach(var part in package.parts){int first=points.Count;points.AddRange(part.mesh.vertices);foreach(int index in part.mesh.triangles)triangles.Add(first+index);}
            using(var w=new BinaryWriter(File.Create(Folder+"/"+filename))){w.Write(0x53434631);Write(w,neutral.vertices,neutral.triangles);Write(w,points.ToArray(),triangles.ToArray());}
        }
        static void Write(BinaryWriter writer,Vector3[] points,int[] faces){writer.Write(points.Length);foreach(var p in points){writer.Write(p.x);writer.Write(p.y);writer.Write(p.z);}writer.Write(faces.Length);foreach(int index in faces)writer.Write(index);}
        static void Capture(FashionFitApp app,string file)
        {
            Camera c=app.Player.View;c.rect=new Rect(0,0,1,1);c.transform.position=new Vector3(.65f,1.42f,1.7f);c.transform.LookAt(new Vector3(0,1.15f,0));c.fieldOfView=39;
            var snapshots=new List<GameObject>();var meshes=new List<Mesh>();var skins=app.Model.GetComponentsInChildren<SkinnedMeshRenderer>();foreach(var skin in skins){var mesh=new Mesh();skin.BakeMesh(mesh);meshes.Add(mesh);var go=new GameObject("Baked detail snapshot");go.transform.SetPositionAndRotation(skin.transform.position,skin.transform.rotation);go.transform.localScale=skin.transform.lossyScale;go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;snapshots.Add(go);}
            var rt=new RenderTexture(1100,1300,24);var texture=new Texture2D(1100,1300,TextureFormat.RGB24,false);var previous=RenderTexture.active;
            try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1100,1300),0,0);texture.Apply();File.WriteAllBytes(Folder+"/"+file,texture.EncodeToPNG());}finally{c.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);foreach(var go in snapshots)UnityEngine.Object.DestroyImmediate(go);foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);foreach(var skin in skins)skin.enabled=true;}
        }
    }
}
#endif
