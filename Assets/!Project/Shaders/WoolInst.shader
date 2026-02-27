Shader "Wool/Wool_Instanced"
{
    Properties
    {
        [Header(Color)]
        [Space]
        [NoScaleOffset] _MainTex ("Color", 2D) = "white" {}
        _ColorCount ("Color Count", float) = 1
        _ColorOffset ("Color Offset", float) = 0
        [Header(Light)]
        [Space]
        _LightDirection ("Light Direction", Vector) = (0,0,0,0)
        [Gamma]_ShadowColor ("Shadow Color", Color) = (1,1,1,1)
        [Header(Occlusion)]
        [Space]
        _OcclusionTex ("Occlusion Mask.r", 2D) = "white" {}

        [Header(Cutout)]
        [Space]
        _cutout ("Cutout", range(0,1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "AlphaTest"
        }

        Pass
        {
            Name "Wool Unlit"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Back
            ZWrite On
            ZTest LEqual // Less Greater LEqual GEqual Equal NotEqual Always Never
            AlphaToMask On

            HLSLPROGRAM
            #pragma prefer_hlslcc gles gles3
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0
            
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
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
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
                half _ColorCount;
                float _ColorOffset;
                half4 _ShadowColor;
                float4 _LightDirection;
                float4 _OcclusionTex_ST;
                float _cutout;
            CBUFFER_END

            // Для стандартного GPU инстансинга
            UNITY_INSTANCING_BUFFER_START(PerInstance)
                UNITY_DEFINE_INSTANCED_PROP(float, _ColorOffsetInst)
                UNITY_DEFINE_INSTANCED_PROP(float, _cutoutInst)
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
                float instanceColorOffset = _ColorOffset;
                
                #if defined(UNITY_INSTANCING_ENABLED)
                    instanceColorOffset = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _ColorOffsetInst);
                #endif
                
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv * _OcclusionTex_ST.xy + _OcclusionTex_ST.zw;
                OUT.uv1 = IN.uv1 * float2(1, 1) + float2(instanceColorOffset / _ColorCount, 0);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normal);
                OUT.vertexMask = IN.color.r;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                
                float instanceCutout = _cutout;
                #if defined(UNITY_INSTANCING_ENABLED)
                    instanceCutout = UNITY_ACCESS_INSTANCED_PROP(PerInstance, _cutoutInst);
                #endif



                half occlusionMask = SAMPLE_TEXTURE2D(_OcclusionTex, sampler_OcclusionTex, IN.uv);
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex_linear_clamp, 
                    float2(IN.uv1.x, max(1 - IN.vertexMask, occlusionMask)));

                texColor.a = step(IN.uv1.y, instanceCutout);
                clip(texColor.a - 0.5);

                half shadowMask = dot(IN.normalWS, normalize(_LightDirection.xyz));
                shadowMask = saturate(shadowMask);
                half combineMask = shadowMask* (1-_LightDirection.w) + occlusionMask*_LightDirection.w;


                half4 finalColor = lerp(texColor * _ShadowColor, texColor, combineMask);
                finalColor.a =1.0;

                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}