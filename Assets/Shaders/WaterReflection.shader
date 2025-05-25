// FILE: Assets/Shaders/WaterReflection.shader
Shader "Sprites/WaterReflection"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ReflectionTex ("Reflection Texture", 2D) = "black" {}
        _ReflectionIntensity ("Reflection Intensity", Range(0, 1)) = 0.7
        _ReflectionTint ("Reflection Tint", Color) = (0.8, 0.9, 1.0, 1.0)
        _ReflectionOffsetY ("Reflection Offset Y", Float) = 0.1
        _RippleStrength ("Ripple Strength", Range(0, 0.1)) = 0.02
        _RippleSpeed ("Ripple Speed", Range(0.1, 10)) = 2
        _EnableRipples ("Enable Ripples", Float) = 1
        
        // Sprite Renderer Props
        _EnableExternalAlpha ("Enable External Alpha", Float) = 0
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

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _ReflectionTex;
            sampler2D _AlphaTex;
            
            fixed4 _Color;
            fixed4 _ReflectionTint;
            float _ReflectionIntensity;
            float _ReflectionOffsetY;
            float _RippleStrength;
            float _RippleSpeed;
            float _EnableRipples;
            float _EnableExternalAlpha;
            
            float4 _MainTex_ST;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                o.screenPos = ComputeScreenPos(o.vertex);
                
                #ifdef PIXELSNAP_ON
                o.vertex = UnityPixelSnap(o.vertex);
                #endif

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample the water tile texture
                fixed4 waterColor = tex2D(_MainTex, i.texcoord) * i.color;
                
                // Early out if water tile is transparent
                if (waterColor.a < 0.01)
                    return waterColor;
                
                // Calculate screen UV for reflection sampling
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
                
                // Apply vertical offset
                screenUV.y += _ReflectionOffsetY;
                
                // Add ripple distortion if enabled
                if (_EnableRipples > 0.5)
                {
                    float2 rippleOffset = float2(0, 0);
                    
                    // Create animated ripples using world position
                    float ripplePhase = _Time.y * _RippleSpeed;
                    rippleOffset.x = sin(i.worldPos.y * 3.0 + ripplePhase) * _RippleStrength;
                    rippleOffset.y = cos(i.worldPos.x * 3.0 + ripplePhase * 0.7) * _RippleStrength;
                    
                    screenUV += rippleOffset;
                }
                
                // Clamp UV to avoid sampling outside texture
                screenUV = saturate(screenUV);
                
                // Sample reflection texture
                fixed4 reflection = tex2D(_ReflectionTex, screenUV);
                
                // Apply reflection tint
                reflection.rgb *= _ReflectionTint.rgb;
                
                // Blend water and reflection
                fixed3 finalColor = lerp(waterColor.rgb, reflection.rgb, 
                                        _ReflectionIntensity * reflection.a * waterColor.a);
                
                // Handle external alpha if needed
                fixed4 c = fixed4(finalColor, waterColor.a);
                c.rgb *= c.a;
                
                #if ETC1_EXTERNAL_ALPHA
                if (_EnableExternalAlpha > 0.5)
                {
                    fixed4 alpha = tex2D(_AlphaTex, i.texcoord);
                    c.a = lerp(c.a, alpha.r, _EnableExternalAlpha);
                }
                #endif
                
                return c;
            }
            ENDCG
        }
    }
}