using System;using System.Collections.Generic;using UnityEngine;
namespace FashionFit {
 // Reduced, overdamped Kelvin-Voigt deformation: eta*u' + k*(u-u_eq)=0.
 // Exact exponential relaxation is independent of frame rate. This is not FEM or tearing.
 public sealed class ElasticSurface:MonoBehaviour {
  sealed class Piece{public Mesh mesh,original;public MeshFilter filter;public Vector3[] rest,basePoints,offset,output;public int[] triangles;public Matrix4x4 toRoot,fromRoot;}
  struct Edge{public int piece,a,b;public float fitRatio;}
  readonly List<Piece> pieces=new List<Piece>();readonly List<Edge> edges=new List<Edge>();
  readonly int[] heldPiece={-1,-1},heldVertex={-1,-1};readonly Vector3[] start=new Vector3[2],target=new Vector3[2];
  public ContactGarment ContactMotion{get;private set;}
  public void SetContactMotion(ContactGarment motion){ContactMotion=motion;}
  public MembraneMaterial Material{get;private set;}=MembraneMaterial.Presets[0];
  readonly float[] requested={0,0};bool balancedCommand;float balancedRequest,balancedApplied;
  public float RequestedTravelM=>balancedCommand?balancedRequest:Mathf.Max(requested[0],requested[1]);
  public float AppliedTravelM=>ContactMotion&&ContactMotion.Active?ContactMotion.AcceptedTravel:balancedCommand?balancedApplied:Mathf.Max(heldPiece[0]>=0?(target[0]-start[0]).magnitude:0,heldPiece[1]>=0?(target[1]-start[1]).magnitude:0);
  public bool BalancedCommand=>balancedCommand&&GrabCount==2;
  public float GaugeLengthM=>GrabCount==2?Vector3.Distance(pieces[heldPiece[0]].toRoot.MultiplyPoint3x4(pieces[heldPiece[0]].basePoints[heldVertex[0]]),pieces[heldPiece[1]].toRoot.MultiplyPoint3x4(pieces[heldPiece[1]].basePoints[heldVertex[1]])):Material.referenceGaugeM;
  HeadFitSupport head;bool dirty=true,sleeping;float rawOffset,lastPeak;
  public MembraneReading Reading{get;private set;}public HeadFitSupport Head{get=>head;set{if(head!=value){if(head&&!value)foreach(var piece in pieces)for(int i=0;i<piece.rest.Length;i++){Vector3 baseline=new Vector3(piece.rest[i].x*Size,piece.rest[i].y,piece.rest[i].z*Size);piece.offset[i]=piece.output[i]-baseline;}head=value;dirty=true;}}}public float Size{get;private set;}=1;
  public bool PoseResetRequested{get;private set;}
  public void EnablePoseDriver(){DynamicBase=true;}
  public void PoseDrive(out int a,out int b,out Vector3 da,out Vector3 db){a=heldPiece[0]>=0?heldVertex[0]:-1;b=heldPiece[1]>=0?heldVertex[1]:-1;da=target[0]-start[0];db=target[1]-start[1];}
  public bool NeedsUpdate=>(ContactMotion&&ContactMotion.Active)||dirty||!sleeping||GrabCount>0;
  public bool OutsideLinearRange=>lastPeak>Material.maxStrain;
  public int GrabCount=>(heldPiece[0]>=0?1:0)+(heldPiece[1]>=0?1:0);
  public float MaximumOffset{get;private set;}public int VertexCount{get;private set;}
  public Vector2 CrownOpening=new Vector2(.094f,.105f);
  public Func<int,Vector3,Vector3> IndexedProject;public float SuspensionY;
  public Func<Vector3,Vector3> BodyProject;public bool DynamicBase{get;private set;}
  public void Initialize(MembraneMaterial material){Material=material;
   foreach(var f in GetComponentsInChildren<MeshFilter>())if(f.sharedMesh){var p=new Piece{original=f.sharedMesh,mesh=Instantiate(f.sharedMesh),filter=f};p.mesh.name=f.sharedMesh.name+" elastic instance";p.mesh.MarkDynamic();f.sharedMesh=p.mesh;p.rest=p.mesh.vertices;p.basePoints=(Vector3[])p.rest.Clone();p.offset=new Vector3[p.rest.Length];p.output=new Vector3[p.rest.Length];p.triangles=p.mesh.triangles;p.toRoot=transform.worldToLocalMatrix*f.transform.localToWorldMatrix;p.fromRoot=p.toRoot.inverse;int piece=pieces.Count;pieces.Add(p);VertexCount+=p.rest.Length;
    var unique=new HashSet<long>();var fabricTriangles=new List<int[]>();var renderer=f.GetComponent<MeshRenderer>();for(int sub=0;sub<p.mesh.subMeshCount;sub++){var mat=renderer&&renderer.sharedMaterials.Length>sub?renderer.sharedMaterials[sub]:null;string materialName=mat?mat.name.ToLowerInvariant():"";bool decoration=materialName.Contains("thread")||materialName.Contains("stitch")||materialName.Contains("buckle")||materialName.Contains("braid")||materialName.Contains("eyelet");if(!decoration&&(!mat||!mat.HasProperty("_Metallic")||mat.GetFloat("_Metallic")<.5f))fabricTriangles.Add(p.mesh.GetTriangles(sub));}foreach(int[] tri in fabricTriangles)for(int i=0;i<tri.Length;i+=3)for(int e=0;e<3;e++){int a=tri[i+e],b=tri[i+(e+1)%3];long k=((long)Mathf.Min(a,b)<<32)|(uint)Mathf.Max(a,b);if(unique.Add(k)&&(p.rest[a]-p.rest[b]).sqrMagnitude>.000225f)edges.Add(new Edge{piece=piece,a=a,b=b});}
   }
  }
  public void SetMaterial(MembraneMaterial material){Material=material;dirty=true;}
  public void SetSize(float size){Size=Mathf.Clamp(size,.75f,1.20f);dirty=true;}
  public void SetBase(int piece,Vector3[] positions){DynamicBase=true;Array.Copy(positions,pieces[piece].basePoints,positions.Length);}
  bool Locate(Vector3 world,float distance,out Vector3 point,out int piece,out int vertex){point=world;piece=vertex=-1;float best=distance;for(int k=0;k<pieces.Count;k++){var p=pieces[k];var bounds=p.mesh.bounds;bounds.Expand(distance*2);if(!bounds.Contains(p.filter.transform.InverseTransformPoint(world)))continue;if(GarmentPicking.Mesh(p.output,p.triangles,p.filter.transform.localToWorldMatrix,world,best,out Vector3 q,out int v)){float d=Vector3.Distance(world,q);best=d;point=q;piece=k;vertex=v;}}return piece>=0;}
  public bool Nearest(Vector3 world,float distance,out Vector3 point)=>Locate(world,distance,out point,out _,out _);
  public bool BeginGrip(int slot,Vector3 world){if(slot<0||slot>1||heldPiece[slot]>=0||!Locate(world,.12f,out Vector3 point,out int pi,out int vi))return false;if(pi==heldPiece[1-slot]&&vi==heldVertex[1-slot])return false;balancedCommand=false;requested[slot]=0;heldPiece[slot]=pi;heldVertex[slot]=vi;start[slot]=transform.InverseTransformPoint(world);target[slot]=start[slot];if(ContactMotion&&!ContactMotion.BeginGrip(slot,vi,world)){heldPiece[slot]=-1;return false;}return true;}
  public float InteractionLimit=>Material.pullTravelM;
  public Vector3 HandForce0{get;private set;}public Vector3 HandForce1{get;private set;}public Vector3 AnchorReaction=>-(HandForce0+HandForce1);
  public void MoveBalancedGrips(float spanChange,Vector3 axisWorld){if(GrabCount!=2)return;balancedCommand=true;balancedRequest=spanChange;balancedApplied=Mathf.Clamp(spanChange,-Material.pullTravelM,Material.pullTravelM);Vector3 axis=transform.InverseTransformDirection(axisWorld.normalized);float extension=Mathf.Clamp(spanChange,-Material.pullTravelM,Material.pullTravelM);target[0]=start[0]-axis*extension*.5f;target[1]=start[1]+axis*extension*.5f;float force=Material.CommandForce(extension);HandForce0=-axis*force*Mathf.Sign(extension);HandForce1=-HandForce0;}
  public void MoveGrip(int slot,Vector3 world){if(slot>=0&&slot<2&&heldPiece[slot]>=0&&RunwayVRHands.ValidPose(world,Quaternion.identity)){if(ContactMotion){requested[slot]=(transform.InverseTransformPoint(world)-start[slot]).magnitude;ContactMotion.MoveGrip(slot,world);return;}balancedCommand=false;requested[slot]=(transform.InverseTransformPoint(world)-start[slot]).magnitude;Vector3 delta=Vector3.ClampMagnitude(transform.InverseTransformPoint(world)-start[slot],InteractionLimit);target[slot]=start[slot]+delta;Vector3 force=delta.sqrMagnitude>1e-10f?delta.normalized*Material.CommandForce(delta.magnitude):Vector3.zero;if(slot==0)HandForce0=force;else HandForce1=force;}}
  public void EndGrip(int slot){if(slot>=0&&slot<2){if(ContactMotion)ContactMotion.EndGrip(slot);heldPiece[slot]=-1;requested[slot]=0;balancedCommand=false;if(slot==0)HandForce0=Vector3.zero;else HandForce1=Vector3.zero;}}
  public void ResetShape(){if(ContactMotion)ContactMotion.ResetPlacement();EndGrip(0);EndGrip(1);if(DynamicBase){PoseResetRequested=true;dirty=true;return;}foreach(var p in pieces)Array.Clear(p.offset,0,p.offset.Length);Step(.1f);}
  void LateUpdate(){if(!DynamicBase&&NeedsUpdate)Step(Time.deltaTime);}
  public void Step(float dt){if(pieces.Count==0||dt<=0)return;dt=Mathf.Min(dt,.05f);float blend=1-Mathf.Exp(-dt/(GrabCount>0?Material.viscousSeconds:Material.recoverySeconds));MaximumOffset=rawOffset=0;Vector3 gravity=Physics.gravity;Vector3 headScale=Head?Head.OpeningScale(transform,CrownOpening,Size):Vector3.one;
   foreach(var p in pieces){Matrix4x4 worldMatrix=p.filter.transform.localToWorldMatrix,localMatrix=p.filter.transform.worldToLocalMatrix;for(int i=0;i<p.rest.Length;i++){
     Vector3 baseline=DynamicBase?p.basePoints[i]:new Vector3(p.rest[i].x*Size,p.rest[i].y,p.rest[i].z*Size);Vector3 root=p.toRoot.MultiplyPoint3x4(baseline),goal=Vector3.zero;if(DynamicBase&&SuspensionY>0){float drop=Mathf.Max(0,SuspensionY-baseline.y);goal=transform.InverseTransformVector(gravity)*(Material.arealMass*drop*drop/(2*Mathf.Max(1,Material.SheetWarp)));}
     for(int h=0;h<2;h++)if(heldPiece[h]>=0){Vector3 hit=pieces[heldPiece[h]].toRoot.MultiplyPoint3x4(pieces[heldPiece[h]].basePoints[heldVertex[h]]);float t=Mathf.Clamp01(1-Vector3.Distance(root,hit)/.22f);float weight=t*t*(3-2*t);Vector3 delta=Vector3.ClampMagnitude(target[h]-start[h],InteractionLimit);goal+=delta*weight;}
     Vector3 localGoal=p.fromRoot.MultiplyVector(goal);p.offset[i]=Vector3.Lerp(p.offset[i],localGoal,blend);rawOffset=Mathf.Max(rawOffset,(localGoal-p.offset[i]).sqrMagnitude);Vector3 pos=Vector3.Scale(baseline,headScale)+p.offset[i],world=worldMatrix.MultiplyPoint3x4(pos);
     if(Head)world=Head.ProjectWorld(world);if(BodyProject!=null)world=BodyProject(world);if(IndexedProject!=null)world=IndexedProject(i,world);
     p.output[i]=localMatrix.MultiplyPoint3x4(world);MaximumOffset=Mathf.Max(MaximumOffset,(p.output[i]-baseline).magnitude);
    }
    // Surface interior samples, not just vertices, receive the fitted contact.
    if(Head||BodyProject!=null||IndexedProject!=null)for(int pass=0;pass<5;pass++)for(int k=0;k<p.triangles.Length;k+=3){int a=p.triangles[k],b=p.triangles[k+1],c=p.triangles[k+2];Vector3 local=(p.output[a]+p.output[b]+p.output[c])/3,world=worldMatrix.MultiplyPoint3x4(local),q=Head?Head.ProjectWorld(world):world;if(BodyProject!=null)q=BodyProject(q);if(IndexedProject!=null)q=IndexedProject(p.rest.Length+k/3,q);Vector3 d=localMatrix.MultiplyVector(q-world);p.output[a]+=d;p.output[b]+=d;p.output[c]+=d;}
    if(Head||BodyProject!=null||IndexedProject!=null)for(int i=0;i<p.output.Length;i++){Vector3 world=worldMatrix.MultiplyPoint3x4(p.output[i]);if(Head)world=Head.ProjectWorld(world);if(BodyProject!=null)world=BodyProject(world);if(IndexedProject!=null)world=IndexedProject(i,world);p.output[i]=localMatrix.MultiplyPoint3x4(world);}
    p.mesh.vertices=p.output;p.mesh.RecalculateNormals();p.mesh.RecalculateBounds();p.mesh.RecalculateTangents();
   }
   UpdateReading();
  }
  void UpdateReading(){
   float weight=0,pull2=0,push2=0,tension2=0,compression2=0;var value=new MembraneReading{hasStiffness=true};for(int index=0;index<edges.Count;index++){var e=edges[index];var p=pieces[e.piece];Vector3 restEdge=new Vector3((p.rest[e.b].x-p.rest[e.a].x)*(DynamicBase?1:Size),p.rest[e.b].y-p.rest[e.a].y,(p.rest[e.b].z-p.rest[e.a].z)*(DynamicBase?1:Size));float len=restEdge.magnitude;if(DynamicBase){float anchorLength=(p.basePoints[e.b]-p.basePoints[e.a]).magnitude;if(e.fitRatio<=0){e.fitRatio=(p.output[e.b]-p.output[e.a]).magnitude/Mathf.Max(.00001f,anchorLength);edges[index]=e;}len=anchorLength*e.fitRatio;}if(len<.015f)continue;float strain=((p.output[e.b]-p.output[e.a]).magnitude-len)/len;float force=Material.MembraneForce(strain,restEdge);value.tensileStrain=Mathf.Max(value.tensileStrain,strain);value.compressiveStrain=Mathf.Max(value.compressiveStrain,-strain);value.tensionNm=Mathf.Max(value.tensionNm,force);value.compressionNm=Mathf.Max(value.compressionNm,-force);float w=len*len;weight+=w;if(strain>0){pull2+=strain*strain*w;tension2+=force*force*w;}else{push2+=strain*strain*w;compression2+=force*force*w;}}lastPeak=value.tensileStrain;value.peakTensileStrain=value.tensileStrain;value.peakCompressiveStrain=value.compressiveStrain;if(DynamicBase&&weight>0){value.rms=true;value.tensileStrain=Mathf.Sqrt(pull2/weight);value.compressiveStrain=Mathf.Sqrt(push2/weight);value.tensionNm=Mathf.Sqrt(tension2/weight);value.compressionNm=Mathf.Sqrt(compression2/weight);}Reading=value;dirty=false;sleeping=GrabCount==0&&rawOffset<2.5e-9f;
  }
  public int[] PoseReadingPairs(){if(pieces.Count!=1)throw new InvalidOperationException("Native pose readings require one mesh");var pairs=new int[edges.Count*2];for(int i=0;i<edges.Count;i++){pairs[i*2]=edges[i].a;pairs[i*2+1]=edges[i].b;}return pairs;}
  public void AcceptPoseReading(Vector3[] positions,Vector3[] anchors,float residual,float maximum,MembraneReading reading){var p=pieces[0];Array.Copy(positions,p.output,positions.Length);Array.Copy(anchors,p.basePoints,anchors.Length);p.mesh.vertices=p.output;p.mesh.RecalculateNormals();p.mesh.RecalculateBounds();p.mesh.RecalculateTangents();rawOffset=residual;MaximumOffset=maximum;PoseResetRequested=false;Reading=reading;lastPeak=reading.peakTensileStrain;dirty=false;sleeping=GrabCount==0&&rawOffset<2.5e-9f;}
  public void AcceptPose(Vector3[] positions,Vector3[] anchors,float residual,float maximum){var p=pieces[0];Array.Copy(positions,p.output,positions.Length);Array.Copy(anchors,p.basePoints,anchors.Length);p.mesh.vertices=p.output;p.mesh.RecalculateNormals();p.mesh.RecalculateBounds();p.mesh.RecalculateTangents();rawOffset=residual;MaximumOffset=maximum;PoseResetRequested=false;UpdateReading();}
  public Vector3[] CurrentPoints=>pieces[0].output;
  public Mesh MeshAt(int index)=>pieces[index].mesh;
  void OnDestroy(){foreach(var p in pieces)if(p.mesh){if(Application.isPlaying)Destroy(p.mesh);else DestroyImmediate(p.mesh);}}
 }
}
