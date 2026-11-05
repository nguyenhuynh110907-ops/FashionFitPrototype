using System;
using System.Linq;
using FashionFit;
using UnityEngine;

static class GarmentFitChecks
{
    static int checks;
    static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;}
    static bool Finite(Vector3 v)=>!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsNaN(v.z)&&!float.IsInfinity(v.x)&&!float.IsInfinity(v.y)&&!float.IsInfinity(v.z);
    static float HemWidth(Vector3[] points)
    {
        var hem=points.Skip((SampleSkirtGeometry.Rings-1)*SampleSkirtGeometry.Segments);return hem.Max(p=>p.x)-hem.Min(p=>p.x);
    }
    static int Main()
    {
        float rose=0,column=0;
        for(int look=0;look<5;look++)
        {
            SampleSkirtGeometry.Build((DressPreset)look,out Vector3[] source,out Vector2[] uv,out int[] triangles);
            Check(source.Length==672&&source.All(Finite),"Cage geometry invalid");
            Check(source.Take(32).All(p=>Mathf.Abs(p.y-BodyFitProfile.WaistY)<1e-6f),"Waist is not at measured landmark");
            var backup=(Vector3[])source.Clone();
            var metre=GarmentFit.Fit(source,1,BodyFitProfile.WaistY,BodyFitProfile.HipY,0,BodyFitProfile.CentreZ(BodyFitProfile.WaistY),true,.012f,0,0,0,source);
            var centimetre=GarmentFit.Fit(source.Select(p=>p*100).ToArray(),.01f,BodyFitProfile.WaistY,BodyFitProfile.HipY,0,BodyFitProfile.CentreZ(BodyFitProfile.WaistY),true,.012f,0,0,0,source);
            Check(metre.All(Finite),"Adapted garment is nonfinite");
            Check(metre.Zip(centimetre,(a,b)=>Vector3.Distance(a,b)).Max()<1e-5f,"Metre/cm fit differ");
            Check(source.SequenceEqual(backup),"Source garment mutated");
            Check(metre.Take(32).All(p=>Mathf.Abs(p.y-BodyFitProfile.WaistY)<1e-5f),"Adaptation moved waistband");
            float centreZ=(metre.Take(32).Max(p=>p.z)+metre.Take(32).Min(p=>p.z))*.5f;
            Check(Mathf.Abs(centreZ-BodyFitProfile.CentreZ(BodyFitProfile.WaistY))<1e-5f,"Waist front/back centre mismatch");
            if(look==1)column=HemWidth(source);if(look==2)rose=HemWidth(source);
        }
        Check(rose>column*1.4f,"Fitted/flared silhouettes indistinguishable");
        Console.WriteLine("GARMENT_FIT_CHECKS_OK checks="+checks+" roseHem="+rose+" columnHem="+column);return 0;
    }
}
