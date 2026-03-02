Shader "Project/Sprites/Sprite_alpha_Luna"
{
    Properties
    {
        [MainTexture] _MainTex("MainTex", 2D) = "white" {}
        _occlusionMask("Occlusion Mask", 2D) = "white" {}
        _colorOffset("Color Offset", Float) = 0
        _colorCount("Color Count", Float) = 1
        _maskPower("Mask Power", Float) = 1
        _power("Power", Float) = 1
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
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_occlusionMask);
            SAMPLER(sampler_occlusionMask);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float _colorOffset;
                float _colorCount;
                float _maskPower;
                float _power;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float4 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 uv0 : TEXCOORD0;
                float4 uv1 : TEXCOORD1;
                float4 color : COLOR;
            };

            Varyings vert(Attributes v)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.uv0 = v.uv0;
                o.uv1 = v.uv1;
                o.color = v.color * _Color;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float2 uvMain = i.uv0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
                half4 texMain = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvMain);

                // Some fire atlases keep shape in R while A can be fully white.
                // Use the tighter mask to avoid rectangular particles.
                half alphaMask = min(texMain.a, texMain.r);
                half alpha = alphaMask * i.color.a;
                clip(alpha - 1e-5h);

                half safeColorCount = max(_colorCount, 1e-5h);
                half baseMask = max(pow(saturate(texMain.r), _maskPower), 1.0h - i.color.r);
                half2 paletteUv;
                paletteUv.x = (i.uv1.x + (_colorOffset + 0.5h)) / safeColorCount;
                paletteUv.y = 0.03h + baseMask * 0.9h;

                half3 palette = SAMPLE_TEXTURE2D(_occlusionMask, sampler_occlusionMask, paletteUv).rgb;
                half luma = dot(palette, half3(0.2126729h, 0.7151522h, 0.0721750h));
                half3 colorized = lerp(luma.xxx, palette, _power);
                colorized = lerp(half3(0.21763764h, 0.21763764h, 0.21763764h), colorized, _power);

                half3 rgb = colorized * i.color.rgb;

                return half4(rgb, alpha);
            }
            ENDHLSL
        }
    }
}
