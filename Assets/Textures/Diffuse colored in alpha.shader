Shader "Diffuse colored in alpha" {
	Properties {
		_Color ("Main Color", Color) = (1,1,1,1)
		_MainTex ("Base (RGBA)", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 250
		
		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Lambert

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
	fixed4 _Color;

		struct Input {
			float2 uv_MainTex;
			float2 uv_DecalTex;
		};


		void surf (Input IN, inout SurfaceOutput o) {
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = lerp(_Color, c.rgb, c.a);
			o.Alpha = 1;
		}
		ENDCG
	}
	FallBack "Diffuse"
}
