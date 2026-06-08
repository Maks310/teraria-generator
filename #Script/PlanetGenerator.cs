using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public class PlanetGenerator : MonoBehaviour
{
    [Header("Planet Settings")]
    public int seed = 42;
    [Range(50f, 5000f)] public float planetRadius = 420f;
    [Range(5f, 400f)] public float heightScale = 75f;
    [Range(0f, 1f)] public float waterLevel = 0.46f;
    [Range(0f, 0.6f)] public float oceanDepth = 0.26f;
    [Range(0f, 0.6f)] public float mountainAmount = 0.22f;

    [Header("Chunked Cube-Sphere")]
    [Range(1, 32)] public int chunksPerFace = 4;
    [Range(4, 128)] public int chunkResolution = 24;
    public bool generateOnStart = true;
    public bool generateInEditor;
    public bool clearBeforeGenerate = true;
    [Tooltip("When a ChunkStreamingManager with a player is assigned, runtime generation starts with nearby chunks instead of the whole planet.")]
    public bool preferRuntimeStreaming = true;

    [Header("Pipeline Settings")]
    public PlanetNoiseSettings noiseSettings = new PlanetNoiseSettings();
    public PlanetClimateSettings climateSettings = new PlanetClimateSettings();

    [Header("Pipeline References")]
    public CubeSphereMeshBuilder meshBuilder;
    public PlanetBiomeResolver biomeResolver;
    public PlanetSurfaceSpawner surfaceSpawner;
    public PlanetPOIManager poiManager;
    public PlanetWaterSystem waterSystem;
    public ChunkStreamingManager streamingManager;
    public PlanetMapSystem mapSystem;

    [Header("Materials")]
    public Material terrainMaterial;
    public Material waterMaterial;

    private readonly Dictionary<string, CubeSphereMeshBuilder.PlanetChunk> _chunks = new Dictionary<string, CubeSphereMeshBuilder.PlanetChunk>();
    private PlanetGenerationData _generationData;

    public PlanetGenerationData GenerationData
    {
        get
        {
            if (_generationData == null)
            {
                _generationData = new PlanetGenerationData(this);
            }
            return _generationData;
        }
    }

    public IEnumerable<CubeSphereMeshBuilder.PlanetChunk> LoadedChunks { get { return _chunks.Values; } }

    private void Awake()
    {
        EnsureSubsystems();
    }

    private void Start()
    {
        if (generateOnStart && Application.isPlaying)
        {
            GeneratePlanet();
        }
    }

    private void OnValidate()
    {
        seed = Mathf.Max(0, seed);
        planetRadius = Mathf.Max(1f, planetRadius);
        heightScale = Mathf.Max(0.1f, heightScale);
        chunksPerFace = Mathf.Max(1, chunksPerFace);
        chunkResolution = Mathf.Max(2, chunkResolution);
        if (noiseSettings != null)
        {
            noiseSettings.seed = seed;
        }
    }

    [ContextMenu("Generate Planet")]
    public void GeneratePlanet()
    {
        EnsureSubsystems();
        if (clearBeforeGenerate)
        {
            ClearPlanet();
        }

        if (noiseSettings == null)
        {
            noiseSettings = new PlanetNoiseSettings();
        }
        noiseSettings.seed = seed;
        if (biomeResolver != null)
        {
            biomeResolver.waterLevel = waterLevel;
        }

        bool useStreamingBootstrap = preferRuntimeStreaming && Application.isPlaying && streamingManager != null && streamingManager.player != null;
        if (useStreamingBootstrap)
        {
            streamingManager.planet = this;
            streamingManager.UpdateStreaming();
        }
        else
        {
            BuildAllTerrainChunksForPreview();
        }

        if (waterSystem != null)
        {
            waterSystem.BuildWaterShell(this);
        }

        if (poiManager != null)
        {
            poiManager.GeneratePOIs(this);
        }

        if (surfaceSpawner != null && surfaceSpawner.spawnAfterPlanetGeneration)
        {
            surfaceSpawner.SpawnForLoadedChunks(this);
        }

        if (mapSystem != null)
        {
            mapSystem.Initialize(this);
        }

        Debug.Log("Generated cube-sphere survival planet with " + _chunks.Count + " terrain chunks. Seed: " + seed, this);
    }

    [ContextMenu("Clear Planet")]
    public void ClearPlanet()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroySmart(transform.GetChild(i).gameObject);
        }
        _chunks.Clear();
    }


    public void BuildAllTerrainChunksForPreview()
    {
        for (int face = 0; face < CubeSphereMeshBuilder.FaceCount; face++)
        {
            for (int y = 0; y < chunksPerFace; y++)
            {
                for (int x = 0; x < chunksPerFace; x++)
                {
                    BuildOrLoadChunk(new CubeSphereMeshBuilder.PlanetChunkKey(face, x, y));
                }
            }
        }
    }

    public PlanetSurfaceSample SampleSurface(Vector3 direction)
    {
        direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.up;
        PlanetNoiseSample noise = PlanetNoise.Sample(direction, noiseSettings);
        float normalizedHeight = PlanetNoise.BuildHeight(noise, waterLevel, mountainAmount, oceanDepth);
        PlanetClimateSample climate = PlanetClimate.Evaluate(direction, normalizedHeight, noise, waterLevel, climateSettings);
        BiomeDefinition biome = biomeResolver != null ? biomeResolver.ResolveBiome(direction, normalizedHeight, noise, climate) : null;

        PlanetSurfaceSample sample = new PlanetSurfaceSample();
        sample.direction = direction;
        sample.normalizedHeight = normalizedHeight;
        sample.altitude = (normalizedHeight - waterLevel) * heightScale;
        sample.underwater = normalizedHeight < waterLevel;
        sample.noise = noise;
        sample.climate = climate;
        sample.biome = biome;
        return sample;
    }

    public Vector3 GetSurfacePosition(Vector3 direction)
    {
        PlanetSurfaceSample sample = SampleSurface(direction);
        return transform.position + sample.direction * (planetRadius + sample.altitude);
    }

    public Vector3 GetGravity(Vector3 worldPosition)
    {
        Vector3 toCenter = transform.position - worldPosition;
        return toCenter.sqrMagnitude > 0f ? toCenter.normalized : Vector3.down;
    }

    public PlanetSurfaceCoordinate WorldToSurfaceCoordinate(Vector3 worldPosition)
    {
        Vector3 fromCenter = worldPosition - transform.position;
        float altitude = Mathf.Max(0f, fromCenter.magnitude - planetRadius);
        return CubeSphereMeshBuilder.DirectionToCoordinate(fromCenter.normalized, altitude);
    }

    public CubeSphereMeshBuilder.PlanetChunk BuildOrLoadChunk(CubeSphereMeshBuilder.PlanetChunkKey key)
    {
        EnsureSubsystems();
        string chunkId = key.ToString();
        CubeSphereMeshBuilder.PlanetChunk chunk;
        if (_chunks.TryGetValue(chunkId, out chunk))
        {
            return chunk;
        }

        chunk = meshBuilder.BuildChunk(this, key, chunkResolution, chunksPerFace, transform, terrainMaterial);
        _chunks.Add(chunkId, chunk);
        return chunk;
    }

    public void UnloadChunk(CubeSphereMeshBuilder.PlanetChunkKey key)
    {
        string chunkId = key.ToString();
        CubeSphereMeshBuilder.PlanetChunk chunk;
        if (_chunks.TryGetValue(chunkId, out chunk))
        {
            if (chunk != null && chunk.gameObject != null)
            {
                DestroySmart(chunk.gameObject);
            }
            _chunks.Remove(chunkId);
        }
    }

    private void EnsureSubsystems()
    {
        if (meshBuilder == null)
        {
            meshBuilder = GetComponent<CubeSphereMeshBuilder>();
            if (meshBuilder == null) meshBuilder = gameObject.AddComponent<CubeSphereMeshBuilder>();
        }
        if (biomeResolver == null)
        {
            biomeResolver = GetComponent<PlanetBiomeResolver>();
            if (biomeResolver == null) biomeResolver = gameObject.AddComponent<PlanetBiomeResolver>();
        }
        if (surfaceSpawner == null)
        {
            surfaceSpawner = GetComponent<PlanetSurfaceSpawner>();
        }
        if (poiManager == null)
        {
            poiManager = GetComponent<PlanetPOIManager>();
        }
        if (waterSystem == null)
        {
            waterSystem = GetComponent<PlanetWaterSystem>();
        }
        if (streamingManager == null)
        {
            streamingManager = GetComponent<ChunkStreamingManager>();
        }
        if (mapSystem == null)
        {
            mapSystem = GetComponent<PlanetMapSystem>();
        }
    }

    private void DestroySmart(GameObject target)
    {
        if (target == null) return;
        if (Application.isPlaying) Destroy(target);
        else DestroyImmediate(target);
    }
}
