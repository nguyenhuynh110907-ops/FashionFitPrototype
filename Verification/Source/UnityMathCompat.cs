// Test-only math substitutes: Unity's native reference DLLs cannot execute without the engine.
// This file is outside Assets and is NOT compiled into the Unity application.
using System;
namespace UnityEngine
{
    public struct Vector2
    {
        public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public static Vector2 one=>new Vector2(1,1);
        public static Vector2 Lerp(Vector2 a,Vector2 b,float t)=>new Vector2(Mathf.Lerp(a.x,b.x,t),Mathf.Lerp(a.y,b.y,t));
    }
    public struct Vector3
    {
        public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}
        public static Vector3 zero=>new Vector3();public static Vector3 one=>new Vector3(1,1,1);public static Vector3 up=>new Vector3(0,1,0);public static Vector3 down=>new Vector3(0,-1,0);public static Vector3 forward=>new Vector3(0,0,1);
        public float sqrMagnitude=>x*x+y*y+z*z;public float magnitude=>MathF.Sqrt(sqrMagnitude);public Vector3 normalized=>magnitude>1e-8f?this/magnitude:zero;
        public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
        public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.x-b.x,a.y-b.y,a.z-b.z);
        public static Vector3 operator *(Vector3 a,float k)=>new Vector3(a.x*k,a.y*k,a.z*k);public static Vector3 operator /(Vector3 a,float k)=>new Vector3(a.x/k,a.y/k,a.z/k);
        public static float Dot(Vector3 a,Vector3 b)=>a.x*b.x+a.y*b.y+a.z*b.z;public static float Distance(Vector3 a,Vector3 b)=>(a-b).magnitude;
        public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*Mathf.Clamp01(t);
        public static Vector3 Min(Vector3 a,Vector3 b)=>new Vector3(Mathf.Min(a.x,b.x),Mathf.Min(a.y,b.y),Mathf.Min(a.z,b.z));
        public static Vector3 Max(Vector3 a,Vector3 b)=>new Vector3(Mathf.Max(a.x,b.x),Mathf.Max(a.y,b.y),Mathf.Max(a.z,b.z));
    }
    public static class Mathf
    {
        public const float PI=MathF.PI;
        public static float Floor(float v)=>MathF.Floor(v);public static int FloorToInt(float v)=>(int)MathF.Floor(v);public static float Sqrt(float v)=>MathF.Sqrt(v);
        public static int Clamp(int v,int min,int max)=>Math.Min(max,Math.Max(min,v));
        public static int Abs(int v)=>Math.Abs(v);public static int Min(int a,int b)=>Math.Min(a,b);public static int Max(int a,int b)=>Math.Max(a,b);
        public static float Sin(float x)=>MathF.Sin(x);public static float Cos(float x)=>MathF.Cos(x);public static float Pow(float x,float y)=>MathF.Pow(x,y);public static float Abs(float x)=>MathF.Abs(x);
        public static float Min(float a,float b)=>MathF.Min(a,b);public static float Max(float a,float b)=>MathF.Max(a,b);
        public static float Clamp(float x,float min,float max)=>Math.Clamp(x,min,max);public static float Clamp01(float x)=>Math.Clamp(x,0,1);public static float Lerp(float a,float b,float t)=>a+(b-a)*Clamp01(t);
        public static float InverseLerp(float a,float b,float v)=>a==b?0:Clamp01((v-a)/(b-a));
        public static float SmoothStep(float a,float b,float t){t=Clamp01(t);t=t*t*(3-2*t);return b*t+a*(1-t);}
    }
}
