using System.Collections.Generic;
using UnityEngine;

namespace TerariaGenerator.Planets
{
    public sealed class ClimateMaps
    {
        public readonly float[,,] temperature;
        public readonly float[,,] moisture;
        public readonly float[,,] height;
        public readonly int[,,] primaryBiome;
        public readonly Vector4[,,] biomeWeights;

        private readonly PlanetSettings settings;
        private readonly int resolution;
        private readonly float[,,] terrainHeights;
        private readonly bool[,,] rivers;

        private ClimateMaps(PlanetSettings settings, int resolution, float[,,] terrainHeights, bool[,,] rivers)
        {
            this.settings = settings;
            this.resolution = resolution;
            this.terrainHeights = terrainHeights;
            this.rivers = rivers;
            temperature = new float[6, resolution, resolution];
            moisture = new float[6, resolution, resolution];
            height = new float[6, resolution, resolution];
            primaryBiome = new int[6, resolution, resolution];
            biomeWeights = new Vector4[6, resolution, resolution];
        }

        public static ClimateMaps Generate(PlanetSettings settings, int resolution, float[,,] terrainHeights, bool[,,] rivers)
        {
            ClimateMaps maps = new ClimateMaps(settings, resolution, terrainHeights, rivers);
            maps.BuildHeightMap();
            maps.BuildTemperatureMap();
            maps.BuildMoistureMap();
            maps.BuildBiomeMap();
            return maps;
        }

        private void BuildHeightMap()
        {
            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;

            for (int face = 0; face < 6; face++)
            for (int x = 0; x < resolution; x++)
            for (int y = 0; y < resolution; y++)
            {
                float h = terrainHeights[face, x, y];
                minHeight = Mathf.Min(minHeight, h);
                maxHeight = Mathf.Max(maxHeight, h);
            }

            if (Mathf.Approximately(minHeight, maxHeight))
            {
                maxHeight = minHeight + 1f;
            }

            for (int face = 0; face < 6; face++)
            for (int x = 0; x < resolution; x++)
            for (int y = 0; y < resolution; y++)
            {
                height[face, x, y] = Mathf.InverseLerp(minHeight, maxHeight, terrainHeights[face, x, y]);
            }
        }

        private void BuildTemperatureMap()
        {
            for (int face = 0; face < 6; face++)
            for (int x = 0; x < resolution; x++)
            for (int y = 0; y < resolution; y++)
            {
                Vector3 point = Point(face, x, y);
                float latitude = Mathf.Abs(point.y);
                float latitudeTemperature = Mathf.Pow(1f - latitude, settings.latitudeTemperatureFalloff);
                float altitudePenalty = Mathf.Max(0f, terrainHeights[face, x, y] - settings.oceanLevel) * settings.temperatureHeightFalloff;
                float noise = (PlanetNoise.Fbm(point, settings.temperatureNoiseScale, 4, settings.seed + 701) - 0.5f) * settings.temperatureNoiseStrength;
                temperature[face, x, y] = Mathf.Clamp01(latitudeTemperature - altitudePenalty + noise + settings.temperatureOffset);
            }
        }

        private void BuildMoistureMap()
        {
            float[,,] oceanDistance = BuildDistanceField(IsOcean);
            float[,,] riverDistance = BuildDistanceField(IsRiver);

            for (int face = 0; face < 6; face++)
            for (int x = 0; x < resolution; x++)
            for (int y = 0; y < resolution; y++)
            {
                Vector3 point = Point(face, x, y);
                float oceanInfluence = 1f - Mathf.Clamp01(oceanDistance[face, x, y] / Mathf.Max(1f, settings.oceanMoistureDistance * resolution));
                float riverInfluence = 1f - Mathf.Clamp01(riverDistance[face, x, y] / Mathf.Max(1f, settings.riverMoistureDistance * resolution));
                float noise = (PlanetNoise.Fbm(point, settings.moistureNoiseScale, 4, settings.seed + 907) - 0.5f) * settings.moistureNoiseStrength;
                moisture[face, x, y] = Mathf.Clamp01(oceanInfluence * settings.oceanMoistureStrength + riverInfluence * settings.riverMoistureStrength + noise + settings.moistureOffset);
            }
        }

        private void BuildBiomeMap()
        {
            for (int face = 0; face < 6; face++)
            for (int x = 0; x < resolution; x++)
            for (int y = 0; y < resolution; y++)
            {
                float t = temperature[face, x, y];
                float m = moisture[face, x, y];
                float h = height[face, x, y];
                Vector4 weights = Vector4.zero;
                int bestIndex = 0;
                float bestWeight = -1f;
                int count = settings.BiomeCountForShader;

                for (int i = 0; i < count; i++)
                {
                    float weight = settings.GetBiome(i).EvaluateWeight(t, m, h, settings.biomeBlendDistance);
                    weights[i] = weight;
                    if (weight > bestWeight)
                    {
                        bestWeight = weight;
                        bestIndex = i;
                    }
                }

                float total = weights.x + weights.y + weights.z + weights.w;
                if (total <= 0.0001f)
                {
                    bestIndex = FindNearestBiome(t, m, h, count);
                    weights[bestIndex] = 1f;
                }
                else
                {
                    weights /= total;
                }

                primaryBiome[face, x, y] = bestIndex;
                biomeWeights[face, x, y] = weights;
            }
        }

        private int FindNearestBiome(float temperatureValue, float moistureValue, float heightValue, int count)
        {
            int bestIndex = 0;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                BiomeDefinition biome = settings.GetBiome(i);
                float centerT = (biome.minimumTemperature + biome.maximumTemperature) * 0.5f;
                float centerM = (biome.minimumMoisture + biome.maximumMoisture) * 0.5f;
                float centerH = (biome.minimumHeight + biome.maximumHeight) * 0.5f;
                float distance = Mathf.Abs(temperatureValue - centerT) + Mathf.Abs(moistureValue - centerM) + Mathf.Abs(heightValue - centerH);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }
            return bestIndex;
        }

        private float[,,] BuildDistanceField(System.Func<int, int, int, bool> source)
        {
            float[,,] distance = new float[6, resolution, resolution];
            Queue<Vector3Int> queue = new Queue<Vector3Int>();

            for (int face = 0; face < 6; face++)
            for (int x = 0; x < resolution; x++)
            for (int y = 0; y < resolution; y++)
            {
                if (source(face, x, y))
                {
                    distance[face, x, y] = 0f;
                    queue.Enqueue(new Vector3Int(face, x, y));
                }
                else
                {
                    distance[face, x, y] = float.PositiveInfinity;
                }
            }

            while (queue.Count > 0)
            {
                Vector3Int cell = queue.Dequeue();
                float nextDistance = distance[cell.x, cell.y, cell.z] + 1f;
                for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) != 1) continue;
                    int nx = cell.y + dx;
                    int ny = cell.z + dy;
                    if (nx < 0 || nx >= resolution || ny < 0 || ny >= resolution) continue;
                    if (nextDistance >= distance[cell.x, nx, ny]) continue;
                    distance[cell.x, nx, ny] = nextDistance;
                    queue.Enqueue(new Vector3Int(cell.x, nx, ny));
                }
            }

            return distance;
        }

        private bool IsOcean(int face, int x, int y) => terrainHeights[face, x, y] <= settings.oceanLevel;

        private bool IsRiver(int face, int x, int y) => rivers != null && rivers[face, x, y];

        private Vector3 Point(int face, int x, int y)
        {
            return CubeSphereUtility.PointOnFace(face, x / (resolution - 1f), y / (resolution - 1f));
        }
    }
}
