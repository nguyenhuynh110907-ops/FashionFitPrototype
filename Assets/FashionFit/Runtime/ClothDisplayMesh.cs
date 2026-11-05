using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FashionFit
{
    // One render subdivision per source triangle. Shared edge samples preserve
    // normals and the authored slit; no extra distance constraints are created.
    public sealed class ClothDisplayMesh
    {
        readonly int[] edgeA, edgeB;
        readonly Vector3[] vertices;
        readonly int sourceCount;
        int[] boundaryA,boundaryB;
        Vector3[] rounded;
        public Mesh Mesh { get; private set; }

        public ClothDisplayMesh(string name,Vector3[] source,Vector2[] uv,int[] triangles,Bounds bounds,bool roundArmholes=false)
        {
            sourceCount=source.Length;
            var a=new List<int>();var b=new List<int>();var displayUV=new List<Vector2>(uv);
            var indices=new List<int>(triangles.Length*4);var cache=new Dictionary<long,int>();
            for(int n=0;n<triangles.Length;n+=3)
            {
                int i=triangles[n],j=triangles[n+1],k=triangles[n+2];
                int ij=Midpoint(i,j,sourceCount,a,b,displayUV,cache);
                int jk=Midpoint(j,k,sourceCount,a,b,displayUV,cache);
                int ki=Midpoint(k,i,sourceCount,a,b,displayUV,cache);
                indices.Add(i);indices.Add(ij);indices.Add(ki);
                indices.Add(ij);indices.Add(j);indices.Add(jk);
                indices.Add(ki);indices.Add(jk);indices.Add(k);
                indices.Add(ij);indices.Add(jk);indices.Add(ki);
            }
            edgeA=a.ToArray();edgeB=b.ToArray();vertices=new Vector3[sourceCount+edgeA.Length];
            for(int i=0;i<sourceCount;i++)vertices[i]=source[i];
            for(int i=0;i<edgeA.Length;i++)vertices[sourceCount+i]=(source[edgeA[i]]+source[edgeB[i]])*.5f;
            Mesh=new Mesh{name=name,vertices=vertices,uv=displayUV.ToArray(),triangles=indices.ToArray(),bounds=bounds};
            Mesh.RecalculateNormals();Mesh.RecalculateTangents();Mesh.MarkDynamic();
            if(roundArmholes){
                boundaryA=new int[vertices.Length];boundaryB=new int[vertices.Length];rounded=new Vector3[vertices.Length];
                for(int i=0;i<vertices.Length;i++){boundaryA[i]=-1;boundaryB[i]=-1;}
                var uses=new Dictionary<long,int>();
                for(int i=0;i<indices.Count;i+=3)for(int side=0;side<3;side++){
                    int x=indices[i+side],y=indices[i+(side+1)%3];long key=((long)Mathf.Min(x,y)<<32)|(uint)Mathf.Max(x,y);
                    uses.TryGetValue(key,out int count);uses[key]=count+1;
                }
                foreach(var pair in uses)if(pair.Value==1){int x=(int)(pair.Key>>32),y=(int)pair.Key;
                    if(boundaryA[x]<0)boundaryA[x]=y;else boundaryB[x]=y;
                    if(boundaryA[y]<0)boundaryA[y]=x;else boundaryB[y]=x;
                }
                for(int i=0;i<vertices.Length;i++)if(vertices[i].y<=1.20f||vertices[i].y>=1.44f)boundaryB[i]=-1;
            }
        }

        static int Midpoint(int i,int j,int offset,List<int> a,List<int> b,List<Vector2> uv,Dictionary<long,int> cache)
        {
            long key=((long)Mathf.Min(i,j)<<32)|(uint)Mathf.Max(i,j);
            if(cache.TryGetValue(key,out int index))return index;
            index=offset+a.Count;cache.Add(key,index);a.Add(i);b.Add(j);uv.Add((uv[i]+uv[j])*.5f);return index;
        }

        public void Publish(FittingClothSolver solver,ClothCapsule[] capsules,ClothEllipsoid[] ellipsoids,bool tangents)
        {
            for(int i=0;i<sourceCount;i++)vertices[i]=solver.Positions[i];
            for(int i=0;i<edgeA.Length;i++)vertices[sourceCount+i]=(solver.Positions[edgeA[i]]+solver.Positions[edgeB[i]])*.5f;
            if(rounded!=null)for(int pass=0;pass<2;pass++){
                System.Array.Copy(vertices,rounded,vertices.Length);
                for(int i=0;i<vertices.Length;i++)if(boundaryA[i]>=0&&boundaryB[i]>=0){
                    Vector3 delta=((rounded[boundaryA[i]]+rounded[boundaryB[i]])*.5f-rounded[i])*.4f;
                    vertices[i]+=Vector3.ClampMagnitude(delta,.006f);
                }
            }
            // A straight render edge between safe physics points can cut a curved
            // collider. Project its display samples as well.
            for(int i=0;i<vertices.Length;i++)solver.ProjectRenderPoint(ref vertices[i],capsules,ellipsoids);
            const MeshUpdateFlags flags=MeshUpdateFlags.DontRecalculateBounds|MeshUpdateFlags.DontValidateIndices;
            Mesh.SetVertices(vertices,0,vertices.Length,flags);Mesh.RecalculateNormals(flags);
            if(tangents)Mesh.RecalculateTangents(flags);
        }
    }
}
