Shader "Teraria/Planet Terrain Debug"
{
    Properties
    {
        _LowColor("Low Height", Color) = (0.10, 0.34, 0.12, 1)
        _MidColor("Mid Height", Color) = (0.38, 0.30, 0.15, 1)
        _HighColor("High Height", Color) = (0.82, 0.82, 0.78, 1)
        _SlopeColor("Slope Mask", Color) = (0.33, 0.31, 0.30, 1)
        _CoastColor("Coast Mask", Color) = (0.82, 0.74, 0.48, 1)
        _DebugHeight("Debug Height Colors", Range(0,1)) = 0
        _SlopeStrength("Slope Mask Strength", Range(0,1)) = 0.7
        _CoastStrength("Coast Mask Strength", Range(0,1)) = 0.8
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        fixed4 _LowColor;
        fixed4 _MidColor;
        fixed4 _HighColor;
        fixed4 _SlopeColor;
        fixed4 _CoastColor;
        half _DebugHeight;
        half _SlopeStrength;
        half _CoastStrength;

        struct Input
        {
            float4 color : COLOR;
        };

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            half heightMask = saturate(IN.color.r);
            half slopeMask = saturate(IN.color.g);
            half coastMask = saturate(IN.color.b);
            half riverMask = saturate(IN.color.a);

            fixed3 lowMid = lerp(_LowColor.rgb, _MidColor.rgb, smoothstep(0.15, 0.62, heightMask));
            fixed3 terrain = lerp(lowMid, _HighColor.rgb, smoothstep(0.62, 1.0, heightMask));
            terrain = lerp(terrain, _SlopeColor.rgb, slopeMask * _SlopeStrength);
            terrain = lerp(terrain, _CoastColor.rgb, coastMask * _CoastStrength);
            terrain = lerp(terrain, fixed3(0.02, 0.18, 0.11), riverMask * 0.5);

            fixed3 debug = fixed3(heightMask, slopeMask, coastMask);
            o.Albedo = lerp(terrain, debug, _DebugHeight);
            o.Smoothness = lerp(0.25, 0.55, slopeMask);
            o.Metallic = 0;
        }
        ENDCG
    }
    FallBack "Standard"
}
