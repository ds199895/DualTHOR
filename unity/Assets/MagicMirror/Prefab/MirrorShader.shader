Shader "Universal Render Pipeline/Mirror"
{
	Properties
	{
		_MainTex ("Emissive Texture", 2D) = "black" {}
		_DetailTex ("Detail Texture", 2D) = "white" {}
		_Color ("Detail Tint Color", Color) = (1,1,1,1)
		[HDR] _SpecColor ("Specular Color", Color) = (1,1,1,1)
		_SpecularArea ("Specular Area", Range (0, 0.99)) = 0.1
		_SpecularIntensity ("Specular Intensity", Range (0, 1)) = 0.75
		_ReflectionColor ("Reflection Tint Color", Color) = (1,1,1,1)
		_Smoothness ("Smoothness", Range(0,1)) = 0.5
	}
	SubShader
	{ 
		Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
		LOD 300
     
		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
		ENDHLSL

		Pass
		{
			Name "Forward"
			Tags { "LightMode" = "UniversalForward" }
			
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fog
			#pragma multi_compile_instancing
			#pragma multi_compile __ MIRROR_RECURSION
			
			#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
			
			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);
			TEXTURE2D(_DetailTex);
			SAMPLER(sampler_DetailTex);
			
			CBUFFER_START(UnityPerMaterial)
				float4 _DetailTex_ST;
				half4 _Color;
				half4 _ReflectionColor;
				half4 _SpecColor;
				half _SpecularArea;
				half _SpecularIntensity;
				half _Smoothness;
			CBUFFER_END
			
			struct Attributes
			{
				float4 positionOS : POSITION;
				float3 normalOS : NORMAL;
				float2 uv : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};
			
			struct Varyings
			{
				float4 positionCS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 positionWS : TEXCOORD1;
				float3 normalWS : TEXCOORD2;
				float4 screenPos : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
				
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_TRANSFER_INSTANCE_ID(input, output);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
				
				VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
				VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
				
				output.positionCS = vertexInput.positionCS;
				output.positionWS = vertexInput.positionWS;
				output.normalWS = normalInput.normalWS;
				output.uv = TRANSFORM_TEX(input.uv, _DetailTex);
				output.screenPos = vertexInput.positionNDC;
				
				return output;
			}
			
			half4 frag(Varyings input) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID(input);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
				
				half4 detail = SAMPLE_TEXTURE2D(_DetailTex, sampler_DetailTex, input.uv);
				
				half4 refl;
				#if MIRROR_RECURSION
					refl = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
				#else
					float2 screenUV = input.screenPos.xy / max(0.001, input.screenPos.w);
					refl = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, screenUV);
				#endif
				
				// 基础光照计算
				InputData lightingInput = (InputData)0;
				lightingInput.positionWS = input.positionWS;
				lightingInput.normalWS = normalize(input.normalWS);
				lightingInput.viewDirectionWS = normalize(GetWorldSpaceViewDir(input.positionWS));
				lightingInput.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
				
				// 为PBR设置材质属性
				SurfaceData surfaceData = (SurfaceData)0;
				surfaceData.albedo = detail.rgb * _Color.rgb;
				surfaceData.specular = _SpecColor.rgb;
				surfaceData.metallic = 0;
				surfaceData.smoothness = _Smoothness * (1.0 - _SpecularArea);
				surfaceData.emission = refl.rgb * _ReflectionColor.rgb;
				surfaceData.alpha = 1;
				
				// 使用URP光照函数
				half4 color = UniversalFragmentBlinnPhong(lightingInput, surfaceData);
				
				return color;
			}
			ENDHLSL
		}
		
		// 这里可以添加ShadowCaster等其他Pass
	}
	FallBack "Universal Render Pipeline/Lit"
}