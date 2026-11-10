Shader "Wool/Ice Tile"
{
    Properties
    {
        _MainTex ("Color", 2D) = "white" {}
        [Space]
        _Alpha ("Alpha", range(0,1)) = 1
        _NoiseScale ("Noise Scale",float) = 1
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
            ZWrite On
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _Alpha;
                float _NoiseScale;
            CBUFFER_END

            #ifdef UNITY_DOTS_INSTANCING_ENABLED
                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float, _Alpha)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)

                #define _Alpha UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _Alpha)
            #endif

            UNITY_INSTANCING_BUFFER_START(PerInstance)
                UNITY_DEFINE_INSTANCED_PROP(float, _AlphaInst)
            UNITY_INSTANCING_BUFFER_END(PerInstance)

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float Hash(float3 position)
            {
                return frac(sin(dot(position, float3(127.1, 311.7, 74.7))) * 43758.5453);
            }

            float ValueNoise(float3 position)
            {
                float3 cell = floor(position);
                float3 blend = frac(position);
                blend = blend * blend * (3.0 - 2.0 * blend);

                float x00 = lerp(Hash(cell), Hash(cell + float3(1, 0, 0)), blend.x);
                float x10 = lerp(Hash(cell + float3(0, 1, 0)), Hash(cell + float3(1, 1, 0)), blend.x);
                float x01 = lerp(Hash(cell + float3(0, 0, 1)), Hash(cell + float3(1, 0, 1)), blend.x);
                float x11 = lerp(Hash(cell + float3(0, 1, 1)), Hash(cell + float3(1, 1, 1)), blend.x);

                return lerp(lerp(x00, x10, blend.y), lerp(x01, x11, blend.y), blend.z);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                OUT.positionOS = IN.positionOS.xyz;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float instanceAlpha = _Alpha;

                #if defined(UNITY_DOTS_INSTANCING_ENABLED)
                    instanceAlpha = _Alpha;
                #elif defined(UNITY_INSTANCING_ENABLED)
                    instanceAlpha = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _AlphaInst);
                #endif

                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                if (instanceAlpha < 1.0)
                {
                    float noise = ValueNoise(IN.positionOS * _NoiseScale);
                    texColor.a *= step(1.0 - instanceAlpha, noise);
                }

                return texColor;
            }

            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}