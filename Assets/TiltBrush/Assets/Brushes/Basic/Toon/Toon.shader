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

Shader "Brush/Special/Toon" {
Properties {
  _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
  _OutlineMax("Maximum outline size", Range(0, .5)) = .005
  _Shininess ("Shininess", Range (0.01, 1)) = 0.078125
}

HLSLINCLUDE
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

CBUFFER_START(UnityPerMaterial)
  sampler2D _MainTex;
  float4 _MainTex_ST;
  float _OutlineMax;
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
    float3 texcoord : TEXCOORD0;
  };

  struct v2f {
    float4 vertex : SV_POSITION;
    half4 color : COLOR;
    float2 texcoord : TEXCOORD0;
  };

  // Transforms position from object space to homogenous space
  float4 TransformObjectToHClip(float4 positionOS)
  {
      // More efficient than computing M*VP matrix product
      return mul(GetWorldToHClipMatrix(), mul(GetObjectToWorldMatrix(), positionOS));
  }

  v2f vertInflate (appdata_t v, float inflate)
  {

    v2f o;
    float outlineEnabled = inflate;
    float radius = v.texcoord.z;
    inflate *= radius * .4;
    float bulge = 0.0;
	float3 worldNormal = TransformObjectToWorldNormal(v.normal);
    //
    // Careful: perspective projection is non-afine, so math assumptions may not be valid here.
    //

    // Technically these are not yet in NDC because they haven't been divided by W, so their
    // range is currently [-W, W].
    o.vertex = TransformObjectToHClip(float4(v.vertex.xyz + v.normal.xyz * bulge, v.vertex.w).xyz);
    float4 outline_NDC = TransformObjectToHClip(float4(v.vertex.xyz + v.normal.xyz * inflate, v.vertex.w).xyz);

    // Displacement in proper NDC coords (e.g. [-1, 1])
    float3 disp = outline_NDC.xyz / outline_NDC.w - o.vertex.xyz / o.vertex.w;

    // Magnitude is a scaling factor to shrink large outlines down to a max width, in NDC space.
    // Notice here we're only measuring 2D displacment in X and Y.
    float mag = length(disp.xy);
    mag = min(_OutlineMax, mag) / mag;

    // Ideally we would project back into world space to do the scaling, but the inverse
    // projection matrix is not currently available. So instead, we multiply back in the w
    // component so both sides of the += operator below are in the same space. Also note
    // that the w component is a function of depth, so modifying X and Y independent of Z
    // should mean that the original w value remains valid.
    o.vertex.xyz += float3(disp.xy * mag, disp.z) * o.vertex.w * outlineEnabled;

    // Push Z back to avoid z-fighting when scaled very small. This is not legit,
    // mathematically speaking and likely causes crazy surface derivitives.
    o.vertex.z -= disp.z * o.vertex.w * outlineEnabled;

        o.color = v.color;
        o.color.a = 1;
        o.color.xyz += worldNormal.y *.2;
        o.color.xyz = max(0, o.color.xyz);
        o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
        return o;
  }

  v2f vert (appdata_t v)
  {
    return vertInflate(v,0);
  }

  v2f vertEdge (appdata_t v)
  {
    // v.color = TbVertToNative(v.color); no need
    return vertInflate(v, 1.0);
  }

  half4 fragBlack (v2f i) : SV_Target
  {
    float4 color = float4(0,0,0,1);
    return color;
  }

  half4 fragColor (v2f i) : SV_Target
  {
    return i.color;
  }

ENDHLSL


SubShader {
  // For exportManifest.json:
  //   GltfCull Back
  Cull Back
  Pass{
    HLSLPROGRAM
    #pragma vertex vert
    #pragma fragment fragColor
    ENDHLSL
    }

  Cull Front
  Pass{
    HLSLPROGRAM
    #pragma vertex vertEdge
    #pragma fragment fragBlack
    ENDHLSL
    }
  }
Fallback "Diffuse"
}
