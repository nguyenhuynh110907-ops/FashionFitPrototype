using System.Collections.Generic;
using UnityEngine;
namespace FashionFit {
 public sealed class HeadFitSupport:MonoBehaviour {
  public Transform Anchor{get;private set;}public RunwayAvatar Avatar{get;private set;}
  public Vector3 Centre{get;private set;}public Vector3 Radius{get;private set;}
  public Vector3[] Points{get;private set;}
  Transform frame;
  public void Initialize(RunwayAvatar avatar){
   Avatar=avatar;Transform head=avatar.FindJoint("Head");if(!head)head=avatar.transform;float top=avatar.BodyMesh.bounds.max.y;
   Vector3 low=Vector3.one*float.PositiveInfinity,high=Vector3.one*float.NegativeInfinity;
   foreach(var p in avatar.BodyVertices)if(p.y>top-.115f&&p.y<top-.055f){low=Vector3.Min(low,p);high=Vector3.Max(high,p);}
   Vector3 centre=new Vector3((low.x+high.x)*.5f,top-.113f,(low.z+high.z)*.5f);
   frame=new GameObject("Scalp contact frame").transform;frame.SetParent(head,false);frame.position=avatar.transform.TransformPoint(centre);frame.rotation=avatar.transform.rotation;Centre=Vector3.zero;
   Radius=new Vector3(Mathf.Max(.06f,(high.x-low.x)*.5f),.116f,Mathf.Max(.07f,(high.z-low.z)*.5f));var points=new List<Vector3>();float inflate=1;
   foreach(var p in avatar.BodyVertices)if(p.y>top-.135f){Vector3 q=frame.InverseTransformPoint(avatar.transform.TransformPoint(p));points.Add(q);float vertical=q.y/Radius.y;float section=Mathf.Sqrt(Mathf.Max(.015f,1-vertical*vertical));inflate=Mathf.Max(inflate,Mathf.Sqrt(q.x*q.x/(Radius.x*Radius.x)+q.z*q.z/(Radius.z*Radius.z))/section);}
   Radius=new Vector3(Radius.x*inflate+.002f,Radius.y,Radius.z*inflate+.002f);
   int stride=Mathf.Max(1,points.Count/128);var sampled=new List<Vector3>();for(int i=0;i<points.Count;i+=stride)sampled.Add(points[i]);Points=sampled.ToArray();
   Anchor=new GameObject("Measured head / hat anchor").transform;Anchor.SetParent(head,false);Anchor.position=avatar.transform.TransformPoint(new Vector3(centre.x,top-.035f,centre.z));Anchor.rotation=avatar.transform.rotation;
  }
  public Vector3 OpeningScale(Transform hat,Vector2 opening,float size){float y=frame.InverseTransformPoint(hat.position).y/Radius.y;float section=Mathf.Sqrt(Mathf.Max(.01f,1-y*y));return new Vector3(Mathf.Max(1,Radius.x*section/(opening.x*size)),1,Mathf.Max(1,Radius.z*section/(opening.y*size)));}
  public Vector3 ProjectWorld(Vector3 world){Vector3 q=frame.InverseTransformPoint(world);float y=q.y/Radius.y;if(Mathf.Abs(y)>=1)return world;float section=Mathf.Sqrt(Mathf.Max(.001f,1-y*y));float s=new Vector2(q.x/Radius.x,q.z/Radius.z).magnitude/section;if(s>=1)return world;if(s<1e-6f)q.z=Radius.z*section;else{q.x/=s;q.z/=s;}return frame.TransformPoint(q);}
  public float Clearance(Vector3 world){Vector3 q=frame.InverseTransformPoint(world);return (new Vector3(q.x/Radius.x,q.y/Radius.y,q.z/Radius.z).magnitude-1)*Mathf.Min(Radius.x,Mathf.Min(Radius.y,Radius.z));}
 }
}
