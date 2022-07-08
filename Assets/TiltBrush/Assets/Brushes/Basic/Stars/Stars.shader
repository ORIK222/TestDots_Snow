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

Shader "Brush/Particle/Stars" {
Properties {
  _MainTex ("Particle Texture", 2D) = "white" {}
  _SparkleRate ("Sparkle Rate", Float) = 2.5
  _SpreadRate ("Spread Rate", Range(0.3, 5)) = 1.539
}

Category {
  Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "DisableBatching"="True" }
  Blend One One // SrcAlpha One
  BlendOp Add, Min
  AlphaTest Greater .01
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
      float4 _MainTex_ST;
      float _SparkleRate;
      float _SpreadRate;
CBUFFER_END
      struct v2f {
        float4 vertex : SV_POSITION;
        half4 color : COLOR;
        float2 texcoord : TEXCOORD0;
      };

      v2f vert (ParticleVertexWithSpread_t v)
      {
        v.color = TbVertToSrgb(v.color);
        v2f o;
        float birthTime = v.texcoord.w;
        float rotation = v.texcoord.z;
        float halfSize = GetParticleHalfSize(v.corner.xyz, v.center, birthTime);
        float spreadProgress = SpreadProgress(birthTime, _SpreadRate);
        float4 center = SpreadParticle(v, spreadProgress);

        float phase = v.color.a * (2 * 3.14159265359);
        float brightness;
        brightness = 800 * pow(abs(sin(_Time.y * _SparkleRate + phase)), 20);
        o.color.rgb = v.color.rgb * brightness;
        o.color.a = 1;
        o.texcoord = TRANSFORM_TEX(v.texcoord.xy,_MainTex);

        float4 corner = OrientParticle(center.xyz, halfSize, v.vid, rotation);
        o.vertex = TransformObjectToHClip(corner.xyz);

        return o;
      }

      // Input color is srgb
      half4 frag (v2f i) : SV_Target
      {
        float4 color = i.color * tex2D(_MainTex, i.texcoord);
        color = float4(color.rgb * color.a, 1.0);
        color = SrgbToNative(color);
        return color;
      }
      ENDHLSL
    }
  }
}
}
