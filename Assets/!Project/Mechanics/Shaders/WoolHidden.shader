Shader "Wool/Wool_Hidden"
{
    Properties
    {
        [NoScaleOffset] _MainTex ("Color", 2D) = "white" {}
        _Alpha ("Tex Alpha", range(0,1)) = 1
        _RainbowAlpha ("Rainbow Alpha", Range(0, 1)) = 0.5

        [Space]
        _LightDirection ("Light Direction", Vector) = (0,0,0,0)
        [Gamma]_ShadowColor ("Shadow Color", Color) = (1,1,1,1)

        [Space]
        _OcclusionTex ("Occlusion Mask.r", 2D) = "white" {}

        [Header(Rainbow Effect)]
        [Space]
        _NoiseSpeed ("Noise Speed (X, Y, W - scale)", Vector) = (0.5, 0.5, 0, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "Wool Unlit"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma prefer_hlslcc gles gles3
            #pragma exclude_renderers d3d11_9x

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing

            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float3 normal : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 uv1 : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                half vertexMask : TEXCOORD3;
                float3 positionWS : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _ShadowColor;
                float4 _LightDirection;
                float4 _OcclusionTex_ST;
                float _Alpha;
                float4 _NoiseSpeed;
                float _RainbowAlpha;
            CBUFFER_END

            #ifdef UNITY_DOTS_INSTANCING_ENABLED
                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float, _Alpha)
                    UNITY_DOTS_INSTANCED_PROP(float, _RainbowAlpha)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

                #define _Alpha UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _Alpha)
                #define _RainbowAlpha UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _RainbowAlpha)
            #endif

            UNITY_INSTANCING_BUFFER_START(PerInstance)
                UNITY_DEFINE_INSTANCED_PROP(float, _AlphaInst)
                UNITY_DEFINE_INSTANCED_PROP(float, _RainbowAlphaInst)
            UNITY_INSTANCING_BUFFER_END(PerInstance)

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex_linear_clamp);

            TEXTURE2D(_OcclusionTex);
            SAMPLER(sampler_OcclusionTex);

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);

                OUT.uv = IN.uv * _OcclusionTex_ST.xy + _OcclusionTex_ST.zw;
                OUT.uv1 = IN.uv1;

                OUT.normalWS = TransformObjectToWorldNormal(IN.normal);
                OUT.vertexMask = IN.color.r;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float instanceAlpha = _Alpha;
                float instanceRainbowAlpha = _RainbowAlpha;

                #if defined(UNITY_DOTS_INSTANCING_ENABLED)
                    instanceAlpha = _Alpha;
                    instanceRainbowAlpha = _RainbowAlpha;
                #elif defined(UNITY_INSTANCING_ENABLED)
                    instanceAlpha = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _AlphaInst);
                    instanceRainbowAlpha = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _RainbowAlphaInst);
                #endif

                float occlusionMask = SAMPLE_TEXTURE2D(_OcclusionTex, sampler_OcclusionTex, IN.uv).r;

                float2 texUV = float2(IN.uv1.x, max(1 - IN.vertexMask, occlusionMask));

                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex_linear_clamp, texUV);

                float shadowMask = dot(IN.normalWS, normalize(_LightDirection.xyz));
                shadowMask = saturate(shadowMask);

                float combineMask = shadowMask * (1 - _LightDirection.w) + occlusionMask * _LightDirection.w;

                float4 finalColor = lerp(texColor * _ShadowColor, texColor, combineMask);

                float2 patternUV = IN.positionWS.xz * _NoiseSpeed.w + _Time.y * _NoiseSpeed.xy;

                float2 speedDir = length(_NoiseSpeed.xy) > 0.001 ? normalize(_NoiseSpeed.xy) : float2(1.0, 0.0);
                float linearCoord = dot(patternUV, speedDir);
                float patternVal = sin(linearCoord * 3.14159) * 0.5 + 0.5;

                float hue = (IN.positionWS.x * 0.2 + IN.positionWS.z * 0.2) + patternVal;
                float3 rainbowColor = 0.5 + 0.5 * cos(6.28318 * (hue + float3(0.0, 0.333, 0.667)));

                finalColor.rgb += (rainbowColor * instanceRainbowAlpha);

                finalColor.a = instanceAlpha;
                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}