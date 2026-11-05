using System;
using System.Collections.Generic;
using UnityEngine;
namespace FashionFit
{
    // Open triangle mesh. Structural links never cross a front opening.
    public sealed class DoffableClothSolver
    {
        public readonly Vector3[] Positions,Previous,Rest;
        public readonly int[] Triangles;
        public readonly int[] LeftBank,RightBank;
        public readonly bool[] Closed=new[]{true,true,true,true};
        struct Edge { public int a,b;public float rest,stiffness; }
        readonly Edge[] edges;readonly List<Edge>[] structuralAdj;
        readonly int[] buttonsA=new int[4],buttonsB=new int[4];
        readonly float[] buttonRest=new float[4];
        readonly int[] grabs={-1,-1};
        readonly Vector3[] targets=new Vector3[2],offsets=new Vector3[2],stepTargets=new Vector3[2],accepted=new Vector3[2];
        readonly HashSet<long> neighbors=new HashSet<long>();
        readonly Dictionary<Vector3Int,List<int>> hash=new Dictionary<Vector3Int,List<int>>();
        readonly List<List<int>> buckets=new List<List<int>>();
        readonly bool[] contactDirty,contactNext;
        int usedBuckets;readonly float[][] gripDistances=new float[2][];
        public bool Supported=true;
        public float Attachment=1;
        public int GrabCount=>(grabs[0]>=0?1:0)+(grabs[1]>=0?1:0);
        public int OpenCount {get {int n=0;foreach(bool b in Closed)if(!b)n++;return n;}}
        public float MaxSpeed {get;private set;}
        public float RmsSpeed {get;private set;}
        public float MaxStretch {get {float s=1;foreach(var e in edges)if(e.stiffness>.5f)s=Mathf.Max(s,Vector3.Distance(Positions[e.a],Positions[e.b])/e.rest);return s;}}
        static long Key(int a,int b)=>((long)Mathf.Min(a,b)<<32)|(uint)Mathf.Max(a,b);
        public DoffableClothSolver(Vector3[] positions,int[] triangles,Vector3[] localRest,int[] left,int[] right)
        {
            contactDirty=new bool[positions.Length];contactNext=new bool[positions.Length];Positions=(Vector3[])positions.Clone();Previous=(Vector3[])positions.Clone();Rest=localRest;Triangles=triangles;LeftBank=left;RightBank=right;
            var links=new List<Edge>();
            Action<int,int> add=(a,b)=>{long key=Key(a,b);if(neighbors.Add(key)){float d=Vector3.Distance(localRest[a],localRest[b]);if(d>.002f)links.Add(new Edge{a=a,b=b,rest=d,stiffness=.92f});}};
            for(int i=0;i<triangles.Length;i+=3){int a=triangles[i],b=triangles[i+1],c=triangles[i+2];add(a,b);add(b,c);add(c,a);}
            var opposites=new Dictionary<long,int>();
            Action<int,int,int> bend=(a,b,c)=>{long key=Key(a,b);if(opposites.TryGetValue(key,out int other)){if(other!=c){float d=Vector3.Distance(localRest[other],localRest[c]);if(d>.015f){links.Add(new Edge{a=other,b=c,rest=d,stiffness=.12f});neighbors.Add(Key(other,c));}}}else opposites.Add(key,c);};
            for(int i=0;i<triangles.Length;i+=3){int a=triangles[i],b=triangles[i+1],c=triangles[i+2];bend(a,b,c);bend(b,c,a);bend(c,a,b);}
            edges=links.ToArray();structuralAdj=new List<Edge>[Rest.Length];for(int i=0;i<Rest.Length;i++)structuralAdj[i]=new List<Edge>();foreach(var edge in edges)if(edge.stiffness>.5f){structuralAdj[edge.a].Add(edge);structuralAdj[edge.b].Add(edge);}
            for(int k=0;k<4;k++){
                float y=1.403f-k*.074f;buttonsA[k]=NearestY(left,y);buttonsB[k]=NearestY(right,y);
                buttonRest[k]=Vector3.Distance(Positions[buttonsA[k]],Positions[buttonsB[k]]);
                neighbors.Add(Key(buttonsA[k],buttonsB[k]));
            }
        }
        int NearestY(int[] bank,float y){int best=-1;float d=float.PositiveInfinity;foreach(int i in bank){float e=Mathf.Abs(Rest[i].y-y);if(e<d){d=e;best=i;}}if(best<0)throw new InvalidOperationException("Missing authored front edge");return best;}
        public int ButtonA(int i)=>buttonsA[i];public int ButtonB(int i)=>buttonsB[i];
        public Vector3 ButtonPoint(int i)=>Positions[buttonsA[i]];
        public float ButtonGap(int i)=>Vector3.Distance(Positions[buttonsA[i]],Positions[buttonsB[i]]);
        public bool Toggle(int i)
        {
            if(i<0||i>=4)return false;
            if(!Closed[i]&&ButtonGap(i)>.025f)return false;
            Closed[i]=!Closed[i];return true;
        }
        public bool Nearest(Vector3 p,float reach,out Vector3 surface)
        {
            int n=NearestIndex(p,reach);surface=n<0?p:Positions[n];return n>=0;
        }
        int NearestIndex(Vector3 p,float reach){float d=reach*reach;int best=-1;for(int i=0;i<Positions.Length;i++){float e=(Positions[i]-p).sqrMagnitude;if(e<d){d=e;best=i;}}return best;}
        public bool BeginGrab(int slot,Vector3 p)
        {
            if(slot<0||slot>1||grabs[slot]>=0)return false;
            int i=NearestIndex(p,.085f);if(i<0||i==grabs[1-slot])return false;
            grabs[slot]=i;offsets[slot]=Positions[i]-p;targets[slot]=accepted[slot]=Positions[i];gripDistances[slot]=Geodesic(i);return true;
        }
        float[] Geodesic(int source)
        {
            int n=Rest.Length;var d=new float[n];var done=new bool[n];for(int i=0;i<n;i++)d[i]=float.PositiveInfinity;d[source]=0;
            for(int pass=0;pass<n;pass++){
                int v=-1;float best=float.PositiveInfinity;for(int i=0;i<n;i++)if(!done[i]&&d[i]<best){best=d[i];v=i;}if(v<0)break;done[v]=true;
                foreach(var e in structuralAdj[v]){int other=e.a==v?e.b:e.b==v?e.a:-1;if(other>=0&&d[other]>d[v]+e.rest)d[other]=d[v]+e.rest;}
            }return d;
        }
        void GripTethers()
        {
            for(int slot=0;slot<2;slot++)if(grabs[slot]>=0&&gripDistances[slot]!=null)for(int i=0;i<Positions.Length;i++){
                if(Held(i))continue;Vector3 delta=Positions[i]-stepTargets[slot];float limit=gripDistances[slot][i]*1.08f;
                if(delta.sqrMagnitude>limit*limit)Positions[i]-=delta.normalized*Mathf.Min(.012f,delta.magnitude-limit)*.8f;
            }
        }
        public void MoveGrab(int slot,Vector3 p){if(slot>=0&&slot<2&&grabs[slot]>=0)targets[slot]=p+offsets[slot];}
        public void EndGrab(int slot){if(slot>=0&&slot<2)grabs[slot]=-1;}
        public Vector3 GrabPoint(int slot)=>grabs[slot]<0?Vector3.zero:Positions[grabs[slot]];
        public void Reset(Vector3[] fitted)
        {
            Array.Copy(fitted,Positions,fitted.Length);Array.Copy(fitted,Previous,fitted.Length);grabs[0]=grabs[1]=-1;
            for(int i=0;i<4;i++)Closed[i]=true;Supported=true;Attachment=1;MaxSpeed=0;
        }
        bool Held(int i)=>i==grabs[0]||i==grabs[1];
        Vector3 LimitGrip(int slot,Vector3 desired,Func<Vector3,Vector3> contact)
        {
            int i=grabs[slot];Vector3 old=accepted[slot],delta=desired-old;float fraction=1;
            // Reduce advance toward the hand when strained, but never let neighbors drag a held point down.
            for(int pass=0;pass<10;pass++){
                Vector3 candidate=old+delta*fraction;bool valid=true;
                foreach(var e in structuralAdj[i]){int other=e.a==i?e.b:e.b==i?e.a:-1;if(other<0)continue;
                    float limit=Mathf.Max(e.rest*1.22f,Vector3.Distance(old,Positions[other])+.0002f);
                    if((candidate-Positions[other]).sqrMagnitude>limit*limit){valid=false;break;}
                }
                if(valid)return contact(candidate);fraction*=.5f;
            }
            return contact(old);
        }
        void PinGrips(){for(int s=0;s<2;s++)if(grabs[s]>=0)Positions[grabs[s]]=stepTargets[s];}
        void Distance(int a,int b,float rest,float stiffness)
        {
            Vector3 d=Positions[b]-Positions[a];float l=d.magnitude;if(l<1e-7f)return;
            float wa=Held(a)?0:1,wb=Held(b)?0:1,sum=wa+wb;if(sum==0)return;
            Vector3 correction=d*((l-rest)/l*stiffness/sum);Positions[a]+=correction*wa;Positions[b]-=correction*wb;
        }
        public void Step(float dt,Vector3[] anchors,Func<Vector3,Vector3> contact,Func<Vector3,Vector3> surfaceContact)
        {
            dt=Mathf.Clamp(dt,.001f,DoffableBlouse.SimulationStep);MaxSpeed=0;float velocitySquared=0;Vector3 gravity=Physics.gravity;float damping=Mathf.Pow(.985f,dt*90);
            for(int i=0;i<Positions.Length;i++){
                Vector3 p=Positions[i],v=Vector3.ClampMagnitude((p-Previous[i])*damping,1.6f*dt);
                Positions[i]=contact(p+v+gravity*(dt*dt));Previous[i]=p;
            }
            for(int slot=0;slot<2;slot++)if(grabs[slot]>=0)stepTargets[slot]=LimitGrip(slot,Vector3.MoveTowards(accepted[slot],targets[slot],1.4f*dt),contact);
            for(int iteration=0;iteration<15;iteration++){
                PinGrips();
                foreach(var e in edges)Distance(e.a,e.b,e.rest,e.stiffness);GripTethers();
                for(int k=0;k<4;k++)if(Closed[k])Distance(buttonsA[k],buttonsB[k],buttonRest[k],.98f);
                if(Supported&&Attachment>0)for(int i=0;i<Positions.Length;i++){
                    float support=Rest[i].y>1.39f?.75f:.08f;
                    Vector3 d=Positions[i]-anchors[i];float limit=Rest[i].y>1.39f?.006f:.018f;
                    if(d.magnitude>limit)Positions[i]-=Vector3.ClampMagnitude(d,d.magnitude-limit)*support*Attachment;
                }
                PinGrips();
                if(iteration==8)SelfContacts();
                if(iteration%3==2)for(int i=0;i<Positions.Length;i++)Positions[i]=contact(Positions[i]);
            }
            // Face interior and edge samples keep coarse triangles off the body.
            for(int pass=0;pass<6;pass++){
                Array.Clear(contactNext,0,contactNext.Length);
                for(int k=0;k<Triangles.Length;k+=3){
                int a=Triangles[k],b=Triangles[k+1],c=Triangles[k+2];
                if(pass>0&&!contactDirty[a]&&!contactDirty[b]&&!contactDirty[c]&&!contactNext[a]&&!contactNext[b]&&!contactNext[c])continue;
                Sample(a,b,c,1f/3,1f/3,1f/3,surfaceContact);Sample(a,b,c,.5f,.5f,0,surfaceContact);Sample(a,b,c,0,.5f,.5f,surfaceContact);Sample(a,b,c,.5f,0,.5f,surfaceContact);
                if((Positions[a]-Positions[b]).sqrMagnitude>.0009f){Sample(a,b,c,.25f,.75f,0,surfaceContact);Sample(a,b,c,.75f,.25f,0,surfaceContact);}
                if((Positions[b]-Positions[c]).sqrMagnitude>.0009f){Sample(a,b,c,0,.25f,.75f,surfaceContact);Sample(a,b,c,0,.75f,.25f,surfaceContact);}
                if((Positions[a]-Positions[c]).sqrMagnitude>.0009f){Sample(a,b,c,.25f,0,.75f,surfaceContact);Sample(a,b,c,.75f,0,.25f,surfaceContact);}
            }
                Array.Copy(contactNext,contactDirty,contactDirty.Length);
            }
            for(int i=0;i<Positions.Length;i++){
                Positions[i]=contact(Positions[i]);Vector3 v=Vector3.ClampMagnitude(Positions[i]-Previous[i],dt*2);
                velocitySquared+=v.sqrMagnitude/(dt*dt);MaxSpeed=Mathf.Max(MaxSpeed,v.magnitude/dt);Previous[i]=Positions[i]-v;
                if(!RunwayVRHands.ValidPose(Positions[i],Quaternion.identity))throw new InvalidOperationException("Nonfinite blouse particle");
            }
            RmsSpeed=Mathf.Sqrt(velocitySquared/Positions.Length);for(int slot=0;slot<2;slot++)if(grabs[slot]>=0)accepted[slot]=Positions[grabs[slot]];
        }
        void Sample(int a,int b,int c,float wa,float wb,float wc,Func<Vector3,Vector3> contact)
        {
            Vector3 p=Positions[a]*wa+Positions[b]*wb+Positions[c]*wc,d=contact(p)-p;if(Held(a))wa=0;if(Held(b))wb=0;if(Held(c))wc=0;float denom=wa*wa+wb*wb+wc*wc;if(denom<1e-8f)return;
            if(d.sqrMagnitude<1e-12f)return;contactNext[a]=contactNext[b]=contactNext[c]=true;Positions[a]+=d*(wa/denom);Positions[b]+=d*(wb/denom);Positions[c]+=d*(wc/denom);
        }
        void SelfContacts()
        {
            const float size=.012f,thickness=.004f;hash.Clear();usedBuckets=0;
            for(int i=0;i<Positions.Length;i++){
                Vector3 p=Positions[i];var key=new Vector3Int(Mathf.FloorToInt(p.x/size),Mathf.FloorToInt(p.y/size),Mathf.FloorToInt(p.z/size));
                if(!hash.TryGetValue(key,out var list)){if(usedBuckets==buckets.Count)buckets.Add(new List<int>(8));list=buckets[usedBuckets++];list.Clear();hash.Add(key,list);}list.Add(i);
            }
            for(int i=0;i<Positions.Length;i++){
                Vector3 p=Positions[i];int x=Mathf.FloorToInt(p.x/size),y=Mathf.FloorToInt(p.y/size),z=Mathf.FloorToInt(p.z/size);
                for(int a=-1;a<=1;a++)for(int b=-1;b<=1;b++)for(int c=-1;c<=1;c++)if(hash.TryGetValue(new Vector3Int(x+a,y+b,z+c),out var list))foreach(int j in list){
                    if(j<=i||neighbors.Contains(Key(i,j)))continue;Vector3 d=Positions[j]-Positions[i];float l=d.magnitude;
                    if(l<thickness&&l>1e-7f){d*=.5f*(thickness-l)/l;Positions[i]-=d;Positions[j]+=d;}
                }
            }
        }
    }
}
