#if UNITY_EDITOR
using System;using System.Reflection;using System.IO;using UnityEngine;using UnityEditor;
namespace FashionFit.Editor {
 public static class MensCollectionVerification {
  public static void RunBatch(){GameObject root=null;try{root=new GameObject("Five mens collection QA");var app=root.AddComponent<FashionFitApp>();typeof(FashionFitApp).GetMethod("Start",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(app,null);if(!app.Ready)throw new Exception(app.FatalError);MensCollectionScenario.Run(app,Path.GetFullPath("Verification/HollisterMens"));UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);if(root)UnityEngine.Object.DestroyImmediate(root);EditorApplication.Exit(1);}}
 }
}
#endif
