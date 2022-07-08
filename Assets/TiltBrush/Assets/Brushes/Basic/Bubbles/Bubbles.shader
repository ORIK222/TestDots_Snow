// Copyright 2017 Google Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

Shader "Brush/Particle/Bubbles" {
Properties {
  _MainTex ("Particle Texture", 2D) = "white" {}
  _ScrollRate("Scroll Rate", Float) = 1.0
  _ScrollJitterIntensity("Scroll Jitter Intensity", Float) = 1.0
  _ScrollJitterFrequency("Scroll Jitter Frequency", Float) = 1.0
  _SpreadRate ("Spread Rate", Range(0.3, 5)) = 1.539
}

Category {
  Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "DisableBatching"="True" }
  Blend One One
  AlphaTest Greater .01
  ColorMask RGB
  Cull Off Lighting Off ZWrite Off Fog { Color (0,0,0,0) }

  SubShader {
    Pass {

      HLSLPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #pragma exclude_renderers gles gles3 glcore
      #pragma target 4.5
      #pragma multi_compile_instancing
      #pragma instancing_options renderinglayer
      #pragma multi_compile _ DOTS_INSTANCING_ON
      #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
      #include "../../../Shaders/Include/Brush.hlsl"
      #include "../../../Shaders/Include/Particles.hlsl"
      #include "Assets/ThirdParty/Noise/Shaders/Noise.hlsl"

      sampler2D _MainTex;
      half4 _TintColor;

      struct v2f {
        float4 vertex : SV_POSITION;
        half4 color : COLOR;
        float2 texcoord : TEXCOORD0;
      };
CBUFFER_START(UnityPerMaterial)
      float4 _MainTex_ST;
      float _ScrollRate;
      float _ScrollJitterIntensity;
      float _ScrollJitterFrequency;
      float3 _WorldSpaceRootCameraPosition;
      float _SpreadRate;
CBUFFER_END

    float3 computeDisplacement(float3 seed, float timeOffset) {
	    float3 jitter;
	    float t = _Time.y * _ScrollRate + timeOffset;
	    jitter.x = sin(t + _Time.y + seed.z * _ScrollJitterFrequency);
	    jitter.z = cos(t + _Time.y + seed.x * _ScrollJitterFrequency);
	    jitter.y = cos(t * 1.2 + _Time.y + seed.x * _ScrollJitterFrequency);
	    jitter *= _ScrollJitterIntensity;

	    float3 curl;
	    float3 v = (seed + jitter) * .1 + _Time.x * 5;
	    float d = 30;
	    curl = float3(curlX(v, d), curlY(v, d), curlZ(v, d)) * 10;

	    return (jitter + curl) * kDecimetersToWorldUnits;
    }

      v2f vert (ParticleVertexWithSpread_t v) {
        v2f o;
        v.color = TbVertToSrgb(v.color);
        float birthTime = v.texcoord.w;
        float rotation = v.texcoord.z;
        float halfSize = GetParticleHalfSize(v.corner.xyz, v.center, birthTime);
        float spreadProgress = SpreadProgress(birthTime, _SpreadRate);
        float4 center = SpreadParticle(v, spreadProgress);

        float3 displacement_SS = spreadProgress * computeDisplacement(center.xyz, 1);
        float3 displacement_WS = mul(xf_CS, float4(displacement_SS, 0)).xyz;
        float3 displacement_OS = mul(unity_WorldToObject, float4(displacement_WS, 0)).xyz;
        center.xyz += displacement_OS;
        float4 corner = OrientParticle(center.xyz, halfSize, v.vid, rotation);
        o.vertex = TransformObjectToHClip(corner.xyz);

        // Brighten up the bubbles
        o.color = v.color;
        o.color.a = 1;
        o.texcoord = TRANSFORM_TEX(v.texcoord.xy,_MainTex);

        return o;
      }

      half4 frag (v2f i) : SV_Target
      {
        float4 tex = tex2D(_MainTex, i.texcoord);

        // RGB Channels of the texture are affected by color
        float3 basecolor = i.color.rgb * tex.rgb;

        // Alpha channel of the texture is not affected by color.  It is the fake "highlight" bubble effect.
        float3 highlightcolor = tex.a;

        float4 color = float4(basecolor + highlightcolor, 1);
        return SrgbToNative(color);
      }
      ENDHLSL
    }
  }
}
}
