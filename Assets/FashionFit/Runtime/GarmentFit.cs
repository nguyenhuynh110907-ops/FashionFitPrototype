using System;
using System.IO;
using UnityEngine;
namespace FashionFit
{
    // Limited, auditable first fit for separate skirts. No inferred physical sizing.
    public static class GarmentFit
    {
        public const float BodyWaistY=BodyFitProfile.WaistY,BodyHipY=BodyFitProfile.HipY;
        public static Vector3[] Fit(Vector3[] source,float units,float waist,float hip,float cx,float cz,bool adapt,float ease,float ox,float oy,float oz,Vector3[] body)
        {
            if(source==null||source.Length<3||units<=0||units>1||waist-hip<.04f)throw new InvalidDataException("Kiểm tra đơn vị và mốc eo cao hơn hông ít nhất 4 cm.");
            float minY=float.PositiveInfinity,maxY=float.NegativeInfinity,maxX=0,maxZ=0;
            foreach(var p in source){minY=Mathf.Min(minY,p.y*units);maxY=Mathf.Max(maxY,p.y*units);maxX=Mathf.Max(maxX,Mathf.Abs(p.x*units-cx));maxZ=Mathf.Max(maxZ,Mathf.Abs(p.z*units-cz));}
            if(maxY-minY<.05f||maxY-minY>1.7f||maxX>1.5f||maxZ>1.5f)throw new InvalidDataException("Kích thước không phù hợp với váy. Chọn lại m / cm / mm.");
            if(Mathf.Abs(maxY-waist)>.02f||hip<=minY||hip>=waist)throw new InvalidDataException("Mốc eo cần ở mép trên của váy; mốc hông nằm trong chiều dài váy.");
            Vector2 rw=Vector2.one,rh=Vector2.one;
            float sourceWaistCentre=cz,sourceHipCentre=cz;
            if(adapt){
                sourceWaistCentre=SectionCentreZ(source,units,waist,.035f);sourceHipCentre=SectionCentreZ(source,units,hip,.04f);
                Vector2 sw=Profile(source,units,waist,cx,sourceWaistCentre,.035f),sh=Profile(source,units,hip,cx,sourceHipCentre,.04f);
                Vector2 bw=new Vector2(BodyFitProfile.Radius(BodyWaistY,0),BodyFitProfile.Depth(BodyWaistY)),bh=new Vector2(BodyFitProfile.Radius(BodyHipY,0),BodyFitProfile.Depth(BodyHipY));
                rw=new Vector2((bw.x+ease)/sw.x,(bw.y+ease)/sw.y);rh=new Vector2((bh.x+ease)/sh.x,(bh.y+ease)/sh.y);
                if(rw.x<.65f||rw.x>1.5f||rw.y<.65f||rw.y>1.5f||rh.x<.65f||rh.x>1.5f||rh.y<.65f||rh.y>1.5f)throw new InvalidDataException("Cần sửa phom lớn hơn giới hạn 0,65–1,5. Dừng để kiểm tra, không tự kéo méo thiết kế.");}
            var result=new Vector3[source.Length];float shift=BodyWaistY-waist;
            for(int i=0;i<result.Length;i++){
                Vector3 p=source[i]*units;float t=Mathf.SmoothStep(0,1,Mathf.InverseLerp(waist,hip,p.y));Vector2 r=Vector2.Lerp(rw,rh,t);
                // Preserve vertical dimensions in size-evaluation mode. Adapt mode maps the hip landmark.
                float y=adapt?(p.y>=hip?Mathf.Lerp(BodyHipY,BodyWaistY,(p.y-hip)/(waist-hip)):BodyHipY+p.y-hip):p.y+shift;
                float centreZ=adapt?BodyFitProfile.CentreZ(Mathf.Max(y,BodyHipY)):BodyFitProfile.CentreZ(BodyWaistY);
                float sourceCentre=adapt?Mathf.Lerp(sourceWaistCentre,sourceHipCentre,t):cz;
                result[i]=new Vector3((p.x-cx)*r.x+ox,y+oy,(p.z-sourceCentre)*r.y+centreZ+oz);
            }
            return result;
        }
        static float SectionCentreZ(Vector3[] vertices,float units,float y,float band)
        {
            float min=float.PositiveInfinity,max=float.NegativeInfinity;int count=0;
            foreach(Vector3 vertex in vertices){Vector3 p=vertex*units;if(Mathf.Abs(p.y-y)>band+.00001f)continue;min=Mathf.Min(min,p.z);max=Mathf.Max(max,p.z);count++;}
            if(count<6)throw new InvalidDataException("Thiếu tiết diện để xác định tâm trước/sau của trang phục.");
            return (min+max)*.5f;
        }
        static Vector2 Profile(Vector3[] vertices,float unit,float y,float cx,float cz,float band,float maxRadius=10)
        {
            float x=0,z=0;int count=0;foreach(var q in vertices){Vector3 p=q*unit;if(Mathf.Abs(p.y-y)>band+.00001f||Mathf.Abs(p.x-cx)>maxRadius+.00001f)continue;x=Mathf.Max(x,Mathf.Abs(p.x-cx));z=Mathf.Max(z,Mathf.Abs(p.z-cz));count++;}
            if(count<6||x<.01f||z<.01f)throw new InvalidDataException("Thiếu tiết diện quanh mốc eo/hông. Điều chỉnh mốc theo lưới thực.");return new Vector2(x,z);
        }
    }
}
