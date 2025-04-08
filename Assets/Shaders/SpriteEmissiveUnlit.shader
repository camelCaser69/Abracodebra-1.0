Shader "Universal Render Pipeline/2D/Sprite Emissive Unlit"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HDR] _EmissionColor ("Emission Color", Color) = (0,0,0,1) // Default to no emission
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "RenderPipeline"="UniversalPipeline"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha // Standard alpha blending

        Pass
        {
            Name "SpriteEmissivePass"

            HLSLPROGRAM
            #pragma vertex SpriteVertex
            #pragma fragment SpriteFragment

            // Use URP's core library functions
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // Use URP's 2D library functions for Sprite handling
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/2D/Sprite.hlsl"


            // Define Material Properties (match Properties block)
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _EmissionColor; // HDR Emission
                float _PixelSnap;
            CBUFFER_END


            // Input structure for the fragment shader
            struct Varyings
            {
                float4 positionHCS    : SV_POSITION;
                float2 uv             : TEXCOORD0;
                half4 color           : COLOR; // Includes vertex color + renderer color
#if defined(_FLIP_ON)
                half2 scale           : TEXCOORD1; // Store flip scale
#endif
                 UNITY_VERTEX_OUTPUT_STEREO // For Stereo Rendering
            };


            // Vertex Shader (mostly standard URP Sprite Vertex)
            Varyings SpriteVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.texcoord;
                output.color = input.color * _Color * _RendererColor; // Combine vertex, material tint, and renderer color

                // Handle Pixel Snap option
                #if defined(PIXELSNAP_ON)
                output.positionHCS = UnityPixelSnap(output.positionHCS);
                #endif

                // Handle Flip property (from SpriteRenderer component)
                #if defined(_FLIP_ON)
                output.scale = _Flip.xy;
                #endif

                return output;
            }


            // Fragment Shader
            half4 SpriteFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Apply Sprite Flip if necessary
                #if defined(_FLIP_ON)
                // Flip UVs only if scale component is negative
                // Use step() for potentially better performance than branch/if
                input.uv.x = lerp(input.uv.x, 1.0 - input.uv.x, step(input.scale.x, 0));
                input.uv.y = lerp(input.uv.y, 1.0 - input.uv.y, step(input.scale.y, 0));
                #endif

                // Sample the main texture
                half4 mainTexSample = SampleSpriteTexture(input.uv);

                // Combine texture color with vertex/tint color
                half4 finalColor = mainTexSample * input.color;

                // Apply emission (additive to the base color before alpha application)
                // Emission doesn't affect alpha, only RGB
                finalColor.rgb += _EmissionColor.rgb; // Additive emission

                // Handle external alpha if enabled (usually for UI masks)
                #if defined(_USE_EXTERNAL_ALPHA)
                // Sample external alpha texture
                half alpha = SampleSpriteAlpha(input.uv);
                // Multiply final alpha by external alpha
                finalColor.a *= alpha;
                #endif

                // Apply alpha clipping/threshold if needed (standard Sprite-Lit does this, optional here)
                // clip(finalColor.a - _AlphaClip);

                return finalColor;
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/2D/Sprite-Unlit-Default" // Fallback to standard sprite shader
}