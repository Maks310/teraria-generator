using UnityEngine;

public class WorldObjectSpawner : MonoBehaviour
{
    [Header("References")]
    public WorldGenerator1 worldGenerator;
    public GameObject[] treePrefabs;
    public GameObject[] rockPrefabs;

    [Header("Sampling")]
    [Range(1, 256)] public int placementStep = 16;
    [Range(0f, 1f)] public float treeDensity = 0.08f;
    [Range(0f, 1f)] public float rockDensity = 0.04f;
    [Range(0f, 1f)] public float maxRiverSpawnStrength = 0.25f;
    public bool clearPreviousObjects = true;

    [Header("Randomization")]
    public int spawnSeedOffset = 9000;
    public Vector2 randomScaleRange = new Vector2(0.85f, 1.25f);

    [ContextMenu("Spawn World Objects")]
    public void SpawnObjects()
    {
        if (worldGenerator == null || worldGenerator.BiomeMap == null)
        {
            return;
        }

        if (clearPreviousObjects)
        {
            ClearSpawnedObjects();
        }

        int worldSize = worldGenerator.WorldSize;
        int step = Mathf.Max(1, placementStep);

        for (int z = 0; z < worldSize; z += step)
        {
            for (int x = 0; x < worldSize; x += step)
            {
                TrySpawnAt(x, z, step);
            }
        }
    }

    [ContextMenu("Clear Spawned Objects")]
    public void ClearSpawnedObjects()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private void TrySpawnAt(int gridX, int gridZ, int step)
    {
        float jitterX = (NoiseGenerator.Hash01(gridX, gridZ, spawnSeedOffset) - 0.5f) * step;
        float jitterZ = (NoiseGenerator.Hash01(gridX, gridZ, spawnSeedOffset + 1) - 0.5f) * step;
        float worldX = gridX + jitterX;
        float worldZ = gridZ + jitterZ;

        if (worldGenerator.IsUnderwater(worldX, worldZ) || worldGenerator.GetRiverAt(worldX, worldZ) > maxRiverSpawnStrength)
        {
            return;
        }

        BiomeType biome = worldGenerator.GetBiomeAt(worldX, worldZ);
        float height = worldGenerator.GetHeightAt(worldX, worldZ);
        float roll = NoiseGenerator.Hash01(gridX, gridZ, spawnSeedOffset + 2);

        if (CanSpawnTrees(biome) && roll < treeDensity * GetVegetationMultiplier(biome))
        {
            SpawnObject(treePrefabs, worldX, height, worldZ, gridX, gridZ);
            return;
        }

        if (CanSpawnRocks(biome) && roll < treeDensity * GetVegetationMultiplier(biome) + rockDensity * GetRockMultiplier(biome))
        {
            SpawnObject(rockPrefabs, worldX, height, worldZ, gridX, gridZ);
        }
    }

    private void SpawnObject(GameObject[] prefabs, float x, float y, float z, int gridX, int gridZ)
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            return;
        }

        int prefabIndex = Mathf.FloorToInt(NoiseGenerator.Hash01(gridX, gridZ, spawnSeedOffset + 3) * prefabs.Length);
        prefabIndex = Mathf.Clamp(prefabIndex, 0, prefabs.Length - 1);
        Quaternion rotation = Quaternion.Euler(0f, NoiseGenerator.Hash01(gridX, gridZ, spawnSeedOffset + 4) * 360f, 0f);
        GameObject instance = Instantiate(prefabs[prefabIndex], new Vector3(x, y, z), rotation, transform);
        float scale = Mathf.Lerp(randomScaleRange.x, randomScaleRange.y, NoiseGenerator.Hash01(gridX, gridZ, spawnSeedOffset + 5));
        instance.transform.localScale = instance.transform.localScale * Mathf.Max(0.01f, scale);
    }

    private bool CanSpawnTrees(BiomeType biome)
    {
        return biome == BiomeType.Plains || biome == BiomeType.Forest || biome == BiomeType.Tundra;
    }

    private bool CanSpawnRocks(BiomeType biome)
    {
        return biome == BiomeType.Plains || biome == BiomeType.Forest || biome == BiomeType.Tundra ||
               biome == BiomeType.Desert || biome == BiomeType.Mountains || biome == BiomeType.Snow;
    }

    private float GetVegetationMultiplier(BiomeType biome)
    {
        switch (biome)
        {
            case BiomeType.Forest:
                return 1.8f;
            case BiomeType.Plains:
                return 0.9f;
            case BiomeType.Tundra:
                return 0.25f;
            default:
                return 0f;
        }
    }

    private float GetRockMultiplier(BiomeType biome)
    {
        switch (biome)
        {
            case BiomeType.Mountains:
                return 2.5f;
            case BiomeType.Desert:
            case BiomeType.Tundra:
            case BiomeType.Snow:
                return 1.4f;
            default:
                return 0.75f;
        }
    }
}
