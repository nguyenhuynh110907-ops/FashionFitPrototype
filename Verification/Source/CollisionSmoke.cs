using System;
using System.Linq;
using UnityEngine;
using FashionFit;

// Fast regression for the collision/tether core. It deliberately starts a
// cylindrical cloth inside a torso ellipsoid and requires every dynamic point
// to be projected outside after a moving-collider step.
static class CollisionSmoke
{
    const int Segments = 16, Rings = 8, FixedRings = 1;

    static bool Finite(Vector3 p)
    {
        return !float.IsNaN(p.x) && !float.IsInfinity(p.x) &&
               !float.IsNaN(p.y) && !float.IsInfinity(p.y) &&
               !float.IsNaN(p.z) && !float.IsInfinity(p.z);
    }

    static float EllipsoidDistance(Vector3 p, Vector3 centre, Vector3 radius)
    {
        Vector3 d = p - centre;
        return new Vector3(d.x / radius.x, d.y / radius.y, d.z / radius.z).magnitude;
    }

    static int Main()
    {
        var rest = new Vector3[Segments * Rings];
        for (int r = 0; r < Rings; r++)
        for (int s = 0; s < Segments; s++)
        {
            float angle = s * Mathf.PI * 2 / Segments;
            rest[r * Segments + s] = new Vector3(.17f * Mathf.Cos(angle), 1.30f - r * .045f, .14f * Mathf.Sin(angle));
        }

        var solver = new FittingClothSolver(rest, Segments, Rings, FixedRings, false, .012f, 8, .28f, .08f);
        var anchors = (Vector3[])rest.Clone();
        var capsule = new[] { new ClothCapsule(new Vector3(0, .90f, 0), new Vector3(0, 1.38f, 0), .105f) };
        var centre = new Vector3(.025f, 1.13f, .025f);
        var radius = new Vector3(.180f, .400f, .150f);
        var ellipsoid = new[] { new ClothEllipsoid(centre, new Vector3(-.025f, 1.13f, 0), radius) };

        solver.Reset(anchors, capsule, ellipsoid);
        for (int i = 0; i < 8; i++) solver.Step(1f / 120f, anchors, capsule, ellipsoid);

        if (!solver.Positions.All(Finite)) throw new Exception("NaN/Infinity in cloth points");
        Vector3 expanded = radius + Vector3.one * .0115f;
        float minimum = float.PositiveInfinity;
        for (int i = FixedRings * Segments; i < solver.Positions.Length; i++)
            minimum = Mathf.Min(minimum, EllipsoidDistance(solver.Positions[i], centre, expanded));
        if (minimum < .9985f) throw new Exception("Dynamic point remained inside torso: " + minimum);
        float collisionStretch = solver.MaxStretch();
        if (collisionStretch > 1.65f) throw new Exception("Unbounded constraint stretch: " + collisionStretch);

        solver.Reset(anchors, capsule, ellipsoid);
        Vector3[] first = (Vector3[])solver.Positions.Clone();
        solver.Reset(anchors, capsule, ellipsoid);
        for (int i = 0; i < first.Length; i++)
            if (Vector3.Distance(first[i], solver.Positions[i]) > 1e-6f) throw new Exception("Reset is not deterministic");

        Console.WriteLine("COLLISION_SMOKE_OK points=" + rest.Length + " minOutside=" + minimum + " maxStretch=" + collisionStretch);
        return 0;
    }
}
