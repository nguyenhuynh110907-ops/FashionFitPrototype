using UnityEngine;
namespace FashionFit {
 // Anatomical shoulder/sleeve topology replaces the old torso cylinder.
 // Uses the same visible surface for near Grip and deformation.
 public sealed class InteractiveShirt:MonoBehaviour {
  FashionFitApp app;RunwayAvatar avatar;BodyBoundSurface driver;Mesh authored;Material fabric;
  public float Looseness{get;private set;}=1;public float Length{get;private set;}=1;
  public bool SimulationReady=>driver&&driver.Surface;public int GrabCount=>SimulationReady?driver.Surface.GrabCount:0;public bool IsGrabbed=>GrabCount>0;
  public float ClothSpeed=>driver?driver.MaxSpeed:0;public int ProxyPenetrations=>0;public float MaxStretch=>SimulationReady?1+driver.Surface.Reading.tensileStrain:1;public string StretchDiagnostic=>SimulationReady?driver.Surface.Reading.Summary:"";
  public ElasticSurface Surface=>driver?driver.Surface:null;MembraneMaterial material=MembraneMaterial.Presets[0];
  public void Initialize(FashionFitApp owner,RunwayAvatar body,Material displayMaterial){app=owner;avatar=body;fabric=displayMaterial;Build();}
  void Build(){if(driver){driver.gameObject.SetActive(false);if(Application.isPlaying)Destroy(driver.gameObject);else DestroyImmediate(driver.gameObject);}if(authored){if(Application.isPlaying)Destroy(authored);else DestroyImmediate(authored);}authored=FittedGarmentGeometry.Shell(avatar,false,false);var points=authored.vertices;float neck=FittedGarmentGeometry.Neutral(avatar,"Neck").y;for(int i=0;i<points.Length;i++){float t=Mathf.Clamp01((neck-points[i].y)/.45f);points[i].x*=Mathf.Lerp(1,Looseness,t);points[i].z*=Mathf.Lerp(1,Looseness,t);points[i].y=neck+(points[i].y-neck)*Mathf.Lerp(1,Length,t);}authored.vertices=points;authored.RecalculateBounds();var go=new GameObject("Ivory actual grabbable shoulder and sleeve surface");go.transform.SetParent(transform,false);driver=go.AddComponent<BodyBoundSurface>();driver.Initialize(avatar,authored,fabric,"Female");driver.Surface.SetMaterial(material);}
  public void SetPhysicsMaterial(ClothMaterialProfile p){if(p==null)return;material=new MembraneMaterial{label=p.label,pullTravelM=p.pullTravelM>0?p.pullTravelM:.05f,youngWarpPa=p.youngWarpPa>0?p.youngWarpPa:180000,youngWeftPa=p.youngWeftPa>0?p.youngWeftPa:120000,thicknessM=p.thicknessM>0?p.thicknessM:.0006f,compressionRatio=p.compressionRatio>0?p.compressionRatio:.025f,recoverySeconds=p.recoverySeconds>0?p.recoverySeconds:.8f,viscousSeconds=.12f,arealMass=p.arealMass,maxStrain=.22f};if(Surface)Surface.SetMaterial(material);}
  public void SetAirVelocity(Vector3 velocity){}
  public void SetFit(float looseness,float length){Looseness=Mathf.Clamp(looseness,.94f,1.16f);Length=Mathf.Clamp(length,.94f,1.12f);Build();}
  public void ResetAdjustment(){Looseness=Length=1;Build();}
  public void ResetState(){if(Surface){Surface.ResetShape();driver.Step(1f/30);}}
  public void Step(float dt){} // Driven once by BodyBoundSurface.LateUpdate.
  public void BeginVRGrab(Vector3 p,Quaternion q){BeginVRGrab(0,p);}public bool BeginVRGrab(int slot,Vector3 p){bool ok=Surface&&Surface.BeginGrip(slot,p);if(ok)app.NotifyClothGrab(true);return ok;}
  public void MoveVRGrab(Vector3 p,Quaternion q){MoveVRGrab(0,p);}public void MoveVRGrab(int slot,Vector3 p){if(Surface)Surface.MoveGrip(slot,p);}
  public void EndVRGrab(int slot){if(Surface)Surface.EndGrip(slot);app.NotifyClothGrab(IsGrabbed);}public void EndVRGrab(){EndVRGrab(0);EndVRGrab(1);}
  void OnDestroy(){if(authored){if(Application.isPlaying)Destroy(authored);else DestroyImmediate(authored);}}
 }
}
