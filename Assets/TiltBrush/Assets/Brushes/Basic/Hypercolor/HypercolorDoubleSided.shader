Shader "Brush/Special/HypercolorDoubleSided" {
Properties {
  _Color ("Main Color", Color) = (1,1,1,1)
  _SpecColor ("Specular Color", Color) = (0.5, 0.5, 0.5, 0)
  _Shininess ("Shininess", Range (0.01, 1)) = 0.078125
  _MainTex ("Base (RGB) TransGloss (A)", 2D) = "white" {}
  _BumpMap ("Normalmap", 2D) = "bump" {}
  _Cutoff ("Alpha cutoff", Range(0,1)) = 0.5
}
    SubShader {
    Tags {}
    Pass{
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

    struct appdata
    {
    float4 vertex : POSITION;
    float4 tangent : TANGENT;
    float3 normal : NORMAL;
    float4 texcoord : TEXCOORD0;
    float4 texcoord1 : TEXCOORD1;
    float4 texcoord2 : TEXCOORD2;
    float4 texcoord3 : TEXCOORD3;
    half4 color : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};
    struct Input {
      float2 uv_MainTex;
      float2 uv_BumpMap;
      float4 color : Color;
      float3 worldPos;
    };
struct v2f {
    float4 pos : SV_POSITION;
  float4 pack0 : TEXCOORD0; // _MainTex _BumpMap
  float3 tSpace0 : TEXCOORD1;
  float3 tSpace1 : TEXCOORD2;
  float3 tSpace2 : TEXCOORD3;
  half4 color : COLOR0;
};
    CBUFFER_START(UnityPerMaterial)
    sampler2D _MainTex;
    float4 _MainTex_ST;
    sampler2D _BumpMap;
    float4 _BumpMap_ST;
    half4 _Color;
    half _Shininess;
    CBUFFER_END
    
        v2f vert (appdata v) {
          v2f o;
          o.pos = TransformObjectToHClip(v.vertex.xyz);
          o.pack0.xy = TRANSFORM_TEX(v.texcoord, _MainTex);
          o.pack0.zw = TRANSFORM_TEX(v.texcoord, _BumpMap);
          o.color = TbVertToSrgb(v.color);
          half3 wNormal = TransformObjectToWorldNormal(v.normal);
          half3 wTangent = TransformObjectToWorldDir(v.tangent.xyz);
          half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
          half3 wBitangent = cross(wNormal, wTangent) * tangentSign;
          o.tSpace0 = float3(wTangent.x, wBitangent.x, wNormal.x);
          o.tSpace1 = float3(wTangent.y, wBitangent.y, wNormal.y);
          o.tSpace2 = float3(wTangent.z, wBitangent.z, wNormal.z);
          float t = 0.0;
#ifdef AUDIO_REACTIVE
          t = _BeatOutputAccum.z * 5;
          float waveIntensity = _BeatOutput.z * .1 * strokeWidth;
          o.pos.xyz += (pow(1 - (sin(t + v.texcoord.x * 5 + v.texcoord.y * 10) + 1), 2)
              * cross(v.tangent.xyz, v.normal.xyz)
              * waveIntensity);
#endif
          return o; 
        }

          half4 frag(v2f i, half vface : VFACE) : SV_Target{
          half4 tex =  tex2D(_MainTex, i.pack0.xy);
          //col.a = tex2D(_MainTex, i.pack0.xy).a * col.a;
          half3 tnormal = UnpackNormal(tex2D(_BumpMap, i.pack0.zw));
          tnormal.z *= vface;
          float scroll = _Time.z;
          half3 worldNormal;
          worldNormal.x = dot(i.tSpace0, tnormal);
          worldNormal.y = dot(i.tSpace1, tnormal);
          worldNormal.z = dot(i.tSpace2, tnormal);
          tex.rgb =  float3(1,0,0) * (sin(tex.r * 2 + scroll*0.5 - i.pack0.xy.x) + 1) * 2;
          tex.rgb += float3(0,1,0) * (sin(tex.r * 3.3 + scroll*1 - i.pack0.xy.x) + 1) * 2;
          tex.rgb += float3(0,0,1) * (sin(tex.r * 4.66 + scroll*0.25 - i.pack0.xy.x) + 1) * 2;
          //half ndotl = saturate(dot(worldNormal, normalize(float3(1,1,1).xyz)));
          //half3 lighting = ndotl * 0.9;
          half4 col;
          col.rgb = SrgbToNative(tex * i.color).rgb;
          col.a = tex.a * i.color.a;

          //col.rgb *= lighting;
          //col.rgb = SrgbToNative(col).rgb;
          //col.rgb = tex2D(_MainTex, i.pack0.xy);
          //col = tex;
          return col;
        }
    ENDHLSL
    }
}
  FallBack "Transparent/Cutout/VertexLit"
}



