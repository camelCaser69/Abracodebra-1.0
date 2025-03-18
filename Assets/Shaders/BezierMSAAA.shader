Shader "UI/Default_MSAAApprox"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // How many samples to approximate
        // (Only 4 in code below, but you can set offsets for 8 if you like.)
        _SampleScale ("Sample Offset Scale", Range(0,2)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="true"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="true"
            "PreviewType"="Plane"
        }

        ZTest [unity_GUIZTestMode]
        ZWrite Off
        Cull Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            //=================================================
            // STRUCTS
            //=================================================
            struct appdata
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            //=================================================
            // UNIFORMS
            //=================================================
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            // In case Unity doesn't supply a real texel size for the default white texture
            float4 _MainTex_TexelSize; // x,y = 1/width,1/height

            // Additional property for scaling the sampling offsets
            float _SampleScale;

            //=================================================
            // VERTEX
            //=================================================
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            //=================================================
            // FRAGMENT
            //=================================================
            fixed4 frag(v2f i) : SV_Target
            {
                // Base color sampling (like UI/Default)
                // We will do multiple samples around this UV.
                // 
                // For a truly crisp result, the geometry must be high-res enough
                // so that line edges are sub-pixel. If the line is literally 1px thick,
                // multi-sampling helps only so much.

                // Fallback if we have no valid texel size
                float2 texel = _MainTex_TexelSize.xy;
                if (texel.x <= 0.0)
                    texel.x = 1.0 / _ScreenParams.x;
                if (texel.y <= 0.0)
                    texel.y = 1.0 / _ScreenParams.y;

                // Scale for how far we offset each sample
                float2 offset = texel * _SampleScale;

                // We'll do 4 sub-samples. You can expand this to 8 or more if you like.
                // Offsets chosen in a diagonal pattern for 4x sampling
                float2 sampleOffsets[4] = {
                    float2(-0.5, -0.5),
                    float2( 0.5, -0.5),
                    float2(-0.5,  0.5),
                    float2( 0.5,  0.5)
                };

                // Accumulate color
                fixed4 colorSum = 0;
                for (int s = 0; s < 4; s++)
                {
                    float2 sampleUV = i.texcoord + sampleOffsets[s] * offset;
                    colorSum += tex2D(_MainTex, sampleUV) * i.color;
                }

                // Average the 4 samples
                fixed4 finalColor = colorSum * 0.25;

                return finalColor;
            }
            ENDCG
        }
    }
    Fallback "UI/Default"
}
