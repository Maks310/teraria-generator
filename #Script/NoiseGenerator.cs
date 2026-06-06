using UnityEngine;

public static class NoiseGenerator
{
    public static float Repeat01(float value)
    {
        return value - Mathf.Floor(value);
    }

    /// <summary>
    /// Seamless pseudo-4D Perlin noise via toroidal sin/cos mapping.
    /// Coordinates are expected in [0, 1], but wrapping is applied defensively.
    /// </summary>
    public static float SeamlessPerlin(float x, float y, float scale, int seed)
    {
        x = Repeat01(x);
        y = Repeat01(y);

        float nx = x * Mathf.Max(0.0001f, scale);
        float ny = y * Mathf.Max(0.0001f, scale);

        float angleX = nx * Mathf.PI * 2f;
        float angleY = ny * Mathf.PI * 2f;

        float x1 = Mathf.Cos(angleX);
        float y1 = Mathf.Sin(angleX);
        float x2 = Mathf.Cos(angleY);
        float y2 = Mathf.Sin(angleY);

        float seedOffset = seed * 37.719f;
        float n1 = Mathf.PerlinNoise(x1 + seedOffset, y1 - seedOffset);
        float n2 = Mathf.PerlinNoise(x2 - seedOffset + 101.3f, y2 + seedOffset + 17.7f);
        float n3 = Mathf.PerlinNoise(x1 + x2 + seedOffset * 0.37f, y1 + y2 - seedOffset * 0.29f);
        float n4 = Mathf.PerlinNoise(x1 - y2 + seedOffset * 0.11f, y1 + x2 + seedOffset * 0.13f);

        return Mathf.Clamp01((n1 + n2 + n3 + n4) * 0.25f);
    }

    public static float SeamlessOctavePerlin(float x, float y, int octaves, float persistence, float lacunarity, float scale, int seed)
    {
        float total = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float maxValue = 0f;

        octaves = Mathf.Max(1, octaves);
        persistence = Mathf.Clamp01(persistence);
        lacunarity = Mathf.Max(1.01f, lacunarity);

        for (int i = 0; i < octaves; i++)
        {
            total += SeamlessPerlin(x, y, scale * frequency, seed + i * 53) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return maxValue <= 0f ? 0f : total / maxValue;
    }

    public static float RidgedSeamlessPerlin(float x, float y, float scale, int seed)
    {
        float noise = SeamlessPerlin(x, y, scale, seed);
        return 1f - Mathf.Abs(noise * 2f - 1f);
    }

    public static float RidgedOctavePerlin(float x, float y, int octaves, float persistence, float lacunarity, float scale, int seed)
    {
        float total = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float maxValue = 0f;

        octaves = Mathf.Max(1, octaves);

        for (int i = 0; i < octaves; i++)
        {
            total += RidgedSeamlessPerlin(x, y, scale * frequency, seed + i * 79) * amplitude;
            maxValue += amplitude;
            amplitude *= Mathf.Clamp01(persistence);
            frequency *= Mathf.Max(1.01f, lacunarity);
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
        float warpY = SeamlessOctavePerlin(x, y, 3, 0.5f, 2f, scale, seed + 997) - 0.5f;
        return new Vector2(Repeat01(x + warpX * strength), Repeat01(y + warpY * strength));
    }

    public static float SmoothStep(float edge0, float edge1, float value)
    {
        float t = Mathf.Clamp01((value - edge0) / Mathf.Max(0.0001f, edge1 - edge0));
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
