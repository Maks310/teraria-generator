using UnityEngine;

public static class NoiseGenerator
{
    public static float Repeat01(float value)
    {
        return value - Mathf.Floor(value);
    }

    /// <summary>
    /// Seamless noise on a torus. x/y are normalized world coordinates, so x=0 equals x=1 and y=0 equals y=1.
    /// </summary>
    public static float SeamlessPerlin(float x, float y, float scale, int seed)
    {
        x = Repeat01(x);
        y = Repeat01(y);
        scale = Mathf.Max(0.0001f, scale);

        float angleX = x * Mathf.PI * 2f;
        float angleY = y * Mathf.PI * 2f;
        float circleX = Mathf.Cos(angleX) * scale;
        float circleY = Mathf.Sin(angleX) * scale;
        float circleZ = Mathf.Cos(angleY) * scale;
        float circleW = Mathf.Sin(angleY) * scale;
        float offset = seed * 19.191f;

        float a = Mathf.PerlinNoise(circleX + offset, circleZ - offset);
        float b = Mathf.PerlinNoise(circleY - offset * 0.37f, circleW + offset * 0.61f);
        float c = Mathf.PerlinNoise(circleX + circleZ + offset * 0.13f, circleY + circleW - offset * 0.17f);
        float d = Mathf.PerlinNoise(circleX - circleW + offset * 0.71f, circleZ + circleY + offset * 0.29f);
        return Mathf.Clamp01((a + b + c + d) * 0.25f);
    }

    public static float SeamlessOctavePerlin(float x, float y, int octaves, float persistence, float lacunarity, float scale, int seed)
    {
        octaves = Mathf.Max(1, octaves);
        persistence = Mathf.Clamp01(persistence);
        lacunarity = Mathf.Max(1.01f, lacunarity);

        float total = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float maxValue = 0f;

        for (int i = 0; i < octaves; i++)
        {
            total += SeamlessPerlin(x, y, scale * frequency, seed + i * 97) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return maxValue <= 0f ? 0f : Mathf.Clamp01(total / maxValue);
    }

    public static float RidgedSeamlessPerlin(float x, float y, float scale, int seed)
    {
        float n = SeamlessPerlin(x, y, scale, seed);
        return 1f - Mathf.Abs(n * 2f - 1f);
    }

    public static float RidgedOctavePerlin(float x, float y, int octaves, float persistence, float lacunarity, float scale, int seed)
    {
        octaves = Mathf.Max(1, octaves);
        persistence = Mathf.Clamp01(persistence);
        lacunarity = Mathf.Max(1.01f, lacunarity);

        float total = 0f;
        float amplitude = 1f;
        float frequency = 1f;
        float maxValue = 0f;

        for (int i = 0; i < octaves; i++)
        {
            total += RidgedSeamlessPerlin(x, y, scale * frequency, seed + i * 131) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return maxValue <= 0f ? 0f : Mathf.Clamp01(total / maxValue);
    }

    public static Vector2 SeamlessDomainWarp(float x, float y, float scale, float strength, int seed)
    {
        if (strength <= 0f)
        {
            return new Vector2(Repeat01(x), Repeat01(y));
        }

        float warpX = SeamlessOctavePerlin(x, y, 3, 0.5f, 2f, scale, seed) - 0.5f;
        float warpY = SeamlessOctavePerlin(x, y, 3, 0.5f, 2f, scale, seed + 1009) - 0.5f;
        return new Vector2(Repeat01(x + warpX * strength), Repeat01(y + warpY * strength));
    }

    public static float SmoothStep(float edge0, float edge1, float value)
    {
        float t = Mathf.Clamp01((value - edge0) / Mathf.Max(0.00001f, edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    public static int Hash(int x, int y, int seed)
    {
        unchecked
        {
            int hash = seed;
            hash = (hash * 397) ^ x;
            hash = (hash * 397) ^ y;
            hash ^= hash << 13;
            hash ^= hash >> 17;
            hash ^= hash << 5;
            return hash;
        }
    }

    public static float Hash01(int x, int y, int seed)
    {
        uint hash = (uint)Hash(x, y, seed);
        return (hash & 0x00FFFFFF) / 16777215f;
    }
}
