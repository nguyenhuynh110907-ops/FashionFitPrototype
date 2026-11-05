using System.Collections.Generic;
using UnityEngine;

namespace FashionFit
{
    public sealed class RunwayPoseRig : MonoBehaviour
    {
        RunwayAvatar avatar;
        FashionFitApp app;
        readonly List<RunwayPoseHandle> handles=new List<RunwayPoseHandle>();
        readonly Dictionary<Transform,Quaternion> rotations=new Dictionary<Transform,Quaternion>();
        readonly Dictionary<Transform,Vector3> positions=new Dictionary<Transform,Vector3>();
        Material normal,held;
        public string Label {get;private set;}
        public RunwayAvatar Avatar=>avatar;
        public FashionFitApp Owner=>app;
        public IReadOnlyList<RunwayPoseHandle> Handles=>handles;
        public bool IsHeld {get{foreach(var h in handles)if(h.IsHeld)return true;return false;}}
        public bool CanGrab(RunwayPoseHandle candidate){var stand=GetComponentInParent<RunwayPoseBase>();if(stand&&stand.IsHeld)return false;foreach(var h in handles)if(h!=candidate&&h.IsHeld&&candidate.Conflicts(h))return false;return true;}
        public void Initialize(FashionFitApp owner,RunwayAvatar model,string label)
        {
            app=owner;avatar=model;Label=label;
            normal=RunwayStage.Matte("Pose handles cyan",new Color(.18f,.70f,.82f),.28f);normal.SetColor("_EmissionColor",new Color(.06f,.25f,.30f));
            held=RunwayStage.Matte("Pose handle selected",new Color(1,.72f,.22f),.3f);
            foreach(var bone in avatar.Bones){rotations[bone]=bone.localRotation;positions[bone]=bone.localPosition;}
            Add("Hips","Spine","Hông",35,false,false,.18f);
            Add("Spine","Spine2","Lưng dưới",22,false,false,.18f);
            Add("Spine2","Neck","Ngực / lưng trên",30,false,false,.18f);
            Add("Neck","Head","Cổ",35,false,false,.11f);
            Add("Head",null,"Đầu",65,false,false,.09f);
            foreach(string side in new[]{"Left","Right"}){
                string name=side=="Left"?"trái":"phải";
                Add(side+"Arm",side+"ForeArm","Vai "+name,110,false,false,.045f);
                Add(side+"ForeArm",side+"Hand","Khuỷu "+name,145,true,false,.045f);
                Add(side+"Hand",null,"Cổ tay "+name,70,false,true,.045f);
                Add(side+"UpLeg",side+"Leg","Khớp háng "+name,100,false,false,.13f);
                Add(side+"Leg",side+"Foot","Gối "+name,145,true,false,.055f);
                Add(side+"Foot",null,"Cổ chân "+name,35,false,true,.07f);
            }
        }
        void Add(string bone,string child,string label,float limit,bool hinge,bool ik,float offset)
        {
            var joint=avatar.FindJoint(bone);if(!joint)return;
            var go=new GameObject(Label+" · "+label);go.transform.SetParent(transform,false);go.layer=9;
            var collider=go.AddComponent<SphereCollider>();collider.radius=.055f;collider.isTrigger=true;
            var visual=GameObject.CreatePrimitive(PrimitiveType.Sphere);visual.name="Joint marker";visual.transform.SetParent(go.transform,false);visual.transform.localScale=Vector3.one*.043f;
            var primitiveCollider=visual.GetComponent<Collider>();if(Application.isPlaying)Destroy(primitiveCollider);else DestroyImmediate(primitiveCollider);
            var renderer=visual.GetComponent<Renderer>();renderer.sharedMaterial=normal;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            var handle=go.AddComponent<RunwayPoseHandle>();handle.Initialize(this,joint,child==null?null:avatar.FindJoint(child),label,limit,hinge,ik,offset,renderer,normal,held);
            go.AddComponent<RunwayVRGrabTarget>().ConfigureJoint(handle);handles.Add(handle);
        }
        public void SetHandles(bool enabled){foreach(var handle in handles)handle.gameObject.SetActive(enabled);}
        public void Begin(RunwayPoseHandle handle){app.Report(Label+" · "+handle.Label+" • kéo nhẹ / xoay tay • thả Grip để giữ tư thế");}
        public void End(){}
        public void ResetPose(){foreach(var handle in handles)handle.EndGrab();foreach(var bone in avatar.Bones){bone.localRotation=rotations[bone];bone.localPosition=positions[bone];}foreach(var handle in handles)handle.Synchronize();}
        void OnDestroy(){foreach(var material in new[]{normal,held})if(material){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}}
    }

    // Rotation joints keep local bone positions fixed. Wrists/ankles use analytical
    // two-bone IK with a fixed bend plane and bounded reach, never scale/stretch.
    public sealed class RunwayPoseHandle : MonoBehaviour
    {
        RunwayPoseRig rig;Transform bone,child;Renderer marker;Material normal,selected;
        Quaternion restRotation,startRotation,startHand,upperRest,lowerRest;Vector3 startPoint,startTip,axisInParent,bendHint,smoothedTarget;
        float limit,offset;bool hinge,ik,leg,grabbing;
        public string Label {get;private set;}
        public Transform Bone=>bone;
        public bool IsHeld=>grabbing;
        public bool CanGrab=>!grabbing&&rig.CanGrab(this);
        Transform AffectedRoot=>ik?bone.parent.parent:bone;
        public bool Conflicts(RunwayPoseHandle other)=>AffectedRoot==other.AffectedRoot||AffectedRoot.IsChildOf(other.AffectedRoot)||other.AffectedRoot.IsChildOf(AffectedRoot);
        public void Initialize(RunwayPoseRig owner,Transform joint,Transform end,string label,float max,bool isHinge,bool isIK,float frontOffset,Renderer surface,Material idle,Material active)
        {
            rig=owner;bone=joint;child=end;Label=label;limit=max;hinge=isHinge;ik=isIK;offset=frontOffset;marker=surface;normal=idle;selected=active;restRotation=bone.localRotation;
            leg=bone.name.Contains("Leg")||bone.name.Contains("Foot");
            axisInParent=bone.parent.InverseTransformDirection(rig.transform.right*(leg?1:-1));
            if(ik){var upper=bone.parent.parent;upperRest=upper.localRotation;lowerRest=bone.parent.localRotation;Vector3 line=bone.position-upper.position;bendHint=Vector3.ProjectOnPlane(bone.parent.position-upper.position,line.normalized);if(bendHint.sqrMagnitude<.0001f)bendHint=leg?rig.transform.forward:-rig.transform.forward;bendHint=rig.transform.InverseTransformDirection(bendHint.normalized);}
            Synchronize();
        }
        public void Synchronize(){transform.position=bone.position+rig.transform.forward*Mathf.Max(offset,.085f);transform.rotation=Quaternion.identity;}
        void LateUpdate(){Synchronize();}
        public void BeginGrab(Vector3 point,Quaternion hand){if(!CanGrab||!Finite(point)||!Finite(hand))return;grabbing=true;startPoint=point;startHand=hand.normalized;startRotation=bone.rotation;startTip=child?child.position:bone.position+rig.transform.up*.12f;smoothedTarget=bone.position;marker.sharedMaterial=selected;rig.Begin(this);}
        public void Drag(Vector3 point,Quaternion hand,float dt)
        {
            if(!grabbing||!Finite(point)||!Finite(hand))return;
            hand=hand.normalized;
            float frame=Mathf.Clamp(dt,.001f,.05f);
            Vector3 delta=Vector3.ClampMagnitude(point-startPoint,.85f);
            if(ik){
                Vector3 target=startTip+delta-rig.transform.up*.12f;
                if(leg)target.y=Mathf.Max(rig.transform.position.y+.09f,target.y);
                smoothedTarget=Vector3.MoveTowards(smoothedTarget,target,1.5f*frame);
                SolveLimb(bone.parent.parent,bone.parent,bone,smoothedTarget,rig.transform.TransformDirection(bendHint),leg?145:150);
                bone.parent.parent.localRotation=Quaternion.RotateTowards(upperRest,bone.parent.parent.localRotation,leg?100:110);
                bone.parent.localRotation=Quaternion.RotateTowards(lowerRest,bone.parent.localRotation,145);
                Quaternion handDelta=hand*Quaternion.Inverse(startHand);
                Quaternion wanted=leg?Quaternion.Inverse(bone.parent.rotation)*(handDelta*startRotation):Quaternion.Inverse(bone.parent.rotation)*handDelta*bone.parent.rotation*restRotation;
                bone.localRotation=Quaternion.RotateTowards(bone.localRotation,Quaternion.RotateTowards(restRotation,wanted,limit),240*frame);
            }else{
                Vector3 before=startTip-bone.position,after=startTip+delta-bone.position;
                if(before.sqrMagnitude<.00001f||after.sqrMagnitude<.00001f)return;
                Quaternion handDelta=hand*Quaternion.Inverse(startHand);
                Quaternion desired=Quaternion.FromToRotation(before,after)*handDelta*startRotation;
                Quaternion local=Quaternion.Inverse(bone.parent.rotation)*desired;
                if(hinge){
                    Vector3 baseline=Quaternion.Inverse(startRotation)*before;
                    Vector3 restDirection=restRotation*baseline;
                    float angle=Vector3.SignedAngle(Vector3.ProjectOnPlane(restDirection,axisInParent),Vector3.ProjectOnPlane(local*baseline,axisInParent),axisInParent);
                    local=Quaternion.AngleAxis(Mathf.Clamp(angle,0,limit),axisInParent)*restRotation;
                }else local=Quaternion.RotateTowards(restRotation,local,limit);
                bone.localRotation=Quaternion.RotateTowards(bone.localRotation,local,180*frame);
            }
            Synchronize();
        }
        static void SolveLimb(Transform upper,Transform lower,Transform end,Vector3 target,Vector3 hint,float maxFlex)
        {
            Vector3 origin=upper.position;float a=Vector3.Distance(origin,lower.position),b=Vector3.Distance(lower.position,end.position);
            Vector3 line=target-origin;if(line.sqrMagnitude<1e-10f)line=Vector3.down;
            float min=Mathf.Sqrt(a*a+b*b+2*a*b*Mathf.Cos(maxFlex*Mathf.Deg2Rad));
            float max=Mathf.Sqrt(a*a+b*b+2*a*b*Mathf.Cos(5*Mathf.Deg2Rad));
            float d=Mathf.Clamp(line.magnitude,min+.0001f,max-.0001f);line.Normalize();target=origin+line*d;
            Vector3 bend=Vector3.ProjectOnPlane(hint,line);if(bend.sqrMagnitude<.00001f)bend=Vector3.ProjectOnPlane(Vector3.forward,line);if(bend.sqrMagnitude<.00001f)bend=Vector3.ProjectOnPlane(Vector3.right,line);bend.Normalize();
            float along=(a*a+d*d-b*b)/(2*d);Vector3 middle=origin+line*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
            upper.rotation=Quaternion.FromToRotation(lower.position-origin,middle-origin)*upper.rotation;
            lower.rotation=Quaternion.FromToRotation(end.position-lower.position,target-lower.position)*lower.rotation;
        }
        public void EndGrab(){if(!grabbing)return;grabbing=false;marker.sharedMaterial=normal;rig.End();}
        static bool Finite(Vector3 value)=>Finite(value.x)&&Finite(value.y)&&Finite(value.z);
        static bool Finite(Quaternion value)=>Finite(value.x)&&Finite(value.y)&&Finite(value.z)&&Finite(value.w)&&value.x*value.x+value.y*value.y+value.z*value.z+value.w*value.w>.01f;
        static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
