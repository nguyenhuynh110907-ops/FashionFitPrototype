using System;using System.IO;using System.Collections;using System.Collections.Generic;using UnityEngine;
namespace FashionFit {
 public sealed class DoffablePlayerVerification:MonoBehaviour {
  [Serializable] sealed class Result {public string status="passed",scope="Exported Windows player, real render-time cloth updates; no physical headset test";public int checks,exceptions;public bool nearOnly,opened,reclosed,twoHands,free,independentRelease,gravity,persistent,reset;public float dropM,maxStretch,meanFrameMs,p95FrameMs,baselineFrameMs,settledFrameMs;public bool lowQualityPreset;}
  string output;bool trial;Result result=new Result();FashionFitApp app;DoffableBlouse blouse;readonly List<float> frameTimes=new List<float>();
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Install(){var args=Environment.GetCommandLineArgs();string folder=null;bool preview=false;for(int i=0;i<args.Length;i++){if(args[i]=="-fashionDoffTrial")preview=true;if(args[i]=="-fashionDoffVerify"&&i+1<args.Length)folder=args[i+1];}if(folder==null&&!preview)return;var v=new GameObject("Doffable blouse opt-in verification").AddComponent<DoffablePlayerVerification>();v.output=folder;v.trial=folder==null;}
  void Check(bool condition,string message){result.checks++;if(!condition)throw new InvalidOperationException(message);}
  void Log(string a,string b,LogType t){if(t==LogType.Error||t==LogType.Exception)result.exceptions++;}
  IEnumerator Start(){
   Application.runInBackground=true;Application.logMessageReceived+=Log;if(output!=null)Directory.CreateDirectory(output);var scenario=Scenario();
   while(true){object next=null;bool moved=false;Exception failure=null;try{moved=scenario.MoveNext();if(moved)next=scenario.Current;}catch(Exception e){failure=e;}
    if(failure!=null){result.status="failed";if(output!=null){File.WriteAllText(Path.Combine(output,"failure.txt"),failure.ToString());Save();Application.Quit(1);}else {if(app)app.Report("Chưa mở được áo thử: "+failure.Message);Debug.LogException(failure);Destroy(gameObject);}yield break;}
    if(!moved)break;yield return next;
   }
   if(!trial){Save();Application.Quit(0);}else Destroy(gameObject);
  }
  IEnumerator Scenario(){
   double until=Time.realtimeSinceStartupAsDouble+40;while(!app||!app.Ready){app=FindObjectOfType<FashionFitApp>();if(Time.realtimeSinceStartupAsDouble>until)throw new Exception("FashionFit startup timeout");yield return null;}
   app.Player.PrepareForVRTrial();result.lowQualityPreset=app.Player.LowQuality;
   if(!trial){yield return new WaitForSecondsRealtime(1);float total=0;int n=0;double end=Time.realtimeSinceStartupAsDouble+2;while(Time.realtimeSinceStartupAsDouble<end){total+=Time.unscaledDeltaTime*1000;n++;yield return null;}result.baselineFrameMs=total/Mathf.Max(1,n);}
   app.Wardrobe.WearDoffableBlouse();blouse=app.Wardrobe.Blouse;Check(blouse&&blouse.Ready,"Blouse missing in exported player");app.Player.FocusTailoring();var ui=app.GetComponent<RunwayUI>();if(ui)ui.ShowDoffingTrial();
   if(trial){app.Report("Áo cài nút đã sẵn sàng · menu VR: ÁO CÀI NÚT / CỞI ÁO · desktop có nút Mở 1–4");yield break;}
   yield return new WaitForSecondsRealtime(2.2f);
   Vector3 p=blouse.Fasteners[0].transform.position+blouse.Fasteners[0].transform.forward*.018f;
   result.nearOnly=!blouse.Fasteners[0].Activate(p+Vector3.forward*.30f);Check(result.nearOnly,"Distant physical button activated");
   Check(blouse.Fasteners[0].Activate(p),"Near button did not open");yield return null;
   p=blouse.Fasteners[0].transform.position+blouse.Fasteners[0].transform.forward*.018f;
   result.reclosed=blouse.Fasteners[0].Activate(p)&&blouse.Solver.Closed[0];Check(result.reclosed,"Button could not be reclosed");yield return null;
   for(int k=0;k<4;k++){p=blouse.Fasteners[k].transform.position+blouse.Fasteners[k].transform.forward*.018f;Check(blouse.Fasteners[k].Activate(p),"Button failed "+k);yield return null;}
   result.opened=blouse.OpenCount==4;Check(result.opened,"Not all buttons opened");
   var target=blouse.GetComponent<RunwayVRGrabTarget>();object left=new object(),right=new object();Vector3 a=Top(blouse.Solver.LeftBank),b=Top(blouse.Solver.RightBank);
   Check(target.TryAcquire(left,a,Quaternion.identity)&&target.TryAcquire(right,b,Quaternion.identity),"Two grabs failed");result.twoHands=target.OwnerCount==2;Check(blouse.BeginDoff(),"Doffing transition failed");
   double begin=Time.realtimeSinceStartupAsDouble;
   while(Time.realtimeSinceStartupAsDouble-begin<12){float t=(float)(Time.realtimeSinceStartupAsDouble-begin)/12f,down=Mathf.SmoothStep(0,1,Mathf.Clamp01(t*2)),outwards=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.35f)/.65f)),up=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.75f)/.25f));
    target.DragOwned(left,a+new Vector3(-.24f*down,-.60f*down+.50f*up,.18f*down+.75f*outwards),Quaternion.identity);
    target.DragOwned(right,b+new Vector3(.24f*down,-.60f*down+.50f*up,.18f*down+.75f*outwards),Quaternion.identity);frameTimes.Add(Time.unscaledDeltaTime*1000);yield return null;
   }
   result.free=blouse.IsFree;result.maxStretch=blouse.Solver.MaxStretch;Check(result.free,"Player garment did not detach");Check(result.maxStretch<1.9f,"Player cloth overstretched");
   target.Release(left,false);yield return null;result.independentRelease=target.OwnerCount==1&&blouse.Solver.GrabCount==1;Check(result.independentRelease,"First release lost the other hand");
   float y=Centre().y;target.Release(right,true);yield return new WaitForSecondsRealtime(2.5f);result.dropM=y-Centre().y;result.gravity=result.dropM>.10f;Check(result.gravity,"Player garment did not fall after last release");
   var material=blouse.GetComponent<MeshRenderer>().sharedMaterial;app.SetLook(2);yield return null;result.persistent=blouse&&blouse.GetComponent<MeshRenderer>().sharedMaterial==material;Check(result.persistent,"Changing look destroyed dropped garment");
   blouse.ResetOnBody();yield return new WaitForSecondsRealtime(.5f);result.reset=app.Wardrobe.Blouse==blouse&&blouse.OpenCount==0;Check(result.reset,"Reset did not adopt the same garment instance");
   yield return new WaitForSecondsRealtime(2.1f);float settledTotal=0;int settledCount=0;double settledEnd=Time.realtimeSinceStartupAsDouble+2;while(Time.realtimeSinceStartupAsDouble<settledEnd){settledTotal+=Time.unscaledDeltaTime*1000;settledCount++;yield return null;}result.settledFrameMs=settledTotal/Mathf.Max(1,settledCount);
   Check(result.exceptions==0,"Unity errors during exported player test");Capture();
  }
  Vector3 Top(int[] bank){int best=bank[0];foreach(int i in bank)if(blouse.Solver.Rest[i].y>blouse.Solver.Rest[best].y)best=i;return blouse.Solver.Positions[best];}
  Vector3 Centre(){Vector3 p=Vector3.zero;foreach(var q in blouse.Solver.Positions)p+=q;return p/blouse.Solver.Positions.Length;}
  void Save(){if(frameTimes.Count>0){float total=0;foreach(float v in frameTimes)total+=v;result.meanFrameMs=total/frameTimes.Count;frameTimes.Sort();result.p95FrameMs=frameTimes[Mathf.Min(frameTimes.Count-1,(int)(frameTimes.Count*.95f))];}File.WriteAllText(Path.Combine(output,"dof-player.json"),JsonUtility.ToJson(result,true));Debug.Log("DOFFABLE_PLAYER "+JsonUtility.ToJson(result));}
  void Capture(){var c=app.Player.View;var rt=new RenderTexture(1200,900,24);var tex=new Texture2D(1200,900,TextureFormat.RGB24,false);var old=RenderTexture.active;try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1200,900),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(output,"DoffPlayer.png"),tex.EncodeToPNG());}finally{c.targetTexture=null;RenderTexture.active=old;Destroy(tex);Destroy(rt);}}
  void OnDestroy(){Application.logMessageReceived-=Log;}
 }
}
