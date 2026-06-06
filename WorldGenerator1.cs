using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class WorldGenerator1 : MonoBehaviour
{
    [Header("World Settings")]
    public int worldSize = 2048;
    public int meshResolution = 512; // Менше за worldSize для оптимізації
    public float heightScale = 100f;
    public int seed = 42;

    [Header("Noise Settings")]
    public float noiseScale = 4f;
    [Range(1, 8)] public int octaves = 4;
    [Range(0f, 1f)] public float persistence = 0.5f;
    public float lacunarity = 2f;

    [Header("Water Settings")]
    [Range(0f, 1f)] public float waterLevel = 0.3f;
    public Material waterMaterial;

    [Header("Biomes")]
    public BiomeData plainsbiome;
    public BiomeData desertBiome;
    public BiomeData tundraBiome;
    public float biomeScale = 2f;

    [Header("Materials")]
    public Material terrainMaterial;

    [Header("Debug")]
    public bool autoUpdate = true;
    public bool showBiomeDebug = false;

    // Приватні поля
    private MeshFilter _terrainMeshFilter;
    private MeshRenderer _terrainMeshRenderer;
    private MeshCollider _terrainCollider;
    private GameObject _waterObject;
    private float[,] _heightMap;
    private BiomeType[,] _biomeMap;
    private float[,] _temperatureMap;
    private float[,] _moistureMap;

    // Публічний доступ для систем спавну об'єктів
    public float[,] HeightMap => _heightMap;
    public BiomeType[,] BiomeMap => _biomeMap;
    public int WorldSize => worldSize;
    public float HeightScale => heightScale;
    public float WaterLevel => waterLevel;

    private void OnValidate()
    {
        if (autoUpdate && Application.isEditor)
        {
            // Затримка для уникнення зайвих перегенерацій
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null) GenerateWorld();
            };
        }
    }

    [ContextMenu("Generate World")]
    public void GenerateWorld()
    {
        CleanupPreviousGeneration();
        SetupComponents();
        GenerateMaps();
        GenerateTerrainMesh();
        GenerateWater();

        Debug.Log($"World generated: {worldSize}x{worldSize}, mesh resolution: {meshResolution}x{meshResolution}");
    }

    private void CleanupPreviousGeneration()
    {
        if (_waterObject != null)
        {
            if (Application.isPlaying)
                Destroy(_waterObject);
            else
                DestroyImmediate(_waterObject);
        }
    }

    private void SetupComponents()
    {
        _terrainMeshFilter = GetComponent<MeshFilter>();
        if (_terrainMeshFilter == null)
            _terrainMeshFilter = gameObject.AddComponent<MeshFilter>();

        _terrainMeshRenderer = GetComponent<MeshRenderer>();
        if (_terrainMeshRenderer == null)
            _terrainMeshRenderer = gameObject.AddComponent<MeshRenderer>();

        _terrainCollider = GetComponent<MeshCollider>();
        if (_terrainCollider == null)
            _terrainCollider = gameObject.AddComponent<MeshCollider>();

        if (terrainMaterial != null)
            _terrainMeshRenderer.sharedMaterial = terrainMaterial;
    }

    private void GenerateMaps()
    {
        int resolution = meshResolution + 1;
        _heightMap = new float[resolution, resolution];
        _biomeMap = new BiomeType[resolution, resolution];
        _temperatureMap = new float[resolution, resolution];
        _moistureMap = new float[resolution, resolution];

        // Генеруємо карти температури та вологості для біомів
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float normX = (float)x / meshResolution;
                float normY = (float)y / meshResolution;

                // Температура та вологість — окремі шумові карти
                _temperatureMap[x, y] = NoiseGenerator.SeamlessPerlin(normX, normY, biomeScale, seed + 1000);
                _moistureMap[x, y] = NoiseGenerator.SeamlessPerlin(normX, normY, biomeScale * 1.5f, seed + 2000);

                // Визначаємо біом
                _biomeMap[x, y] = DetermineBiome(_temperatureMap[x, y], _moistureMap[x, y]);

                // Генеруємо висоту з урахуванням біому
                float baseHeight = NoiseGenerator.SeamlessOctavePerlin(
                    normX, normY, octaves, persistence, lacunarity, noiseScale, seed
                );

                // Модифікуємо висоту залежно від біому
                BiomeData biome = GetBiomeData(_biomeMap[x, y]);
                if (biome != null)
                {
                    baseHeight *= biome.heightMultiplier;

                    // Додаємо деталізацію залежно від roughness
                    float detail = NoiseGenerator.SeamlessPerlin(normX, normY, noiseScale * 4f, seed + 500);
                    baseHeight += detail * biome.roughness * 0.1f;
                }

                _heightMap[x, y] = baseHeight;
            }
        }
    }

    private BiomeType DetermineBiome(float temperature, float moisture)
    {
        // Тундра: низька температура
        if (temperature < 0.35f)
            return BiomeType.Tundra;

        // Пустеля: висока температура, низька вологість
        if (temperature > 0.6f && moisture < 0.4f)
            return BiomeType.Desert;

        // Рівнини: все інше
        return BiomeType.Plains;
    }

    private BiomeData GetBiomeData(BiomeType type)
    {
        return type switch
        {
            BiomeType.Plains => plainsbiome,
            BiomeType.Desert => desertBiome,
            BiomeType.Tundra => tundraBiome,
            _ => plainsbiome
        };
    }

    private void GenerateTerrainMesh()
    {
        int resolution = meshResolution + 1;
        Vector3[] vertices = new Vector3[resolution * resolution];
        Color[] colors = new Color[resolution * resolution];
        Vector2[] uvs = new Vector2[resolution * resolution];
        int[] triangles = new int[meshResolution * meshResolution * 6];

        float cellSize = (float)worldSize / meshResolution;

        // Генеруємо вершини
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int index = y * resolution + x;

                float height = _heightMap[x, y] * heightScale;

                // Позиція у світових координатах
                vertices[index] = new Vector3(x * cellSize, height, y * cellSize);

                // UV для можливого текстурування
                uvs[index] = new Vector2((float)x / meshResolution, (float)y / meshResolution);

                // Колір з біому
                BiomeData biome = GetBiomeData(_biomeMap[x, y]);
                if (biome != null)
                {
                    // Варіація кольору для природності
                    float colorNoise = NoiseGenerator.SeamlessPerlin(
                        (float)x / meshResolution,
                        (float)y / meshResolution,
                        noiseScale * 8f,
                        seed + 3000
                    );
                    colors[index] = Color.Lerp(biome.groundColor, biome.groundColorVariation, colorNoise);

                    // Якщо під водою — затемнюємо
                    if (_heightMap[x, y] < waterLevel)
                    {
                        colors[index] = Color.Lerp(colors[index], new Color(0.2f, 0.3f, 0.4f), 0.5f);
                    }
                }
                else
                {
                    colors[index] = Color.magenta; // Debug: відсутній біом
                }
            }
        }

        // Генеруємо трикутники
        int triIndex = 0;
        for (int y = 0; y < meshResolution; y++)
        {
            for (int x = 0; x < meshResolution; x++)
            {
                int vertIndex = y * resolution + x;

                // Два трикутники на клітинку
                triangles[triIndex] = vertIndex;
                triangles[triIndex + 1] = vertIndex + resolution;
                triangles[triIndex + 2] = vertIndex + 1;

                triangles[triIndex + 3] = vertIndex + 1;
                triangles[triIndex + 4] = vertIndex + resolution;
                triangles[triIndex + 5] = vertIndex + resolution + 1;

                triIndex += 6;
            }
        }

        // Створюємо mesh
        Mesh mesh = new Mesh();
        mesh.name = "ProceduralTerrain";
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // Для великих mesh'ів

        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        _terrainMeshFilter.sharedMesh = mesh;
        _terrainCollider.sharedMesh = mesh;
    }

    private void GenerateWater()
    {
        _waterObject = new GameObject("Water");
        _waterObject.transform.SetParent(transform);
        _waterObject.transform.localPosition = new Vector3(worldSize / 2f, waterLevel * heightScale, worldSize / 2f);

        MeshFilter waterMeshFilter = _waterObject.AddComponent<MeshFilter>();
        MeshRenderer waterMeshRenderer = _waterObject.AddComponent<MeshRenderer>();

        // Простий плоский mesh для води
        Mesh waterMesh = CreateWaterMesh();
        waterMeshFilter.sharedMesh = waterMesh;

        if (waterMaterial != null)
            waterMeshRenderer.sharedMaterial = waterMaterial;

        // Додаємо компонент для анімації
        WaterController waterController = _waterObject.AddComponent<WaterController>();
        waterController.Initialize(waterMaterial);
    }

    private Mesh CreateWaterMesh()
    {
        // Вода трохи більша за терейн для візуального ефекту
        float waterSize = worldSize * 1.1f;
        int waterResolution = 64; // Менша роздільність для продуктивності

        Vector3[] vertices = new Vector3[(waterResolution + 1) * (waterResolution + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[waterResolution * waterResolution * 6];

        float cellSize = waterSize / waterResolution;
        float halfSize = waterSize / 2f;

        for (int y = 0; y <= waterResolution; y++)
        {
            for (int x = 0; x <= waterResolution; x++)
            {
                int index = y * (waterResolution + 1) + x;
                vertices[index] = new Vector3(x * cellSize - halfSize, 0, y * cellSize - halfSize);
                uvs[index] = new Vector2((float)x / waterResolution, (float)y / waterResolution);
            }
        }

        int triIndex = 0;
        for (int y = 0; y < waterResolution; y++)
        {
            for (int x = 0; x < waterResolution; x++)
            {
                int vertIndex = y * (waterResolution + 1) + x;

                triangles[triIndex] = vertIndex;
                triangles[triIndex + 1] = vertIndex + waterResolution + 1;
                triangles[triIndex + 2] = vertIndex + 1;

                triangles[triIndex + 3] = vertIndex + 1;
                triangles[triIndex + 4] = vertIndex + waterResolution + 1;
                triangles[triIndex + 5] = vertIndex + waterResolution + 2;

                triIndex += 6;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "WaterMesh";
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        return mesh;
    }

    /// <summary>
    /// Отримує висоту у світових координатах (для спавну об'єктів)
    /// </summary>
    public float GetHeightAt(float worldX, float worldZ)
    {
        if (_heightMap == null) return 0f;

        float normX = worldX / worldSize;
        float normZ = worldZ / worldSize;

        // Wraparound для безшовності
        normX = normX - Mathf.Floor(normX);
        normZ = normZ - Mathf.Floor(normZ);

        int mapX = Mathf.FloorToInt(normX * meshResolution);
        int mapZ = Mathf.FloorToInt(normZ * meshResolution);

        mapX = Mathf.Clamp(mapX, 0, meshResolution);
        mapZ = Mathf.Clamp(mapZ, 0, meshResolution);

        return _heightMap[mapX, mapZ] * heightScale;
    }

    /// <summary>
    /// Отримує біом у світових координатах
    /// </summary>
    public BiomeType GetBiomeAt(float worldX, float worldZ)
    {
        if (_biomeMap == null) return BiomeType.Plains;

        float normX = worldX / worldSize;
        float normZ = worldZ / worldSize;

        normX = normX - Mathf.Floor(normX);
        normZ = normZ - Mathf.Floor(normZ);

        int mapX = Mathf.FloorToInt(normX * meshResolution);
        int mapZ = Mathf.FloorToInt(normZ * meshResolution);

        mapX = Mathf.Clamp(mapX, 0, meshResolution);
        mapZ = Mathf.Clamp(mapZ, 0, meshResolution);

        return _biomeMap[mapX, mapZ];
    }

    /// <summary>
    /// Перевіряє чи точка під водою
    /// </summary>
    public bool IsUnderwater(float worldX, float worldZ)
    {
        return GetHeightAt(worldX, worldZ) < waterLevel * heightScale;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!showBiomeDebug || _biomeMap == null) return;

        int step = Mathf.Max(1, meshResolution / 64);
        float cellSize = (float)worldSize / meshResolution;

        for (int y = 0; y < meshResolution; y += step)
        {
            for (int x = 0; x < meshResolution; x += step)
            {
                BiomeType biome = _biomeMap[x, y];
                Gizmos.color = biome switch
                {
                    BiomeType.Plains => Color.green,
                    BiomeType.Desert => Color.yellow,
                    BiomeType.Tundra => Color.cyan,
                    _ => Color.white
                };

                Vector3 pos = transform.position + new Vector3(x * cellSize, GetHeightAt(x * cellSize, y * cellSize) + 5f, y * cellSize);
                Gizmos.DrawSphere(pos, 2f);
            }
        }
    }
#endif
}
