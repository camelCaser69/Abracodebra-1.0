Shader "UI/BezierCurveAA"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _EdgeSmoothness ("Edge Smoothness", Range(0.5, 3.0)) = 1.0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _Color;
            float _EdgeSmoothness;

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 screenPos : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color * _Color;
                o.screenPos = o.vertex.xy / o.vertex.w;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Use screen-space derivatives to smooth the line edges.
                float alpha = i.color.a;

                // Compute screen-space derivative-based AA
                float edgeAlpha = fwidth(alpha) * _EdgeSmoothness;
                alpha = smoothstep(0.0, edgeAlpha, alpha);

                return fixed4(i.color.rgb, alpha);
            }
            ENDCG
        }
    }
}
