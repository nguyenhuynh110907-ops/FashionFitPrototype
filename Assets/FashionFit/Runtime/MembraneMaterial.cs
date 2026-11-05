using System;
using UnityEngine;
namespace FashionFit {
 // SI units. Illustrative constitutive presets, NOT measured strength certificates.
 [Serializable] public sealed class MembraneMaterial {
  public string label;public float youngWarpPa,youngWeftPa,thicknessM,compressionRatio,viscousSeconds,recoverySeconds,arealMass,maxStrain;
  public float pullTravelM=.05f;public float virtualReferenceForceN=10,referenceGaugeM=.30f,referenceWidthM=.05f;
  public float CommandForce(float extension)=>virtualReferenceForceN*Mathf.Pow(Mathf.Clamp01(Mathf.Abs(extension)/Mathf.Max(.001f,pullTravelM)),1.5f);
  public float SheetWarp=>youngWarpPa*thicknessM;public float SheetWeft=>youngWeftPa*thicknessM;
  public static readonly MembraneMaterial[] Presets={
   new MembraneMaterial{label="Cotton dệt · thử 5 cm",pullTravelM=.05f,youngWarpPa=180000,youngWeftPa=120000,thicknessM=.0006f,compressionRatio=.025f,viscousSeconds=.12f,recoverySeconds=.8f,arealMass=.18f,maxStrain=.22f},
   new MembraneMaterial{label="Vải co giãn · thử 10 cm",pullTravelM=.10f,youngWarpPa=24000,youngWeftPa=18000,thicknessM=.0008f,compressionRatio=.012f,viscousSeconds=.18f,recoverySeconds=1.3f,arealMass=.16f,maxStrain=.48f},
   new MembraneMaterial{label="Nỉ định hình · thử 2,5 cm",pullTravelM=.025f,youngWarpPa=650000,youngWeftPa=500000,thicknessM=.002f,compressionRatio=.12f,viscousSeconds=.20f,recoverySeconds=1.8f,arealMass=.38f,maxStrain=.09f},
   new MembraneMaterial{label="Da mềm · thử 2 cm",pullTravelM=.02f,youngWarpPa=1500000,youngWeftPa=900000,thicknessM=.0012f,compressionRatio=.15f,viscousSeconds=.15f,recoverySeconds=1.1f,arealMass=.65f,maxStrain=.12f}
  };
  public float DirectionalYoung(Vector3 edge){float warp=edge.sqrMagnitude<1e-12f?.5f:edge.y*edge.y/edge.sqrMagnitude;return Mathf.Lerp(youngWeftPa,youngWarpPa,warp);}
  public float MembraneForce(float strain,Vector3 edge)=>DirectionalYoung(edge)*thicknessM*strain*(strain<0?compressionRatio:1);
  public float GripLimit(float radius)=>pullTravelM;
  public static float Recover(float value,float dt,float tau)=>value*Mathf.Exp(-Mathf.Max(0,dt)/Mathf.Max(.01f,tau));
  public ClothMaterialProfile ClothProfile()=>new ClothMaterialProfile{label=label,pullTravelM=pullTravelM,arealMass=arealMass,bendCompliance=1e-3f,damping=4,friction=.35f,roughness=.75f,stretchWarp=1/SheetWarp,stretchWeft=1/SheetWeft,shear=2/Mathf.Sqrt(SheetWarp*SheetWeft),airDrag=1.1f,youngWarpPa=youngWarpPa,youngWeftPa=youngWeftPa,thicknessM=thicknessM,compressionRatio=compressionRatio,recoverySeconds=recoverySeconds};
 }
 [Serializable] public struct MembraneReading {
  public float peakTensileStrain,peakCompressiveStrain;public bool hasStiffness,rms;public float tensileStrain,compressiveStrain,tensionNm,compressionNm;
  public string Summary=>(rms?"ε RMS kéo ":"ε kéo ")+(tensileStrain*100).ToString("F1")+"% · nén "+(compressiveStrain*100).ToString("F1")+"%"+(hasStiffness?" · T≈"+tensionNm.ToString("F1")+" N/m":" · chưa gán E");
 }
}
