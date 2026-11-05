Shader "FittingRoom/Fabric"
{
 Properties {
  _MainTex("UV channel",2D)="white"{}
  _Color("Fabric color",Color)=(.22,.31,.38,1)
  _Glossiness("Smoothness",Range(0,1))=.25
  _Weave("Micro weave",Range(0,1))=1
  _BaseMap("Base color (sRGB)",2D)="white"{}
  _NormalMap("Normal (OpenGL +Y, linear RGB)",2D)="bump"{}
  _UseBase("Use base map",Float)=0
  _UseNormal("Use normal map",Float)=0
  _RoughnessMap("Roughness (linear)",2D)="white"{}
  _UseRoughness("Use roughness map",Float)=0
  _RoughnessBias("Roughness adjustment",Range(-1,1))=0
  _MapScale("Texture repetitions",Float)=1
 }
 SubShader {
  Tags {"RenderType"="Opaque"} Cull Off
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows addshadow
  #pragma target 3.0
  sampler2D _BaseMap,_NormalMap,_RoughnessMap;
  fixed4 _Color;half _Glossiness,_Weave,_UseBase,_UseNormal,_MapScale,_UseRoughness,_RoughnessBias;
  struct Input {float2 uv_MainTex;};
  void surf(Input IN,inout SurfaceOutputStandard o){
   float2 p=IN.uv_MainTex*1100;
   float fade=1-saturate(max(fwidth(p.x),fwidth(p.y))*.7);
   float warp=sin(p.x*6.28318),weft=sin(p.y*6.28318);
   fixed3 baseMap=tex2D(_BaseMap,IN.uv_MainTex*_MapScale).rgb;
   o.Albedo=_Color.rgb*lerp(1,baseMap,_UseBase)*(1+.018*(warp+weft)*fade*_Weave);
   float3 weaveNormal=normalize(float3(warp*.045*fade*_Weave,weft*.045*fade*_Weave,1));
   float3 normalMap=normalize(tex2D(_NormalMap,IN.uv_MainTex*_MapScale).xyz*2-1);
   o.Normal=normalize(lerp(weaveNormal,normalMap,_UseNormal));
   o.Metallic=0;o.Smoothness=_Glossiness;
   if(_UseRoughness>.5)o.Smoothness=1-saturate(tex2D(_RoughnessMap,IN.uv_MainTex*_MapScale).r+_RoughnessBias);
   o.Occlusion=1;o.Alpha=1;
  }
  ENDCG
 }
 Fallback "Standard"
}
