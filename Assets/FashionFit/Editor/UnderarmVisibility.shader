Shader "Hidden/FashionFit/UnderarmVisibility"
{
    Properties {_Positions("Export world coordinates",Float)=0}
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct output {float4 position:SV_POSITION;float3 model:TEXCOORD0;};
            // Skinned render input may be expressed relative to the root bone.
            // This isolated QA scene keeps the mannequin at the world origin.
            output vert(float4 p:POSITION){output o;o.position=UnityObjectToClipPos(p);o.model=mul(unity_ObjectToWorld,p).xyz;return o;}
            float _Positions;
            float4 frag(output i):SV_Target
            {
                float x=abs(i.model.x),y=i.model.y;
                // Cuffs end below 1.255 m. This band belongs inside the shirt.
                bool covered=x>.15&&x<.255&&y>1.275&&y<1.42;
                if(!covered)return float4(0,0,0,1);
                return _Positions>.5?float4(i.model,1):float4(1,0,0,1);
            }
            ENDCG
        }
    }
}
