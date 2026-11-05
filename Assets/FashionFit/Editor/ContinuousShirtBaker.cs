#if UNITY_EDITOR
using System;using System.IO;using System.Reflection;using UnityEngine;using UnityEditor;
namespace FashionFit.Editor {
 public static class ContinuousShirtBaker {
  public static void RunBatch(){GameObject root=null;try{Directory.CreateDirectory("Assets/Resources/ContinuousShirts");AssetDatabase.Refresh();root=new GameObject("Continuous shirt cache authoring");var app=root.AddComponent<FashionFitApp>();typeof(FashionFitApp).GetMethod("Start",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(app,null);if(!app.Ready)throw new Exception(app.FatalError);var bodies=new[]{app.Model,app.PoseStudio.Rigs[0].Avatar,app.PoseStudio.Rigs[1].Avatar};foreach(var body in bodies){var mesh=ContinuousShirtShell.Generate(body,false);string path="Assets/Resources/ContinuousShirts/"+ContinuousShirtShell.CacheKey(body,false)+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old){EditorUtility.CopySerialized(mesh,old);EditorUtility.SetDirty(old);UnityEngine.Object.DestroyImmediate(mesh);}else AssetDatabase.CreateAsset(mesh,path);}AssetDatabase.SaveAssets();UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);if(root)UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(1);}}
 }
}
#endif
