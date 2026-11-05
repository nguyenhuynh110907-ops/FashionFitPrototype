using System.Collections.Generic;
using UnityEngine;

namespace FashionFit
{
    // Separate pose study: the current fitted female and its cloth remain independent.
    public sealed class RunwayPoseStudio : MonoBehaviour
    {
        FashionFitApp app;
        readonly List<PoseWearable> wearables=new List<PoseWearable>();
        public IReadOnlyList<PoseWearable> Wearables=>wearables;
        readonly List<RunwayPoseRig> rigs = new List<RunwayPoseRig>();
        readonly List<RunwayPoseBase> bases=new List<RunwayPoseBase>();
        public IReadOnlyList<RunwayPoseBase> Bases=>bases;
        readonly List<Material> materials = new List<Material>();
        public IReadOnlyList<RunwayPoseRig> Rigs => rigs;
        public bool HandlesVisible { get; private set; }
        public void Initialize(FashionFitApp owner)
        {
            app=owner;
            var stone=RunwayStage.Matte("Pose studio stone",new Color(.25f,.24f,.22f),.25f);materials.Add(stone);
            for(int i=0;i<2;i++){
                string gender=i==0?"female":"male";
                var standRoot=new GameObject(i==0?"Female movable pose stand":"Male movable pose stand").transform;standRoot.SetParent(transform,false);standRoot.localPosition=new Vector3(i==0?1.55f:2.85f,.025f,-1.90f);
                var avatar=new GameObject(i==0?"CC0 female pose mannequin":"CC0 male pose mannequin").AddComponent<RunwayAvatar>();
                avatar.transform.SetParent(standRoot,false);avatar.transform.localPosition=Vector3.zero;
                avatar.LoadTechnical("PoseModels/"+gender,"MakeHuman CC0 adult "+gender+" pose study");
                SkinnedBounds.SetFromModel(avatar.Renderer,avatar.transform,new Bounds(new Vector3(0,1,0),new Vector3(2.6f,2.9f,2.4f)),0);
                avatar.Original.SetColor("_Color",new Color(.075f,.083f,.095f));avatar.Original.SetFloat("_Glossiness",.42f);
                var rig=avatar.gameObject.AddComponent<RunwayPoseRig>();rig.Initialize(app,avatar,i==0?"Mẫu nữ":"Mẫu nam");rigs.Add(rig);var wearable=avatar.gameObject.AddComponent<PoseWearable>();wearable.Initialize(rig);wearables.Add(wearable);
                RunwayStage.Box(standRoot,"Pose mannequin plinth",new Vector3(0,-.02f,0),new Vector3(.78f,.04f,.76f),stone);
                var stand=standRoot.gameObject.AddComponent<RunwayPoseBase>();stand.Initialize(app,rig);bases.Add(stand);
                var rail=RunwayStage.Box(standRoot,"Stand grip rail",new Vector3(-.34f,.42f,.43f),new Vector3(.034f,.84f,.034f),stone);
                var handle=GameObject.CreatePrimitive(PrimitiveType.Sphere);handle.name="Grip here to move stand";handle.layer=9;handle.transform.SetParent(standRoot,false);handle.transform.localPosition=new Vector3(-.34f,.87f,.43f);handle.transform.localScale=Vector3.one*.09f;handle.GetComponent<Renderer>().sharedMaterial=stone;handle.GetComponent<Collider>().isTrigger=true;
                RunwayStage.Box(standRoot,"Right stand grip rail",new Vector3(.34f,.42f,.43f),new Vector3(.034f,.84f,.034f),stone);
                var rightHandle=GameObject.CreatePrimitive(PrimitiveType.Sphere);rightHandle.name="Grip here to move stand right";rightHandle.layer=9;rightHandle.transform.SetParent(standRoot,false);rightHandle.transform.localPosition=new Vector3(.34f,.87f,.43f);rightHandle.transform.localScale=Vector3.one*.09f;rightHandle.GetComponent<Renderer>().sharedMaterial=stone;rightHandle.GetComponent<Collider>().isTrigger=true;
                var label=RunwayStage.Text(standRoot,i==0?"NỮ / 1,72 m\nGRIP BỆ ĐỂ DI CHUYỂN":"NAM / 1,82 m\nGRIP BỆ ĐỂ DI CHUYỂN",new Vector3(0,.12f,.43f),.043f,new Color(.93f,.83f,.63f));label.transform.localRotation=Quaternion.Euler(0,180,0);
            }
            var heading=RunwayStage.Text(transform,"POSE STUDIO",new Vector3(2.20f,2.22f,-2.32f),.09f,new Color(.93f,.83f,.63f));heading.transform.localRotation=Quaternion.Euler(0,180,0);
            SetHandles(false);
        }
        public void DressDefaults(){if(Resources.Load<PoseOutfitAsset>("PoseWardrobe/Female05")&&Resources.Load<PoseOutfitAsset>("PoseWardrobe/Male06")){wearables[0].Wear(5);wearables[1].Wear(6);}}
        public void Wear(int index,int look){if(app.LayerLab)app.LayerLab.Clear(Mathf.Clamp(index,0,1)+1);index=Mathf.Clamp(index,0,1);wearables[index].Wear(look);app.Report("Đã mặc "+MensReferenceCollection.Label(wearables[index].Look)+" cho "+rigs[index].Label+" · khớp vẫn điều chỉnh được");}
        public void SetHandles(bool visible)
        {
            if(app.Player)app.Player.EndActiveGrab();HandlesVisible=visible;
            foreach(var rig in rigs)rig.SetHandles(visible);
            if(app.Player&&app.Player.VRMenu)app.Player.VRMenu.Refresh();
        }
        public void ToggleHandles(){SetHandles(!HandlesVisible);app.Report(HandlesVisible?"POSE: Grip chấm xanh để chỉnh khớp; thả giữ tư thế; nút POSE / VẢI ở đầu bảng Y":"VẢI: Grip áo để kéo; một Grip cầm nón, hai tay kéo giãn; tư thế được giữ nguyên");}
        public void Focus(int index){SetHandles(true);if(app.Player.VRMenu)app.Player.VRMenu.Hide();app.Player.FocusPoseStudio(Mathf.Clamp(index,0,1));}
        public void ResetPose(int index){if(app.Player)app.Player.EndActiveGrab();index=Mathf.Clamp(index,0,1);rigs[index].ResetPose();bases[index].ReturnHome();app.Report("Đã đặt lại tư thế và bệ "+rigs[index].Label);}
        public void ResetBases(){if(app.Player)app.Player.EndActiveGrab();foreach(var stand in bases)stand.ReturnHome();app.Report("Đã đưa hai bệ pose về vị trí ban đầu");}
        public Vector3 ConstrainWalker(Vector3 target,float radius)
        {
            foreach(var rig in rigs){Vector3 d=target-rig.transform.position;d.y=0;float min=radius+.40f;if(d.sqrMagnitude<min*min){if(d.sqrMagnitude<1e-8f)d=Vector3.forward;d=d.normalized*min;target.x=rig.transform.position.x+d.x;target.z=rig.transform.position.z+d.z;}}
            return target;
        }
        void OnDestroy(){foreach(var material in materials)if(material){if(Application.isPlaying)Destroy(material);else DestroyImmediate(material);}}
    }
}
