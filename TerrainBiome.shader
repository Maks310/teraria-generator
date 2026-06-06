Shader "Custom/TerrainBiome"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Glossiness ("Smoothness", Range(0,1)) = 0.2
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _ColorIntensity ("Color Intensity", Range(0.5, 2)) = 1.0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;
        half _Glossiness;
        half _Metallic;
        half _ColorIntensity;

        struct Input
        {
            float2 uv_MainTex;
            float4 vertexColor : COLOR;
            float3 worldPos;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.vertexColor = v.color;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Основний колір з вершинних кольорів (біоми)
            fixed4 biomeColor = IN.vertexColor * _ColorIntensity;

            // Можна додати текстуру для деталізації
            fixed4 texColor = tex2D(_MainTex, IN.uv_MainTex * 100); // Тайлинг

            // Комбінуємо
            o.Albedo = biomeColor.rgb * texColor.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1.0;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
