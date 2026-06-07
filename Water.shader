Shader "Custom/SeamlessSurvivalWater"
{
    Properties
    {
        _Color ("Shallow Color", Color) = (0.08, 0.40, 0.44, 0.72)
        _DeepColor ("Deep Color", Color) = (0.02, 0.08, 0.16, 0.86)
        _WaveOffset ("Wave Offset", Float) = 0
        _WaveScale ("Wave Count", Float) = 6
        _WaveHeight ("Wave Height", Float) = 0.45
        _WorldSize ("World Size", Float) = 2048
        _FoamStrength ("Foam Strength", Range(0,1)) = 0.16
        _Glossiness ("Smoothness", Range(0,1)) = 0.86
        _Metallic ("Metallic", Range(0,1)) = 0
        _FresnelPower ("Fresnel Power", Range(1,10)) = 4
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 250
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
        float _WorldSize;
        float _FoamStrength;
        half _Glossiness;
        half _Metallic;
        half _FresnelPower;

        struct Input
        {
            float3 worldPos;
            float3 viewDir;
            float3 worldNormal;
        };

        float SeamlessWave(float2 worldXZ, float phase)
        {
            float2 angle = worldXZ / max(_WorldSize, 1.0) * 6.2831853;
            float a = sin(angle.x * _WaveScale + phase) * cos(angle.y * (_WaveScale * 0.7) + phase * 0.8);
            float b = sin(angle.x * (_WaveScale * 1.7) - phase * 1.1) * cos(angle.y * (_WaveScale * 1.3) + phase);
            return a + b * 0.5;
        }

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            float wave = SeamlessWave(worldPos.xz, _WaveOffset);
            v.vertex.y += wave * _WaveHeight;

            float eps = max(_WorldSize / 512.0, 0.25);
            float waveX = SeamlessWave(worldPos.xz + float2(eps, 0), _WaveOffset);
            float waveZ = SeamlessWave(worldPos.xz + float2(0, eps), _WaveOffset);
            float dx = (waveX - wave) * _WaveHeight / eps;
            float dz = (waveZ - wave) * _WaveHeight / eps;
            v.normal = normalize(float3(-dx, 1, -dz));
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float fresnel = pow(1.0 - saturate(dot(normalize(IN.viewDir), normalize(IN.worldNormal))), _FresnelPower);
            float ripple = SeamlessWave(IN.worldPos.xz, _WaveOffset * 1.35) * 0.5 + 0.5;
            fixed4 finalColor = lerp(_DeepColor, _Color, saturate(fresnel + ripple * 0.18));
            finalColor.rgb += ripple * _FoamStrength;

            o.Albedo = finalColor.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = finalColor.a;
        }
        ENDCG
    }

    FallBack "Transparent/Diffuse"
}
