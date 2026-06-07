using UnityEngine;

[System.Serializable]
public class BiomeSpawnRule
{
    public BiomeType biome = BiomeType.Plains;
    [Range(0f, 1f)] public float treeChance = 0.08f;
    [Range(0f, 1f)] public float rockChance = 0.04f;
    public GameObject[] extraPrefabs;
    [Range(0f, 1f)] public float extraChance = 0.02f;
}

public class WorldObjectSpawner : MonoBehaviour
{
    [Header("References")]
    public WorldGenerator worldGenerator;
    public GameObject[] treePrefabs;
    public GameObject[] rockPrefabs;

    [Header("Biome Rules")]
    public BiomeSpawnRule[] biomeRules =
    {
        new BiomeSpawnRule { biome = BiomeType.Plains, treeChance = 0.08f, rockChance = 0.035f, extraChance = 0.02f },
        new BiomeSpawnRule { biome = BiomeType.Desert, treeChance = 0.00f, rockChance = 0.08f, extraChance = 0.025f },
        new BiomeSpawnRule { biome = BiomeType.Tundra, treeChance = 0.045f, rockChance = 0.07f, extraChance = 0.04f }
    };

    [Header("Sampling")]
    [Range(2, 256)] public int placementStep = 20;
    [Range(0f, 1f)] public float globalDensity = 1f;
    public bool clearPreviousObjects = true;
    public bool skipBeaches = true;

    [Header("Randomization")]
    public int spawnSeedOffset = 9000;
    public Vector2 randomScaleRange = new Vector2(0.85f, 1.25f);
    [Range(0f, 45f)] public float maxSlopeAngle = 32f;

    [ContextMenu("Spawn World Objects")]
    public void SpawnObjects()
    {
        if (worldGenerator == null || worldGenerator.BiomeMap == null)
        {
            Debug.LogWarning("Assign a generated WorldGenerator before spawning objects.", this);
            return;
        }

        if (clearPreviousObjects)
        {
            ClearSpawnedObjects();
        }

        int worldSize = worldGenerator.WorldSize;
        int step = Mathf.Max(2, placementStep);
        Vector3 origin = worldGenerator.transform.position;

        for (int z = 0; z < worldSize; z += step)
        {
            for (int x = 0; x < worldSize; x += step)
            {
                TrySpawnAt(origin, x, z, step);
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

    private void TrySpawnAt(Vector3 origin, int gridX, int gridZ, int step)
    {
        float jitterX = (NoiseGenerator.Hash01(gridX, gridZ, spawnSeedOffset) - 0.5f) * step;
        float jitterZ = (NoiseGenerator.Hash01(gridX, gridZ, spawnSeedOffset + 1) - 0.5f) * step;
        float worldX = origin.x + gridX + jitterX;
        float worldZ = origin.z + gridZ + jitterZ;

        if (worldGenerator.IsUnderwater(worldX, worldZ))
        {
            return;
        }

        BiomeType biome = worldGenerator.GetBiomeAt(worldX, worldZ);
        if (skipBeaches && biome == BiomeType.Beach)
        {
            return;
        }

        BiomeSpawnRule rule = GetRuleForBiome(biome);
        if (rule == null)
        {
            return;
        }

        if (GetApproxSlope(worldX, worldZ, step) > maxSlopeAngle)
        {
            return;
        }

        float height = worldGenerator.GetHeightAt(worldX, worldZ);
        float roll = NoiseGenerator.Hash01(gridX, gridZ, spawnSeedOffset + 2);
        float treeLimit = rule.treeChance * globalDensity;
        float rockLimit = treeLimit + rule.rockChance * globalDensity;
        float extraLimit = rockLimit + rule.extraChance * globalDensity;

        if (roll < treeLimit)
        {
            SpawnObject(treePrefabs, worldX, height, worldZ, gridX, gridZ);
        }
        else if (roll < rockLimit)
        {
            SpawnObject(rockPrefabs, worldX, height, worldZ, gridX, gridZ);
        }
        else if (roll < extraLimit)
        {
            SpawnObject(rule.extraPrefabs, worldX, height, worldZ, gridX, gridZ);
        }
    }

    private float GetApproxSlope(float worldX, float worldZ, int step)
    {
        float offset = Mathf.Max(1f, step * 0.35f);
        float center = worldGenerator.GetHeightAt(worldX, worldZ);
        float dx = Mathf.Abs(worldGenerator.GetHeightAt(worldX + offset, worldZ) - center);
        float dz = Mathf.Abs(worldGenerator.GetHeightAt(worldX, worldZ + offset) - center);
        float steepness = Mathf.Max(dx, dz) / offset;
        return Mathf.Atan(steepness) * Mathf.Rad2Deg;
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

    private BiomeSpawnRule GetRuleForBiome(BiomeType biome)
    {
        if (biomeRules == null)
        {
            return null;
        }

        for (int i = 0; i < biomeRules.Length; i++)
        {
            if (biomeRules[i] != null && biomeRules[i].biome == biome)
            {
                return biomeRules[i];
            }
        }

        return null;
    }
}
