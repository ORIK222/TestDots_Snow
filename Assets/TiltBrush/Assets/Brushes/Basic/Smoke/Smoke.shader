Shader "Brush/Particle/Smoke" {
Properties {
  _TintColor ("Tint Color", Color) = (0.5,0.5,0.5,0.5)
  _MainTex ("Particle Texture", 2D) = "white" {}
  _ScrollRate("Scroll Rate", Float) = 1.0
}

Category {
  Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "DisableBatching"="True" }
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
      #include "../../../Shaders/Include/Brush.hlsl"
      #include "../../../Shaders/Include/Particles.hlsl"
      #include "Assets/ThirdParty/Noise/Shaders/Noise.hlsl"
      
CBUFFER_START(UnityPerMaterial)
      sampler2D _MainTex;
      half4 _TintColor;
      float4 _MainTex_ST;
      float _ScrollRate;
      CBUFFER_END
      struct v2f {
        float4 vertex : SV_POSITION;
        half4 color : COLOR;
        float2 texcoord : TEXCOORD0;
      };
      
      float dist_from_line(float3 line_dir, float3 point_on_line, float3 pos) {
        float3 point_to_line = pos - point_on_line;
        float3 dist_along_line = dot(point_to_line, line_dir);
        float3 closest_point_on_line = dist_along_line * line_dir + point_on_line;
        return length(closest_point_on_line - pos);
      }
      float dist_from_line_repeating(float3 line_dir, float3 point_on_line, float3 pos) {
        float3 point_to_line = pos - point_on_line;
        float3 dist_along_line = dot(point_to_line, line_dir);
        float3 closest_point_on_line = dist_along_line * line_dir + point_on_line;
        return length( sin(closest_point_on_line - pos));
      }
      float4 dist_from_plane(float3 plane_normal, float3 point_on_plane, float3 pos) {
        float dist = dot(plane_normal, pos - point_on_plane);
        float3 closest_point_on_plane = pos - dist * plane_normal;
        return float4(closest_point_on_plane.xyz, abs(dist));
      }
 v2f vert (ParticleVertex_t v)
      {
        v.color = TbVertToSrgb(v.color);
        v2f o;
        float birthTime = v.texcoord.w;
        float rotation = v.texcoord.z;
        float halfSize = GetParticleHalfSize(v.corner.xyz, v.center, birthTime);
        float4 center = float4(v.center.xyz, 1);
        float4 center_WS = mul(unity_ObjectToWorld, center);

        float t = _Time.y*_ScrollRate + v.color.a * 10;
        float time = _Time.x * 5;
        float d = 30;
        float freq = .1;
        float3 disp = float3(1,0,0) * curlX(center_WS.xyz * freq + time, d);
        disp += float3(0,1,0) * curlY(center_WS.xyz * freq +time, d);
        disp += float3(0,0,1) * curlZ(center_WS.xyz * freq + time, d);
        disp = disp * 5 * kDecimetersToWorldUnits;

        center_WS.xyz += mul(xf_CS, float4(disp, 0)).xyz;

        const float3 corner = OrientParticle_WS(center_WS.xyz, halfSize, v.vid, rotation).xyz;
        o.vertex.xyz = TransformObjectToWorld (corner);
        o.vertex.xyz = TransformWorldToView(o.vertex.xyz);
        o.vertex = mul(UNITY_MATRIX_P, float4(o.vertex.xyz, 1.0f));

        o.color = v.color;
        o.texcoord = TRANSFORM_TEX(v.texcoord.xy,_MainTex);

        return o;
      }

      half4 frag (v2f i) : SV_Target
      {
        float4 c =  tex2D(_MainTex, i.texcoord);
        c *= i.color * _TintColor;
        c = SrgbToNative(c);
        return c;
      }
      ENDHLSL
    }
  }
}
}
