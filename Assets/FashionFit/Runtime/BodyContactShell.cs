using UnityEngine;

namespace FashionFit
{
    // A closed radial torso envelope measured from this technical avatar.
    // Used in addition to limb proxies; not a collider for arbitrary avatars.
    public sealed class BodyContactShell
    {
        Matrix4x4 toRest, toCloth;
        float minY, maxY, margin,neckMargin;
        public void Update(Matrix4x4 restToCloth, float lower, float upper, float clearance,float neckClearance=-1)
        {
            toCloth = restToCloth; toRest = restToCloth.inverse;
            minY = lower; maxY = upper; margin = clearance;neckMargin=neckClearance;
        }
        float Margin(float y){return neckMargin<0?margin:Mathf.Lerp(margin,neckMargin,Mathf.InverseLerp(1.375f,1.435f,y));}
        public float Clearance(Vector3 point)
        {
            Vector3 p = toRest.MultiplyPoint3x4(point);
            if (p.y < minY || p.y > maxY) return float.PositiveInfinity;
            float z = p.z - BodyFitProfile.CentreZ(p.y);
            float angle = Mathf.Atan2(z, p.x);
            return Mathf.Sqrt(p.x * p.x + z * z) - BodyFitProfile.Radius(p.y, angle) - Margin(p.y);
        }
        public void Project(ref Vector3 point)
        {
            Vector3 p = toRest.MultiplyPoint3x4(point);
            if (p.y < minY || p.y > maxY) return;
            float z = p.z - BodyFitProfile.CentreZ(p.y);
            float length = Mathf.Sqrt(p.x * p.x + z * z);
            float angle = length > 1e-7f ? Mathf.Atan2(z, p.x) : Mathf.PI * .5f;
            float radius = BodyFitProfile.Radius(p.y, angle) + Margin(p.y);
            if (length >= radius) return;
            p.x = Mathf.Cos(angle) * radius;
            p.z = BodyFitProfile.CentreZ(p.y) + Mathf.Sin(angle) * radius;
            point = toCloth.MultiplyPoint3x4(p);
        }
    }
}
