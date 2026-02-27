Shader "Core/Hyper Lighting Vertical Fog"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _LightDirection("Light Direction", Vector) = (0.25,1,-0.25,1)
        _Brightness("brightness", Range(0,1)) = 0.4
        _shadow("shadow strength", Range(0,1)) = 0.4
        _Color ("Main Color", Color) = (1,1,1,1)
        _FogColor ("Fog Color", Color) = (0, 0, 0, 0)
        _MinDistance ("Min Distance", Float) = 100
        _MaxDistance ("Max Distance", Float) = 1000
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"  }
        LOD 100

        //Blend SrcAlpha OneMinusSrcAlpha 

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal: NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 worldPos : SV_Target;
                float3 normalDir : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            uniform float4 _Color;

            float _shadow;
            float _Brightness;
            fixed4 _LightDirection;

            float _MaxDistance;
            float _MinDistance;
    
            float4 _FogColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul (unity_ObjectToWorld, v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                float4x4 modelMatrixInverse = unity_WorldToObject;
                o.normalDir = normalize(mul(float4(v.normal, 0.0), modelMatrixInverse).xyz);

                UNITY_TRANSFER_FOG(o,o.vertex);

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
              

                fixed4 col = tex2D(_MainTex, i.uv);
                //col *= _Color;
                
                half3 th = normalize(half3(_LightDirection.x, _LightDirection.y, _LightDirection.z));
                // Fake lighting
                float lightVal = dot(i.normalDir, th);

                col.rgb = col.rgb * _Color * max(lerp(1.5, 0, _shadow), lightVal) + lightVal * _Brightness;

                half4 dist = i.worldPos.y;

                half4 weight =  (dist - _MinDistance) / (_MaxDistance - _MinDistance);

                half4 distanceColor = lerp(col, _FogColor, saturate(weight));

                col = distanceColor;



                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}