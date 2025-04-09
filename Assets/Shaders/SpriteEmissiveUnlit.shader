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
            // NO LONGER INCLUDING: Packages/com.unity.render-pipelines.universal/ShaderLibrary/2D/Sprite.hlsl


            // Define Material Properties (match Properties block)
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _EmissionColor; // HDR Emission
                float _PixelSnap;
            CBUFFER_END

            // Define standard URP Per-Renderer / Sprite properties
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float4 _MainTex_ST; // Needed for TRANSFORM_TEX macro

            TEXTURE2D(_AlphaTex); SAMPLER(sampler_AlphaTex);

            // Input structure for the vertex shader (standard attributes)
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 texcoord     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            // Input structure for the fragment shader
            struct Varyings
            {
                float4 positionHCS    : SV_POSITION;
                float2 uv             : TEXCOORD0;
                half4 color           : COLOR; // Includes vertex color + renderer color
                // Removed scale for flip - handle flip directly if needed, simplifies dependencies
                 UNITY_VERTEX_OUTPUT_STEREO // For Stereo Rendering
            };


            // Vertex Shader (simplified, relying on Core.hlsl and standard functions)
            Varyings SpriteVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // Use standard URP vertex transformation
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                // Use standard texture coordinate transformation
                output.uv = TRANSFORM_TEX(input.texcoord, _MainTex);
                // Combine vertex color and material Tint (_Color)
                // Note: _RendererColor is often handled internally or via different mechanism in modern URP sprites
                output.color = input.color * _Color;

                // Handle Pixel Snap option
                #ifdef PIXELSNAP_ON // Check if keyword is defined by material
                output.positionHCS = UnityPixelSnap(output.positionHCS);
                #endif

                return output;
            }


            // Fragment Shader
            half4 SpriteFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Sample the main texture
                half4 mainTexSample = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                // Combine texture color with vertex/tint color
                half4 finalColor = mainTexSample * input.color;

                // Apply emission (additive to the base color before alpha application)
                // Emission doesn't affect alpha, only RGB
                finalColor.rgb += _EmissionColor.rgb; // Additive emission

                // Handle external alpha if enabled (usually for UI masks)
                #if defined(_USE_EXTERNAL_ALPHA) // Check for keyword if Feature is enabled
                 half alpha = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, input.uv).a;
                 finalColor.a *= alpha;
                #endif

                // Apply alpha clipping/threshold if needed (standard Sprite-Lit does this, optional here)
                // clip(finalColor.a - _AlphaClip);

                // Add simple sprite flip handling based on vertex color alpha if needed (common technique)
                // Or rely on SpriteRenderer component's Flip setting (often handled before shader)

                return finalColor;
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/2D/Sprite-Unlit-Default" // Fallback to standard sprite shader
}