Shader "Custom/TerrainBiome"
{
    Properties
    {
        _MainTex ("Detail Texture", 2D) = "white" {}
        _TintStrength ("Biome Tint Strength", Range(0, 1)) = 0.78
        _DetailScale ("Procedural Detail Scale", Range(0.01, 2)) = 0.22
        _DetailStrength ("Procedural Detail Strength", Range(0, 1)) = 0.38
        _SlopeRockStart ("Slope Rock Start", Range(0, 1)) = 0.48
        _SlopeRockBlend ("Slope Rock Blend", Range(0.01, 1)) = 0.26
        _RiverWetness ("River Wetness", Range(0, 1)) = 0.55
        _GlobalSmoothness ("Global Smoothness", Range(0, 1)) = 0.28
        _GlobalMetallic ("Global Metallic", Range(0, 1)) = 0.0

        _PlainsColorA ("Plains Grass", Color) = (0.24, 0.52, 0.18, 1)
        _PlainsColorB ("Plains Dry Grass", Color) = (0.46, 0.58, 0.25, 1)
        _ForestColorA ("Forest Moss", Color) = (0.08, 0.30, 0.11, 1)
        _ForestColorB ("Forest Ground", Color) = (0.17, 0.22, 0.10, 1)
        _DesertColorA ("Desert Sand", Color) = (0.83, 0.69, 0.38, 1)
        _DesertColorB ("Desert Dunes", Color) = (0.98, 0.86, 0.55, 1)
        _TundraColorA ("Tundra Lichen", Color) = (0.48, 0.59, 0.53, 1)
        _TundraColorB ("Tundra Frost", Color) = (0.70, 0.78, 0.78, 1)
        _BeachColorA ("Beach Sand", Color) = (0.78, 0.69, 0.45, 1)
        _BeachColorB ("Wet Beach Sand", Color) = (0.50, 0.43, 0.30, 1)
        _MountainColorA ("Mountain Rock", Color) = (0.34, 0.34, 0.32, 1)
        _MountainColorB ("Mountain Highlight", Color) = (0.58, 0.56, 0.52, 1)
        _SnowColorA ("Snow", Color) = (0.88, 0.93, 0.96, 1)
        _SnowColorB ("Ice Shadow", Color) = (0.62, 0.76, 0.86, 1)
        _OceanColorA ("Shallow Ocean", Color) = (0.08, 0.38, 0.55, 1)
        _OceanColorB ("Deep Ocean", Color) = (0.01, 0.04, 0.14, 1)
        _RiverColor ("River Overlay", Color) = (0.05, 0.36, 0.72, 1)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;
        float _TintStrength;
        float _DetailScale;
        float _DetailStrength;
        float _SlopeRockStart;
        float _SlopeRockBlend;
        float _RiverWetness;
        half _GlobalSmoothness;
        half _GlobalMetallic;

        fixed4 _PlainsColorA;
        fixed4 _PlainsColorB;
        fixed4 _ForestColorA;
        fixed4 _ForestColorB;
        fixed4 _DesertColorA;
        fixed4 _DesertColorB;
        fixed4 _TundraColorA;
        fixed4 _TundraColorB;
        fixed4 _BeachColorA;
        fixed4 _BeachColorB;
        fixed4 _MountainColorA;
        fixed4 _MountainColorB;
        fixed4 _SnowColorA;
        fixed4 _SnowColorB;
        fixed4 _OceanColorA;
        fixed4 _OceanColorB;
        fixed4 _RiverColor;

        struct Input
        {
            float2 uv_MainTex;
            fixed4 vertexColor : COLOR;
            float3 worldPos;
            float3 worldNormal;
            float2 biomeData;
            float2 waterData;
        };

        float Hash21(float2 p)
        {
            p = frac(p * float2(123.34, 456.21));
            p += dot(p, p + 45.32);
            return frac(p.x * p.y);
        }

        float ValueNoise(float2 p)
        {
            float2 i = floor(p);
            float2 f = frac(p);
            float a = Hash21(i);
            float b = Hash21(i + float2(1, 0));
            float c = Hash21(i + float2(0, 1));
            float d = Hash21(i + float2(1, 1));
            float2 u = f * f * (3.0 - 2.0 * f);
            return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
        }

        float Fbm(float2 p)
        {
            float value = 0.0;
            float amplitude = 0.5;
            for (int i = 0; i < 4; i++)
            {
                value += ValueNoise(p) * amplitude;
                p *= 2.07;
                amplitude *= 0.5;
            }
            return value;
        }

        float StripeMask(float2 p, float noiseValue)
        {
            float dunes = sin((p.x * 0.65 + p.y * 0.22 + noiseValue * 2.0) * 6.28318);
            return smoothstep(-0.15, 0.85, dunes) * 0.55 + noiseValue * 0.45;
        }

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.vertexColor = v.color;
            o.biomeData = v.texcoord1.xy;
            o.waterData = v.texcoord2.xy;
        }

        fixed3 GetBiomePalette(float biomeId, float2 worldXZ, float noiseValue, float height01)
        {
            float id = floor(biomeId + 0.5);

            if (id == 1.0)
            {
                return lerp(_DesertColorA.rgb, _DesertColorB.rgb, StripeMask(worldXZ * _DetailScale * 0.55, noiseValue));
            }

            if (id == 2.0)
            {
                float frost = smoothstep(0.35, 0.85, noiseValue + height01 * 0.25);
                return lerp(_TundraColorA.rgb, _TundraColorB.rgb, frost);
            }

            if (id == 3.0)
            {
                return lerp(_OceanColorA.rgb, _OceanColorB.rgb, saturate(height01 + noiseValue * 0.12));
            }

            if (id == 4.0)
            {
                float wetSand = smoothstep(0.0, 0.7, noiseValue);
                return lerp(_BeachColorB.rgb, _BeachColorA.rgb, wetSand);
            }

            if (id == 5.0)
            {
                float leafLitter = smoothstep(0.25, 0.95, noiseValue);
                return lerp(_ForestColorA.rgb, _ForestColorB.rgb, leafLitter);
            }

            if (id == 6.0)
            {
                float strata = saturate(noiseValue * 0.65 + frac(worldXZ.y * _DetailScale * 0.9) * 0.35);
                return lerp(_MountainColorA.rgb, _MountainColorB.rgb, strata);
            }

            if (id == 7.0)
            {
                float ice = smoothstep(0.15, 0.95, noiseValue);
                return lerp(_SnowColorA.rgb, _SnowColorB.rgb, ice * 0.38);
            }

            return lerp(_PlainsColorA.rgb, _PlainsColorB.rgb, smoothstep(0.2, 0.9, noiseValue));
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float2 worldXZ = IN.worldPos.xz;
            float noiseValue = Fbm(worldXZ * _DetailScale);
            float fineNoise = Fbm(worldXZ * _DetailScale * 5.0);
            float biomeId = IN.biomeData.x;
            float height01 = saturate(IN.biomeData.y);
            float river = saturate(IN.waterData.x);
            float oceanDepth = saturate(IN.waterData.y);

            fixed3 proceduralColor = GetBiomePalette(biomeId, worldXZ, noiseValue, lerp(height01, oceanDepth, step(2.5, biomeId) * step(biomeId, 3.5)));
            fixed3 vertexTint = max(IN.vertexColor.rgb, 0.001);
            fixed3 detailTex = tex2D(_MainTex, IN.uv_MainTex * 96.0).rgb;

            float slope = 1.0 - saturate(IN.worldNormal.y);
            float rockMask = smoothstep(_SlopeRockStart, _SlopeRockStart + _SlopeRockBlend, slope);
            fixed3 slopeRock = lerp(_MountainColorA.rgb, _MountainColorB.rgb, fineNoise);

            fixed3 color = lerp(proceduralColor, proceduralColor * vertexTint * 1.35, _TintStrength);
            color *= lerp(1.0 - _DetailStrength * 0.45, 1.0 + _DetailStrength * 0.35, fineNoise);
            color *= lerp(1.0, detailTex * 1.15, _DetailStrength * 0.35);
            color = lerp(color, slopeRock, rockMask * step(0.5, biomeId) * (1.0 - step(6.5, biomeId)));
            color = lerp(color, _RiverColor.rgb, smoothstep(0.04, 0.85, river) * _RiverWetness);

            o.Albedo = saturate(color);
            o.Metallic = _GlobalMetallic;
            o.Smoothness = saturate(_GlobalSmoothness + river * 0.35 + oceanDepth * 0.25);
            o.Alpha = 1.0;
        }
        ENDCG
    }

    FallBack "Diffuse"
}