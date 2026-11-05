using System.Collections.Generic;
using UnityEngine;

namespace FashionFit
{
    public sealed class RunwayShowroom : MonoBehaviour
    {
        FashionFitApp app;
        readonly List<Mesh> meshes=new List<Mesh>();
        readonly List<Material> materials=new List<Material>();
        readonly List<Bounds> obstacles=new List<Bounds>();
        readonly List<RunwayPickup> items=new List<RunwayPickup>();
        readonly List<RunwayPickup> hats=new List<RunwayPickup>();
        readonly List<TextMesh> lookLabels=new List<TextMesh>();
        readonly List<Renderer> selectionBars=new List<Renderer>();
        MaterialPropertyBlock selectionBlock;
        int shownLook=-1;
        public int BakedPreviewCount {get;private set;}
        Transform hatAnchor;public int OutfitTarget{get;private set;}public int TargetLook=>OutfitTarget==0?app.Look:app.PoseStudio.Wearables[OutfitTarget-1].Look;public HeadFitSupport MainHead{get;private set;}
        public void SelectOutfitTarget(int index){OutfitTarget=Mathf.Clamp(index,0,2);Report(index==0?"Đích mặc: mẫu đi bộ":index==1?"Đích mặc: pose nữ":"Đích mặc: pose nam");if(app.Player&&app.Player.VRMenu)app.Player.VRMenu.Refresh();}
        public HeadFitSupport HeadForTarget(int index)=>index==0?MainHead:app.PoseStudio.Wearables[Mathf.Clamp(index-1,0,1)].Head;
        Vector3 WearPointFor(int target){if(target==0)return WearPoint;var avatar=app.PoseStudio.Rigs[target-1].Avatar;return avatar.transform.TransformPoint(new Vector3(0,avatar.BodyMesh.bounds.max.y*.67f,.10f));}
        public void RegisterReferenceItem(RunwayPickup item){items.Add(item);}
        public IReadOnlyList<RunwayPickup> Items=>items;
        public IReadOnlyList<RunwayPickup> Hats=>hats;
        public Vector3 WearPoint=>app.Model.transform.TransformPoint(new Vector3(0,1.15f,.15f));
        public Vector3 HatPoint=>hatAnchor.position;
        public static readonly string[] LookNames={"Atelier Midi","Emerald","Rose Couture","Sapphire","Ivory Set","Polo Study"};
        Material Material(string name,Color color,float gloss=.25f,float metal=0)
        {var m=RunwayStage.Matte(name,color,gloss,metal);materials.Add(m);return m;}
        GameObject Box(string name,Vector3 p,Vector3 scale,Material m,bool solid=false)
        {
            var go=RunwayStage.Box(transform,name,p,scale,m);go.layer=10;
            if(solid)go.AddComponent<BoxCollider>();return go;
        }
        public void Initialize(FashionFitApp owner)
        {
            app=owner;selectionBlock=new MaterialPropertyBlock();
            var wood=Material("Showroom smoked oak",new Color(.19f,.125f,.085f));
            var cream=Material("Showroom ivory stone",new Color(.76f,.71f,.61f),.35f);
            var metal=Material("Showroom brushed brass",new Color(.48f,.34f,.14f),.5f,.65f);
            var dark=Material("Showroom charcoal",new Color(.045f,.053f,.06f));
            // Dedicated solid furniture/floor; the visual stage has no colliders.
            var floor=new GameObject("Pickup floor collider");floor.transform.SetParent(transform,false);floor.layer=10;
            var ground=floor.AddComponent<BoxCollider>();ground.center=new Vector3(0,-.06f,0);ground.size=new Vector3(8,.1f,8);
            Box("Hat display tabletop",new Vector3(-2.05f,.79f,.65f),new Vector3(2.6f,.08f,1.35f),cream,true);
            for(int x=0;x<2;x++)for(int z=0;z<2;z++)Box("Table brass leg",new Vector3(-2.05f+(x==0?-1.14f:1.14f),.37f,.65f+(z==0?-.5f:.5f)),new Vector3(.045f,.74f,.045f),metal,true);
            obstacles.Add(new Bounds(new Vector3(-2.05f,.4f,.65f),new Vector3(2.6f,.8f,1.35f)));
            var inset=Material("Wardrobe textile back",new Color(.24f,.23f,.21f),.12f);
            var glow=Material("Warm wardrobe light strip",new Color(.45f,.38f,.25f));glow.SetColor("_EmissionColor",new Color(.40f,.29f,.14f));
            Box("Wardrobe back",new Vector3(-2.05f,1.14f,-2.08f),new Vector3(3.12f,2.04f,.07f),wood,true);
            Box("Wardrobe fabric inset",new Vector3(-2.05f,1.30f,-2.035f),new Vector3(3.00f,1.60f,.018f),inset);
            Box("Wardrobe left",new Vector3(-3.64f,1.14f,-1.76f),new Vector3(.07f,2.04f,.7f),wood,true);
            Box("Wardrobe right",new Vector3(-.46f,1.14f,-1.76f),new Vector3(.07f,2.04f,.7f),wood,true);
            Box("Wardrobe top",new Vector3(-2.05f,2.16f,-1.76f),new Vector3(3.25f,.07f,.7f),wood,true);
            Box("Wardrobe lower shelf",new Vector3(-2.05f,.28f,-1.76f),new Vector3(3.12f,.07f,.7f),cream,true);
            Box("Wardrobe hanging rail",new Vector3(-2.05f,1.82f,-1.64f),new Vector3(3.02f,.022f,.022f),metal);
            Box("Wardrobe recessed light",new Vector3(-2.05f,2.045f,-1.60f),new Vector3(3.00f,.016f,.022f),glow);
            Box("Wardrobe brass top reveal",new Vector3(-2.05f,2.13f,-1.392f),new Vector3(3.10f,.012f,.012f),metal);
            Box("Wardrobe front fascia",new Vector3(-2.05f,2.01f,-1.40f),new Vector3(3.12f,.21f,.020f),wood);
            Box("Wardrobe stone plinth",new Vector3(-2.05f,.057f,-1.76f),new Vector3(3.25f,.126f,.74f),cream,true);
            for(int bay=0;bay<3;bay++){
                float x=-1.05f-bay;Box("Wardrobe lower drawer",new Vector3(x,.39f,-1.755f),new Vector3(.96f,.20f,.61f),wood,true);
                Box("Drawer brass pull",new Vector3(x,.40f,-1.435f),new Vector3(.23f,.014f,.021f),metal);
                if(bay<2)Box("Wardrobe bay divider",new Vector3(x-.50f,1.29f,-1.79f),new Vector3(.008f,1.51f,.53f),wood);
            }
            var fill=new GameObject("Wardrobe soft fill").AddComponent<Light>();fill.transform.SetParent(transform,false);fill.transform.localPosition=new Vector3(-2.1f,1.85f,-1.23f);fill.type=LightType.Point;fill.range=1.7f;fill.intensity=1.2f;fill.color=new Color(1,.87f,.68f);fill.shadows=LightShadows.None;fill.cullingMask=1<<11;
            obstacles.Add(new Bounds(new Vector3(-2.05f,1,-1.76f),new Vector3(3.25f,2.2f,.75f)));
            Text("REFERENCE HATS / 10",new Vector3(-2.05f,.70f,1.335f),.052f);
            Text("GRIP TO PICK UP",new Vector3(-2.05f,.61f,1.335f),.028f);
            Text("ATELIER COLLECTION",new Vector3(-2.05f,2.045f,-1.38f),.067f);
            Text("06 LOOKS  /  GRIP TO TRY",new Vector3(-2.05f,1.96f,-1.38f),.028f);

            var catalog=new ReferenceHatCatalog(meshes,materials);
            // Low caps at the front, tall/brimmed hats behind, all at wearable scale.
            int[] front={0,1,4,6,9},back={2,3,5,7,8};
            var displayHats=new RunwayPickup[ReferenceHatCatalog.Labels.Length];
            for(int i=0;i<displayHats.Length;i++){
                var go=catalog.Build(i,transform,out Bounds bounds,out Vector3 fitOffset);
                int column=System.Array.IndexOf(front,i);bool rear=column<0;if(rear)column=System.Array.IndexOf(back,i);
                go.transform.localPosition=new Vector3(-1.05f-column*.5f,.836f-bounds.min.y,rear?.27f:1.00f);
                var collider=go.AddComponent<BoxCollider>();collider.center=bounds.center;collider.size=bounds.size+Vector3.one*.002f;
                var pickup=go.AddComponent<RunwayPickup>();pickup.HatFitOffset=fitOffset;pickup.Initialize(this,ReferenceHatCatalog.Labels[i],0);displayHats[i]=pickup;
                Text((i+1).ToString("00"),new Vector3(go.transform.localPosition.x,.843f,rear?.48f:1.25f),.024f);
            }
            hats.AddRange(displayHats);items.AddRange(displayHats);
            Color[] colors={new Color(.12f,.27f,.28f),new Color(.035f,.19f,.13f),new Color(.47f,.13f,.18f),new Color(.045f,.16f,.38f),new Color(.85f,.8f,.68f),new Color(.035f,.037f,.04f)};
            for(int i=0;i<6;i++){
                var go=new GameObject(LookNames[i]+" display sample");go.transform.SetParent(transform,false);
                Bounds previewBounds;
                if(ShowroomPreviewCatalog.Create(i+1,go.transform,materials,out previewBounds))BakedPreviewCount++;
                else{
                    var fabric=Material(LookNames[i]+" display fabric",colors[i],.18f);float hem=i==1?.105f:i==2?.178f:.145f;
                    var fallback=Lathe(new[]{new Vector2(hem,0),new Vector2(hem*.98f,.04f),new Vector2(.085f,.28f),new Vector2(.115f,.42f),new Vector2(.105f,.48f),new Vector2(.04f,.53f)},28,.42f);MeshObject(go.transform,"Fallback garment silhouette",fallback,fabric);previewBounds=fallback.bounds;
                }
                go.transform.localPosition=new Vector3(-.80f-i*.50f,1.71f-previewBounds.max.y,-1.70f);
                float previewTop=previewBounds.max.y;
                var hanger=RunwayStage.Box(go.transform,"Wood hanger",new Vector3(0,previewTop+.025f,0),new Vector3(.24f,.016f,.04f),wood);hanger.transform.localRotation=Quaternion.Euler(0,0,-4);
                var hook=RunwayStage.Box(go.transform,"Hanger hook",new Vector3(0,previewTop+.075f,0),new Vector3(.010f,.10f,.018f),metal);
                var collider=go.AddComponent<BoxCollider>();collider.center=previewBounds.center;collider.size=previewBounds.size+new Vector3(.008f,.09f,.025f);
                var pickup=go.AddComponent<RunwayPickup>();pickup.Initialize(this,LookNames[i],i+1);items.Add(pickup);
                Box("Look name plate",new Vector3(go.transform.localPosition.x,.66f,-1.40f),new Vector3(.46f,.17f,.016f),dark);
                var namePlate=RunwayStage.Text(transform,(i+1).ToString("00")+" · "+LookNames[i],new Vector3(go.transform.localPosition.x,.69f,-1.385f),.030f,new Color(.95f,.89f,.76f));namePlate.transform.localRotation=Quaternion.Euler(0,180,0);lookLabels.Add(namePlate);
                selectionBars.Add(Box("Active look indicator",new Vector3(go.transform.localPosition.x,.605f,-1.38f),new Vector3(.38f,.009f,.012f),metal).GetComponent<Renderer>());
            }
            RefreshSelectedLook();
            MensReferenceCollection.InstallRack(app,this);
            MainHead=app.Model.gameObject.AddComponent<HeadFitSupport>();MainHead.Initialize(app.Model);hatAnchor=MainHead.Anchor;
        }
        void Text(string text,Vector3 p,float size){var t=RunwayStage.Text(transform,text,p,size,new Color(.88f,.79f,.60f));t.transform.localRotation=Quaternion.Euler(0,180,0);}
        Mesh Lathe(Vector2[] profile,int segments,float depth)
        {
            var p=new Vector3[profile.Length*(segments+1)];var uv=new Vector2[p.Length];var t=new List<int>();
            for(int r=0;r<profile.Length;r++)for(int s=0;s<=segments;s++){
                int i=r*(segments+1)+s;float a=2*Mathf.PI*s/segments;p[i]=new Vector3(profile[r].x*Mathf.Cos(a),profile[r].y,profile[r].x*Mathf.Sin(a)*depth);uv[i]=new Vector2(s/(float)segments,r/(float)(profile.Length-1));
                if(r==0||s==segments)continue;int j=i-(segments+1);t.Add(j);t.Add(i);t.Add(i+1);t.Add(j);t.Add(i+1);t.Add(j+1);
            }
            var m=new Mesh{name="Showroom rounded profile"};m.vertices=p;m.uv=uv;m.triangles=t.ToArray();m.RecalculateNormals();m.RecalculateBounds();meshes.Add(m);return m;
        }
        void MeshObject(Transform parent,string name,Mesh mesh,Material mat){var go=new GameObject(name);go.transform.SetParent(parent,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=mat;}
        public bool TryWear(RunwayPickup item){
            int target=-1;float distance=item.Look==0?.30f:.65f;
            for(int i=0;i<3;i++){float d=Vector3.Distance(item.Look==0?item.transform.position:item.FittingPoint,item.Look==0?HeadForTarget(i).Anchor.position:WearPointFor(i));if(d<distance){distance=d;target=i;}}
            if(target<0)return false;if(MensReferenceCollection.Contains(item.Look)&&target!=2){Report("Bộ tham chiếu nam: đưa gần mannequin NAM hoặc chọn ở mục HOLLISTER NAM.");return false;}OutfitTarget=target;
            if(item.Look==0){EquipHat(item,target);return true;}WearLookOn(item.Look,target);item.ReturnToDisplay();return true;
        }
        void EquipHat(RunwayPickup item,int target){var support=HeadForTarget(target);foreach(var other in hats)if(other!=item&&other.IsEquipped&&other.Elastic&&other.Elastic.Head==support)other.ReturnToDisplay();item.Equip(support.Anchor,item.HatFitOffset,Quaternion.identity);Report("Đã đội "+item.Label+" · hai Grip để kéo/nén, một Grip để tháo");}
        public void WearLookOn(int look,int target){if(MensReferenceCollection.Contains(look)&&target!=2){Report("Bộ nam được dựng vừa mannequin pose NAM; chọn mục HOLLISTER NAM.");return;}if(target==0)app.SetLook(Mathf.Clamp(look,1,6));else app.PoseStudio.Wear(target-1,look);Report("Đã mặc "+MensReferenceCollection.Label(look)+" cho "+(target==0?"mẫu đi bộ":target==1?"pose nữ":"pose nam"));if(app.Player&&app.Player.VRMenu)app.Player.VRMenu.Refresh();}
        public void WearLook(int look){WearLookOn(look,OutfitTarget);}
        public void WearHat(int index){EquipHat(hats[Mathf.Clamp(index,0,hats.Count-1)],OutfitTarget);}
        public void ResetProps(){foreach(var item in items)if(item)item.ReturnToDisplay();Report("Đã trả nón và các mẫu đồ về bàn/kệ");}
        public void Report(string message){app.Report(message);}
        void Update(){if(app&&app.Look!=shownLook)RefreshSelectedLook();}
        public void RefreshSelectedLook(){shownLook=app.Look;for(int i=0;i<lookLabels.Count;i++){lookLabels[i].text=(i+1).ToString("00")+" · "+LookNames[i]+(shownLook==i+1?"\nĐANG MẶC":"");lookLabels[i].color=shownLook==i+1?new Color(.67f,1,.82f):new Color(.95f,.89f,.76f);selectionBlock.SetColor("_Color",shownLook==i+1?new Color(.22f,.72f,.44f):new Color(.31f,.26f,.18f));selectionBlock.SetColor("_EmissionColor",shownLook==i+1?new Color(.10f,.35f,.18f):Color.black);selectionBars[i].SetPropertyBlock(selectionBlock);}}
        public Vector3 ConstrainPosition(Vector3 world,float radius)
        {
            Vector3 local=transform.InverseTransformPoint(world);
            foreach(var b in obstacles){var min=b.min-Vector3.one*radius;var max=b.max+Vector3.one*radius;
                if(local.x<=min.x||local.x>=max.x||local.z<=min.z||local.z>=max.z)continue;
                float l=min.x>=-3.6f?local.x-min.x:float.PositiveInfinity,r=max.x<=3.6f?max.x-local.x:float.PositiveInfinity,f=max.z<=3.6f?max.z-local.z:float.PositiveInfinity,back=min.z>=-3.6f?local.z-min.z:float.PositiveInfinity;float nearest=Mathf.Min(l,r,f,back);
                if(nearest==l)local.x=min.x;else if(nearest==r)local.x=max.x;else if(nearest==f)local.z=max.z;else local.z=min.z;}
            return transform.TransformPoint(local);
        }
        public Vector3 ResolveWalk(Vector3 start,Vector3 delta,float radius,bool avoidModel)
        {
            // Small collision steps prevent skipping a cabinet during a hitch;
            // projection permits sliding along edges instead of orbiting a model.
            int steps=Mathf.Clamp(Mathf.CeilToInt(delta.magnitude/.05f),1,128);Vector3 position=start;
            for(int i=0;i<steps;i++){
                Vector3 target=position+delta/steps,local=transform.InverseTransformPoint(target);local.x=Mathf.Clamp(local.x,-3.6f,3.6f);local.z=Mathf.Clamp(local.z,-3.6f,3.6f);target=ConstrainPosition(transform.TransformPoint(local),radius);
                if(avoidModel){Vector3 radial=target-app.Model.transform.position;radial.y=0;float minimum=.23f+radius;if(radial.sqrMagnitude<minimum*minimum){if(radial.sqrMagnitude<1e-8f)radial=Vector3.forward;radial=radial.normalized*minimum;target.x=app.Model.transform.position.x+radial.x;target.z=app.Model.transform.position.z+radial.z;}}
                if(app.PoseStudio)target=app.PoseStudio.ConstrainWalker(target,radius);
                position=target;
            }
            return position;
        }
        void OnDestroy(){foreach(var item in items)if(item&&item.transform.parent!=transform)Destroy(item.gameObject);if(hatAnchor)Destroy(hatAnchor.gameObject);foreach(var m in meshes)if(m)Destroy(m);foreach(var m in materials)if(m)Destroy(m);}
    }
}
