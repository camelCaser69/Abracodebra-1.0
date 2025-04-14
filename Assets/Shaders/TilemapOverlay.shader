// FILE: Assets/Shaders/TilemapOverlay.shader (Complete - No _Color Property)
Shader "Custom/TilemapOverlay"
{
    Properties
    {
        // _Color property removed

        [PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {} // Base texture from TilemapRenderer

        // --- Overlay Properties ---
        [LocalKeyword(_USEOVERLAY_ON)] _UseOverlay("Use Overlay", Float) = 0
        [NoScaleOffset] _OverlayTex("Overlay Texture (Set Wrap Mode!)", 2D) = "white" {}
        _OverlayColor("Overlay Color", Color) = (1,1,1,1) // Tint specifically for the overlay
        _OverlayScaleValue("Overlay Scale (World Units per Texture Tile)", Float) = 1.0
        _OverlayOffset("Overlay Offset (World Units)", Vector) = (0,0,0,0)

        // --- Animation Properties ---
        [LocalKeyword(_USEANIMATION_ON)] _UseAnimation("Use Animation", Float) = 0
        _AnimSpeed("Animation Speed (Frames/Sec)", Float) = 1.0
        _AnimTiles("Animation Tiles (X count)", Float) = 1

        // --- Blending Properties ---
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 5 // SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 10 // OneMinusSrcAlpha

        // --- Standard Tilemap/Sprite properties (usually hidden) ---
         [HideInInspector] _StencilComp("Stencil Comparison", Float) = 8
         [HideInInspector] _Stencil("Stencil ID", Float) = 0
         [HideInInspector] _StencilOp("Stencil Operation", Float) = 0
         [HideInInspector] _StencilWriteMask("Stencil Write Mask", Float) = 255
         [HideInInspector] _StencilReadMask("Stencil Read Mask", Float) = 255
         [HideInInspector] _ColorMask("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
            "IgnoreProjector" = "False"
            "ForceNoShadowCasting" = "True"
            "DisableBatching" = "False"
        }

        Stencil
        {
            Ref[_Stencil]
            Comp[_StencilComp]
            Pass[_StencilOp]
            ReadMask[_StencilReadMask]
            WriteMask[_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest LEqual
        Blend[_SrcBlend][_DstBlend]
        ColorMask[_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #pragma multi_compile_local _ _USEOVERLAY_ON
            #pragma multi_compile_local _ _USEANIMATION_ON

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR; // Vertex Color (includes Tilemap.color)
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float4 color    : COLOR;        // Pass Vertex Color * Tilemap.color through
                float2 texcoord : TEXCOORD0;    // UV for _MainTex
                float2 worldPos : TEXCOORD1;    // World position for overlay UV calculation
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _OverlayTex;

            float4 _MainTex_ST;

            // _Color removed
            float4 _OverlayColor;
            float4 _OverlayOffset;
            float  _OverlayScaleValue;

            float  _AnimSpeed;
            float  _AnimTiles;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.vertex = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color; // Pass vertex color (including Tilemap tint) directly
                OUT.worldPos = mul(unity_ObjectToWorld, v.vertex).xy;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                // 1. Sample the main texture
                fixed4 texColor = tex2D(_MainTex, IN.texcoord);

                // 2. Calculate base color using texture and IN.color (Vertex Color * Tilemap.color)
                // NO multiplication by a shader _Color property here
                fixed4 baseColor = texColor * IN.color;

                // Default final color is the base color
                fixed4 finalColor = baseColor;

                #if defined(_USEOVERLAY_ON)

                    float2 overlayUV = (IN.worldPos / max(0.001, _OverlayScaleValue)) + _OverlayOffset.xy;

                    #if defined(_USEANIMATION_ON)
                        float timeVal = _Time.y * _AnimSpeed;
                        float frameIndex = floor(fmod(timeVal, max(1.0, _AnimTiles)));
                        float frameOffsetX = frameIndex / max(1.0, _AnimTiles);
                        overlayUV.x = overlayUV.x + frameOffsetX;
                    #endif

                    fixed4 overlaySample = tex2D(_OverlayTex, overlayUV);
                    // Apply overlay-specific tint
                    fixed4 overlayColor = overlaySample * _OverlayColor;

                    // Blend overlay RGB onto base RGB using overlay's calculated alpha
                    finalColor.rgb = lerp(baseColor.rgb, overlayColor.rgb, overlayColor.a);
                    // Set final alpha (e.g., use base alpha)
                    finalColor.a = baseColor.a;

                #endif // _USEOVERLAY_ON

                // clip(finalColor.a - 0.001); // Optional alpha clipping

                return finalColor;
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default" // Fallback might need adjustment if URP specific fallback is better
    // FallBack "Universal Render Pipeline/2D/Sprite-Unlit-Default" // Potential URP specific fallback
}