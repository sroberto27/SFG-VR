Shader "Custom/CurvedBlendDual"
{
    Properties
    {
        _LeftTex ("Left Half", 2D) = "white" {}
        _RightTex ("Right Half", 2D) = "white" {}
        _Curvature ("Curvature", Range(0.0, 2.0)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _LeftTex;
            sampler2D _RightTex;
            float _Curvature;

            v2f vert (appdata v)
            {
                v2f o;
                float curve = sin((v.uv.x - 0.5) * _Curvature * 3.14159);
                v.vertex.xyz += float3(0, 0, curve * 0.5);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                if (uv.x < 0.5)
                {
                    return tex2D(_LeftTex, float2(uv.x * 2, uv.y));
                }
                else
                {
                    return tex2D(_RightTex, float2((uv.x - 0.5) * 2, uv.y));
                }
            }
            ENDCG
        }
    }
}
