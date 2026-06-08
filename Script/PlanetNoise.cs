using UnityEngine;

namespace TerariaGenerator.Planets
{
    public static class PlanetNoise
    {
        public static float EvaluateTerrain(Vector3 unitSpherePoint, PlanetSettings settings)
        {
            float continentFrequency = settings.continentScale * Mathf.Sqrt(Mathf.Max(1, settings.continentCount));
            float continent = RidgedFbm(unitSpherePoint, continentFrequency, 5, settings.seed + 11);
            float continentMask = Mathf.Pow(Mathf.Clamp01(continent), Mathf.Lerp(1.8f, 0.75f, settings.terrainSmoothness));
            continentMask = Mathf.SmoothStep(0f, 1f, continentMask);

            float continentHeight = (continentMask - 0.48f) * settings.continentStrength;
            float coast = Mathf.SmoothStep(-settings.coastSmoothness, settings.coastSmoothness, continentHeight - settings.oceanLevel);

            float mountains = RidgedFbm(unitSpherePoint, settings.mountainScale, 6, settings.seed + 101);
            mountains = Mathf.Pow(Mathf.Clamp01(mountains), settings.mountainSharpness) * settings.mountainHeight * coast;

            float hills = Fbm(unitSpherePoint, settings.hillScale, 4, settings.seed + 211) * settings.hillHeight * coast;
            float detail = Fbm(unitSpherePoint, settings.detailScale, 3, settings.seed + 307) * settings.detailHeight * coast;

            return continentHeight + mountains + hills + detail;
        }

        public static float Fbm(Vector3 p, float scale, int octaves, int seed)
        {
            float sum = 0f;
            float amplitude = 0.5f;
            float frequency = Mathf.Max(0.001f, scale);
            float normalizer = 0f;

            for (int i = 0; i < octaves; i++)
            {
                sum += ValueNoise3D(p * frequency, seed + i * 37) * amplitude;
                normalizer += amplitude;
                amplitude *= 0.5f;
                frequency *= 2.03f;
            }

            return normalizer <= 0f ? 0f : sum / normalizer;
        }

        public static float RidgedFbm(Vector3 p, float scale, int octaves, int seed)
        {
            float sum = 0f;
            float amplitude = 0.55f;
            float frequency = Mathf.Max(0.001f, scale);
            float normalizer = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float n = ValueNoise3D(p * frequency, seed + i * 53);
                n = 1f - Mathf.Abs(n * 2f - 1f);
                sum += n * amplitude;
                normalizer += amplitude;
                amplitude *= 0.5f;
                frequency *= 2.11f;
            }

            return normalizer <= 0f ? 0f : sum / normalizer;
        }

        private static float ValueNoise3D(Vector3 p, int seed)
        {
            Vector3 offset = SeedOffset(seed);
            float xy = Mathf.PerlinNoise(p.x + offset.x, p.y + offset.y);
            float yz = Mathf.PerlinNoise(p.y + offset.y, p.z + offset.z);
            float xz = Mathf.PerlinNoise(p.x + offset.x, p.z + offset.z);
            float yx = Mathf.PerlinNoise(p.y - offset.y, p.x - offset.x);
            float zy = Mathf.PerlinNoise(p.z - offset.z, p.y - offset.y);
            float zx = Mathf.PerlinNoise(p.z + offset.z * 0.37f, p.x - offset.x * 0.61f);
            return (xy + yz + xz + yx + zy + zx) / 6f;
        }

        private static Vector3 SeedOffset(int seed)
        {
            unchecked
            {
                uint h = (uint)seed;
                h ^= h << 13; h ^= h >> 17; h ^= h << 5;
                float x = (h & 1023u) * 0.173f + 17.13f;
                h = h * 1664525u + 1013904223u;
                float y = (h & 1023u) * 0.197f + 31.77f;
                h = h * 1664525u + 1013904223u;
                float z = (h & 1023u) * 0.211f + 47.91f;
                return new Vector3(x, y, z);
            }
        }
    }
}
