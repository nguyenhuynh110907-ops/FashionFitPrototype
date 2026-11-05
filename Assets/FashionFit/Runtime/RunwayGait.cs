using UnityEngine;

namespace FashionFit
{
    // Authored runway trial, not extracted motion capture. Each leg has a longer
    // support phase and a shorter swing, with a smooth toe-clearance trajectory.
    public static class RunwayGait
    {
        public static void Sample(float phase,out float forward,out float lift,out float rocker)
        {
            float p=phase-Mathf.Floor(phase);
            const float support=.62f;
            if(p<support)
            {
                float t=p/support;
                forward=Mathf.Lerp(.145f,-.145f,Mathf.SmoothStep(0,1,t));
                lift=0;
                rocker=Mathf.Lerp(-5,8,Mathf.SmoothStep(0,1,t));
            }
            else
            {
                float t=(p-support)/(1-support);
                forward=Mathf.Lerp(-.145f,.145f,Mathf.SmoothStep(0,1,t));
                float arc=Mathf.Sin(t*Mathf.PI);lift=.055f*arc*arc;
                rocker=Mathf.Lerp(8,-5,Mathf.SmoothStep(0,1,t));
            }
        }

        public static float PelvisLift(float phase)
        {
            return .003f*(1-Mathf.Cos(phase*Mathf.PI*4));
        }
    }
}
