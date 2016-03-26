Shader "Hidden/SpectrumSweepBlurShader"
{
	Properties
	{
		_MainTex ("Primary Texture (iterative)", 2D) = "white" {}
		_SpectrumAmplitudesTex ("Incoming Spectrum Amplitudes Texture (single-row)", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert(
				appdata v)
			{
				v2f o;
				o.vertex = mul(UNITY_MATRIX_MVP, v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;
			uniform half4 _MainTex_TexelSize;
			
			sampler2D _SpectrumAmplitudesTex;
			uniform half4 _SpectrumAmplitudesTex_TexelSize;

			fixed4 frag(v2f inputs) : SV_Target
			{
				// If this is the input-column, else it's the histogram that scrolls out from the input column.
				if (inputs.uv[0] <= _MainTex_TexelSize.x)
				{
					float2 rotatedTextCoords = float2(
						inputs.uv[1],
						0.0f);

					fixed4 incomingSpectrumColor = 
						tex2D(_SpectrumAmplitudesTex, rotatedTextCoords);

					return incomingSpectrumColor;
				}
				else
				{
					float2 lowerTexCoordOffset = float2(
						(-1 * _MainTex_TexelSize.x),
						(-1 * _MainTex_TexelSize.y));

					float2 evenTexCoordOffset = float2(
						(-1 * _MainTex_TexelSize.x),
						0.0f);
					
					float2 higherTexCoordOffset = float2(
						(-1 * _MainTex_TexelSize.x),
						_MainTex_TexelSize.y);

					float2 deeperTexCoordOffset = float2(
						(-2 * _MainTex_TexelSize.x),
						0.0f);

					fixed blurredHistoryColor = (
						(
							(1 * tex2D(_MainTex, (inputs.uv + lowerTexCoordOffset))) + 
							(1 * tex2D(_MainTex, (inputs.uv + evenTexCoordOffset))) + 
							(2 * tex2D(_MainTex, (inputs.uv + higherTexCoordOffset))) + 
							(0 * tex2D(_MainTex, (inputs.uv + deeperTexCoordOffset)))
						) /
						fixed4(4, 4, 4, 4));

					return blurredHistoryColor;
				}
			}
			ENDCG
		}
	}
}
