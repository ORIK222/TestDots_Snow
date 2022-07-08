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

//
// Tilt Brush variant of the Blocks gem shader
//
Shader  "Blocks/BlocksGemHLSL"  {
Properties {
    _MainTex("Texture", 2D) = "white" {}
    _Color ("Color", Color) = (1,1,1,1)
    _Shininess ("Shininess", Range(0,1)) = 0.8
    _RimIntensity ("Rim Intensity", Range(0,1)) = .2
    _RimPower ("Rim Power", Range(0,16)) = 5
    _Frequency ("Frequency", Float) = 1
    _Jitter ("Jitter", Float) = 1
    _WorldLight ("Light Direction", Vector) = (1.0,1.0,1.0) 
}
  SubShader{
    Tags{ "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" }
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

   CBUFFER_START(UnityPerMaterial)
      sampler2D _MainTex;
      float4 _MainTex_ST;
      sampler2D _BumpMap;
      float4 _BumpMap_ST;
      half _Shininess;
      half _RimIntensity;
      half _RimPower;
      half4 _Color;
      float4 _WorldLight;
      uniform float _Frequency;
      uniform float _Jitter;
      CBUFFER_END


  //1/7
  #define K 0.142857142857
  //3/7
  #define Ko 0.428571428571

  #define OCTAVES 1

  float3 mod(float3 x, float y) { return x - y * floor(x/y); }
  float2 mod(float2 x, float y) { return x - y * floor(x/y); }

  // Permutation polynomial: (34x^2 + x) mod 289
  float3 Permutation(float3 x)
  {
    return mod((34.0 * x + 1.0) * x, 289.0);
  }

  float2 inoise(float3 P, float jitter)
  {
    float3 Pi = mod(floor(P), 289.0);
    float3 Pf = frac(P);
    float3 oi = float3(-1.0, 0.0, 1.0);
    float3 of = float3(-0.5, 0.5, 1.5);
    float3 px = Permutation(Pi.x + oi);
    float3 py = Permutation(Pi.y + oi);

    float3 p, ox, oy, oz, dx, dy, dz;
    float2 F = 1e6;

    for(int i = 0; i < 3; i++) {
      for(int j = 0; j < 3; j++) {
        p = Permutation(px[i] + py[j] + Pi.z + oi); // pij1, pij2, pij3

        ox = frac(p*K) - Ko;
        oy = mod(floor(p*K),7.0)*K - Ko;

        p = Permutation(p);

        oz = frac(p*K) - Ko;

        dx = Pf.x - of[i] + jitter*ox;
        dy = Pf.y - of[j] + jitter*oy;
        dz = Pf.z - of + jitter*oz;

        float3 d = dx * dx + dy * dy + dz * dz; // dij1, dij2 and dij3, squared

        //Find lowest and second lowest distances
        for(int n = 0; n < 3; n++) {
          if(d[n] < F[0]) {
            F[1] = F[0];
            F[0] = d[n];
          } else if(d[n] < F[1]) {
            F[1] = d[n];
          }
        }
      }
    }
    return F;
  }

  // fractal sum, range -1.0 - 1.0
  float2 fBm_F0(float3 p, int octaves)
  {
    float freq = _Frequency, amp = 0.5;
    float2 F = inoise(p * freq, _Jitter) * amp;
    return F;
  }

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

          const float kPerturbIntensity = 10;
          float2 F = fBm_F0(i.pos.xyz, OCTAVES);
          float gem = (F.y - F.x);
          float3 normal = float3(0,0,1) + kPerturbIntensity * float3(ddy(gem), ddx(gem),0);
          
          half3 emission = (pow(1 - saturate(dot(GetViewForwardDir(), normal)), _RimPower)) * _RimIntensity;
          half3 refl = clamp(normal + gem, -1.0,1.0);
           float3 colorRamp = float3(1,.3,0)*sin(refl.x * 30) + float3(0,1,.5)*cos(refl.y * 37.77) + float3(0,0,1)*sin(refl.z*43.33);
          half3 specularColor = _Color.rgb + colorRamp * .1;
          return _Color * float4(specularColor.xyz,1) * float4(emission.xyz, 1);
        }
    ENDHLSL
} // end subshader
}
  FallBack "Diffuse"
}
// end shader
