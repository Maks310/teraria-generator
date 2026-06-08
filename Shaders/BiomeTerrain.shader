Shader "Teraria/Biome Terrain"
{
    Properties
    {
        _BiomeTex0("Biome 0 Albedo", 2D) = "white" {}
        _BiomeTex1("Biome 1 Albedo", 2D) = "white" {}
        _BiomeTex2("Biome 2 Albedo", 2D) = "white" {}
        _BiomeTex3("Biome 3 Albedo", 2D) = "white" {}
        _BiomeTint0("Biome 0 Tint", Color) = (0.78, 0.67, 0.42, 1)
        _BiomeTint1("Biome 1 Tint", Color) = (0.28, 0.48, 0.18, 1)
        _BiomeTint2("Biome 2 Tint", Color) = (0.58, 0.62, 0.54, 1)
        _BiomeTint3("Biome 3 Tint", Color) = (0.77, 0.61, 0.31, 1)
        _BiomeSmoothness0("Biome 0 Smoothness", Range(0,1)) = 0.25
        _BiomeSmoothness1("Biome 1 Smoothness", Range(0,1)) = 0.25
        _BiomeSmoothness2("Biome 2 Smoothness", Range(0,1)) = 0.25
        _BiomeSmoothness3("Biome 3 Smoothness", Range(0,1)) = 0.25
        _BiomeScale0("Biome 0 Texture Scale", Range(0.1,64)) = 12
        _BiomeScale1("Biome 1 Texture Scale", Range(0.1,64)) = 12
        _BiomeScale2("Biome 2 Texture Scale", Range(0.1,64)) = 12
        _BiomeScale3("Biome 3 Texture Scale", Range(0.1,64)) = 12
        _RockTex("Rock Texture", 2D) = "gray" {}
        _RockTint("Rock Tint", Color) = (0.34, 0.33, 0.31, 1)
        _RockScale("Rock Texture Scale", Range(0.1,64)) = 18
        _SlopeRockStart("Rock Slope Start", Range(0,1)) = 0.35
        _SlopeRockEnd("Rock Slope End", Range(0,1)) = 0.72
        _ShoreTex("Shore Texture", 2D) = "white" {}
        _ShoreTint("Shore Tint", Color) = (0.82, 0.74, 0.48, 1)
        _ShoreScale("Shore Texture Scale", Range(0.1,64)) = 16
        _HeightBlendStrength("Height Texture Blend", Range(0,1)) = 0.35
        _CoastStrength("Shore Blend Strength", Range(0,1)) = 0.9
        _RiverTint("River Bed Tint", Color) = (0.02, 0.18, 0.11, 1)
        _DebugClimate("Debug Climate Maps", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _BiomeTex0;
        sampler2D _BiomeTex1;
        sampler2D _BiomeTex2;
        sampler2D _BiomeTex3;
        sampler2D _RockTex;
        sampler2D _ShoreTex;

        fixed4 _BiomeTint0;
        fixed4 _BiomeTint1;
        fixed4 _BiomeTint2;
        fixed4 _BiomeTint3;
        fixed4 _RockTint;
        fixed4 _ShoreTint;
        fixed4 _RiverTint;

        half _BiomeSmoothness0;
        half _BiomeSmoothness1;
        half _BiomeSmoothness2;
        half _BiomeSmoothness3;
        half _BiomeScale0;
        half _BiomeScale1;
        half _BiomeScale2;
        half _BiomeScale3;
        half _RockScale;
        half _ShoreScale;
        half _SlopeRockStart;
        half _SlopeRockEnd;
        half _HeightBlendStrength;
        half _CoastStrength;
        half _DebugClimate;

        struct Input
        {
            float2 uv_BiomeTex0;
            float4 color : COLOR;
            float4 uv2_BiomeTex0;
            float4 uv3_BiomeTex0;
        };

        fixed3 SampleBiomeLayer(sampler2D tex, fixed4 tint, float2 uv, half scale, half heightMask)
        {
            fixed3 baseColor = tex2D(tex, uv * scale).rgb * tint.rgb;
            fixed3 highColor = lerp(baseColor, fixed3(0.86, 0.86, 0.82), saturate((heightMask - 0.72) * 3.5));
            fixed3 lowColor = lerp(baseColor, fixed3(0.24, 0.18, 0.10), saturate((0.22 - heightMask) * 4.0));
            return lerp(lerp(baseColor, lowColor, _HeightBlendStrength), highColor, _HeightBlendStrength * saturate((heightMask - 0.55) * 2.2));
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            half heightMask = saturate(IN.color.r);
            half slopeMask = saturate(IN.color.g);
            half coastMask = saturate(IN.color.b);
            half riverMask = saturate(IN.color.a);
            half temperature = saturate(IN.uv2_BiomeTex0.x);
            half moisture = saturate(IN.uv2_BiomeTex0.y);
            half4 weights = saturate(IN.uv3_BiomeTex0);
            weights /= max(0.0001, weights.x + weights.y + weights.z + weights.w);

            fixed3 biome0 = SampleBiomeLayer(_BiomeTex0, _BiomeTint0, IN.uv_BiomeTex0, _BiomeScale0, heightMask);
            fixed3 biome1 = SampleBiomeLayer(_BiomeTex1, _BiomeTint1, IN.uv_BiomeTex0, _BiomeScale1, heightMask);
            fixed3 biome2 = SampleBiomeLayer(_BiomeTex2, _BiomeTint2, IN.uv_BiomeTex0, _BiomeScale2, heightMask);
            fixed3 biome3 = SampleBiomeLayer(_BiomeTex3, _BiomeTint3, IN.uv_BiomeTex0, _BiomeScale3, heightMask);
            fixed3 biomeColor = biome0 * weights.x + biome1 * weights.y + biome2 * weights.z + biome3 * weights.w;

            fixed3 rockColor = tex2D(_RockTex, IN.uv_BiomeTex0 * _RockScale).rgb * _RockTint.rgb;
            half rockMask = smoothstep(_SlopeRockStart, _SlopeRockEnd, slopeMask);
            biomeColor = lerp(biomeColor, rockColor, rockMask);

            fixed3 shoreColor = tex2D(_ShoreTex, IN.uv_BiomeTex0 * _ShoreScale).rgb * _ShoreTint.rgb;
            biomeColor = lerp(biomeColor, shoreColor, coastMask * _CoastStrength);
            biomeColor = lerp(biomeColor, _RiverTint.rgb, riverMask * 0.45);

            fixed3 climateDebug = fixed3(temperature, moisture, heightMask);
            o.Albedo = lerp(biomeColor, climateDebug, _DebugClimate);
            o.Smoothness = saturate(_BiomeSmoothness0 * weights.x + _BiomeSmoothness1 * weights.y + _BiomeSmoothness2 * weights.z + _BiomeSmoothness3 * weights.w + rockMask * 0.15 + coastMask * 0.18);
            o.Metallic = 0;
        }
        ENDCG
    }
    FallBack "Teraria/Planet Terrain Debug"
}
