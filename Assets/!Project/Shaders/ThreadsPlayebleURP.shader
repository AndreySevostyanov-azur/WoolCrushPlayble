Shader "Shader Graphs/ThreadsPlayeble/UniversalForward/_"
{
    Properties
    {
        _MainTex ("MainTex", 2D) = "white" {}
        _occlusionMask ("occlusionMask", 2D) = "white" {}
        _colorOffset ("colorOffset", Float) = 0
        _colorCount ("colorCount", Float) = 9
        _maskPower ("maskPower", Float) = 1
        _power ("power", Float) = 1
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Overlay"
            "RenderType"="Transparent"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_occlusionMask);
            SAMPLER(sampler_occlusionMask);

            CBUFFER_START(UnityPerMaterial)
                float _colorOffset;
                float _colorCount;
                float _maskPower;
                float _power;
                float _Cutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            static float2 ApplyColorOffsetUV(float2 uv, float colorOffset, float colorCount)
            {
                float c = max(1.0, colorCount);
                float o = clamp(colorOffset, 0.0, c - 1.0);
                // Atlas is laid out by X (see WoolInst.shader pattern).
                uv.x = (uv.x / c) + (o / c);
                return uv;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = ApplyColorOffsetUV(IN.uv, _colorOffset, _colorCount);

                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                half4 occ = SAMPLE_TEXTURE2D(_occlusionMask, sampler_occlusionMask, IN.uv);

                // WebGL-safe: keep texture RGB; use vertex alpha + occlusion for transparency.
                col.a *= IN.color.a;
                col.a *= occ.r;

                float a = saturate(col.a);
                a = pow(max(0.0, a), max(0.0001, _power));
                a = saturate(a * _maskPower);
                col.a = a;

                clip(col.a - _Cutoff);
                return col;
            }
            ENDHLSL
        }
    }
}

