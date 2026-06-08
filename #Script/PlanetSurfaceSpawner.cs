using UnityEngine;

public class PlanetSurfaceSpawner : MonoBehaviour
{
    [Header("Chunk-Based Spawning")]
    public bool spawnAfterPlanetGeneration;
    [Range(1, 32)] public int samplesPerChunkAxis = 5;
    [Range(0f, 4f)] public float globalDensity = 1f;
    [Range(0f, 75f)] public float maxSlopeAngle = 38f;
    public int spawnSeedOffset = 17000;
    public bool clearPreviousObjects = true;

    [Header("Fallback Prefabs")]
    public GameObject[] grassPrefabs;
    public GameObject[] treePrefabs;
    public GameObject[] mushroomPrefabs;
    public GameObject[] crystalPrefabs;
    public GameObject[] rockPrefabs;
    public GameObject[] mobPrefabs;

    [ContextMenu("Spawn For Generated Planet")]
    public void SpawnForGeneratedPlanet()
    {
        PlanetGenerator generator = GetComponent<PlanetGenerator>();
        if (generator == null)
        {
            Debug.LogWarning("PlanetSurfaceSpawner needs a PlanetGenerator on the same GameObject.", this);
            return;
        }
        SpawnForLoadedChunks(generator);
    }

    public void SpawnForLoadedChunks(PlanetGenerator generator)
    {
        if (generator == null)
        {
            return;
        }

        if (clearPreviousObjects)
        {
            ClearSpawnedObjects();
        }

        foreach (CubeSphereMeshBuilder.PlanetChunk chunk in generator.LoadedChunks)
        {
            SpawnForChunk(generator, chunk.key);
        }
    }

    public void SpawnForChunk(PlanetGenerator generator, CubeSphereMeshBuilder.PlanetChunkKey key)
    {
        int sampleCount = Mathf.Max(1, samplesPerChunkAxis);
        for (int y = 0; y < sampleCount; y++)
        {
            for (int x = 0; x < sampleCount; x++)
            {
                float u = (key.x + (x + 0.5f) / sampleCount) / generator.chunksPerFace;
                float v = (key.y + (y + 0.5f) / sampleCount) / generator.chunksPerFace;
                TrySpawnAt(generator, key.face, u, v, x, y);
            }
        }
    }

    [ContextMenu("Clear Planet Surface Spawns")]
    public void ClearSpawnedObjects()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.GetComponent<PlanetPlacedObject>() != null)
            {
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }
    }

    private void TrySpawnAt(PlanetGenerator generator, int face, float u, float v, int cellX, int cellY)
    {
        Vector3 direction = CubeSphereMeshBuilder.FaceUvToDirection(face, u, v);
        PlanetSurfaceSample sample = generator.SampleSurface(direction);
        if (sample.underwater || sample.biome == null)
        {
            return;
        }

        if (ApproxSlope(generator, direction) > maxSlopeAngle)
        {
            return;
        }

        int hashX = Mathf.RoundToInt((face + 1) * 10000 + u * 100000f + cellX);
        int hashY = Mathf.RoundToInt(v * 100000f + cellY);
        float roll = NoiseGenerator.Hash01(hashX, hashY, generator.seed + spawnSeedOffset);

        GameObject prefab = PickPrefab(sample.biome, roll, hashX, hashY, generator.seed);
        if (prefab == null)
        {
            return;
        }

        Vector3 position = generator.transform.position + direction * (generator.planetRadius + sample.altitude);
        Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction) * Quaternion.Euler(0f, NoiseGenerator.Hash01(hashX, hashY, generator.seed + spawnSeedOffset + 7) * 360f, 0f);
        GameObject instance = Instantiate(prefab, position, rotation, transform);
        float scale = Mathf.Lerp(0.85f, 1.25f, NoiseGenerator.Hash01(hashX, hashY, generator.seed + spawnSeedOffset + 9));
        instance.transform.localScale *= scale;

        PlanetPlacedObject placedObject = instance.GetComponent<PlanetPlacedObject>();
        if (placedObject == null)
        {
            placedObject = instance.AddComponent<PlanetPlacedObject>();
        }
        placedObject.Initialize(new PlanetSurfaceCoordinate(direction, sample.altitude, face, new Vector2(u, v)), sample.biome.biomeId);
    }

    private GameObject PickPrefab(BiomeDefinition biome, float roll, int hashX, int hashY, int seed)
    {
        float density = Mathf.Clamp01(globalDensity * Mathf.Max(0.01f, biome.vegetationDensity + biome.rockDensity + biome.mobDensity) / 3f);
        if (roll > density * 0.22f)
        {
            return null;
        }

        if (biome.spawnEntries != null && biome.spawnEntries.Length > 0)
        {
            for (int i = 0; i < biome.spawnEntries.Length; i++)
            {
                PlanetSpawnEntry entry = biome.spawnEntries[i];
                if (entry != null && entry.prefab != null && roll < entry.probability * density)
                {
                    return entry.prefab;
                }
            }
        }

        switch (biome.biomeId)
        {
            case PlanetBiomeId.VioletForest:
            case PlanetBiomeId.MushroomOcean:
                return Pick(mushroomPrefabs, hashX, hashY, seed);
            case PlanetBiomeId.CrystalDesert:
            case PlanetBiomeId.EnergyCrystalFields:
            case PlanetBiomeId.Glassland:
                return Pick(crystalPrefabs, hashX, hashY, seed);
            case PlanetBiomeId.MagneticCliffs:
            case PlanetBiomeId.StoneTreeForest:
                return Pick(rockPrefabs, hashX, hashY, seed);
            case PlanetBiomeId.BioluminescentJungle:
            case PlanetBiomeId.GiantFlowerForest:
            case PlanetBiomeId.BlackForest:
                return Pick(treePrefabs, hashX, hashY, seed);
            default:
                return Pick(rockPrefabs, hashX, hashY, seed) ?? Pick(grassPrefabs, hashX, hashY, seed);
        }
    }

    private GameObject Pick(GameObject[] prefabs, int x, int y, int seed)
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            return null;
        }
        int index = Mathf.FloorToInt(NoiseGenerator.Hash01(x, y, seed + spawnSeedOffset + 3) * prefabs.Length);
        return prefabs[Mathf.Clamp(index, 0, prefabs.Length - 1)];
    }

    private float ApproxSlope(PlanetGenerator generator, Vector3 direction)
    {
        Vector3 tangent = Vector3.Cross(direction, Vector3.up);
        if (tangent.sqrMagnitude < 0.001f)
        {
            tangent = Vector3.Cross(direction, Vector3.right);
        }
        tangent.Normalize();
        Vector3 bitangent = Vector3.Cross(direction, tangent).normalized;
        float angle = 0.006f;
        float center = generator.SampleSurface(direction).altitude;
        float a = generator.SampleSurface((direction + tangent * angle).normalized).altitude;
        float b = generator.SampleSurface((direction + bitangent * angle).normalized).altitude;
        float steepness = Mathf.Max(Mathf.Abs(a - center), Mathf.Abs(b - center)) / Mathf.Max(0.001f, generator.planetRadius * angle);
        return Mathf.Atan(steepness) * Mathf.Rad2Deg;
    }
}
