// Executes the actual fitting/cloth/three-dress numerical core with test-only math substitutes.
// This does not run Unity rendering, XR devices, shaders, file dialogs or remote APIs.
using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;
using FashionFit;

class NumericalChecks
{
    static int checks;
    static void Check(bool ok,string name){if(!ok)throw new Exception("FAIL: "+name);checks++;}
    static void Reject(Action a,string name){try{a();}catch(InvalidDataException){checks++;return;}throw new Exception("Expected rejection: "+name);}
    static Vector3 ReadV(BinaryReader r)=>new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
    static bool Finite(Vector3 p)=>!float.IsNaN(p.x)&&!float.IsInfinity(p.x)&&!float.IsNaN(p.y)&&!float.IsInfinity(p.y)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.z);
    static Vector3 Rx(Vector3 p,float angle){float c=MathF.Cos(angle),s=MathF.Sin(angle);return new Vector3(p.x,c*p.y-s*p.z,s*p.y+c*p.z);}

    static ClothCapsule[] Capsules(Vector3[] joints,float time,out float bob)
    {
        float phase=time*MathF.PI*2/1.2f,s=MathF.Sin(phase);bob=.012f*(1-MathF.Cos(phase*2));var up=Vector3.up*bob;
        Vector3 lh=joints[5]+up,rh=joints[8]+up;
        float l=20*s*MathF.PI/180,r=-l,lkAngle=-30*MathF.Max(0,-s)*MathF.PI/180,rkAngle=-30*MathF.Max(0,s)*MathF.PI/180;
        Vector3 lk=lh+Rx(joints[6]-joints[5],l),rk=rh+Rx(joints[9]-joints[8],r);
        Vector3 la=lk+Rx(joints[7]-joints[6],l+lkAngle),ra=rk+Rx(joints[10]-joints[9],r+rkAngle);
        Vector3 lu=Vector3.Lerp(lh,lk,.48f),ru=Vector3.Lerp(rh,rk,.48f),ll=Vector3.Lerp(lk,la,.52f),rl=Vector3.Lerp(rk,ra,.52f);
        return new[]{new ClothCapsule(lh,lu,.112f),new ClothCapsule(lu,lk,.095f),new ClothCapsule(lk,ll,.078f),new ClothCapsule(ll,la,.060f),new ClothCapsule(rh,ru,.112f),new ClothCapsule(ru,rk,.095f),new ClothCapsule(rk,rl,.078f),new ClothCapsule(rl,ra,.060f)};
    }

    static ClothEllipsoid[] Ellipsoids(Vector3[] joints,float bob,DressPreset preset)
    {
        Vector3 radius=preset==DressPreset.EmeraldColumn?new Vector3(.195f,.145f,.145f):new Vector3(.210f,.160f,.160f);
        return new[]{new ClothEllipsoid(joints[0]+Vector3.up*bob,radius)};
    }

    static void Obj(string path,Vector3[] v,Vector2[] uv,int[] indices,float scale)
    {
        using(var w=new StreamWriter(path)){
            w.WriteLine("# Sample midi skirt, Y-up, +Z-front, separate garment. Unit "+(scale==1?"metres":"centimetres"));
            foreach(var p in v)w.WriteLine(FormattableString.Invariant($"v {-p.x*scale:R} {p.y*scale:R} {p.z*scale:R}"));
            foreach(var p in uv)w.WriteLine(FormattableString.Invariant($"vt {p.x:R} {p.y:R}"));
            for(int i=0;i<indices.Length;i+=3){int a=indices[i]+1,b=indices[i+2]+1,c=indices[i+1]+1;w.WriteLine($"f {a}/{a} {b}/{b} {c}/{c}");}
        }
    }

    static Vector3[] ReadRig(string path)
    {
        string text=File.ReadAllText(path);
        MatchCollection nodes=Regex.Matches(text,"\\\"name\\\"\\s*:\\s*\\\"[^\\\"]+\\\"\\s*,\\s*\\\"parent\\\"\\s*:\\s*(-?\\d+)\\s*,\\s*\\\"position\\\"\\s*:\\s*\\[\\s*([-+0-9.eE]+)\\s*,\\s*([-+0-9.eE]+)\\s*,\\s*([-+0-9.eE]+)",RegexOptions.Singleline);
        Check(nodes.Count>=11,"rig nodes parsed");var joints=new Vector3[nodes.Count];
        for(int i=0;i<joints.Length;i++){
            int parent=int.Parse(nodes[i].Groups[1].Value,CultureInfo.InvariantCulture);
            var v=new Vector3(float.Parse(nodes[i].Groups[2].Value,CultureInfo.InvariantCulture),float.Parse(nodes[i].Groups[3].Value,CultureInfo.InvariantCulture),float.Parse(nodes[i].Groups[4].Value,CultureInfo.InvariantCulture));
            joints[i]=v+(parent>=0?joints[parent]:Vector3.zero);
        }
        return joints;
    }

    static void ExercisePreset(DressPreset preset,Vector3[] joints,int substeps,ref float maxStretch,ref float minY)
    {
        float presetStretch=1;
        SampleSkirtGeometry.Build(preset,out Vector3[] rest,out Vector2[] uv,out int[] tri);
        int closedIndices=(SampleSkirtGeometry.Rings-1)*SampleSkirtGeometry.Segments*6;
        int slitIndices=((SampleSkirtGeometry.Rings-1)-(SampleSkirtGeometry.Rings/2))*6;
        int expectedIndices=preset==DressPreset.EmeraldColumn?closedIndices-slitIndices:closedIndices;
        Check(rest.Length==SampleSkirtGeometry.Segments*SampleSkirtGeometry.Rings&&tri.Length==expectedIndices,preset+" topology");
        Check(rest.All(Finite),preset+" geometry finite");
        if(preset==DressPreset.EmeraldColumn)Check(rest.Min(p=>p.y)<.13f,"column gown length");
        if(preset==DressPreset.RoseCouture)Check(rest.Max(p=>MathF.Abs(p.x))>.39f,"couture flare");
        var solver=new FittingClothSolver(rest,SampleSkirtGeometry.Segments,SampleSkirtGeometry.Rings,2,preset==DressPreset.EmeraldColumn,preset==DressPreset.EmeraldColumn?.007f:.014f,preset==DressPreset.EmeraldColumn?9:7);var anchors=(Vector3[])rest.Clone();var caps=Capsules(joints,0,out float bob);var ellipsoids=Ellipsoids(joints,bob,preset);
        solver.Reset(anchors,caps,ellipsoids);Check(solver.Positions.All(Finite),preset+" settling finite");

        for(int i=1;i<=substeps;i++){
            float time=i/120f;caps=Capsules(joints,time,out bob);for(int k=0;k<anchors.Length;k++)anchors[k]=rest[k]+Vector3.up*bob;
            solver.Step(1f/120f,anchors,caps,Ellipsoids(joints,bob,preset));
            if(i%60==0){Check(solver.Positions.All(Finite),preset+" walking finite");presetStretch=MathF.Max(presetStretch,solver.MaxStretch());maxStretch=MathF.Max(maxStretch,presetStretch);minY=MathF.Min(minY,solver.Positions.Min(p=>p.y));}
        }
        Console.WriteLine(FormattableString.Invariant($"{preset}: stretch={presetStretch:R}, minY={solver.Positions.Min(p=>p.y):R}"));

        int grabbed=(SampleSkirtGeometry.Rings-5)*SampleSkirtGeometry.Segments;Vector3 before=solver.Positions[grabbed],target=before+new Vector3(.08f,.06f,.04f);
        solver.BeginGrab(before);solver.MoveGrab(target);for(int i=0;i<12;i++)solver.Step(1f/120f,anchors,caps,ellipsoids);
        Check(Vector3.Distance(solver.Positions[grabbed],before)>.035f,preset+" VR grab moves cloth");Check(solver.Positions.All(Finite),preset+" VR grab finite");solver.EndGrab();
        for(int i=0;i<120;i++)solver.Step(1f/120f,anchors,caps,ellipsoids);Check(solver.Positions.All(Finite),preset+" post-release gravity finite");
        var resetEllipsoids=Ellipsoids(joints,bob,preset);
        solver.Reset(rest,Capsules(joints,0,out bob),resetEllipsoids);var reset1=(Vector3[])solver.Positions.Clone();solver.Reset(rest,Capsules(joints,0,out bob),resetEllipsoids);Check(reset1.SequenceEqual(solver.Positions),preset+" deterministic reset");
    }

    static int Main(string[] args)
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;string root=args[0];
        Vector3[] body;using(var r=new BinaryReader(File.OpenRead(Path.Combine(root,"Assets/Resources/Fitting/female-mesh.bytes")))){r.ReadInt32();int n=r.ReadInt32();body=new Vector3[n];for(int i=0;i<n;i++)body[i]=ReadV(r);}
        Vector3[] joints=ReadRig(Path.Combine(root,"Assets/Resources/Fitting/female-rig.json"));
        SampleSkirtGeometry.Build(out Vector3[] rest,out Vector2[] uv,out int[] tri);var backup=(Vector3[])rest.Clone();
        var original=GarmentFit.Fit(rest,1,BodyFitProfile.WaistY,BodyFitProfile.HipY,0,.02f,false,.015f,0,0,0,body);
        for(int i=1;i<rest.Length;i+=101)Check(MathF.Abs(Vector3.Distance(original[0],original[i])-Vector3.Distance(rest[0],rest[i]))<1e-6,"preserve source dimensions");
        var fit=GarmentFit.Fit(rest,1,BodyFitProfile.WaistY,BodyFitProfile.HipY,0,.02f,true,.015f,0,0,0,body);Check(fit.All(Finite),"fit finite");Check(rest.SequenceEqual(backup),"source preserved");
        var cm=rest.Select(p=>p*100).ToArray();var fitCM=GarmentFit.Fit(cm,.01f,BodyFitProfile.WaistY,BodyFitProfile.HipY,0,.02f,true,.015f,0,0,0,body);Check(fit.Zip(fitCM,(a,b)=>Vector3.Distance(a,b)).Max()<1e-5,"centimetre normalization");
        Reject(()=>GarmentFit.Fit(cm,1,104.5f,89,0,2,true,.015f,0,0,0,body),"wrong units rejected");Reject(()=>GarmentFit.Fit(rest,1,.8f,.89f,0,0,true,.015f,0,0,0,body),"inverted landmarks");
        Reject(()=>GarmentFit.Fit(rest.Select(p=>new Vector3(p.x*.1f,p.y,p.z*.1f)).ToArray(),1,BodyFitProfile.WaistY,BodyFitProfile.HipY,0,0,true,.015f,0,0,0,body),"extreme warp rejected");
        Obj(Path.Combine(root,"Samples/Sample_Midi_Skirt.obj"),rest,uv,tri,1);Obj(Path.Combine(root,"Samples/Sample_Midi_Skirt_cm.obj"),rest,uv,tri,100);
        float maxStretch=1,minY=10;var watch=System.Diagnostics.Stopwatch.StartNew();
        ExercisePreset(DressPreset.AtelierMidi,joints,720,ref maxStretch,ref minY);
        ExercisePreset(DressPreset.EmeraldColumn,joints,360,ref maxStretch,ref minY);
        ExercisePreset(DressPreset.RoseCouture,joints,360,ref maxStretch,ref minY);
        ExercisePreset(DressPreset.SapphireCocktail,joints,240,ref maxStretch,ref minY);
        ExercisePreset(DressPreset.IvoryShirtSet,joints,240,ref maxStretch,ref minY);
        watch.Stop();Check(maxStretch<1.45f,"bounded spring stretch across presets");Check(minY>=.0119f,"ground nonpenetration");
        string json=FormattableString.Invariant($"{{\n  \"checks\": {checks},\n  \"clothSubsteps\": 1920,\n  \"simulatedSeconds\": 16,\n  \"dressPresets\": 5,\n  \"vrGrabReleaseTests\": 5,\n  \"maxConstraintStretch\": {maxStretch},\n  \"minY\": {minY},\n  \"cpuMilliseconds\": {watch.ElapsedMilliseconds},\n  \"scope\": \"Actual five-look fitting/solver source with test-only math substitutes. Swept hip and leg proxies, VR cloth grab/release and post-release gravity; no Unity rendering or headset.\"\n}}");
        File.WriteAllText(Path.Combine(root,"Verification/Numerical_Checks.json"),json);Console.WriteLine(json);return 0;
    }
}
