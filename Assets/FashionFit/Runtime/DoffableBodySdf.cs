using System;
using System.IO;
using UnityEngine;
namespace FashionFit
{
    // Offline mesh-derived collision data for the explicitly validated stationary pose.
    public sealed class DoffableBodySdf
    {
        static float[] values;static int nx,ny,nz;static Vector3 origin;static float cell;static string poseKey;
        Matrix4x4 toLocal,toWorld;
        public DoffableBodySdf(RunwayAvatar avatar)
        {
            if(values==null){
                var asset=Resources.Load<TextAsset>("Fitting/BlouseBodySdf");if(!asset)throw new InvalidOperationException("Thiếu dữ liệu va chạm áo cài nút");
                using(var r=new BinaryReader(new MemoryStream(asset.bytes))){
                    if(r.ReadInt32()!=0x42445331)throw new InvalidDataException("Invalid body SDF");
                    nx=r.ReadInt32();ny=r.ReadInt32();nz=r.ReadInt32();origin=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());cell=r.ReadSingle();poseKey=r.ReadString();
                    if(nx<2||ny<2||nz<2||nx*ny*nz>8000000||cell<.001f||cell>.02f)throw new InvalidDataException("Invalid SDF bounds");
                    values=new float[nx*ny*nz];var bytes=r.ReadBytes(values.Length*4);if(bytes.Length!=values.Length*4)throw new InvalidDataException("Truncated SDF");Buffer.BlockCopy(bytes,0,values,0,bytes.Length);
                }
            }
            var baked=new Mesh();var previous=avatar.Renderer.sharedMesh;
            try{avatar.Renderer.sharedMesh=avatar.BodyMesh;avatar.Renderer.BakeMesh(baked);string key=TailoredPoloStudy.GeometryKey(baked);if(key!=poseKey)throw new InvalidOperationException("Tư thế/cơ thể khác dữ liệu va chạm áo thử: "+key);}
            finally{avatar.Renderer.sharedMesh=previous;if(Application.isPlaying)UnityEngine.Object.Destroy(baked);else UnityEngine.Object.DestroyImmediate(baked);}
            Update(avatar.transform.localToWorldMatrix);
        }
        public void Update(Matrix4x4 bodyToWorld){toWorld=bodyToWorld;toLocal=bodyToWorld.inverse;}
        float At(int x,int y,int z)=>values[(x*ny+y)*nz+z];
        float Sample(Vector3 p,out Vector3 gradient)
        {
            Vector3 q=(p-origin)/cell;int x=Mathf.FloorToInt(q.x),y=Mathf.FloorToInt(q.y),z=Mathf.FloorToInt(q.z);gradient=Vector3.zero;
            if(x<0||y<0||z<0||x>=nx-1||y>=ny-1||z>=nz-1)return 1;
            float u=q.x-x,v=q.y-y,w=q.z-z;
            float a=At(x,y,z),b=At(x+1,y,z),c=At(x,y+1,z),d=At(x+1,y+1,z),e=At(x,y,z+1),f=At(x+1,y,z+1),g=At(x,y+1,z+1),h=At(x+1,y+1,z+1);
            float s0=Mathf.Lerp(Mathf.Lerp(a,b,u),Mathf.Lerp(c,d,u),v),s1=Mathf.Lerp(Mathf.Lerp(e,f,u),Mathf.Lerp(g,h,u),v);
            float distance=Mathf.Lerp(s0,s1,w);if(distance>=.008f)return distance;
            gradient.x=Mathf.Lerp(Mathf.Lerp(b-a,d-c,v),Mathf.Lerp(f-e,h-g,v),w)/cell;
            gradient.y=Mathf.Lerp(Mathf.Lerp(c-a,d-b,u),Mathf.Lerp(g-e,h-f,u),w)/cell;
            gradient.z=(s1-s0)/cell;return Mathf.Lerp(s0,s1,w);
        }
        public Vector3 Correction(Vector3 world)
        {
            const float margin=.008f;Vector3 p=toLocal.MultiplyPoint3x4(world),q=p;
            for(int n=0;n<3;n++){
                float distance=Sample(q,out Vector3 normal);if(distance>=margin)break;
                if(normal.sqrMagnitude<.05f){
                    float best=float.NegativeInfinity;Vector3 selected=Vector3.forward;
                    for(int axis=0;axis<3;axis++)for(int sign=-1;sign<=1;sign+=2){Vector3 direction=axis==0?Vector3.right:axis==1?Vector3.up:Vector3.forward;direction*=sign;float nearby=Sample(q+direction*cell*2,out _);if(nearby>best){best=nearby;selected=direction;}}normal=selected;
                }
                q+=normal.normalized*Mathf.Min(.04f,margin-distance);
            }
            return toWorld.MultiplyVector(q-p);
        }
    }
}
