using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.Rendering;

namespace FashionFit
{
    // Authored 2D-reference study of Gucci 864361, NOT an official Gucci mesh or
    // a trained image-to-3D reconstruction. Hidden construction is estimated.
    public sealed class TailoredPoloStudy : MonoBehaviour
    {
        RunwayAvatar body;
        Mesh upperMesh,detailMesh;
        Vector3[] fittedUpperNormals;
        Material trimFabric,stitchFabric;
        readonly List<Material> ownedMaterials=new List<Material>();
        public SkinnedMeshRenderer UpperSurface {get;private set;}
        public SkinnedMeshRenderer DetailSurface {get;private set;}
        public bool DetailsVisible=>DetailSurface&&DetailSurface.enabled;
        public const int ButtonCount=4;
        public bool ContactFitApplied {get;private set;}
        public string RawUpperKey {get;private set;}
        public string RawDetailKey {get;private set;}
#if UNITY_EDITOR
        public static bool ExportUnfittedGeometry;
#endif
#pragma warning disable 0649
        [Serializable] sealed class ContactFit {public string upper_key,detail_key;public float[] upper,detail,upper_normals;}
#pragma warning restore 0649
        public static string GeometryKey(Mesh mesh)
        {
            using(var stream=new MemoryStream()){using(var writer=new BinaryWriter(stream,System.Text.Encoding.UTF8,true)){
                Vector3[] p=mesh.vertices;int[] t=mesh.triangles;writer.Write(p.Length);foreach(var v in p){writer.Write(v.x);writer.Write(v.y);writer.Write(v.z);}writer.Write(t.Length);foreach(int i in t)writer.Write(i);}
                using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(stream.ToArray())).Replace("-","").ToLowerInvariant();}
        }
        void ApplyContactFit()
        {
            RawUpperKey=GeometryKey(upperMesh);RawDetailKey=GeometryKey(detailMesh);
#if UNITY_EDITOR
            if(ExportUnfittedGeometry)return;
#endif
            if(body.IsOriginal)return;var asset=Resources.Load<TextAsset>("Fitting/polo-contact-fit");if(!asset)return;
            var fit=JsonUtility.FromJson<ContactFit>(asset.text);
            if(fit==null||fit.upper_key!=RawUpperKey||fit.detail_key!=RawDetailKey||fit.upper==null||fit.detail==null||fit.upper.Length!=upperMesh.vertexCount*3||fit.detail.Length!=detailMesh.vertexCount*3)return;
            Action<Mesh,float[]> apply=(mesh,values)=>{var p=new Vector3[values.Length/3];for(int i=0;i<p.Length;i++){p[i]=new Vector3(values[i*3],values[i*3+1],values[i*3+2]);if(float.IsNaN(p[i].x)||float.IsNaN(p[i].y)||float.IsNaN(p[i].z)||float.IsInfinity(p[i].x)||float.IsInfinity(p[i].y)||float.IsInfinity(p[i].z))throw new InvalidDataException("Invalid contact-fit cache");}mesh.vertices=p;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();};
            apply(upperMesh,fit.upper);apply(detailMesh,fit.detail);ContactFitApplied=true;
            if(fit.upper_normals!=null&&fit.upper_normals.Length==upperMesh.vertexCount*3){fittedUpperNormals=new Vector3[upperMesh.vertexCount];for(int i=0;i<fittedUpperNormals.Length;i++){var n=new Vector3(fit.upper_normals[i*3],fit.upper_normals[i*3+1],fit.upper_normals[i*3+2]);if(float.IsNaN(n.sqrMagnitude)||float.IsInfinity(n.sqrMagnitude)||n.sqrMagnitude<.5f)throw new InvalidDataException("Invalid fitted front normal");fittedUpperNormals[i]=n.normalized;}}
        }

        struct V {public Vector3 p,n;public BoneWeight w;}
        sealed class Builder
        {
            public readonly List<Vector3> p=new List<Vector3>();
            public readonly List<Vector2> uv=new List<Vector2>();
            public readonly List<BoneWeight> weights=new List<BoneWeight>();
            public readonly List<int>[] triangles;
            public Builder(int materials){triangles=new List<int>[materials];for(int i=0;i<materials;i++)triangles[i]=new List<int>();}
            public int Add(Vector3 point,Vector2 tex,BoneWeight weight){int i=p.Count;p.Add(point);uv.Add(tex);weights.Add(weight);return i;}
            public void Tri(int a,int b,int c,int slot){triangles[slot].Add(a);triangles[slot].Add(b);triangles[slot].Add(c);}
            public void Quad(int a,int b,int c,int d,int slot){Tri(a,b,c,slot);Tri(a,c,d,slot);}
            public Mesh Mesh(string name,Matrix4x4[] bind){var m=new Mesh{name=name,indexFormat=IndexFormat.UInt32};m.SetVertices(p);m.SetUVs(0,uv);m.boneWeights=weights.ToArray();m.bindposes=bind;m.subMeshCount=triangles.Length;for(int i=0;i<triangles.Length;i++)m.SetTriangles(triangles[i],i);m.RecalculateNormals();m.RecalculateTangents();m.RecalculateBounds();return m;}
        }
        static BoneWeight Blend(BoneWeight a,BoneWeight b,float t)
        {
            var pairs=new Dictionary<int,float>();
            Action<int,float> add=(id,w)=>{if(w<=0)return;pairs.TryGetValue(id,out float old);pairs[id]=old+w;};
            add(a.boneIndex0,a.weight0*(1-t));add(a.boneIndex1,a.weight1*(1-t));add(a.boneIndex2,a.weight2*(1-t));add(a.boneIndex3,a.weight3*(1-t));
            add(b.boneIndex0,b.weight0*t);add(b.boneIndex1,b.weight1*t);add(b.boneIndex2,b.weight2*t);add(b.boneIndex3,b.weight3*t);
            var list=new List<KeyValuePair<int,float>>(pairs);list.Sort((x,y)=>y.Value.CompareTo(x.Value));
            int[] ids=new int[4];float[] wts=new float[4];float sum=0;for(int i=0;i<Math.Min(4,list.Count);i++){ids[i]=list[i].Key;wts[i]=list[i].Value;sum+=wts[i];}
            return new BoneWeight{boneIndex0=ids[0],boneIndex1=ids[1],boneIndex2=ids[2],boneIndex3=ids[3],weight0=wts[0]/sum,weight1=wts[1]/sum,weight2=wts[2]/sum,weight3=wts[3]/sum};
        }
        static List<V> Clip(List<V> polygon,Vector3 normal,float distance)
        {
            var output=new List<V>();if(polygon.Count==0)return output;
            V previous=polygon[polygon.Count-1];float previousD=Vector3.Dot(previous.p,normal)+distance;
            foreach(V current in polygon){float d=Vector3.Dot(current.p,normal)+distance;
                if((d>=0)!=(previousD>=0)){float t=previousD/(previousD-d);output.Add(new V{p=Vector3.Lerp(previous.p,current.p,t),n=Vector3.Lerp(previous.n,current.n,t).normalized,w=Blend(previous.w,current.w,t)});}
                if(d>=0)output.Add(current);previous=current;previousD=d;
            }return output;
        }
        public void Initialize(RunwayAvatar avatar,Material fabric)
        {
            body=avatar;BuildUpper();var detail=new Builder(3);
            BuildCollar(detail);BuildPlacket(detail);BuildButtons(detail);BuildSeams(detail);
            detailMesh=detail.Mesh("Polo collar, placket, four GG-study buttons and tailoring seams",body.GarmentBindposes);
            ApplyContactFit();
            SmoothShellNormals(upperMesh);
            if(fittedUpperNormals!=null){upperMesh.normals=fittedUpperNormals;upperMesh.RecalculateTangents();}
            Material collar=Fabric("Folded collar and placket",new Color(.030f,.031f,.033f),.86f);
            Material thread=Fabric("Fine topstitch and seam",new Color(.065f,.064f,.060f),.82f);
            trimFabric=collar;stitchFabric=thread;
            Material gold=new Material(Shader.Find("Standard")){name="Gold-tone Double-G study detail",color=new Color(.65f,.46f,.20f)};gold.SetFloat("_Metallic",.85f);gold.SetFloat("_Glossiness",.72f);ownedMaterials.Add(gold);
            UpperSurface=Skin("Fitted short sleeve shell",upperMesh,new[]{fabric});DetailSurface=Skin("Collar stand, fold and hardware",detailMesh,new[]{collar,thread,gold});
        }
        static void SmoothShellNormals(Mesh mesh)
        {
            // UV/weight splits must not create visible facets on a continuous
            // cloth shell. Average area-weighted normals at coincident points;
            // keep positions, UVs, skin weights and triangles intact.
            Vector3[] p=mesh.vertices;int[] t=mesh.triangles;var keys=new Vector3Int[p.Length];var sums=new Dictionary<Vector3Int,Vector3>();
            for(int i=0;i<p.Length;i++)keys[i]=new Vector3Int(Mathf.RoundToInt(p[i].x*100000),Mathf.RoundToInt(p[i].y*100000),Mathf.RoundToInt(p[i].z*100000));
            for(int i=0;i<t.Length;i+=3){Vector3 n=Vector3.Cross(p[t[i+1]]-p[t[i]],p[t[i+2]]-p[t[i]]);for(int corner=0;corner<3;corner++){var key=keys[t[i+corner]];sums.TryGetValue(key,out Vector3 sum);sums[key]=sum+n;}}
            var normals=new Vector3[p.Length];for(int i=0;i<p.Length;i++)normals[i]=sums.TryGetValue(keys[i],out Vector3 n)&&n.sqrMagnitude>1e-16f?n.normalized:Vector3.up;
            mesh.normals=normals;mesh.RecalculateTangents();
        }
        Material Fabric(string name,Color color,float roughness){var m=new Material(Resources.Load<Shader>("Runway/DressFabric")){name=name};m.SetColor("_Color",color);m.SetFloat("_Glossiness",1-roughness);m.SetFloat("_Weave",.35f);ownedMaterials.Add(m);return m;}
        SkinnedMeshRenderer Skin(string name,Mesh mesh,Material[] materials)
        {
            var go=new GameObject(name);go.transform.SetParent(transform,false);var r=go.AddComponent<SkinnedMeshRenderer>();r.sharedMesh=mesh;r.sharedMaterials=materials;r.bones=body.Bones;r.rootBone=body.RootBone;r.quality=SkinQuality.Bone4;r.shadowCastingMode=ShadowCastingMode.On;
            SkinnedBounds.SetFromModel(r,body.transform,mesh.bounds,.09f);return r;
        }
        Vector3 Dressed(Vector3 p,Vector3 normal)
        {
            Vector3 q=p+normal*.010f;
            if(p.y<1.445f&&Mathf.Abs(p.x)<.215f){float z=q.z-BodyFitProfile.CentreZ(q.y),r=new Vector2(q.x,z).magnitude,angle=Mathf.Atan2(z,q.x);
                float ease=Mathf.Lerp(.010f,.012f,Mathf.InverseLerp(BodyFitProfile.WaistY,1.36f,q.y));
                float rx=Mathf.Max(BodyFitProfile.Radius(q.y,0),BodyFitProfile.Radius(q.y,Mathf.PI))+ease;
                float rz=BodyFitProfile.Radius(q.y,z>=0?Mathf.PI*.5f:Mathf.PI*1.5f)+ease;
                float ellipse=1/Mathf.Sqrt(Mathf.Pow(Mathf.Cos(angle)/rx,2)+Mathf.Pow(Mathf.Sin(angle)/rz,2));
                float minimum=Mathf.Max(ellipse,BodyFitProfile.Radius(q.y,angle)+.009f);
                float bust=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.18f,1.225f,q.y))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.30f,1.35f,q.y)));
                float wrap=1-.75f*bust*Mathf.Clamp01(Mathf.Sin(angle)*2);
                float target=Mathf.Lerp(r,Mathf.Max(r,minimum),wrap*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.38f,1.445f,q.y))));
                if(r>1e-6f){q.x*=target/r;q.z=BodyFitProfile.CentreZ(q.y)+z*target/r;}}
            return q;
        }
        void BuildUpper()
        {
            var source=body.BodyMesh;Vector3[] p=source.vertices,n=source.normals;BoneWeight[] w=source.boneWeights;var b=new Builder(1);
            Vector3 la=body.Joint("LeftArm"),le=body.Joint("LeftForeArm"),ra=body.Joint("RightArm"),re=body.Joint("RightForeArm");
            Vector3 ln=(le-la).normalized,rn=(re-ra).normalized,lc=Vector3.Lerp(la,le,.53f),rc=Vector3.Lerp(ra,re,.53f);
            int[] tri=source.triangles;
            for(int i=0;i<tri.Length;i+=3){var polygon=new List<V>();for(int k=0;k<3;k++){int id=tri[i+k];polygon.Add(new V{p=p[id],n=n[id],w=w[id]});}
                polygon=Clip(polygon,Vector3.up,-(BodyFitProfile.WaistY-.018f));polygon=Clip(polygon,Vector3.down,1.52f);
                // Partition at the torso/arm boundary: an arm cut plane must
                // never slice away the lower torso on its other side.
                var centre=Clip(Clip(Clip(polygon,Vector3.right,.073f),Vector3.left,.073f),Vector3.down,1.465f);
                var shoulderL=Clip(Clip(polygon,Vector3.right,-.073f),Vector3.left,.18f);
                var shoulderR=Clip(Clip(polygon,Vector3.left,-.073f),Vector3.right,.18f);
                var left=Clip(Clip(polygon,Vector3.right,-.18f),-ln,Vector3.Dot(ln,lc));
                var right=Clip(Clip(polygon,Vector3.left,-.18f),-rn,Vector3.Dot(rn,rc));
                foreach(var part in new[]{centre,shoulderL,shoulderR,left,right}){
                    if(part.Count<3)continue;int first=b.p.Count;
                    foreach(V v in part){Vector3 q=Dressed(v.p,v.n);b.Add(q,new Vector2(q.x/.5f+.5f,(q.y-1.10f)/.38f),v.w);}
                    for(int k=1;k<part.Count-1;k++)b.Tri(first,first+k,first+k+1,0);
                }
            }
            // Merge only exact coincident vertices with identical UV and weights.
            // Smooth normals must describe the garment, not the underlying skin.
            var compact=new Builder(1);var groups=new Dictionary<Vector3,List<int>>();var remap=new int[b.p.Count];
            for(int i=0;i<b.p.Count;i++){
                if(!groups.TryGetValue(b.p[i],out List<int> group)){group=new List<int>();groups[b.p[i]]=group;}
                int found=-1;foreach(int id in group)if(compact.uv[id].Equals(b.uv[i])&&compact.weights[id].Equals(b.weights[i])){found=id;break;}
                if(found<0){found=compact.Add(b.p[i],b.uv[i],b.weights[i]);group.Add(found);}remap[i]=found;
            }
            foreach(int index in b.triangles[0])compact.triangles[0].Add(remap[index]);
            upperMesh=compact.Mesh("Closed tailored torso and clipped short sleeves",body.GarmentBindposes);
        }
        BoneWeight Weight(Vector3 p)
        {
            int hips=Array.IndexOf(body.Bones,body.RootBone);if(hips<0)hips=0;int spine=body.BoneIndex("Spine"),chest=body.BoneIndex("Spine2");
            float t=Mathf.InverseLerp(BodyFitProfile.WaistY,1.335f,p.y);
            if(t<.32f){float f=Mathf.SmoothStep(0,1,t/.32f);return new BoneWeight{boneIndex0=hips,weight0=1-f,boneIndex1=spine,weight1=f};}
            float blend=Mathf.SmoothStep(0,1,(t-.32f)/.68f);return new BoneWeight{boneIndex0=spine,weight0=1-blend,boneIndex1=chest,weight1=blend};
        }
        public static Vector3 Front(float x,float y,float relief=.002f)
        {
            float ease=(y>=1.39f?.021f:Mathf.Lerp(.022f,.024f,Mathf.InverseLerp(BodyFitProfile.WaistY,1.36f,y)))+relief;
            float z=BodyFitProfile.Radius(y,Mathf.PI*.5f)+ease;
            for(int i=0;i<4;i++){float angle=Mathf.Atan2(z,x),r=BodyFitProfile.Radius(y,angle)+ease;z=Mathf.Sqrt(Mathf.Max(.00001f,r*r-x*x));}
            return new Vector3(x,y,BodyFitProfile.CentreZ(y)+z);
        }
        void Patch(Builder b,Vector3[,] grid,int material,float thickness=.002f)
        {
            int rows=grid.GetLength(0),cols=grid.GetLength(1),start=b.p.Count;
            for(int layer=0;layer<2;layer++)for(int row=0;row<rows;row++)for(int col=0;col<cols;col++){
                Vector3 p=grid[row,col];Vector3 outward=new Vector3(p.x,0,p.z-BodyFitProfile.CentreZ(p.y)).normalized;
                p-=outward*(layer*thickness);b.Add(p,new Vector2(col/(float)(cols-1),row/(float)(rows-1)),Weight(p));}
            int plane=rows*cols;
            for(int row=0;row<rows-1;row++)for(int col=0;col<cols-1;col++){int a=start+row*cols+col;b.Quad(a,a+1,a+cols+1,a+cols,material);b.Quad(a+plane+cols,a+plane+cols+1,a+plane+1,a+plane,material);}
            for(int col=0;col<cols-1;col++){int a=start+col,z=start+(rows-1)*cols+col;b.Quad(a+plane,a+plane+1,a+1,a,material);b.Quad(z,z+1,z+plane+1,z+plane,material);}
            for(int row=0;row<rows-1;row++){int a=start+row*cols,z=a+cols-1;b.Quad(a,a+cols,a+cols+plane,a+plane,material);b.Quad(z+plane,z+cols+plane,z+cols,z,material);}
        }
        void Tube(Builder b,List<Vector3> path,float radius,int material)
        {
            const int sides=6;int start=b.p.Count;
            for(int i=0;i<path.Count;i++){
                Vector3 tangent=(path[Mathf.Min(i+1,path.Count-1)]-path[Mathf.Max(i-1,0)]).normalized;
                Vector3 a=Vector3.Cross(tangent,Vector3.forward);if(a.sqrMagnitude<.001f)a=Vector3.Cross(tangent,Vector3.up);a.Normalize();Vector3 c=Vector3.Cross(tangent,a).normalized;
                for(int s=0;s<sides;s++){float angle=s*Mathf.PI*2/sides;Vector3 p=path[i]+radius*(a*Mathf.Cos(angle)+c*Mathf.Sin(angle));b.Add(p,new Vector2(s/(float)sides,i/(float)(path.Count-1)),Weight(p));}
            }
            for(int i=0;i<path.Count-1;i++)for(int s=0;s<sides;s++){int a=start+i*sides+s,c=start+i*sides+(s+1)%sides;b.Quad(a,c,c+sides,a+sides,material);}
        }
        void BuildCollar(Builder b)
        {
            const int cols=97,rows=7;var fold=new Vector3[rows,cols];var stand=new Vector3[3,cols];
            for(int col=0;col<cols;col++){
                float theta=Mathf.PI*.5f+.24f+col/(float)(cols-1)*(Mathf.PI*2-.48f),front=Mathf.Pow(Mathf.Max(0,Mathf.Sin(theta)),6);
                float outerAngle=theta-Mathf.Sign(Mathf.Cos(theta))*.30f*front;
                Vector3 inner=BodyFitProfile.Surface(1.474f,theta,.017f),outer=BodyFitProfile.Surface(1.435f-.030f*front,outerAngle,.027f);
                for(int row=0;row<rows;row++){float t=row/(float)(rows-1);fold[row,col]=Vector3.Lerp(inner,outer,t)+Vector3.up*(Mathf.Sin(t*Mathf.PI)*.004f);}
                for(int row=0;row<3;row++)stand[row,col]=BodyFitProfile.Surface(Mathf.Lerp(1.452f,1.474f,row/2f),theta,.013f);
            }
            fold[rows-1,0]=Vector3.Lerp(fold[rows-1,0],(fold[rows-2,0]+fold[rows-1,1])*.5f,.65f);
            fold[rows-1,cols-1]=Vector3.Lerp(fold[rows-1,cols-1],(fold[rows-2,cols-1]+fold[rows-1,cols-2])*.5f,.65f);
            Patch(b,stand,0,.002f);Patch(b,fold,0,.0025f);
            var edge=new List<Vector3>();for(int col=0;col<cols;col++)edge.Add(fold[rows-1,col]+Vector3.up*.0008f);Tube(b,edge,.00065f,1);
        }
        void BuildPlacket(Builder b)
        {
            var grid=new Vector3[33,3];for(int row=0;row<33;row++)for(int col=0;col<3;col++)grid[row,col]=Front((col-1)*.017f,Mathf.Lerp(1.18f,1.423f,row/32f),.003f);
            Patch(b,grid,0,.002f);
            foreach(float x in new[]{-.0145f,.0145f}){var path=new List<Vector3>();for(int i=0;i<=40;i++)path.Add(Front(x,Mathf.Lerp(1.185f,1.418f,i/40f),.004f));Tube(b,path,.0005f,1);}
        }
        void BuildButtons(Builder b)
        {
            for(int button=0;button<ButtonCount;button++){
                Vector3 center=Front(0,1.397f-button*.061f,.006f);
                // Raised, intertwined open G-shaped metal strokes rather than a flat image decal.
                for(int side=0;side<2;side++){
                    var path=new List<Vector3>();for(int i=0;i<=26;i++){
                        float theta=Mathf.Lerp(.32f,Mathf.PI*2-.32f,i/26f);if(side==1)theta=Mathf.PI-theta;
                        path.Add(center+new Vector3((side==0?-.003f:.003f)+Mathf.Cos(theta)*.0047f,Mathf.Sin(theta)*.0047f,.002f));}
                    Tube(b,path,.00085f,2);
                    float sign=side==0?1:-1;Tube(b,new List<Vector3>{center+new Vector3((side==0?-.003f:.003f)+sign*.0045f,0,.002f),center+new Vector3((side==0?-.003f:.003f),0,.002f)},.00085f,2);
                }
                // Small dark buttonhole beside the raised hardware.
                Tube(b,new List<Vector3>{center+new Vector3(.011f,-.003f,-.001f),center+new Vector3(.011f,.003f,-.001f)},.0006f,1);
            }
        }
        void BuildSeams(Builder b)
        {
            foreach(float side in new[]{-1f,1f}){
                var path=new List<Vector3>();for(int i=0;i<=50;i++){float t=i/50f,y=Mathf.Lerp(1.125f,1.405f,t),x=side*Mathf.Lerp(.086f,.123f,t);path.Add(Front(x,y,.0015f));}Tube(b,path,.0007f,1);
            }
        }
        public void SetFabric(Material material){if(UpperSurface)UpperSurface.sharedMaterial=material;if(trimFabric&&stitchFabric){Color tint=material.GetFloat("_UseBase")>.5f?new Color(.030f,.031f,.033f):material.GetColor("_Color");trimFabric.SetColor("_Color",tint*.98f);stitchFabric.SetColor("_Color",tint*.88f);}}
        public void ToggleDetails(){if(DetailSurface)DetailSurface.enabled=!DetailSurface.enabled;}
        void OnDestroy(){foreach(Material m in ownedMaterials)if(m){if(Application.isPlaying)Destroy(m);else DestroyImmediate(m);}foreach(Mesh m in new[]{upperMesh,detailMesh})if(m){if(Application.isPlaying)Destroy(m);else DestroyImmediate(m);}}
    }
}
