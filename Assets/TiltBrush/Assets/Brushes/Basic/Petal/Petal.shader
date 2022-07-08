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

Shader "Brush/Special/Petal" {
  Properties{
    _SpecColor("Specular Color", Color) = (0.5, 0.5, 0.5, 0)
    _Shininess("Shininess", Range(0.01, 1)) = 0.3
    _MainTex("Base (RGB) TransGloss (A)", 2D) = "white" {}
  }
  SubShader{
    Tags {"IgnoreProjector" = "True" "RenderType" = "Opaque"}
Cull off
    LOD 200

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
      #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight noshadow

        struct appdata {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
            half3 normal : NORMAL;
            half4 color : COLOR;
            float4 tangent : TANGENT;
        };

        struct v2f {
            float4 pos : SV_POSITION;
            float2 uv : TEXCOORD0;
            half3 worldNormal : NORMAL;
            half4 color : COLOR;
            half3 tspace0 : TEXCOORD1;
            half3 tspace1 : TEXCOORD2;
            half3 tspace2 : TEXCOORD3;
        };
CBUFFER_START(UnityPerMaterial)
        sampler2D _MainTex;
        float4 _MainTex_ST;
CBUFFER_END


        v2f vert (appdata v) {
          v2f o;
          o.pos = TransformObjectToHClip(v.vertex.xyz);
          o.uv = TRANSFORM_TEX(v.uv, _MainTex);
          o.worldNormal = TransformObjectToWorldNormal(v.normal);
          o.color = v.color;

          half3 wNormal = TransformObjectToWorldNormal(v.normal);
          half3 wTangent = TransformObjectToWorldDir(v.tangent.xyz);
          half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
          half3 wBitangent = cross(wNormal, wTangent) * tangentSign;
          o.tspace0 = half3(wTangent.x, wBitangent.x, wNormal.x);
          o.tspace1 = half3(wTangent.y, wBitangent.y, wNormal.y);
          o.tspace2 = half3(wTangent.z, wBitangent.z, wNormal.z);
          return o;
        }

        half4 frag (v2f i, half vface : VFACE) : SV_Target {
          float4 darker_color = i.color * 0.6;
          float4 finalColor = lerp(i.color, darker_color, 1 - i.uv.x);
          float fAO = .5 * i.uv.x;

          half4 color = finalColor * fAO;
          
          half3 lighting = 0.9;
          color.rgb *= lighting;
          return color;
        }
      ENDHLSL
    } // pass
}
}