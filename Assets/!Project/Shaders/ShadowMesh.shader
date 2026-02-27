Shader "Wool/ShadowMesh"
{
    Properties
    {
        _baseColor ("Base Color", Color) = (0,0,0,1)
        _alpha ("Alpha", Range(0,1)) = 0.5
        _shadow ("Shadow", Range(0,1)) = 1
        _Brightness ("Brightness", Range(0,2)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "ShadowMesh Unlit"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Back
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma prefer_hlslcc gles gles3
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _baseColor;
                half _alpha;
                half _shadow;
                half _Brightness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half3 rgb = _baseColor.rgb * _Brightness;
                // Matches previous ShaderGraph: Alpha = UV.y * _alpha
                half a = saturate(IN.uv.y * _alpha * _shadow);
                return half4(rgb, a);
            }
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Unlit"
}
