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

Shader "Brush/Visualizer/Dots" {
Properties {
  _TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
  _MainTex ("Particle Texture", 2D) = "white" {}
  _WaveformFreq("Waveform Freq", Float) = 1
  _WaveformIntensity("Waveform Intensity", Vector) = (0,1,0,0)
  _BaseGain("Base Gain", Float) = 0
  _EmissionGain("Emission Gain", Float) = 0
  _Shininess ("Shininess", Range (0.01, 1)) = 0.078125
}

Category {
  Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "DisableBatching"="True" }
  Blend One One
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
      #pragma exclude_renderers gles gles3 glcore
      #pragma target 4.5
      #pragma multi_compile_instancing
      #pragma instancing_options renderinglayer
      #pragma multi_compile _ DOTS_INSTANCING_ON
      #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
      #include "../../../Shaders/Include/Particles.hlsl"


CBUFFER_START(UnityPerMaterial)
      float4 _MainTex_ST;
      float _WaveformFreq;
      float4 _WaveformIntensity;
      float _EmissionGain;
      float _BaseGain;
      sampler2D _MainTex;
      half4 _TintColor;
      CBUFFER_END
      #ifdef UNITY_DOTS_INSTANCING_ENABLED  
                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float, _Shininess)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
            #define _Shininess UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _Shininess)
      #endif
      struct v2f
  {
        float4 vertex : SV_POSITION;
        half4 color : COLOR;
        float2 texcoord : TEXCOORD0;
        float waveform : TEXCOORD1;
      };
  
      v2f vert (ParticleVertex_t v)
      {
        v2f o;
        float birthTime = v.texcoord.w;
        float rotation = v.texcoord.z;
        float halfSize = GetParticleHalfSize(v.corner.xyz, v.center, birthTime);
        float4 center = float4(v.center.xyz, 1);
        float4 corner = OrientParticle(center.xyz, halfSize, v.vid, rotation);
        float waveform = 0;
        // TODO: displacement should happen before orientation

        o.vertex = TransformObjectToHClip(corner.xyz);
        o.color = v.color * _BaseGain;
        o.texcoord = TRANSFORM_TEX(v.texcoord.xy,_MainTex);
        o.waveform = waveform * 15;
        return o;
      }

      // Input color is srgb
      half4 frag (v2f i) : SV_Target
      {
        float4 tex = tex2D(_MainTex, i.texcoord);
        float4 c = i.color * _TintColor * tex;

        // Only alpha channel receives emission boost
        c.rgb += c.rgb * c.a * _EmissionGain;
        c.a = 1;
        //c = SrgbToNative(c);
        return float4(c.rgb, 1.0);
      }
      ENDHLSL
    }
  }
}
  }
