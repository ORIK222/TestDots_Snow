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

Shader "Brush/Special/VelvetInk" {
Properties {
  _MainTex ("Texture", 2D) = "white" {}
  _Shininess ("Shininess", Range (0.01, 1)) = 0.078125
}

Category {
  Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
  Blend SrcAlpha One
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
      
CBUFFER_START(UnityPerMaterial)
      sampler2D _MainTex;
      float4 _MainTex_ST;
      float _Shininess;
      CBUFFER_END
      #ifdef UNITY_DOTS_INSTANCING_ENABLED  
                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float, _Shininess)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
            #define _Shininess UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _Shininess)
      #endif

      struct appdata_t {
        float4 vertex : POSITION;
        half4 color : COLOR;
        float3 normal : NORMAL;
        float2 texcoord : TEXCOORD0;
      };

      struct v2f {
        float4 vertex : SV_POSITION;
        half4 color : COLOR;
        float2 texcoord : TEXCOORD0;
      };



      v2f vert (appdata_t v)
      {

        v2f o;

        o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);

        o.color = v.color;
        o.vertex = TransformObjectToHClip(v.vertex.xyz);

        return o;
      }

      half4 frag (v2f i) : SV_Target
      {
        half4 c = tex2D(_MainTex , i.texcoord );
        return i.color * c;
      }
      ENDHLSL
    }
  }
}
}
