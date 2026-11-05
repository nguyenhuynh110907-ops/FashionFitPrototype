using UnityEngine;
using UnityEngine.Rendering;

namespace FashionFit
{
    // One independent interaction state per Touch controller. UI rays can reach
    // the menu; physical grabs always use the short hand-space reach below.
    public sealed class RunwayVRHands : MonoBehaviour
    {
        public const float NearRadius=.16f, ForwardReach=.25f;
        sealed class Hand
        {
            public bool grip=true,trigger=true,tracked;
            public Vector3 position,offset;public Quaternion rotation;
            public RunwayVRGrabTarget target;
            public RunwayVRButton hover;public GarmentFastener fastener;
            public Transform visual;public Transform[] fingers;
            public LineRenderer ray;public TextMesh hint;
            public Material material;public float nextHint;public Object hintSource;public bool hintHeld;
        }
        readonly Hand[] hands={new Hand(),new Hand()};
        readonly Collider[] candidates=new Collider[96];
        readonly Collider[] embedded=new Collider[24];
        readonly RaycastHit[] obstacles=new RaycastHit[96];
        FashionFitApp app;RunwayPlayer player;
        public int HeldCount=>(hands[0].target?1:0)+(hands[1].target?1:0);
        public void Initialize(FashionFitApp owner,RunwayPlayer rig)
        {
            app=owner;player=rig;
            for(int side=0;side<2;side++){
                Hand h=hands[side];h.material=RunwayStage.Matte(side==0?"Left Touch glove":"Right Touch glove",side==0?new Color(.24f,.62f,.72f):new Color(.79f,.60f,.32f),.3f);
                h.visual=new GameObject(side==0?"Left controller hand":"Right controller hand").transform;h.visual.SetParent(transform,false);
                Primitive(h.visual,"Palm",new Vector3(0,-.012f,-.025f),new Vector3(.067f,.034f,.086f),h.material);
                Primitive(h.visual,"Cuff",new Vector3(0,-.018f,-.077f),new Vector3(.056f,.035f,.036f),h.material);
                h.fingers=new Transform[5];
                for(int f=0;f<5;f++){
                    var pivot=new GameObject("Finger "+f).transform;pivot.SetParent(h.visual,false);
                    pivot.localPosition=f==4?new Vector3((side==0?1:-1)*.042f,-.007f,-.019f):new Vector3((f-1.5f)*.017f,0,.015f);
                    if(f==4)pivot.localRotation=Quaternion.Euler(0,side==0?42:-42,0);
                    Primitive(pivot,"Finger",new Vector3(0,0,.022f),new Vector3(.014f,.018f,f==4?.045f:.055f),h.material);h.fingers[f]=pivot;
                }
                h.ray=new GameObject("Menu pointer").AddComponent<LineRenderer>();h.ray.transform.SetParent(h.visual,false);h.ray.positionCount=2;h.ray.startWidth=.0024f;h.ray.endWidth=.001f;h.ray.useWorldSpace=true;h.ray.sharedMaterial=h.material;
                h.hint=RunwayStage.Text(transform,"",Vector3.zero,.022f,new Color(1,.89f,.65f));h.hint.gameObject.SetActive(false);h.visual.gameObject.SetActive(false);
            }
        }
        static void Primitive(Transform parent,string name,Vector3 p,Vector3 scale,Material m)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=p;go.transform.localScale=scale;
            var c=go.GetComponent<Collider>();c.enabled=false;if(Application.isPlaying)Destroy(c);else DestroyImmediate(c);
            var r=go.GetComponent<Renderer>();r.sharedMaterial=m;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
        }
        public RunwayVRGrabTarget HeldBy(int side)=>hands[Mathf.Clamp(side,0,1)].target;
        public bool Visible(int side)=>hands[Mathf.Clamp(side,0,1)].visual.gameObject.activeSelf;
        public static bool Button(float value,bool previous)=>value>=(previous?.35f:.65f);
        public static bool ValidPose(Vector3 p,Quaternion q)=>Finite(p.x)&&Finite(p.y)&&Finite(p.z)&&p.sqrMagnitude<10000&&Finite(q.x)&&Finite(q.y)&&Finite(q.z)&&Finite(q.w)&&q.sqrMagnitude()>.1f;
        static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
        public void Step(int side,bool tracked,Vector3 p,Quaternion q,float grip,float trigger,float dt)
        {
            Hand h=hands[side];tracked&=ValidPose(p,q);
            if(!tracked){Release(side,true);h.tracked=false;h.grip=h.trigger=true;h.visual.gameObject.SetActive(false);h.hint.gameObject.SetActive(false);SetHover(side,null);SetFastener(side,null);return;}
            h.tracked=true;h.position=p;h.rotation=q.normalized;h.visual.gameObject.SetActive(true);h.visual.SetPositionAndRotation(p,h.rotation);
            bool held=Button(grip,h.grip),pressed=Button(trigger,h.trigger);
            for(int f=0;f<4;f++)h.fingers[f].localRotation=Quaternion.Euler((f==0?Mathf.Clamp01(trigger):Mathf.Clamp01(grip))*65,0,0);
            Vector3 direction=h.rotation*Vector3.forward;Ray ray=new Ray(p,direction);
            bool hit=Physics.Raycast(ray,out RaycastHit ui,4,(1<<8)|(1<<9)|(1<<10),QueryTriggerInteraction.Ignore);
            var physical=GarmentFastener.FindNear(p);SetFastener(side,physical);
            var button=!physical&&hit?ui.collider.GetComponent<RunwayVRButton>():null;
            SetHover(side,button);
            bool mannequinHold=false;
            if(pressed&&!h.trigger&&!app.PoseStudio.HandlesVisible){var contact=app.GetComponent<GarmentContactWorld>();mannequinHold=contact&&contact.ManualDressing&&contact.ToggleHandNear(p);if(mannequinHold){app.Report("Đã đổi trạng thái tay mannequin giữ vải");Pulse(side,.2f);}}
            if(!mannequinHold&&pressed&&!h.trigger&&physical){if(physical.Activate(p))Pulse(side,.16f);}
            if(!mannequinHold&&pressed&&!h.trigger&&button){button.Activate();Pulse(side,.12f);if(!player.InVR){ReleaseAll(true);return;}}
            if(held&&!h.grip&&TryBeginNearGrip(side,p,h.rotation,pressed))Pulse(side,.25f);
            if(h.target&&(!h.target.isActiveAndEnabled||!h.target.OwnedBy(h)))Release(side,true);
            if(h.target&&held)h.target.DragOwned(h,p+h.rotation*h.offset,h.rotation);
            if(!held&&h.grip)Release(side,false);
            h.grip=held;h.trigger=pressed;
            var nearby=h.target?h.target:FindTarget(p,direction,out _);
            Vector3 end=button?ui.point:p+direction*(nearby ? .18f : .07f);
            h.ray.enabled=button||nearby;h.ray.SetPosition(0,p);h.ray.SetPosition(1,end);
            h.hint.gameObject.SetActive(nearby);
            Object source=physical?(Object)physical:nearby;bool hasTarget=h.target;
            // Live input, picking and hand transforms still run every frame.
            // Only human-readable text formatting is throttled to 10 Hz.
            if(source&&(Time.unscaledTime>=h.nextHint||source!=h.hintSource||hasTarget!=h.hintHeld)){h.nextHint=Time.unscaledTime+.10f;h.hintSource=source;h.hintHeld=hasTarget;h.hint.text=physical?physical.Hint:nearby.DisplayName+"\n"+(h.target?nearby.ReleaseHint:"Giữ Grip để cầm gần");}
            if(source){h.hint.transform.position=p+Vector3.up*.12f;h.hint.transform.rotation=Quaternion.LookRotation(h.hint.transform.position-player.View.transform.position);h.hint.gameObject.SetActive(true);}
            if(player.VRMenu&&player.VRMenu.Visible&&player.VRMenu.Page==12)h.hint.gameObject.SetActive(false);
        }
        void SetFastener(int side,GarmentFastener f){var h=hands[side];if(h.fastener==f)return;if(h.fastener)h.fastener.SetHovered(side,false);h.fastener=f;if(f)f.SetHovered(side,true);}
        void SetHover(int side,RunwayVRButton b)
        {
            Hand h=hands[side];var old=h.hover;h.hover=b;
            if(old&&hands[1-side].hover!=old)old.SetHovered(false);if(b)b.SetHovered(true);
        }
        void Pulse(int side,float strength)
        {
            var device=UnityEngine.XR.InputDevices.GetDeviceAtXRNode(side==0?UnityEngine.XR.XRNode.LeftHand:UnityEngine.XR.XRNode.RightHand);
            if(device.TryGetHapticCapabilities(out UnityEngine.XR.HapticCapabilities caps)&&caps.supportsImpulse)device.SendHapticImpulse(0,strength,.04f);
        }
        bool ClearPath(Vector3 origin,Vector3 point,Collider destination)
        {
            Vector3 d=point-origin;if(d.sqrMagnitude<.000001f)return true;
            int n=Physics.RaycastNonAlloc(origin,d.normalized,obstacles,d.magnitude+.001f,(1<<8)|(1<<9)|(1<<10),QueryTriggerInteraction.Ignore);
            for(int i=0;i<n;i++){
                Collider c=obstacles[i].collider;if(c==destination)continue;
                // Joint markers are selectable points, not solid obstacles.
                var t=c.GetComponentInParent<RunwayVRGrabTarget>();if(t&&(t.IsJoint||t.IsPoseBase))continue;
                if(obstacles[i].distance<d.magnitude-.005f)return false;
            }
            return n<obstacles.Length;
        }
        RunwayVRGrabTarget FindTarget(Vector3 p,Vector3 direction,out Vector3 point)
        {
            point=p;RunwayVRGrabTarget target=null;float best=float.PositiveInfinity;
            // Raycasts miss a collider when the ray starts inside it. Refuse
            // grabs from hands embedded in a tabletop, cabinet or UI backing.
            int solids=Physics.OverlapSphereNonAlloc(p,.003f,embedded,(1<<8)|(1<<10),QueryTriggerInteraction.Ignore);
            if(solids>0)return null;
            int count=Physics.OverlapSphereNonAlloc(p,NearRadius,candidates,1<<9,QueryTriggerInteraction.Collide);
            // Fail closed on buffer overflow; never silently grab an arbitrary item.
            if(count==candidates.Length)return null;
            for(int i=0;i<count;i++){
                Collider c=candidates[i];var t=c.GetComponentInParent<RunwayVRGrabTarget>();if(!t||!t.CanGrab)continue;
                if(app.PoseStudio.HandlesVisible&&!t.IsJoint&&!t.IsPoseBase&&!t.IsPickup)continue;Vector3 nearest=t.IsJoint?c.transform.position:c.ClosestPoint(p);if(t.PickSurface&&!t.PickSurface.Nearest(p,.11f,out nearest))continue;if(t.Panel&&!t.Panel.Nearest(p,.11f,out nearest))continue;if(t.Blouse&&!t.Blouse.Nearest(p,.085f,out nearest))continue;float d=(nearest-p).sqrMagnitude;
                // A volume enclosing the whole mannequin must not steal a touch on cloth.
                if(t.IsPoseBase&&c is BoxCollider&&c.bounds.size.y>.5f)continue;
                bool sameDistance=target&&Mathf.Abs(Mathf.Sqrt(d)-Mathf.Sqrt(best))<.035f;
                bool jointPriority=app.PoseStudio.HandlesVisible&&target&&t.IsJoint!=target.IsJoint;
                bool preferred=jointPriority?t.IsJoint:target&&sameDistance&&t.GripPriority!=target.GripPriority?t.GripPriority<target.GripPriority:d<best;
                if(preferred&&ClearPath(p,nearest,c)){target=t;point=nearest;best=d;}
            }
            if(!target&&Physics.Raycast(p,direction,out RaycastHit hit,ForwardReach,(1<<8)|(1<<9)|(1<<10),QueryTriggerInteraction.Collide)){
                var t=hit.collider.GetComponentInParent<RunwayVRGrabTarget>();
                if(t&&t.IsPoseBase&&app.PoseStudio.HandlesVisible&&t.TryPoseJointRay(new Ray(p,direction),ForwardReach,out RaycastHit aimedJoint)){target=aimedJoint.collider.GetComponentInParent<RunwayVRGrabTarget>();point=aimedJoint.point;}
                else if(t&&t.CanGrab&&(!app.PoseStudio.HandlesVisible||t.IsJoint||t.IsPoseBase||t.IsPickup)){Vector3 actual=hit.point;if((!t.PickSurface||t.PickSurface.Nearest(p,.11f,out actual))&&(!t.Panel||t.Panel.Nearest(p,.11f,out actual))&&(!t.Blouse||t.Blouse.Nearest(actual,.06f,out _))){target=t;point=actual;}}
            }

            return target;
        }
        public bool TryBeginNearGrip(int side,Vector3 p,Quaternion q,bool detachHat=false)
        {
            if(side<0||side>1||!ValidPose(p,q)||hands[side].target)return false;
            var target=FindTarget(p,q*Vector3.forward,out Vector3 point);if(!target)return false;
            Hand h=hands[side];if(!target.TryAcquire(h,point,q,detachHat))return false;
            h.target=target;h.offset=Quaternion.Inverse(q)*(point-p);if(player.VRMenu&&player.VRMenu.Page!=12)player.VRMenu.Hide();SetHover(0,null);SetHover(1,null);return true;
        }
        public void Release(int side,bool cancelled)
        {
            Hand h=hands[side];if(h.target)h.target.Release(h,cancelled);h.target=null;SetFastener(side,null);
        }
        public void ReleaseAll(bool cancelled){Release(0,cancelled);Release(1,cancelled);}
        public void Rearm(){ReleaseAll(true);foreach(var h in hands)h.grip=h.trigger=true;}
        public void Hide(){Rearm();for(int i=0;i<2;i++){hands[i].visual.gameObject.SetActive(false);hands[i].hint.gameObject.SetActive(false);SetHover(i,null);}}
        void OnDestroy(){ReleaseAll(true);foreach(var h in hands)if(h.material){if(Application.isPlaying)Destroy(h.material);else DestroyImmediate(h.material);}}
    }
    static class RunwayQuaternionMath {public static float sqrMagnitude(this Quaternion q)=>q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w;}
}
