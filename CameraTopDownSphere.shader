Shader "Custom/CameraTopDownSphere"
{
    Properties
    {
        _MainTex ("Top Down Camera Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Exposure ("Exposure", Range(0.1, 3)) = 1
        _ProjectionMode ("Projection Mode 0=LatLong 1=Planar", Range(0, 1)) = 0
        _SeamBlend ("Longitude Seam Blend", Range(0, 0.05)) = 0.01
        _PolarFade ("Planar Polar Fade", Range(0, 0.5)) = 0.18
        _BackgroundColor ("Back/Polar Fill", Color) = (0.01, 0.04, 0.08, 1)
        _Glossiness ("Smoothness", Range(0, 1)) = 0.35
        _Metallic ("Metallic", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 250

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        fixed4 _BackgroundColor;
        half _Glossiness;
        half _Metallic;
        float _Exposure;
        float _ProjectionMode;
        float _SeamBlend;
        float _PolarFade;

        struct Input
        {
            float2 uv_MainTex;
            float3 objectDir;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.objectDir = normalize(v.vertex.xyz);
        }

        float2 DirectionToLatLongUv(float3 dir)
        {
            const float invTwoPi = 0.15915494309;
            const float invPi = 0.31830988618;
            float longitude = atan2(dir.z, dir.x) * invTwoPi + 0.5;
            float latitude = asin(clamp(dir.y, -1.0, 1.0)) * invPi + 0.5;
            return float2(longitude, latitude);
        }

        float2 DirectionToPlanarTopDownUv(float3 dir)
        {
            return dir.xz * 0.5 + 0.5;
        }

        fixed3 SampleLatLongWithSeam(float2 uv)
        {
            fixed3 color = tex2D(_MainTex, uv).rgb;
            float blendWidth = max(_SeamBlend, 0.0001);
            float leftBlend = saturate((blendWidth - uv.x) / blendWidth);
            float rightBlend = saturate((uv.x - (1.0 - blendWidth)) / blendWidth);

            fixed3 wrappedRight = tex2D(_MainTex, float2(uv.x + 1.0, uv.y)).rgb;
            fixed3 wrappedLeft = tex2D(_MainTex, float2(uv.x - 1.0, uv.y)).rgb;
            color = lerp(color, wrappedRight, leftBlend);
            color = lerp(color, wrappedLeft, rightBlend);
            return color;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float3 dir = normalize(IN.objectDir);
            float2 latLongUv = DirectionToLatLongUv(dir);
            float2 planarUv = DirectionToPlanarTopDownUv(dir);

            fixed3 latLongColor = SampleLatLongWithSeam(latLongUv);
            fixed3 planarColor = tex2D(_MainTex, planarUv).rgb;
            float planarCoverage = smoothstep(0.0, max(_PolarFade, 0.0001), length(dir.xz));
            planarColor = lerp(_BackgroundColor.rgb, planarColor, planarCoverage);

            fixed3 mapColor = lerp(latLongColor, planarColor, step(0.5, _ProjectionMode));
            mapColor *= _Color.rgb * _Exposure;

            o.Albedo = saturate(mapColor);
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = 1;
        }
        ENDCG
    }

    FallBack "Diffuse"
}
