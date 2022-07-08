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

Shader "Brush/Special/CelVinyl" {
  Properties{
    _MainTex("MainTex", 2D) = "white" {}
    _Color("Color", Color) = (1,1,1,1)
    _Cutoff ("Alpha Cutoff", Range (0,1)) = 0.5
  }

  SubShader{
    Pass {
      Tags{ "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout"}

      Lighting Off
      Cull Off

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

        #pragma target 3.0
CBUFFER_START(UnityPerMaterial)
        sampler2D _MainTex;
        float4 _MainTex_ST;
        half4 _Color;
        float _Cutoff;
      CBUFFER_END
        struct appdata_t {
            float4 vertex : POSITION;
            float2 texcoord : TEXCOORD0;
            float4 color : COLOR;
        };

        struct v2f {
            float4 vertex : SV_POSITION;
            float2 texcoord : TEXCOORD0;
            float4 color : COLOR;
        };

        v2f vert (appdata_t v)
        {

          v2f o;

          o.vertex = TransformObjectToHClip(v.vertex.xyz);
          o.texcoord = v.texcoord;
          o.color = TbVertToNative(v.color);
          return o;
        }

        half4 frag (v2f i) : SV_Target
        {
          half4 tex = tex2D(_MainTex, i.texcoord) * i.color;
          if (tex.a < _Cutoff) {
            discard;
          }
          return tex;
        }

      ENDHLSL
    }
  }

  Fallback "Unlit/Diffuse"
}