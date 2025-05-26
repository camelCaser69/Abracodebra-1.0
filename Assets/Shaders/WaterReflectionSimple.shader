// FILE: Assets/Shaders/WaterReflectionGradient.shader
Shader "Custom/WaterReflectionGradient"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color      ("Tint", Color) = (1,1,1,1)

        [Header(Gradient Fade)]
        _FadeStart  ("Fade Start Y", Float)             = 0.0
        _FadeEnd    ("Fade End Y",   Float)             = 1.0
        _MinAlpha   ("Minimum Alpha", Range(0,1))       = 0.0
        _OriginalY  ("Original Object Y Position", Float) = 0.0

        [Header(Sprite Mask)]
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        _StencilComp     ("Stencil Comparison", Float) = 8
        _Stencil         ("Stencil ID",         Float) = 0
        _StencilOp       ("Stencil Operation",  Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask",  Float) = 255
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref       [_Stencil]
            Comp      [_StencilComp]
            Pass      [_StencilOp]
            ReadMask  [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID        // keeps SRP batching happy
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                float  worldY        : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO            // replacement for the removed macro
            };

            sampler2D _MainTex;
            fixed4   _Color;
            float    _FadeStart;
            float    _FadeEnd;
            float    _MinAlpha;
            float    _OriginalY;
            fixed4   _TextureSampleAdd;
            float4   _ClipRect;
            float4   _MainTex_ST;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);     // copy instance ID to the output
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPosition = v.vertex;
                o.vertex        = UnityObjectToClipPos(o.worldPosition);
                o.texcoord      = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color         = v.color * _Color;

                // world-space Y for the gradient fade
                float4 wp = mul(unity_ObjectToWorld, v.vertex);
                o.worldY  = wp.y;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                half4 col = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;

                // vertical-distance fade
                float d = abs(i.worldY - _OriginalY);
                float a = 1.0;

                if (d > _FadeStart)
                {
                    a = (d >= _FadeEnd) ? _MinAlpha
                                        : lerp(1.0, _MinAlpha, (d - _FadeStart) / (_FadeEnd - _FadeStart));
                }
                col.a *= a;

                #ifdef UNITY_UI_CLIP_RECT
                    col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                    clip(col.a - 0.001);
                #endif

                return col;
            }
            ENDCG
        }
    }
}
