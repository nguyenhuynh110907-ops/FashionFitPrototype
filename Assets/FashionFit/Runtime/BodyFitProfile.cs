using UnityEngine;

namespace FashionFit
{
    // Technical-avatar measurements baked once from the rest mesh. No per-frame
    // mesh scans and no inference of measurements from reference photographs.
    public static class BodyFitProfile
    {
        public const float WaistY=1.12f,HipY=.84f,BustY=1.26f;

        static void Rows(float y,out int a,out int b,out float blend)
        {
            float row=Mathf.Clamp((y-TechnicalBodyFitData.StartY)/TechnicalBodyFitData.StepY,0,TechnicalBodyFitData.CentreZ.Length-1);
            a=Mathf.FloorToInt(row);b=Mathf.Min(a+1,TechnicalBodyFitData.CentreZ.Length-1);blend=row-a;
        }
        public static float CentreZ(float y)
        {
            Rows(y,out int a,out int b,out float blend);return Mathf.Lerp(TechnicalBodyFitData.CentreZ[a],TechnicalBodyFitData.CentreZ[b],blend);
        }
        public static float Radius(float y,float angle)
        {
            Rows(y,out int a,out int b,out float blend);
            float column=angle/(Mathf.PI*2)*TechnicalBodyFitData.Directions;
            // Wrap the integer cell, not the float: a tiny negative angle can
            // otherwise round to column 64 and read past the last body row.
            int cell=Mathf.FloorToInt(column),width=TechnicalBodyFitData.Directions;
            float t=column-cell;int i=((cell%width)+width)%width,j=(i+1)%width;
            return Mathf.Lerp(Mathf.Lerp(TechnicalBodyFitData.Radius[a*width+i],TechnicalBodyFitData.Radius[a*width+j],t),Mathf.Lerp(TechnicalBodyFitData.Radius[b*width+i],TechnicalBodyFitData.Radius[b*width+j],t),blend);
        }
        public static float Depth(float y){return Mathf.Max(Radius(y,Mathf.PI*.5f),Radius(y,Mathf.PI*1.5f));}
        public static Vector3 Surface(float y,float angle,float radialEase)
        {
            float r=Radius(y,angle)+radialEase;return new Vector3(r*Mathf.Cos(angle),y,CentreZ(y)+r*Mathf.Sin(angle));
        }
        public static float ContactMargin(DressPreset preset){return preset==DressPreset.EmeraldColumn||preset==DressPreset.IvoryShirtSet?.008f:.012f;}
        public static string Description(DressPreset preset)
        {
            return preset==DressPreset.GucciPoloStudy?"Tham chiếu ảnh Gucci 864361 · cổ gập, nẹp bốn nút · váy kín":preset==DressPreset.EmeraldColumn?"Ôm eo–hông · chân váy kín, không xẻ giữa":preset==DressPreset.RoseCouture?"Thân áo ôm · cổ tim · chân váy xòe rộng":preset==DressPreset.SapphireCocktail?"Ôm eo · cổ bất đối xứng nhẹ · xòe trên gối":preset==DressPreset.IvoryShirtSet?"Áo có độ dư · lỗ tay rõ · chân váy thẳng":"Eo vừa · rộng dần dưới hông · dáng midi";
        }
        public static float HemY(DressPreset preset)
        {
            return preset==DressPreset.GucciPoloStudy?.70f:preset==DressPreset.EmeraldColumn?.12f:preset==DressPreset.RoseCouture?.20f:preset==DressPreset.SapphireCocktail?.48f:preset==DressPreset.IvoryShirtSet?.49f:.43f;
        }
        static float EllipseRadius(float angle,float x,float z)
        {
            float c=Mathf.Cos(angle),s=Mathf.Sin(angle);return 1/Mathf.Sqrt(c*c/(x*x)+s*s/(z*z));
        }
        public static Vector3 Skirt(DressPreset preset,float t,float angle)
        {
            float hem=HemY(preset),y=Mathf.Lerp(WaistY,hem,t);
            float hipEase=preset==DressPreset.GucciPoloStudy?.024f:preset==DressPreset.RoseCouture?.025f:preset==DressPreset.SapphireCocktail?.020f:preset==DressPreset.AtelierMidi?.016f:.012f;
            float upper=Mathf.SmoothStep(0,1,Mathf.InverseLerp(WaistY,HipY,y));
            float r=Radius(Mathf.Max(y,HipY),angle)+Mathf.Lerp(preset==DressPreset.GucciPoloStudy?.022f:.012f,hipEase,upper);
            float centre=CentreZ(Mathf.Max(y,HipY));
            if(y<HipY)
            {
                float q=Mathf.InverseLerp(HipY,hem,y);
                float x=preset==DressPreset.GucciPoloStudy?.245f:preset==DressPreset.RoseCouture?.44f:preset==DressPreset.SapphireCocktail?.34f:preset==DressPreset.EmeraldColumn?.26f:preset==DressPreset.IvoryShirtSet?.235f:.31f;
                float z=preset==DressPreset.GucciPoloStudy?.19f:preset==DressPreset.RoseCouture?.36f:preset==DressPreset.SapphireCocktail?.28f:preset==DressPreset.EmeraldColumn?.205f:preset==DressPreset.IvoryShirtSet?.19f:.25f;
                r=Mathf.Lerp(r,EllipseRadius(angle,x,z),Mathf.Pow(q,preset==DressPreset.RoseCouture?1.18f:1));
                float amplitude=preset==DressPreset.RoseCouture?.007f:preset==DressPreset.SapphireCocktail?.004f:preset==DressPreset.AtelierMidi?.002f:0;
                r+=amplitude*Mathf.SmoothStep(0,1,q)*Mathf.Cos(angle*(preset==DressPreset.RoseCouture?8:6));
                centre=Mathf.Lerp(centre,.025f,q);
            }
            return new Vector3(r*Mathf.Cos(angle),y,centre+r*Mathf.Sin(angle));
        }
    }
}
