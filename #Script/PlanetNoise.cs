using UnityEngine;

[System.Serializable]
public struct PlanetNoiseSample
{
    public float continentalMask;
    public float heightNoise;
    public float ridgeMask;
    public float domainWarp;
    public float climateNoise;
    public float volcanicMask;
    public float crystalMask;
    public float magneticMask;
    public float ruinMask;
    public float meteorMask;
    public float floatingIslandMask;
    public float caveMask;
    public float anomalyMask;
}

[System.Serializable]
public class PlanetNoiseSettings
{
    public int seed = 42;
    public float continentScale = 1.15f;
    public float terrainScale = 5.2f;
    public float ridgeScale = 8.5f;
    public float climateScale = 2.1f;
    [Range(0f, 0.5f)] public float domainWarpStrength = 0.16f;
    [Range(1, 9)] public int terrainOctaves = 5;
    [Range(0f, 1f)] public float persistence = 0.48f;
    public float lacunarity = 2f;
}

public static class PlanetNoise
{
    public static PlanetNoiseSample Sample(Vector3 direction, PlanetNoiseSettings settings)
    {
        if (settings == null)
        {
            settings = new PlanetNoiseSettings();
        }

        direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.up;
        Vector3 warpedDirection = DomainWarp(direction, settings);

        PlanetNoiseSample sample = new PlanetNoiseSample();
        sample.domainWarp = Vector3.Angle(direction, warpedDirection) / 180f;
        sample.continentalMask = FractalSphereNoise(warpedDirection, settings.continentScale, 4, 0.58f, 2.03f, settings.seed + 11);
        sample.heightNoise = FractalSphereNoise(warpedDirection, settings.terrainScale, settings.terrainOctaves, settings.persistence, settings.lacunarity, settings.seed + 101);
        sample.ridgeMask = RidgedFractalSphereNoise(warpedDirection, settings.ridgeScale, 4, 0.52f, 2.1f, settings.seed + 211);
        sample.climateNoise = FractalSphereNoise(warpedDirection, settings.climateScale, 3, 0.55f, 2f, settings.seed + 307);
        sample.volcanicMask = Anomaly(warpedDirection, 3.4f, settings.seed + 401);
        sample.crystalMask = Anomaly(warpedDirection, 4.1f, settings.seed + 503);
        sample.magneticMask = Anomaly(warpedDirection, 5.2f, settings.seed + 607);
        sample.ruinMask = Anomaly(warpedDirection, 2.7f, settings.seed + 709);
        sample.meteorMask = Anomaly(warpedDirection, 6.3f, settings.seed + 811);
        sample.floatingIslandMask = Anomaly(warpedDirection + Vector3.one * 0.07f, 5.8f, settings.seed + 907);
        sample.caveMask = RidgedFractalSphereNoise(warpedDirection, 12.5f, 3, 0.5f, 2.2f, settings.seed + 1009);
        sample.anomalyMask = Mathf.Max(sample.volcanicMask, sample.crystalMask, sample.magneticMask, sample.ruinMask, sample.meteorMask, sample.floatingIslandMask);
        return sample;
    }

    public static float BuildHeight(PlanetNoiseSample sample, float waterLevel, float mountainAmount, float oceanDepth)
    {
        float continents = NoiseGenerator.SmoothStep(1f - Mathf.Clamp01(waterLevel) - 0.18f, 1f - Mathf.Clamp01(waterLevel) + 0.18f, sample.continentalMask);
        float baseLand = Mathf.Lerp(-Mathf.Clamp01(oceanDepth), 0.36f, continents);
        float detail = (sample.heightNoise - 0.5f) * Mathf.Lerp(0.05f, 0.22f, continents);
        float ridges = Mathf.Pow(Mathf.Clamp01(sample.ridgeMask), 2.2f) * Mathf.Clamp01(mountainAmount) * continents;
        float volcano = Mathf.Pow(sample.volcanicMask, 4f) * 0.16f;
        float floating = Mathf.Pow(sample.floatingIslandMask, 5f) * 0.2f;
        return Mathf.Clamp(baseLand + detail + ridges + volcano + floating + 0.38f, 0f, 1f);
    }

    public static Vector3 DomainWarp(Vector3 direction, PlanetNoiseSettings settings)
    {
        float strength = settings != null ? settings.domainWarpStrength : 0f;
        if (strength <= 0f)
        {
            return direction.normalized;
        }

        int seed = settings.seed;
        Vector3 warp = new Vector3(
            SignedNoise(direction, 2.7f, seed + 17),
            SignedNoise(direction, 2.9f, seed + 29),
            SignedNoise(direction, 3.1f, seed + 43));
        return (direction + warp * strength).normalized;
    }

    public static float FractalSphereNoise(Vector3 direction, float scale, int octaves, float persistence, float lacunarity, int seed)
    {
        float total = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float maxValue = 0f;
        for (int i = 0; i < Mathf.Max(1, octaves); i++)
        {
            total += SphereNoise(direction, scale * frequency, seed + i * 131) * amplitude;
            maxValue += amplitude;
            amplitude *= Mathf.Clamp01(persistence);
            frequency *= Mathf.Max(1.01f, lacunarity);
        }
        return maxValue <= 0f ? 0f : Mathf.Clamp01(total / maxValue);
    }

    public static float RidgedFractalSphereNoise(Vector3 direction, float scale, int octaves, float persistence, float lacunarity, int seed)
    {
        float n = FractalSphereNoise(direction, scale, octaves, persistence, lacunarity, seed);
        return 1f - Mathf.Abs(n * 2f - 1f);
    }

    public static float SphereNoise(Vector3 direction, float scale, int seed)
    {
        scale = Mathf.Max(0.0001f, scale);
        float offset = seed * 23.173f;
        float xy = Mathf.PerlinNoise(direction.x * scale + offset, direction.y * scale - offset);
        float yz = Mathf.PerlinNoise(direction.y * scale + offset * 0.37f, direction.z * scale + offset * 0.61f);
        float zx = Mathf.PerlinNoise(direction.z * scale - offset * 0.29f, direction.x * scale + offset * 0.13f);
        return Mathf.Clamp01((xy + yz + zx) / 3f);
    }

    private static float SignedNoise(Vector3 direction, float scale, int seed)
    {
        return SphereNoise(direction, scale, seed) * 2f - 1f;
    }

    private static float Anomaly(Vector3 direction, float scale, int seed)
    {
        float broad = FractalSphereNoise(direction, scale, 3, 0.55f, 2.1f, seed);
        float local = RidgedFractalSphereNoise(direction, scale * 3.3f, 2, 0.5f, 2f, seed + 71);
        return Mathf.Clamp01(broad * 0.72f + local * 0.28f);
    }
}
