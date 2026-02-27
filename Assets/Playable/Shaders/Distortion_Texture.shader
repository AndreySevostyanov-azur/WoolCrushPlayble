// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Mobile/Particles/Distortion_Texture"
{
	Properties
	{
		[Header(Blending)]
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcFactor("Source Factor", Int) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _DstFactor("Destination Factor", Int) = 10
		[Enum(UnityEngine.Rendering.CullMode)] _Culling ("Culling", Int) = 0
		[Header(Main Settings)]
		[NoScaleOffset]_MainTex("MainTex", 2D) = "gray" {}
		_Tiling("Tiling", Vector) = (1,1,0,0)
		_MainTexScroll("MainTexScroll", Vector) = (0,0,0,0)
		[Header(Distortion)]
		[Toggle(_DISTORTIONMAINTEX_ON)] _DistortionMainTex("DistortionMainTex", Float) = 0
		_DistortionTexture("DistortionTexture", 2D) = "gray" {}
		_Speed_Distortion("Speed_Distortion", Vector) = (1,1,0,0)
		_Distortion_Amount("Distortion_Amount", Range( 0 , 1)) = 0
		[Header(Mask)]
		[Toggle(_MASKENABLE_ON)] _MaskEnable("MaskEnable", Float) = 0
		[Toggle(_DISTORTIONMASK_ON)] _DistortionMask("DistortionMask", Float) = 0
		[NoScaleOffset]_Mask("Mask", 2D) = "white" {}
		_Multiply("Multiply", Range( 0 , 2)) = 1
	}
	
	SubShader
	{
		
		
		Tags { "Queue" = "Transparent" "RenderType"="Transparent" }
		//LOD 100

		CGINCLUDE
		#pragma target 2.0
		ENDCG
		Blend [_SrcFactor] [_DstFactor]
		Cull [_Culling]
		ColorMask RGBA
		ZWrite Off
		//ZTest LEqual
		
		
		
		Pass
		{
			Name "Unlit"
			CGPROGRAM

			

			#ifndef UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX
			//only defining to not throw compilation error over Unity 5.5
			#define UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input)
			#endif
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_instancing
			#include "UnityCG.cginc"
			#include "UnityShaderVariables.cginc"
			#pragma shader_feature _MASKENABLE_ON
			#pragma shader_feature _DISTORTIONMAINTEX_ON
			#pragma shader_feature _DISTORTIONMASK_ON


			struct appdata
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				float4 ase_texcoord : TEXCOORD0;
			};
			
			struct v2f
			{
				float4 vertex : SV_POSITION;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
				float4 ase_texcoord : TEXCOORD0;
				float4 ase_color : COLOR;
			};

			uniform sampler2D _MainTex;
			uniform float2 _MainTexScroll;
			uniform float2 _Tiling;
			uniform sampler2D _DistortionTexture;
			uniform float2 _Speed_Distortion;
			uniform float4 _DistortionTexture_ST;
			uniform float _Distortion_Amount;
			uniform sampler2D _Mask;
			uniform float _Multiply;
			
			v2f vert ( appdata v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				UNITY_TRANSFER_INSTANCE_ID(v, o);

				o.ase_texcoord = v.ase_texcoord;
				o.ase_color = v.color;
				float3 vertexValue = float3(0, 0, 0);
				#if ASE_ABSOLUTE_VERTEX_POS
				vertexValue = v.vertex.xyz;
				#endif
				vertexValue = vertexValue;
				#if ASE_ABSOLUTE_VERTEX_POS
				v.vertex.xyz = vertexValue;
				#else
				v.vertex.xyz += vertexValue;
				#endif
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}
			
			fixed4 frag (v2f i ) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
				fixed4 finalColor;
				float2 uv054 = i.ase_texcoord * _Tiling + (i.ase_texcoord).zw;
				float2 panner16 = ( 1.0 * _Time.y * _MainTexScroll + uv054);
				float2 uv0_DistortionTexture = i.ase_texcoord.xy * _DistortionTexture_ST.xy + _DistortionTexture_ST.zw;
				float2 panner45 = ( 1.0 * _Time.y * _Speed_Distortion + uv0_DistortionTexture);
				float temp_output_6_0 = ( (tex2D( _DistortionTexture, panner45 )).r * _Distortion_Amount );
				float2 temp_cast_0 = (temp_output_6_0).xx;
				#ifdef _DISTORTIONMAINTEX_ON
				float2 staticSwitch33 = ( panner16 - temp_cast_0 );
				#else
				float2 staticSwitch33 = panner16;
				#endif
				float4 tex2DNode2 = tex2D( _MainTex, staticSwitch33 );
				float2 uv0117 = i.ase_texcoord.xy * float2( 1,1 ) + float2( 0,0 );
				float2 temp_cast_1 = (temp_output_6_0).xx;
				#ifdef _DISTORTIONMASK_ON
				float2 staticSwitch124 = ( uv0117 - temp_cast_1 );
				#else
				float2 staticSwitch124 = uv0117;
				#endif
				#ifdef _MASKENABLE_ON
				float4 staticSwitch85 = ( tex2DNode2 * tex2D( _Mask, staticSwitch124 ).a );
				#else
				float4 staticSwitch85 = tex2DNode2;
				#endif
				
				
				finalColor = ( ( staticSwitch85 * _Multiply ) * i.ase_color * i.ase_color.a );
				return finalColor;
			}
			ENDCG
		}
	}
	
	
	
}
