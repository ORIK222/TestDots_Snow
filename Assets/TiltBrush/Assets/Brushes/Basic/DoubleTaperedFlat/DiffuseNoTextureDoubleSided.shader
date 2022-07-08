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

Shader "Brush/Special/DiffuseNoTextureDoubleSided" {
Properties {
  _Color ("Main Color", Color) = (1,1,1,1)
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
      sampler2D _BumpMap;
CBUFFER_END
            float4 _BumpMap_ST;
            float4 _MainTex_ST;
        struct appdata {
            float4 vertex : POSITION;
            float2 uv : TEXCOORD0;
            float3 texcoord1 : TEXCOORD1;
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
      
      v2f vert (appdata v)
      {
        v2f o;
        float envelope = sin(v.uv.x * 3.14159);
        float widthMultiplier = 1 - envelope;
        v.vertex.xyz += -v.texcoord1 * widthMultiplier;
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
          half4 col = i.color;
          col.a = tex2D(_MainTex, i.uv).a * col.a;
          half3 tnormal = half3(0.4,0.4,0.4);
          tnormal.z *= vface;

          // transform normal from tangent to world space
          half3 worldNormal;
          worldNormal.x = dot(i.tspace0, tnormal);
          worldNormal.y = dot(i.tspace1, tnormal);
          worldNormal.z = dot(i.tspace2, tnormal);

          half ndotl = saturate(dot(worldNormal, normalize(float3(0.5,0.5,0.5).xyz)));
          half3 lighting = ndotl * 1.1;
          col.rgb *= lighting;
          return col;
        }

      ENDHLSL
    }
  }
}
Fallback "Transparent/Cutout/VertexLit"
}
