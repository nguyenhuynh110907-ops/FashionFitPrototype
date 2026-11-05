#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace FashionFit.Editor
{
    public static class DoffableVerification
    {
        [Serializable] sealed class Result {
            public string status="passed",scope="Injected live Touch states and actual triangle cloth; not physical HMD testing";
            public int checks,exceptions,particles,triangles,exportedPoses;public bool nearOnly,realOpen,twoHands,independentRelease,free,gravity,persistent,reset;
            public float closedGap,openGap,droppedM,maxStretch,meanStepMs,peakStepMs;public string state;
        }
        static Result result;static FashionFitApp app;static RunwayVRHands hands;static DoffableBlouse blouse;static string folder;
        static void Check(bool b,string s){result.checks++;if(!b)throw new InvalidOperationException(s);}
        static void Log(string m,string s,LogType t){if(t==LogType.Exception||t==LogType.Error)result.exceptions++;}
        static void Mode(bool b)=>typeof(RunwayPlayer).GetProperty("InVR").GetSetMethod(true).Invoke(app.Player,new object[]{b});
        static void Tick(int h,Vector3 p,float grip,float trigger=0,bool tracked=true)=>hands.Step(h,tracked,p,Quaternion.Euler(0,180,0),grip,trigger,1f/60);
        static int stepCount;static double totalMs;
        static void Step(int n){for(int i=0;i<n;i++){var watch=System.Diagnostics.Stopwatch.StartNew();blouse.StepSimulation(1f/60);watch.Stop();totalMs+=watch.Elapsed.TotalMilliseconds;stepCount++;result.peakStepMs=Mathf.Max(result.peakStepMs,(float)watch.Elapsed.TotalMilliseconds);Physics.SyncTransforms();}}
        static Vector3 Top(int[] bank){int best=bank[0];foreach(int i in bank)if(blouse.Solver.Rest[i].y>blouse.Solver.Rest[best].y)best=i;return blouse.Solver.Positions[best];}
        static Vector3 Centre(){Vector3 p=Vector3.zero;foreach(var v in blouse.Solver.Positions)p+=v;return p/blouse.Solver.Positions.Length;}
        public static void RunBatch()
        {
            folder=Path.GetFullPath("Verification/GarmentDoffing/Trial");Directory.CreateDirectory(folder);Directory.CreateDirectory(folder+"/Contact");
            if(File.Exists(folder+"/failure.txt"))File.Delete(folder+"/failure.txt");
            result=new Result();GameObject root=null;var oldMode=Physics.simulationMode;Application.logMessageReceived+=Log;
            try{
                Physics.simulationMode=SimulationMode.Script;root=new GameObject("Doffable verification");app=root.AddComponent<FashionFitApp>();typeof(FashionFitApp).GetMethod("Start",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(app,null);Check(app.Ready,app.FatalError);
                app.Wardrobe.WearDoffableBlouse();blouse=app.Wardrobe.Blouse;Check(blouse&&blouse.Ready,"No blouse created");
                result.particles=blouse.Solver.Positions.Length;result.triangles=blouse.Solver.Triangles.Length/3;Check(result.particles<3000,"Unbounded simulation mesh");
                Check(!blouse.BeginDoff(),"Closed garment may not detach");Check(!blouse.BeginVRGrab(0,new Vector3(5,5,5)),"Distant cloth grip allowed");app.StartPresentationAt(.5f);Check(!app.Playing&&app.FittingPose,"Pilot pose safety gate failed");
                Step(30);result.closedGap=blouse.Solver.ButtonGap(1);Export("Rest");Export("Closed");Capture("Closed.png");Debug.Log("DOFF_STAGE closed "+result.particles);
                app.Player.EnsureHands();hands=app.Player.Hands;Mode(true);
                Vector3 p=blouse.Fasteners[0].transform.position+blouse.Fasteners[0].transform.forward*.018f;
                Tick(0,p+Vector3.forward*.3f,0,0);Tick(0,p+Vector3.forward*.3f,0,1);result.nearOnly=blouse.OpenCount==0;Check(result.nearOnly,"Distant Trigger unbuttons clothing");
                for(int k=0;k<4;k++){
                    p=blouse.Fasteners[k].transform.position+blouse.Fasteners[k].transform.forward*.018f;
                    Check(GarmentFastener.FindNear(p)==blouse.Fasteners[k],"Fastener cannot be reached "+k);
                    Tick(0,p,0,0);Tick(0,p,0,1);Check(!blouse.Solver.Closed[k],"Near Trigger did not open button "+k);
                    Tick(0,p,0,1);Check(!blouse.Solver.Closed[k],"Held Trigger toggled twice");
                }
                Check(!app.Playing,"Pilot should be stationary");
                var target=blouse.GetComponent<RunwayVRGrabTarget>();
                Vector3 a=Top(blouse.Solver.LeftBank),b=Top(blouse.Solver.RightBank);
                Tick(0,a,0);Tick(1,b,0);Tick(0,a,1);Tick(1,b,1);result.twoHands=target.OwnerCount==2&&blouse.Solver.GrabCount==2;Check(result.twoHands,"Two hands cannot grip the new cloth");
                Check(blouse.BeginDoff(),"Open blouse cannot begin doffing");
                var legacyTime=typeof(RunwayWardrobe).GetField("materialTime",BindingFlags.Instance|BindingFlags.NonPublic);double oldClock=(double)legacyTime.GetValue(app.Wardrobe);app.AdvanceSimulation(1f/60);Check((double)legacyTime.GetValue(app.Wardrobe)==oldClock,"Blouse grip woke unrelated legacy skirt solver");
                for(int i=0;i<720;i++){
                    float t=i/719f;float down=Mathf.SmoothStep(0,1,Mathf.Clamp01(t*2));float outwards=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.35f)/.65f));
                    Tick(0,a+new Vector3(-.24f*down,-.60f*down+.50f*Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.75f)/.25f)),.18f*down+.75f*outwards),1);
                    Tick(1,b+new Vector3(.24f*down,-.60f*down+.50f*Mathf.SmoothStep(0,1,Mathf.Clamp01((t-.75f)/.25f)),.18f*down+.75f*outwards),1);
                    Step(1);if(i%90==0){Check(target.OwnerCount==2,"Lost hand ownership while pulling");Export("Pull_"+i.ToString("D3"));Debug.Log("DOFF_FRAME "+i+" centre="+Centre()+" grip="+blouse.Solver.GrabPoint(0));}
                }
                result.openGap=blouse.Solver.ButtonGap(1);result.realOpen=result.openGap>result.closedGap+.04f;Check(result.realOpen,"Unbuttoning did not physically separate front banks");
                result.free=blouse.IsFree;result.state=blouse.State.ToString();result.maxStretch=blouse.Solver.MaxStretch;
                Capture("Held.png");Export("Held");Debug.Log("DOFF_STAGE held "+JsonUtility.ToJson(result));
                Check(result.maxStretch<1.9f,"Blouse overstretched: "+result.maxStretch);
                Check(result.free,"Garment did not clear mannequin; state="+blouse.State+" centre="+Centre());
                Vector3 before=blouse.Solver.GrabPoint(1);hands.Release(0,false);Step(1);result.independentRelease=target.OwnerCount==1&&blouse.Solver.GrabCount==1&&Vector3.Distance(before,blouse.Solver.GrabPoint(1))<.04f;Check(result.independentRelease,"One-hand handover drops or jumps blouse");
                float y=Centre().y;hands.Release(1,true);Check(blouse.Solver.GrabCount==0,"Tracking loss left a cloth patch held");Step(180);result.droppedM=y-Centre().y;result.gravity=result.droppedM>.15f;Check(result.gravity,"Released blouse did not fall independently of mannequin");Export("Floor");Capture("Floor.png");
                var fabric=blouse.GetComponent<MeshRenderer>().sharedMaterial;app.SetLook(2);result.persistent=blouse&&blouse.Ready&&blouse.GetComponent<MeshRenderer>().sharedMaterial==fabric;Check(result.persistent,"Changing outfit deletes removed cloth/material");
                blouse.ResetOnBody();result.reset=blouse.State==DoffableBlouse.WearState.Worn&&blouse.OpenCount==0;Check(result.reset,"Explicit reset did not restore blouse");
                var skirt=app.GetComponentInChildren<FittingCloth>();var point=skirt.Surface.transform.TransformPoint(skirt.Surface.GetComponent<MeshFilter>().sharedMesh.vertices[0]);
                Check(skirt.BeginVRGrab(0,point),"Legacy skirt cannot be grabbed with blouse present");oldClock=(double)legacyTime.GetValue(app.Wardrobe);app.AdvanceSimulation(1f/60);Check((double)legacyTime.GetValue(app.Wardrobe)>oldClock,"Real skirt grip did not wake its solver");skirt.EndVRGrab(0);oldClock=(double)legacyTime.GetValue(app.Wardrobe);app.AdvanceSimulation(1f/60);Check((double)legacyTime.GetValue(app.Wardrobe)>oldClock,"Skirt stopped settling immediately on release");
                Check(result.exceptions==0,"Runtime errors");result.meanStepMs=(float)(totalMs/stepCount);File.WriteAllText(folder+"/verification.json",JsonUtility.ToJson(result,true));Debug.Log("DOFFABLE_OK "+JsonUtility.ToJson(result));
                Mode(false);Cleanup(root);root=null;Physics.simulationMode=oldMode;EditorApplication.Exit(0);
            }catch(Exception e){result.status="failed";result.meanStepMs=stepCount>0?(float)(totalMs/stepCount):0;File.WriteAllText(folder+"/verification.json",JsonUtility.ToJson(result,true));File.WriteAllText(folder+"/failure.txt",e.ToString());Debug.LogException(e);Physics.simulationMode=oldMode;EditorApplication.Exit(1);}
            finally{Application.logMessageReceived-=Log;Physics.simulationMode=oldMode;if(root)Cleanup(root);}
        }
        static void Cleanup(GameObject root){for(int i=DoffableBlouse.Live.Count-1;i>=0;i--)if(DoffableBlouse.Live[i])UnityEngine.Object.DestroyImmediate(DoffableBlouse.Live[i].gameObject);if(root)UnityEngine.Object.DestroyImmediate(root);}
        static void Export(string name)
        {
            var body=new Mesh();var original=app.Model.Renderer.sharedMesh;app.Model.Renderer.sharedMesh=app.Model.BodyMesh;app.Model.Renderer.BakeMesh(body);app.Model.Renderer.sharedMesh=original;
            Vector3[] p=body.vertices;for(int i=0;i<p.Length;i++)p[i]=app.Model.Renderer.transform.TransformPoint(p[i]);
            using(var w=new BinaryWriter(File.Create(folder+"/Contact/"+name+".bin"))){w.Write(0x53434631);Write(w,p,body.triangles);Write(w,name=="Rest"?blouse.Solver.Rest:blouse.Solver.Positions,blouse.Solver.Triangles);}UnityEngine.Object.DestroyImmediate(body);result.exportedPoses++;
        }
        static void Write(BinaryWriter w,Vector3[] p,int[] t){w.Write(p.Length);foreach(var v in p){w.Write(v.x);w.Write(v.y);w.Write(v.z);}w.Write(t.Length);foreach(int i in t)w.Write(i);}
        static void Capture(string name)
        {
            bool previousMode=app.Player.InVR;Mode(false);var c=app.Player.View;c.rect=new Rect(0,0,1,1);c.transform.position=new Vector3(.8f,1.60f,2.15f);c.transform.LookAt(new Vector3(0,1.15f,.05f));
            var rt=new RenderTexture(1200,1000,24);var tex=new Texture2D(1200,1000,TextureFormat.RGB24,false);var old=RenderTexture.active;
            try{c.targetTexture=rt;c.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1200,1000),0,0);tex.Apply();File.WriteAllBytes(folder+"/"+name,tex.EncodeToPNG());}
            finally{c.targetTexture=null;RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(tex);UnityEngine.Object.DestroyImmediate(rt);}Mode(previousMode);
        }
    }
}
#endif
