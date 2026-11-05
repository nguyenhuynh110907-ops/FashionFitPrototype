using System.Collections.Generic;
using UnityEngine;

namespace FashionFit
{
    // Render-only removal of triangles wholly under closed authored clothing.
    // The original body mesh stays intact for measurements and collision QA.
    public sealed class RunwayBodyCoverage
    {
        readonly RunwayAvatar body;
        Mesh masked;
        bool enabled = true;
        public int HiddenTriangles { get; private set; }
        public RunwayBodyCoverage(RunwayAvatar avatar) { body = avatar; }
        public void SetEnabled(bool value) { enabled = value; Apply(); }
        void Apply() { if (body.Renderer) { body.Renderer.sharedMesh = enabled && masked ? masked : body.BodyMesh; RefreshBounds(); } }
        void RefreshBounds(){if(body.Renderer&&body.BodyMesh&&!body.IsOriginal)SkinnedBounds.SetFromModel(body.Renderer,body.transform,body.BodyMesh.bounds,.18f);}
        public void Clear()
        {
            if (body.Renderer) body.Renderer.sharedMesh = body.BodyMesh;
            RefreshBounds();
            if (masked) { if (Application.isPlaying) Object.Destroy(masked); else Object.DestroyImmediate(masked); }
            masked = null; HiddenTriangles = 0;
        }
        public void Wear(DressPreset preset)
        {
            Clear();
            if (body.IsOriginal || !body.CanFit) return;
            Vector3[] vertices = body.BodyVertices;
            BoneWeight[] weights = body.BodyMesh.boneWeights;
            var covered = new bool[vertices.Length];
            var arm = new bool[body.Bones.Length];
            for (int i = 0; i < arm.Length; i++) { string name = body.Bones[i].name; arm[i] = name.Contains("Arm") || name.Contains("Hand") || name.Contains("Shoulder"); }
            for (int i = 0; i < covered.Length; i++) {
                BoneWeight w = weights[i];
                float armWeight = (arm[w.boneIndex0] ? w.weight0 : 0) + (arm[w.boneIndex1] ? w.weight1 : 0) + (arm[w.boneIndex2] ? w.weight2 : 0) + (arm[w.boneIndex3] ? w.weight3 : 0);
                covered[i] = armWeight < .02f && Covered(vertices[i], preset);
            }
            masked = Object.Instantiate(body.BodyMesh); masked.name = "Body with covered interior triangles omitted";
            masked.hideFlags = HideFlags.HideAndDontSave;
            for (int sub = 0; sub < body.BodyMesh.subMeshCount; sub++) {
                int[] source = body.BodyMesh.GetTriangles(sub); var kept = new List<int>(source.Length);
                for (int i = 0; i < source.Length; i += 3) {
                    int a = source[i], b = source[i + 1], c = source[i + 2];
                    if (covered[a] && covered[b] && covered[c] && Covered((vertices[a] + vertices[b] + vertices[c]) / 3, preset)) { HiddenTriangles++; continue; }
                    kept.Add(a); kept.Add(b); kept.Add(c);
                }
                masked.SetTriangles(kept, sub, false);
            }
            masked.bounds = body.BodyMesh.bounds; Apply();
        }
        static bool Covered(Vector3 p, DressPreset preset)
        {
            float z = p.z - BodyFitProfile.CentreZ(p.y), angle = Mathf.Atan2(z, p.x);
            if (p.y > BodyFitProfile.HemY(preset) + .065f && p.y < BodyFitProfile.WaistY - .035f) {
                float t = Mathf.InverseLerp(BodyFitProfile.WaistY, BodyFitProfile.HemY(preset), p.y);
                Vector3 skirt = BodyFitProfile.Skirt(preset, t, angle);
                float radius = new Vector2(skirt.x, skirt.z - BodyFitProfile.CentreZ(p.y)).magnitude;
                return new Vector2(p.x, z).magnitude < radius - .015f;
            }
            if (preset == DressPreset.AtelierMidi||preset==DressPreset.GucciPoloStudy) return false;
            if (preset == DressPreset.IvoryShirtSet)
                return p.y > 1.16f && p.y < 1.30f && (p.y < 1.28f || Mathf.Abs(Mathf.Cos(angle)) < .70f);
            return p.y > BodyFitProfile.WaistY + .015f && p.y < SampleSkirtGeometry.BodiceTop(preset, angle) - .035f;
        }
    }
}
