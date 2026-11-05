using System.Collections.Generic;
using UnityEngine;

namespace FashionFit
{
    public struct ClothCapsule
    {
        public Vector3 A, B, PreviousA, PreviousB;
        public float Radius;
        public ClothCapsule(Vector3 a, Vector3 b, float radius)
        {
            A = PreviousA = a; B = PreviousB = b; Radius = radius;
        }
        public ClothCapsule(Vector3 a, Vector3 b, Vector3 previousA, Vector3 previousB, float radius)
        {
            A = a; B = b; PreviousA = previousA; PreviousB = previousB; Radius = radius;
        }
    }

    public struct ClothEllipsoid
    {
        public Vector3 Centre, PreviousCentre, Radius;
        public ClothEllipsoid(Vector3 centre, Vector3 radius)
        {
            Centre = PreviousCentre = centre; Radius = radius;
        }
        public ClothEllipsoid(Vector3 centre, Vector3 previousCentre, Vector3 radius)
        {
            Centre = centre; PreviousCentre = previousCentre; Radius = radius;
        }
    }

    // Allocation-free PBD core for the authored procedural dresses.
    // Every real point has a soft animated tether (Unity Cloth-style max distance),
    // while triangle centroids add collision samples. These proxy contacts reduce
    // intersections but do not constitute triangle-level collision guarantees.
    public sealed class FittingClothSolver
    {
        enum EdgeKind:byte{Warp,Weft,Shear,Bend}
        struct Edge { public int a, b; public float rest, stiffness; public EdgeKind kind; }
#if !FASHIONFIT_NUMERICAL
        ClothMaterialProfile material;
        float[] particleAreas,lambdas,compliances;
        public Vector3 AirVelocity {get;set;}
        public float TotalMass {get;private set;}
#endif

        const float DefaultCollisionMargin = .014f;
        const float FloorY = .012f;
        readonly Vector3[] rest, previous,motionStart,recoveryBase,recoveryAnchor,lastAnchors;float recoveryRemaining;
        readonly float[] invMass, tetherLimit;
        readonly bool[] activePoints;
        readonly Edge[] edges;
        readonly int segments, rings;
        readonly int iterations;
        readonly float collisionMargin;
        readonly float tetherStiffness;
        readonly bool openLowerFrontSlit;
        readonly int[] grabIndices = {-1,-1};
        readonly Vector3[] grabTargets=new Vector3[2],grabOffsets=new Vector3[2],grabStarts=new Vector3[2];
        readonly int[,] patches=new int[2,5];
        readonly Vector3[,] patchOffsets=new Vector3[2,5];
        float contactStart, contactEnd, floorHeight = FloorY;
        readonly int[] hashHeads = new int[2048];
        readonly int[] hashNext;

        ClothCapsule[] activeCapsules;
        ClothEllipsoid[] activeEllipsoids;
        ClothCapsule[] contactCapsules;
        ClothEllipsoid[] contactEllipsoids;
        ClothCapsule[] sweptCapsules;
        ClothEllipsoid[] sweptEllipsoids;
        Vector3[] sweepLower,sweepUpper,ellipsoidLower,ellipsoidUpper;
        float[] capsuleMotionBounds;
        int supportedUpperRings;

        public Vector3[] Positions { get; private set; }
#if !FASHIONFIT_NUMERICAL
        public MembraneReading MaterialReading {get{var result=new MembraneReading{hasStiffness=material!=null&&material.youngWarpPa>0};foreach(var e in edges)if(e.kind!=EdgeKind.Bend&&e.rest>.003f){float strain=Vector3.Distance(Positions[e.a],Positions[e.b])/e.rest-1;result.tensileStrain=Mathf.Max(result.tensileStrain,strain);result.compressiveStrain=Mathf.Max(result.compressiveStrain,-strain);if(material!=null&&material.youngWarpPa>0){Vector3 d=rest[e.b]-rest[e.a];float blend=d.y*d.y/Mathf.Max(1e-8f,d.sqrMagnitude),young=Mathf.Lerp(material.youngWeftPa,material.youngWarpPa,blend);float force=young*material.thicknessM*strain*(strain<0?material.compressionRatio:1);result.tensionNm=Mathf.Max(result.tensionNm,force);result.compressionNm=Mathf.Max(result.compressionNm,-force);}}return result;}}
#endif
        public bool IsGrabbed=>GrabCount>0;
        public int GrabCount=>(grabIndices[0]>=0?1:0)+(grabIndices[1]>=0?1:0);
        public int GrabIndex(int slot)=>grabIndices[slot];
        public float MaxSpeed { get; private set; }
        public int LastPenetratingPoints { get; private set; }
        public bool UseBroadPhase {get;set;}=true;
        #if !FASHIONFIT_NUMERICAL
        public BodyContactShell TorsoShell { get; set; }
        #endif

        public FittingClothSolver(Vector3[] positions, int segmentCount, int rings, int fixedRings = 2, bool openLowerFrontSlit = false, float contactMargin = DefaultCollisionMargin, int solverIterations = 7, float maxTetherDistance = .55f, float anchorStiffness = .07f, bool armOpenings = false)
        {
            segments = segmentCount;
            this.rings = rings;
            this.openLowerFrontSlit = openLowerFrontSlit;
            collisionMargin = Mathf.Max(.004f, contactMargin);
            iterations = solverIterations < 4 ? 4 : solverIterations;
            tetherStiffness = Mathf.Clamp(anchorStiffness, .02f, .65f);
            rest = (Vector3[])positions.Clone();
            Positions = (Vector3[])positions.Clone();
            previous = (Vector3[])positions.Clone();
            motionStart=new Vector3[positions.Length];recoveryBase=(Vector3[])positions.Clone();recoveryAnchor=(Vector3[])positions.Clone();lastAnchors=(Vector3[])positions.Clone();
            invMass = new float[positions.Length];
            activePoints=new bool[positions.Length];
            tetherLimit = new float[positions.Length];
            hashNext = new int[positions.Length];
            for (int i = 0; i < invMass.Length; i++)
            {
                int ring = i / segments;
                activePoints[i]=!armOpenings||!IsArmOpening(i,segments,rings);
                bool fixedPoint = ring < fixedRings||!activePoints[i];
                invMass[i] = fixedPoint ? 0 : 1;
                float t = Mathf.Clamp01((ring - fixedRings) / Mathf.Max(1f, rings - fixedRings - 1f));
                tetherLimit[i] = fixedPoint ? 0 : Mathf.Lerp(.018f, Mathf.Max(.05f, maxTetherDistance), Mathf.Pow(t, 1.18f));
            }

            var links = new List<Edge>(positions.Length * 6);
            for (int r = 0; r < rings; r++)
            for (int s = 0; s < segments; s++)
            {
                int i = r * segments + s, j = r * segments + (s + 1) % segments;
                int slitColumn = segments / 4 - 1;
                bool openSlit = openLowerFrontSlit && r >= rings / 2 && s == slitColumn;
                if (!openSlit) Add(links, i, j, 1f,EdgeKind.Weft);
                if (r < rings - 1)
                {
                    Add(links, i, i + segments, 1f,EdgeKind.Warp);
                    if (!openSlit) { Add(links, i, j + segments, .72f,EdgeKind.Shear); Add(links, j, i + segments, .72f,EdgeKind.Shear); }
                }
                if (r < rings - 2) Add(links, i, i + 2 * segments, .20f,EdgeKind.Bend);
                if (!(openLowerFrontSlit && r >= rings / 2 && (s == slitColumn || s == slitColumn - 1)))
                    Add(links, i, r * segments + (s + 2) % segments, .14f,EdgeKind.Bend);
            }
            edges = links.ToArray();
        }

        void Add(List<Edge> links, int a, int b, float stiffness,EdgeKind kind)
        {
            if(!activePoints[a]||!activePoints[b])return;
            links.Add(new Edge { a = a, b = b, rest = Vector3.Distance(rest[a], rest[b]), stiffness = stiffness,kind=kind });
        }
#if !FASHIONFIT_NUMERICAL
        public void SetMaterial(ClothMaterialProfile value)
        {
            material=value;
            if(particleAreas==null){
                particleAreas=new float[rest.Length];lambdas=new float[edges.Length];compliances=new float[edges.Length];
                for(int r=0;r<rings-1;r++)for(int s=0;s<segments;s++){
                    int a=r*segments+s,b=r*segments+(s+1)%segments,c=a+segments,d=b+segments;
                    AddArea(a,b,c);AddArea(b,d,c);
                }
            }
            TotalMass=0;
            for(int i=0;i<invMass.Length;i++){
                float mass=Mathf.Max(1e-7f,particleAreas[i]*material.arealMass);TotalMass+=mass;
                if(invMass[i]>0)invMass[i]=1/mass;
            }
            for(int i=0;i<edges.Length;i++){
                switch(edges[i].kind){case EdgeKind.Warp:compliances[i]=material.stretchWarp;break;case EdgeKind.Weft:compliances[i]=material.stretchWeft;break;case EdgeKind.Shear:compliances[i]=material.shear;break;default:compliances[i]=material.bendCompliance;break;}
            }
            if(material.youngWarpPa>0&&material.thicknessM>0)for(int i=0;i<edges.Length;i++)if(edges[i].kind!=EdgeKind.Bend){var e=edges[i];Vector3 d=rest[e.b]-rest[e.a];float blend=d.y*d.y/Mathf.Max(1e-8f,d.sqrMagnitude),young=Mathf.Lerp(material.youngWeftPa,material.youngWarpPa,blend);float area=Mathf.Max(1e-7f,(particleAreas[e.a]+particleAreas[e.b])*.5f);float k=young*material.thicknessM*area/Mathf.Max(1e-8f,e.rest*e.rest);compliances[i]=1/Mathf.Max(1e-5f,k);}
            System.Array.Clear(lambdas,0,lambdas.Length);
        }
        void AddArea(int a,int b,int c)
        {
            if(!activePoints[a]||!activePoints[b]||!activePoints[c])return;
            float share=Vector3.Cross(rest[b]-rest[a],rest[c]-rest[a]).magnitude/6;
            particleAreas[a]+=share;particleAreas[b]+=share;particleAreas[c]+=share;
        }
        Vector3 AerodynamicAcceleration(int i,float dt)
        {
            int r=i/segments,s=i%segments;
            Vector3 horizontal=motionStart[r*segments+(s+1)%segments]-motionStart[r*segments+(s+segments-1)%segments];
            Vector3 vertical=motionStart[Mathf.Min(rings-1,r+1)*segments+s]-motionStart[Mathf.Max(0,r-1)*segments+s];
            Vector3 normal=Vector3.Cross(horizontal,vertical).normalized;
            float speed=Vector3.Dot(AirVelocity-(motionStart[i]-previous[i])/dt,normal);
            return Vector3.ClampMagnitude(normal*(.5f*1.225f*material.airDrag/material.arealMass*speed*Mathf.Abs(speed)),20);
        }
#endif

        public static bool IsArmOpening(int index,int segmentCount,int ringCount)
        {
            int ring=index/segmentCount;
            return ring>=2&&ring<=ringCount/3+1&&Mathf.Abs(Mathf.Cos((index%segmentCount)*Mathf.PI*2/segmentCount))>.97f;
        }

        public static void FitRestPose(Vector3[] positions,ClothCapsule[] capsules,ClothEllipsoid[] ellipsoids,float margin)
        {
            // Fit pins as well as free points BEFORE computing rest lengths. This
            // avoids fighting an undersized waistband with permanent contact.
            for(int pass=0;pass<4;pass++)for(int i=0;i<positions.Length;i++)
            {
                for(int n=0;n<ellipsoids.Length;n++)Ellipsoid(ref positions[i],ellipsoids[n].Centre,ellipsoids[n].Radius,margin);
                for(int n=0;n<capsules.Length;n++)Capsule(ref positions[i],capsules[n].A,capsules[n].B,capsules[n].Radius+margin);
            }
        }

        public void BeginGrab(Vector3 localPoint){BeginGrab(0,localPoint);}
        public bool BeginGrab(int slot,Vector3 localPoint)
        {
            if(slot<0||slot>1||!FinitePoint(localPoint)||grabIndices[slot]>=0)return false;
            float best=float.PositiveInfinity;int selected=-1,other=grabIndices[1-slot];
            for(int i=2*segments;i<Positions.Length;i++){
                if(invMass[i]==0)continue;float d=(Positions[i]-localPoint).sqrMagnitude;
                if(d<best){best=d;selected=i;}
            }
            if(selected<0)return false;
            if(other>=0){
                int columns=Mathf.Abs(selected%segments-other%segments);columns=Mathf.Min(columns,segments-columns);
                if((Mathf.Abs(selected/segments-other/segments)<=2&&columns<=2)||Vector3.Distance(Positions[selected],Positions[other])<.045f)return false;
            }
            #if !FASHIONFIT_NUMERICAL
            if(GrabCount==0&&material!=null&&material.recoverySeconds>0){System.Array.Copy(Positions,recoveryBase,Positions.Length);System.Array.Copy(lastAnchors,recoveryAnchor,Positions.Length);recoveryRemaining=0;}
#endif
            grabIndices[slot]=selected;grabOffsets[slot]=Positions[selected]-localPoint;grabStarts[slot]=grabTargets[slot]=Positions[selected];
            int ring=selected/segments,column=selected%segments;
            patches[slot,0]=selected;patches[slot,1]=ring*segments+(column+segments-1)%segments;patches[slot,2]=ring*segments+(column+1)%segments;
            patches[slot,3]=ring>2?selected-segments:-1;patches[slot,4]=ring+1<rings?selected+segments:-1;
            for(int i=0;i<5;i++){int v=patches[slot,i];patchOffsets[slot,i]=v>=0?Positions[v]-Positions[selected]:Vector3.zero;}
            return true;
        }
        static bool FinitePoint(Vector3 p)=>!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.x)&&!float.IsInfinity(p.y)&&!float.IsInfinity(p.z);
        public void MoveGrab(Vector3 localPoint){MoveGrab(0,localPoint);}
        public void MoveGrab(int slot,Vector3 localPoint){if(slot>=0&&slot<2&&grabIndices[slot]>=0&&FinitePoint(localPoint))grabTargets[slot]=grabStarts[slot]+Vector3.ClampMagnitude(localPoint+grabOffsets[slot]-grabStarts[slot],.12f);}
        public void EndGrab(int slot){if(slot>=0&&slot<2)grabIndices[slot]=-1;
#if !FASHIONFIT_NUMERICAL
            if(GrabCount==0&&material!=null)recoveryRemaining=material.recoverySeconds*6;
#endif
        }
        public void EndGrab(){EndGrab(0);EndGrab(1);}
        public void SupportUpperBody(int ringCount){supportedUpperRings=Mathf.Clamp(ringCount,0,rings);}

        public void Reset(Vector3[] anchors, ClothCapsule[] capsules, ClothEllipsoid[] ellipsoids)
        {
            EndGrab();recoveryRemaining=0;
            for (int i = 0; i < Positions.Length; i++) Positions[i] = previous[i] = anchors[i];
            // Warm-start only. Remaining settling is spread over real frames.
            for (int i = 0; i < 16; i++) Step(1f / 120f, anchors, capsules, ellipsoids);
        }

        public void Step(float dt, Vector3[] anchors, ClothCapsule[] capsules, ClothEllipsoid[] ellipsoids, float frameStart = 0, float frameEnd = 1, float localFloor = FloorY)
        {
            if (dt <= 0) return;
            activeCapsules=capsules;activeEllipsoids=ellipsoids;
            contactStart = Mathf.Clamp01(frameStart); contactEnd = Mathf.Clamp01(frameEnd); floorHeight = localFloor;
            if(contactCapsules==null||contactCapsules.Length!=capsules.Length)
            {
                contactCapsules=new ClothCapsule[capsules.Length];sweptCapsules=new ClothCapsule[capsules.Length];
                sweepLower=new Vector3[capsules.Length];sweepUpper=new Vector3[capsules.Length];capsuleMotionBounds=new float[capsules.Length];
            }
            if(contactEllipsoids==null||contactEllipsoids.Length!=ellipsoids.Length)
            {
                contactEllipsoids=new ClothEllipsoid[ellipsoids.Length];sweptEllipsoids=new ClothEllipsoid[ellipsoids.Length];
                ellipsoidLower=new Vector3[ellipsoids.Length];ellipsoidUpper=new Vector3[ellipsoids.Length];
            }
            for(int i=0;i<capsules.Length;i++)
            {
                ClothCapsule c=capsules[i];
                Vector3 a=Vector3.Lerp(c.PreviousA,c.A,contactEnd),b=Vector3.Lerp(c.PreviousB,c.B,contactEnd);
                Vector3 pa=Vector3.Lerp(c.PreviousA,c.A,contactStart),pb=Vector3.Lerp(c.PreviousB,c.B,contactStart);
                contactCapsules[i]=new ClothCapsule(a,b,c.Radius);sweptCapsules[i]=new ClothCapsule(a,b,pa,pb,c.Radius);
                Vector3 margin=Vector3.one*(c.Radius+collisionMargin);
                sweepLower[i]=Vector3.Min(Vector3.Min(pa,pb),Vector3.Min(a,b))-margin;
                sweepUpper[i]=Vector3.Max(Vector3.Max(pa,pb),Vector3.Max(a,b))+margin;
                capsuleMotionBounds[i]=Mathf.Max((a-pa).magnitude,(b-pb).magnitude);
            }
            for(int i=0;i<ellipsoids.Length;i++)
            {
                ClothEllipsoid e=ellipsoids[i];Vector3 a=Vector3.Lerp(e.PreviousCentre,e.Centre,contactStart),b=Vector3.Lerp(e.PreviousCentre,e.Centre,contactEnd);
                contactEllipsoids[i]=new ClothEllipsoid(b,e.Radius);sweptEllipsoids[i]=new ClothEllipsoid(b,a,e.Radius);
                Vector3 radius=e.Radius+Vector3.one*collisionMargin;ellipsoidLower[i]=Vector3.Min(a,b)-radius;ellipsoidUpper[i]=Vector3.Max(a,b)+radius;
            }
            System.Array.Copy(anchors,lastAnchors,anchors.Length);
            Vector3[] x = Positions;
            float inverseDtSquared=1/(dt*dt);
            System.Array.Copy(x,motionStart,x.Length);
            float damping = Mathf.Pow(.984f, dt * 120f);
#if !FASHIONFIT_NUMERICAL
            if(material!=null){damping=Mathf.Exp(-material.damping*dt);System.Array.Clear(lambdas,0,lambdas.Length);}
#endif
            for (int i = 0; i < x.Length; i++)
            {
                if (invMass[i] == 0) { x[i] = previous[i] = anchors[i]; if(activePoints[i])ProjectCollisions(ref x[i],ref previous[i],capsules,ellipsoids,collisionMargin); continue; }
                Vector3 p = x[i];
                Vector3 displacement = (x[i] - previous[i]) * damping + Vector3.down * (9.81f * dt * dt);
#if !FASHIONFIT_NUMERICAL
                if(material!=null)displacement+=AerodynamicAcceleration(i,dt)*(dt*dt);
#endif
                // A bad frame or a fast VR hand must not teleport a point through the body.
                float maxStep = Mathf.Max(.025f, collisionMargin * 3.5f);
                if (displacement.sqrMagnitude > maxStep * maxStep) displacement = displacement.normalized * maxStep;
                x[i] += displacement;
                previous[i] = p;
                LimitSweptMotion(p, ref x[i], capsules, ellipsoids);
                ProjectCollisions(ref x[i], ref previous[i], capsules, ellipsoids, collisionMargin);
            }

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                for (int n = 0; n < edges.Length; n++)
                {
                    Edge e = edges[n];
                    Vector3 d = x[e.b] - x[e.a];
                    float length = d.magnitude, sum = invMass[e.a] + invMass[e.b];
                    if (length < 1e-7f || sum == 0) continue;
                    Vector3 correction;
#if !FASHIONFIT_NUMERICAL
                    if(material!=null){
                        float alpha=compliances[n]*inverseDtSquared;if(material.youngWarpPa>0&&length<e.rest)alpha/=Mathf.Max(.001f,material.compressionRatio);
                        float deltaLambda=(-(length-e.rest)-alpha*lambdas[n])/(sum+alpha);
                        lambdas[n]+=deltaLambda;correction=-d*(deltaLambda/length);
                    }else
#endif
                    correction=d*((length-e.rest)/length*e.stiffness/sum);
                    x[e.a] += correction * invMass[e.a];
                    x[e.b] -= correction * invMass[e.b];
                }

                ApplyAnimatedTethers(anchors);
                ApplyGrabConstraint();
                for (int i = 0; i < x.Length; i++)
                {
                    if (invMass[i] == 0) { x[i] = anchors[i]; if(activePoints[i])ProjectCollisions(ref x[i],ref previous[i],capsules,ellipsoids,collisionMargin); continue; }
                    ProjectCollisions(ref x[i], ref previous[i], capsules, ellipsoids, collisionMargin);
                }
            }

            // Virtual particles are a collision sampling pass, not structural
            // constraints; once per substep is sufficient and avoids CPU spikes.
#if !FASHIONFIT_NUMERICAL
            if(recoveryRemaining>0&&GrabCount==0&&material!=null&&material.recoverySeconds>0){float factor=1-Mathf.Exp(-dt/material.recoverySeconds);recoveryRemaining-=dt;for(int i=0;i<x.Length;i++)if(invMass[i]>0){Vector3 correction=(recoveryBase[i]+anchors[i]-recoveryAnchor[i]-x[i])*factor;x[i]+=correction;previous[i]+=correction*.9f;}}
#endif
            ProjectVirtualParticles(capsules, ellipsoids);
            ProjectSelfContacts();
            // Constraints can pull points back into a collider on the final iteration.
            for(int pass=0;pass<(IsGrabbed?6:3);pass++)for (int i = 0; i < x.Length; i++)
                if (activePoints[i]) ProjectCollisions(ref x[i], ref previous[i], capsules, ellipsoids, collisionMargin);
            MaxSpeed = 0; LastPenetratingPoints = 0;
            for (int i = 0; i < x.Length; i++)
            {
                if (!activePoints[i]) continue;
                MaxSpeed = Mathf.Max(MaxSpeed, (x[i] - motionStart[i]).magnitude / dt);
                if (PointPenetrates(x[i], capsules, ellipsoids)) LastPenetratingPoints++;
            }
        }

        void ApplyAnimatedTethers(Vector3[] anchors)
        {
            for (int i = 0; i < Positions.Length; i++)
            {
                if (invMass[i] == 0) continue;
                Vector3 d = Positions[i] - anchors[i];
                float length = d.magnitude, limit = tetherLimit[i],strength=tetherStiffness;
                bool heldPatch=(grabIndices[0]>=0&&Mathf.Abs(i/segments-grabIndices[0]/segments)<=1)||(grabIndices[1]>=0&&Mathf.Abs(i/segments-grabIndices[1]/segments)<=1);
                if(i/segments<supportedUpperRings&&!heldPatch){limit=Mathf.Min(limit,.008f);strength=.45f;}
                if (length > limit && length > 1e-7f)
                    Positions[i] -= d * ((length - limit) / length * strength);
            }
        }

        // One virtual particle at each triangle centroid fills the collision gaps
        // between real mesh points.  Its correction is distributed to the triangle.
        void ProjectVirtualParticles(ClothCapsule[] capsules, ClothEllipsoid[] ellipsoids)
        {
            int slitColumn = segments / 4 - 1;
            for (int r = 0; r < rings - 1; r++)
            for (int s = 0; s < segments; s++)
            {
                if (openLowerFrontSlit && r >= rings / 2 && s == slitColumn) continue;
                int a = r * segments + s;
                int b = r * segments + (s + 1) % segments;
                int c = a + segments;
                int d = b + segments;
                ProjectVirtualTriangle(a, b, c, capsules, ellipsoids);
                ProjectVirtualTriangle(b, d, c, capsules, ellipsoids);
            }
        }

        void ProjectVirtualTriangle(int a, int b, int c, ClothCapsule[] capsules, ClothEllipsoid[] ellipsoids)
        {
            float mass = invMass[a] + invMass[b] + invMass[c];
            if(!activePoints[a]||!activePoints[b]||!activePoints[c])return;
            // Never let a virtual sample fight a sewn/pinned edge; the real
            // particles still handle collision there without stretching seams.
            if (invMass[a]<=0||invMass[b]<=0||invMass[c]<=0) return;
            Vector3 sample = (Positions[a] + Positions[b] + Positions[c]) / 3f;
            Vector3 oldSample = sample;
            ProjectCollisions(ref sample, ref oldSample, capsules, ellipsoids, collisionMargin * .72f);
            Vector3 correction = sample - (Positions[a] + Positions[b] + Positions[c]) / 3f;
            if (correction.sqrMagnitude < 1e-12f) return;
            float scale = 2.25f / mass;
            ApplyVirtualCorrection(a, correction * (invMass[a] * scale));
            ApplyVirtualCorrection(b, correction * (invMass[b] * scale));
            ApplyVirtualCorrection(c, correction * (invMass[c] * scale));
        }

        void ApplyVirtualCorrection(int index, Vector3 correction)
        {
            if (invMass[index] == 0) return;
            Positions[index] += correction;
            previous[index] += correction * .86f;
        }

        void ApplyGrabConstraint()
        {
            for(int slot=0;slot<2;slot++)if(grabIndices[slot]>=0)
                for(int n=0;n<5;n++)ApplyGrabPoint(slot,patches[slot,n],patchOffsets[slot,n],n==0?1:n<3?.58f:.45f);
        }
        void ApplyGrabPoint(int slot,int index,Vector3 offset,float weight)
        {
            if(index<0||index>=Positions.Length||invMass[index]==0)return;
            Vector3 target=grabTargets[slot]+offset;
            // Limit the hand's requested stretch relative to the other patch.
            // Existing tethers/contact projection remain authoritative.
            int other=grabIndices[1-slot];
            if(other>=0){float restDistance=Vector3.Distance(rest[grabIndices[slot]],rest[other]);target=grabTargets[1-slot]+Vector3.ClampMagnitude(target-grabTargets[1-slot],restDistance*1.35f+.025f);}
            Vector3 delta=Vector3.ClampMagnitude((target-Positions[index])*weight,.018f);
            Vector3 proposed=Positions[index]+delta;
            float start=contactStart;contactStart=contactEnd;
            LimitSweptMotion(Positions[index],ref proposed,activeCapsules,activeEllipsoids);contactStart=start;
            delta=proposed-Positions[index];Positions[index]=proposed;previous[index]=proposed-delta*.12f;
        }

        void ProjectCollisions(ref Vector3 p, ref Vector3 old, ClothCapsule[] capsules, ClothEllipsoid[] ellipsoids, float margin)
        {
            Vector3 before = p;
            for (int n = 0; n < ellipsoids.Length; n++)
            {
                if(UseBroadPhase&&!Inside(p,ellipsoidLower[n],ellipsoidUpper[n]))continue;
                ClothEllipsoid e = contactEllipsoids[n];
                Ellipsoid(ref p, e.Centre, e.Radius, margin);
            }
            for (int n = 0; n < capsules.Length; n++)
            {
                if(UseBroadPhase&&!Inside(p,sweepLower[n],sweepUpper[n]))continue;
                ClothCapsule c = contactCapsules[n];
                float radius = c.Radius + margin;
                Capsule(ref p, c.A, c.B, radius);
            }
            if (p.y < floorHeight) p.y = floorHeight;
            #if !FASHIONFIT_NUMERICAL
            if(TorsoShell!=null)TorsoShell.Project(ref p);
            #endif
            if ((p - before).sqrMagnitude > 1e-12f){
                Vector3 contact=p-before;old+=contact*.84f;
#if !FASHIONFIT_NUMERICAL
                if(material!=null){
                    Vector3 normal=contact.normalized,travel=p-old,tangent=travel-normal*Vector3.Dot(travel,normal);
                    float distance=tangent.magnitude;
                    if(distance>1e-8f)old+=tangent*Mathf.Min(1,material.friction*contact.magnitude/distance);
                }
#endif
            }
        }

        // Conservative advancement for a point against moving convex proxies.
        // The displacement bound includes point and capsule endpoint motion.
        // This is not triangle/edge CCD; final contact projection still follows it.
        void LimitSweptMotion(Vector3 from, ref Vector3 to, ClothCapsule[] capsules, ClothEllipsoid[] ellipsoids)
        {
            Vector3 proposed = to, travel = proposed - from;
            Vector3 pathMin=Vector3.Min(from,proposed),pathMax=Vector3.Max(from,proposed);
            float travelLength=travel.magnitude;
            bool stationary=contactStart==contactEnd;
            float allowed = 1;
            for (int i = 0; i < ellipsoids.Length; i++)
            {
                if(!Overlaps(pathMin,pathMax,ellipsoidLower[i],ellipsoidUpper[i]))continue;
                ClothEllipsoid e = sweptEllipsoids[i];
                Vector3 a = stationary?e.Centre:e.PreviousCentre;
                Vector3 b = e.Centre;
                Vector3 radius = e.Radius + Vector3.one * collisionMargin;
                Vector3 u = from - a, v = travel - (b - a);
                u = new Vector3(u.x/radius.x,u.y/radius.y,u.z/radius.z);
                v = new Vector3(v.x/radius.x,v.y/radius.y,v.z/radius.z);
                float aa = Vector3.Dot(v,v), bb = Vector3.Dot(u,v), cc = Vector3.Dot(u,u)-1;
                if (cc <= .00001f || aa < 1e-12f || bb >= 0) continue;
                float discriminant = bb*bb-aa*cc;
                if (discriminant < 0) continue;
                float hit = (-bb-Mathf.Sqrt(discriminant))/aa;
                if (hit >= 0 && hit < allowed) allowed = Mathf.Max(0,hit-.001f);
            }
            for (int i = 0; i < capsules.Length; i++)
            {
                if(!Overlaps(pathMin,pathMax,sweepLower[i],sweepUpper[i]))continue;
                ClothCapsule c = sweptCapsules[i];
                Vector3 a0=stationary?c.A:c.PreviousA,a1=c.A;
                Vector3 b0=stationary?c.B:c.PreviousB,b1=c.B;
                float radius=c.Radius+collisionMargin;
                float bound=travelLength+(stationary?0:capsuleMotionBounds[i]);
                if(bound<1e-8f)continue;
                float t=0;
                for(int k=0;k<12;k++)
                {
                    float gap=CapsuleGap(from+travel*t,Vector3.Lerp(a0,a1,t),Vector3.Lerp(b0,b1,t),radius);
                    if(t==0&&gap<=.00001f)break; // existing contacts are resolved by projection
                    if(gap<=.00001f){allowed=Mathf.Min(allowed,Mathf.Max(0,t-.001f));break;}
                    float advance=gap/bound*.90f;
                    if(t+advance>=allowed)break;
                    t+=advance;
                    if(k==11)allowed=Mathf.Min(allowed,t);
                }
            }
            to=from+travel*allowed;
        }

        static bool Overlaps(Vector3 aMin,Vector3 aMax,Vector3 bMin,Vector3 bMax)
        {
            return aMax.y>=bMin.y&&aMin.y<=bMax.y&&aMax.x>=bMin.x&&aMin.x<=bMax.x&&aMax.z>=bMin.z&&aMin.z<=bMax.z;
        }
        static bool Inside(Vector3 p,Vector3 lower,Vector3 upper){return p.x>=lower.x&&p.x<=upper.x&&p.y>=lower.y&&p.y<=upper.y&&p.z>=lower.z&&p.z<=upper.z;}

        static float CapsuleGap(Vector3 p,Vector3 a,Vector3 b,float radius)
        {
            Vector3 ab=b-a;
            float t=Mathf.Clamp01(Vector3.Dot(p-a,ab)/Mathf.Max(1e-8f,ab.sqrMagnitude));
            return (p-(a+ab*t)).magnitude-radius;
        }

        bool PointPenetrates(Vector3 p,ClothCapsule[] capsules,ClothEllipsoid[] ellipsoids)
        {
            const float tolerance=.001f;
            #if !FASHIONFIT_NUMERICAL
            if(TorsoShell!=null&&TorsoShell.Clearance(p)<-tolerance)return true;
            #endif
            if(p.y<floorHeight-tolerance)return true;
            for(int n=0;n<ellipsoids.Length;n++)
            {
                if(UseBroadPhase&&!Inside(p,ellipsoidLower[n],ellipsoidUpper[n]))continue;
                ClothEllipsoid e=contactEllipsoids[n];Vector3 r=e.Radius+Vector3.one*(collisionMargin-tolerance);
                Vector3 d=p-e.Centre;
                if(d.x*d.x/(r.x*r.x)+d.y*d.y/(r.y*r.y)+d.z*d.z/(r.z*r.z)<1)return true;
            }
            for(int n=0;n<capsules.Length;n++)
            {
                if(UseBroadPhase&&!Inside(p,sweepLower[n],sweepUpper[n]))continue;
                ClothCapsule c=contactCapsules[n];Vector3 ab=c.B-c.A;
                float t=Mathf.Clamp01(Vector3.Dot(p-c.A,ab)/Mathf.Max(1e-8f,ab.sqrMagnitude));
                float radius=c.Radius+collisionMargin-tolerance;
                if((p-c.A-ab*t).sqrMagnitude<radius*radius)return true;
            }
            return false;
        }

        static int Hash(int x,int y,int z){unchecked{return ((x*73856093)^(y*19349663)^(z*83492791))&2047;}}
        void ProjectSelfContacts()
        {
            const float spacing=.010f;
            for(int i=0;i<hashHeads.Length;i++)hashHeads[i]=-1;
            for(int i=0;i<Positions.Length;i++)
            {
                if(!activePoints[i]){hashNext[i]=-1;continue;}
                Vector3 p=Positions[i];int x=Mathf.FloorToInt(p.x/spacing),y=Mathf.FloorToInt(p.y/spacing),z=Mathf.FloorToInt(p.z/spacing);
                int bucket=Hash(x,y,z);hashNext[i]=hashHeads[bucket];hashHeads[bucket]=i;
            }
            for(int i=0;i<Positions.Length;i++)
            {
                if(!activePoints[i])continue;
                Vector3 p=Positions[i];int x=Mathf.FloorToInt(p.x/spacing),y=Mathf.FloorToInt(p.y/spacing),z=Mathf.FloorToInt(p.z/spacing);
                for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)for(int dz=-1;dz<=1;dz++)
                for(int j=hashHeads[Hash(x+dx,y+dy,z+dz)];j>=0;j=hashNext[j])
                {
                    if(j<=i)continue;
                    int column=Mathf.Abs(i%segments-j%segments);column=Mathf.Min(column,segments-column);
                    if(Mathf.Abs(i/segments-j/segments)<=2&&column<=2)continue;
                    float mass=invMass[i]+invMass[j];if(mass==0)continue;
                    Vector3 d=Positions[j]-Positions[i];float squared=d.sqrMagnitude;
                    if(squared>=spacing*spacing)continue;
                    float distance=Mathf.Sqrt(squared);
                    Vector3 correction=(distance>1e-7f?d/distance:Vector3.forward)*((spacing-distance)*.65f/mass);
                    Positions[i]-=correction*invMass[i];Positions[j]+=correction*invMass[j];
                }
            }
        }

        #if !FASHIONFIT_NUMERICAL
        public void TransformState(Matrix4x4 oldToNew)
        {
            for(int i=0;i<recoveryBase.Length;i++){recoveryBase[i]=oldToNew.MultiplyPoint3x4(recoveryBase[i]);recoveryAnchor[i]=oldToNew.MultiplyPoint3x4(recoveryAnchor[i]);lastAnchors[i]=oldToNew.MultiplyPoint3x4(lastAnchors[i]);}
            for(int slot=0;slot<2;slot++)if(grabIndices[slot]>=0){
                grabStarts[slot]=oldToNew.MultiplyPoint3x4(grabStarts[slot]);
                grabOffsets[slot]=oldToNew.MultiplyVector(grabOffsets[slot]);
                for(int n=0;n<5;n++)patchOffsets[slot,n]=oldToNew.MultiplyVector(patchOffsets[slot,n]);
            }
            // Preserve world position and velocity when the containing rig moves.
            for(int i=0;i<Positions.Length;i++)
            {
                Positions[i]=oldToNew.MultiplyPoint3x4(Positions[i]);
                previous[i]=oldToNew.MultiplyPoint3x4(previous[i]);
            }
        }

        public void ProjectRenderPoint(ref Vector3 point,ClothCapsule[] capsules,ClothEllipsoid[] ellipsoids)
        {
            Vector3 old=point;
            for(int pass=0;pass<2;pass++)ProjectCollisions(ref point,ref old,capsules,ellipsoids,collisionMargin);
        }
        #endif

        static void Capsule(ref Vector3 p, Vector3 a, Vector3 b, float radius)
        {
            if(p.y<Mathf.Min(a.y,b.y)-radius||p.y>Mathf.Max(a.y,b.y)+radius||
               p.x<Mathf.Min(a.x,b.x)-radius||p.x>Mathf.Max(a.x,b.x)+radius||
               p.z<Mathf.Min(a.z,b.z)-radius||p.z>Mathf.Max(a.z,b.z)+radius)return;
            Vector3 ab = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(1e-8f, ab.sqrMagnitude));
            Vector3 q = a + ab * t, d = p - q;
            if(d.sqrMagnitude>=radius*radius)return;
            float length = d.magnitude;
            if (length < radius) p = q + (length > 1e-7f ? d / length : Vector3.forward) * radius;
        }

        static void Ellipsoid(ref Vector3 p, Vector3 centre, Vector3 radius, float margin)
        {
            Vector3 expanded = radius + Vector3.one * margin;
            Vector3 d = p - centre;
            if(Mathf.Abs(d.y)>=expanded.y||Mathf.Abs(d.x)>=expanded.x||Mathf.Abs(d.z)>=expanded.z)return;
            Vector3 q = new Vector3(d.x / expanded.x, d.y / expanded.y, d.z / expanded.z);
            if(q.sqrMagnitude>=1)return;
            float length = q.magnitude;
            if (length >= 1) return;
            p = centre + (length > 1e-7f ? d / length : Vector3.forward * expanded.z);
        }

        public float MaxStretch()
        {
            float max = 1;
            for (int n = 0; n < edges.Length; n++)
            {
                Edge e = edges[n];
                max = Mathf.Max(max, Vector3.Distance(Positions[e.a], Positions[e.b]) / e.rest);
            }
            return max;
        }

        public string StretchDiagnostic()
        {
            float max=0;Edge worst=new Edge();
            for(int n=0;n<edges.Length;n++){Edge e=edges[n];float ratio=Vector3.Distance(Positions[e.a],Positions[e.b])/e.rest;if(ratio>max){max=ratio;worst=e;}}
            return "ratio="+max+" a="+worst.a+" b="+worst.b+" rest="+worst.rest+" p="+Positions[worst.a]+" q="+Positions[worst.b];
        }

    }
}
