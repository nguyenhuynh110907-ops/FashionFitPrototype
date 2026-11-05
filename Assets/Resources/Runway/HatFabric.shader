Shader "Runway/HatFabric"
{
    Properties {
        _MainTex("UV channel",2D)="white"{}
        _ClothNormal("Woven normal (+Y RGB)",2D)="bump"{}
        _ClothRoughness("Woven roughness",2D)="gray"{}
        _UseClothDetail("Use woven maps",Float)=0
        _Color("Fabric color",Color)=(.2,.3,.5,1)
        _Glossiness("Smoothness",Range(0,1))=.15
        _Metallic("Metallic",Range(0,1))=0
        _TextureKind("Felt / woven / leather / denim",Float)=0
    }
    SubShader {
        Tags {"RenderType"="Opaque"}
        Cull Off
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0
        sampler2D _ClothNormal,_ClothRoughness;half _UseClothDetail;
        fixed4 _Color;half _Glossiness,_Metallic,_TextureKind;
        struct Input {float3 worldPos;float2 uv_MainTex;};
        void surf(Input IN,inout SurfaceOutputStandard o){
            float3 p=mul(unity_WorldToObject,float4(IN.worldPos,1)).xyz;
            float grain=frac(sin(dot(floor(p*1600),float3(12.9898,78.233,45.164)))*43758.5453);
            float thread=sin((p.x+p.y*.25)*3100)*sin(p.z*3100);
            float worn=.5+.5*sin(p.x*71+p.y*103)*sin(p.z*83-p.y*49);
            float denim=step(2.5,_TextureKind),leather=step(1.5,_TextureKind)*(1-denim);
            float woven=step(.5,_TextureKind)*(1-leather)*(1-denim);
            float pixelGrain=max(length(ddx(p*1600)),length(ddy(p*1600)));
            grain=lerp(grain,.5,saturate(pixelGrain));
            thread*=1-saturate(max(length(ddx(p*3100)),length(ddy(p*3100))));
            float variation=1+(grain-.5)*(.06+.06*woven+.16*denim)+thread*(.012+.014*woven)+leather*(worn-.5)*.25;
            o.Albedo=_Color.rgb*variation;
            o.Smoothness=saturate(_Glossiness+leather*(grain-.5)*.06);
            if(_UseClothDetail>.5){
                float2 uv=IN.uv_MainTex*5;
                float fade=1-saturate(max(length(ddx(uv*64)),length(ddy(uv*64))));
                float3 detail=tex2D(_ClothNormal,uv).xyz*2-1;
                o.Normal=normalize(float3(detail.xy*.18*fade,1));
                o.Smoothness=saturate(o.Smoothness+(tex2D(_ClothRoughness,uv).r-.5)*.08);
            }
            o.Metallic=_Metallic;o.Alpha=1;
        }
        ENDCG
    }
    Fallback "Standard"
}
