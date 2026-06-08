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
    public int plateId;
    public float plateBoundary;
    public float convergentBoundary;
    public float divergentBoundary;
    public float oceanicPlateMask;
    public float oceanBasinMask;
    public float continentalShelfMask;
    public float islandArcMask;
    public float tectonicMountainMask;
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

    [Header("Tectonics")]
    [Range(4, 96)] public int plateCount = 24;
    public int plateSeedOffset = 17031;
    [Range(0f, 1f)] public float plateJitter = 0.38f;
    [Range(0f, 1f)] public float oceanicPlateRatio = 0.58f;
    [Range(0.005f, 0.22f)] public float shelfWidth = 0.075f;
    [Range(0f, 1f)] public float islandArcStrength = 0.42f;
    [Range(0f, 1f)] public float boundaryMountainStrength = 0.72f;
    [Range(0f, 1f)] public float basinDepth = 0.68f;
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
        ApplyTectonics(direction, settings, ref sample);
        float continentalDetail = FractalSphereNoise(warpedDirection, settings.continentScale, 3, 0.5f, 2.03f, settings.seed + 11);
        sample.continentalMask = Mathf.Clamp01(sample.continentalMask + (continentalDetail - 0.5f) * 0.16f * (1f - sample.plateBoundary));
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
        waterLevel = Mathf.Clamp01(waterLevel);
        float depth = Mathf.Clamp01(oceanDepth);
        float continents = Mathf.Clamp01(sample.continentalMask);
        float oceanBasin = Mathf.Clamp01(sample.oceanBasinMask);
        float shelf = Mathf.Clamp01(sample.continentalShelfMask);
        float mountains = Mathf.Clamp01(sample.tectonicMountainMask);
        float islandArc = Mathf.Clamp01(sample.islandArcMask);

        float deepOceanHeight = waterLevel - Mathf.Lerp(0.18f, 0.42f, depth) * oceanBasin;
        float continentalHeight = waterLevel + Mathf.Lerp(0.05f, 0.22f, continents);
        float baseHeight = Mathf.Lerp(deepOceanHeight, continentalHeight, continents);
        baseHeight = Mathf.Lerp(baseHeight, waterLevel - 0.035f, shelf * (1f - continents));

        float detailStrength = Mathf.Lerp(0.025f, 0.115f, continents) * (1f - oceanBasin * 0.55f);
        float detail = (sample.heightNoise - 0.5f) * detailStrength;
        float erosionRidges = Mathf.Pow(Mathf.Clamp01(sample.ridgeMask), 2.4f) * Mathf.Clamp01(mountainAmount) * (0.035f + 0.08f * continents) * (1f - oceanBasin);
        float tectonicUplift = mountains * Mathf.Clamp01(mountainAmount) * 0.24f;
        float arcUplift = islandArc * Mathf.Lerp(0.04f, 0.14f, Mathf.Clamp01(mountainAmount));
        float riftDrop = Mathf.Clamp01(sample.divergentBoundary) * (1f - continents) * 0.055f;
        float volcano = Mathf.Pow(sample.volcanicMask, 4f) * Mathf.Lerp(0.035f, 0.11f, sample.convergentBoundary + islandArc);
        float floating = Mathf.Pow(sample.floatingIslandMask, 5f) * 0.2f;
        return Mathf.Clamp01(baseHeight + detail + erosionRidges + tectonicUplift + arcUplift + volcano + floating - riftDrop);
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

    private static void ApplyTectonics(Vector3 direction, PlanetNoiseSettings settings, ref PlanetNoiseSample sample)
    {
        int plateCount = Mathf.Clamp(settings.plateCount, 4, 96);
        int plateSeed = settings.seed + settings.plateSeedOffset;
        float nearestDistance = float.MaxValue;
        float secondDistance = float.MaxValue;
        int nearestId = 0;
        int secondId = 0;

        for (int i = 0; i < plateCount; i++)
        {
            Vector3 center = PlateCenter(i, plateCount, plateSeed, settings.plateJitter);
            float distance = Mathf.Acos(Mathf.Clamp(Vector3.Dot(direction, center), -1f, 1f));
            if (distance < nearestDistance)
            {
                secondDistance = nearestDistance;
                secondId = nearestId;
                nearestDistance = distance;
                nearestId = i;
            }
            else if (distance < secondDistance)
            {
                secondDistance = distance;
                secondId = i;
            }
        }

        bool nearestOceanic = IsOceanicPlate(nearestId, plateSeed, settings.oceanicPlateRatio);
        bool secondOceanic = IsOceanicPlate(secondId, plateSeed, settings.oceanicPlateRatio);
        float boundaryDistance = Mathf.Max(0f, secondDistance - nearestDistance);
        float boundaryWidth = Mathf.Max(0.006f, settings.shelfWidth * 0.85f);
        float boundary = 1f - Smooth01(boundaryDistance / boundaryWidth);
        float shelf = 1f - Smooth01(boundaryDistance / Mathf.Max(0.008f, settings.shelfWidth * 1.65f));
        bool mixedCrust = nearestOceanic != secondOceanic;
        bool bothOceanic = nearestOceanic && secondOceanic;
        bool bothContinental = !nearestOceanic && !secondOceanic;
        float pairMotion = Hash01(Mathf.Min(nearestId, secondId), Mathf.Max(nearestId, secondId), plateSeed + 4049);
        float convergenceChance = bothContinental ? 0.82f : (mixedCrust ? 0.74f : 0.56f);
        float convergent = boundary * (pairMotion < convergenceChance ? 1f : 0f);
        float divergent = boundary * (pairMotion >= convergenceChance ? 1f : 0f);
        float boundaryNoise = Hash01(nearestId, secondId, plateSeed + 8191) * 0.35f + 0.65f;

        sample.plateId = nearestId;
        sample.plateBoundary = boundary;
        sample.convergentBoundary = convergent;
        sample.divergentBoundary = divergent;
        sample.oceanicPlateMask = nearestOceanic ? 1f : 0f;
        sample.continentalMask = nearestOceanic ? 0f : 1f;
        sample.oceanBasinMask = nearestOceanic ? Mathf.Clamp01((1f - shelf) * settings.basinDepth) : 0f;
        sample.continentalShelfMask = mixedCrust ? shelf : 0f;
        sample.islandArcMask = Mathf.Clamp01(convergent * settings.islandArcStrength * boundaryNoise * (bothOceanic ? 1f : (mixedCrust && nearestOceanic ? 0.62f : 0.18f)));
        sample.tectonicMountainMask = Mathf.Clamp01(convergent * settings.boundaryMountainStrength * boundaryNoise * (bothContinental ? 1f : (mixedCrust ? 0.72f : 0.24f)));
    }

    private static Vector3 PlateCenter(int index, int plateCount, int seed, float jitter)
    {
        float t = (index + 0.5f) / Mathf.Max(1, plateCount);
        float y = 1f - 2f * t;
        float radius = Mathf.Sqrt(Mathf.Max(0f, 1f - y * y));
        float angle = index * 2.39996323f + seed * 0.0137f;
        Vector3 center = new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
        Vector3 offset = HashVector(index, seed + 1931);
        return (center + offset * Mathf.Clamp01(jitter) * 0.48f).normalized;
    }

    private static bool IsOceanicPlate(int plateId, int seed, float oceanicRatio)
    {
        return Hash01(plateId, 0, seed + 2711) < Mathf.Clamp01(oceanicRatio);
    }

    private static Vector3 HashVector(int value, int seed)
    {
        Vector3 vector = new Vector3(
            Hash01(value, 11, seed) * 2f - 1f,
            Hash01(value, 23, seed) * 2f - 1f,
            Hash01(value, 37, seed) * 2f - 1f);
        return vector.sqrMagnitude > 0.0001f ? vector.normalized : Vector3.up;
    }

    private static float Hash01(int x, int y, int seed)
    {
        uint hash = (uint)NoiseGenerator.Hash(x, y, seed);
        return (hash & 0x00FFFFFF) / 16777215f;
    }

    private static float Smooth01(float value)
    {
        float t = Mathf.Clamp01(value);
        return t * t * (3f - 2f * t);
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
