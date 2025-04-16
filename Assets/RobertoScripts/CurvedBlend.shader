Shader "Custom/CurvedBlend"
{
    Properties
    {
        _MainTex ("Stitched Texture", 2D) = "white" {}
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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Curvature;

            v2f vert (appdata v)
            {
                v2f o;
                float curve = sin((v.uv.x - 0.5) * _Curvature * 3.14159);
                v.vertex.xyz += float3(0, 0, curve * 0.5);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}
