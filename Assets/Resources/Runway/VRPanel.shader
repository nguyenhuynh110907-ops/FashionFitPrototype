Shader "Runway/VRPanel"
{
    Properties {_Color("Color",Color)=(.08,.10,.12,1)}
    SubShader {
        Tags {"RenderType"="Opaque"}
        Cull Off
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            fixed4 _Color;
            struct Input {float4 vertex:POSITION;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct Output {float4 vertex:SV_POSITION;UNITY_VERTEX_OUTPUT_STEREO};
            Output vert(Input v){Output o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(Output,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.vertex=UnityObjectToClipPos(v.vertex);return o;}
            fixed4 frag(Output i):SV_Target {UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);return _Color;}
            ENDCG
        }
    }
}
