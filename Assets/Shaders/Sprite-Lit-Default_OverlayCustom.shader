Shader "Universal Render Pipeline/2D/Sprite-Lit-Overlay"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _ZWrite("ZWrite", Float) = 0

        // Overlay texture properties
        [Space(10)]
        [Header(Overlay Settings)]
        _OverlayTex("Overlay Texture", 2D) = "white" {}
        _OverlayColor("Overlay Color", Color) = (1,1,1,1)
        _OverlayOpacity("Overlay Opacity", Range(0, 1)) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _OverlayBlendSrc("Overlay Blend Mode (Source)", Float) = 5 // SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _OverlayBlendDst("Overlay Blend Mode (Destination)", Float) = 10 // OneMinusSrcAlpha
        
        [Space(5)]
        [Header(World UV Settings)]
        _OverlayScaleX("Overlay Scale X", Float) = 0.1
        _OverlayScaleY("Overlay Scale Y", Float) = 0.1
        _OverlayOffsetX("Overlay Offset X", Float) = 0
        _OverlayOffsetY("Overlay Offset Y", Float) = 0

        // Legacy properties. They're here so that materials using this shader can gracefully fallback to the legacy sprite shader.
        [HideInInspector] _Color("Tint", Color) = (1,1,1,1)
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
        Cull Off
        ZWrite [_ZWrite]
        ZTest Off

        Pass
        {
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex CombinedShapeLightVertex
            #pragma fragment CombinedShapeLightFragment

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"

            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_SKINNED_VERTEX_INPUTS
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS  : SV_POSITION;
                half4   color       : COLOR;
                float2  uv          : TEXCOORD0;
                half2   lightingUV  : TEXCOORD1;
                float3  positionWS  : TEXCOORD2; // Always include world position for overlay
                UNITY_VERTEX_OUTPUT_STEREO
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX(_MainTex);

            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);

            // Overlay texture
            TEXTURE2D(_OverlayTex);
            SAMPLER(sampler_OverlayTex);

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _OverlayColor;
                float _OverlayOpacity;
                float _OverlayBlendSrc;
                float _OverlayBlendDst;
                float4 _OverlayTex_ST;
                float _OverlayScaleX;
                float _OverlayScaleY;
                float _OverlayOffsetX;
                float _OverlayOffsetY;
            CBUFFER_END

            #if USE_SHAPE_LIGHT_TYPE_0
            SHAPE_LIGHT(0)
            #endif

            #if USE_SHAPE_LIGHT_TYPE_1
            SHAPE_LIGHT(1)
            #endif

            #if USE_SHAPE_LIGHT_TYPE_2
            SHAPE_LIGHT(2)
            #endif

            #if USE_SHAPE_LIGHT_TYPE_3
            SHAPE_LIGHT(3)
            #endif

            Varyings CombinedShapeLightVertex(Attributes v)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_SKINNED_VERTEX_COMPUTE(v);

                v.positionOS = UnityFlipSprite(v.positionOS, unity_SpriteProps.xy);
                o.positionCS = TransformObjectToHClip(v.positionOS);
                // Always compute world position for overlay texture
                o.positionWS = TransformObjectToWorld(v.positionOS);
                o.uv = v.uv;
                o.lightingUV = half2(ComputeScreenPos(o.positionCS / o.positionCS.w).xy);

                o.color = v.color * _Color * unity_SpriteColor;
                return o;
            }

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            // Custom blend function based on blend mode parameters
            half4 BlendOverlay(half4 base, half4 overlay)
            {
                half4 result = base;
                
                // Apply opacity to the overlay
                overlay.a *= _OverlayOpacity;
                
                // Simple alpha blending
                // Note: For more complex blend modes, you would implement specific blend calculations here
                float srcAlpha = overlay.a;
                float dstAlpha = 1.0 - srcAlpha;
                
                result.rgb = overlay.rgb * srcAlpha + base.rgb * dstAlpha;
                // Preserve the base alpha for proper transparency handling
                // result.a remains unchanged from base.a
                
                return result;
            }

            half4 CombinedShapeLightFragment(Varyings i) : SV_Target
            {
                // Sample main texture
                half4 main = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                
                // Sample overlay texture using world position
                float2 worldUV = float2(
                    i.positionWS.x * _OverlayScaleX + _OverlayOffsetX,
                    i.positionWS.y * _OverlayScaleY + _OverlayOffsetY
                );
                half4 overlay = SAMPLE_TEXTURE2D(_OverlayTex, sampler_OverlayTex, worldUV) * _OverlayColor;
                
                // Apply overlay blending
                half4 blended = BlendOverlay(main, overlay);
                
                // Continue with normal lighting process
                const half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv);
                SurfaceData2D surfaceData;
                InputData2D inputData;

                InitializeSurfaceData(blended.rgb, main.a, mask, surfaceData);
                InitializeInputData(i.uv, i.lightingUV, inputData);

                SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData, i.positionWS, i.positionCS, _MainTex);

                return CombinedShapeLightShared(surfaceData, inputData);
            }
            ENDHLSL
        }

        Pass
        {
            ZWrite Off

            Tags { "LightMode" = "NormalsRendering"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex NormalsRenderingVertex
            #pragma fragment NormalsRenderingFragment

            #pragma multi_compile _ SKINNED_SPRITE

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                float4 tangent      : TANGENT;
                UNITY_SKINNED_VERTEX_INPUTS
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS      : SV_POSITION;
                half4   color           : COLOR;
                float2  uv              : TEXCOORD0;
                half3   normalWS        : TEXCOORD1;
                half3   tangentWS       : TEXCOORD2;
                half3   bitangentWS     : TEXCOORD3;
                float3  positionWS      : TEXCOORD4; // Added for world position
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            // Overlay texture
            TEXTURE2D(_OverlayTex);
            SAMPLER(sampler_OverlayTex);

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START( UnityPerMaterial )
                half4 _Color;
                half4 _OverlayColor;
                float _OverlayOpacity;
                float _OverlayBlendSrc;
                float _OverlayBlendDst;
                float4 _OverlayTex_ST;
                float _OverlayScaleX;
                float _OverlayScaleY;
                float _OverlayOffsetX;
                float _OverlayOffsetY;
            CBUFFER_END

            Varyings NormalsRenderingVertex(Attributes attributes)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(attributes);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_SKINNED_VERTEX_COMPUTE(attributes);

                attributes.positionOS = UnityFlipSprite(attributes.positionOS, unity_SpriteProps.xy);
                o.positionCS = TransformObjectToHClip(attributes.positionOS);
                o.positionWS = TransformObjectToWorld(attributes.positionOS); // Add world position
                o.uv = attributes.uv;
                o.color = attributes.color;
                o.normalWS = -GetViewForwardDir();
                o.tangentWS = TransformObjectToWorldDir(attributes.tangent.xyz);
                o.bitangentWS = cross(o.normalWS, o.tangentWS) * attributes.tangent.w;
                return o;
            }

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl"

            // Custom blend function based on blend mode parameters
            half4 BlendOverlay(half4 base, half4 overlay)
            {
                half4 result = base;
                
                // Apply opacity to the overlay
                overlay.a *= _OverlayOpacity;
                
                // Simple alpha blending
                float srcAlpha = overlay.a;
                float dstAlpha = 1.0 - srcAlpha;
                
                result.rgb = overlay.rgb * srcAlpha + base.rgb * dstAlpha;
                
                return result;
            }

            half4 NormalsRenderingFragment(Varyings i) : SV_Target
            {
                // Sample main texture
                half4 mainTex = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                
                // Sample overlay texture using world position
                float2 worldUV = float2(
                    i.positionWS.x * _OverlayScaleX + _OverlayOffsetX,
                    i.positionWS.y * _OverlayScaleY + _OverlayOffsetY
                );
                half4 overlay = SAMPLE_TEXTURE2D(_OverlayTex, sampler_OverlayTex, worldUV) * _OverlayColor;
                
                // Apply overlay blending
                half4 blended = BlendOverlay(mainTex, overlay);
                
                const half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv));

                return NormalsRenderingShared(blended, normalTS, i.tangentWS.xyz, i.bitangentWS.xyz, i.normalWS.xyz);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "UniversalForward" "Queue"="Transparent" "RenderType"="Transparent"}

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
            #if defined(DEBUG_DISPLAY)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl"
            #endif

            #pragma vertex UnlitVertex
            #pragma fragment UnlitFragment

            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            struct Attributes
            {
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_SKINNED_VERTEX_INPUTS
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4  positionCS      : SV_POSITION;
                float4  color           : COLOR;
                float2  uv              : TEXCOORD0;
                float3  positionWS      : TEXCOORD2; // Always include world position
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            UNITY_TEXTURE_STREAMING_DEBUG_VARS_FOR_TEX(_MainTex);

            // Overlay texture
            TEXTURE2D(_OverlayTex);
            SAMPLER(sampler_OverlayTex);

            // NOTE: Do not ifdef the properties here as SRP batcher can not handle different layouts.
            CBUFFER_START( UnityPerMaterial )
                half4 _Color;
                half4 _OverlayColor;
                float _OverlayOpacity;
                float _OverlayBlendSrc;
                float _OverlayBlendDst;
                float4 _OverlayTex_ST;
                float _OverlayScaleX;
                float _OverlayScaleY;
                float _OverlayOffsetX;
                float _OverlayOffsetY;
            CBUFFER_END

            Varyings UnlitVertex(Attributes attributes)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(attributes);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_SKINNED_VERTEX_COMPUTE(attributes);

                attributes.positionOS = UnityFlipSprite( attributes.positionOS, unity_SpriteProps.xy);
                o.positionCS = TransformObjectToHClip(attributes.positionOS);
                // Always include world position
                o.positionWS = TransformObjectToWorld(attributes.positionOS);
                o.uv = attributes.uv;
                o.color = attributes.color * _Color * unity_SpriteColor;
                return o;
            }

            // Custom blend function based on blend mode parameters
            half4 BlendOverlay(half4 base, half4 overlay)
            {
                half4 result = base;
                
                // Apply opacity to the overlay
                overlay.a *= _OverlayOpacity;
                
                // Simple alpha blending
                float srcAlpha = overlay.a;
                float dstAlpha = 1.0 - srcAlpha;
                
                result.rgb = overlay.rgb * srcAlpha + base.rgb * dstAlpha;
                
                return result;
            }

            float4 UnlitFragment(Varyings i) : SV_Target
            {
                // Sample main texture
                float4 mainTex = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                
                // Sample overlay texture using world position
                float2 worldUV = float2(
                    i.positionWS.x * _OverlayScaleX + _OverlayOffsetX,
                    i.positionWS.y * _OverlayScaleY + _OverlayOffsetY
                );
                half4 overlay = SAMPLE_TEXTURE2D(_OverlayTex, sampler_OverlayTex, worldUV) * _OverlayColor;
                
                // Apply overlay blending
                half4 blended = BlendOverlay(mainTex, overlay);

                #if defined(DEBUG_DISPLAY)
                SurfaceData2D surfaceData;
                InputData2D inputData;
                half4 debugColor = 0;

                InitializeSurfaceData(blended.rgb, mainTex.a, surfaceData);
                InitializeInputData(i.uv, inputData);
                SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData, i.positionWS, i.positionCS, _MainTex);

                if(CanDebugOverrideOutputColor(surfaceData, inputData, debugColor))
                {
                    return debugColor;
                }
                #endif

                return half4(blended.rgb, mainTex.a);
            }
            ENDHLSL
        }
    }
}