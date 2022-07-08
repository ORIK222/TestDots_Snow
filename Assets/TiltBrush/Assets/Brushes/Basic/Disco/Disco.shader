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

Shader "Brush/Disco" {
  Properties {
    _Color ("Main Color", Color) = (1,1,1,1)
    _SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 0)
    _Shininess ("Shininess", Range (0.01, 1)) = 0.078125
    _MainTex ("Base (RGB) TransGloss (A)", 2D) = "white" {}
    _BumpMap ("Normalmap", 2D) = "bump" {}
  }
  Category {
  Cull Off Lighting Off
  
  SubShader{
    Tags{ "Queue" = "AlphaTest" "IgnoreProjector" = "True" "RenderType" = "TransparentCutout" }
    Cull Back
    Cull Front
    LOD 100
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
     // #include "UnityCG.cginc"
      //#include "Lighting.cginc" 
      #include "../../../Shaders/Include/Brush.hlsl"
      
            struct appdata {
            float4 vertex : POSITION;
            float4 uv : TEXCOORD0;
            float4 texcoord1 : TEXCOORD1;
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
    half4 _MainTex_ST;
    sampler2D _BumpMap;
    half4 _BumpMap_ST;
    half4 _Color;
    half _Shininess;
CBUFFER_END
    
    half3 SHEvalLinearL0L1 (half4 normal)
{
    half3 x;

    // Linear (L1) + constant (L0) polynomial terms
    x.r = dot(unity_SHAr,normal);
    x.g = dot(unity_SHAg,normal);
    x.b = dot(unity_SHAb,normal);

    return x;
}

// normal should be normalized, w=1.0
half3 SHEvalLinearL2 (half4 normal)
{
    half3 x1, x2;
    // 4 of the quadratic (L2) polynomials
    half4 vB = normal.xyzz * normal.yzzx;
    x1.r = dot(unity_SHBr,vB);
    x1.g = dot(unity_SHBg,vB);
    x1.b = dot(unity_SHBb,vB);

    // Final (5th) quadratic (L2) polynomial
    half vC = normal.x*normal.x - normal.y*normal.y;
    x2 = unity_SHC.rgb * vC;

    return x1 + x2;
}

// normal should be normalized, w=1.0
// output in active color space
half3 ShadeSH9 (half4 normal)
{
    // Linear + constant polynomial terms
    half3 res = SHEvalLinearL0L1 (normal);

    // Quadratic polynomials
    res += SHEvalLinearL2 (normal);

#   ifdef UNITY_COLORSPACE_GAMMA
        res = LinearToGammaSpace (res);
#   endif

    return res;
}
    
    v2f vert (appdata v)
      {
        v2f o;
      float t, uTileRate, waveIntensity;
      float radius = v.uv.z;
      t = _Time.z;
      uTileRate = 10;
      waveIntensity = .6;
      float theta = fmod(v.uv.y, 1);
      v.vertex.xyz += pow(1 -(sin(t + v.uv.x * uTileRate + theta * 10) + 1),2)
              * v.normal.xyz * waveIntensity
              * radius;
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
         float3 worldNormal;
          worldNormal.x = dot(i.tspace0, tnormal);
          worldNormal.y = dot(i.tspace1, tnormal);
          worldNormal.z = dot(i.tspace2, tnormal);
          worldNormal *= normalize(cross(ddy(i.pos.xyz), ddx(i.pos.xyz)));
          half3 ndotl = normalize(-cross(cross(i.worldNormal, worldNormal), worldNormal));
          half3 lighting = (ndotl * float3(1,1,1)) + ShadeSH9(half4(worldNormal, 1.0));
          col.rgb *= lighting;
          return col;
        }
    
    ENDHLSL
  }
}
    }
  FallBack "Transparent/Cutout/VertexLit"
}