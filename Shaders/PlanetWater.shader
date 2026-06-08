Shader "Teraria/Planet Water"
{
    Properties
    {
        _Color("Water Color", Color) = (0.03, 0.28, 0.55, 0.48)
        _DeepColor("Deep Water Color", Color) = (0.00, 0.04, 0.18, 0.75)
        _WaveStrength("Wave Strength", Range(0,0.25)) = 0.035
        _WaveScale("Wave Scale", Range(0.1,20)) = 5
        _WaveSpeed("Wave Speed", Range(0,5)) = 0.8
        _DepthFade("Depth Fade", Range(0.01,10)) = 2.5
        _Transparency("Transparency", Range(0,1)) = 0.52
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Back
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard alpha:fade vertex:vert
        #pragma target 3.0
        #include "UnityCG.cginc"

        fixed4 _Color;
        fixed4 _DeepColor;
        half _WaveStrength;
        half _WaveScale;
        half _WaveSpeed;
        half _DepthFade;
        half _Transparency;

        UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
            float4 screenPos;
        };

        void vert(inout appdata_full v)
        {
            float3 n = normalize(v.vertex.xyz);
            float waveA = sin((n.x + n.z) * _WaveScale + _Time.y * _WaveSpeed);
            float waveB = cos((n.y - n.x) * _WaveScale * 1.73 - _Time.y * _WaveSpeed * 1.37);
            v.vertex.xyz += n * (waveA + waveB) * _WaveStrength;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            half fresnel = pow(1.0 - saturate(dot(normalize(IN.worldNormal), normalize(UnityWorldSpaceViewDir(IN.worldPos)))), 3.0);
            float sceneDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(IN.screenPos)));
            float waterDepth = IN.screenPos.w;
            half depthCue = saturate((sceneDepth - waterDepth) / max(_DepthFade, 0.001));
            fixed3 water = lerp(_Color.rgb, _DeepColor.rgb, depthCue);
            o.Albedo = water;
            o.Emission = water * fresnel * 0.18;
            o.Smoothness = 0.88;
            o.Metallic = 0;
            o.Alpha = saturate(_Transparency + fresnel * 0.22);
        }
        ENDCG
    }
    FallBack "Transparent/Diffuse"
}
