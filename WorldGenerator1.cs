using UnityEngine;

[ExecuteInEditMode]
public class WorldGenerator1 : MonoBehaviour
{
    [Header("World Settings")]
    [Range(32, 8192)] public int worldSize = 2048;
    [Range(32, 1024)] public int meshResolution = 512;
    [Range(1f, 1000f)] public float heightScale = 100f;
    public int seed = 42;
    public bool wrapEastWest = true;
    public bool wrapNorthSouth = true;

    [Header("Continent Shape")]
    [Range(0.05f, 1f)] public float landMass = 0.48f;
    [Range(0.01f, 0.5f)] public float coastBlend = 0.12f;
    public float continentScale = 1.65f;
    public float domainWarpScale = 1.25f;
    [Range(0f, 0.35f)] public float domainWarpStrength = 0.08f;
    [Range(0f, 1f)] public float polarOceanBias = 0.18f;

    [Header("Height Noise")]
    public float noiseScale = 4f;
    [Range(1, 10)] public int octaves = 5;
    [Range(0f, 1f)] public float persistence = 0.48f;
    public float lacunarity = 2f;
    [Range(0f, 1f)] public float terrainSmoothness = 0.35f;
    [Range(0, 6)] public int smoothingPasses = 2;

    [Header("Mountains")]
    public float mountainScale = 5.5f;
    [Range(0f, 1f)] public float mountainAmount = 0.52f;
    [Range(0f, 2f)] public float mountainHeight = 0.55f;
    [Range(0.5f, 4f)] public float mountainSharpness = 1.8f;

    [Header("Water")]
    [Range(0f, 1f)] public float waterLevel = 0.34f;
    [Range(0f, 1f)] public float oceanDepth = 0.42f;
    [Range(0f, 1f)] public float shelfBlend = 0.18f;
    public Material waterMaterial;
    [Range(8, 256)] public int waterResolution = 96;

    [Header("Rivers")]
    public bool generateRivers = true;
    [Range(0, 256)] public int maxRiverSources = 72;
    [Range(0f, 1f)] public float riverSourceMinHeight = 0.62f;
    [Range(0.001f, 0.08f)] public float riverWidth = 0.012f;
    [Range(0f, 0.2f)] public float riverCarveDepth = 0.055f;
    [Range(64, 8192)] public int maxRiverLength = 1800;

    [Header("Climate & Biomes")]
    public BiomeData oceanBiome;
    public BiomeData beachBiome;
    public BiomeData plainsbiome;
    public BiomeData forestBiome;
    public BiomeData desertBiome;
    public BiomeData tundraBiome;
    public BiomeData mountainsBiome;
    public BiomeData snowBiome;
    public float biomeScale = 2f;
    [Range(0f, 1f)] public float latitudeTemperatureInfluence = 0.65f;
    [Range(0f, 1f)] public float snowHeight = 0.76f;

    [Header("Materials")]
    public Material terrainMaterial;

    [Header("Debug")]
    public bool autoUpdate = true;
    public bool showBiomeDebug = false;
    public bool showRiverDebug = false;

    private MeshFilter _terrainMeshFilter;
    private MeshRenderer _terrainMeshRenderer;
    private MeshCollider _terrainCollider;
    private GameObject _waterObject;
    private float[,] _heightMap;
    private float[,] _riverMap;
    private float[,] _oceanDepthMap;
    private BiomeType[,] _biomeMap;
    private float[,] _temperatureMap;
    private float[,] _moistureMap;

    public float[,] HeightMap { get { return _heightMap; } }
    public float[,] RiverMap { get { return _riverMap; } }
    public float[,] OceanDepthMap { get { return _oceanDepthMap; } }
    public BiomeType[,] BiomeMap { get { return _biomeMap; } }
    public int WorldSize { get { return worldSize; } }
    public float HeightScale { get { return heightScale; } }
    public float WaterLevel { get { return waterLevel; } }

    private void OnValidate()
    {
        worldSize = Mathf.Max(32, worldSize);
        meshResolution = Mathf.Clamp(meshResolution, 32, 1024);
        heightScale = Mathf.Max(1f, heightScale);
        lacunarity = Mathf.Max(1.01f, lacunarity);
        continentScale = Mathf.Max(0.01f, continentScale);
        noiseScale = Mathf.Max(0.01f, noiseScale);
        mountainScale = Mathf.Max(0.01f, mountainScale);
        biomeScale = Mathf.Max(0.01f, biomeScale);

#if UNITY_EDITOR
        if (autoUpdate && Application.isEditor)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null)
                {
                    GenerateWorld();
                }
            };
        }
#endif
    }

    [ContextMenu("Generate World")]
    public void GenerateWorld()
    {
        CleanupPreviousGeneration();
        SetupComponents();
        GenerateMaps();
        GenerateTerrainMesh();
        GenerateWater();

        Debug.Log($"World generated: {worldSize}x{worldSize}, mesh resolution: {meshResolution}x{meshResolution}, seed: {seed}");
    }

    private void CleanupPreviousGeneration()
    {
        if (_waterObject == null)
        {
            Transform existingWater = transform.Find("Water");
            if (existingWater != null)
            {
                _waterObject = existingWater.gameObject;
            }
        }

        if (_waterObject != null)
        {
            if (Application.isPlaying)
            {
                Destroy(_waterObject);
            }
            else
            {
                DestroyImmediate(_waterObject);
            }
        }
    }

    private void SetupComponents()
    {
        _terrainMeshFilter = GetComponent<MeshFilter>();
        if (_terrainMeshFilter == null)
        {
            _terrainMeshFilter = gameObject.AddComponent<MeshFilter>();
        }

        _terrainMeshRenderer = GetComponent<MeshRenderer>();
        if (_terrainMeshRenderer == null)
        {
            _terrainMeshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        _terrainCollider = GetComponent<MeshCollider>();
        if (_terrainCollider == null)
        {
            _terrainCollider = gameObject.AddComponent<MeshCollider>();
        }

        if (terrainMaterial != null)
        {
            _terrainMeshRenderer.sharedMaterial = terrainMaterial;
        }
    }

    private void GenerateMaps()
    {
        int resolution = meshResolution + 1;
        _heightMap = new float[resolution, resolution];
        _riverMap = new float[resolution, resolution];
        _oceanDepthMap = new float[resolution, resolution];
        _biomeMap = new BiomeType[resolution, resolution];
        _temperatureMap = new float[resolution, resolution];
        _moistureMap = new float[resolution, resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float normX = (float)x / meshResolution;
                float normY = (float)y / meshResolution;
                Vector2 warped = NoiseGenerator.SeamlessDomainWarp(normX, normY, domainWarpScale, domainWarpStrength, seed + 41);

                float latitude = Mathf.Abs(normY - 0.5f) * 2f;
                float polarMask = Mathf.Pow(latitude, 2.2f);
                float equatorWarmth = 1f - polarMask;

                float continentNoise = NoiseGenerator.SeamlessOctavePerlin(warped.x, warped.y, 4, 0.55f, 2f, continentScale, seed + 100);
                continentNoise -= polarMask * polarOceanBias;
                float landMask = NoiseGenerator.SmoothStep(landMass - coastBlend, landMass + coastBlend, continentNoise);

                float rollingTerrain = NoiseGenerator.SeamlessOctavePerlin(warped.x, warped.y, octaves, persistence, lacunarity, noiseScale, seed + 200);
                float ridges = NoiseGenerator.RidgedOctavePerlin(warped.x, warped.y, 5, 0.55f, 2.05f, mountainScale, seed + 300);
                float mountainMask = NoiseGenerator.SmoothStep(mountainAmount, 1f, ridges);
                float mountains = Mathf.Pow(mountainMask, mountainSharpness) * mountainHeight;

                float landHeight = Mathf.Lerp(0.38f, 0.68f, rollingTerrain) + mountains;
                float seaFloorNoise = NoiseGenerator.SeamlessOctavePerlin(warped.x, warped.y, 4, 0.5f, 2f, noiseScale * 0.75f, seed + 400);
                float seaFloor = waterLevel - oceanDepth * Mathf.Lerp(0.35f, 1f, seaFloorNoise);

                float shelf = NoiseGenerator.SmoothStep(0f, Mathf.Max(0.0001f, shelfBlend), landMask);
                float height = Mathf.Lerp(seaFloor, landHeight, shelf);
                height = Mathf.Lerp(height, waterLevel + (height - waterLevel) * (1f - terrainSmoothness * 0.45f), 1f - landMask);

                _heightMap[x, y] = Mathf.Clamp01(height);
                _temperatureMap[x, y] = Mathf.Clamp01(Mathf.Lerp(NoiseGenerator.SeamlessPerlin(warped.x, warped.y, biomeScale, seed + 1000), equatorWarmth, latitudeTemperatureInfluence));
                _moistureMap[x, y] = Mathf.Clamp01(NoiseGenerator.SeamlessOctavePerlin(warped.x, warped.y, 4, 0.55f, 2f, biomeScale * 1.4f, seed + 2000));
            }
        }

        SmoothHeightMap(smoothingPasses);

        if (generateRivers)
        {
            GenerateRiverMap();
            CarveRivers();
            SmoothHeightMap(Mathf.Max(0, smoothingPasses / 2));
        }

        CopyWrappedBorders();
        BuildDerivedMaps();
    }

    private void SmoothHeightMap(int passes)
    {
        if (passes <= 0 || terrainSmoothness <= 0f)
        {
            return;
        }

        int resolution = meshResolution + 1;
        float[,] buffer = new float[resolution, resolution];

        for (int pass = 0; pass < passes; pass++)
        {
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    float sum = 0f;
                    float weight = 0f;

                    for (int oy = -1; oy <= 1; oy++)
                    {
                        for (int ox = -1; ox <= 1; ox++)
                        {
                            int sx = WrapOrClampX(x + ox);
                            int sy = WrapOrClampY(y + oy);
                            float sampleWeight = ox == 0 && oy == 0 ? 4f : (Mathf.Abs(ox) + Mathf.Abs(oy) == 1 ? 2f : 1f);
                            sum += _heightMap[sx, sy] * sampleWeight;
                            weight += sampleWeight;
                        }
                    }

                    buffer[x, y] = Mathf.Lerp(_heightMap[x, y], sum / weight, terrainSmoothness);
                }
            }

            float[,] swap = _heightMap;
            _heightMap = buffer;
            buffer = swap;
        }
    }

    private void GenerateRiverMap()
    {
        int resolution = meshResolution + 1;
        int sourcesCreated = 0;
        int scanStep = Mathf.Max(2, meshResolution / Mathf.Max(8, Mathf.RoundToInt(Mathf.Sqrt(Mathf.Max(1, maxRiverSources)) * 4f)));

        for (int y = 0; y < meshResolution && sourcesCreated < maxRiverSources; y += scanStep)
        {
            for (int x = 0; x < meshResolution && sourcesCreated < maxRiverSources; x += scanStep)
            {
                float jitter = NoiseGenerator.Hash01(x, y, seed + 5000);
                int sx = WrapOrClampX(x + Mathf.RoundToInt((jitter - 0.5f) * scanStep));
                int sy = WrapOrClampY(y + Mathf.RoundToInt((NoiseGenerator.Hash01(x, y, seed + 5001) - 0.5f) * scanStep));

                if (_heightMap[sx, sy] < riverSourceMinHeight || _heightMap[sx, sy] <= waterLevel + 0.08f)
                {
                    continue;
                }

                if (NoiseGenerator.Hash01(sx, sy, seed + 5002) > 0.55f)
                {
                    continue;
                }

                TraceRiver(sx, sy, sourcesCreated);
                sourcesCreated++;
            }
        }
    }

    private void TraceRiver(int startX, int startY, int riverIndex)
    {
        int x = startX;
        int y = startY;
        int riverRadius = Mathf.Max(1, Mathf.RoundToInt(riverWidth * meshResolution));
        float flow = 0.35f + NoiseGenerator.Hash01(startX, startY, seed + 6000) * 0.65f;

        for (int step = 0; step < maxRiverLength; step++)
        {
            AddRiverStamp(x, y, riverRadius, flow);

            if (_heightMap[x, y] <= waterLevel + 0.01f)
            {
                break;
            }

            int nextX = x;
            int nextY = y;
            float bestHeight = _heightMap[x, y];

            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    if (ox == 0 && oy == 0)
                    {
                        continue;
                    }

                    int sx = WrapOrClampX(x + ox);
                    int sy = WrapOrClampY(y + oy);
                    float meander = NoiseGenerator.Hash01(sx + riverIndex * 17, sy - riverIndex * 31, seed + step) * 0.006f;
                    float candidate = _heightMap[sx, sy] + meander;

                    if (candidate < bestHeight)
                    {
                        bestHeight = candidate;
                        nextX = sx;
                        nextY = sy;
                    }
                }
            }

            if (nextX == x && nextY == y)
            {
                break;
            }

            flow = Mathf.Clamp01(flow + 0.004f);
            x = nextX;
            y = nextY;
        }
    }

    private void AddRiverStamp(int centerX, int centerY, int radius, float flow)
    {
        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                float distance = Mathf.Sqrt(x * x + y * y) / Mathf.Max(1f, radius);
                if (distance > 1f)
                {
                    continue;
                }

                int sx = WrapOrClampX(centerX + x);
                int sy = WrapOrClampY(centerY + y);
                float strength = (1f - distance) * flow;
                _riverMap[sx, sy] = Mathf.Max(_riverMap[sx, sy], strength);
            }
        }
    }

    private void CarveRivers()
    {
        int resolution = meshResolution + 1;
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                if (_riverMap[x, y] <= 0f || _heightMap[x, y] <= waterLevel)
                {
                    continue;
                }

                _heightMap[x, y] = Mathf.Clamp01(_heightMap[x, y] - _riverMap[x, y] * riverCarveDepth);
            }
        }
    }

    private void BuildDerivedMaps()
    {
        int resolution = meshResolution + 1;
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                _oceanDepthMap[x, y] = Mathf.Clamp01((waterLevel - _heightMap[x, y]) / Mathf.Max(0.0001f, oceanDepth));
                _biomeMap[x, y] = DetermineBiome(_temperatureMap[x, y], _moistureMap[x, y], _heightMap[x, y], _riverMap[x, y]);
            }
        }
    }

    private BiomeType DetermineBiome(float temperature, float moisture, float height, float river)
    {
        if (height < waterLevel)
        {
            return BiomeType.Ocean;
        }

        if (height < waterLevel + 0.035f)
        {
            return BiomeType.Beach;
        }

        if (height > snowHeight && temperature < 0.55f)
        {
            return BiomeType.Snow;
        }

        if (height > 0.70f || (height > 0.62f && river < 0.2f))
        {
            return BiomeType.Mountains;
        }

        if (temperature < 0.32f)
        {
            return BiomeType.Tundra;
        }

        if (temperature > 0.62f && moisture < 0.38f)
        {
            return BiomeType.Desert;
        }

        if (moisture > 0.58f)
        {
            return BiomeType.Forest;
        }

        return BiomeType.Plains;
    }

    private BiomeData GetBiomeData(BiomeType type)
    {
        switch (type)
        {
            case BiomeType.Ocean:
                return oceanBiome;
            case BiomeType.Beach:
                return beachBiome;
            case BiomeType.Forest:
                return forestBiome != null ? forestBiome : plainsbiome;
            case BiomeType.Desert:
                return desertBiome;
            case BiomeType.Tundra:
                return tundraBiome;
            case BiomeType.Mountains:
                return mountainsBiome != null ? mountainsBiome : tundraBiome;
            case BiomeType.Snow:
                return snowBiome != null ? snowBiome : tundraBiome;
            default:
                return plainsbiome;
        }
    }

    private void GenerateTerrainMesh()
    {
        int resolution = meshResolution + 1;
        Vector3[] vertices = new Vector3[resolution * resolution];
        Color[] colors = new Color[resolution * resolution];
        Vector2[] uvs = new Vector2[resolution * resolution];
        int[] triangles = new int[meshResolution * meshResolution * 6];
        float cellSize = (float)worldSize / meshResolution;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int index = y * resolution + x;
                float height = _heightMap[x, y] * heightScale;
                vertices[index] = new Vector3(x * cellSize, height, y * cellSize);
                uvs[index] = new Vector2((float)x / meshResolution, (float)y / meshResolution);
                colors[index] = GetVertexColor(x, y);
            }
        }

        int triIndex = 0;
        for (int y = 0; y < meshResolution; y++)
        {
            for (int x = 0; x < meshResolution; x++)
            {
                int vertIndex = y * resolution + x;
                triangles[triIndex] = vertIndex;
                triangles[triIndex + 1] = vertIndex + resolution;
                triangles[triIndex + 2] = vertIndex + 1;
                triangles[triIndex + 3] = vertIndex + 1;
                triangles[triIndex + 4] = vertIndex + resolution;
                triangles[triIndex + 5] = vertIndex + resolution + 1;
                triIndex += 6;
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "ProceduralTerrain";
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        _terrainMeshFilter.sharedMesh = mesh;
        _terrainCollider.sharedMesh = mesh;
    }

    private Color GetVertexColor(int x, int y)
    {
        BiomeType biomeType = _biomeMap[x, y];
        BiomeData biome = GetBiomeData(biomeType);
        Color color;

        if (biome != null)
        {
            float colorNoise = NoiseGenerator.SeamlessPerlin((float)x / meshResolution, (float)y / meshResolution, noiseScale * 8f, seed + 3000);
            color = Color.Lerp(biome.groundColor, biome.groundColorVariation, colorNoise);
        }
        else
        {
            color = DefaultBiomeColor(biomeType);
        }

        if (_heightMap[x, y] < waterLevel)
        {
            Color shallow = new Color(0.08f, 0.38f, 0.55f);
            Color deep = new Color(0.01f, 0.04f, 0.14f);
            color = Color.Lerp(shallow, deep, _oceanDepthMap[x, y]);
        }
        else if (_riverMap[x, y] > 0.05f)
        {
            color = Color.Lerp(color, new Color(0.05f, 0.36f, 0.72f), Mathf.Clamp01(_riverMap[x, y] * 1.4f));
        }

        return color;
    }

    private Color DefaultBiomeColor(BiomeType biomeType)
    {
        switch (biomeType)
        {
            case BiomeType.Ocean:
                return new Color(0.03f, 0.14f, 0.35f);
            case BiomeType.Beach:
                return new Color(0.78f, 0.70f, 0.48f);
            case BiomeType.Forest:
                return new Color(0.12f, 0.42f, 0.16f);
            case BiomeType.Desert:
                return new Color(0.76f, 0.61f, 0.32f);
            case BiomeType.Tundra:
                return new Color(0.55f, 0.66f, 0.62f);
            case BiomeType.Mountains:
                return new Color(0.38f, 0.36f, 0.33f);
            case BiomeType.Snow:
                return new Color(0.88f, 0.92f, 0.95f);
            default:
                return new Color(0.30f, 0.58f, 0.20f);
        }
    }

    private void GenerateWater()
    {
        _waterObject = new GameObject("Water");
        _waterObject.transform.SetParent(transform);
        _waterObject.transform.localPosition = new Vector3(worldSize / 2f, waterLevel * heightScale, worldSize / 2f);

        MeshFilter waterMeshFilter = _waterObject.AddComponent<MeshFilter>();
        MeshRenderer waterMeshRenderer = _waterObject.AddComponent<MeshRenderer>();
        waterMeshFilter.sharedMesh = CreateWaterMesh();

        if (waterMaterial != null)
        {
            waterMeshRenderer.sharedMaterial = waterMaterial;
        }

        WaterController waterController = _waterObject.AddComponent<WaterController>();
        waterController.Initialize(waterMaterial);
    }

    private Mesh CreateWaterMesh()
    {
        float waterSize = worldSize * 1.04f;
        int resolution = Mathf.Clamp(waterResolution, 8, 256);
        Vector3[] vertices = new Vector3[(resolution + 1) * (resolution + 1)];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[resolution * resolution * 6];
        float cellSize = waterSize / resolution;
        float halfSize = waterSize / 2f;

        for (int y = 0; y <= resolution; y++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                int index = y * (resolution + 1) + x;
                vertices[index] = new Vector3(x * cellSize - halfSize, 0f, y * cellSize - halfSize);
                uvs[index] = new Vector2((float)x / resolution, (float)y / resolution);
            }
        }

        int triIndex = 0;
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                int vertIndex = y * (resolution + 1) + x;
                triangles[triIndex] = vertIndex;
                triangles[triIndex + 1] = vertIndex + resolution + 1;
                triangles[triIndex + 2] = vertIndex + 1;
                triangles[triIndex + 3] = vertIndex + 1;
                triangles[triIndex + 4] = vertIndex + resolution + 1;
                triangles[triIndex + 5] = vertIndex + resolution + 2;
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

    public float GetHeightAt(float worldX, float worldZ)
    {
        if (_heightMap == null)
        {
            return 0f;
        }

        return SampleMapBilinear(_heightMap, worldX, worldZ) * heightScale;
    }

    public float GetRiverAt(float worldX, float worldZ)
    {
        if (_riverMap == null)
        {
            return 0f;
        }

        return SampleMapBilinear(_riverMap, worldX, worldZ);
    }

    public float GetOceanDepthAt(float worldX, float worldZ)
    {
        if (_oceanDepthMap == null)
        {
            return 0f;
        }

        return SampleMapBilinear(_oceanDepthMap, worldX, worldZ);
    }

    public BiomeType GetBiomeAt(float worldX, float worldZ)
    {
        if (_biomeMap == null)
        {
            return BiomeType.Plains;
        }

        float normX = NormalizeWorldCoordinate(worldX);
        float normZ = NormalizeWorldCoordinate(worldZ);
        int mapX = Mathf.Clamp(Mathf.FloorToInt(normX * meshResolution), 0, meshResolution);
        int mapZ = Mathf.Clamp(Mathf.FloorToInt(normZ * meshResolution), 0, meshResolution);
        return _biomeMap[mapX, mapZ];
    }

    public bool IsUnderwater(float worldX, float worldZ)
    {
        return GetHeightAt(worldX, worldZ) < waterLevel * heightScale;
    }

    private float SampleMapBilinear(float[,] map, float worldX, float worldZ)
    {
        float normX = NormalizeWorldCoordinate(worldX);
        float normZ = NormalizeWorldCoordinate(worldZ);
        float fx = normX * meshResolution;
        float fz = normZ * meshResolution;
        int x0 = Mathf.Clamp(Mathf.FloorToInt(fx), 0, meshResolution);
        int z0 = Mathf.Clamp(Mathf.FloorToInt(fz), 0, meshResolution);
        int x1 = WrapOrClampX(x0 + 1);
        int z1 = WrapOrClampY(z0 + 1);
        float tx = fx - Mathf.Floor(fx);
        float tz = fz - Mathf.Floor(fz);
        float a = Mathf.Lerp(map[x0, z0], map[x1, z0], tx);
        float b = Mathf.Lerp(map[x0, z1], map[x1, z1], tx);
        return Mathf.Lerp(a, b, tz);
    }

    private float NormalizeWorldCoordinate(float value)
    {
        float normalized = value / Mathf.Max(1f, worldSize);
        return normalized - Mathf.Floor(normalized);
    }

    private int WrapOrClampX(int index)
    {
        return wrapEastWest ? WrapIndex(index) : Mathf.Clamp(index, 0, meshResolution);
    }

    private int WrapOrClampY(int index)
    {
        return wrapNorthSouth ? WrapIndex(index) : Mathf.Clamp(index, 0, meshResolution);
    }

    private int WrapIndex(int index)
    {
        int resolution = meshResolution + 1;
        index %= resolution;
        if (index < 0)
        {
            index += resolution;
        }

        return index;
    }

    private void CopyWrappedBorders()
    {
        int last = meshResolution;
        if (wrapEastWest)
        {
            for (int y = 0; y <= last; y++)
            {
                _heightMap[last, y] = _heightMap[0, y];
                _riverMap[last, y] = _riverMap[0, y];
                _temperatureMap[last, y] = _temperatureMap[0, y];
                _moistureMap[last, y] = _moistureMap[0, y];
            }
        }

        if (wrapNorthSouth)
        {
            for (int x = 0; x <= last; x++)
            {
                _heightMap[x, last] = _heightMap[x, 0];
                _riverMap[x, last] = _riverMap[x, 0];
                _temperatureMap[x, last] = _temperatureMap[x, 0];
                _moistureMap[x, last] = _moistureMap[x, 0];
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if ((!showBiomeDebug && !showRiverDebug) || _biomeMap == null)
        {
            return;
        }

        int step = Mathf.Max(1, meshResolution / 64);
        float cellSize = (float)worldSize / meshResolution;

        for (int y = 0; y < meshResolution; y += step)
        {
            for (int x = 0; x < meshResolution; x += step)
            {
                if (showRiverDebug && _riverMap[x, y] > 0.05f)
                {
                    Gizmos.color = Color.blue;
                }
                else if (showBiomeDebug)
                {
                    Gizmos.color = DefaultBiomeColor(_biomeMap[x, y]);
                }
                else
                {
                    continue;
                }

                Vector3 pos = transform.position + new Vector3(x * cellSize, _heightMap[x, y] * heightScale + 5f, y * cellSize);
                Gizmos.DrawSphere(pos, 2f);
            }
        }
    }
#endif
}
