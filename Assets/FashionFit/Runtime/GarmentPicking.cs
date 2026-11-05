using UnityEngine;
namespace FashionFit {
 public static class GarmentPicking {
  // Ericson closest-point regions; callers use the visible triangle surface,
  // rather than requiring the hand to happen to touch a sparse render vertex.
  public static Vector3 Triangle(Vector3 p,Vector3 a,Vector3 b,Vector3 c){Vector3 ab=b-a,ac=c-a,ap=p-a;float d1=Vector3.Dot(ab,ap),d2=Vector3.Dot(ac,ap);if(d1<=0&&d2<=0)return a;Vector3 bp=p-b;float d3=Vector3.Dot(ab,bp),d4=Vector3.Dot(ac,bp);if(d3>=0&&d4<=d3)return b;float vc=d1*d4-d3*d2;if(vc<=0&&d1>=0&&d3<=0)return a+ab*(d1/(d1-d3));Vector3 cp=p-c;float d5=Vector3.Dot(ab,cp),d6=Vector3.Dot(ac,cp);if(d6>=0&&d5<=d6)return c;float vb=d5*d2-d1*d6;if(vb<=0&&d2>=0&&d6<=0)return a+ac*(d2/(d2-d6));float va=d3*d6-d5*d4;if(va<=0&&d4-d3>=0&&d5-d6>=0)return b+(c-b)*((d4-d3)/((d4-d3)+(d5-d6)));float sum=va+vb+vc;if(Mathf.Abs(sum)<1e-12f)return a;return a+ab*(vb/sum)+ac*(vc/sum);}
  public static bool Mesh(Vector3[] vertices,int[] faces,Matrix4x4 matrix,Vector3 world,float reach,out Vector3 point,out int vertex){point=world;vertex=-1;Matrix4x4 inverse=matrix.inverse;Vector3 local=inverse.MultiplyPoint3x4(world);
   // The inverse-image of the world reach sphere fits this local AABB, even
   // with non-uniform scale/shear. Only reject triangles outside that box.
   Vector3 radius=new Vector3(new Vector3(inverse.m00,inverse.m01,inverse.m02).magnitude,new Vector3(inverse.m10,inverse.m11,inverse.m12).magnitude,new Vector3(inverse.m20,inverse.m21,inverse.m22).magnitude)*reach+Vector3.one*.00001f;
   Vector3 low=local-radius,high=local+radius;float best=reach*reach;for(int i=0;i<faces.Length;i+=3){int a=faces[i],b=faces[i+1],c=faces[i+2];Vector3 va=vertices[a],vb=vertices[b],vc=vertices[c];
   if((va.x<low.x&&vb.x<low.x&&vc.x<low.x)||(va.x>high.x&&vb.x>high.x&&vc.x>high.x)||(va.y<low.y&&vb.y<low.y&&vc.y<low.y)||(va.y>high.y&&vb.y>high.y&&vc.y>high.y)||(va.z<low.z&&vb.z<low.z&&vc.z<low.z)||(va.z>high.z&&vb.z>high.z&&vc.z>high.z))continue;
   Vector3 q=Triangle(local,va,vb,vc),w=matrix.MultiplyPoint3x4(q);float d=(w-world).sqrMagnitude;if(d<best){best=d;point=w;float da=(q-vertices[a]).sqrMagnitude,db=(q-vertices[b]).sqrMagnitude,dc=(q-vertices[c]).sqrMagnitude;vertex=da<db?(da<dc?a:c):(db<dc?b:c);}}return vertex>=0;}
 }
}
