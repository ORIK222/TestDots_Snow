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

Shader "Brush/Special/Comet" {
Properties {
  _MainTex ("Texture", 2D) = "white" {}
  _AlphaMask("Alpha Mask", 2D) = "white" {}
  _Speed ("Animation Speed", Range (0,1)) = 1
  _EmissionGain ("Emission Gain", Range(0, 1)) = 0.5
}

Category {
  Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
  Blend One One // SrcAlpha One
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
      
CBUFFER_START(UnityPerMaterial)
      sampler2D _MainTex;
      sampler2D _AlphaMask;
      float4 _MainTex_ST;
      float4 _AlphaMask_ST;
      float _Speed;
      half _EmissionGain;
      CBUFFER_END
      struct appdata_t {
        float4 vertex : POSITION;
        half4 color : COLOR;
        float3 normal : NORMAL;
        float3 texcoord : TEXCOORD0;
      };

      struct v2f {
        float4 vertex : POSITION;
        half4 color : COLOR;
        float2 texcoord : TEXCOORD0;
      };


      v2f vert (appdata_t v)
      {
        v2f o;

        o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
        o.color = TbVertToNative(v.color);
        o.vertex = TransformObjectToHClip(v.vertex.xyz);

        return o;
      }

      half4 frag (v2f i) : COLOR
      {
        float time = _Time.y * -_Speed;
        half2 scrollUV = i.texcoord;
        half2 scrollUV2 = i.texcoord;
        half2 scrollUV3 = i.texcoord;
        scrollUV.y += time; // a little twisting motion
        scrollUV.x += time;
        scrollUV2.x += time * 1.5;
        scrollUV3.x += time * 0.5;

        // Each channel has its own tileable pattern which we want to scroll against one another
        // at different rates. We pack 'em into channels because it's more performant than
        // using 3 different texture lookups.
        float r = tex2D(_MainTex, scrollUV).r;
        float g = tex2D(_MainTex, scrollUV2).g;
        float b = tex2D(_MainTex, scrollUV3).b;

        // Combine all channels
        float gradient_lookup_value = (r + g + b) / 3.0;
        gradient_lookup_value *= (1 - i.texcoord.x); // rescales the lookup value from start to finish
        gradient_lookup_value = (pow(gradient_lookup_value, 2) + 0.125) * 3;

        float falloff = max((0.2 - i.texcoord.x) * 5, 0);
        float tex = tex2D(_AlphaMask, saturate(gradient_lookup_value + falloff)).r;

        return float4((tex * i.color).rgb , 1.0);
      }
      ENDHLSL
    }
  }
}
}
