using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FashionFit
{
    // Offline surface binding. Bodies must have an explicitly shared vertex correspondence.
    // This is a geometric baseline, not a trained network or a cloth/contact solver.
    public sealed class GarmentSurfaceTransfer
    {
        public struct Hit { public int triangle; public Vector3 point,barycentric; public float distanceSquared; }
        sealed class Node { public Bounds bounds;public int start,count;public Node left,right; }
        public sealed class Surface
        {
            sealed class EdgeComparer:IEqualityComparer<ulong>{public bool Equals(ulong a,ulong b)=>a==b;public int GetHashCode(ulong v)=>unchecked((int)((uint)v*16777619u^(uint)(v>>32)*2166136261u));}
            public readonly Vector3[] vertices;public readonly int[] triangles;
            readonly int[] order;readonly Node root;
            public Surface(Vector3[] points,int[] faces)
            {
                if(points==null||points.Length<3||faces==null||faces.Length<3||faces.Length%3!=0)throw new InvalidDataException("Missing triangulated surface.");
                vertices=(Vector3[])points.Clone();triangles=(int[])faces.Clone();
                foreach(var p in vertices)Finite(p);
                order=new int[faces.Length/3];for(int i=0;i<order.Length;i++){order[i]=i;for(int k=0;k<3;k++)if(faces[i*3+k]<0||faces[i*3+k]>=points.Length)throw new InvalidDataException("Invalid triangle index.");}
                root=Build(0,order.Length);
            }
            Node Build(int start,int count)
            {
                var n=new Node{start=start,count=count,bounds=new Bounds(vertices[triangles[order[start]*3]],Vector3.zero)};
                for(int i=start;i<start+count;i++)for(int k=0;k<3;k++)n.bounds.Encapsulate(vertices[triangles[order[i]*3+k]]);
                if(count<=12)return n;Vector3 size=n.bounds.size;int axis=size.x>=size.y&&size.x>=size.z?0:size.y>=size.z?1:2;
                Array.Sort(order,start,count,Comparer<int>.Create((a,b)=>Centre(a,axis).CompareTo(Centre(b,axis))));int half=count/2;n.left=Build(start,half);n.right=Build(start+half,count-half);return n;
            }
            float Centre(int t,int axis)=>(vertices[triangles[t*3]][axis]+vertices[triangles[t*3+1]][axis]+vertices[triangles[t*3+2]][axis])/3;
            public Bounds Bounds=>root.bounds;
            public Hit Nearest(Vector3 p){Finite(p);var hit=new Hit{triangle=-1,distanceSquared=float.PositiveInfinity};Find(root,p,ref hit);if(hit.triangle<0)throw new InvalidDataException("Body contains no usable triangles.");return hit;}
            static readonly Vector3[] signRays={new Vector3(1,.173f,.357f).normalized,new Vector3(.291f,1,.419f).normalized,new Vector3(.213f,.137f,1).normalized};
            readonly List<float> contactRayHits=new List<float>(64);
            public void Refit(Vector3[] points){if(points.Length!=vertices.Length)throw new InvalidDataException("Changed body topology");Array.Copy(points,vertices,points.Length);RefitNode(root);}
            void RefitNode(Node n){if(n.left!=null){RefitNode(n.left);RefitNode(n.right);n.bounds=n.left.bounds;n.bounds.Encapsulate(n.right.bounds);return;}Vector3 low=Vector3.one*float.PositiveInfinity,high=Vector3.one*float.NegativeInfinity;for(int i=n.start;i<n.start+n.count;i++)for(int k=0;k<3;k++){Vector3 p=vertices[triangles[order[i]*3+k]];low=Vector3.Min(low,p);high=Vector3.Max(high,p);}n.bounds=new Bounds((low+high)*.5f,high-low);}
            public bool ContainsPoint(Vector3 p){int votes=0;foreach(var ray in signRays){contactRayHits.Clear();RayHits(root,new Ray(p,ray),contactRayHits);contactRayHits.Sort();int count=0;float last=float.NegativeInfinity;foreach(float h in contactRayHits)if(h-last>1e-6f){last=h;count++;}if((count&1)==1)votes++;}return votes>=2;}
            public Vector3 ProjectOutside(Vector3 p,float margin){for(int pass=0;pass<2;pass++){var h=Nearest(p);int t=h.triangle*3;Vector3 normal=Vector3.Cross(vertices[triangles[t+1]]-vertices[triangles[t]],vertices[triangles[t+2]]-vertices[triangles[t]]).normalized;float distance=Mathf.Sqrt(h.distanceSquared);bool inside=Vector3.Dot(p-h.point,normal)<0&&ContainsPoint(p);if(!inside&&distance>=margin)break;Vector3 outward=inside||distance<1e-7f?normal:(p-h.point)/distance;p=h.point+outward*margin;}return p;}
            public void RequireClosedBody()
            {
                var counts=new Dictionary<ulong,int>(new EdgeComparer());var directions=new Dictionary<ulong,int>(new EdgeComparer());double volume=0;
                for(int i=0;i<triangles.Length;i+=3){int a=triangles[i],b=triangles[i+1],c=triangles[i+2];Vector3 x=vertices[a],y=vertices[b],z=vertices[c];volume+=(double)x.x*(y.y*z.z-y.z*z.y)+(double)x.y*(y.z*z.x-y.x*z.z)+(double)x.z*(y.x*z.y-y.y*z.x);int[] corners={a,b,c};for(int j=0;j<3;j++){int from=corners[j],to=corners[(j+1)%3];ulong key=((ulong)(uint)Math.Min(from,to)<<32)|(uint)Math.Max(from,to);counts[key]=(counts.TryGetValue(key,out int n)?n:0)+1;directions[key]=(directions.TryGetValue(key,out int d)?d:0)+(from<to?1:-1);}}
                foreach(var edge in counts)if(edge.Value!=2||directions[edge.Key]!=0)throw new InvalidDataException("Body must be closed, manifold and consistently wound. Use the canonical body mesh, not a UV-split re-export.");
                if(volume<=1e-6)throw new InvalidDataException("Body needs outward winding and positive enclosed volume.");
            }
            void Find(Node n,Vector3 p,ref Hit hit)
            {
                if(n.bounds.SqrDistance(p)>hit.distanceSquared)return;
                if(n.left!=null){float a=n.left.bounds.SqrDistance(p),b=n.right.bounds.SqrDistance(p);if(a<=b){Find(n.left,p,ref hit);Find(n.right,p,ref hit);}else{Find(n.right,p,ref hit);Find(n.left,p,ref hit);}return;}
                for(int i=n.start;i<n.start+n.count;i++){
                    int t=order[i],a=triangles[t*3],b=triangles[t*3+1],c=triangles[t*3+2];
                    if(Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]).sqrMagnitude<1e-18f)continue;
                    Vector3 q=Closest(p,vertices[a],vertices[b],vertices[c],out Vector3 bary);float d=(p-q).sqrMagnitude;
                    if(d<hit.distanceSquared||(d==hit.distanceSquared&&t<hit.triangle))hit=new Hit{triangle=t,point=q,barycentric=bary,distanceSquared=d};
                }
            }
            // Face sign is valid for an interior projection. At an edge/vertex,
            // use ray parity instead: a neighbouring concave face can give the wrong sign.
            // Requires a closed, consistently oriented body; still finite surface samples, not CCD.
            public float LocalSignedClearance(Vector3 p)
            {
                Hit h=Nearest(p);if(h.distanceSquared<1e-16f)return 0;bool inside;
                if(Mathf.Min(h.barycentric.x,Mathf.Min(h.barycentric.y,h.barycentric.z))>1e-5f){int i=h.triangle*3;Vector3 n=Vector3.Cross(vertices[triangles[i+1]]-vertices[triangles[i]],vertices[triangles[i+2]]-vertices[triangles[i]]).normalized;inside=Vector3.Dot(p-h.point,n)<0;}
                else{int votes=0;foreach(var direction in new[]{new Vector3(1,.173f,.357f),new Vector3(.291f,1,.419f),new Vector3(.213f,.137f,1)}){var hits=new List<float>();RayHits(root,new Ray(p,direction.normalized),hits);hits.Sort();int count=0;float previous=float.NegativeInfinity;foreach(float hit in hits)if(hit-previous>1e-6f){count++;previous=hit;}if((count&1)==1)votes++;}inside=votes>=2;}
                return Mathf.Sqrt(h.distanceSquared)*(inside?-1:1);
            }
            void RayHits(Node node,Ray ray,List<float> hits)
            {
                if(!node.bounds.IntersectRay(ray,out float entry)||entry<0)return;
                if(node.left!=null){RayHits(node.left,ray,hits);RayHits(node.right,ray,hits);return;}
                for(int j=node.start;j<node.start+node.count;j++){int t=order[j]*3;Vector3 a=vertices[triangles[t]],e1=vertices[triangles[t+1]]-a,e2=vertices[triangles[t+2]]-a;Vector3 cross=Vector3.Cross(ray.direction,e2);float determinant=Vector3.Dot(e1,cross);if(Mathf.Abs(determinant)<1e-12f)continue;float inv=1/determinant;Vector3 delta=ray.origin-a;float u=Vector3.Dot(delta,cross)*inv;if(u<0||u>1)continue;Vector3 q=Vector3.Cross(delta,e1);float v=Vector3.Dot(ray.direction,q)*inv;if(v<0||u+v>1)continue;float distance=Vector3.Dot(e2,q)*inv;if(distance>1e-7f)hits.Add(distance);}
            }
        }
        public readonly Surface source,target;
        readonly Matrix4x4[] gradients;readonly Matrix4x4 global;readonly BoneWeight[] bodyWeights;readonly bool identity;
        public GarmentSurfaceTransfer(Vector3[] from,int[] fromFaces,Vector3[] to,int[] toFaces,BoneWeight[] weights=null)
        {
            if(from==null||to==null||from.Length!=to.Length||fromFaces==null||toFaces==null||fromFaces.Length!=toFaces.Length)throw new InvalidDataException("Bodies need the same vertex order and triangle correspondence. Registration is required first.");
            for(int i=0;i<fromFaces.Length;i++)if(fromFaces[i]!=toFaces[i])throw new InvalidDataException("Body triangle correspondence differs. Do not guess correspondence from vertex count.");
            source=new Surface(from,fromFaces);target=new Surface(to,toFaces);identity=true;
            for(int i=0;i<from.Length;i++){if((from[i]-to[i]).sqrMagnitude>0)identity=false;if(Vector3.Distance(from[i],to[i])>.15f)throw new InvalidDataException("Body displacement exceeds 15 cm. Align neutral poses and units before transfer.");}
            bodyWeights=weights;if(weights!=null&&weights.Length!=to.Length)throw new InvalidDataException("Target body weights do not match vertices.");
            global=identity?Matrix4x4.identity:FitAffine(from,to);gradients=new Matrix4x4[fromFaces.Length/3];
            for(int t=0;t<gradients.Length;t++){
                if(identity){gradients[t]=Matrix4x4.identity;continue;}
                int a=fromFaces[t*3],b=fromFaces[t*3+1],c=fromFaces[t*3+2];Vector3 e1=from[b]-from[a],e2=from[c]-from[a],f1=to[b]-to[a],f2=to[c]-to[a];
                if(Vector3.Cross(e1,e2).sqrMagnitude<1e-18f||Vector3.Cross(f1,f2).sqrMagnitude<1e-18f){gradients[t]=Matrix4x4.identity;continue;}
                CheckScale(e1.magnitude,f1.magnitude);CheckScale(e2.magnitude,f2.magnitude);CheckScale((e1-e2).magnitude,(f1-f2).magnitude);
                // A global affine fit handles size/rigid changes without inverting tiny body triangles.
                // Residuals interpolate continuously across shared body edges. The tangential
                // residual derivative is used only for shading, with a condition-number guard.
                Matrix4x4 f=global;float aa=Vector3.Dot(e1,e1),bb=Vector3.Dot(e2,e2),ab=Vector3.Dot(e1,e2),det=aa*bb-ab*ab;
                if(det>aa*bb*1e-6f){Vector3 r1=f1-global.MultiplyVector(e1),r2=f2-global.MultiplyVector(e2),u=(e1*bb-e2*ab)/det,v=(e2*aa-e1*ab)/det;for(int column=0;column<3;column++){Vector3 correction=r1*u[column]+r2*v[column];for(int row=0;row<3;row++)f[row,column]+=correction[row];}}
                gradients[t]=f;
            }
        }
        static void CheckScale(float a,float b){if(a<1e-9f)return;float r=b/a;if(r<.65f||r>1.5f)throw new InvalidDataException("Body edge scale outside 0.65–1.5. Registration/pose is incompatible.");}
        static Matrix4x4 FitAffine(Vector3[] from,Vector3[] to)
        {
            var equations=new double[4,7];for(int i=0;i<from.Length;i++){double[] x={from[i].x,from[i].y,from[i].z,1};for(int row=0;row<4;row++){for(int col=0;col<4;col++)equations[row,col]+=x[row]*x[col];for(int axis=0;axis<3;axis++)equations[row,4+axis]+=x[row]*to[i][axis];}}
            double lambda=1e-12*Math.Max(1,from.Length);for(int row=0;row<4;row++){equations[row,row]+=lambda;if(row<3)equations[row,4+row]+=lambda;}
            for(int pivot=0;pivot<4;pivot++){int best=pivot;for(int row=pivot+1;row<4;row++)if(Math.Abs(equations[row,pivot])>Math.Abs(equations[best,pivot]))best=row;for(int col=0;col<7;col++){double value=equations[pivot,col];equations[pivot,col]=equations[best,col];equations[best,col]=value;}double divisor=equations[pivot,pivot];if(Math.Abs(divisor)<1e-20)throw new InvalidDataException("Source body registration is singular.");for(int col=pivot;col<7;col++)equations[pivot,col]/=divisor;for(int row=0;row<4;row++)if(row!=pivot){double value=equations[row,pivot];for(int col=pivot;col<7;col++)equations[row,col]-=value*equations[pivot,col];}}
            Matrix4x4 affine=Matrix4x4.identity;for(int row=0;row<3;row++)for(int col=0;col<4;col++)affine[row,col]=(float)equations[col,4+row];if(affine.determinant<=0)throw new InvalidDataException("Mirrored or singular body registration.");return affine;
        }
        public Vector3 Map(Vector3 p,out Hit binding,out Matrix4x4 gradient)
        {
            binding=source.Nearest(p);if(binding.distanceSquared>.5f*.5f)throw new InvalidDataException("Garment is over 50 cm from source body. Check source frame, units or loose panels.");
            gradient=gradients[binding.triangle];if(identity)return p;
            int t=binding.triangle*3;Vector3 q=toPoint(t,binding.barycentric);Vector3 result=q+global.MultiplyVector(p-binding.point);Finite(result);return result;
        }
        Vector3 toPoint(int t,Vector3 b)=>target.vertices[target.triangles[t]]*b.x+target.vertices[target.triangles[t+1]]*b.y+target.vertices[target.triangles[t+2]]*b.z;
        public BoneWeight Weights(Hit binding)
        {
            if(bodyWeights==null)throw new InvalidOperationException("Target skin weights missing.");var sum=new Dictionary<int,float>();int t=binding.triangle*3;
            for(int j=0;j<3;j++){BoneWeight w=bodyWeights[target.triangles[t+j]];float f=binding.barycentric[j];Add(sum,w.boneIndex0,w.weight0*f);Add(sum,w.boneIndex1,w.weight1*f);Add(sum,w.boneIndex2,w.weight2*f);Add(sum,w.boneIndex3,w.weight3*f);}
            var list=new List<KeyValuePair<int,float>>(sum);list.Sort((a,b)=>{int v=b.Value.CompareTo(a.Value);return v!=0?v:a.Key.CompareTo(b.Key);});
            var indices=new int[4];var values=new float[4];float total=0;for(int i=0;i<Math.Min(4,list.Count);i++){indices[i]=list[i].Key;values[i]=list[i].Value;total+=values[i];}
            if(total<=0)throw new InvalidDataException("Invalid zero target skin weight.");return new BoneWeight{boneIndex0=indices[0],boneIndex1=indices[1],boneIndex2=indices[2],boneIndex3=indices[3],weight0=values[0]/total,weight1=values[1]/total,weight2=values[2]/total,weight3=values[3]/total};
        }
        static void Add(Dictionary<int,float> sum,int index,float value){if(value<=0)return;sum[index]=(sum.TryGetValue(index,out float n)?n:0)+value;}
        public Mesh Transfer(Mesh mesh,Matrix4x4 sourceMatrix,Matrix4x4[] bindposes,out TransferMetrics metrics)
        {
            if(!mesh||!mesh.isReadable||mesh.blendShapeCount!=0)throw new InvalidDataException("Readable neutral mesh required. Bake blend shapes explicitly before transfer.");
            if(sourceMatrix.determinant<=0)throw new InvalidDataException("Mirrored or singular source transform. Apply transforms in the authoring tool first.");
            var original=mesh.vertices;var normals=mesh.normals;var tangents=mesh.tangents;var positions=new Vector3[original.Length];var outputNormals=new Vector3[normals.Length];var outputTangents=new Vector4[tangents.Length];var skin=new BoneWeight[original.Length];
            metrics=new TransferMetrics{vertices=original.Length,triangles=mesh.triangles.Length/3,submeshes=mesh.subMeshCount};Matrix4x4 normalMatrix=sourceMatrix.inverse.transpose;
            for(int i=0;i<original.Length;i++){
                Vector3 p=sourceMatrix.MultiplyPoint3x4(original[i]);original[i]=p;positions[i]=Map(p,out Hit h,out Matrix4x4 f);skin[i]=Weights(h);metrics.maxSourceDistance=Mathf.Max(metrics.maxSourceDistance,Mathf.Sqrt(h.distanceSquared));
                if(normals.Length==original.Length)outputNormals[i]=f.inverse.transpose.MultiplyVector(normalMatrix.MultiplyVector(normals[i])).normalized;
                if(tangents.Length==original.Length){Vector3 tangent=f.MultiplyVector(sourceMatrix.MultiplyVector(new Vector3(tangents[i].x,tangents[i].y,tangents[i].z))).normalized;if(normals.Length==original.Length)tangent=Vector3.ProjectOnPlane(tangent,outputNormals[i]).normalized;outputTangents[i]=new Vector4(tangent.x,tangent.y,tangent.z,tangents[i].w);}
                metrics.maxDisplacement=Mathf.Max(metrics.maxDisplacement,Vector3.Distance(p,positions[i]));
            }
            foreach(var p in positions)Contact(p,metrics);int[] faces=mesh.triangles;
            for(int i=0;i<faces.Length;i+=3){int a=faces[i],b=faces[i+1],c=faces[i+2];Contact((positions[a]+positions[b]+positions[c])/3,metrics);Edge(original[a],original[b],positions[a],positions[b],metrics);Edge(original[b],original[c],positions[b],positions[c],metrics);Edge(original[c],original[a],positions[c],positions[a],metrics);float before=Vector3.Cross(original[b]-original[a],original[c]-original[a]).magnitude,after=Vector3.Cross(positions[b]-positions[a],positions[c]-positions[a]).magnitude;if(before>1e-10f&&after<before*.1f)metrics.collapsedTriangles++;}
            var copy=UnityEngine.Object.Instantiate(mesh);copy.name=mesh.name+" — transferred copy";copy.vertices=positions;copy.boneWeights=skin;copy.bindposes=bindposes;
            if(normals.Length==original.Length)copy.normals=outputNormals;else copy.RecalculateNormals();if(tangents.Length==original.Length)copy.tangents=outputTangents;else if(mesh.uv.Length==original.Length)copy.RecalculateTangents();copy.RecalculateBounds();return copy;
        }
        void Contact(Vector3 p,TransferMetrics metrics){float d=target.LocalSignedClearance(p);metrics.contactSamples++;metrics.maxLocalPenetration=Mathf.Max(metrics.maxLocalPenetration,-d);if(d<-.001f)metrics.localPenetrationSamples++;}
        static void Edge(Vector3 a,Vector3 b,Vector3 x,Vector3 y,TransferMetrics m){float before=Vector3.Distance(a,b);if(before>1e-7f)m.maxRelativeEdgeChange=Mathf.Max(m.maxRelativeEdgeChange,Mathf.Abs(Vector3.Distance(x,y)/before-1));}
        public static void Finite(Vector3 p){if(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.x)||float.IsInfinity(p.y)||float.IsInfinity(p.z))throw new InvalidDataException("Non-finite geometry.");}
        // Closest point on a non-degenerate triangle, with barycentric coordinates.
        public static Vector3 Closest(Vector3 p,Vector3 a,Vector3 b,Vector3 c,out Vector3 bary)
        {
            Vector3 ab=b-a,ac=c-a,ap=p-a;float d1=Vector3.Dot(ab,ap),d2=Vector3.Dot(ac,ap);if(d1<=0&&d2<=0){bary=new Vector3(1,0,0);return a;}
            Vector3 bp=p-b;float d3=Vector3.Dot(ab,bp),d4=Vector3.Dot(ac,bp);if(d3>=0&&d4<=d3){bary=new Vector3(0,1,0);return b;}
            float vc=d1*d4-d3*d2;if(vc<=0&&d1>=0&&d3<=0){float v=d1/(d1-d3);bary=new Vector3(1-v,v,0);return a+v*ab;}
            Vector3 cp=p-c;float d5=Vector3.Dot(ab,cp),d6=Vector3.Dot(ac,cp);if(d6>=0&&d5<=d6){bary=new Vector3(0,0,1);return c;}
            float vb=d5*d2-d1*d6;if(vb<=0&&d2>=0&&d6<=0){float w=d2/(d2-d6);bary=new Vector3(1-w,0,w);return a+w*ac;}
            float va=d3*d6-d5*d4;if(va<=0&&(d4-d3)>=0&&(d5-d6)>=0){float w=(d4-d3)/((d4-d3)+(d5-d6));bary=new Vector3(0,1-w,w);return b+w*(c-b);}
            float denominator=1/(va+vb+vc),v1=vb*denominator,w1=vc*denominator;bary=new Vector3(1-v1-w1,v1,w1);return a+ab*v1+ac*w1;
        }
    }
    [Serializable] public sealed class TransferMetrics
    {
        public int vertices,triangles,submeshes,contactSamples,localPenetrationSamples,collapsedTriangles;
        public float maxSourceDistance,maxDisplacement,maxRelativeEdgeChange,maxLocalPenetration;
        public bool Passes=>localPenetrationSamples==0&&collapsedTriangles==0&&maxRelativeEdgeChange<=.25f;
    }
}
