Shader "Project/Sprites/Sprite_add_Luna"
{
    Properties
    {
        [MainTexture] _MainTex("MainTex", 2D) = "white" {}
        [MainColor] _Color("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite Off
            Blend SrcAlpha One

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float4 uv0 : TEXCOORD0;
                float4 uv2 : TEXCOORD2;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uv0 : TEXCOORD0;
                float4 uv2 : TEXCOORD1;
                float4 color : COLOR;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv0 = v.uv0;
                o.uv2 = v.uv2;
                o.color = v.color * _Color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.uv0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
                half mainMask = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).r;

                half alpha = mainMask * i.color.a;
                clip(alpha - 1e-5h);

                half3 colorMix = i.uv0.z * i.color.rgb + i.color.rgb - i.uv2.xyz;
                half3 rgb = (mainMask * colorMix + i.uv2.xyz) * i.color.rgb;

                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
