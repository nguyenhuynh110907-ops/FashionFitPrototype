using UnityEngine;

namespace FashionFit
{
    // A conservative normal-only secondary membrane over the fitted front.
    // It cannot slide or enter the body: displacement is only forward from
    // the tested fit, with collar/placket/waist/armpits pinned. Not a full
    // sewn-panel cloth solver. The low grid never increases collision points.
    public sealed class TailoredSurfaceDynamics
    {
        const int Width=17,Height=19,Count=Width*Height;
        const float XMin=-.13f,XMax=.13f,YMin=1.15f,YMax=1.37f;
        readonly float[] q=new float[Count],previous=new float[Count],forceNormalY=new float[Count],inverseMass=new float[Count];
        readonly bool[] pinned=new bool[Count];
        readonly Mesh upper,detail;
        readonly RunwayAvatar body;
        Vector3[] rest,restDetail,vertices,detailVertices,normals,workingNormals,detailNormals,workingDetailNormals;
        Vector4[] tangents,workingTangents,detailTangents,workingDetailTangents;
        Vector3 previousAnchor,previousVelocity;
        bool anchorReady;
        int frame;
        ClothMaterialProfile material;
        public float MaxOffset {get;private set;}
        public int ParticleCount=>Count;
        public TailoredSurfaceDynamics(RunwayAvatar avatar,Mesh upperMesh,Mesh detailMesh,ClothMaterialProfile profile)
        {
            body=avatar;upper=upperMesh;detail=detailMesh;SetMaterial(profile);Rebase();
            var p=rest;var triangles=upper.triangles;
            for(int r=0;r<Height;r++)for(int c=0;c<Width;c++){
                int id=r*Width+c;float x=Mathf.Lerp(XMin,XMax,c/(float)(Width-1)),y=Mathf.Lerp(YMin,YMax,r/(float)(Height-1));
                pinned[id]=r==0||r==Height-1||c==0||c==Width-1||Mathf.Abs(x)<.025f;
                float z=float.NegativeInfinity;Vector3 normal=Vector3.forward;
                for(int k=0;k<triangles.Length;k+=3){
                    Vector3 a=p[triangles[k]],b=p[triangles[k+1]],d=p[triangles[k+2]];
                    float determinant=(b.y-d.y)*(a.x-d.x)+(d.x-b.x)*(a.y-d.y);if(Mathf.Abs(determinant)<1e-10f)continue;
                    float u=((b.y-d.y)*(x-d.x)+(d.x-b.x)*(y-d.y))/determinant,v=((d.y-a.y)*(x-d.x)+(a.x-d.x)*(y-d.y))/determinant,w=1-u-v;
                    if(u<0||v<0||w<0)continue;float hit=u*a.z+v*b.z+w*d.z;
                    if(hit>z){z=hit;normal=Vector3.Cross(b-a,d-a).normalized;if(normal.z<0)normal=-normal;}
                }
                forceNormalY[id]=float.IsNegativeInfinity(z)?0:normal.y;
            }
        }
        public void SetMaterial(ClothMaterialProfile profile)
        {
            material=profile;float mass=profile.arealMass*(XMax-XMin)/(Width-1)*(YMax-YMin)/(Height-1);
            for(int i=0;i<Count;i++)inverseMass[i]=1/Mathf.Max(mass,1e-7f);
        }
        public void Rebase()
        {
            rest=upper.vertices;restDetail=detail.vertices;vertices=(Vector3[])rest.Clone();detailVertices=(Vector3[])restDetail.Clone();normals=upper.normals;workingNormals=(Vector3[])normals.Clone();
            detailNormals=detail.normals;workingDetailNormals=(Vector3[])detailNormals.Clone();tangents=upper.tangents;workingTangents=(Vector4[])tangents.Clone();detailTangents=detail.tangents;workingDetailTangents=(Vector4[])detailTangents.Clone();
            upper.MarkDynamic();detail.MarkDynamic();Publish(true);
        }
        public void Step(float dt,Vector3 worldAir)
        {
            Vector3 anchor=body.transform.TransformPoint(body.Joint("Spine2")),velocity=Vector3.zero,acceleration=Vector3.zero;
            if(anchorReady){velocity=(anchor-previousAnchor)/dt;acceleration=Vector3.ClampMagnitude((velocity-previousVelocity)/dt,12);}
            previousAnchor=anchor;previousVelocity=velocity;anchorReady=true;
            Vector3 localAcceleration=body.transform.InverseTransformDirection(acceleration),localAir=body.transform.InverseTransformDirection(worldAir-velocity);
            float maximum=material.arealMass<.08f?.006f:material.arealMass<.2f?.0045f:.003f;
            float damping=Mathf.Exp(-material.damping*dt),pressure=.5f*1.225f*material.airDrag/material.arealMass*localAir.z*Mathf.Abs(localAir.z);
            for(int i=0;i<Count;i++){
                if(pinned[i]){q[i]=previous[i]=0;continue;}
                float old=q[i];float a=-9.81f*forceNormalY[i]-localAcceleration.z-localAcceleration.y*forceNormalY[i]+pressure;
                q[i]=Mathf.Clamp(q[i]+(q[i]-previous[i])*damping+Mathf.Clamp(a,-12,12)*dt*dt,0,maximum);previous[i]=old;
            }
            // Normal-space membrane tension, calibrated separately from bending.
            for(int pass=0;pass<4;pass++)for(int r=1;r<Height-1;r++)for(int c=1;c<Width-1;c++){
                int i=r*Width+c;if(pinned[i])continue;
                float target=(q[i-1]+q[i+1]+q[i-Width]+q[i+Width])*.25f;
                float compliance=material.bendCompliance/(dt*dt);
                float stiffness=inverseMass[i]/(inverseMass[i]+compliance);
                q[i]=Mathf.Clamp(Mathf.Lerp(q[i],target,stiffness*.65f),0,maximum);
            }
            MaxOffset=0;foreach(float value in q)MaxOffset=Mathf.Max(MaxOffset,value);
            Publish((++frame%4)==0);
        }
        float Offset(Vector3 p)
        {
            if(p.x<=XMin||p.x>=XMax||p.y<=YMin||p.y>=YMax||p.z<.065f)return 0;
            float x=(p.x-XMin)/(XMax-XMin)*(Width-1),y=(p.y-YMin)/(YMax-YMin)*(Height-1);
            int c=Mathf.FloorToInt(x),r=Mathf.FloorToInt(y);float u=x-c,v=y-r;
            return Mathf.Lerp(Mathf.Lerp(q[r*Width+c],q[r*Width+c+1],u),Mathf.Lerp(q[(r+1)*Width+c],q[(r+1)*Width+c+1],u),v);
        }
        void Publish(bool updateNormals)
        {
            for(int i=0;i<rest.Length;i++){vertices[i]=rest[i];vertices[i].z+=Offset(rest[i]);}
            upper.vertices=vertices;
            // Keep the princess-seam threads sewn to the moving fabric. Collar,
            // placket and hardware remain on their pinned reference surface.
            for(int i=0;i<restDetail.Length;i++){detailVertices[i]=restDetail[i];if(i>=4652)detailVertices[i].z+=Offset(restDetail[i]);}
            detail.vertices=detailVertices;
            if(updateNormals){
                UpdateNormals(rest,normals,workingNormals,tangents,workingTangents,0);
                UpdateNormals(restDetail,detailNormals,workingDetailNormals,detailTangents,workingDetailTangents,4652);
                upper.normals=workingNormals;upper.tangents=workingTangents;detail.normals=workingDetailNormals;detail.tangents=workingDetailTangents;
            }
        }
        void UpdateNormals(Vector3[] p,Vector3[] source,Vector3[] target,Vector4[] baseTangent,Vector4[] tangent,int start)
        {
            const float h=.002f;
            for(int i=0;i<p.Length;i++){
                Vector3 n=source[i];
                if(i>=start&&Offset(p[i])>0){float dx=(Offset(p[i]+Vector3.right*h)-Offset(p[i]-Vector3.right*h))/(2*h),dy=(Offset(p[i]+Vector3.up*h)-Offset(p[i]-Vector3.up*h))/(2*h);n=new Vector3(n.x-dx*n.z,n.y-dy*n.z,n.z).normalized;}
                target[i]=n;if(i>=tangent.Length)continue;Vector4 t=baseTangent[i];Vector3 d=(new Vector3(t.x,t.y,t.z)-n*Vector3.Dot(new Vector3(t.x,t.y,t.z),n)).normalized;tangent[i]=new Vector4(d.x,d.y,d.z,t.w);
            }
        }
    }
}
