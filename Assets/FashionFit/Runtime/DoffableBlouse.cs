using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace FashionFit
{
    // A persistent, world-space garment. No Rigidbody or old body skinning drives its vertices.
    public sealed class DoffableBlouse:MonoBehaviour
    {
        // Cloth runs at 60 Hz independently of the headset render rate.
        public const float SimulationStep=1f/60;
        public enum WearState { Worn, Doffing, Free }
        public static readonly List<DoffableBlouse> Live=new List<DoffableBlouse>();
        public WearState State {get;private set;}=WearState.Worn;
        public DoffableClothSolver Solver {get;private set;}
        readonly System.Diagnostics.Stopwatch simulationWatch=new System.Diagnostics.Stopwatch();
        public float SimulationMilliseconds {get;private set;}
        public bool Ready=>Solver!=null;
        public bool IsFree=>State==WearState.Free;
        public bool IsGrabbed=>Solver!=null&&Solver.GrabCount>0;
        public string Hint=>State==WearState.Worn?"Grip gần cổ/vai áo · Trigger sát nút để mở":State==WearState.Doffing?"Kéo nhẹ khỏi vai / tay · va chạm vẫn bật":"Giữ hai tay để mang · thả để rơi";
        public Mesh DisplayMesh=>mesh;
        public int OpenCount=>Solver==null?0:Solver.OpenCount;
        public GarmentFastener[] Fasteners {get;private set;}
        FashionFitApp app;RunwayAvatar avatar;DoffableMeshAsset.Data data;
        Mesh mesh;Material fabric,metal;readonly List<Texture> ownedMaps=new List<Texture>();BoxCollider grabVolume;
        Vector3[] anchors,normals;Matrix4x4 bodyToLocal,bodyToWorld;
        DoffableBodySdf bodySdf;
        sealed class RoomBox {public Vector3 min,max;public Matrix4x4 local,world;public Vector3 centre,half;}
        readonly List<RoomBox> room=new List<RoomBox>();Vector3 bodyMin,bodyMax;
        float releaseTime,accumulator,quietTime,idleTime;bool sleeping;
        Vector3 centre;float lastMotion;
        public void Initialize(FashionFitApp owner,Material source)
        {
            app=owner;avatar=app.Model;
            if(avatar.IsOriginal)throw new InvalidOperationException("Áo cài nút chỉ kiểm tra trên mannequin nữ kỹ thuật hiện tại");
            fabric=new Material(source){name="Doffable ivory cotton"};fabric.SetColor("_Color",new Color(.83f,.77f,.65f));fabric.SetFloat("_Glossiness",.12f);
            foreach(string property in new[]{"_BaseMap","_NormalMap","_RoughnessMap"}){var texture=fabric.GetTexture(property);if(texture){var copy=Instantiate(texture);copy.name=texture.name+" - blouse owned";fabric.SetTexture(property,copy);ownedMaps.Add(copy);}}
            data=DoffableMeshAsset.Load();
            transform.SetParent(app.transform,false);transform.position=Vector3.zero;transform.rotation=Quaternion.identity;transform.localScale=Vector3.one;
            bodySdf=new DoffableBodySdf(avatar);anchors=new Vector3[data.rest.Length];
            UpdateBody();UpdateAnchors();for(int i=0;i<anchors.Length;i++)anchors[i]=Contact(anchors[i]);
            // Persist the collision-fitted rest pose. Never pull particles back to pre-fit points inside the body.
            for(int i=0;i<data.rest.Length;i++)data.rest[i]=avatar.transform.InverseTransformPoint(anchors[i]);
            Solver=new DoffableClothSolver(anchors,data.triangles,data.rest,data.left,data.right);
            mesh=new Mesh{name="Open blouse - independent front banks",indexFormat=IndexFormat.UInt32};mesh.MarkDynamic();mesh.vertices=Solver.Positions;mesh.triangles=data.triangles;mesh.uv=data.uv;
            gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;gameObject.AddComponent<MeshRenderer>().sharedMaterial=fabric;
            grabVolume=gameObject.AddComponent<BoxCollider>();grabVolume.isTrigger=true;gameObject.layer=9;gameObject.AddComponent<RunwayVRGrabTarget>().ConfigureDoffable(this);
            metal=new Material(Shader.Find("Standard")){name="Blouse button brass",color=new Color(.65f,.48f,.23f)};metal.SetFloat("_Metallic",.65f);metal.SetFloat("_Glossiness",.45f);
            Fasteners=new GarmentFastener[4];
            for(int i=0;i<4;i++){
                var button=GameObject.CreatePrimitive(PrimitiveType.Sphere);button.name="Functional blouse button "+(i+1);button.transform.SetParent(transform,false);button.transform.localScale=new Vector3(.012f,.012f,.005f);button.layer=9;
                var collider=button.GetComponent<Collider>();collider.enabled=false;if(Application.isPlaying)Destroy(collider);else DestroyImmediate(collider);
                Fasteners[i]=button.AddComponent<GarmentFastener>();Fasteners[i].Initialize(this,i,metal);
            }
            foreach(var box in app.GetComponentsInChildren<BoxCollider>())if(box.gameObject.layer==10&&!box.isTrigger){var broad=box.bounds;broad.Expand(.02f);room.Add(new RoomBox{min=broad.min,max=broad.max,local=box.transform.worldToLocalMatrix,world=box.transform.localToWorldMatrix,centre=box.center,half=box.size*.5f+Vector3.one*.005f});}
            Live.Add(this);avatar.SetGarmentCoverage(0);Publish();
        }
        void LateUpdate(){StepSimulation(Time.deltaTime);}
        public void StepSimulation(float dt)
        {
            SimulationMilliseconds=0;if(!Ready||dt<=0)return;
            if(State!=WearState.Free){UpdateBody();UpdateAnchors();}else if(!sleeping)UpdateBody();
            if(sleeping&&!IsGrabbed)return;
            simulationWatch.Restart();accumulator=Mathf.Min(accumulator+dt,.05f);
            while(accumulator>=SimulationStep){float step=SimulationStep;accumulator-=step;
                if(State==WearState.Doffing){releaseTime+=step;Solver.Attachment=1-Mathf.SmoothStep(0,1,releaseTime/.65f);if(releaseTime>=.65f)Solver.Supported=false;}
                Solver.Step(step,anchors,Contact,p=>p+BodyCorrection(p));
                if(State==WearState.Doffing&&!Solver.Supported){
                    int touching=0;for(int i=0;i<Solver.Positions.Length;i++)if(BodyCorrection(Solver.Positions[i]).magnitude>.001f)touching++;
                    if(Vector3.Distance(Centre(),AnchorCentre())>.32f&&touching==0){State=WearState.Free;app.Report("Áo đã rời mannequin · thả Grip để áo rơi, vẫn giữ texture");}
                }
            }
            lastMotion=Solver.MaxSpeed;idleTime=!IsGrabbed?idleTime+dt:0;quietTime=!IsGrabbed&&Solver.RmsSpeed<.035f&&lastMotion<.30f?quietTime+dt:0;
            if(quietTime>1.5f||(State==WearState.Worn&&OpenCount==0&&idleTime>2))sleeping=true;Publish();simulationWatch.Stop();SimulationMilliseconds=(float)simulationWatch.Elapsed.TotalMilliseconds;
        }
        Vector3 Centre(){Vector3 p=Vector3.zero;foreach(var v in Solver.Positions)p+=v;return p/Solver.Positions.Length;}
        Vector3 AnchorCentre(){Vector3 p=Vector3.zero;foreach(var v in anchors)p+=v;return p/anchors.Length;}
        public bool BeginDoff()
        {
            if(State!=WearState.Worn)return false;
            if(OpenCount!=4){app.Report("Còn "+(4-OpenCount)+" nút đang cài · đưa tay sát nút và bóp Trigger");return false;}
            State=WearState.Doffing;releaseTime=0;sleeping=false;app.Report("Đang cởi · Grip nẹp hai bên, kéo nhẹ khỏi vai và tay");return true;
        }
        public bool ToggleFastener(int index)
        {
            if(!Ready||index<0||index>=4)return false;
            
            if(!Solver.Toggle(index)){app.Report("Đưa hai mép khuy lại gần dưới 2,5 cm trước khi cài");return false;}
            sleeping=false;quietTime=idleTime=0;app.Report("Nút "+(index+1)+(Solver.Closed[index]?": đã cài":": đã mở")+" · "+OpenCount+"/4 nút mở");Publish();if(app.Player.VRMenu)app.Player.VRMenu.Refresh();return true;
        }
        public void ResetOnBody()
        {
            if(!Ready)return;app.Player.EndActiveGrab();if(app.Wardrobe.Blouse!=this)app.Wardrobe.AdoptDoffableBlouse(this);app.StandForFitting();UpdateBody();UpdateAnchors();for(int i=0;i<anchors.Length;i++)anchors[i]=Contact(anchors[i]);
            Solver.Reset(anchors);State=WearState.Worn;releaseTime=quietTime=accumulator=0;sleeping=false;avatar.SetGarmentCoverage(0);Publish();app.Report("Đã đặt lại áo lên mẫu và cài 4 nút · đây là thao tác đặt lại bản thử");
        }
        public bool Nearest(Vector3 p,float reach,out Vector3 nearest){nearest=p;return Ready&&Solver.Nearest(p,reach,out nearest);}
        public bool BeginVRGrab(int slot,Vector3 p)
        {
            if(!Ready||BodyCorrection(p).magnitude>.025f||!Solver.BeginGrab(slot,p))return false;
            sleeping=false;quietTime=idleTime=0;return true;
        }
        public void MoveVRGrab(int slot,Vector3 p)
        {
            if(!Ready)return;Solver.MoveGrab(slot,p);
            if(State==WearState.Worn&&OpenCount==4){
                Vector3 delta=Solver.GrabPoint(slot);if(Solver.Nearest(p,.085f,out var near)&&Vector3.Distance(p,near)<.02f)return;
                if(Vector3.Distance(p,delta)>.055f)BeginDoff();
            }
        }
        public void EndVRGrab(int slot){if(!Ready)return;Solver.EndGrab(slot);sleeping=false;quietTime=idleTime=0;}
        public bool CanTouchFastener(Vector3 hand,int i)
        {
            if(!Ready||i<0||i>=4)return false;Vector3 b=Fasteners[i].transform.position;
            if(Vector3.Distance(hand,b)>.035f||Physics.CheckSphere(hand,.003f,(1<<8)|(1<<10),QueryTriggerInteraction.Ignore))return false;
            for(int n=0;n<=4;n++)if(BodyCorrection(Vector3.Lerp(hand,b,n/4f)).magnitude>.004f)return false;
            Vector3 delta=b-hand;if(delta.magnitude>.003f&&Physics.Raycast(hand,delta.normalized,delta.magnitude,(1<<8)|(1<<10),QueryTriggerInteraction.Ignore))return false;
            return true;
        }
        void UpdateAnchors()
        {
            Matrix4x4 world=avatar.transform.localToWorldMatrix;
            for(int i=0;i<anchors.Length;i++)anchors[i]=world.MultiplyPoint3x4(data.rest[i]);
        }

        void UpdateBody(){bodyToWorld=avatar.transform.localToWorldMatrix;bodyToLocal=bodyToWorld.inverse;bodySdf.Update(bodyToWorld);}
        Vector3 BodyCorrection(Vector3 p)=>bodySdf.Correction(p);
        Vector3 Contact(Vector3 p)
        {
            p+=BodyCorrection(p);
            // One-way conservative outer layer for the stationary procedural skirt.
            // Arbitrary imported garments are not silently treated as this proxy.
            if(app.Wardrobe.IsSample){Vector3 q=bodyToLocal.MultiplyPoint3x4(p);var preset=app.Look>=1&&app.Look<=6?(DressPreset)(app.Look-1):DressPreset.IvoryShirtSet;
                float hem=BodyFitProfile.HemY(preset);
                if(q.y>=hem&&q.y<=BodyFitProfile.WaistY+.006f){float centre=BodyFitProfile.CentreZ(Mathf.Max(q.y,BodyFitProfile.HipY)),angle=Mathf.Atan2(q.z-centre,q.x),t=Mathf.InverseLerp(BodyFitProfile.WaistY,hem,q.y);
                    Vector3 skirt=BodyFitProfile.Skirt(preset,t,angle);float r=new Vector2(skirt.x,skirt.z-centre).magnitude+.022f;
                    if(new Vector2(q.x,q.z-centre).sqrMagnitude<r*r){q.x=Mathf.Cos(angle)*r;q.z=centre+Mathf.Sin(angle)*r;p=bodyToWorld.MultiplyPoint3x4(q);p+=BodyCorrection(p);}
                }
            }
            p.y=Mathf.Max(.008f,p.y);
            foreach(var box in room){if(p.x<box.min.x||p.x>box.max.x||p.y<box.min.y||p.y>box.max.y||p.z<box.min.z||p.z>box.max.z)continue;
                Vector3 local=box.local.MultiplyPoint3x4(p)-box.centre,half=box.half;
                if(Mathf.Abs(local.x)>=half.x||Mathf.Abs(local.y)>=half.y||Mathf.Abs(local.z)>=half.z)continue;
                float dx=half.x-Mathf.Abs(local.x),dy=half.y-Mathf.Abs(local.y),dz=half.z-Mathf.Abs(local.z);
                if(dx<dy&&dx<dz)local.x=(local.x>=0?1:-1)*half.x;else if(dy<dz)local.y=(local.y>=0?1:-1)*half.y;else local.z=(local.z>=0?1:-1)*half.z;
                p=box.world.MultiplyPoint3x4(local+box.centre);
            }
            return p;
        }
        public void Publish()
        {
            if(!Ready)return;mesh.vertices=Solver.Positions;mesh.RecalculateNormals();mesh.RecalculateBounds();mesh.RecalculateTangents();normals=mesh.normals;
            Bounds b=mesh.bounds;grabVolume.center=b.center;grabVolume.size=b.size+Vector3.one*.025f;centre=b.center;
            for(int i=0;i<4;i++){
                int id=Solver.ButtonA(i);Vector3 normal=normals[id];if(normal.sqrMagnitude<.5f)normal=Vector3.forward;
                Fasteners[i].transform.SetPositionAndRotation(Solver.Positions[id]+normal*.006f,Quaternion.LookRotation(normal));Fasteners[i].Refresh();
            }
        }
        void OnDestroy()
        {
            Live.Remove(this);if(Fasteners!=null)foreach(var f in Fasteners)if(f)f.Unregister();
            foreach(var map in ownedMaps)if(map){if(Application.isPlaying)Destroy(map);else DestroyImmediate(map);}
            foreach(UnityEngine.Object o in new UnityEngine.Object[]{mesh,fabric,metal})if(o){if(Application.isPlaying)Destroy(o);else DestroyImmediate(o);}
        }
    }
}
