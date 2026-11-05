Shader "Runway/Surface"
{
    Properties {
        _MainTex("Base texture",2D)="white"{}
        _Color("Color",Color)=(1,1,1,1)
        _Glossiness("Smoothness",Range(0,1))=.22
        _Metallic("Metallic",Range(0,1))=0
        _EmissionColor("Emission",Color)=(0,0,0,0)
        _PhotoTex("Photo front",2D)="white"{}
        _BackTex("Photo back",2D)="white"{}
        _UsePhoto("Use photo",Float)=0
        _UseBack("Use back",Float)=0
        _Solid("Solid garment",Float)=0
        _DressLayer("Neutral underlayer for reference dress",Float)=0
        _Wardrobe("Recolor uniform",Float)=0
        _TopColor("Top color",Color)=(.81,.77,.65,1)
        _BottomColor("Bottom color",Color)=(.12,.13,.15,1)
        _PhotoScale("Photo scale",Vector)=(1,1,0,0)
    }
    SubShader {
        Tags {"RenderType"="Opaque"}
        LOD 200
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0
        sampler2D _MainTex,_PhotoTex,_BackTex;
        fixed4 _Color,_EmissionColor,_TopColor,_BottomColor;
        half _Glossiness,_Metallic,_UsePhoto,_UseBack,_Solid,_Wardrobe,_DressLayer;
        float4 _PhotoScale;
        struct Input {float2 uv_MainTex;float2 uv2_PhotoTex;float3 worldNormal;float4 color:COLOR;};
        void surf(Input IN,inout SurfaceOutputStandard o){
            fixed4 original=tex2D(_MainTex,IN.uv_MainTex);
            fixed3 baseColor=lerp(original.rgb,1,_Solid)*_Color.rgb;
            float h=IN.uv2_PhotoTex.y*.65+.86;
            float x=abs((IN.uv2_PhotoTex.x-.5)*.52);
            float red=smoothstep(.025,.07,original.r-original.g)*smoothstep(0,.035,original.b-.9*original.g);
            float green=smoothstep(.015,.06,original.g-original.r)*smoothstep(.01,.045,original.g-original.b);
            float fabric=saturate(red+green);
            float torso=(1-smoothstep(.18,.23,x))*smoothstep(.91,.95,h)*(1-smoothstep(1.45,1.50,h));
            float upper=smoothstep(.875,.90,h)*(1-smoothstep(1.58,1.62,h))*max(fabric,torso);
            float lower=smoothstep(.54,.58,h)*(1-smoothstep(.875,.90,h))*(1-smoothstep(.22,.25,x))*fabric;
            baseColor=lerp(baseColor,_TopColor.rgb,upper*_Wardrobe);
            baseColor=lerp(baseColor,_BottomColor.rgb,lower*_Wardrobe);
            float underlayer=smoothstep(.82,.86,h)*(1-smoothstep(1.55,1.59,h));
            float shoes=1-smoothstep(.17,.20,h);
            baseColor=lerp(baseColor,float3(.055,.06,.065),max(underlayer*(1-IN.color.a),shoes)*_DressLayer);
            float2 uv=(IN.uv2_PhotoTex-.5)/max(_PhotoScale.xy,.1)+.5+_PhotoScale.zw;
            fixed4 photo=tex2D(_PhotoTex,uv);
            float3 localNormal=mul((float3x3)unity_WorldToObject,IN.worldNormal);
            float back=step(localNormal.z,0)*_UseBack;
            photo=lerp(photo,tex2D(_BackTex,float2(1-uv.x,uv.y)),back);
            float inside=step(0,uv.x)*step(uv.x,1)*step(0,uv.y)*step(uv.y,1);
            o.Albedo=lerp(baseColor,photo.rgb,_UsePhoto*inside*photo.a);o.Metallic=_Metallic;o.Smoothness=_Glossiness;o.Alpha=1;o.Emission=_EmissionColor.rgb;
        }
        ENDCG
    }
    Fallback "Standard"
}
