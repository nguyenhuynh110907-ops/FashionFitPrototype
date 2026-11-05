using System;
using System.Collections.Generic;
using UnityEngine;

namespace FashionFit
{
    // Fit the generated envelope before binding it to the body. Retaining a
    // small fraction of its original offset preserves triangle separation.
    public static class GarmentCloseFit
    {
        public static float NecklineHeight(RunwayAvatar avatar)
        {
            FittedGarmentGeometry.NeckProfile(avatar, out float height, out _, out _, out _);
            return height - .028f;
        }

        public static void Conform(List<Vector3> points, List<int> triangles, GarmentSurfaceTransfer.Surface body, RunwayAvatar avatar)
        {
            const float retainedOffset = .12f;
            float height = avatar.BodyMesh.bounds.max.y;
            float hem = height * .60f - .075f;
            float neckline = NecklineHeight(avatar);
            FittedGarmentGeometry.NeckProfile(avatar, out float oldNeckline, out _, out _, out _);
            oldNeckline -= .018f;
            float neckDrop = Mathf.Max(0, oldNeckline - neckline);
            float transition = Mathf.Max(.18f, 2 * neckDrop);
            var envelope = new TorsoEnvelope(avatar, hem, neckline);
            var normals = avatar.BodyMesh.normals;
            var faces = avatar.BodyMesh.triangles;
            var weights = avatar.BodyMesh.boneWeights;
            var armBones = new float[avatar.Bones.Length];
            for (int i = 0; i < armBones.Length; i++)
            {
                string name = avatar.Bones[i].name;
                armBones[i] = name.Contains("Arm") || name.Contains("Hand") ? 1 : name.Contains("Shoulder") ? .5f : 0;
            }
            float ArmWeight(BoneWeight w) => armBones[w.boneIndex0] * w.weight0 + armBones[w.boneIndex1] * w.weight1
                + armBones[w.boneIndex2] * w.weight2 + armBones[w.boneIndex3] * w.weight3;
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 p = points[i];
                var hit = body.Nearest(p);
                int face = hit.triangle * 3;
                float armWeight = ArmWeight(weights[faces[face]]) * hit.barycentric.x
                    + ArmWeight(weights[faces[face + 1]]) * hit.barycentric.y
                    + ArmWeight(weights[faces[face + 2]]) * hit.barycentric.z;
                float sleeveBlend = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(.10f, .90f, armWeight));
                float clearance = Mathf.Lerp(.028f, .010f, Mathf.SmoothStep(0, 1,
                    Mathf.InverseLerp(height * .60f - .015f, height * .60f + .07f, p.y)));
                Vector3 torso = p;
                if (sleeveBlend < 1)
                {
                    torso.y -= neckDrop * Mathf.InverseLerp(oldNeckline - transition, oldNeckline, p.y);
                    Vector2 current = new Vector2(torso.x, torso.z);
                    float radius = current.magnitude;
                    if (radius > .00001f)
                    {
                        Vector2 direction = current / radius;
                        float fitted = envelope.Radius(direction, torso.y, clearance);
                        float accepted = fitted + Mathf.Max(0, radius - fitted) * retainedOffset;
                        torso.x = direction.x * accepted; torso.z = direction.y * accepted;
                    }
                }
                Vector3 normal = (normals[faces[face]] * hit.barycentric.x
                    + normals[faces[face + 1]] * hit.barycentric.y
                    + normals[faces[face + 2]] * hit.barycentric.z).normalized;
                Vector3 sleeve = Vector3.Lerp(p, hit.point + normal * .012f, 1 - retainedOffset);
                points[i] = Vector3.Lerp(torso, sleeve, sleeveBlend);
            }
            // Rebalance interior triangles after the radial fit. Fixed opening
            // boundaries retain the collar, hem and cuff cuts.
            var neighbors = new HashSet<int>[points.Count];
            var edgeCount = new Dictionary<long, int>();
            for (int i = 0; i < points.Count; i++) neighbors[i] = new HashSet<int>();
            for (int i = 0; i < triangles.Count; i += 3)
                for (int k = 0; k < 3; k++)
                {
                    int a = triangles[i + k], b = triangles[i + (k + 1) % 3];
                    neighbors[a].Add(b); neighbors[b].Add(a);
                    long key = ((long)Mathf.Min(a, b) << 32) | (uint)Mathf.Max(a, b);
                    edgeCount[key] = edgeCount.TryGetValue(key, out int count) ? count + 1 : 1;
                }
            var boundary = new bool[points.Count];
            foreach (var edge in edgeCount)
                if (edge.Value == 1) { boundary[(int)(edge.Key >> 32)] = true; boundary[(int)edge.Key] = true; }
            var next = points.ToArray();
            for (int pass = 0; pass < 12; pass++)
            {
                for (int i = 0; i < points.Count; i++)
                {
                    if (boundary[i] || neighbors[i].Count == 0) { next[i] = points[i]; continue; }
                    Vector3 mean = Vector3.zero;
                    foreach (int j in neighbors[i]) mean += points[j];
                    next[i] = Vector3.Lerp(points[i], mean / neighbors[i].Count, .35f);
                }
                for (int i = 0; i < points.Count; i++) points[i] = next[i];
            }
            for (int i = 0; i < points.Count; i++) points[i] = body.ProjectOutside(points[i], .022f);
        }

        public sealed class TorsoEnvelope
        {
            const int Rows = 32, Angles = 48;
            readonly float[,] support = new float[Rows, Angles];
            readonly Vector2[] directions = new Vector2[Angles];
            readonly float bottom, top;
            public TorsoEnvelope(RunwayAvatar avatar, float bottom, float top)
            {
                this.bottom = bottom; this.top = top;
                for (int a = 0; a < Angles; a++)
                {
                    float angle = a * 2 * Mathf.PI / Angles;
                    directions[a] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                }
                var points = avatar.BodyVertices;
                var weights = avatar.BodyMesh.boneWeights;
                for (int row = 0; row < Rows; row++)
                {
                    float y = Mathf.Lerp(bottom, top, row / (Rows - 1f));
                    for (int a = 0; a < Angles; a++) support[row, a] = .035f;
                    for (int i = 0; i < points.Length; i++)
                    {
                        var p = points[i];
                        float band = Mathf.Lerp(.020f, .006f, Mathf.InverseLerp(top - .08f, top, y));
                        if (Mathf.Abs(p.y - y) > band || Mathf.Abs(p.x) > .30f) continue;
                        string name = avatar.Bones[weights[i].boneIndex0].name;
                        if (name.Contains("Arm") || name.Contains("Hand")) continue;
                        var point = new Vector2(p.x, p.z);
                        for (int a = 0; a < Angles; a++)
                            support[row, a] = Mathf.Max(support[row, a], Vector2.Dot(point, directions[a]));
                    }
                }
            }
            public float Radius(Vector2 direction, float height, float clearance)
            {
                float position = Mathf.Clamp01((height - bottom) / (top - bottom)) * (Rows - 1);
                int row = Mathf.Min(Rows - 2, Mathf.FloorToInt(position));
                float radius = float.PositiveInfinity;
                for (int a = 0; a < Angles; a++)
                {
                    float cosine = Vector2.Dot(direction, directions[a]);
                    if (cosine <= .0001f) continue;
                    float distance = Mathf.Lerp(support[row, a], support[row + 1, a], position - row) + clearance;
                    radius = Mathf.Min(radius, distance / cosine);
                }
                return radius;
            }
        }

        public static Vector4 LegSection(RunwayAvatar avatar, int side, float height, float ease)
        {
            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
            float minZ = float.PositiveInfinity, maxZ = float.NegativeInfinity;
            int count = 0;
            var points = avatar.BodyVertices;
            var weights = avatar.BodyMesh.boneWeights;
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 p = points[i];
                if (Mathf.Abs(p.y - height) > .020f || side * p.x < .012f || Mathf.Abs(p.x) > .285f) continue;
                string name = avatar.Bones[weights[i].boneIndex0].name;
                if (name.Contains("Arm") || name.Contains("Hand")) continue;
                minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                minZ = Mathf.Min(minZ, p.z); maxZ = Mathf.Max(maxZ, p.z);
                count++;
            }
            if (count < 4) return new Vector4(side * .112f, .012f, .065f + ease, .060f + ease);
            return new Vector4((minX + maxX) * .5f, (minZ + maxZ) * .5f,
                (maxX - minX) * .5f + ease, (maxZ - minZ) * .5f + ease);
        }
    }
}

