// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Core/Hyper Lighting Frost"
{
  Properties
  {
    _FrostTexture("Frost Texture", 2D) = "white" {}
    _FrostIntensity("Frost Intensity", Range(0.1,1.5)) = 1
    _FrostScale("Frost Scale", Range(0,10)) = 0

    _DissolveTexture("Dissolve Texture", 2D) = "white" {}
    _DissolveAmount("Dissolve Amount", Range(0,2)) = 0
    _DissolveScale("Dissolve Scale", Range(0,10)) = 0
 

    [MaterialToggle] _DissolveTransition("Hard Edges", Range(0,1)) = 0

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
    Tags { "RenderType" = "Opaque" }
    LOD 200

    //Blend SrcAlpha OneMinusSrcAlpha 
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
      sampler2D _FrostTexture;
      // Tiling/Offset for _MainTex, used by TRANSFORM_TEX in vertex shader
      float4 _MainTex_ST;
      float4 _FrostTexture_ST;

      float _FrostIntensity;

      fixed4 _LightDirection;
      sampler2D _DissolveTexture;

      half _DissolveTransition;
      half _DissolveAmount;
      half _LineSize;
      half _FrostScale;
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

        half3 objNormal : TEXCOORD3;
        float3 frostCoords : TEXCOORD4;
        float3 disolveCoords : TEXCOORD5;
      };

      // This is the vertex shader
      v2f vert(appdata v)
      {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.txuv = TRANSFORM_TEX(v.texcoord.xy,_MainTex);
        // Calculating normal so it can be used for fake lighting
        // in the fragment shader
        o.normalDir = normalize(mul(float4(v.normal, 0.0), unity_WorldToObject).xyz);

        // triplanar UV for frost
        o.objNormal = v.normal;
        o.frostCoords = v.vertex.xyz * _FrostScale;
        o.disolveCoords = v.vertex.xyz * _DissolveScale;

        UNITY_TRANSFER_FOG(o, o.pos);
    
        return o;
      }

      // This is the fragment shader
      half4 frag(v2f i) : COLOR
      {
    
        half3 blend = abs(i.objNormal);
        // make sure the weights sum up to 1 (divide by sum of x+y+z)
        blend /= dot(blend,1.0);
        // read the three texture projections, for x,y,z axes
        fixed4 cx = tex2D(_FrostTexture, i.frostCoords.yz);
        fixed4 cy = tex2D(_FrostTexture, i.frostCoords.xz);
        fixed4 cz = tex2D(_FrostTexture, i.frostCoords.xy);
        // blend the textures based on weights
   
        half4 frostColor = (cx * blend.x + cy * blend.y + cz * blend.z) * _FrostIntensity;

        cx = tex2D(_DissolveTexture, i.disolveCoords.yz);
        cy = tex2D(_DissolveTexture, i.disolveCoords.xz);
        cz = tex2D(_DissolveTexture, i.disolveCoords.xy);

        half4 disolveColor = cx * blend.x + cy * blend.y + cz * blend.z;
        half dissolve_value = disolveColor.r;//tex2D(_DissolveTexture, frac(screenUV * _DissolveScale)).r;
 
        half4 col;

        if (_DissolveTransition == 0)
        {
            col = lerp(tex2D(_MainTex, i.txuv.xy), lerp(tex2D(_MainTex, i.txuv.xy), frostColor, frostColor.a), saturate(_DissolveAmount - dissolve_value));
        }
        else
        {
            if (dissolve_value > _DissolveAmount)
            {
                col = tex2D(_MainTex, i.txuv.xy);

            }
            else
            {
                if (dissolve_value + _LineSize < _DissolveAmount )
                {
                    col = lerp(tex2D(_MainTex, i.txuv.xy), frostColor, frostColor.a);
                }
                else
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