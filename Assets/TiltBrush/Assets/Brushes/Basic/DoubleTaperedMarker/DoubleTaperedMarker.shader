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

Shader "Brush/Special/DoubleTaperedMarker" {
Properties {
}

Category {
  Cull Off Lighting Off

  SubShader {
    Tags{ }
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
CBUFFER_END
      struct appdata_t {
        float4 vertex : POSITION;
        half4 color : COLOR;
        float2 texcoord0 : TEXCOORD0;
        float3 texcoord1 : TEXCOORD1; //per vert offset vector
      };

      struct v2f {
        float4 vertex : SV_POSITION;
        half4 color : COLOR;
        float2 texcoord : TEXCOORD0;
      };
      v2f vert (appdata_t v)
      {
        v2f o;
        float envelope = sin(v.texcoord0.x * 3.14159);
        float widthMultiplier = 1 - envelope;
        v.vertex.xyz += -v.texcoord1 * widthMultiplier;
        o.vertex = TransformObjectToHClip(v.vertex.xyz);
        o.color = TbVertToNative(v.color);
        o.texcoord = v.texcoord0;
        return o;
      }

      half4 frag (v2f i) : SV_Target
      {
        return float4(i.color.rgb, 1);

      }

      ENDHLSL
    }
  }
}
}
