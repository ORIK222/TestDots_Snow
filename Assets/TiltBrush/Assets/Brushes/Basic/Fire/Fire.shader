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

Shader "Brush/Special/Fire"
{
    Properties
    {
        _MainTex ("Particle Texture", 2D) = "white" {}
        _Scroll1 ("Scroll1", Float) = 0
        _Scroll2 ("Scroll2", Float) = 0
        _DisplacementIntensity("Displacement", Float) = .1
        _EmissionGain ("Emission Gain", Range(0, 1)) = 0.5
        _Shininess ("Shininess", Range(0, 1)) = 0.5
    }

    Category
    {
        Tags
        {
            "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"
        }
        Blend One One // SrcAlpha One
        BlendOp Add, Min
        ColorMask RGBA
        Cull Off Lighting Off ZWrite Off Fog
        {
            Color (0,0,0,0)
        }

        SubShader
        {
            Pass
            {

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
                half _Scroll1;
                half _Scroll2;
                half _DisplacementIntensity;
                half _EmissionGain;
                CBUFFER_END

                 #ifdef UNITY_DOTS_INSTANCING_ENABLED  
                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(float, _Shininess)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
            #define _Shininess UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(float, _Shininess)
      #endif


                struct appdata_t
                {
                    float4 vertex : POSITION;
                    half4 color : COLOR;
                    float3 normal : NORMAL;
                    #if SHADER_TARGET >= 40
        centroid float2 texcoord : TEXCOORD0;
                    #else
                    float2 texcoord : TEXCOORD0;
                    #endif
                    float3 worldPos : TEXCOORD1;
                };

                struct v2f
                {
                    float4 vertex : POSITION;
                    float4 color : COLOR;
                    #if SHADER_TARGET >= 40
        centroid float2 texcoord : TEXCOORD0;
                    #else
                    float2 texcoord : TEXCOORD0;
                    #endif
                    float3 worldPos : TEXCOORD1;
                };


                v2f vert(appdata_t v)
                {
                    v2f o;
                    o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                    o.color = v.color;
                    o.vertex = TransformObjectToHClip(v.vertex.xyz);
                    o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                    return o;
                }

                // Note: input color is srgb
                half4 frag(v2f i) : COLOR
                {
                    half2 displacement;

                    displacement = tex2D(_MainTex, i.texcoord + half2(-_Time.x * _Scroll1, 0)).a;

                    half4 tex = tex2D(
                        _MainTex, i.texcoord + half2(-_Time.x * _Scroll2, 0) - displacement * _DisplacementIntensity);
                    #ifdef AUDIO_REACTIVE
        tex = tex * .5 + 2 * procedural_line * ( envelope * envelopeHalf);
                    #endif
                    float4 color = i.color * tex;
                    color = float4(color.rgb * color.a, 1.0);
                    return color;
                }
                ENDHLSL
            }
        }
    }
}