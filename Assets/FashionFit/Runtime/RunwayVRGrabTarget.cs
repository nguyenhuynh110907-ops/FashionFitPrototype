using UnityEngine;

namespace FashionFit
{
    public sealed class RunwayVRGrabTarget:MonoBehaviour
    {
        readonly object[] owners=new object[2];
        readonly Vector3[] points=new Vector3[2];
        readonly Quaternion[] rotations=new Quaternion[2];
        FashionFitApp app;FittingCloth cloth;InteractiveShirt shirt;bool mannequin;
        readonly Vector3[] localPickupContact=new Vector3[2];float joinedSpan;bool wornPatch;LayeredPanel panel;public LayeredPanel Panel=>panel;public ElasticSurface PickSurface=>elastic?elastic:pickup?pickup.Elastic:null;public bool IsWornHat=>pickup&&pickup.Elastic&&pickup.IsEquipped;
        ElasticSurface elastic;public ElasticSurface Elastic=>elastic;
        DoffableBlouse blouse;
        public DoffableBlouse Blouse=>blouse;
        RunwayPickup pickup;RunwayPoseHandle joint;RunwayPoseBase poseBase;
        Quaternion sharedRotation=Quaternion.identity;
        static readonly RaycastHit[] poseRayHits=new RaycastHit[64];
        // The invisible body selection box belongs to the movable stand. It
        // must not hide a real joint handle aimed at through that same box.
        public bool TryPoseJointRay(Ray ray,float distance,out RaycastHit selected)
        {
            selected=default;var stand=poseBase?poseBase:elastic?GetComponentInParent<RunwayPoseBase>():null;if(!stand)return false;
            int count=Physics.RaycastNonAlloc(ray,poseRayHits,distance,(1<<8)|(1<<9)|(1<<10),QueryTriggerInteraction.Collide);if(count>=poseRayHits.Length)return false;
            float blocker=distance,nearest=float.PositiveInfinity;
            for(int i=0;i<count;i++){
                var c=poseRayHits[i].collider;var t=c.GetComponentInParent<RunwayVRGrabTarget>();
                if(t==this||(t&&(t.poseBase==stand||((t.joint||t.elastic)&&t.GetComponentInParent<RunwayPoseBase>()==stand))))continue;
                blocker=Mathf.Min(blocker,poseRayHits[i].distance);
            }
            bool found=false;
            for(int i=0;i<count;i++){
                var t=poseRayHits[i].collider.GetComponentInParent<RunwayVRGrabTarget>();
                if(t&&t.joint&&t.CanGrab&&t.GetComponentInParent<RunwayPoseBase>()==stand&&poseRayHits[i].distance<blocker+.001f&&poseRayHits[i].distance<nearest){selected=poseRayHits[i];nearest=selected.distance;found=true;}
            }
            return found;
        }
        public int OwnerCount=>(owners[0]!=null?1:0)+(owners[1]!=null?1:0);
        public bool SupportsTwoHands=>pickup||cloth||shirt||blouse||elastic||panel||poseBase||mannequin;
        public bool IsJoint=>joint;
        public bool IsPoseBase=>poseBase;
        public bool IsPickup=>pickup;
        public int GripPriority=>joint?0:pickup?1:panel?2:elastic?2:blouse?2:shirt?2:cloth?3:4;
        public bool CanGrab=>isActiveAndEnabled&&(!GetComponent<GarmentLayerPart>()||GetComponent<GarmentLayerPart>().Interactable)&&(!mannequin||DoffableBlouse.Live.Count==0)&&OwnerCount<(SupportsTwoHands?2:1)&&(!joint||joint.CanGrab)&&(!poseBase||poseBase.CanGrab)&&(!pickup||OwnerCount>0||!pickup.IsHeld);
        public string DisplayName=>panel?panel.Label:elastic?"Vải mannequin pose":blouse?"Áo cài nút VR":joint?joint.Label:poseBase?poseBase.Label+" · bệ di chuyển":pickup?pickup.Label:mannequin?"Ma-nơ-canh":shirt?"Áo Ivory":"Váy / đầm";
        public string ReleaseHint=>wornPatch?"Trigger + Grip: chỉnh nón tại chỗ · "+pickup.Elastic.Reading.Summary:panel?panel.Reading.Summary:elastic?(elastic.ContactMotion&&elastic.ContactMotion.Active?elastic.ContactMotion.Summary:elastic.Reading.Summary):pickup&&pickup.Elastic&&OwnerCount==2?"Tách hai tay để kéo · đưa gần để nén · "+pickup.Elastic.Reading.Summary:blouse?blouse.Hint:OwnerCount==2?"Hai tay đang giữ • thả một tay để tiếp tục bằng tay còn lại":joint?"Thả Grip để giữ tư thế":poseBase?"Thả Grip để đặt bệ xuống • giữ nguyên tư thế":pickup?(pickup.Look==0?"Một Grip: nhấc nón · hai tay: kéo giãn · thả gần đầu để đội":"Thả gần thân để mặc; nơi khác để rơi"):"Thả Grip để vải ổn định theo trọng lực";
        int Slot(object owner){if(owner==null)return -1;for(int i=0;i<2;i++)if(ReferenceEquals(owner,owners[i]))return i;return -1;}
        public bool OwnedBy(object owner)=>owner!=null&&Slot(owner)>=0;
        public bool TryAcquire(object owner,Vector3 point,Quaternion rotation,bool adjustWornHat=false)
        {
            if(owner==null||OwnedBy(owner)||!CanGrab||!RunwayVRHands.ValidPose(point,rotation))return false;
            if(pickup&&pickup.Elastic&&!pickup.Elastic.Nearest(point,.10f,out _))return false;
            int slot=owners[0]==null?0:1;bool started;if(OwnerCount==0)wornPatch=IsWornHat&&adjustWornHat;
            if(wornPatch)started=pickup.Elastic.BeginGrip(slot,point);else if(panel)started=panel.BeginGrip(slot,point);else if(elastic)started=elastic.BeginGrip(slot,point);
            else if(blouse)started=blouse.BeginVRGrab(slot,point);
            else if(cloth)started=cloth.BeginVRGrab(slot,point);
            else if(shirt)started=shirt.BeginVRGrab(slot,point);
            else {started=true;if(OwnerCount==0)BeginGrab(point,rotation);}
            if(!started)return false;
            owners[slot]=owner;points[slot]=point;rotations[slot]=rotation;if(pickup)localPickupContact[slot]=pickup.transform.InverseTransformPoint(point);
            if(OwnerCount==2&&!wornPatch&&!panel&&!cloth&&!shirt&&!blouse&&!elastic){SharedFrame(out Vector3 p,out Quaternion q);Rebase(p,q);}
            if(OwnerCount==2&&!wornPatch&&pickup&&pickup.Elastic){pickup.Elastic.BeginGrip(0,pickup.transform.TransformPoint(localPickupContact[0]));pickup.Elastic.BeginGrip(1,pickup.transform.TransformPoint(localPickupContact[1]));joinedSpan=Vector3.Distance(points[0],points[1]);}
            return true;
        }
        public void DragOwned(object owner,Vector3 point,Quaternion rotation)
        {
            int slot=Slot(owner);if(slot<0||!RunwayVRHands.ValidPose(point,rotation))return;
            points[slot]=point;rotations[slot]=rotation;
            if(wornPatch)pickup.Elastic.MoveGrip(slot,point);else if(panel)panel.MoveGrip(slot,point);else if(elastic)elastic.MoveGrip(slot,point);else if(blouse)blouse.MoveVRGrab(slot,point);else if(cloth)cloth.MoveVRGrab(slot,point);else if(shirt)shirt.MoveVRGrab(slot,point);
            else if(OwnerCount==2){if(pickup&&pickup.Elastic){pickup.Elastic.MoveBalancedGrips(Vector3.Distance(points[0],points[1])-joinedSpan,points[1]-points[0]);}SharedFrame(out Vector3 p,out Quaternion q);Drag(p,q);}else Drag(point,rotation);
        }
        void SharedFrame(out Vector3 point,out Quaternion rotation)
        {
            point=(points[0]+points[1])*.5f;Vector3 line=points[1]-points[0];
            Vector3 up=rotations[0]*Vector3.up+rotations[1]*Vector3.up;
            if(line.sqrMagnitude>.0016f){Vector3 forward=Vector3.Cross(line.normalized,up);if(forward.sqrMagnitude>.001f)sharedRotation=Quaternion.LookRotation(forward.normalized,Vector3.Cross(forward,line).normalized);}
            rotation=sharedRotation;
        }
        void Rebase(Vector3 p,Quaternion q)
        {
            if(pickup)pickup.RebaseGrip(p,q);else if(poseBase)poseBase.Rebase(p,q);else if(mannequin&&app)app.BeginMannequinGrab(p,q);
        }
        public void Release(object owner,bool cancelled)
        {
            int slot=Slot(owner);if(slot<0)return;owners[slot]=null;
            if(wornPatch){pickup.Elastic.EndGrip(slot);if(OwnerCount==0)wornPatch=false;return;}if(panel){panel.EndGrip(slot,cancelled);return;}if(elastic){elastic.EndGrip(slot);return;}if(pickup&&pickup.Elastic){pickup.Elastic.EndGrip(0);pickup.Elastic.EndGrip(1);}if(blouse){blouse.EndVRGrab(slot);return;}if(cloth){cloth.EndVRGrab(slot);return;}if(shirt){shirt.EndVRGrab(slot);return;}
            if(OwnerCount>0){int other=1-slot;Rebase(points[other],rotations[other]);return;}
            if(pickup&&cancelled)pickup.CancelGrip();else if(poseBase)poseBase.EndGrab(cancelled);else EndGrab();
        }
        void OnDisable(){for(int i=0;i<2;i++)if(owners[i]!=null)Release(owners[i],true);}
        public void ConfigurePanel(LayeredPanel p){panel=p;}
        public void ConfigureElastic(ElasticSurface surface){elastic=surface;}
        public void ConfigureDoffable(DoffableBlouse b){blouse=b;}
        public void ConfigureJoint(RunwayPoseHandle h){joint=h;}
        public void ConfigurePoseBase(RunwayPoseBase b){poseBase=b;}
        public void ConfigurePickup(RunwayPickup p){pickup=p;}
        public void ConfigureMannequin(FashionFitApp a){app=a;mannequin=true;}
        public void ConfigureCloth(FittingCloth c){cloth=c;}
        public void ConfigureShirt(InteractiveShirt s){shirt=s;}
        public void BeginGrab(Vector3 p,Quaternion q)
        {
            if(elastic)elastic.BeginGrip(0,p);else if(blouse)blouse.BeginVRGrab(0,p);else if(joint)joint.BeginGrab(p,q);else if(pickup)pickup.BeginGrip(p,q);else if(poseBase)poseBase.BeginGrab(p,q);else if(mannequin&&app)app.BeginMannequinGrab(p,q);else if(cloth)cloth.BeginVRGrab(p);else if(shirt)shirt.BeginVRGrab(p,q);
        }
        public void Drag(Vector3 p,Quaternion q)
        {
            if(elastic)elastic.MoveGrip(0,p);else if(blouse)blouse.MoveVRGrab(0,p);else if(joint)joint.Drag(p,q,Time.unscaledDeltaTime);else if(pickup)pickup.MoveGrip(p,q,Time.unscaledDeltaTime);else if(poseBase)poseBase.Drag(p,q);else if(mannequin&&app)app.UpdateMannequinGrab(p,q);else if(cloth)cloth.MoveVRGrab(p);else if(shirt)shirt.MoveVRGrab(p,q);
        }
        public void EndGrab()
        {
            if(elastic)elastic.EndGrip(0);else if(blouse)blouse.EndVRGrab(0);else if(joint)joint.EndGrab();else if(pickup)pickup.ReleaseGrip();else if(poseBase)poseBase.EndGrab(false);else if(mannequin&&app)app.EndMannequinGrab();else if(cloth)cloth.EndVRGrab();else if(shirt)shirt.EndVRGrab();
        }
    }
}
