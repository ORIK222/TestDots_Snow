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

Shader "Brush/Visualizer/RainbowTube" {
Properties {
}

Category {
  Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
  Blend One One //SrcAlpha One
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
      

      struct appdata_t {
        float4 vertex : POSITION;
        half4 color : COLOR;
        float2 texcoord : TEXCOORD0;
      };

      struct v2f {
        float4 vertex : SV_POSITION;
        float4 color : COLOR;
        float2 texcoord : TEXCOORD0;
        float4 unbloomedColor : TEXCOORD1;
      };

      v2f vert (appdata_t v)
      {
        v2f o;
        o.vertex = TransformObjectToHClip(v.vertex.xyz);
        o.texcoord = v.texcoord;
        o.color = v.color;
        o.unbloomedColor = v.color;
        return o;
      }

      half4 frag (v2f i) : SV_Target
      {

        float waveform_r = .15 * sin( -20 * i.unbloomedColor.r * _Time.w + i.texcoord.x * 100 * i.unbloomedColor.r);
        float waveform_g = .15 * sin( -30 * i.unbloomedColor.g * _Time.w + i.texcoord.x * 100 * i.unbloomedColor.g);
        float waveform_b = .15 * sin( -40 * i.unbloomedColor.b * _Time.w + i.texcoord.x * 100 * i.unbloomedColor.b);

        i.texcoord.y = fmod(i.texcoord.y + i.texcoord.x, 1);
        float procedural_line_r = saturate(1 - 40*abs(i.texcoord.y - .5 + waveform_r));
        float procedural_line_g = saturate(1 - 40*abs(i.texcoord.y - .5 + waveform_g));
        float procedural_line_b = saturate(1 - 40*abs(i.texcoord.y - .5 + waveform_b));
        float4 color = procedural_line_r * float4(1,0,0,0) + procedural_line_g * float4(0,1,0,0) + procedural_line_b * float4(0,0,1,0);
        color.w = 1;
        color = i.color * color;

        color = float4(color.rgb * color.a, 1.0);
        return color;
      }
      ENDHLSL
    }
  }
}
}
