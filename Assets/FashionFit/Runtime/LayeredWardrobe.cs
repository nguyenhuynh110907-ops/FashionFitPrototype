using UnityEngine;
namespace FashionFit {
 public sealed class LayeredWardrobe:MonoBehaviour {
  FashionFitApp app;readonly LayeredAoDai[] outfits=new LayeredAoDai[3];public LayeredAoDai Current=>outfits[app.Showroom.OutfitTarget];public LayeredAoDai ForTarget(int i)=>outfits[Mathf.Clamp(i,0,2)];
  public void Initialize(FashionFitApp owner){app=owner;}
  public void WearSelected(){int target=app.Showroom.OutfitTarget;app.Player.EndActiveGrab();Clear(target);if(target==0){app.SetLook(0);app.StandForFitting();}else{app.PoseStudio.Wearables[target-1].Clear();app.PoseStudio.SetHandles(false);}var avatar=target==0?app.Model:app.PoseStudio.Rigs[target-1].Avatar;var go=new GameObject("Layered Ao dai trial");go.transform.SetParent(avatar.transform,false);var cloth=go.AddComponent<LayeredAoDai>();outfits[target]=cloth;cloth.Initialize(app,avatar,target==2?"Male":"Female");app.Report("Áo dài thử đã mặc · Grip tà gần tay · chọn từng lớp trong menu ÁO DÀI / LỚP VẢI");}
  public void Clear(int target){var outfit=outfits[Mathf.Clamp(target,0,2)];if(outfit){outfit.gameObject.SetActive(false);if(Application.isPlaying)Destroy(outfit.gameObject);else DestroyImmediate(outfit.gameObject);}outfits[Mathf.Clamp(target,0,2)]=null;}
  public void CycleLayer(){if(Current)Current.CycleLayer();else app.Report("Mặc áo dài trước khi chọn lớp");}
  public void ToggleArrangement(){if(Current)Current.ToggleArrangement();}
  public void ResetDrapeSelected(){if(Current)Current.ResetDrape();}
  public void CycleLength(){if(Current){float value=Current.LengthScale>=1.10f ? .85f : Current.LengthScale+.05f;Current.SetDesign(value,Current.WidthScale);}}
  public void CycleWidth(){if(Current){float value=Current.WidthScale>=1.14f ? .90f : Current.WidthScale+.05f;Current.SetDesign(Current.LengthScale,value);}}
 }
}
