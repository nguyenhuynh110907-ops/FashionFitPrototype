using UnityEngine;
using UnityEngine.Rendering;

namespace FashionFit
{
    public sealed class FittingCloth : MonoBehaviour
    {
        RunwayAvatar body;
        FashionFitApp app;
        Mesh mesh, bodiceMesh;
        ClothDisplayMesh display;
        Vector3[] rest, anchors,previousAnchors,halfAnchors;
        Matrix4x4 previousLocalToWorld;
        Matrix4x4[] skinMatrices;
        readonly ClothCapsule[] capsules = new ClothCapsule[8];
        ClothEllipsoid[] ellipsoids;
        readonly Vector3[] previousA = new Vector3[8], previousB = new Vector3[8];
        readonly Vector3[] previousTorsoCentre = new Vector3[2];
        FittingClothSolver solver;
        int hips, publishFrame;
        bool proxiesReady;
        Bounds stableBounds;

        public MeshRenderer Surface { get; private set; }
        public SkinnedMeshRenderer BodiceSurface { get; private set; }
        public float MaxStretch { get { return solver != null ? solver.MaxStretch() : 1; } }
        public int GrabCount=>solver!=null?solver.GrabCount:0;
        public bool IsGrabbed { get { return solver != null && solver.IsGrabbed; } }
        public float ClothSpeed { get { return solver != null ? solver.MaxSpeed : 0; } }
        public int ProxyPenetrations { get { return solver != null ? solver.LastPenetratingPoints : 0; } }
        public double PhysicsMilliseconds {get;private set;}
        public double DisplayMilliseconds {get;private set;}
        public bool SimulationReady=>body&&solver!=null&&display!=null&&anchors!=null&&previousAnchors!=null&&halfAnchors!=null&&skinMatrices!=null;
        public DressPreset Preset { get; private set; }
        public MembraneReading MaterialReading=>solver!=null?solver.MaterialReading:new MembraneReading();
        public float MaterialMass=>solver!=null?solver.TotalMass:0;
        public void SetPhysicsMaterial(ClothMaterialProfile profile){if(solver!=null)solver.SetMaterial(profile);}
        public void SetAirVelocity(Vector3 worldVelocity){if(solver!=null)solver.AirVelocity=transform.InverseTransformDirection(worldVelocity);}

        public void Initialize(RunwayAvatar avatar, DressPreset preset)
        {
            body = avatar; app = avatar.GetComponentInParent<FashionFitApp>(); Preset = preset;
            hips = System.Array.IndexOf(body.Bones, body.RootBone);
            if (hips < 0) hips = 0;
            skinMatrices = new Matrix4x4[body.Bones.Length];
            SampleSkirtGeometry.Build(preset, out rest, out Vector2[] uv, out int[] indices);
            ellipsoids=new ClothEllipsoid[2];UpdateCollisionProxies();
            float margin=BodyFitProfile.ContactMargin(preset);
            FittingClothSolver.FitRestPose(rest,capsules,ellipsoids,margin+.002f);
            anchors = new Vector3[rest.Length];
            previousAnchors=new Vector3[rest.Length];halfAnchors=new Vector3[rest.Length];
            bool fitted=preset==DressPreset.EmeraldColumn||preset==DressPreset.IvoryShirtSet||preset==DressPreset.GucciPoloStudy;
            float tether=preset==DressPreset.RoseCouture?.14f:preset==DressPreset.SapphireCocktail?.22f:fitted?.20f:.35f;
            solver = new FittingClothSolver(rest, SampleSkirtGeometry.Segments, SampleSkirtGeometry.Rings, 2, false, margin, preset == DressPreset.EmeraldColumn ? 7 : 5,tether,preset==DressPreset.RoseCouture?.12f:fitted?.10f:.07f);
            if(!body.IsOriginal)solver.TorsoShell=new BodyContactShell();
            bool wide = preset == DressPreset.RoseCouture || preset == DressPreset.SapphireCocktail;
            stableBounds = new Bounds(new Vector3(0, .65f, .02f), wide ? new Vector3(1.25f, 1.55f, 1.15f) : new Vector3(.95f, 1.65f, .85f));

            display = new ClothDisplayMesh(preset + " display - 672 point physics cage",rest,uv,indices,stableBounds);
            mesh = display.Mesh;
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            Surface = gameObject.AddComponent<MeshRenderer>();
            Surface.shadowCastingMode = ShadowCastingMode.On; Surface.receiveShadows = true;
            BuildBodice();
            BuildGrabVolume();
            ResetState();
        }

        void BuildBodice()
        {
            bodiceMesh = SampleSkirtGeometry.BuildBodice(Preset);
            if (bodiceMesh == null) return;
            bodiceMesh.bindposes = body.GarmentBindposes;
            var weights = new BoneWeight[bodiceMesh.vertexCount];
            int spine = body.BoneIndex("Spine"), chest = body.BoneIndex("Spine2");
            Vector3[] vertices = bodiceMesh.vertices;
            // The lower bodice uses exactly the same seam as the physics cage.
            // A small vertical overlap prevents a gap during animated skinning.
            for(int i=0;i<64;i++)
            {
                float column=i*SampleSkirtGeometry.Segments/64f;int a=Mathf.FloorToInt(column),b=(a+1)%SampleSkirtGeometry.Segments;
                vertices[i]=Vector3.Lerp(rest[a],rest[b],column-a)+Vector3.down*.008f;
            }
            bodiceMesh.vertices=vertices;bodiceMesh.RecalculateNormals();bodiceMesh.RecalculateTangents();
            for (int i = 0; i < weights.Length; i++)
            {
                float t = Mathf.InverseLerp(BodyFitProfile.WaistY, 1.335f, vertices[i].y);
                if (t < .32f)
                {
                    float blend = Mathf.SmoothStep(0, 1, t / .32f);
                    weights[i] = new BoneWeight { boneIndex0 = hips, weight0 = 1 - blend, boneIndex1 = spine, weight1 = blend };
                }
                else
                {
                    float blend = Mathf.SmoothStep(0, 1, (t - .32f) / .68f);
                    weights[i] = new BoneWeight { boneIndex0 = spine, weight0 = 1 - blend, boneIndex1 = chest, weight1 = blend };
                }
            }
            bodiceMesh.boneWeights = weights;
            var go = new GameObject(Preset + " fitted bodice");
            go.transform.SetParent(transform, false);
            BodiceSurface = go.AddComponent<SkinnedMeshRenderer>();
            BodiceSurface.sharedMesh = bodiceMesh; BodiceSurface.bones = body.Bones; BodiceSurface.rootBone = body.RootBone;
            BodiceSurface.localBounds = new Bounds(new Vector3(0, 1.24f, .045f), new Vector3(.82f, .74f, .72f));
            SkinnedBounds.SetFromModel(BodiceSurface,body.transform,bodiceMesh.bounds,.08f);
            BodiceSurface.quality=SkinQuality.Bone2;
            BodiceSurface.shadowCastingMode = ShadowCastingMode.On; BodiceSurface.receiveShadows = true;
        }

        void BuildGrabVolume()
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0, .68f, .02f);
            box.size = Preset == DressPreset.RoseCouture || Preset == DressPreset.SapphireCocktail ? new Vector3(.95f, 1.05f, .82f) : new Vector3(.70f, 1.18f, .62f);
            gameObject.layer = 9;
            var target = gameObject.AddComponent<RunwayVRGrabTarget>();
            target.ConfigureCloth(this);
        }

        public void SetMaterial(Material material)
        {
            if (Surface) Surface.sharedMaterial = material;
            if (BodiceSurface) BodiceSurface.sharedMaterial = material;
        }

        void UpdateCollisionProxies()
        {
            Vector3 leftHip=body.Joint("LeftUpLeg"),rightHip=body.Joint("RightUpLeg"),leftKnee=body.Joint("LeftLeg"),rightKnee=body.Joint("RightLeg"),leftFoot=body.Joint("LeftFoot"),rightFoot=body.Joint("RightFoot");
            Vector3 origin=body.Joint("Hips");
            const float hipSection=.88f;
            float waistSection=BodyFitProfile.WaistY-.035f;
            Vector3 hip=origin+new Vector3(0,hipSection-.892425f,BodyFitProfile.CentreZ(hipSection)-.025177f);
            Vector3 waist=origin+new Vector3(0,waistSection-.892425f,BodyFitProfile.CentreZ(waistSection)-.025177f);
            if (!proxiesReady)
            {
                Vector3 leftUpper=Vector3.Lerp(leftHip,leftKnee,.48f),rightUpper=Vector3.Lerp(rightHip,rightKnee,.48f);
                Vector3 leftLower=Vector3.Lerp(leftKnee,leftFoot,.52f),rightLower=Vector3.Lerp(rightKnee,rightFoot,.52f);
                previousA[0]=leftHip;previousB[0]=leftUpper;previousA[1]=leftUpper;previousB[1]=leftKnee;
                previousA[2]=leftKnee;previousB[2]=leftLower;previousA[3]=leftLower;previousB[3]=leftFoot;
                previousA[4]=rightHip;previousB[4]=rightUpper;previousA[5]=rightUpper;previousB[5]=rightKnee;
                previousA[6]=rightKnee;previousB[6]=rightLower;previousA[7]=rightLower;previousB[7]=rightFoot;
                previousTorsoCentre[0] = hip; previousTorsoCentre[1] = waist; proxiesReady = true;
            }
            Vector3 lu=Vector3.Lerp(leftHip,leftKnee,.48f),ru=Vector3.Lerp(rightHip,rightKnee,.48f),ll=Vector3.Lerp(leftKnee,leftFoot,.52f),rl=Vector3.Lerp(rightKnee,rightFoot,.52f);
            // The calf bulge extends beyond the old .078 m proxy when a hand
            // presses a straight skirt against it. Retain 8 mm extra clearance.
            SetCapsule(0,leftHip,lu,.112f);SetCapsule(1,lu,leftKnee,.095f);SetCapsule(2,leftKnee,ll,.086f);SetCapsule(3,ll,leftFoot,.060f);
            SetCapsule(4,rightHip,ru,.112f);SetCapsule(5,ru,rightKnee,.095f);SetCapsule(6,rightKnee,rl,.086f);SetCapsule(7,rl,rightFoot,.060f);
            Vector3 hipRadius=new Vector3(BodyFitProfile.Radius(hipSection,0),.135f,BodyFitProfile.Depth(hipSection));
            ellipsoids[0] = new ClothEllipsoid(hip, previousTorsoCentre[0], hipRadius);
            ellipsoids[1] = new ClothEllipsoid(waist, previousTorsoCentre[1], new Vector3(BodyFitProfile.Radius(waistSection,0),.100f,BodyFitProfile.Depth(waistSection)));
            previousTorsoCentre[0] = hip; previousTorsoCentre[1] = waist;
        }

        void SetCapsule(int index,Vector3 a,Vector3 b,float radius)
        {
            capsules[index]=new ClothCapsule(a,b,previousA[index],previousB[index],radius);previousA[index]=a;previousB[index]=b;
        }

        void UpdateAnchors()
        {
            body.FillSkinMatrices(skinMatrices);
            Matrix4x4 m = skinMatrices[hips];
            if(solver!=null&&solver.TorsoShell!=null)solver.TorsoShell.Update(m,.72f,1.16f,BodyFitProfile.ContactMargin(Preset)+.004f);
            for (int i = 0; i < rest.Length; i++) anchors[i] = m.MultiplyPoint3x4(rest[i]);
        }

        public void ResetState()
        {
            if (solver == null) return;
            proxiesReady = false; UpdateAnchors(); UpdateCollisionProxies();
            previousLocalToWorld=transform.localToWorldMatrix;
            System.Array.Copy(anchors,previousAnchors,anchors.Length);
            solver.Reset(anchors, capsules, ellipsoids); Publish(true);
        }

        public void Step(float dt)
        {
            if(!SimulationReady)return;
            long started=System.Diagnostics.Stopwatch.GetTimestamp();
            Matrix4x4 oldToNew=transform.worldToLocalMatrix*previousLocalToWorld;
            solver.TransformState(oldToNew);previousLocalToWorld=transform.localToWorldMatrix;
            for(int i=0;i<previousAnchors.Length;i++)previousAnchors[i]=oldToNew.MultiplyPoint3x4(previousAnchors[i]);
            for(int i=0;i<previousA.Length;i++){previousA[i]=oldToNew.MultiplyPoint3x4(previousA[i]);previousB[i]=oldToNew.MultiplyPoint3x4(previousB[i]);}
            for(int i=0;i<previousTorsoCentre.Length;i++)previousTorsoCentre[i]=oldToNew.MultiplyPoint3x4(previousTorsoCentre[i]);
            UpdateAnchors(); UpdateCollisionProxies();
            for(int i=0;i<anchors.Length;i++)halfAnchors[i]=Vector3.Lerp(previousAnchors[i],anchors[i],.5f);
            float half = dt * .5f;
            float localFloor=transform.InverseTransformPoint(new Vector3(transform.position.x,.012f,transform.position.z)).y;
            bool subdivide=IsGrabbed;
            for(int i=0;i<capsules.Length;i++)if((capsules[i].A-capsules[i].PreviousA).sqrMagnitude>.000625f||(capsules[i].B-capsules[i].PreviousB).sqrMagnitude>.000625f)subdivide=true;
            if(subdivide){solver.Step(half,halfAnchors,capsules,ellipsoids,0,.5f,localFloor);solver.Step(half,anchors,capsules,ellipsoids,.5f,1,localFloor);}
            else solver.Step(dt,anchors,capsules,ellipsoids,0,1,localFloor);
            System.Array.Copy(anchors,previousAnchors,anchors.Length);
            long physicsDone=System.Diagnostics.Stopwatch.GetTimestamp();
            Publish(false);
            long displayDone=System.Diagnostics.Stopwatch.GetTimestamp();
            PhysicsMilliseconds=(physicsDone-started)*1000.0/System.Diagnostics.Stopwatch.Frequency;
            DisplayMilliseconds=(displayDone-physicsDone)*1000.0/System.Diagnostics.Stopwatch.Frequency;
        }

        public void BeginVRGrab(Vector3 worldPoint){BeginVRGrab(0,worldPoint);}
        public bool BeginVRGrab(int slot,Vector3 worldPoint)
        {
            if(!SimulationReady||!solver.BeginGrab(slot,body.transform.InverseTransformPoint(worldPoint)))return false;
            if(app!=null)app.NotifyClothGrab(true);return true;
        }
        public void MoveVRGrab(Vector3 worldPoint){MoveVRGrab(0,worldPoint);}
        public void MoveVRGrab(int slot,Vector3 worldPoint){if(SimulationReady)solver.MoveGrab(slot,body.transform.InverseTransformPoint(worldPoint));}
        public void EndVRGrab(int slot){if(solver!=null)solver.EndGrab(slot);if(app!=null&&app.Ready)app.NotifyClothGrab(IsGrabbed);}
        public void EndVRGrab(){if(solver!=null)solver.EndGrab();if(app!=null&&app.Ready)app.NotifyClothGrab(false);}

        void Publish(bool forceTangents)
        {
            display.Publish(solver,capsules,ellipsoids,forceTangents||(++publishFrame%12)==0);
            mesh.bounds = stableBounds;
        }

        void OnDestroy()
        {
            if (mesh) {if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}
            if (bodiceMesh) {if(Application.isPlaying)Destroy(bodiceMesh);else DestroyImmediate(bodiceMesh);}
        }
    }
}
