Shader "Custom/Water"
{
    Properties
    {
        _Color ("Water Color", Color) = (0.1, 0.4, 0.6, 0.8)
        _DeepColor ("Deep Water Color", Color) = (0.02, 0.1, 0.2, 0.9)
        _WaveOffset ("Wave Offset", Float) = 0
        _WaveScale ("Wave Scale", Float) = 0.1
        _WaveHeight ("Wave Height", Float) = 0.5
        _Glossiness ("Smoothness", Range(0,1)) = 0.9
        _Metallic ("Metallic", Range(0,1)) = 0.0
        _FresnelPower ("Fresnel Power", Range(1, 10)) = 3
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        CGPROGRAM
        #pragma surface surf Standard alpha:fade vertex:vert
        #pragma target 3.0

        fixed4 _Color;
        fixed4 _DeepColor;
        float _WaveOffset;
        float _WaveScale;
        float _WaveHeight;
        half _Glossiness;
        half _Metallic;
        half _FresnelPower;

        struct Input
        {
            float3 worldPos;
            float3 viewDir;
            float3 worldNormal;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);

            // Анімація хвиль у вершинному шейдері
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;

            float wave1 = sin(worldPos.x * _WaveScale + _WaveOffset) *
                          cos(worldPos.z * _WaveScale * 0.7 + _WaveOffset * 0.8);
            float wave2 = sin(worldPos.x * _WaveScale * 1.3 + _WaveOffset * 1.2) *
                          cos(worldPos.z * _WaveScale * 1.1 + _WaveOffset);

            v.vertex.y += (wave1 + wave2 * 0.5) * _WaveHeight;

            // Оновлюємо нормалі для правильного освітлення
            float dx = cos(worldPos.x * _WaveScale + _WaveOffset) * _WaveScale * _WaveHeight;
            float dz = -sin(worldPos.z * _WaveScale * 0.7 + _WaveOffset * 0.8) * _WaveScale * 0.7 * _WaveHeight;
            v.normal = normalize(float3(-dx, 1, -dz));
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Fresnel для реалістичності
            float fresnel = pow(1.0 - saturate(dot(IN.viewDir, IN.worldNormal)), _FresnelPower);

            // Колір залежно від кута огляду
            fixed4 finalColor = lerp(_DeepColor, _Color, fresnel);

            o.Albedo = finalColor.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = finalColor.a;
        }
        ENDCG
    }

    FallBack "Transparent/Diffuse"
}
