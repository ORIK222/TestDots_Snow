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

Shader "Brush/Particle/Embers" {
Properties {
  _TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
  _MainTex ("Particle Texture", 2D) = "white" {}
  _ScrollRate("Scroll Rate", Float) = 1.0
  _ScrollDistance("Scroll Distance", Vector) = (1.0, 0, 0)
  _ScrollJitterIntensity("Scroll Jitter Intensity", Float) = 1.0
  _ScrollJitterFrequency("Scroll Jitter Frequency", Float) = 1.0
  _SpreadRate ("Spread Rate", Range(0.3, 5)) = 1.539
}

Category {
  Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "DisableBatching"="True" }
  Blend One One  // SrcAlpha One
  BlendOp Add, Min
  ColorMask RGBA
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
      
CBUFFER_START(UnityPerMaterial)
      sampler2D _MainTex;
      half4 _TintColor;
      float4 _MainTex_ST;
      float _ScrollRate;
      // In decimeters
      float3 _ScrollDistance;
      // Amplitude: in decimeters
      float _ScrollJitterIntensity;
      float _ScrollJitterFrequency;
      float _SpreadRate;
      CBUFFER_END
      
      struct v2f {
        float4 vertex : SV_POSITION;
        half4 color : COLOR;
        float2 texcoord : TEXCOORD0;
      };



      // pos and seed should be stable values.
      // seed is a value in [0, 1]
      // t01 is a time value in [0, 1]
      float3 ComputeDisplacement(float3 pos, float seed, float t01) {
        float t2 = _Time.y;

        // Animate the motion of the embers
        // Accumulate all displacement into a common, pre-transformed space.
        float4 dispVec = float4(_ScrollDistance, 0.0) * t01;

        dispVec.x += sin(t01 * _ScrollJitterFrequency + seed * 100 + t2 + pos.z) * _ScrollJitterIntensity;
        dispVec.y += (fmod(seed * 100, 1) - 0.5) * _ScrollDistance.y * t01;
        dispVec.z += cos(t01 * _ScrollJitterFrequency + seed * 100 + t2 + pos.x) * _ScrollJitterIntensity;
        // Allow scaling to affect particle speed and distance in toolkit
        return dispVec.xyz * kDecimetersToWorldUnits  * length(unity_ObjectToWorld[0].xyz);
      }

      v2f vert (ParticleVertexWithSpread_t v) {
        v.color = TbVertToSrgb(v.color);
        v2f o;
        // Used as a random-ish seed for various calculations
        float seed = v.color.a;
        float t01 = fmod(_Time.y*_ScrollRate + seed * 10, 1);
        float birthTime = v.texcoord.w;
        float rotation = v.texcoord.z;
        float halfSize = GetParticleHalfSize(v.corner.xyz, v.center, birthTime);
        float spreadProgress = SpreadProgress(birthTime, _SpreadRate);
        float4 center = SpreadParticle(v, spreadProgress);
        float3 disp = ComputeDisplacement(center.xyz, seed, t01);
        disp = spreadProgress * disp;

        // Ramp color from bright to dark over particle lifetime
        float3 incolor = v.color.rgb;
        float t_minus_1 = 1-t01;
        float sparkle = (pow(abs(sin(_Time.y * 3 + seed * 10)), 30));
        v.color.rgb += pow(t_minus_1,10)*incolor*200;
        v.color.rgb += incolor * sparkle * 50;
        // Dim over lifetime
        v.color.rgb *= incolor * pow (1 - t01, 2)*5;
        
        float4 center_WS = mul(unity_ObjectToWorld, center);
        center_WS.xyz += mul(xf_CS, float4(disp, 0)).xyz;
        float4 corner_WS = OrientParticle_WS(center_WS.xyz, halfSize, v.vid, rotation);
        o.vertex.xyz = TransformObjectToWorld (corner_WS.xyz);
        o.vertex.xyz = TransformWorldToView(o.vertex.xyz);
        o.vertex = mul(UNITY_MATRIX_P, float4(o.vertex.xyz, 1.0f));
        o.color = v.color;
        o.texcoord = TRANSFORM_TEX(v.texcoord.xy,_MainTex);

        return o;
      }

      // i.color is srgb
      half4 frag (v2f i) : SV_Target
      {
        float4 color = 2.0f * i.color * _TintColor * tex2D(_MainTex, i.texcoord);
        color = float4(color.rgb * color.a, 1.0);
        color = SrgbToNative(color);
        return color;
      }
      ENDHLSL
    }
  }
}
}
