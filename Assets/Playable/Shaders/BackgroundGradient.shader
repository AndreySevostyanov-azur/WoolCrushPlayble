Shader "AzurMarketing/BackgroundGradient" {
  Properties {
    _ColorTop ("Top Color", Color) = (1,1,1,1)
    _ColorTopOffset ("Top Color Offset", Range(0,1)) = 0
    _ColorBot ("Bottom Color", Color) = (1,1,1,1)
    _ColorBotOffset ("Bottom Color Offset", Range(0, 1)) = 0
  }
  
  SubShader {
    Tags {"Queue"="Background" "IgnoreProjector"="True"}
    Cull Off ZWrite Off
    
    Pass {
      CGPROGRAM
      #pragma vertex vert  
      #pragma fragment frag
      #include "UnityCG.cginc"
      
      fixed4 _ColorTop;
      fixed4 _ColorBot;
      float _ColorBotOffset;
      float _ColorTopOffset;
      
      struct v2f {
        fixed4 pos : SV_POSITION;
        fixed4 texcoord : TEXCOORD0;
      };
      
      v2f vert (appdata_full v) {
        v2f o;
        o.pos = UnityObjectToClipPos (v.vertex);
        o.texcoord = v.texcoord;
        return o;
      }

      float remap(float value, float from1, float to1, float from2, float to2)
      {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
      }
      
      fixed4 frag (v2f i) : COLOR {
        fixed4 color;
        fixed coordinate = i.texcoord.y;
        if(coordinate < _ColorBotOffset)
        {
          color = _ColorBot;
        }
        else if(coordinate > (1 - _ColorTopOffset))
        {
          color = _ColorTop;
        }
        else
        {
          float l = remap(coordinate, 0, 1, _ColorBotOffset, 1 - _ColorTopOffset);
          l = remap(coordinate, _ColorBotOffset, 1 - _ColorTopOffset, 0, 1);
          color = lerp(_ColorBot, _ColorTop, l);
        }
        return color;
      }

      ENDCG
    }
  }
}
