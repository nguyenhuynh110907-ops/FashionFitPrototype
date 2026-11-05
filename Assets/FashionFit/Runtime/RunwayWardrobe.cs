using System;
using System.IO;
using UnityEngine;
namespace FashionFit
{
    // Imported meshes are staged first; source coordinates are never silently rescaled.
    public sealed class RunwayWardrobe:MonoBehaviour
    {
        FashionFitApp app;
        Mesh source,display;
        GameObject garmentObject,previewObject;
        Material material,previewMaterial,shirtMaterial;
        FittingCloth cloth;
        InteractiveShirt shirt;DoffableBlouse blouse;
        public DoffableBlouse Blouse=>blouse;
        TailoredPoloStudy tailored;
        TransferredGarmentAsset transferred;
        TransferredGarmentAsset[] transfers;
        public bool HasDetailedTransfer=>transferred!=null;
        public bool SimulationReady=>(!cloth||cloth.SimulationReady)&&(!shirt||shirt.SimulationReady);
        public TransferredGarmentAsset[] TransferCatalog=>transfers??(transfers=Resources.LoadAll<TransferredGarmentAsset>("TransferredGarments"));
        public void WearTransferred(TransferredGarmentAsset package)
        {
            if(!package){app.Report("Chưa có gói trang phục 3D đã chuyển.");return;}
            string issue=package.Check(app.Model);if(issue!=null){app.Report(issue);return;}
            app.StandForFitting();issue=package.CheckNeutralRig(app.Model);if(issue!=null){app.Report(issue);return;}
            GameObject staged=null;
            try{
                staged=new GameObject("Detailed 3D transfer — stationary review");staged.SetActive(false);staged.transform.SetParent(app.Model.transform,false);
                foreach(var part in package.parts){var child=new GameObject(part.name);child.transform.SetParent(staged.transform,false);var skin=child.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=part.mesh;skin.sharedMaterials=part.materials;skin.bones=app.Model.Bones;skin.rootBone=app.Model.RootBone;skin.quality=SkinQuality.Bone4;SkinnedBounds.SetFromModel(skin,app.Model.transform,part.mesh.bounds,.05f);}
                ClearGarment();app.StandForFitting();garmentObject=staged;transferred=package;GarmentName=package.displayName+" · chuyển từ 3D";app.Look=0;garmentObject.SetActive(true);if(previewObject)previewObject.SetActive(false);
                app.Report("Đã mặc bản chuyển 3D • Giữ vật liệu/UV gốc • Xem đứng yên; chưa kiểm định bước đi hoặc kéo vải VR");
            }catch(Exception e){if(staged)Release(staged);app.Report("Chưa mặc được bản chuyển: "+e.Message);}
        }
        public MembraneMaterial ActiveMembraneMaterial{get;private set;}
        public MembraneReading MaterialReading=>cloth?cloth.MaterialReading:new MembraneReading();
        public void SetMembraneMaterial(MembraneMaterial profile){ActiveMembraneMaterial=profile;if(cloth)cloth.SetPhysicsMaterial(profile.ClothProfile());if(shirt)shirt.SetPhysicsMaterial(profile.ClothProfile());app.RequestClothSettle();}
        bool materialExplicit;
        double materialTime;
        public int PhysicsMaterialIndex {get;private set;}=2;
        public bool AirDemo {get;private set;}
        public float SecondaryOffset=>tailored!=null?tailored.SecondaryOffset:0;
        public void SetAirDemo(bool enabled){AirDemo=enabled;app.RequestClothSettle();}
        public void SetPhysicsMaterial(int index){PhysicsMaterialIndex=Mathf.Clamp(index,0,ClothMaterialProfile.All.Length-1);materialExplicit=true;ApplyPhysicsMaterial();app.RequestClothSettle();}
        void ApplyPhysicsMaterial(){ActiveMembraneMaterial=null;var p=ClothMaterialProfile.All[PhysicsMaterialIndex];if(cloth)cloth.SetPhysicsMaterial(p);if(shirt)shirt.SetPhysicsMaterial(p);if(tailored)tailored.SetPhysicsMaterial(p);if(material)material.SetFloat("_ClothDetailStrength",PhysicsMaterialIndex==0?.08f:PhysicsMaterialIndex==1?.18f:.22f);originalRoughness=p.roughness;if(!HasSurfaceMaps){Roughness=p.roughness;UpdateMaterial();}}
        public bool HasTailoredDetails=>tailored!=null;
        public bool TailoredDetailsVisible=>tailored!=null&&tailored.DetailsVisible;
        public void ToggleTailoredDetails(){if(tailored)tailored.ToggleDetails();}
        Vector3[] sourceVertices;
        Texture2D baseMap,normalMap,roughnessMap;
        Color originalColor=new Color(.12f,.27f,.28f);
        float originalRoughness=.68f;
        public bool MapsVisible=true;
        public bool HasSurfaceMaps=>baseMap||normalMap||roughnessMap;
        public bool HasRoughnessMap=>roughnessMap!=null;
        public float MapScale=1;
        public bool HasGarment=>garmentObject!=null;
        public bool HasPending=>source!=null;
        public bool IsSample=>cloth!=null;
        public bool LegacyClothGrabbed=>(cloth!=null&&cloth.IsGrabbed)||(shirt!=null&&shirt.IsGrabbed);
        public bool ClothGrabbed=>(blouse&&blouse.IsGrabbed)||LegacyClothGrabbed;
        public float ClothSpeed=>Mathf.Max(cloth!=null?cloth.ClothSpeed:0,shirt!=null?shirt.ClothSpeed:0);
        public int ProxyPenetrations=>(cloth!=null?cloth.ProxyPenetrations:0)+(shirt!=null?shirt.ProxyPenetrations:0);
        readonly System.Diagnostics.Stopwatch clothWatch=new System.Diagnostics.Stopwatch();
        float legacyClothMilliseconds;
        public float ClothMilliseconds {get{float ms=legacyClothMilliseconds;foreach(var b in DoffableBlouse.Live)if(b)ms+=b.SimulationMilliseconds;return ms;}private set{legacyClothMilliseconds=value;}}
        public bool HasInteractiveShirt=>shirt!=null;
        public float ShirtLooseness=>shirt!=null?shirt.Looseness:1;
        public float ShirtLength=>shirt!=null?shirt.Length:1;
        public string GarmentName="Chưa có trang phục",ImportSummary="";
        public float UnitMeters=1,WaistY=BodyFitProfile.WaistY,HipY=BodyFitProfile.HipY,CenterX,CenterZ;
        public float FitX,FitY,FitZ,Ease=.015f;
        public bool LandmarksConfirmed,AdaptToBody;
        public bool ShowPending=true;
        public float Roughness=.68f;
        public Color FabricColor=new Color(.12f,.27f,.28f);
        public void Initialize(FashionFitApp owner){app=owner;}
        public void WearSampleSkirt(){WearPreset(DressPreset.AtelierMidi);}
        public void WearPreset(DressPreset preset)
        {
            if(!app.Model.CanFit){app.Report(app.Model.FitIssue);return;}
            ClearGarment();app.StandForFitting();app.Model.HoldUpperBodyForFitting=preset==DressPreset.GucciPoloStudy;
            string objectName=preset==DressPreset.GucciPoloStudy?"Gucci 864361 - 2D reference study":preset==DressPreset.AtelierMidi?"Atelier midi":preset==DressPreset.EmeraldColumn?"Emerald column gown":preset==DressPreset.RoseCouture?"Rose couture gown":preset==DressPreset.SapphireCocktail?"Sapphire cocktail dress":"Ivory blouse set";
            originalColor=preset==DressPreset.GucciPoloStudy?new Color(.025f,.026f,.028f):preset==DressPreset.AtelierMidi?new Color(.12f,.27f,.28f):preset==DressPreset.EmeraldColumn?new Color(.035f,.19f,.13f):preset==DressPreset.RoseCouture?new Color(.47f,.13f,.18f):preset==DressPreset.SapphireCocktail?new Color(.045f,.16f,.38f):new Color(.20f,.23f,.24f);
            if(!baseMap)FabricColor=originalColor;
            originalRoughness=preset==DressPreset.GucciPoloStudy?.88f:preset==DressPreset.EmeraldColumn?.48f:preset==DressPreset.RoseCouture?.62f:preset==DressPreset.SapphireCocktail?.42f:.68f;
            if(!roughnessMap)Roughness=originalRoughness;
            garmentObject=new GameObject(objectName+" · procedural PBD");garmentObject.transform.SetParent(app.Model.transform,false);
            cloth=garmentObject.AddComponent<FittingCloth>();cloth.Initialize(app.Model,preset);
            material=Fabric(objectName+" fabric");cloth.SetMaterial(material);
            if(preset==DressPreset.IvoryShirtSet){shirtMaterial=new Material(material){name="Ivory blouse fabric"};shirtMaterial.SetColor("_Color",new Color(.90f,.86f,.78f));var top=new GameObject("Áo lụa tương tác VR");top.transform.SetParent(garmentObject.transform,false);shirt=top.AddComponent<InteractiveShirt>();shirt.Initialize(app,app.Model,shirtMaterial);}
            if(preset==DressPreset.GucciPoloStudy){var upper=new GameObject("Tailored polo construction study");upper.transform.SetParent(garmentObject.transform,false);tailored=upper.AddComponent<TailoredPoloStudy>();tailored.Initialize(app.Model,material);}
            if(!materialExplicit)PhysicsMaterialIndex=preset==DressPreset.GucciPoloStudy?2:preset==DressPreset.IvoryShirtSet||preset==DressPreset.RoseCouture?0:1;
            ApplyPhysicsMaterial();
            UpdateMaterial();
            GarmentName=preset==DressPreset.GucciPoloStudy?"Gucci 864361 · bản dựng tham chiếu từ ảnh 2D":preset==DressPreset.AtelierMidi?"Váy midi Atelier":preset==DressPreset.EmeraldColumn?"Đầm Emerald Column · chân váy kín":preset==DressPreset.RoseCouture?"Đầm Rose Couture · thiết kế nguyên bản":preset==DressPreset.SapphireCocktail?"Đầm Sapphire Cocktail · thiết kế nguyên bản":"Bộ Ivory · áo lụa chỉnh bằng VR + chân váy";
            app.Model.SetGarmentCoverage((int)preset+1);app.RefreshBodyCoverage();
            app.Report(GarmentName+(tailored?" • Cổ, nẹp và nút là mesh 3D • Phần khuất ước lượng":shirt!=null?" • Grip phần ngực áo để chỉnh rộng/dài/xoay":" • Grip phải trong VR để kéo vải"));
        }
        public void WearDoffableBlouse()
        {
            if(blouse&&blouse.State==DoffableBlouse.WearState.Worn){app.Report("Áo đang trên mẫu · mở nút hoặc đặt lại áo");return;}
            if(DoffableBlouse.Live.Count>=3){app.Report("Đã có 3 áo trong phòng · dùng Dọn áo rời trước khi tạo thêm");return;}
            if(app.Model.IsOriginal){app.Report("Bản thử áo cài nút dành cho mannequin nữ kỹ thuật");return;}
            app.SetLook(5);app.StandForFitting();
            if(shirt){shirt.gameObject.SetActive(false);Release(shirt.gameObject);shirt=null;}
            var root=new GameObject("Blouse - persistent open-front cloth");blouse=root.AddComponent<DoffableBlouse>();
            try{blouse.Initialize(app,shirtMaterial);app.Look=0;GarmentName="Áo blouse cài 4 nút · cởi bằng hai tay VR";app.Model.HoldUpperBodyForFitting=true;app.Report("Áo thử đã sẵn sàng · Grip giữ nẹp, Trigger sát từng nút · mở 4 nút rồi kéo");}
            catch(Exception e){root.SetActive(false);Release(root);blouse=null;app.Report("Chưa tạo được áo cài nút: "+e.Message);throw;}
        }
        public void BeginDoffBlouse(){if(blouse)blouse.BeginDoff();else app.Report("Chọn Mặc áo cài nút trước");}
        public void AdoptDoffableBlouse(DoffableBlouse existing)
        {
            if(!existing||!existing.Ready)return;app.SetLook(5);
            if(shirt){shirt.gameObject.SetActive(false);Release(shirt.gameObject);shirt=null;}
            blouse=existing;app.Look=0;GarmentName="Áo blouse cài 4 nút · cởi bằng hai tay VR";app.Model.HoldUpperBodyForFitting=true;
        }
        public void ResetDoffableBlouse(){if(blouse)blouse.ResetOnBody();else if(DoffableBlouse.Live.Count>0)DoffableBlouse.Live[DoffableBlouse.Live.Count-1].ResetOnBody();else WearDoffableBlouse();}
        public void ClearFreeBlouses(){for(int i=DoffableBlouse.Live.Count-1;i>=0;i--){var b=DoffableBlouse.Live[i];if(b&&b.IsFree&&!b.IsGrabbed){if(blouse==b)blouse=null;b.gameObject.SetActive(false);Release(b.gameObject);}}app.Report("Đã dọn các áo rời không được cầm");}
        public void SetShirtFit(float looseness,float length){if(shirt)shirt.SetFit(looseness,length);}
        public void ResetShirt(){if(shirt)shirt.ResetAdjustment();}
        Material Fabric(string name){var m=new Material(Resources.Load<Shader>("Runway/DressFabric")){name=name};m.SetColor("_Color",MapsVisible?FabricColor:originalColor);m.SetFloat("_Glossiness",1-(MapsVisible?Roughness:originalRoughness));ApplyMaps(m);return m;}
        public void UpdateMaterial(){if(material){material.SetColor("_Color",MapsVisible?FabricColor:originalColor);material.SetFloat("_Glossiness",1-(MapsVisible?Roughness:originalRoughness));ApplyMaps(material);}if(tailored&&material)tailored.SetFabric(material);if(shirtMaterial){shirtMaterial.SetColor("_Color",MapsVisible&&baseMap?Color.white:new Color(.90f,.86f,.78f));shirtMaterial.SetFloat("_Glossiness",1-(MapsVisible?Roughness:originalRoughness));ApplyMaps(shirtMaterial);}}
        void ApplyMaps(Material m){m.SetTexture("_BaseMap",baseMap);m.SetTexture("_NormalMap",normalMap);m.SetTexture("_RoughnessMap",roughnessMap);m.SetFloat("_UseBase",MapsVisible&&baseMap?1:0);m.SetFloat("_UseNormal",MapsVisible&&normalMap?1:0);m.SetFloat("_UseRoughness",MapsVisible&&roughnessMap?1:0);m.SetFloat("_RoughnessBias",Roughness-.68f);m.SetFloat("_Weave",MapsVisible&&(baseMap||normalMap)?0:tailored!=null?.14f:1);m.SetFloat("_MapScale",MapScale);WovenFabricDetail.Apply(m,tailored!=null);}
        public void ToggleMapComparison(){MapsVisible=!MapsVisible;UpdateMaterial();}
        static Texture2D ReadMap(string path,bool linear)
        {
            if(string.IsNullOrEmpty(path))return null;
            if(!File.Exists(path)||new FileInfo(path).Length>20*1024*1024)throw new InvalidDataException("Texture không tồn tại hoặc vượt 20 MB.");
            byte[] bytes=File.ReadAllBytes(path);RunwayFiles.CheckImageSize(bytes);
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,true,linear){name=Path.GetFileName(path),wrapMode=TextureWrapMode.Repeat,filterMode=FilterMode.Trilinear,anisoLevel=4};
            if(!ImageConversion.LoadImage(texture,bytes)){Release(texture);throw new InvalidDataException("Không đọc được ảnh texture.");}
            return texture;
        }
        // Replace maps atomically; this path never changes vertices, UVs or solver state.
        public void ApplySurfaceFiles(string color,string normal=null,string roughness=null)
        {
            if(HasDetailedTransfer)throw new InvalidOperationException("Trang phục chuyển 3D giữ vật liệu gốc; chọn trang phục mẫu để thử texture.");
            Texture2D c=null,n=null,r=null;
            try{c=ReadMap(color,false);if(!c)throw new InvalidDataException("Chưa có ảnh màu.");n=ReadMap(normal,true);r=ReadMap(roughness,true);}
            catch{if(c)Release(c);if(n)Release(n);if(r)Release(r);throw;}
            ReleaseMaps();baseMap=c;normalMap=n;roughnessMap=r;FabricColor=Color.white;Roughness=.68f;MapScale=1;MapsVisible=true;UpdateMaterial();
            app.Report("Đã áp bề mặt vải • Giữ nguyên mesh và UV • Có thể bật/tắt để so sánh");
        }
        void ReleaseMaps(){if(baseMap)Release(baseMap);if(normalMap)Release(normalMap);if(roughnessMap)Release(roughnessMap);baseMap=normalMap=roughnessMap=null;}
        public void ChooseMap(bool normal)
        {
            try{string path=RunwayFiles.Pick(normal?"Normal map RGB +Y":"Base color của vải","png,jpg,jpeg");if(string.IsNullOrEmpty(path))return;
                var info=new FileInfo(path);if(info.Length>20*1024*1024)throw new InvalidDataException("Ảnh tối đa 20 MB.");byte[] bytes=File.ReadAllBytes(path);RunwayFiles.CheckImageSize(bytes);
                var texture=new Texture2D(2,2,TextureFormat.RGBA32,true,normal);if(!ImageConversion.LoadImage(texture,bytes)){Destroy(texture);throw new InvalidDataException("Ảnh không hợp lệ.");}texture.wrapMode=TextureWrapMode.Repeat;texture.anisoLevel=4;
                if(normal){if(normalMap)Destroy(normalMap);normalMap=texture;}else{ReleaseMaps();baseMap=texture;FabricColor=Color.white;}MapsVisible=true;UpdateMaterial();app.Report("Đã áp bề mặt vải theo UV • Không thay đổi hình dáng hay tính chất cơ học");
            }catch(Exception e){app.Report("Không nhập được texture: "+e.Message);}
        }
        public void ClearMaps(){ReleaseMaps();MapsVisible=true;FabricColor=originalColor;Roughness=originalRoughness;MapScale=1;UpdateMaterial();}
        public void ApplyGeneratedTexture(byte[] bytes,string prompt)
        {
            if(bytes==null||bytes.Length==0)throw new InvalidDataException("Ảnh AI rỗng.");
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,true,false);
            if(!ImageConversion.LoadImage(texture,bytes,true)){Destroy(texture);throw new InvalidDataException("Định dạng ảnh AI không được Unity hỗ trợ.");}
            texture.name="Nano Banana fabric - "+prompt;texture.wrapMode=TextureWrapMode.Repeat;texture.filterMode=FilterMode.Trilinear;texture.anisoLevel=4;
            ReleaseMaps();baseMap=texture;MapsVisible=true;FabricColor=Color.white;MapScale=2;UpdateMaterial();
        }
        public void StepCloth(float dt){
            clothWatch.Restart();materialTime+=dt;float t=(float)materialTime;
            Vector3 air=AirDemo?new Vector3(.10f*Mathf.Sin(t*.8f),0,.22f+.07f*Mathf.Sin(t*1.3f)):Vector3.zero;
            if(cloth){cloth.SetAirVelocity(air);cloth.Step(dt);}if(shirt){shirt.SetAirVelocity(air);shirt.Step(dt);}if(tailored)tailored.StepMaterial(dt,air);
            clothWatch.Stop();ClothMilliseconds=(float)clothWatch.Elapsed.TotalMilliseconds;
        }
        public void ResetCloth(){if(cloth)cloth.ResetState();if(shirt)shirt.ResetState();}
        public void ChooseOBJ()
        {
            try{string p=RunwayFiles.Pick("Chọn trang phục OBJ tách riêng","obj");if(!string.IsNullOrEmpty(p))LoadOBJ(p);}
            catch(Exception e){app.Report("Không nhập được OBJ: "+e.Message);}
        }
        public void LoadOBJ(string path)
        {
            Mesh parsed=RunwayOBJ.Read(path);
            if(parsed.bounds.size.y<.00001f){Destroy(parsed);throw new InvalidDataException("Trang phục cần dựng đứng theo Y; chiều cao đang bằng 0.");}
            DiscardPending();source=parsed;sourceVertices=source.vertices;UnitMeters=1;ShowPending=true;LandmarksConfirmed=false;AdaptToBody=false;FitX=FitY=FitZ=0;
            WaistY=source.bounds.max.y;HipY=WaistY-source.bounds.size.y*.24f;CenterX=source.bounds.center.x;CenterZ=source.bounds.center.z;
            previewObject=new GameObject("Pending garment · dimensions unconfirmed");previewObject.transform.SetParent(app.Model.transform,false);previewObject.AddComponent<MeshFilter>().sharedMesh=source;
            previewMaterial=Fabric("Pending preview");previewMaterial.SetColor("_Color",new Color(.55f,.36f,.18f));previewObject.AddComponent<MeshRenderer>().sharedMaterial=previewMaterial;
            ImportSummary=Path.GetFileName(path)+" • "+source.vertexCount.ToString("N0")+" đỉnh • "+(source.triangles.Length/3).ToString("N0")+" tam giác";
            RefreshPreview();app.StandForFitting();app.Report("OBJ đang chờ: chọn đơn vị, kiểm tra chiều cao và mốc eo/hông trước khi mặc.");
        }
        public Vector3 PendingSize=>source?source.bounds.size*UnitMeters:Vector3.zero;
        public void SetUnit(float meters){if(!source)return;UnitMeters=meters;WaistY=source.bounds.max.y*meters;HipY=WaistY-source.bounds.size.y*meters*.24f;CenterX=source.bounds.center.x*meters;CenterZ=source.bounds.center.z*meters;LandmarksConfirmed=false;RefreshPreview();}
        public void RefreshPreview(){if(previewObject){previewObject.transform.localScale=Vector3.one*UnitMeters;Vector3 size=PendingSize;previewObject.SetActive(ShowPending&&size.y>=.05f&&size.y<=2.5f&&size.x<=3&&size.z<=3);}}
        public void ApplyPending()
        {
            if(!source)return;
            if(!app.Model.CanFit){app.Report(app.Model.FitIssue);return;}
            if(!LandmarksConfirmed){app.Report("Hãy xác nhận đơn vị, chiều cao, eo và hông. Bản này hỗ trợ căn váy rời; chưa căn tay áo/cổ áo.");return;}
            try {
                app.StandForFitting();Vector3[] v=GarmentFit.Fit(sourceVertices,UnitMeters,WaistY,HipY,CenterX,CenterZ,AdaptToBody,Ease,FitX,FitY,FitZ,app.Model.BodyVertices);
                var newMesh=Instantiate(source);newMesh.name="Fitted skirt preview";newMesh.vertices=v;newMesh.bindposes=app.Model.GarmentBindposes;
                var weights=new BoneWeight[v.Length];int hips=Array.IndexOf(app.Model.Bones,app.Model.RootBone);if(hips<0)hips=0;
                for(int i=0;i<weights.Length;i++)weights[i]=new BoneWeight{boneIndex0=hips,weight0=1};newMesh.boneWeights=weights;newMesh.RecalculateNormals();newMesh.RecalculateBounds();newMesh.RecalculateTangents();
                ClearGarment();display=newMesh;garmentObject=new GameObject("Imported skirt · geometric preview only");garmentObject.transform.SetParent(app.Model.transform,false);
                var skin=garmentObject.AddComponent<SkinnedMeshRenderer>();skin.sharedMesh=display;skin.bones=app.Model.Bones;skin.rootBone=app.Model.RootBone;skin.localBounds=new Bounds(new Vector3(0,1,0),Vector3.one*3);material=Fabric("Imported fabric");skin.sharedMaterial=material;
                GarmentName=source.name+(AdaptToBody?" • căn eo/hông":" • giữ kích thước gốc");ShowPending=false;previewObject.SetActive(false);
                app.Report("Đã mặc bản sao OBJ • Xem trước hình học • Chưa mô phỏng vải/va chạm cho OBJ nhập");
            } catch(Exception e){app.Report("Chưa căn được: "+e.Message);}
        }
        public void ClearGarment()
        {
            if(blouse){if(blouse.State==DoffableBlouse.WearState.Worn){blouse.gameObject.SetActive(false);Release(blouse.gameObject);}blouse=null;}
            if(app!=null&&app.Model!=null){app.Model.SetGarmentCoverage(0);app.Model.HoldUpperBodyForFitting=false;}
            if(garmentObject){garmentObject.SetActive(false);Release(garmentObject);}garmentObject=null;cloth=null;shirt=null;tailored=null;transferred=null;
            if(display)Release(display);display=null;if(material)Release(material);material=null;if(shirtMaterial)Release(shirtMaterial);shirtMaterial=null;GarmentName="Chưa có trang phục";
        }
        static void Release(UnityEngine.Object owned){if(Application.isPlaying)Destroy(owned);else DestroyImmediate(owned);}
        public void DiscardPending(){if(previewObject){previewObject.SetActive(false);Destroy(previewObject);}previewObject=null;if(previewMaterial)Destroy(previewMaterial);previewMaterial=null;if(source)Destroy(source);source=null;sourceVertices=null;ImportSummary="";}
        void OnDestroy(){ClearGarment();DiscardPending();ReleaseMaps();}
    }
}
