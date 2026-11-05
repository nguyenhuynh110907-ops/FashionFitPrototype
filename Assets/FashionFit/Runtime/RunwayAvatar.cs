using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
namespace FashionFit
{
    public sealed class RunwayAvatar:MonoBehaviour
    {
        [Serializable] class RigNode{public string name;public int parent;public float[] position,rotation,scale;}
        [Serializable] class Rig{public RigNode[] nodes;public int[] bones;public int rootBone;}
        public Mesh BodyMesh{get;private set;}
        public SkinnedMeshRenderer Renderer{get;private set;}
        public Transform[] Bones{get;private set;}
        public Transform RootBone{get;private set;}
        public Material Original{get;private set;}
        public Vector3[] BodyVertices{get;private set;}
        public Matrix4x4[] GarmentBindposes{get;private set;}
        public bool IsOriginal{get;private set;}
        public bool HoldUpperBodyForFitting{get;set;}
        public bool CanFit{get;private set;}
        public string FitIssue{get;private set;}
        readonly Dictionary<string,Transform> joints=new Dictionary<string,Transform>();
        readonly Dictionary<Transform,Quaternion> rest=new Dictionary<Transform,Quaternion>();
        readonly Dictionary<Transform,Vector3> positions=new Dictionary<Transform,Vector3>();
        Animator animator;int walkHash;Transform hips;Vector3 hipRest;bool ownsBody;
        RunwayBodyCoverage coverage;
        public int HiddenBodyTriangles { get { return coverage!=null?coverage.HiddenTriangles:0; } }
        public void SetGarmentCoverage(int look) { if(coverage==null)coverage=new RunwayBodyCoverage(this);if(look<=0)coverage.Clear();else coverage.Wear((DressPreset)(look-1)); }
        public void SetCoverageEnabled(bool enabled) { if(coverage!=null)coverage.SetEnabled(enabled); }
        public void Load(GameObject prefab,RuntimeAnimatorController controller,string state)
        {
            if(prefab!=null){LoadOriginal(prefab,controller,state);return;}
            LoadTechnical("Fitting/female", "Technical female stand-in — not supplied character");
        }
        public void LoadTechnical(string resourceStem,string displayName)
        {
            var rigAsset=Resources.Load<TextAsset>(resourceStem+"-rig");var meshAsset=Resources.Load<TextAsset>(resourceStem+"-mesh");
            if(rigAsset==null||meshAsset==null)throw new FileNotFoundException("Thiếu dữ liệu ma-nơ-canh nữ. Giải nén toàn bộ dự án.");
            var rig=JsonUtility.FromJson<Rig>(rigAsset.text);var nodes=new Transform[rig.nodes.Length];
            for(int i=0;i<nodes.Length;i++)nodes[i]=new GameObject(rig.nodes[i].name).transform;
            for(int i=0;i<nodes.Length;i++){var n=rig.nodes[i];var tr=nodes[i];tr.SetParent(n.parent<0?transform:nodes[n.parent],false);tr.localPosition=V(n.position);tr.localScale=V(n.scale);tr.localRotation=new Quaternion(n.rotation[0],n.rotation[1],n.rotation[2],n.rotation[3]);Register(n.name,tr);}
            Bones=Array.ConvertAll(rig.bones,i=>nodes[i]);RootBone=nodes[rig.rootBone];hips=RootBone;hipRest=hips.localPosition;
            using(var r=new BinaryReader(new MemoryStream(meshAsset.bytes))){
                if(r.ReadInt32()!=0x46525633)throw new InvalidDataException("Sai định dạng cơ thể.");
                int n=r.ReadInt32();var v=new Vector3[n];var normal=new Vector3[n];var uv=new Vector2[n];var weights=new BoneWeight[n];
                for(int i=0;i<n;i++)v[i]=ReadV(r);for(int i=0;i<n;i++)normal[i]=ReadV(r);for(int i=0;i<n;i++)uv[i]=new Vector2(r.ReadSingle(),r.ReadSingle());
                for(int i=0;i<n;i++)weights[i]=new BoneWeight{weight0=r.ReadSingle(),weight1=r.ReadSingle(),weight2=r.ReadSingle(),weight3=r.ReadSingle()};
                for(int i=0;i<n;i++){weights[i].boneIndex0=r.ReadInt32();weights[i].boneIndex1=r.ReadInt32();weights[i].boneIndex2=r.ReadInt32();weights[i].boneIndex3=r.ReadInt32();}
                var bind=new Matrix4x4[r.ReadInt32()];for(int i=0;i<bind.Length;i++)for(int a=0;a<4;a++)for(int b=0;b<4;b++)bind[i][a,b]=r.ReadSingle();
                BodyMesh=new Mesh{name=displayName,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32,vertices=v,normals=normal,uv=uv,boneWeights=weights,bindposes=bind};
                BodyMesh.subMeshCount=r.ReadInt32();for(int s=0;s<BodyMesh.subMeshCount;s++){var tri=new int[r.ReadInt32()];for(int i=0;i<tri.Length;i++)tri[i]=r.ReadInt32();BodyMesh.SetTriangles(tri,s);}BodyMesh.RecalculateBounds();ownsBody=true;BodyVertices=v;GarmentBindposes=bind;
            }
            Renderer=gameObject.AddComponent<SkinnedMeshRenderer>();Renderer.sharedMesh=BodyMesh;Renderer.bones=Bones;Renderer.rootBone=RootBone;Renderer.localBounds=new Bounds(new Vector3(0,.9f,0),new Vector3(1.5f,2,1.5f));Renderer.quality=SkinQuality.Bone4;
            SkinnedBounds.SetFromModel(Renderer,transform,BodyMesh.bounds,.18f);
            Original=RunwayStage.Matte("Neutral mannequin clay",new Color(.60f,.57f,.54f),.24f);Renderer.sharedMaterial=Original;CanFit=true;
        }
        void Register(string name,Transform tr){joints[name.Replace("mixamorig:","")]=tr;rest[tr]=tr.localRotation;positions[tr]=tr.localPosition;}
        void LoadOriginal(GameObject prefab,RuntimeAnimatorController controller,string state)
        {
            var instance=Instantiate(prefab,transform,false);instance.name="Original female avatar";
            var skins=instance.GetComponentsInChildren<SkinnedMeshRenderer>();
            if(skins.Length==0)throw new InvalidDataException("Nhân vật nữ cần SkinnedMeshRenderer và bộ xương; ảnh PNG không phải mô hình 3D.");
            Renderer=skins[0];foreach(var s in skins)if(s.sharedMesh!=null&&(Renderer.sharedMesh==null||s.sharedMesh.vertexCount>Renderer.sharedMesh.vertexCount))Renderer=s;
            BodyMesh=Renderer.sharedMesh;if(BodyMesh==null||!BodyMesh.isReadable)throw new InvalidDataException("Bật Read/Write trong Import Settings của mesh cơ thể.");
            Bones=Renderer.bones;RootBone=Renderer.rootBone;
            if(Bones.Length==0||BodyMesh.boneWeights.Length!=BodyMesh.vertexCount)throw new InvalidDataException("Cơ thể chưa có trọng số xương hợp lệ.");
            foreach(var tr in instance.GetComponentsInChildren<Transform>())Register(tr.name,tr);
            animator=instance.GetComponentInChildren<Animator>();
            if(animator==null||!animator.isHuman)throw new InvalidDataException("Đặt Rig = Humanoid và kiểm tra Avatar trước khi gắn nhân vật.");
            if(controller!=null)animator.runtimeAnimatorController=controller;
            animator.Rebind();animator.Update(0);animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.speed=0;
            walkHash=Animator.StringToHash(state);if(animator.runtimeAnimatorController==null||!animator.HasState(0,walkHash))throw new InvalidDataException("Cần Animator Controller có trạng thái "+state+" để đi tại chỗ.");
            hips=animator.GetBoneTransform(HumanBodyBones.Hips);hipRest=hips.localPosition;if(RootBone==null)RootBone=hips;
            Matrix4x4 m=transform.worldToLocalMatrix*Renderer.transform.localToWorldMatrix;
            BodyVertices=BodyMesh.vertices;for(int i=0;i<BodyVertices.Length;i++)BodyVertices[i]=m.MultiplyPoint3x4(BodyVertices[i]);
            var bounds=new Bounds(BodyVertices[0],Vector3.zero);foreach(var p in BodyVertices)bounds.Encapsulate(p);
            if(bounds.size.y<1.35f||bounds.size.y>2.2f||Mathf.Abs(bounds.min.y)>.06f)throw new InvalidDataException("Chuẩn hóa nhân vật: cao 1,35–2,2 m, chân ở Y=0 và trục Y hướng lên.");
            GarmentBindposes=BodyMesh.bindposes;for(int i=0;i<GarmentBindposes.Length;i++)GarmentBindposes[i]=GarmentBindposes[i]*m.inverse;
            IsOriginal=true;CanFit=false;FitIssue="Nhân vật gốc cần hiệu chỉnh mốc cơ thể trước khi căn đồ; không dùng số đo ma-nơ-canh tạm.";
        }
        public void Animate(float time,float walking,float speed)
        {
            if(IsOriginal){
                if(walking>0){animator.Play(walkHash,0,time/FashionFitApp.CycleSeconds);animator.Update(0);var p=hips.localPosition;p.x=hipRest.x;p.z=hipRest.z;hips.localPosition=p;}
                else foreach(var pair in rest){pair.Key.localRotation=pair.Value;pair.Key.localPosition=positions[pair.Key];}
                return;
            }
            foreach(var pair in rest){pair.Key.localRotation=pair.Value;pair.Key.localPosition=positions[pair.Key];}
            float phase=time/FashionFitApp.CycleSeconds;
            float s=Mathf.Sin(phase*Mathf.PI*2)*walking;
            // Leave room for knee flexion and keep the stance ankle on its
            // authored floor height using two-bone IK in model space.
            Vector3 leftRest=Joint("LeftFoot"),rightRest=Joint("RightFoot");
            hips.localPosition=hipRest+Vector3.up*((-.018f+RunwayGait.PelvisLift(phase))*walking);
            PoseLeg("LeftUpLeg","LeftLeg","LeftFoot",leftRest,phase,walking);
            PoseLeg("RightUpLeg","RightLeg","RightFoot",rightRest,phase+.5f,walking);
            Swing("Head",Vector3.up,1.2f*Mathf.Sin(phase*Mathf.PI)*walking);
            // The tailored photo study has a skinned upper, not a dynamic
            // underarm cloth solver. Hold it while legs/skirt present slowly.
            if(!HoldUpperBodyForFitting){
                Swing("LeftArm",Vector3.right,-5*s);Swing("RightArm",Vector3.right,5*s);
                Swing("LeftForeArm",Vector3.right,4*walking);Swing("RightForeArm",Vector3.right,4*walking);
                Swing("Spine2",Vector3.up,-.8f*s);
            }
        }
        void PoseLeg(string upperName,string lowerName,string footName,Vector3 footRest,float phase,float blend)
        {
            if(blend<=0||!joints.TryGetValue(upperName,out Transform upper)||!joints.TryGetValue(lowerName,out Transform lower)||!joints.TryGetValue(footName,out Transform foot))return;
            RunwayGait.Sample(phase,out float forward,out float lift,out float rocker);
            Vector3 target=transform.TransformPoint(footRest+new Vector3(0,lift*blend,forward*blend));
            Quaternion footRotation=foot.rotation;
            Vector3 origin=upper.position;
            float a=Vector3.Distance(origin,lower.position),b=Vector3.Distance(lower.position,foot.position);
            Vector3 direction=target-origin;float distance=Mathf.Clamp(direction.magnitude,Mathf.Abs(a-b)+.001f,a+b-.0001f);direction.Normalize();
            float along=(a*a+distance*distance-b*b)/(2*distance);
            float height=Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            Vector3 bend=Vector3.ProjectOnPlane(transform.forward,direction).normalized;
            Vector3 knee=origin+direction*along+bend*height;
            upper.rotation=Quaternion.FromToRotation(lower.position-origin,knee-origin)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(foot.position-lower.position,target-lower.position)*lower.rotation;
            foot.rotation=Quaternion.AngleAxis(rocker*blend,transform.right)*footRotation;
        }
        void Swing(string key,Vector3 axis,float degrees){if(!joints.TryGetValue(key,out Transform tr))return;var local=tr.parent.InverseTransformDirection(transform.TransformDirection(axis));tr.localRotation=Quaternion.AngleAxis(degrees,local)*rest[tr];}
        public void FillSkinMatrices(Matrix4x4[] destination){if(destination==null||destination.Length!=Bones.Length)throw new ArgumentException("Sai kích thước bộ đệm skin matrix.");Matrix4x4 worldToModel=transform.worldToLocalMatrix;for(int i=0;i<destination.Length;i++)destination[i]=worldToModel*Bones[i].localToWorldMatrix*GarmentBindposes[i];}
        public Matrix4x4[] SkinMatrices(){var m=new Matrix4x4[Bones.Length];FillSkinMatrices(m);return m;}
        public Transform FindJoint(string name)=>joints.TryGetValue(name,out Transform joint)?joint:null;
        public Vector3 Joint(string name){return joints.TryGetValue(name,out Transform t)?transform.InverseTransformPoint(t.position):Vector3.zero;}
        public int BoneIndex(string name){if(!joints.TryGetValue(name,out Transform joint))return 0;int index=Array.IndexOf(Bones,joint);return index>=0?index:0;}
        static Vector3 V(float[] a)=>new Vector3(a[0],a[1],a[2]);static Vector3 ReadV(BinaryReader r)=>new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
        void OnDestroy(){if(coverage!=null)coverage.Clear();if(ownsBody&&BodyMesh){if(Application.isPlaying)Destroy(BodyMesh);else DestroyImmediate(BodyMesh);}if(Original){if(Application.isPlaying)Destroy(Original);else DestroyImmediate(Original);}}
    }
}
