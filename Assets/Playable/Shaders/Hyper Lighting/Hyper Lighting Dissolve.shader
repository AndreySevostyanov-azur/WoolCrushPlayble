Shader "Core/Hyper Lighting Dissolve"
{
  Properties
  {
    _DissolveTexture("Dissolve Texutre", 2D) = "white" {}

    _Amount("Amount", Range(0,2)) = 0
    _DissolveScale("Dissolve Scale", Range(0,10)) = 0

    _LineSize("Line Size", Range(0,1)) = 0
    _LineColor("Line Color", Color) = (1,1,1,1)

    _LightDirection ("Light Direction", Vector) = (0.25,1,-0.25,1)
    _Color ("Color", Color) = (1,1,1,1)
    _Brightness ("brightness", Range(0,1)) = 0.4
    _shadow ("shadow strength", Range(0,1)) = 0.4
    _MainTex ("Albedo (RGB)", 2D) = "white" {}
  }

  SubShader
  {
    Tags { "Queue" = "Transparent" "RenderType" = "Transparent Cutout" }
    LOD 200

    Blend SrcAlpha OneMinusSrcAlpha 
    //ZWrite Off

    Pass
    {

      CGPROGRAM

      // Define name of vertex shader
      #pragma vertex vert
      // Define name of fragment shader
      #pragma fragment frag

      // Include some common helper functions, such as UnityObjectToClipPos
       #pragma multi_compile_fog
      #include "UnityCG.cginc"

        float4 _Color;
        float4 _LineColor;
        float _shadow;
        float _Brightness;

        // Color Diffuse Map
        sampler2D _MainTex;
        sampler2D _DissolveTexture;
        // Tiling/Offset for _MainTex, used by TRANSFORM_TEX in vertex shader
        float4 _MainTex_ST;

      fixed4 _LightDirection;

      half _DissolveTransition;
      half _Amount;
      half _LineSize;
      half _DissolveScale;

      // This is the vertex shader input: position, UV0, UV1, normal
      struct appdata
      {
        float4 vertex   : POSITION;
        float2 texcoord : TEXCOORD0;
        float3 normal: NORMAL;
      };

      // This is the data passed from the vertex to fragment shader
      struct v2f
      {
        float4 pos  : SV_POSITION;
        float2 txuv : TEXCOORD0;
         UNITY_FOG_COORDS(1)
        float3 normalDir : TEXCOORD2;
         float3 worldPos : TEXCOORD3;
      };

      // This is the vertex shader
      v2f vert(appdata v)
      {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.txuv = TRANSFORM_TEX(v.texcoord.xy,_MainTex);

        float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
        o.worldPos = worldPos.xyz;
        // Calculating normal so it can be used for fake lighting
        // in the fragment shader
        float4x4 modelMatrixInverse = unity_WorldToObject;
        o.normalDir = normalize(mul(float4(v.normal, 0.0), modelMatrixInverse).xyz);

        UNITY_TRANSFER_FOG(o,o.pos);
        return o;
      }

      // This is the fragment shader
      half4 frag(v2f i) : COLOR
      {
        // Reading color from diffuse texture
        half4 col = tex2D(_MainTex, i.txuv.xy);

        float2 uv_front = TRANSFORM_TEX(i.worldPos.xy, _MainTex);
        float2 uv_side = TRANSFORM_TEX(i.worldPos.zy, _MainTex);
        float2 uv_top = TRANSFORM_TEX(i.worldPos.xz, _MainTex);

        half4 dissolve_col_front = tex2D(_DissolveTexture, frac(uv_front * _DissolveScale));
        half4 dissolve_col_side = tex2D(_DissolveTexture, frac(uv_side * _DissolveScale));
        half4 dissolve_col_top = tex2D(_DissolveTexture, frac(uv_top * _DissolveScale));


        float3 weights = i.normalDir;
        weights = abs(weights);

        dissolve_col_front *= weights.z;
        dissolve_col_side *= weights.x;
        dissolve_col_top *= weights.y;

        half4 disolveColor = dissolve_col_front + dissolve_col_side + dissolve_col_top;
        half dissolve_value = disolveColor.r;//tex2D(_DissolveTexture, frac(screenUV * _DissolveScale)).r;


        clip(dissolve_value  - _Amount );

        if (dissolve_value > _Amount)
        {
            if (dissolve_value - _LineSize < _Amount)
            {
                col = _LineColor;
            }
        }
  

        // Using hard-coded light direction for fake lighting
        half3 th = normalize(half3(_LightDirection.x, _LightDirection.y, _LightDirection.z));
        // Fake lighting
        float lightVal = dot (i.normalDir, th);

        // Add in a general brightness (similar to ambient/gamma) and then
        // calculate the final color of the pixel
        //col.rgb = col.rgb * _Color + lightVal * _Brightness;
        col.rgb = col.rgb * _Color * max(lerp(1.5,0,_shadow),lightVal) + lightVal * _Brightness;//- _Brightness;//_IlluminationBright * globalVal;//;max(_IlluminationBright,lightVal);

        UNITY_APPLY_FOG(i.fogCoord, col);
        return col;
      }

      ENDCG
    }
  }
}