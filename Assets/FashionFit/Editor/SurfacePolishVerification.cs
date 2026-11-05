#if UNITY_EDITOR
using System;using System.IO;using System.Reflection;using UnityEngine;using UnityEditor;
namespace FashionFit.Editor {
 public static class SurfacePolishVerification {
  public static void RunBatch(){GameObject root=null;try{root=new GameObject("Surface and readout QA");var app=root.AddComponent<FashionFitApp>();typeof(FashionFitApp).GetMethod("Start",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(app,null);if(!app.Ready)throw new Exception(app.FatalError);SurfacePolishScenario.Run(app,Path.GetFullPath("Verification/SurfacePolish/Final"));UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);if(root)UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(1);}}
 }
}
#endif
