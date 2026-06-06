using UnityEngine;

public static class NoiseGenerator
{
    /// <summary>
    /// Генерує безшовний Perlin noise через тороїдальне маппування.
    /// Координати (x, y) від 0 до 1 мапляться на 4D шум через sin/cos.
    /// </summary>
    public static float SeamlessPerlin(float x, float y, float scale, int seed)
    {
        // Нормалізуємо координати до [0, 1]
        float nx = x * scale;
        float ny = y * scale;

        // Тороїдальне маппування: 2D → 4D через sin/cos
        float angle1 = nx * Mathf.PI * 2f;
        float angle2 = ny * Mathf.PI * 2f;

        float radius = 1f;

        float x1 = Mathf.Cos(angle1) * radius;
        float y1 = Mathf.Sin(angle1) * radius;
        float x2 = Mathf.Cos(angle2) * radius;
        float y2 = Mathf.Sin(angle2) * radius;

        // Unity не має 4D Perlin, тому комбінуємо 2D шуми
        float seedOffset = seed * 1000f;
        float n1 = Mathf.PerlinNoise(x1 + seedOffset, y1 + seedOffset);
        float n2 = Mathf.PerlinNoise(x2 + seedOffset + 100f, y2 + seedOffset + 100f);
        float n3 = Mathf.PerlinNoise(x1 + x2 + seedOffset, y1 + y2 + seedOffset);

        return (n1 + n2 + n3) / 3f;
    }

    /// <summary>
    /// Octave noise для складніших ландшафтів
    /// </summary>
    public static float SeamlessOctavePerlin(float x, float y, int octaves, float persistence, float lacunarity, float scale, int seed)
    {
        float total = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float maxValue = 0f;

        for (int i = 0; i < octaves; i++)
        {
            total += SeamlessPerlin(x, y, scale * frequency, seed + i) * amplitude;
            maxValue += amplitude;
            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return total / maxValue;
    }
}
