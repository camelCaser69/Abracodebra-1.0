Shader "Universal Render Pipeline/2D/Sprite-Lit-Advanced_TopDownReflection"
{
    Properties
    {
        _MainTex("Diffuse", 2D) = "white" {}
        _MaskTex("Mask", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "bump" {}
        _ZWrite("ZWrite", Float) = 0

        // REFLECTION TEXTURE - User must supply this from a Reflection Camera
        [Header(Mirror Reflection Settings)]
        _ReflectionTex("Reflection Layer (Render Texture)", 2D) = "black" {} // Assign your Render Texture here
        _ReflectionIntensity("Reflection Intensity", Range(0,1)) = 0.7
        _ReflectionDistortion("Reflection Distortion", Range(0, 0.1)) = 0.02
        _ReflectionDistortionScale("Reflection Distortion Scale", Float) = 10.0
        _ReflectionDistortionSpeed("Reflection Distortion Speed", Float) = 0.5
        _ReflectionColorTint("Reflection Tint", Color) = (0.7, 0.85, 1.0, 1.0) // To tint the reflection

        // Overlay texture properties
        [Space(10)]
        [Header(Overlay Settings)]
        [Toggle(_OVERLAY_ON)] _EnableOverlay("Enable Overlay", Float) = 0
        _OverlayTex("Overlay Texture", 2D) = "white" {}
        _OverlayColor("Overlay Color", Color) = (1,1,1,1)
        _OverlayOpacity("Overlay Opacity", Range(0, 1)) = 1
        [Enum(Normal,0,Additive,1,Multiply,2,Screen,3)] _OverlayBlendMode("Overlay Blend Mode", Float) = 0
        _OverlayScaleX("Overlay Scale X", Float) = 0.1
        _OverlayScaleY("Overlay Scale Y", Float) = 0.1
        _OverlayOffsetX("Overlay Offset X", Float) = 0
        _OverlayOffsetY("Overlay Offset Y", Float) = 0
        _OverlayScrollSpeedX("Overlay Scroll Speed X", Float) = 0
        _OverlayScrollSpeedY("Overlay Scroll Speed Y", Float) = 0

        // Water effect properties
        [Space(10)]
        [Header(Other Water Effects)]
        [Toggle(_WATER_ON)] _EnableWater("Enable Other Water Effects", Float) = 0 // Renamed for clarity
        _WaterColor("Water Base Color Tint", Color) = (0.3, 0.5, 0.8, 0.6)
        _WaterDepth("Water Tint Strength", Range(0, 1)) = 0.7
        
        [Space(5)]
        [Header(Foam and Whitecaps)]
        _FoamTex("Foam Texture", 2D) = "white" {}
        _FoamScale("Foam Scale", Float) = 0.5
        _FoamSpeed("Foam Speed", Float) = 0.2
        _FoamThreshold("Foam Threshold", Range(0, 1)) = 0.7
        _FoamOpacity("Foam Opacity", Range(0, 1)) = 0.8
        _FoamColor("Foam Color", Color) = (1, 1, 1, 1)
        
        [Space(5)]
        [Header(Caustics)]
        [Toggle(_CAUSTICS_ON)] _EnableCaustics("Enable Caustics", Float) = 0
        _CausticsTex("Caustics Texture", 2D) = "white" {}
        _CausticsScale("Caustics Scale", Float) = 0.3
        _CausticsSpeed("Caustics Speed", Float) = 0.1
        _CausticsStrength("Caustics Strength", Range(0, 1)) = 0.5

        [HideInInspector] _VertexTint("Vertex Tint", Color) = (1,1,1,1) // Renamed from _Color to avoid confusion
        [HideInInspector] _RendererColor("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _AlphaTex("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags {"Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha Cull Off ZWrite [_ZWrite] ZTest LEqual

        Pass
        {
            Name "Universal2D" // Pass Name for clarity
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl"

            #pragma vertex CombinedShapeLightVertex
            #pragma fragment CombinedShapeLightFragment

            #pragma shader_feature_local _OVERLAY_ON
            #pragma shader_feature_local _WATER_ON 
            #pragma shader_feature_local _CAUSTICS_ON 

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/ShapeLightShared.hlsl"
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE

            struct Attributes { 
                float3 positionOS   : POSITION; 
                float4 color        : COLOR; 
                float2 uv           : TEXCOORD0; 
                UNITY_SKINNED_VERTEX_INPUTS 
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings { 
                float4  positionCS  : SV_POSITION; 
                half4   color       : COLOR; 
                float2  uv          : TEXCOORD0; 
                half2   screenUV    : TEXCOORD1; 
                float3  positionWS  : TEXCOORD2; 
                UNITY_VERTEX_OUTPUT_STEREO
            };

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            TEXTURE2D(_MaskTex); SAMPLER(sampler_MaskTex);
            TEXTURE2D(_ReflectionTex); SAMPLER(sampler_ReflectionTex); 

            #if _OVERLAY_ON
            TEXTURE2D(_OverlayTex); SAMPLER(sampler_OverlayTex);
            #endif

            #if _WATER_ON
            TEXTURE2D(_FoamTex); SAMPLER(sampler_FoamTex);
                #if _CAUSTICS_ON
                TEXTURE2D(_CausticsTex); SAMPLER(sampler_CausticsTex);
                #endif
            #endif

            CBUFFER_START(UnityPerMaterial)
                half4 _VertexTint; 
                half _ReflectionIntensity; float _ReflectionDistortion; float _ReflectionDistortionScale; float _ReflectionDistortionSpeed;
                half4 _ReflectionColorTint;

                #if _OVERLAY_ON
                half4 _OverlayColor; float _OverlayOpacity; float _OverlayBlendMode;
                float _OverlayScaleX, _OverlayScaleY, _OverlayOffsetX, _OverlayOffsetY, _OverlayScrollSpeedX, _OverlayScrollSpeedY;
                #endif
                
                #if _WATER_ON
                half4 _WaterColor; float _WaterDepth;
                float _FoamScale; float _FoamSpeed; float _FoamThreshold; float _FoamOpacity; half4 _FoamColor;
                    #if _CAUSTICS_ON
                    float _CausticsScale; float _CausticsSpeed; float _CausticsStrength;
                    #endif
                #endif
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

            float2 GetWorldUV(float3 worldPos, float scaleX, float scaleY, float offsetX, float offsetY, float scrollX, float scrollY) {
                return float2(worldPos.x * scaleX + offsetX + _Time.y * scrollX, worldPos.y * scaleY + offsetY + _Time.y * scrollY);
            }

            half4 BlendColors(half4 base, half4 overlay, float blendMode, float opacity) {
                half4 result = base;
                if (blendMode == 0) { float srcAlpha = overlay.a * opacity; float dstAlpha = 1.0 - srcAlpha; result.rgb = overlay.rgb * srcAlpha + base.rgb * dstAlpha; result.a = base.a + srcAlpha * (1.0 - base.a); }
                else if (blendMode == 1) { result.rgb = base.rgb + overlay.rgb * overlay.a * opacity; }
                else if (blendMode == 2) { result.rgb = lerp(base.rgb, base.rgb * overlay.rgb, overlay.a * opacity); }
                else if (blendMode == 3) { half3 screen = 1.0 - (1.0 - base.rgb) * (1.0 - overlay.rgb); result.rgb = lerp(base.rgb, screen, overlay.a * opacity); }
                return result;
            }
            
            #if _WATER_ON
            half3 ApplyOtherWaterEffects(half3 currentColor, float2 spriteUV, float3 worldPos, half mainSpriteAlpha)
            {
                if (mainSpriteAlpha <= 0.001) { return currentColor; }
                half3 effectedColor = currentColor;
                effectedColor = lerp(effectedColor, _WaterColor.rgb, _WaterColor.a * _WaterDepth * mainSpriteAlpha);
                float2 foamUV = GetWorldUV(worldPos, _FoamScale, _FoamScale, 0, 0, _FoamSpeed, _FoamSpeed * 0.7);
                half4 foamSample = SAMPLE_TEXTURE2D(_FoamTex, sampler_FoamTex, foamUV);
                float foamMask = step(_FoamThreshold, foamSample.r);
                float foamFinalOpacity = foamMask * _FoamOpacity * foamSample.a * _FoamColor.a * mainSpriteAlpha;
                float foamOcclusionFactor = foamFinalOpacity;
                #if _CAUSTICS_ON
                float2 causticsUV1 = GetWorldUV(worldPos, _CausticsScale, _CausticsScale, 0, 0, _CausticsSpeed, -_CausticsSpeed * 0.5);
                float2 causticsUV2 = GetWorldUV(worldPos, _CausticsScale * 0.8, _CausticsScale * 0.8, 0.5, 0.5, -_CausticsSpeed * 0.7, _CausticsSpeed);
                half caustics1 = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, causticsUV1).r;
                half caustics2 = SAMPLE_TEXTURE2D(_CausticsTex, sampler_CausticsTex, causticsUV2).r;
                half caustics = min(caustics1 + caustics2, 1.0);
                effectedColor += caustics * _CausticsStrength * (1.0 - foamOcclusionFactor) * mainSpriteAlpha;
                #endif
                effectedColor = lerp(effectedColor, _FoamColor.rgb, foamFinalOpacity);
                return saturate(effectedColor);
            }
            #endif

            Varyings CombinedShapeLightVertex(Attributes v) {
                Varyings o=(Varyings)0; UNITY_SETUP_INSTANCE_ID(v); UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o); UNITY_SKINNED_VERTEX_COMPUTE(v);
                v.positionOS = UnityFlipSprite(v.positionOS, unity_SpriteProps.xy);
                o.positionCS = TransformObjectToHClip(v.positionOS); 
                o.positionWS = TransformObjectToWorld(v.positionOS);
                o.uv = v.uv;
                o.screenUV = ComputeScreenPos(o.positionCS).xy / ComputeScreenPos(o.positionCS).w;
                o.color = v.color * _VertexTint * unity_SpriteColor; 
                return o;
            }

            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

            half4 CombinedShapeLightFragment(Varyings i) : SV_Target {
                half4 mainColor = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
                half4 finalColor = mainColor; 
                if (_ReflectionIntensity > 0.001 && mainColor.a > 0.001) { 
                    float2 reflectionUV = i.screenUV; 
                    float2 distortionOffset = float2(0,0);
                    float timeVal = _Time.y * _ReflectionDistortionSpeed;
                    distortionOffset.x = sin(i.positionWS.x * _ReflectionDistortionScale * 0.1 + timeVal) * _ReflectionDistortion;
                    distortionOffset.y = cos(i.positionWS.y * _ReflectionDistortionScale * 0.1 + timeVal * 0.7 - i.positionWS.x * 0.02) * _ReflectionDistortion;
                    reflectionUV += distortionOffset;
                    reflectionUV = saturate(reflectionUV);
                    half4 reflectedSample = SAMPLE_TEXTURE2D(_ReflectionTex, sampler_ReflectionTex, reflectionUV);
                    finalColor.rgb = lerp(finalColor.rgb, 
                                          reflectedSample.rgb * _ReflectionColorTint.rgb, 
                                          reflectedSample.a * _ReflectionIntensity * _ReflectionColorTint.a * mainColor.a);
                }
                #if _OVERLAY_ON
                float2 overlayUV = GetWorldUV(i.positionWS, _OverlayScaleX, _OverlayScaleY, _OverlayOffsetX, _OverlayOffsetY, _OverlayScrollSpeedX, _OverlayScrollSpeedY);
                half4 overlaySample = SAMPLE_TEXTURE2D(_OverlayTex, sampler_OverlayTex, overlayUV) * _OverlayColor;
                finalColor = BlendColors(finalColor, overlaySample, _OverlayBlendMode, _OverlayOpacity);
                #endif
                #if _WATER_ON 
                finalColor.rgb = ApplyOtherWaterEffects(finalColor.rgb, i.uv, i.positionWS, mainColor.a);
                #endif
                const half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv);
                SurfaceData2D surfaceData; InputData2D inputData;
                InitializeSurfaceData(finalColor.rgb, finalColor.a, mask, surfaceData); 
                InitializeInputData(i.uv, i.screenUV, inputData); 
                SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData, i.positionWS, i.positionCS, _MainTex);
                return CombinedShapeLightShared(surfaceData, inputData);
            }
            ENDHLSL
        }

        // --- NormalsRendering Pass ---
        Pass 
        { 
            Name "NormalsRendering"
            Tags { "LightMode" = "NormalsRendering"} 
            ZWrite Off 
            HLSLPROGRAM 
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" 
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl" 
            
            #pragma vertex NormalsRenderingVertex 
            #pragma fragment NormalsRenderingFragment 
            #pragma multi_compile _ SKINNED_SPRITE 
            
            struct AttributesNormals { 
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                float4 tangent      : TANGENT;
                UNITY_SKINNED_VERTEX_INPUTS 
                UNITY_VERTEX_INPUT_INSTANCE_ID
            }; 
            struct VaryingsNormals {
                float4  positionCS      : SV_POSITION;
                half4   color           : COLOR;
                float2  uv              : TEXCOORD0;
                half3   normalWS        : TEXCOORD1;
                half3   tangentWS       : TEXCOORD2;
                half3   bitangentWS     : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            }; 
            
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalMap);SAMPLER(sampler_NormalMap); 
            
            CBUFFER_START(UnityPerMaterial)
                half4 _VertexTint;
            CBUFFER_END 
            
            VaryingsNormals NormalsRenderingVertex(AttributesNormals v_in){
                VaryingsNormals v_out = (VaryingsNormals)0;
                UNITY_SETUP_INSTANCE_ID(v_in);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(v_out);
                UNITY_SKINNED_VERTEX_COMPUTE(v_in);
                v_in.positionOS = UnityFlipSprite(v_in.positionOS, unity_SpriteProps.xy);
                v_out.positionCS = TransformObjectToHClip(v_in.positionOS);
                v_out.uv = v_in.uv;
                v_out.color = v_in.color * _VertexTint;
                v_out.normalWS = -GetViewForwardDir();
                v_out.tangentWS = TransformObjectToWorldDir(v_in.tangent.xyz);
                v_out.bitangentWS = cross(v_out.normalWS, v_out.tangentWS) * v_in.tangent.w;
                return v_out;
            } 
            
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl" 
            
            half4 NormalsRenderingFragment(VaryingsNormals i) : SV_Target {
                half4 mainTexAlbedo = i.color * SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap,sampler_NormalMap,i.uv));
                return NormalsRenderingShared(mainTexAlbedo, normalTS, i.tangentWS, i.bitangentWS, i.normalWS);
            } 
            ENDHLSL 
        }

        // --- Unlit Pass ---
        Pass 
        { 
            Name "UniversalForwardUnlit"
            Tags { "LightMode" = "UniversalForward" "Queue"="Transparent" "RenderType"="Transparent"} 
            
            HLSLPROGRAM 
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" 
            #include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Core2D.hlsl" 
            #if defined(DEBUG_DISPLAY) 
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Debug/Debugging2D.hlsl" 
            #endif 
            
            #pragma vertex UnlitVertex 
            #pragma fragment UnlitFragment 
            #pragma multi_compile _ DEBUG_DISPLAY SKINNED_SPRITE 
            
            struct AttributesUnlit{
                float3 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                UNITY_SKINNED_VERTEX_INPUTS 
                UNITY_VERTEX_INPUT_INSTANCE_ID
            }; 
            struct VaryingsUnlit{
                float4  positionCS      : SV_POSITION;
                float4  color           : COLOR;
                float2  uv              : TEXCOORD0;
                float3  positionWS      : TEXCOORD2; // For debug display
                UNITY_VERTEX_OUTPUT_STEREO
            }; 
            
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex); 
            
            CBUFFER_START(UnityPerMaterial)
                half4 _VertexTint;
            CBUFFER_END 
            
            VaryingsUnlit UnlitVertex(AttributesUnlit v_in){
                VaryingsUnlit v_out = (VaryingsUnlit)0;
                UNITY_SETUP_INSTANCE_ID(v_in);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(v_out);
                UNITY_SKINNED_VERTEX_COMPUTE(v_in);
                v_in.positionOS = UnityFlipSprite(v_in.positionOS, unity_SpriteProps.xy);
                v_out.positionCS = TransformObjectToHClip(v_in.positionOS);
                v_out.positionWS = TransformObjectToWorld(v_in.positionOS);
                v_out.uv = v_in.uv;
                v_out.color = v_in.color * _VertexTint * unity_SpriteColor;
                return v_out;
            } 
            
            float4 UnlitFragment(VaryingsUnlit i) : SV_Target {
                float4 mainTex = i.color * SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv);
                #if defined(DEBUG_DISPLAY) 
                SurfaceData2D surfaceData; InputData2D inputData; half4 debugColor=0;
                InitializeSurfaceData(mainTex.rgb,mainTex.a,surfaceData);
                InitializeInputData(i.uv,inputData); // No lighting UV in unlit
                SETUP_DEBUG_TEXTURE_DATA_2D_NO_TS(inputData,i.positionWS,i.positionCS,_MainTex);
                if(CanDebugOverrideOutputColor(surfaceData,inputData,debugColor)){return debugColor;}
                #endif 
                return mainTex;
            } 
            ENDHLSL 
        }
    }
    FallBack "Universal Render Pipeline/2D/Sprite-Lit-Default"
}