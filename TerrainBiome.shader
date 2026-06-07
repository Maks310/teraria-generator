Shader "Custom/SurvivalTerrainBiome"
{
    Properties
    {
        _DetailStrength ("Detail Strength", Range(0, 0.35)) = 0.16
        _DetailScale ("Detail Scale", Float) = 75
        _WaterLevel ("Water Level", Float) = 50
        _ShallowWaterColor ("Shallow Water Color", Color) = (0.08, 0.40, 0.44, 0.72)
        _DeepWaterColor ("Deep Water Color", Color) = (0.02, 0.08, 0.16, 0.86)
        _WorldSize ("World Size", Float) = 2048
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        float _DetailStrength;
        float _DetailScale;
        float _WaterLevel;
        fixed4 _ShallowWaterColor;
        fixed4 _DeepWaterColor;
        float _WorldSize;

        struct Input
        {
            float4 color : COLOR;
            float3 worldPos;
        };

        float Hash21(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 tileUv = frac(IN.worldPos.xz / max(_WorldSize, 1.0));
            float rock = Hash21(floor(tileUv * _DetailScale));
            float pebble = Hash21(floor(tileUv * _DetailScale * 2.7 + 17.0));
            float detail = (rock * 0.65 + pebble * 0.35 - 0.5) * _DetailStrength;

            fixed3 baseColor = IN.color.rgb;
            float wetness = saturate((_WaterLevel - IN.worldPos.y) / 20.0);
            fixed3 underwaterTint = lerp(_ShallowWaterColor.rgb, _DeepWaterColor.rgb, wetness);
            baseColor = lerp(baseColor, underwaterTint, wetness * 0.35);

            o.Albedo = saturate(baseColor + detail);
            o.Smoothness = 0.22;
            o.Metallic = 0;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
