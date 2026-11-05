#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FashionFit.Editor
{
    public static class FabricTrialVerification
    {
        static GameObject root;
        static FashionFitApp app;
        static double started;
        static int checks;
        static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Verification/FabricTrial"));
        static void Check(bool value,string message){checks++;if(!value)throw new InvalidOperationException(message);}
        static void Same(Vector3[] a,Vector3[] b){Check(a.Length==b.Length,"Vertex count changed");for(int i=0;i<a.Length;i++)if(a[i]!=b[i])throw new InvalidOperationException("Material trial changed positions");}
        static byte[] Capture(string file)
        {
            foreach(Transform tr in app.Model.GetComponentsInChildren<Transform>())tr.gameObject.layer=30;
            Camera c=app.Player.View;c.enabled=false;c.rect=new Rect(0,0,1,1);c.cullingMask=1<<30;c.fieldOfView=38;
            c.transform.position=new Vector3(.85f,1.3f,2.4f);c.transform.LookAt(new Vector3(0,.95f,0));
            c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=new Color(.82f,.83f,.82f);
            var rt=new RenderTexture(640,800,24);var texture=new Texture2D(640,800,TextureFormat.RGB24,false);var old=RenderTexture.active;
            try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,640,800),0,0);texture.Apply();byte[] png=texture.EncodeToPNG();File.WriteAllBytes(Path.Combine(Folder,file),png);return texture.GetRawTextureData();}
            finally{c.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
        }
        public static void RunBatch()
        {
            try{
                Directory.CreateDirectory(Folder);root=new GameObject("Fabric trial verification");app=root.AddComponent<FashionFitApp>();
                typeof(FashionFitApp).GetMethod("Start",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(app,null);
                Check(app.Ready,"App not ready");Check(app.FabricTrial.Samples.Length==6,"Expected six fabric samples");Check(app.FabricTrial.CanAnalyze,"Local AI environment missing");
                app.SetLook(2);for(int i=0;i<90;i++)app.AdvanceSimulation(1f/60);
                FittingCloth cloth=app.Model.GetComponentInChildren<FittingCloth>();Mesh mesh=cloth.GetComponent<MeshFilter>().sharedMesh;Vector3[] vertices=mesh.vertices;Vector2[] uv=mesh.uv;
                byte[] original=Capture("Original.png");
                for(int i=0;i<6;i++){
                    app.FabricTrial.SelectSample(i);app.FabricTrial.ApplySelected();Same(vertices,mesh.vertices);
                    Vector2[] currentUV=mesh.uv;Check(currentUV.Length==uv.Length,"UV count changed");for(int n=0;n<uv.Length;n++)Check(currentUV[n]==uv[n],"UV changed");
                    Material material=cloth.Surface.sharedMaterial;
                    Check(material.GetFloat("_UseBase")==1&&material.GetFloat("_UseNormal")==1&&material.GetFloat("_UseRoughness")==1,"PBR maps not connected");
                    Check(!ShaderUtil.ShaderHasError(material.shader),"Fabric shader has errors");
                    Capture("Sample"+i+".png");
                    app.Wardrobe.ToggleMapComparison();Check(material.GetFloat("_UseBase")==0&&material.GetFloat("_UseNormal")==0&&material.GetFloat("_UseRoughness")==0,"Comparison did not disable all maps");Same(vertices,mesh.vertices);
                    if(i==0){byte[] compared=Capture("Comparison.png");int different=0;for(int n=0;n<original.Length;n++)if(original[n]!=compared[n])different++;Check(different<200,"Comparison did not restore the original appearance");}
                    app.Wardrobe.ToggleMapComparison();
                }
                string invalid=Path.Combine(Folder,"invalid.png");File.WriteAllText(invalid,"not an image");
                Texture saved=cloth.Surface.sharedMaterial.GetTexture("_BaseMap");bool rejected=false;
                try{app.Wardrobe.ApplySurfaceFiles(app.FabricTrial.SelectedImage,invalid);}catch(InvalidDataException){rejected=true;}
                Check(rejected&&cloth.Surface.sharedMaterial.GetTexture("_BaseMap")==saved,"Invalid replacement must retain the old material");File.Delete(invalid);
                app.SetLook(5);var shirt=app.Model.GetComponentInChildren<InteractiveShirt>();Material shirtMaterial=shirt.GetComponent<MeshRenderer>().sharedMaterial;
                Check(shirtMaterial.GetFloat("_UseRoughness")==1&&shirtMaterial.color==Color.white,"Changing outfits must retain maps without tinting the blouse");Capture("IvorySample.png");
                app.Wardrobe.ClearMaps();Check(!app.Wardrobe.HasSurfaceMaps,"Clear maps failed");app.SetLook(2);app.FabricTrial.SelectSample(0);app.FabricTrial.ApplySelected();
                app.FabricTrial.Analyze();Check(app.FabricTrial.Busy,"Analysis did not start");started=EditorApplication.timeSinceStartup;EditorApplication.update+=Poll;
            }catch(Exception error){Fail(error);}
        }
        static void Poll()
        {
            try{
                if(EditorApplication.timeSinceStartup-started>200)throw new TimeoutException("AI bridge timed out");
                app.FabricTrial.Poll();if(app.FabricTrial.Busy)return;
                Check(app.FabricTrial.Result!=null,"AI bridge returned no result: "+app.FabricTrial.Status);
                File.WriteAllText(Path.Combine(Folder,"unity_ai_prediction.json"),JsonUtility.ToJson(app.FabricTrial.Result,true));
                app.FabricTrial.Analyze();app.FabricTrial.Cancel();Check(!app.FabricTrial.Busy,"Cancel left a worker running");
                app.FabricTrial.Analyze();app.FabricTrial.SelectSample(1);Check(!app.FabricTrial.Busy&&app.FabricTrial.Result==null,"Selection change must cancel/discard previous results");
                EditorApplication.update-=Poll;
                string json="{\"status\":\"passed\",\"samples\":6,\"checks\":"+checks+",\"mesh_and_uv_unchanged\":true,\"comparison_restores_original\":true,\"pbr_maps_connected\":true,\"python_inference_completed_in_unity\":true,\"cancel_and_selection_change_verified\":true,\"full_student_asset_import\":false}";
                File.WriteAllText(Path.Combine(Folder,"verification.json"),json);Debug.Log("FABRIC_TRIAL_OK "+json);
                UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(0);
            }catch(Exception error){Fail(error);}
        }
        static void Fail(Exception error){EditorApplication.update-=Poll;Debug.LogException(error);Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"failure.txt"),error.ToString());if(root)UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(1);}
    }
}
#endif
