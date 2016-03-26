Shader "Custom/EcholocationShader"
{
	Properties
	{
		_Color ("Color", Color) = (1, 1, 1, 1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_LightingRampTex ("Lighting Ramp (distance, frequency)", 2D) = "white" {}
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM

		#pragma surface surf Ramp
			
		#include "UnityCG.cginc"

		struct Input
		{
			float2 uv_MainTex;
			float3 worldPos;
		};
	
		fixed4 _Color;
		sampler2D _MainTex;
		sampler2D _LightingRampTex;

		void surf (
			Input input, 
			inout SurfaceOutput output)
		{
			fixed4 color = (tex2D(_MainTex, input.uv_MainTex) * _Color);
			output.Albedo = color.rgb;
			output.Alpha = color.a;
		}

		half4 LightingRamp(
			SurfaceOutput surface,
			half3 lightDir,
			half atten)
		{
			// NOTE: The incidence is currently backwards, but there's currently no high-frequency data to render.
			// float lightIncidenceFraction = ((-1 * dot(surface.Normal, lightDir)) * 0.5 + 0.5);
			float lightIncidenceFraction = 0.1f;
			float2 lightingRampTexCoords = float2((1.0f - atten), lightIncidenceFraction);

			half3 rampSampleColor = tex2D(_LightingRampTex, lightingRampTexCoords).rgb;

			half4 lightSampleColor;
			lightSampleColor.rgb = (surface.Albedo * _LightColor0.rgb * rampSampleColor);
			lightSampleColor.a = surface.Alpha;

			return lightSampleColor;
		}

		ENDCG
	}
	FallBack "Diffuse"
}
