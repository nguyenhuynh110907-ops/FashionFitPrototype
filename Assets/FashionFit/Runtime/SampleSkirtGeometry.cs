using System.Collections.Generic;
using UnityEngine;

namespace FashionFit
{
    public enum DressPreset
    {
        AtelierMidi = 0,
        EmeraldColumn = 1,
        RoseCouture = 2,
        SapphireCocktail = 3,
        IvoryShirtSet = 4,
        GucciPoloStudy = 5
    }

    public static class SampleSkirtGeometry
    {
        // The simulation cage is intentionally sparse. Render detail is generated
        // separately, so collision iterations do not scale with display density.
        public const int Segments = 32, Rings = 21;

        public static void Build(out Vector3[] vertices, out Vector2[] uv, out int[] indices)
        {
            Build(DressPreset.AtelierMidi, out vertices, out uv, out indices);
        }

        public static void Build(DressPreset preset, out Vector3[] vertices, out Vector2[] uv, out int[] indices)
        {
            vertices = new Vector3[Segments * Rings];
            uv = new Vector2[vertices.Length];
            var triangles = new List<int>((Rings - 1) * Segments * 6);
            for (int r = 0; r < Rings; r++)
            {
                float t = r / (float)(Rings - 1);
                for (int s = 0; s < Segments; s++)
                {
                    int i = r * Segments + s;
                    float angle = s * Mathf.PI * 2 / Segments;
                    vertices[i] = BodyFitProfile.Skirt(preset,t,angle);
                    uv[i] = new Vector2(s / (float)Segments, t);
                    if (r < Rings - 1)
                    {
                        int j = r * Segments + (s + 1) % Segments;
                        triangles.Add(i); triangles.Add(j); triangles.Add(i + Segments);
                        triangles.Add(j); triangles.Add(j + Segments); triangles.Add(i + Segments);
                    }
                }
            }
            indices = triangles.ToArray();
        }

        #if !FASHIONFIT_NUMERICAL
        public static float BodiceTop(DressPreset preset,float angle)
        {
            float front=Mathf.Max(0,Mathf.Sin(angle));
            float top=preset==DressPreset.RoseCouture?1.30f+.035f*front-.020f*Mathf.Pow(front,10):preset==DressPreset.EmeraldColumn?1.31f+.015f*front-.025f*Mathf.Pow(front,10):1.30f+.020f*front+.012f*Mathf.Cos(angle);
            // The torso envelope excludes arms. A high closed side edge cuts
            // into the real axilla even at rest; shape a lower underarm curve.
            return top-.075f*Mathf.Pow(Mathf.Abs(Mathf.Cos(angle)),4);
        }
        public static Mesh BuildBodice(DressPreset preset)
        {
            if (preset == DressPreset.AtelierMidi || preset == DressPreset.IvoryShirtSet || preset == DressPreset.GucciPoloStudy) return null;
            const int rings = 16,segments=64;
            var vertices = new Vector3[segments * rings];
            var uv = new Vector2[vertices.Length];
            var triangles = new List<int>((rings - 1) * segments * 6);
            for (int r = 0; r < rings; r++)
            {
                float t = r / (float)(rings - 1);
                for (int s = 0; s < segments; s++)
                {
                    int i = r * segments + s;
                    float angle = s * Mathf.PI * 2 / segments;
                    float front = Mathf.Max(0, Mathf.Sin(angle));
                    float top=BodiceTop(preset,angle);
                    float y=Mathf.Lerp(BodyFitProfile.WaistY-.008f,top,t);
                    // Three fitted evening bodices: sweetheart, shallow plunge,
                    // and a slightly asymmetric upper edge. Arms remain outside.
                    float ease=Mathf.Lerp(.012f,preset==DressPreset.RoseCouture?.018f:.014f,Mathf.SmoothStep(0,1,t));
                    vertices[i]=BodyFitProfile.Surface(y,angle,ease);
                    uv[i] = new Vector2(s / (float)segments, 1 - t);
                    if (r < rings - 1)
                    {
                        int j = r * segments + (s + 1) % segments;
                        // Rings ascend on the bodice (opposite to skirt rings).
                        triangles.Add(i); triangles.Add(i + segments); triangles.Add(j);
                        triangles.Add(j); triangles.Add(i + segments); triangles.Add(j + segments);
                    }
                }
            }
            var mesh = new Mesh { name = preset + " bodice", vertices = vertices, uv = uv, triangles = triangles.ToArray() };
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            return mesh;
        }
        #endif
    }
}
