using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlanetRiverSettings
{
    [Header("Sources")]
    [Range(0f, 1f)] public float sourceElevation = 0.68f;
    [Range(0, 16)] public int sourcesPerChunk = 4;
    [Range(0, 256)] public int sourcesPerPlanetPreview = 0;
    [Range(0f, 1f)] public float sourceRidgeThreshold = 0.58f;
    [Range(0f, 1f)] public float sourceTectonicMountainThreshold = 0.35f;

    [Header("Tracing")]
    [Range(8, 512)] public int maxRiverSteps = 96;
    [Range(0.05f, 5f)] public float angularStepSize = 0.75f;
    [Range(0f, 0.08f)] public float lakeFillThreshold = 0.012f;
    [Range(0.01f, 2f)] public float riverWidth = 0.22f;
    [Range(0.05f, 8f)] public float moistureRadius = 1.8f;
    [Range(4, 16)] public int downhillProbeDirections = 8;
}

[System.Serializable]
public struct PlanetRiverSample
{
    public float riverMask;
    public float riverDistance;
    public Vector3 flowDirection;
    public float lakeMask;
    public int drainageId;
    public bool isRiverMouth;
}

[DisallowMultipleComponent]
public class PlanetRiverSystem : MonoBehaviour
{
    public PlanetRiverSettings settings = new PlanetRiverSettings();

    private readonly Dictionary<string, RiverChunkCache> _chunkCaches = new Dictionary<string, RiverChunkCache>();

    public PlanetRiverSample SampleRiver(PlanetGenerator generator, Vector3 direction, PlanetNoiseSample noise, float normalizedHeight)
    {
        PlanetRiverSample empty = new PlanetRiverSample();
        empty.riverDistance = 1f;
        empty.drainageId = -1;

        if (generator == null || settings == null)
        {
            return empty;
        }

        direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.up;
        PlanetSurfaceCoordinate coordinate = CubeSphereMeshBuilder.DirectionToCoordinate(direction, 0f);
        int chunksPerFace = Mathf.Max(1, generator.chunksPerFace);
        int chunkX = Mathf.Clamp(Mathf.FloorToInt(coordinate.uv.x * chunksPerFace), 0, chunksPerFace - 1);
        int chunkY = Mathf.Clamp(Mathf.FloorToInt(coordinate.uv.y * chunksPerFace), 0, chunksPerFace - 1);
        float angularRiverWidth = Mathf.Max(0.0001f, settings.riverWidth / Mathf.Max(1f, generator.planetRadius));
        float angularMoistureRadius = Mathf.Max(angularRiverWidth, settings.moistureRadius / Mathf.Max(1f, generator.planetRadius));
        float bestDistance = angularMoistureRadius;
        PlanetRiverSample best = empty;

        for (int y = chunkY - 1; y <= chunkY + 1; y++)
        {
            if (y < 0 || y >= chunksPerFace) continue;
            for (int x = chunkX - 1; x <= chunkX + 1; x++)
            {
                if (x < 0 || x >= chunksPerFace) continue;
                CubeSphereMeshBuilder.PlanetChunkKey key = new CubeSphereMeshBuilder.PlanetChunkKey(coordinate.face, x, y);
                RiverChunkCache cache = GetOrBuildChunk(generator, key);
                for (int i = 0; i < cache.paths.Count; i++)
                {
                    RiverPath path = cache.paths[i];
                    PlanetRiverSample candidate;
                    float distance = DistanceToPath(direction, path, angularRiverWidth, angularMoistureRadius, out candidate);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = candidate;
                    }
                }
            }
        }

        if (normalizedHeight < generator.waterLevel && best.isRiverMouth)
        {
            best.riverMask = Mathf.Max(best.riverMask, 0.7f);
        }

        return best;
    }

    public void ClearCache()
    {
        _chunkCaches.Clear();
    }

    private RiverChunkCache GetOrBuildChunk(PlanetGenerator generator, CubeSphereMeshBuilder.PlanetChunkKey key)
    {
        string cacheKey = generator.seed + ":" + key;
        RiverChunkCache cache;
        if (_chunkCaches.TryGetValue(cacheKey, out cache))
        {
            return cache;
        }

        cache = BuildChunk(generator, key);
        _chunkCaches.Add(cacheKey, cache);
        return cache;
    }

    private RiverChunkCache BuildChunk(PlanetGenerator generator, CubeSphereMeshBuilder.PlanetChunkKey key)
    {
        RiverChunkCache cache = new RiverChunkCache();
        int sourceCount = Mathf.Max(0, settings.sourcesPerChunk);
        if (settings.sourcesPerPlanetPreview > 0)
        {
            int totalChunks = Mathf.Max(1, generator.chunksPerFace * generator.chunksPerFace * CubeSphereMeshBuilder.FaceCount);
            sourceCount = Mathf.Max(sourceCount, Mathf.CeilToInt(settings.sourcesPerPlanetPreview / (float)totalChunks));
        }

        for (int i = 0; i < sourceCount; i++)
        {
            Vector2 uv = SourceUv(generator.seed, key, i, Mathf.Max(1, generator.chunksPerFace));
            Vector3 sourceDirection = CubeSphereMeshBuilder.FaceUvToDirection(key.face, uv.x, uv.y);
            HeightProbe source = ProbeHeight(generator, sourceDirection);
            if (!IsValidSource(source))
            {
                continue;
            }

            RiverPath path = TraceRiver(generator, sourceDirection, source.normalizedHeight, StableHash(generator.seed, key.face, key.x, key.y, i));
            if (path.nodes.Count > 1)
            {
                cache.paths.Add(path);
            }
        }

        return cache;
    }

    private RiverPath TraceRiver(PlanetGenerator generator, Vector3 sourceDirection, float sourceHeight, int drainageId)
    {
        RiverPath path = new RiverPath();
        path.drainageId = drainageId;
        path.nodes.Add(sourceDirection.normalized);

        Vector3 current = sourceDirection.normalized;
        float currentHeight = sourceHeight;
        float stepRadians = settings.angularStepSize * Mathf.Deg2Rad;
        int probes = Mathf.Clamp(settings.downhillProbeDirections, 4, 16);
        Vector3 previous = Vector3.zero;

        for (int step = 0; step < settings.maxRiverSteps; step++)
        {
            if (currentHeight <= generator.waterLevel)
            {
                path.reachesOcean = true;
                path.mouthDirection = current;
                break;
            }

            Vector3 tangentA = Vector3.Cross(current, Mathf.Abs(current.y) < 0.92f ? Vector3.up : Vector3.right).normalized;
            Vector3 tangentB = Vector3.Cross(current, tangentA).normalized;
            Vector3 bestDirection = current;
            float bestHeight = currentHeight;
            float bestScore = float.MaxValue;

            for (int i = 0; i < probes; i++)
            {
                float angle = (i / (float)probes) * Mathf.PI * 2f;
                Vector3 tangent = tangentA * Mathf.Cos(angle) + tangentB * Mathf.Sin(angle);
                Vector3 candidate = (current * Mathf.Cos(stepRadians) + tangent * Mathf.Sin(stepRadians)).normalized;
                if (previous.sqrMagnitude > 0f && Vector3.Dot(candidate, previous) > 0.999f)
                {
                    continue;
                }

                HeightProbe probe = ProbeHeight(generator, candidate);
                float directionalBias = previous.sqrMagnitude > 0f ? (1f - Vector3.Dot(candidate, (current * 2f - previous).normalized)) * 0.0025f : 0f;
                float score = probe.normalizedHeight + directionalBias;
                if (!probe.underwater && score < bestScore)
                {
                    bestScore = score;
                    bestHeight = probe.normalizedHeight;
                    bestDirection = candidate;
                }
                else if (probe.underwater && probe.normalizedHeight < bestScore)
                {
                    bestScore = probe.normalizedHeight;
                    bestHeight = probe.normalizedHeight;
                    bestDirection = candidate;
                }
            }

            previous = current;
            current = bestDirection;
            path.nodes.Add(current);

            if (bestHeight <= generator.waterLevel)
            {
                path.reachesOcean = true;
                path.mouthDirection = current;
                break;
            }

            if (bestHeight >= currentHeight - Mathf.Max(0f, settings.lakeFillThreshold))
            {
                path.endsInLake = true;
                path.lakeDirection = current;
                break;
            }

            currentHeight = bestHeight;
        }

        if (!path.reachesOcean && !path.endsInLake && path.nodes.Count > 0)
        {
            path.endsInLake = true;
            path.lakeDirection = path.nodes[path.nodes.Count - 1];
        }

        return path;
    }

    private bool IsValidSource(HeightProbe source)
    {
        if (source.underwater || source.normalizedHeight < settings.sourceElevation)
        {
            return false;
        }

        bool ridgeSource = source.noise.ridgeMask >= settings.sourceRidgeThreshold;
        bool tectonicSource = source.noise.tectonicMountainMask >= settings.sourceTectonicMountainThreshold;
        return ridgeSource || tectonicSource;
    }

    private HeightProbe ProbeHeight(PlanetGenerator generator, Vector3 direction)
    {
        PlanetNoiseSample noise = PlanetNoise.Sample(direction, generator.noiseSettings);
        float normalizedHeight = PlanetNoise.BuildHeight(noise, generator.waterLevel, generator.mountainAmount, generator.oceanDepth);
        HeightProbe probe = new HeightProbe();
        probe.normalizedHeight = normalizedHeight;
        probe.underwater = normalizedHeight < generator.waterLevel;
        probe.noise = noise;
        return probe;
    }

    private float DistanceToPath(Vector3 direction, RiverPath path, float riverWidth, float moistureRadius, out PlanetRiverSample sample)
    {
        sample = new PlanetRiverSample();
        sample.riverDistance = 1f;
        sample.drainageId = path.drainageId;
        float best = moistureRadius;
        Vector3 flow = Vector3.zero;

        for (int i = 0; i < path.nodes.Count; i++)
        {
            float distance = Mathf.Acos(Mathf.Clamp(Vector3.Dot(direction, path.nodes[i]), -1f, 1f));
            if (distance < best)
            {
                best = distance;
                if (i + 1 < path.nodes.Count)
                {
                    flow = Vector3.ProjectOnPlane(path.nodes[i + 1] - path.nodes[i], direction).normalized;
                }
                else if (i > 0)
                {
                    flow = Vector3.ProjectOnPlane(path.nodes[i] - path.nodes[i - 1], direction).normalized;
                }
            }
        }

        float lakeDistance = path.endsInLake ? Mathf.Acos(Mathf.Clamp(Vector3.Dot(direction, path.lakeDirection), -1f, 1f)) : float.MaxValue;
        float mouthDistance = path.reachesOcean ? Mathf.Acos(Mathf.Clamp(Vector3.Dot(direction, path.mouthDirection), -1f, 1f)) : float.MaxValue;
        float lakeRadius = Mathf.Max(riverWidth * 2.5f, settings.lakeFillThreshold * 2f);

        sample.riverDistance = Mathf.Clamp01(best / moistureRadius);
        sample.riverMask = 1f - Mathf.Clamp01(best / riverWidth);
        sample.flowDirection = flow;
        sample.lakeMask = path.endsInLake ? 1f - Mathf.Clamp01(lakeDistance / lakeRadius) : 0f;
        sample.isRiverMouth = path.reachesOcean && mouthDistance <= riverWidth * 2f;
        return Mathf.Min(best, lakeDistance);
    }

    private static Vector2 SourceUv(int seed, CubeSphereMeshBuilder.PlanetChunkKey key, int sourceIndex, int chunksPerFace)
    {
        float chunkSize = 1f / chunksPerFace;
        float u = (key.x + Hash01(seed, key.face, key.x, sourceIndex, 17)) * chunkSize;
        float v = (key.y + Hash01(seed, key.face, key.y, sourceIndex, 53)) * chunkSize;
        return new Vector2(Mathf.Clamp01(u), Mathf.Clamp01(v));
    }

    private static float Hash01(params int[] values)
    {
        unchecked
        {
            uint hash = 2166136261u;
            for (int i = 0; i < values.Length; i++)
            {
                hash ^= (uint)values[i] + 0x9e3779b9u + (hash << 6) + (hash >> 2);
                hash *= 16777619u;
            }
            return (hash & 0x00FFFFFF) / 16777215f;
        }
    }

    private static int StableHash(params int[] values)
    {
        unchecked
        {
            int hash = 486187739;
            for (int i = 0; i < values.Length; i++)
            {
                hash = hash * 16777619 ^ values[i];
            }
            return hash;
        }
    }

    private struct HeightProbe
    {
        public float normalizedHeight;
        public bool underwater;
        public PlanetNoiseSample noise;
    }

    private class RiverPath
    {
        public int drainageId;
        public readonly List<Vector3> nodes = new List<Vector3>();
        public bool reachesOcean;
        public bool endsInLake;
        public Vector3 mouthDirection;
        public Vector3 lakeDirection;
    }

    private class RiverChunkCache
    {
        public readonly List<RiverPath> paths = new List<RiverPath>();
    }
}
