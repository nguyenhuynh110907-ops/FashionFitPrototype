using UnityEngine;
namespace FashionFit {
 // Authored fitting studies from public product references, not retailer meshes.
 public static class MensReferenceCollection {
  public const int First=7,Count=5;
  public static readonly string[] Names={"Boxy Tee + Denim","Pique Polo + Chino","Rugby + Denim","Rib Henley + Chino","Layering Tee + Chino"};
  public static bool Contains(int look)=>look>=First&&look<First+Count;
  public static string Label(int look)=>Contains(look)?"Hollister · "+Names[look-First]:RunwayShowroom.LookNames[Mathf.Clamp(look-1,0,5)];
  public static PoseOutfitAsset Asset(int look)=>Contains(look)?Resources.Load<PoseOutfitAsset>("MensReference/Male"+(look-First+1).ToString("00")):null;
  public static void Wear(FashionFitApp app,int index){app.Showroom.SelectOutfitTarget(2);app.Showroom.WearLook(First+Mathf.Clamp(index,0,Count-1));}
  public static void InstallRack(FashionFitApp app,RunwayShowroom showroom){
   if(!Asset(First))return;
   var rack=new GameObject("Hollister mens reference rail");rack.transform.SetParent(showroom.transform,false);rack.transform.localPosition=new Vector3(3.63f,0,1.05f);rack.transform.localRotation=Quaternion.Euler(0,-90,0);
   var metal=RunwayStage.Matte("Mens rail brushed metal",new Color(.22f,.23f,.24f),.4f,.55f);
   var rail=RunwayStage.Box(rack.transform,"Mens rail",new Vector3(0,1.80f,0),new Vector3(2.65f,.025f,.025f),metal);
   for(int s=-1;s<=1;s+=2){RunwayStage.Box(rack.transform,"Mens rack upright",new Vector3(s*1.32f,.91f,0),new Vector3(.028f,1.82f,.028f),metal);RunwayStage.Box(rack.transform,"Mens rack foot",new Vector3(s*1.32f,.04f,0),new Vector3(.18f,.06f,.34f),metal);}
   var label=RunwayStage.Text(rack.transform,"HOLLISTER / 5 MALE STUDIES",new Vector3(0,1.99f,.01f),.046f,new Color(.94f,.87f,.7f));label.transform.localRotation=Quaternion.Euler(0,180,0);
   for(int i=0;i<Count;i++){
    var asset=Asset(First+i);if(!asset)continue;
    var go=new GameObject(Names[i]+" reference sample");go.transform.SetParent(rack.transform,false);go.transform.localPosition=new Vector3((i-2)*.51f,1.72f-asset.mesh.bounds.max.y*.27f,0);
    var visual=new GameObject("Authored mens preview");visual.transform.SetParent(go.transform,false);visual.transform.localScale=Vector3.one*.27f;visual.AddComponent<MeshFilter>().sharedMesh=asset.mesh;visual.AddComponent<MeshRenderer>().sharedMaterials=asset.materials;
    var bounds=new Bounds(asset.mesh.bounds.center*.27f,asset.mesh.bounds.size*.27f);var box=go.AddComponent<BoxCollider>();box.center=bounds.center;box.size=bounds.size+Vector3.one*.015f;
    RunwayStage.Box(go.transform,"Hanger",new Vector3(0,asset.mesh.bounds.max.y*.27f+.02f,0),new Vector3(.22f,.012f,.025f),metal);
    var item=go.AddComponent<RunwayPickup>();item.Initialize(showroom,Label(First+i),First+i);showroom.RegisterReferenceItem(item);
    var plate=RunwayStage.Text(rack.transform,(i+1).ToString("00")+"\n"+Names[i].Split('+')[0],new Vector3((i-2)*.51f,.98f,.06f),.022f,new Color(.94f,.87f,.7f));plate.transform.localRotation=Quaternion.Euler(0,180,0);
   }
  }
 }
}
