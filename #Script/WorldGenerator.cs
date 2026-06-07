using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class WorldGenerator : MonoBehaviour
{
    [Header("World Settings")]
    [Range(256, 4096)] public int worldSize = 2048;
    [Range(64, 1024)] public int meshResolution = 512;
    [Range(10f, 350f)] public float heightScale = 110f;
    public int seed = 42;
    public bool wrapEastWest = true;
    public bool wrapNorthSouth = true;

    [Header("Continents And Water")]
    [Range(0.20f, 0.75f)] public float waterLevel = 0.46f;
    [Range(0.20f, 0.80f)] public float landAmount = 0.50f;
    [Range(0.01f, 0.25f)] public float coastSoftness = 0.09f;
    [Range(0.05f, 0.45f)] public float oceanDepth = 0.28f;
    [Range(0f, 0.35f)] public float inlandSeaAmount = 0.12f;
    public float continentScale = 1.45f;
    public float domainWarpScale = 1.6f;
    [Range(0f, 0.35f)] public float domainWarpStrength = 0.10f;

    [Header("Terrain Detail")]
    public float terrainScale = 4.2f;
    [Range(1, 9)] public int terrainOctaves = 5;
    [Range(0f, 1f)] public float persistence = 0.48f;
    public float lacunarity = 2f;
    [Range(0f, 0.45f)] public float mountainAmount = 0.18f;
    public float mountainScale = 6.5f;
    [Range(1f, 6f)] public float mountainSharpness = 2.4f;
    [Range(0, 5)] public int smoothingPasses = 1;

    [Header("Climate And Biomes")]
    public float biomeScale = 2.1f;
    [Range(0f, 1f)] public float latitudeTemperatureInfluence = 0.58f;
    [Range(0f, 1f)] public float desertDryness = 0.43f;
    [Range(0f, 1f)] public float desertHeat = 0.58f;
    [Range(0f, 1f)] public float tundraCold = 0.38f;

    [Header("Biome Colors")]
    public Color plainsColorA = new Color(0.23f, 0.47f, 0.18f);
    public Color plainsColorB = new Color(0.45f, 0.64f, 0.25f);
    public Color desertColorA = new Color(0.69f, 0.53f, 0.28f);
    public Color desertColorB = new Color(0.93f, 0.78f, 0.43f);
    [Tooltip("Dark violet-blue tundra inspired by the reference image: purple soil, cold conifers, magic-night mood.")]
    public Color tundraColorA = new Color(0.16f, 0.10f, 0.18f);
    public Color tundraColorB = new Color(0.36f, 0.22f, 0.43f);
    public Color beachColor = new Color(0.62f, 0.55f, 0.39f);
    public Color shallowWaterColor = new Color(0.08f, 0.40f, 0.44f, 0.72f);
    public Color deepWaterColor = new Color(0.02f, 0.08f, 0.16f, 0.86f);

    [Header("Materials")]
    public Material terrainMaterial;
    public Material waterMaterial;

    [Header("Water Mesh")]
    [Range(8, 256)] public int waterResolution = 128;
    [Range(0f, 5f)] public float waterSurfaceOffset = 0.15f;

    [Header("Generation")]
    public bool generateOnStart = true;
    public bool autoUpdateInEditor = true;
    public bool updateCollider = true;
    public bool showBiomeDebug;
    public bool showWaterDebug;

    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private MeshCollider _meshCollider;
    private GameObject _waterObject;
    private bool _generationQueued;

    private float[,] _heightMap;
    private float[,] _continentalMap;
    private float[,] _temperatureMap;
    private float[,] _moistureMap;
    private float[,] _waterDepthMap;
    private BiomeType[,] _biomeMap;

    private static readonly int WaterLevelId = Shader.PropertyToID("_WaterLevel");
    private static readonly int WorldSizeId = Shader.PropertyToID("_WorldSize");
    private static readonly int ShallowWaterColorId = Shader.PropertyToID("_ShallowWaterColor");
    private static readonly int DeepWaterColorId = Shader.PropertyToID("_DeepWaterColor");

    public float[,] HeightMap { get { return _heightMap; } }
    public float[,] ContinentalMap { get { return _continentalMap; } }
    public float[,] TemperatureMap { get { return _temperatureMap; } }
    public float[,] MoistureMap { get { return _moistureMap; } }
    public float[,] WaterDepthMap { get { return _waterDepthMap; } }
    public BiomeType[,] BiomeMap { get { return _biomeMap; } }
    public int WorldSize { get { return worldSize; } }
    public float HeightScale { get { return heightScale; } }
    public float WaterLevel { get { return waterLevel; } }
    public float WaterHeight { get { return waterLevel * heightScale + waterSurfaceOffset; } }

    private void Awake()
    {
        SetupComponents();
    }

    private void Start()
    {
        if (generateOnStart && (Application.isPlaying || _heightMap == null))
        {
            GenerateWorld();
        }
    }

    private void OnEnable()
    {
        SetupComponents();
    }

    private void OnValidate()
    {
        ClampSettings();

#if UNITY_EDITOR
        if (autoUpdateInEditor && !Application.isPlaying)
        {
            QueueEditorGeneration();
        }
#endif
    }

    [ContextMenu("Generate World")]
    public void GenerateWorld()
    {
        ClampSettings();
        SetupComponents();
        GenerateMaps();
        SmoothHeightMap();
        CopyWrappedBorders();
        BuildTerrainMesh();
        BuildWaterMesh();
        ApplyMaterialSettings();
        Debug.Log("Generated seamless survival world " + worldSize + "x" + worldSize + " with " + meshResolution + " mesh cells. Seed: " + seed, this);
    }

    [ContextMenu("Clear Generated Water")]
    public void ClearGeneratedWater()
    {
        Transform child = transform.Find("Water");
        if (child != null)
        {
            DestroySmart(child.gameObject);
        }
        _waterObject = null;
    }

    private void ClampSettings()
    {
        worldSize = Mathf.Max(256, worldSize);
        meshResolution = Mathf.Clamp(meshResolution, 64, 1024);
        waterResolution = Mathf.Clamp(waterResolution, 8, 256);
        heightScale = Mathf.Max(1f, heightScale);
        continentScale = Mathf.Max(0.01f, continentScale);
        domainWarpScale = Mathf.Max(0.01f, domainWarpScale);
        terrainScale = Mathf.Max(0.01f, terrainScale);
        mountainScale = Mathf.Max(0.01f, mountainScale);
        biomeScale = Mathf.Max(0.01f, biomeScale);
        lacunarity = Mathf.Max(1.01f, lacunarity);
    }

#if UNITY_EDITOR
    private void QueueEditorGeneration()
    {
        if (_generationQueued)
        {
            return;
        }

        _generationQueued = true;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null)
            {
                return;
            }

            _generationQueued = false;
            if (!Application.isPlaying && autoUpdateInEditor)
            {
                GenerateWorld();
            }
        };
    }
#endif

    private void SetupComponents()
    {
        _meshFilter = GetComponent<MeshFilter>();
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshCollider = GetComponent<MeshCollider>();

        if (terrainMaterial != null)
        {
            _meshRenderer.sharedMaterial = terrainMaterial;
        }
    }

    private void GenerateMaps()
    {
        int points = meshResolution + 1;
        _heightMap = new float[points, points];
        _continentalMap = new float[points, points];
        _temperatureMap = new float[points, points];
        _moistureMap = new float[points, points];
        _waterDepthMap = new float[points, points];
        _biomeMap = new BiomeType[points, points];

        for (int z = 0; z < points; z++)
        {
            for (int x = 0; x < points; x++)
            {
                float nx = (float)x / meshResolution;
                float nz = (float)z / meshResolution;
                Vector2 warped = NoiseGenerator.SeamlessDomainWarp(nx, nz, domainWarpScale, domainWarpStrength, seed + 101);

                float continentNoise = NoiseGenerator.SeamlessOctavePerlin(warped.x, warped.y, 5, 0.55f, 2f, continentScale, seed + 200);
                float landMask = NoiseGenerator.SmoothStep(landAmount - coastSoftness, landAmount + coastSoftness, continentNoise);
                float basinNoise = NoiseGenerator.SeamlessOctavePerlin(warped.x, warped.y, 3, 0.52f, 2f, continentScale * 3.1f, seed + 240);
                float inlandBasins = NoiseGenerator.SmoothStep(1f - inlandSeaAmount, 1f, basinNoise) * landMask;
                landMask = Mathf.Clamp01(landMask - inlandBasins * 0.55f);

                float rolling = NoiseGenerator.SeamlessOctavePerlin(warped.x, warped.y, terrainOctaves, persistence, lacunarity, terrainScale, seed + 300);
                float ridges = NoiseGenerator.RidgedOctavePerlin(warped.x, warped.y, 5, 0.55f, 2.05f, mountainScale, seed + 400);
                float mountainMask = Mathf.Pow(NoiseGenerator.SmoothStep(1f - mountainAmount, 1f, ridges), mountainSharpness);
                float landHeight = Mathf.Lerp(waterLevel + 0.03f, 0.74f, rolling) + mountainMask * 0.24f;

                float seaFloor = waterLevel - oceanDepth * Mathf.Lerp(0.35f, 1f, NoiseGenerator.SeamlessOctavePerlin(warped.x, warped.y, 4, 0.5f, 2f, terrainScale * 0.75f, seed + 500));
                float height = Mathf.Lerp(seaFloor, landHeight, landMask);
                height = Mathf.Clamp01(height);

                float latitude = Mathf.Abs(nz - 0.5f) * 2f;
                float equatorWarmth = 1f - Mathf.Pow(latitude, 1.65f);
                float temperatureNoise = NoiseGenerator.SeamlessOctavePerlin(warped.x, warped.y, 3, 0.5f, 2f, biomeScale, seed + 600);
                float moistureNoise = NoiseGenerator.SeamlessOctavePerlin(warped.x, warped.y, 4, 0.5f, 2f, biomeScale * 1.25f, seed + 700);
                float temperature = Mathf.Clamp01(Mathf.Lerp(temperatureNoise, equatorWarmth, latitudeTemperatureInfluence) - mountainMask * 0.20f);
                float moisture = Mathf.Clamp01(moistureNoise + (waterLevel - height) * 0.45f);

                _heightMap[x, z] = height;
                _continentalMap[x, z] = landMask;
                _temperatureMap[x, z] = temperature;
                _moistureMap[x, z] = moisture;
                _waterDepthMap[x, z] = Mathf.Clamp01((waterLevel - height) / Mathf.Max(0.0001f, oceanDepth));
                _biomeMap[x, z] = PickBiome(height, temperature, moisture, landMask);
            }
        }
    }

    private BiomeType PickBiome(float height, float temperature, float moisture, float landMask)
    {
        if (height < waterLevel)
        {
            return BiomeType.Ocean;
        }

        if (height < waterLevel + 0.025f || landMask < 0.45f)
        {
            return BiomeType.Beach;
        }

        if (temperature <= tundraCold)
        {
            return BiomeType.Tundra;
        }

        if (temperature >= desertHeat && moisture <= desertDryness)
        {
            return BiomeType.Desert;
        }

        return BiomeType.Plains;
    }

    private void SmoothHeightMap()
    {
        int points = meshResolution + 1;
        for (int pass = 0; pass < smoothingPasses; pass++)
        {
            float[,] source = _heightMap;
            float[,] target = new float[points, points];

            for (int z = 0; z < points; z++)
            {
                for (int x = 0; x < points; x++)
                {
                    float sum = 0f;
                    int count = 0;
                    for (int oz = -1; oz <= 1; oz++)
                    {
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            int sx = WrapOrClampX(x + ox);
                            int sz = WrapOrClampZ(z + oz);
                            sum += source[sx, sz];
                            count++;
                        }
                    }
                    target[x, z] = sum / count;
                }
            }

            _heightMap = target;
        }

        for (int z = 0; z < points; z++)
        {
            for (int x = 0; x < points; x++)
            {
                _waterDepthMap[x, z] = Mathf.Clamp01((waterLevel - _heightMap[x, z]) / Mathf.Max(0.0001f, oceanDepth));
                _biomeMap[x, z] = PickBiome(_heightMap[x, z], _temperatureMap[x, z], _moistureMap[x, z], _continentalMap[x, z]);
            }
        }
    }

    private void CopyWrappedBorders()
    {
        int last = meshResolution;
        if (wrapEastWest)
        {
            for (int z = 0; z <= last; z++)
            {
                CopyMapCell(0, z, last, z);
            }
        }

        if (wrapNorthSouth)
        {
            for (int x = 0; x <= last; x++)
            {
                CopyMapCell(x, 0, x, last);
            }
        }
    }

    private void CopyMapCell(int sourceX, int sourceZ, int targetX, int targetZ)
    {
        _heightMap[targetX, targetZ] = _heightMap[sourceX, sourceZ];
        _continentalMap[targetX, targetZ] = _continentalMap[sourceX, sourceZ];
        _temperatureMap[targetX, targetZ] = _temperatureMap[sourceX, sourceZ];
        _moistureMap[targetX, targetZ] = _moistureMap[sourceX, sourceZ];
        _waterDepthMap[targetX, targetZ] = _waterDepthMap[sourceX, sourceZ];
        _biomeMap[targetX, targetZ] = _biomeMap[sourceX, sourceZ];
    }

    private void BuildTerrainMesh()
    {
        int points = meshResolution + 1;
        float cellSize = (float)worldSize / meshResolution;
        Vector3[] vertices = new Vector3[points * points];
        Vector3[] normals = new Vector3[points * points];
        Vector2[] uvs = new Vector2[points * points];
        Color[] colors = new Color[points * points];
        int[] triangles = new int[meshResolution * meshResolution * 6];

        for (int z = 0; z < points; z++)
        {
            for (int x = 0; x < points; x++)
            {
                int index = z * points + x;
                vertices[index] = new Vector3(x * cellSize, _heightMap[x, z] * heightScale, z * cellSize);
                uvs[index] = new Vector2((float)x / meshResolution, (float)z / meshResolution);
                colors[index] = GetBiomeVertexColor(x, z);
            }
        }

        int triangleIndex = 0;
        for (int z = 0; z < meshResolution; z++)
        {
            for (int x = 0; x < meshResolution; x++)
            {
                int a = z * points + x;
                int b = z * points + x + 1;
                int c = (z + 1) * points + x;
                int d = (z + 1) * points + x + 1;

                triangles[triangleIndex++] = a;
                triangles[triangleIndex++] = c;
                triangles[triangleIndex++] = b;
                triangles[triangleIndex++] = b;
                triangles[triangleIndex++] = c;
                triangles[triangleIndex++] = d;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "Generated Survival World";
        mesh.indexFormat = vertices.Length > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        normals = mesh.normals;

        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = Vector3.Lerp(Vector3.up, normals[i], 0.82f).normalized;
        }
        mesh.normals = normals;

        if (_meshFilter.sharedMesh != null)
        {
            DestroySmart(_meshFilter.sharedMesh);
        }
        _meshFilter.sharedMesh = mesh;

        if (updateCollider)
        {
            _meshCollider.sharedMesh = null;
            _meshCollider.sharedMesh = mesh;
        }
    }

    private Color GetBiomeVertexColor(int x, int z)
    {
        BiomeType biome = _biomeMap[x, z];
        float detail = NoiseGenerator.SeamlessOctavePerlin((float)x / meshResolution, (float)z / meshResolution, 3, 0.5f, 2f, 52f, seed + 810);
        Color a;
        Color b;

        switch (biome)
        {
            case BiomeType.Ocean:
                return Color.Lerp(deepWaterColor, shallowWaterColor, 1f - _waterDepthMap[x, z]);
            case BiomeType.Beach:
                return beachColor;
            case BiomeType.Desert:
                a = desertColorA;
                b = desertColorB;
                break;
            case BiomeType.Tundra:
                a = tundraColorA;
                b = tundraColorB;
                break;
            default:
                a = plainsColorA;
                b = plainsColorB;
                break;
        }

        return Color.Lerp(a, b, detail);
    }

    private void BuildWaterMesh()
    {
        if (waterMaterial == null)
        {
            ClearGeneratedWater();
            return;
        }

        if (_waterObject == null)
        {
            Transform existing = transform.Find("Water");
            if (existing != null)
            {
                _waterObject = existing.gameObject;
            }
        }

        if (_waterObject == null)
        {
            _waterObject = new GameObject("Water");
            _waterObject.transform.SetParent(transform, false);
        }

        _waterObject.transform.localPosition = new Vector3(0f, WaterHeight, 0f);
        _waterObject.transform.localRotation = Quaternion.identity;
        _waterObject.transform.localScale = Vector3.one;

        MeshFilter waterFilter = _waterObject.GetComponent<MeshFilter>();
        if (waterFilter == null)
        {
            waterFilter = _waterObject.AddComponent<MeshFilter>();
        }

        MeshRenderer waterRenderer = _waterObject.GetComponent<MeshRenderer>();
        if (waterRenderer == null)
        {
            waterRenderer = _waterObject.AddComponent<MeshRenderer>();
        }

        WaterController waterController = _waterObject.GetComponent<WaterController>();
        if (waterController == null)
        {
            waterController = _waterObject.AddComponent<WaterController>();
        }

        if (waterFilter.sharedMesh != null)
        {
            DestroySmart(waterFilter.sharedMesh);
        }
        waterFilter.sharedMesh = CreateWaterMesh();
        waterRenderer.sharedMaterial = waterMaterial;
        waterController.Initialize(Application.isPlaying ? waterRenderer.material : waterRenderer.sharedMaterial, worldSize);
    }

    private Mesh CreateWaterMesh()
    {
        int points = waterResolution + 1;
        float cellSize = (float)worldSize / waterResolution;
        Vector3[] vertices = new Vector3[points * points];
        Vector2[] uvs = new Vector2[points * points];
        int[] triangles = new int[waterResolution * waterResolution * 6];

        for (int z = 0; z < points; z++)
        {
            for (int x = 0; x < points; x++)
            {
                int index = z * points + x;
                vertices[index] = new Vector3(x * cellSize, 0f, z * cellSize);
                uvs[index] = new Vector2((float)x / waterResolution, (float)z / waterResolution);
            }
        }

        int t = 0;
        for (int z = 0; z < waterResolution; z++)
        {
            for (int x = 0; x < waterResolution; x++)
            {
                int a = z * points + x;
                int b = z * points + x + 1;
                int c = (z + 1) * points + x;
                int d = (z + 1) * points + x + 1;
                triangles[t++] = a;
                triangles[t++] = c;
                triangles[t++] = b;
                triangles[t++] = b;
                triangles[t++] = c;
                triangles[t++] = d;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "Generated Seamless Water";
        mesh.indexFormat = vertices.Length > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void ApplyMaterialSettings()
    {
        if (terrainMaterial != null)
        {
            terrainMaterial.SetFloat(WaterLevelId, transform.position.y + WaterHeight);
            terrainMaterial.SetFloat(WorldSizeId, worldSize);
            terrainMaterial.SetColor(ShallowWaterColorId, shallowWaterColor);
            terrainMaterial.SetColor(DeepWaterColorId, deepWaterColor);
        }

        if (waterMaterial != null)
        {
            waterMaterial.SetFloat(WorldSizeId, worldSize);
            waterMaterial.SetColor("_Color", shallowWaterColor);
            waterMaterial.SetColor("_DeepColor", deepWaterColor);
        }
    }

    public float GetHeightAt(float worldX, float worldZ)
    {
        if (_heightMap == null)
        {
            return transform.position.y;
        }

        return transform.position.y + SampleMapBilinear(_heightMap, worldX, worldZ) * heightScale;
    }

    public float GetWaterDepthAt(float worldX, float worldZ)
    {
        if (_waterDepthMap == null)
        {
            return 0f;
        }

        return SampleMapBilinear(_waterDepthMap, worldX, worldZ);
    }

    public float GetTemperatureAt(float worldX, float worldZ)
    {
        return _temperatureMap == null ? 0.5f : SampleMapBilinear(_temperatureMap, worldX, worldZ);
    }

    public float GetMoistureAt(float worldX, float worldZ)
    {
        return _moistureMap == null ? 0.5f : SampleMapBilinear(_moistureMap, worldX, worldZ);
    }

    public float GetRiverAt(float worldX, float worldZ)
    {
        return 0f;
    }

    public float GetOceanDepthAt(float worldX, float worldZ)
    {
        return GetWaterDepthAt(worldX, worldZ);
    }

    public BiomeType GetBiomeAt(float worldX, float worldZ)
    {
        if (_biomeMap == null)
        {
            return BiomeType.Plains;
        }

        float nx = NormalizeWorldX(worldX);
        float nz = NormalizeWorldZ(worldZ);
        int x = Mathf.Clamp(Mathf.FloorToInt(nx * meshResolution), 0, meshResolution);
        int z = Mathf.Clamp(Mathf.FloorToInt(nz * meshResolution), 0, meshResolution);
        return _biomeMap[x, z];
    }

    public bool IsUnderwater(float worldX, float worldZ)
    {
        return GetHeightAt(worldX, worldZ) < transform.position.y + WaterHeight;
    }

    private float SampleMapBilinear(float[,] map, float worldX, float worldZ)
    {
        float nx = NormalizeWorldX(worldX);
        float nz = NormalizeWorldZ(worldZ);
        float fx = nx * meshResolution;
        float fz = nz * meshResolution;
        int x0 = Mathf.Clamp(Mathf.FloorToInt(fx), 0, meshResolution);
        int z0 = Mathf.Clamp(Mathf.FloorToInt(fz), 0, meshResolution);
        int x1 = WrapOrClampX(x0 + 1);
        int z1 = WrapOrClampZ(z0 + 1);
        float tx = fx - Mathf.Floor(fx);
        float tz = fz - Mathf.Floor(fz);
        float a = Mathf.Lerp(map[x0, z0], map[x1, z0], tx);
        float b = Mathf.Lerp(map[x0, z1], map[x1, z1], tx);
        return Mathf.Lerp(a, b, tz);
    }

    private float NormalizeWorldX(float value)
    {
        float localValue = value - transform.position.x;
        float normalized = localValue / Mathf.Max(1f, worldSize);
        return normalized - Mathf.Floor(normalized);
    }

    private float NormalizeWorldZ(float value)
    {
        float localValue = value - transform.position.z;
        float normalized = localValue / Mathf.Max(1f, worldSize);
        return normalized - Mathf.Floor(normalized);
    }

    private int WrapOrClampX(int index)
    {
        if (wrapEastWest)
        {
            return WrapIndex(index);
        }

        return Mathf.Clamp(index, 0, meshResolution);
    }

    private int WrapOrClampZ(int index)
    {
        if (wrapNorthSouth)
        {
            return WrapIndex(index);
        }

        return Mathf.Clamp(index, 0, meshResolution);
    }

    private int WrapIndex(int index)
    {
        int points = meshResolution + 1;
        index %= points;
        if (index < 0)
        {
            index += points;
        }
        return index;
    }

    private void DestroySmart(Object objectToDestroy)
    {
        if (objectToDestroy == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(objectToDestroy);
        }
        else
        {
            DestroyImmediate(objectToDestroy);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_biomeMap == null || (!showBiomeDebug && !showWaterDebug))
        {
            return;
        }

        int step = Mathf.Max(1, meshResolution / 64);
        float cellSize = (float)worldSize / meshResolution;
        for (int z = 0; z <= meshResolution; z += step)
        {
            for (int x = 0; x <= meshResolution; x += step)
            {
                if (showWaterDebug && _waterDepthMap[x, z] <= 0f)
                {
                    continue;
                }

                Gizmos.color = showWaterDebug ? Color.Lerp(Color.cyan, Color.blue, _waterDepthMap[x, z]) : GetBiomeVertexColor(x, z);
                Vector3 position = transform.position + new Vector3(x * cellSize, _heightMap[x, z] * heightScale + 3f, z * cellSize);
                Gizmos.DrawSphere(position, 2f);
            }
        }
    }
#endif
}
