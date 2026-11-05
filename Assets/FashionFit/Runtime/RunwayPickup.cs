using UnityEngine;

namespace FashionFit
{
    // Swept against furniture while held, dynamic with gravity on release.
    public sealed class RunwayPickup : MonoBehaviour
    {
        public ElasticSurface Elastic{get;private set;}
        RunwayShowroom showroom;Rigidbody body;Collider shape;
        Transform homeParent;
        Vector3 homePosition,grabOffset,releaseVelocity,desiredPosition;
        Quaternion homeRotation,rotationOffset,desiredRotation;
        readonly Collider[] contacts=new Collider[64];
        public string Label {get;private set;}
        public int Look {get;private set;}
        public bool IsBeingAdjusted=>IsEquipped&&Elastic&&Elastic.GrabCount>0;
        public bool IsHeld {get;private set;}
        public bool IsEquipped {get;private set;}
        public Vector3 HatFitOffset {get;set;}
        public Vector3 FittingPoint=>shape?shape.bounds.center:transform.position;
        public void Initialize(RunwayShowroom owner,string label,int look)
        {
            showroom=owner;Label=label;Look=look;homeParent=transform.parent;
            homePosition=transform.localPosition;homeRotation=transform.localRotation;shape=GetComponent<Collider>();
            body=gameObject.AddComponent<Rigidbody>();body.mass=look==0 ? .15f : .3f;
            body.isKinematic=true;body.useGravity=false;body.drag=.8f;body.angularDrag=2;
            body.interpolation=RigidbodyInterpolation.Interpolate;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            if(look==0){Elastic=gameObject.AddComponent<ElasticSurface>();int profile=label.Contains("Cowboy")?3:label.Contains("Top hat")||label.Contains("Nỉ")||label.Contains("Fedora")||label.Contains("Vành be")?2:0;Elastic.Initialize(MembraneMaterial.Presets[profile]);if(profile>=2)Elastic.CrownOpening=new Vector2(.098f,.115f);Elastic.Step(.001f);}
            gameObject.layer=9;gameObject.AddComponent<RunwayVRGrabTarget>().ConfigurePickup(this);
        }
        public void BeginGrip(Vector3 hit,Quaternion hand)
        {
            if(IsHeld)return;
            if(Elastic)Elastic.Head=null;IsHeld=true;IsEquipped=false;MakeStatic();
            transform.SetParent(showroom.transform,true);
            grabOffset=Quaternion.Inverse(hand)*(body.position-hit);rotationOffset=Quaternion.Inverse(hand)*body.rotation;
            desiredPosition=body.position;desiredRotation=body.rotation;releaseVelocity=Vector3.zero;
            showroom.Report("Đang cầm "+Label+" • thả gần "+(Look==0?"đầu để đội":"thân mannequin để mặc"));
        }
        public void RebaseGrip(Vector3 point,Quaternion hand)
        {
            grabOffset=Quaternion.Inverse(hand)*(body.position-point);rotationOffset=Quaternion.Inverse(hand)*body.rotation;
            desiredPosition=body.position;desiredRotation=body.rotation;releaseVelocity=Vector3.zero;
        }
        public void MoveGrip(Vector3 point,Quaternion hand,float dt)
        {
            if(!IsHeld||!RunwayVRHands.ValidPose(point,hand))return;
            desiredPosition=point+hand*grabOffset;desiredRotation=hand*rotationOffset;
        }
        void FixedUpdate(){if(IsHeld)StepHeld(Time.fixedDeltaTime);}
        static bool Solid(Collider c)
        {
            if(c.isTrigger||c.gameObject.layer==8)return false;
            if(c.gameObject.layer==10)return true;
            var item=c.GetComponent<RunwayPickup>();return item&&!item.IsEquipped;
        }
        public void StepHeld(float dt)
        {
            if(!IsHeld)return;dt=Mathf.Clamp(dt,.001f,.05f);
            Vector3 previous=body.position,target=Vector3.MoveTowards(previous,desiredPosition,2.5f*dt),delta=target-previous;
            Quaternion rotation=Quaternion.RotateTowards(body.rotation,desiredRotation,240*dt);
            float distance=delta.magnitude;
            if(distance>.00001f){
                foreach(var hit in body.SweepTestAll(delta/distance,distance+.003f,QueryTriggerInteraction.Ignore))
                    if(hit.collider!=shape&&Solid(hit.collider))target=previous+delta.normalized*Mathf.Min(Vector3.Distance(previous,target),Mathf.Max(0,hit.distance-.003f));
            }
            // Also cover rotations and starting overlaps before MovePosition.
            int count=Physics.OverlapSphereNonAlloc(target,shape.bounds.extents.magnitude+.1f,contacts,(1<<9)|(1<<10),QueryTriggerInteraction.Ignore);
            if(count==contacts.Length){target=previous;rotation=body.rotation;}
            else for(int pass=0;pass<4;pass++)for(int i=0;i<count;i++){
                var other=contacts[i];if(other==shape||!Solid(other))continue;
                if(Physics.ComputePenetration(shape,target,rotation,other,other.transform.position,other.transform.rotation,out Vector3 direction,out float depth))target+=direction*(depth+.001f);
            }
            body.MovePosition(target);body.MoveRotation(rotation);
            releaseVelocity=Vector3.ClampMagnitude((target-previous)/dt,1.5f);
        }
        public void ReleaseGrip(){Release(false);}
        public void CancelGrip(){Release(true);}
        void Release(bool cancelled)
        {
            if(!IsHeld)return;IsHeld=false;
            if(!cancelled&&showroom.TryWear(this))return;
            body.isKinematic=false;body.useGravity=true;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
            body.velocity=cancelled?Vector3.zero:releaseVelocity;body.angularVelocity=Vector3.zero;body.WakeUp();
            showroom.Report(cancelled?"Mất tracking / kết thúc thao tác • thả vật an toàn theo trọng lực":"Đã thả "+Label+" • vật rơi theo trọng lực; Grip gần để lấy lại");
        }
        void MakeStatic(){body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;body.isKinematic=true;body.useGravity=false;}
        public void Equip(Transform anchor,Vector3 position,Quaternion rotation)
        {
            IsHeld=false;IsEquipped=true;MakeStatic();transform.SetParent(anchor,false);transform.localPosition=position;transform.localRotation=rotation;if(Elastic){Elastic.Head=anchor.GetComponentInParent<HeadFitSupport>();Elastic.Step(1f/60);}
        }
        public void ReturnToDisplay()
        {
            if(Elastic){Elastic.Head=null;Elastic.EndGrip(0);Elastic.EndGrip(1);Elastic.ResetShape();}IsHeld=false;IsEquipped=false;MakeStatic();transform.SetParent(homeParent,false);transform.localPosition=homePosition;transform.localRotation=homeRotation;releaseVelocity=Vector3.zero;
        }
        void Update(){if(!IsHeld&&!IsEquipped&&(transform.position.y<-.5f||Mathf.Abs(transform.position.x)>4||Mathf.Abs(transform.position.z)>4))ReturnToDisplay();}
    }
}
