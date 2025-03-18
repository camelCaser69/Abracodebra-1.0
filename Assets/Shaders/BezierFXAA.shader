Shader "UI/BezierFXAA_Advanced"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // FXAA parameters
        _FXAAEnable ("Enable FXAA (0=Off, 1=On)", Range(0,1)) = 1
        _ContrastThreshold ("Contrast Threshold", Range(0.0, 0.1)) = 0.03
        _FXAAOffset ("FXAA Offset Scale", Range(0.0, 2.0)) = 0.5

        // If you want to experiment with a basic blur-based AA:
        //  0 = None, 1 = FXAA, 2 = Simple blur
        _AAType ("AA Type (0=None,1=FXAA,2=Blur)", Range(0,2)) = 1
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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;

            // We might not get a valid texel size for the default white texture,
            // so we provide a fallback using _ScreenParams.
            // If you want to override it in code, you can set it manually.
            float4 _MainTex_TexelSize; // x,y=1/width,height

            float _FXAAEnable;
            float _ContrastThreshold;
            float _FXAAOffset;

            float _AAType;  // 0=None, 1=FXAA, 2=Blur (optional experiment)

            // Vertex
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            // A simple 3-tap blur as an alternate "AA" approach for demonstration.
            // It won't be as "intelligent" as FXAA, but sometimes helps smooth edges.
            fixed4 SimpleBlur(sampler2D tex, float2 uv, float2 texel, fixed4 tint)
            {
                // Weighted average of center + 2 neighbors
                fixed4 c0 = tex2D(tex, uv);
                fixed4 c1 = tex2D(tex, uv + texel * float2(1,0));
                fixed4 c2 = tex2D(tex, uv + texel * float2(-1,0));
                return (c0 + c1 + c2) / 3.0 * tint;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Base sample: the typical UI/Default approach
                fixed4 baseColor = tex2D(_MainTex, i.texcoord) * i.color;

                // Early out if AA is disabled
                if (_AAType < 0.5)
                {
                    // 0 => no AA
                    return baseColor;
                }

                // Get valid texel size or fallback to screen-based guess
                float2 texel = _MainTex_TexelSize.xy;
                if (texel.x == 0)
                    texel = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);

                // If user chose the "blur" type
                if (_AAType > 1.5)
                {
                    // Basic blur approach
                    return SimpleBlur(_MainTex, i.texcoord, texel, i.color);
                }

                // Otherwise do FXAA ( _AAType ~1 )
                if (_FXAAEnable < 0.5)
                {
                    // If the user specifically disabled it via _FXAAEnable
                    return baseColor;
                }

                // -- FXAA Logic --
                float3 lumaVec = float3(0.299, 0.587, 0.114);

                float lumaTL = dot(tex2D(_MainTex, i.texcoord + texel * float2(-1,  1)).rgb, lumaVec);
                float lumaTR = dot(tex2D(_MainTex, i.texcoord + texel * float2( 1,  1)).rgb, lumaVec);
                float lumaBL = dot(tex2D(_MainTex, i.texcoord + texel * float2(-1, -1)).rgb, lumaVec);
                float lumaBR = dot(tex2D(_MainTex, i.texcoord + texel * float2( 1, -1)).rgb, lumaVec);

                float lumaMin = min(lumaTL, min(lumaTR, min(lumaBL, lumaBR)));
                float lumaMax = max(lumaTL, max(lumaTR, max(lumaBL, lumaBR)));

                // If local contrast is too low, skip
                if (lumaMax - lumaMin < _ContrastThreshold)
                    return baseColor;

                // Edge direction
                float2 dir = normalize(
                                float2(lumaTL + lumaTR - (lumaBL + lumaBR),
                                       lumaTL + lumaBL - (lumaTR + lumaBR))
                             );

                // Offset the sample along the detected edge
                float2 newUV = i.texcoord + dir * texel * _FXAAOffset;

                // Final color
                fixed4 fxaaColor = tex2D(_MainTex, newUV) * i.color;
                return fxaaColor;
            }
            ENDCG
        }
    }
    Fallback "UI/Default"
}
