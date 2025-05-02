Shader "Unlit/VROcclusionMask"
{
    Properties
    {
        _MainTex("Base (RGB)", 2D) = "white" {}
        _MaskRadius("Mask Radius", Range(0.0, 1.0)) = 0.9
        _EdgeSoftness("Edge Softness", Range(0.001, 0.5)) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma multi_compile _ STEREO_INSTANCING_ON

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float2 screenUV : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _MaskRadius;
            float _EdgeSoftness;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screenUV = (o.vertex.xy / o.vertex.w) * 0.5 + 0.5;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 center = float2(0.5, 0.5);
                float dist = distance(i.screenUV, center);
                float mask = smoothstep(_MaskRadius, _MaskRadius - _EdgeSoftness, dist);
                fixed4 col = tex2D(_MainTex, i.uv);
                return col * (1.0 - mask);
            }
            ENDHLSL
        }
    }
}
