
Shader "Brush/Bloom" {
Properties {
  _MainTex ("Particle Texture", 2D) = "white" {}
  _EmissionGain ("Emission Gain", Range(0, 1)) = 0.5
  _Shininess ("Shininess", Range (0.01, 1)) = 0.078125
}

Category {
  Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
  Blend One One // SrcAlpha One
  BlendOp Add, Min
  AlphaTest Greater .01
  ColorMask RGBA
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
      CBUFFER_START(UnityPerMaterial)
      sampler2D _MainTex;
      float4 _MainTex_ST;
      float _EmissionGain;
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
        float2 texcoord : TEXCOORD0;
      };

      struct v2f {
        float4 vertex : POSITION;
        float4 color : COLOR;
        float2 texcoord : TEXCOORD0;
      };
          
      
      v2f vert (appdata_t v)
      {
        v.color = TbVertToSrgb(v.color);
        v2f o;
        o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
        o.color = bloomColor(v.color, _EmissionGain);
        o.vertex = TransformObjectToHClip(v.vertex.xyz);
        return o;
      }
      half4 frag (v2f i) : COLOR
      {
        float4 color = i.color * tex2D(_MainTex, i.texcoord);
        color = float4(color.rgb * color.a, 1.0);
        color = SrgbToNative(color);
        return float4(color.rgb, 1.0);
      }

      ENDHLSL
    }
  }
}
}
