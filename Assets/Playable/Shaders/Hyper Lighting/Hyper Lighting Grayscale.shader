Shader "Core/Hyper Lighting Grayscale"
{
  Properties
  {
    _LightDirection ("Light Direction", Vector) = (0.25,1,-0.25,1)
    _Color ("Color", Color) = (1,1,1,1)
    _Brightness ("brightness", Range(0,1)) = 0.4
    _shadow ("shadow strength", Range(0,1)) = 0.4
    _Grayscale("Grayscale", Range(0,1)) = 0
    _MainTex ("Albedo (RGB)", 2D) = "white" {}
  }

  SubShader
  {
    Tags { "RenderType" = "Opaque" }
    LOD 200

    Pass
    {

      CGPROGRAM

      #pragma vertex vert
      #pragma fragment frag
      #pragma multi_compile_fog
      #include "UnityCG.cginc"

      float4 _Color;
      float _shadow;
      float _Brightness;
      float _Grayscale;
      sampler2D _MainTex;
      float4 _MainTex_ST;
      
      
      fixed4 _LightDirection;

      struct appdata
      {
        float4 vertex   : POSITION;
        float2 texcoord : TEXCOORD0;
        float3 normal: NORMAL;
      };

      struct v2f
      {
        float4 pos  : SV_POSITION;
        float2 txuv : TEXCOORD0;
         UNITY_FOG_COORDS(1)
        float3 normalDir : TEXCOORD2;
      };

      v2f vert(appdata v)
      {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.txuv = TRANSFORM_TEX(v.texcoord.xy,_MainTex);

        float4x4 modelMatrixInverse = unity_WorldToObject;
        o.normalDir = normalize(mul(float4(v.normal, 0.0), modelMatrixInverse).xyz);

        UNITY_TRANSFER_FOG(o,o.pos);
        return o;
      }

      half4 frag(v2f i) : COLOR
      {
        half4 col = tex2D(_MainTex, i.txuv.xy);

        half3 th = normalize(half3(_LightDirection.x, _LightDirection.y, _LightDirection.z));
        float lightVal = dot (i.normalDir, th);

        col.rgb = col.rgb * _Color * max(lerp(1.5,0,_shadow), lightVal) + lightVal * _Brightness;

        half grayscale = dot(col.rgb, float3(0.299, 0.587, 0.114));
        col = lerp(half4(col.rgb, col.a), half4(grayscale, grayscale, grayscale, col.a), _Grayscale);

        UNITY_APPLY_FOG(i.fogCoord, col);
        return col;
      }

      ENDCG
    }
  }
}