using System;
using System.IO;
using System.Linq;
using System.Globalization;
using UnityEngine;
using FashionFit;

// Runs production cage geometry, gait curves and solver with math-only substitutes.
// It measures proxy/point contact, not Unity rendering or triangle-level CCD.
static class RunwayTrialChecks
{
    static int checks;
    static void Require(bool value,string name){if(!value)throw new Exception(name);checks++;}
    static bool Finite(Vector3 p)=>!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&!float.IsNaN(p.z)&&!float.IsInfinity(p.x)&&!float.IsInfinity(p.y)&&!float.IsInfinity(p.z);
    static Vector3 Knee(Vector3 hip,Vector3 foot)
    {
        const float a=.400f,b=.410f;
        Vector3 d=foot-hip;float length=Mathf.Clamp(d.magnitude,.02f,a+b-.0001f);d=d.normalized;
        float along=(a*a+length*length-b*b)/(2*length);
        Vector3 bend=(Vector3.forward-d*Vector3.Dot(Vector3.forward,d)).normalized;
        return hip+d*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
    }
    static ClothCapsule[] Legs(float phase)
    {
        var caps=new ClothCapsule[8];
        for(int leg=0;leg<2;leg++)
        {
            float sign=leg==0?1:-1;
            RunwayGait.Sample(phase+leg*.5f,out float forward,out float lift,out float rocker);
            Vector3 hip=new Vector3(sign*.102f,.880f-.018f+RunwayGait.PelvisLift(phase),.0185f);
            Vector3 foot=new Vector3(sign*.178f,.074f+lift,.032f+forward);
            Vector3 knee=Knee(hip,foot),upper=Vector3.Lerp(hip,knee,.48f),lower=Vector3.Lerp(knee,foot,.52f);
            int n=leg*4;caps[n]=new ClothCapsule(hip,upper,.112f);caps[n+1]=new ClothCapsule(upper,knee,.095f);
            caps[n+2]=new ClothCapsule(knee,lower,.078f);caps[n+3]=new ClothCapsule(lower,foot,.060f);
        }
        return caps;
    }
    static void Gait()
    {
        float maxLift=0,maxBob=0;
        for(int i=0;i<=400;i++)
        {
            float phase=i/400f;RunwayGait.Sample(phase,out float forward,out float lift,out float rocker);
            Require(lift>=-.00001f,"Foot below support plane");
            if(phase<.62f)Require(lift==0,"Support foot lifts from floor");
            maxLift=Mathf.Max(maxLift,lift);maxBob=Mathf.Max(maxBob,RunwayGait.PelvisLift(phase));
        }
        Require(maxLift>.054f&&maxLift<.056f,"Swing clearance");Require(maxBob<=.00601f,"Pelvis bob");
        RunwayGait.Sample(.999999f,out float a,out float b,out float c);
        RunwayGait.Sample(0,out float d,out float e,out float f);
        Require(Mathf.Abs(a-d)<.0001f&&Mathf.Abs(b-e)<.0001f&&Mathf.Abs(c-f)<.001f,"Cycle seam");
    }
    static float Sweep(bool capsule)
    {
        const int segments=8,rings=4;
        var p=new Vector3[segments*rings];
        for(int i=0;i<p.Length;i++)p[i]=new Vector3(.30f,1+i*.0001f,0);
        var solver=new FittingClothSolver(p,segments,rings,0,false,.01f,4,1,.02f);
        var caps=capsule?new[]{new ClothCapsule(new Vector3(0,.7f,0),new Vector3(0,1.4f,0),.12f)}:new ClothCapsule[0];
        var ell=capsule?new ClothEllipsoid[0]:new[]{new ClothEllipsoid(new Vector3(0,1,0),new Vector3(.12f,.3f,.12f))};
        solver.BeginGrab(p[16]);solver.MoveGrab(new Vector3(-.30f,1,0));
        for(int i=0;i<4;i++)solver.Step(1f/120f,p,caps,ell);
        Require(solver.Positions.All(Finite),"Sweep finite");
        float minX=solver.Positions.Min(v=>v.x);Require(minX>.10f,"Grab tunneled through "+(capsule?"capsule":"ellipsoid"));
        return minX;
    }
    static int Main(string[] args)
    {
        CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
        Gait();float sphereSweep=Sweep(false),capsuleSweep=Sweep(true);
        var watch=System.Diagnostics.Stopwatch.StartNew();
        var report=new System.Text.StringBuilder();float globalStretch=1;int globalPenetrations=0;
        report.Append("{\n  \"scope\": \"Production 672-point cages, authored gait and solver with test-only math. Point/proxy contacts only; no rendered triangle or headset guarantee.\",\n  \"presets\": [\n");
        for(int preset=0;preset<5;preset++)
        {
            SampleSkirtGeometry.Build((DressPreset)preset,out Vector3[] rest,out Vector2[] uv,out int[] triangles);
            var oldCaps=Legs(0);float maxStretch=1;int penetrations=0;
            var ell=new[]{new ClothEllipsoid(new Vector3(0,.892f,.025f),new Vector3(.210f,.160f,.160f)),new ClothEllipsoid(new Vector3(0,.977f,.010f),new Vector3(.195f,.135f,.155f))};
            FittingClothSolver.FitRestPose(rest,oldCaps,ell,.016f);var anchors=(Vector3[])rest.Clone();
            var solver=new FittingClothSolver(rest,SampleSkirtGeometry.Segments,SampleSkirtGeometry.Rings,2,preset==1,.014f,preset==1?7:5);
            solver.Reset(anchors,oldCaps,ell);
            for(int frame=1;frame<=144;frame++)
            {
                float phase=frame/72f;var caps=Legs(phase);
                for(int n=0;n<caps.Length;n++)caps[n]=new ClothCapsule(caps[n].A,caps[n].B,oldCaps[n].A,oldCaps[n].B,caps[n].Radius);
                float bob=-.018f+RunwayGait.PelvisLift(phase);
                for(int i=0;i<anchors.Length;i++)anchors[i]=rest[i]+Vector3.up*bob;
                solver.Step(1f/60f,anchors,caps,ell,0,1);
                Require(solver.Positions.All(Finite),"Walking finite "+preset);
                penetrations+=solver.LastPenetratingPoints;
                maxStretch=Mathf.Max(maxStretch,solver.MaxStretch());oldCaps=caps;
            }
            globalStretch=Mathf.Max(globalStretch,maxStretch);globalPenetrations+=penetrations;
            report.Append(FormattableString.Invariant($"    {{\"look\": {preset+1}, \"physicsPoints\": {rest.Length}, \"maxStretch\": {maxStretch:R}, \"pointProxyViolations\": {penetrations}}}"));
            report.Append(preset<4?",\n":"\n");
            Console.WriteLine("LOOK "+(preset+1)+" stretch="+maxStretch+" proxyViolations="+penetrations);
        }
        watch.Stop();
        report.Append(FormattableString.Invariant($"  ],\n  \"checks\": {checks},\n  \"clothSubsteps\": 720,\n  \"gaitCyclesPerLook\": 2,\n  \"maxStretch\": {globalStretch:R},\n  \"pointProxyViolations\": {globalPenetrations},\n  \"ellipsoidGrabMinX\": {sphereSweep:R},\n  \"capsuleGrabMinX\": {capsuleSweep:R},\n  \"cpuMilliseconds\": {watch.ElapsedMilliseconds}\n}}\n"));
        if(args.Length>0)File.WriteAllText(Path.Combine(args[0],"Verification/Runway_Trial_Checks.json"),report.ToString());
        Console.WriteLine(report.ToString());
        Require(globalStretch<1.65f,"Walking stretch regression");
        Require(globalPenetrations==0,"Point/proxy contact violations (see report)");return 0;
    }
}
