using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FashionFit
{
    // Free particles have no target on the original body. Only a held patch is pinned.
    public sealed class ContactClothPart : IDisposable
    {
        GarmentContactWorld world;
        ElasticSurface surface;
        GameObject simulation;
        Cloth cloth;
        Mesh mesh;
        Vector3[] reference, rest, positions;
        int[] renderToParticle, triangles;
        ContactClothMesh topology;
        BoneWeight[] weights;
        ClothSkinningCoefficient[] coefficients;
        readonly Transform[] anchors = new Transform[9];
        readonly int[] particles = { -1, -1, -1, -1, -1, -1, -1, -1 };
        readonly Vector3[] targets = new Vector3[8], origins = new Vector3[8], pinOrigins = new Vector3[8];
        readonly MannequinGarmentHold[] supports = new MannequinGarmentHold[6];
        int publishedFrame = -1;
        MembraneMaterial appliedMaterial;
        float nextCollisionRefresh;
        public bool Active => cloth;
        public int SupportCount { get { int n = 0; foreach (var hand in supports) if (hand) n++; return n; } }
        public int ParticleCount => rest == null ? 0 : rest.Length;
        public double Milliseconds { get; private set; }
        public float MaximumStrain { get; private set; }
        public float AcceptedTravel { get; private set; }

        Transform transform => surface.transform;
        string name => surface.name;
        public bool Covers(int renderIndex) => renderToParticle != null && renderToParticle[renderIndex] >= 0;
        public ContactClothPart(GarmentContactWorld contactWorld, ElasticSurface garment)
        { world = contactWorld; surface = garment; }

        public void Activate(int selected = 0)
        {
            if (Active) return;
            world.EnsureInitialized();
            reference = (Vector3[])surface.CurrentPoints.Clone();
            var source = new Vector3[reference.Length];
            for (int i = 0; i < source.Length; i++) source[i] = transform.TransformPoint(reference[i]);
            topology = new ContactClothMesh(source, surface.MeshAt(0).triangles, selected);
            rest = topology.Points;
            renderToParticle = topology.ParticleForRender;
            positions = (Vector3[])rest.Clone();
            triangles = topology.Faces;
            simulation = new GameObject(name + " contact cloth");
            simulation.transform.SetParent(world.transform, false);
            simulation.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            simulation.transform.localScale = Vector3.one;
            var bindposes = new Matrix4x4[anchors.Length];
            for (int i = 0; i < anchors.Length; i++)
            {
                anchors[i] = new GameObject("Held patch " + i).transform;
                anchors[i].SetParent(simulation.transform, false);
                bindposes[i] = Matrix4x4.identity;
            }
            weights = new BoneWeight[rest.Length];
            for (int i = 0; i < weights.Length; i++) weights[i] = new BoneWeight { boneIndex0 = 0, weight0 = 1 };
            mesh = new Mesh { name = "World-space contact fabric", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = rest; mesh.triangles = triangles; mesh.boneWeights = weights; mesh.bindposes = bindposes;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var skin = simulation.AddComponent<SkinnedMeshRenderer>();
            skin.sharedMesh = mesh; skin.bones = anchors; skin.rootBone = anchors[0];
            skin.updateWhenOffscreen = true; skin.forceRenderingOff = true;
            skin.localBounds = new Bounds(new Vector3(0, 1, 0), Vector3.one * 30);
            cloth = simulation.AddComponent<Cloth>();
            cloth.useGravity = true;
            cloth.damping = .15f;
            cloth.stretchingStiffness = 1;
            cloth.bendingStiffness = .05f;
            cloth.clothSolverFrequency = 1200;
            cloth.stiffnessFrequency = 1200;
            cloth.friction = .75f;
            cloth.collisionMassScale = 3;
            cloth.useTethers = false;
            cloth.enableContinuousCollision = true;
            cloth.useVirtualParticles = 1;
            cloth.worldVelocityScale = 0;
            cloth.worldAccelerationScale = 0;
            coefficients = cloth.coefficients;
            if (coefficients.Length != rest.Length) throw new InvalidOperationException("Contact fabric vertex mapping changed");
            // The room is eight metres wide. A finite twenty-metre envelope is
            // unreachable during play and avoids unbounded native radii.
            for (int i = 0; i < coefficients.Length; i++) coefficients[i].maxDistance = 20;
            cloth.coefficients = coefficients;
            var initial = cloth.vertices; float mismatch = 0;
            for (int i = 0; i < rest.Length; i++) mismatch = Mathf.Max(mismatch, Vector3.Distance(initial[i], rest[i]));
            Debug.Log("Native cloth particles=" + rest.Length + " initialMappingError=" + mismatch);
            var collisionIndices = new List<uint>();
            for (int i = 0; i < rest.Length; i++)
            {
                bool separated = true;
                foreach (uint other in collisionIndices)
                    if ((rest[i] - rest[other]).sqrMagnitude < .000144f) { separated = false; break; }
                if (separated) collisionIndices.Add((uint)i);
            }
            cloth.SetSelfAndInterCollisionIndices(collisionIndices);
            cloth.selfCollisionDistance = .006f;
            cloth.selfCollisionStiffness = .7f;
            world.Configure(cloth);
        }

        bool Pin(int slot, int index, Vector3 controller)
        {
            if (index < 0) return false;
            for (int i = 0; i < particles.Length; i++) if (i != slot && particles[i] == index) return false;
            particles[slot] = index;
            origins[slot] = targets[slot] = controller;
            pinOrigins[slot] = positions[index] - rest[index];
            anchors[slot + 1].position = pinOrigins[slot];
            weights[index] = new BoneWeight { boneIndex0 = slot + 1, weight0 = 1 };
            mesh.boneWeights = weights;
            coefficients[index].maxDistance = 0;
            cloth.coefficients = coefficients;
            return true;
        }
        public bool BeginGrip(int slot, int renderIndex, Vector3 controller)
        { Activate(renderIndex); return Pin(slot, renderToParticle[renderIndex], controller); }
        public void MoveGrip(int slot, Vector3 p) { if (particles[slot] >= 0) targets[slot] = p; }
        public void EndGrip(int slot)
        {
            int index = particles[slot];
            if (index < 0 || !cloth) return;
            coefficients[index].maxDistance = 20;
            weights[index] = new BoneWeight { boneIndex0 = 0, weight0 = 1 };
            mesh.boneWeights = weights;
            cloth.coefficients = coefficients;
            particles[slot] = -1;
        }
        public bool NearestParticle(Vector3 p, float reach, out int index, out Vector3 point)
        {
            index = -1; point = p;
            if (!Active) return surface.Nearest(p, reach, out point);
            float best = reach * reach;
            for (int i = 0; i < positions.Length; i++)
            {
                float d = (positions[i] - p).sqrMagnitude;
                if (d < best) { index = i; point = positions[i]; best = d; }
            }
            return index >= 0;
        }
        public bool AddSupport(MannequinGarmentHold hand, int particle)
        {
            for (int i = 0; i < supports.Length; i++)
                if (!supports[i] && Pin(i + 2, particle, hand.Position)) { supports[i] = hand; return true; }
            return false;
        }
        public void ReleaseSupport(MannequinGarmentHold hand)
        {
            for (int i = 0; i < supports.Length; i++)
                if (supports[i] == hand) { EndGrip(i + 2); supports[i] = null; }
        }
        public void Update()
        {
            if (!Active) return;
            if (Time.unscaledTime >= nextCollisionRefresh)
            {
                nextCollisionRefresh = Time.unscaledTime + .1f;
                world.RefreshClothCollisions(cloth, positions);
            }
            if (appliedMaterial != surface.Material)
            {
                appliedMaterial = surface.Material;
                // Empirical native-cloth controls. These are not an E-to-stiffness calibration.
                cloth.stretchingStiffness = appliedMaterial.youngWarpPa < 50000 ? .98f : 1;
                cloth.bendingStiffness = Mathf.Clamp(appliedMaterial.SheetWarp / 3600, .015f, .5f);
                cloth.damping = Mathf.Clamp(.08f + appliedMaterial.viscousSeconds * .2f, .08f, .25f);
            }
            for (int i = 0; i < supports.Length; i++) if (supports[i]) targets[i + 2] = supports[i].Position;
            for (int i = 0; i < particles.Length; i++)
                if (particles[i] >= 0)
                {
                    Vector3 current = rest[particles[i]] + anchors[i + 1].position;
                    Vector3 wanted = rest[particles[i]] + pinOrigins[i] + targets[i] - origins[i];
                    Vector3 accepted = Vector3.MoveTowards(current, wanted, 1.2f * Time.deltaTime);
                    if (i < 2) accepted = world.ProjectHeldPoint(accepted);
                    anchors[i + 1].position = accepted - rest[particles[i]];
                }
        }
        public void Publish(Vector3[] merged)
        {
            if (!Active || publishedFrame == Time.frameCount) return;
            publishedFrame = Time.frameCount;
            long start = System.Diagnostics.Stopwatch.GetTimestamp();
            positions = cloth.vertices;
            AcceptedTravel = 0;
            for (int i = 0; i < particles.Length; i++)
                if (particles[i] >= 0) AcceptedTravel = Mathf.Max(AcceptedTravel, (positions[particles[i]] - rest[particles[i]]).magnitude);
            var inverse = transform.worldToLocalMatrix;
            topology.Publish(positions, merged, inverse);
            MaximumStrain = 0;
            for (int i = 0; i < triangles.Length; i += 3)
                for (int k = 0; k < 3; k++)
                {
                    int a = triangles[i + k], b = triangles[i + (k + 1) % 3];
                    float length = Vector3.Distance(rest[a], rest[b]);
                    if (length > .015f) MaximumStrain = Mathf.Max(MaximumStrain, Vector3.Distance(positions[a], positions[b]) / length - 1);
                }
            Milliseconds = (System.Diagnostics.Stopwatch.GetTimestamp() - start) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        }
        public void ResetPlacement()
        {
            foreach (var hand in supports) if (hand) hand.Toggle();
            for (int i = 0; i < particles.Length; i++) particles[i] = -1;
            if (simulation) { if (Application.isPlaying) UnityEngine.Object.Destroy(simulation); else UnityEngine.Object.DestroyImmediate(simulation); }
            if (mesh) { if (Application.isPlaying) UnityEngine.Object.Destroy(mesh); else UnityEngine.Object.DestroyImmediate(mesh); }
            cloth = null;
        }
        public void Dispose() { ResetPlacement(); }
    }
}
