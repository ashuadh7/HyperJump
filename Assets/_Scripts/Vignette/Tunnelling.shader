﻿Shader "Hidden/Tunnelling" {
	Properties{
		_MainTex("Texture", 2D) = "white" {}
		_AV("Angular Velocity", Float) = 0
		_Feather("Feather", Float) = 0.1
	}
		SubShader{
			// No culling or depth
			Cull Off ZWrite Off ZTest Always

			Pass {
				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma multi_compile __ TUNNEL_SKYBOX

				#include "UnityCG.cginc"

				struct appdata {
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct v2f {
					float2 uv : TEXCOORD0;
					float4 vertex : SV_POSITION;
				};

				v2f vert(appdata v) {
					v2f o;
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = v.uv;
					return o;
				}

				sampler2D _MainTex;
				float4 _MainTex_ST;
				float _AV;
				float _Feather;

				float4x4 _EyeProjection[2];
				float4x4 _EyeToWorld[2];

				inline float4 screenCoords(float2 uv) {
					float2 c = (uv - 0.5) * 2;
					float4 vPos = mul(_EyeProjection[unity_StereoEyeIndex], float4(c, 0, 1));
					vPos.xyz /= vPos.w;
					return vPos;
				}

				fixed4 frag(v2f i) : SV_Target {
					float2 uv = UnityStereoScreenSpaceUVAdjust(i.uv, _MainTex_ST);
					fixed4 col = tex2D(_MainTex, uv);

					float4 coords = screenCoords(i.uv);
					float radius = length(coords.x / (_ScreenParams.x / 2)) / 2;

					if (-_AV > 0 && coords.x < 0 ||
						-_AV < 0 && coords.x > 0)
					{
						radius = 0;
					}

					float avMin = (1 - abs(_AV)) - _Feather;
					float avMax = (1 - abs(_AV)) + _Feather;

					float t = saturate((radius - avMin) / (avMax - avMin));

					// Set vignette color
					fixed4 effect = fixed4(0,0,0,0);

					return lerp(col, effect, t);
				}
				ENDCG
			}
		}
}