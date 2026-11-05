using UnityEngine;

namespace FashionFit
{
    // A heavy upright display stand. Pose bones are children of this root;
    // moving the stand preserves every local joint rotation and limb length.
    public sealed class RunwayPoseBase:MonoBehaviour
    {
        FashionFitApp app;RunwayPoseRig rig;Rigidbody body;
        Vector3 offset,wanted,home;Quaternion turnOffset,wantedRotation,homeRotation;
        readonly Collider[] overlaps=new Collider[96];
        readonly Collider[] shapes=new Collider[2];
        public bool IsHeld {get;private set;}
        public bool CanGrab=>rig&&!rig.IsHeld;
        public string Label=>rig?rig.Label:"Bệ mannequin";
        public RunwayPoseRig Rig=>rig;
        public void Initialize(FashionFitApp owner,RunwayPoseRig model)
        {
            app=owner;rig=model;home=transform.position;homeRotation=transform.rotation;gameObject.layer=9;
            var plinth=gameObject.AddComponent<BoxCollider>();plinth.center=new Vector3(0,-.02f,0);plinth.size=new Vector3(.78f,.04f,.76f);shapes[0]=plinth;
            var torso=gameObject.AddComponent<BoxCollider>();torso.center=new Vector3(0,.92f,0);torso.size=new Vector3(.43f,1.56f,.36f);shapes[1]=torso;
            body=gameObject.AddComponent<Rigidbody>();body.mass=60;body.centerOfMass=new Vector3(0,.08f,0);body.useGravity=false;body.isKinematic=true;body.drag=2;body.angularDrag=5;body.interpolation=RigidbodyInterpolation.Interpolate;
            body.constraints=RigidbodyConstraints.FreezeRotationX|RigidbodyConstraints.FreezeRotationZ;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;
            gameObject.AddComponent<RunwayVRGrabTarget>().ConfigurePoseBase(this);
        }
        public void BeginGrab(Vector3 point,Quaternion rotation)
        {
            if(!CanGrab||IsHeld)return;IsHeld=true;body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;body.isKinematic=true;body.useGravity=false;Rebase(point,rotation);
            app.Report(Label+" • đang giữ bệ • kéo để di chuyển, xoay tay để quay • có thể giữ bằng hai tay");
        }
        public void Rebase(Vector3 point,Quaternion rotation)
        {
            Quaternion yaw=Quaternion.Euler(0,rotation.eulerAngles.y,0);offset=Quaternion.Inverse(yaw)*(body.position-point);turnOffset=Quaternion.Inverse(yaw)*body.rotation;wanted=body.position;wantedRotation=body.rotation;
        }
        public void Drag(Vector3 point,Quaternion rotation)
        {
            if(!IsHeld||!RunwayVRHands.ValidPose(point,rotation))return;
            Quaternion yaw=Quaternion.Euler(0,rotation.eulerAngles.y,0);wanted=point+yaw*offset;wantedRotation=yaw*turnOffset;
            wanted.x=Mathf.Clamp(wanted.x,-3.40f,3.40f);wanted.z=Mathf.Clamp(wanted.z,-3.40f,3.40f);wanted.y=Mathf.Clamp(wanted.y,.032f,.85f);
        }
        bool Solid(Collider c)
        {
            if(c.isTrigger||c.transform.IsChildOf(transform))return false;
            return c.gameObject.layer==10||c.GetComponentInParent<RunwayPoseBase>()||c.GetComponentInParent<RunwayPickup>();
        }
        void FixedUpdate(){if(!body||!rig||!app||!app.Ready)return;if(IsHeld)StepHeld(Time.fixedDeltaTime);else if(!body.isKinematic&&body.IsSleeping())Park();}
        public void StepHeld(float dt)
        {
            if(!IsHeld)return;dt=Mathf.Clamp(dt,.001f,.05f);Vector3 from=body.position,to=Vector3.MoveTowards(from,wanted,.85f*dt),delta=to-from;
            Quaternion q=Quaternion.RotateTowards(body.rotation,wantedRotation,65*dt);
            if(delta.sqrMagnitude>1e-10f)foreach(var hit in body.SweepTestAll(delta.normalized,delta.magnitude+.004f,QueryTriggerInteraction.Ignore))if(Solid(hit.collider)&&Vector3.Dot(delta,hit.normal)<-.000001f)to=from+delta.normalized*Mathf.Min((to-from).magnitude,Mathf.Max(0,hit.distance-.004f));
            int count=Physics.OverlapSphereNonAlloc(to+Vector3.up*.9f,1.3f,overlaps,(1<<9)|(1<<10),QueryTriggerInteraction.Ignore);
            if(count>=overlaps.Length){to=from;q=body.rotation;}
            else for(int pass=0;pass<3;pass++)for(int i=0;i<count;i++)if(Solid(overlaps[i]))foreach(var shape in shapes)
                if(Physics.ComputePenetration(shape,to,q,overlaps[i],overlaps[i].transform.position,overlaps[i].transform.rotation,out Vector3 push,out float depth))to+=push*(depth+.002f);
            Vector3 model=app.Model.transform.position;Vector3 radial=to-model;radial.y=0;
            if(radial.sqrMagnitude<.85f*.85f){if(radial.sqrMagnitude<.0001f)radial=Vector3.forward;radial=radial.normalized*.85f;to.x=model.x+radial.x;to.z=model.z+radial.z;}
            // Stop a held stand before its footprint reaches the user's head.
            Vector3 user=app.Player.View.transform.position;radial=to-user;radial.y=0;
            if(radial.sqrMagnitude<.62f*.62f){to.x=from.x;to.z=from.z;}
            to.x=Mathf.Clamp(to.x,-3.4f,3.4f);to.z=Mathf.Clamp(to.z,-3.4f,3.4f);body.MovePosition(to);body.MoveRotation(q);
        }
        public void EndGrab(bool cancelled)
        {
            if(!IsHeld)return;IsHeld=false;body.isKinematic=false;body.useGravity=true;body.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;body.velocity=Vector3.zero;body.angularVelocity=Vector3.zero;body.WakeUp();
            app.Report(Label+" • đã thả bệ, giữ tư thế và hạ xuống theo trọng lực");
        }
        void Park(){body.collisionDetectionMode=CollisionDetectionMode.ContinuousSpeculative;body.isKinematic=true;body.useGravity=false;}
        public void ReturnHome(){IsHeld=false;Park();body.position=home;body.rotation=homeRotation;transform.SetPositionAndRotation(home,homeRotation);foreach(var h in rig.Handles)h.Synchronize();}
    }
}
