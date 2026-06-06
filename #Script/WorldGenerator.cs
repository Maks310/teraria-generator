using UnityEngine;

// Процедурний генератор світу.
// Відповідає за:
// - створення карти висот;
// - генерацію континентів, гір, річок і води;
// - визначення біомів;
// - побудову mesh для землі.
[ExecuteInEditMode]
public class WorldGenerator : MonoBehaviour
{
    [Header("World Settings")]
    // Загальний розмір світу в одиницях Unity.
    // Збільшення значення робить карту більшою, але може підвищити навантаження.
    [Range(32, 8192)] public int worldSize = 2048;

    // Роздільна здатність сітки рельєфу.
    // Чим більше значення, тим більше вершин і тим плавніший рельєф.
    [Range(32, 1024)] public int meshResolution = 512;

    // Множник висоти ландшафту.
    // Впливає на те, наскільки високими будуть гори та пагорби.
    [Range(1f, 1000f)] public float heightScale = 100f;

    // Seed задає повторюваність генерації.
    // При однаковому seed карта буде створюватися однаково.
    public int seed = 42;

    // Якщо true, краї карти по осі X зшиваються між собою.
    // Корисно для циклічних або планетарних світів.
    public bool wrapEastWest = true;

    // Якщо true, краї карти по осі Y зшиваються між собою.
    // Дозволяє уникати видимих швів на межах карти.
    public bool wrapNorthSouth = true;

    [Header("Continent Shape")]
    // Частка суші в усьому світі.
    // Менше значення = більше океану, більше значення = більше суші.
    [Range(0.05f, 1f)] public float landMass = 0.48f;

    // Ширина переходу між сушею та океаном.
    // Чим вище значення, тим м'якші береги.
    [Range(0.01f, 0.5f)] public float coastBlend = 0.12f;

    // Масштаб основної форми материків.
    // Впливає на розмір і характер великих масивів суші.
    public float continentScale = 1.65f;

    // Масштаб викривлення шуму.
    // Використовується для того, щоб континенти виглядали природніше.
    public float domainWarpScale = 1.25f;

    // Сила викривлення шуму.
    // Чим більше значення, тим сильніше деформуються контури материків.
    [Range(0f, 0.35f)] public float domainWarpStrength = 0.08f;

    // Додатковий зсув океану біля полюсів.
    // Допомагає зробити полярні області холоднішими та водянистішими.
    [Range(0f, 1f)] public float polarOceanBias = 0.18f;

    [Header("Height Noise")]
    // Базовий масштаб шуму рельєфу.
    // Менше значення = більші, плавніші форми; більше = дрібніша деталізація.
    public float noiseScale = 4f;

    // Кількість октав шуму.
    // Більше октав = більше дрібних деталей у рельєфі.
    [Range(1, 10)] public int octaves = 5;

    // Параметр затухання амплітуди між октавами.
    // Визначає, наскільки сильно дрібні деталі впливають на підсумковий шум.
    [Range(0f, 1f)] public float persistence = 0.48f;

    // Параметр збільшення частоти між октавами.
    // Вищий показник означає швидші зміни шуму.
    public float lacunarity = 2f;

    // Сила згладжування рельєфу.
    // Чим більше значення, тим менше різких переходів.
    [Range(0f, 1f)] public float terrainSmoothness = 0.35f;

    // Кількість проходів згладжування.
    // Більше проходів = м'якший і природніший ландшафт.
    [Range(0, 6)] public int smoothingPasses = 2;

    [Header("Mountains")]
    // Масштаб шуму для гір.
    // Впливає на форму та розподіл гірських хребтів.
    public float mountainScale = 5.5f;

    // Насиченість або кількість гірських зон.
    // Менше значення може зробити гори більш рідкісними.
    [Range(0f, 1f)] public float mountainAmount = 0.52f;

    // Висота гір над базовим рівнем рельєфу.
    // Збільшення значення робить гори вищими.
    [Range(0f, 2f)] public float mountainHeight = 0.55f;

    // Різкість гірських вершин.
    // Більше значення = більш гострі, “зубчасті” піки.
    [Range(0.5f, 4f)] public float mountainSharpness = 1.8f;

    [Header("Water")]
    // Рівень води.
    // Все, що нижче цього значення, буде під водою.
    [Range(0f, 1f)] public float waterLevel = 0.34f;

    // Глибина океану.
    // Впливає на те, як швидко темнішає вода вглиб суші.
    [Range(0f, 1f)] public float oceanDepth = 0.42f;

    // Ширина мілководного шельфу біля берегів.
    // Більше значення = плавніший перехід від суші до глибини.
    [Range(0f, 1f)] public float shelfBlend = 0.18f;

    // Матеріал для води.
    public Material waterMaterial;

    // Роздільна здатність водної сітки.
    // Чим більше значення, тим рівнішою і детальнішою буде поверхня води.
    [Range(8, 256)] public int waterResolution = 96;

    [Header("Rivers")]
    // Увімкнення генерації річок.
    public bool generateRivers = true;

    // Максимальна кількість витоків річок.
    // Більше значення = більше річок на карті.
    [Range(0, 256)] public int maxRiverSources = 72;

    // Мінімальна висота для появи витоку річки.
    // Потрібно, щоб річки починалися не надто низько.
    [Range(0f, 1f)] public float riverSourceMinHeight = 0.62f;

    // Ширина русла річки.
    // Впливає на те, наскільки широкими будуть річки.
    [Range(0.001f, 0.08f)] public float riverWidth = 0.012f;

    // Глибина врізання річки в рельєф.
    // Збільшення значення робить русла помітнішими.
    [Range(0f, 0.2f)] public float riverCarveDepth = 0.055f;

    // Максимальна довжина річки в кроках.
    // Вищий ліміт дозволяє річкам тягнутися далі.
    [Range(64, 8192)] public int maxRiverLength = 1800;

    [Header("Climate & Biomes")]
    // Дані для біому океану.
    public BiomeData oceanBiome;

    // Дані для біому пляжу.
    public BiomeData beachBiome;

    // Дані для біому рівнин.
    public BiomeData plainsbiome;

    // Дані для біому лісу.
    public BiomeData forestBiome;

    // Дані для біому пустелі.
    public BiomeData desertBiome;

    // Дані для біому тундри.
    public BiomeData tundraBiome;

    // Дані для біому гір.
    public BiomeData mountainsBiome;

    // Дані для біому снігу.
    public BiomeData snowBiome;

    // Масштаб шуму клімату.
    // Впливає на розподіл температури та вологості.
    public float biomeScale = 2f;

    // Наскільки сильно температура залежить від широти.
    // Більше значення = сильніший перепад між екватором і полюсами.
    [Range(0f, 1f)] public float latitudeTemperatureInfluence = 0.65f;

    // Висота, після якої холодні ділянки можуть стати сніговими.
    [Range(0f, 1f)] public float snowHeight = 0.76f;

    [Header("Materials")]
    // Матеріал для рельєфу.
    public Material terrainMaterial;

    [Header("Debug")]
    // Якщо true, генерація автоматично оновлюється в редакторі.
    public bool autoUpdate = true;

    // Показує відладку біомів у Scene View.
    public bool showBiomeDebug = false;

    // Показує відладку річок у Scene View.
    public bool showRiverDebug = false;

    // Посилання на компоненти меша рельєфу.
    private MeshFilter _terrainMeshFilter;
    private MeshRenderer _terrainMeshRenderer;
    private MeshCollider _terrainCollider;

    // Створений об'єкт води.
    private GameObject _waterObject;

    // Основні карти даних, на яких будується світ.
    private float[,] _heightMap;
    private float[,] _riverMap;
    private float[,] _oceanDepthMap;
    private BiomeType[,] _biomeMap;
    private float[,] _temperatureMap;
    private float[,] _moistureMap;

    // Зовнішній доступ до карт для інших скриптів.
    public float[,] HeightMap { get { return _heightMap; } }
    public float[,] RiverMap { get { return _riverMap; } }
    public float[,] OceanDepthMap { get { return _oceanDepthMap; } }
    public BiomeType[,] BiomeMap { get { return _biomeMap; } }
    public int WorldSize { get { return worldSize; } }
    public float HeightScale { get { return heightScale; } }
    public float WaterLevel { get { return waterLevel; } }

    // Викликається в редакторі, коли змінюються значення в Inspector.
    // Тут ми обмежуємо параметри в безпечних межах і, за потреби, запускаємо автооновлення.
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
        // В редакторі запускаємо перебудову світу із затримкою,
        // щоб не викликати генерацію надто часто під час редагування полів.
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

    // Контекстна команда з Inspector.
    // Дозволяє вручну викликати генерацію світу без запуску гри.
    [ContextMenu("Generate World")]
    public void GenerateWorld()
    {
        // Спочатку прибираємо старі результати попередньої генерації.
        CleanupPreviousGeneration();

        // Переконуємося, що на об'єкті є потрібні компоненти.
        SetupComponents();

        // Формуємо карти висот, температури, вологості та біомів.
        GenerateMaps();

        // Створюємо mesh ландшафту на основі карти висот.
        GenerateTerrainMesh();

        // Будуємо поверхню води.
        GenerateWater();

        Debug.Log($"World generated: {worldSize}x{worldSize}, mesh resolution: {meshResolution}x{meshResolution}, seed: {seed}");
    }

    // Видаляє попередньо згенеровану воду, щоб не накопичувалися дублікати.
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

    // Створює або знаходить компоненти, які потрібні для побудови terrain mesh.
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

        // Якщо задано матеріал рельєфу, застосовуємо його до renderer.
        if (terrainMaterial != null)
        {
            _terrainMeshRenderer.sharedMaterial = terrainMaterial;
        }
    }

    // Генерує основні карти для світу:
    // - висота;
    // - річки;
    // - глибина океану;
    // - біоми;
    // - температура;
    // - вологість.
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
                // Нормалізовані координати точки на карті.
                float normX = (float)x / meshResolution;
                float normY = (float)y / meshResolution;

                // Викривлення шуму, щоб форми були менш “квадратні” і більш природні.
                Vector2 warped = NoiseGenerator.SeamlessDomainWarp(normX, normY, domainWarpScale, domainWarpStrength, seed + 41);

                // Широта: 0 біля екватора, 1 біля полюсів.
                float latitude = Mathf.Abs(normY - 0.5f) * 2f;
                float polarMask = Mathf.Pow(latitude, 2.2f);
                float equatorWarmth = 1f - polarMask;

                // Формування великої континентальної маси.
                float continentNoise = NoiseGenerator.SeamlessOctavePerlin(warped.x, warped.y, 4, 0.55f, 2f, continentScale, seed + 100);
                continentNoise -= polarMask * polarOceanBias;
                float landMask = NoiseGenerator.SmoothStep(landMass - coastBlend, landMass + coastBlend, continentNoise);

                // Основний шум рельєфу.
                float rollingTerrain = NoiseGenerator.SeamlessOctavePerlin(warped.x, warped.y, octaves, persistence, lacunarity, noiseScale, seed + 200);

                // Хребти для гірських зон.
                float ridges = NoiseGenerator.RidgedOctavePerlin(warped.x, warped.y, 5, 0.55f, 2.05f, mountainScale, seed + 300);
                float mountainMask = NoiseGenerator.SmoothStep(mountainAmount, 1f, ridges);
                float mountains = Mathf.Pow(mountainMask, mountainSharpness) * mountainHeight;

                // Базова висота суші.
                float landHeight = Mathf.Lerp(0.38f, 0.68f, rollingTerrain) + mountains;

                // Дно океану.
                float seaFloorNoise = NoiseGenerator.SeamlessOctavePerlin(warped.x, warped.y, 4, 0.5f, 2f, noiseScale * 0.75f, seed + 400);
                float seaFloor = waterLevel - oceanDepth * Mathf.Lerp(0.35f, 1f, seaFloorNoise);

                // Перехід між сушею та океаном.
                float shelf = NoiseGenerator.SmoothStep(0f, Mathf.Max(0.0001f, shelfBlend), landMask);
                float height = Mathf.Lerp(seaFloor, landHeight, shelf);
                height = Mathf.Lerp(height, waterLevel + (height - waterLevel) * (1f - terrainSmoothness * 0.45f), 1f - landMask);

                // Зберігаємо значення у карти.
                _heightMap[x, y] = Mathf.Clamp01(height);
                _temperatureMap[x, y] = Mathf.Clamp01(Mathf.Lerp(NoiseGenerator.SeamlessPerlin(warped.x, warped.y, biomeScale, seed + 1000), equatorWarmth, latitudeTemperatureInfluence));
                _moistureMap[x, y] = Mathf.Clamp01(NoiseGenerator.SeamlessOctavePerlin(warped.x, warped.y, 4, 0.55f, 2f, biomeScale * 1.4f, seed + 2000));
            }
        }

        // Згладжуємо карту висот, щоб зменшити різкі перепади.
        SmoothHeightMap(smoothingPasses);

        // Якщо річки увімкнені, будуємо їх і додатково коригуємо рельєф.
        if (generateRivers)
        {
            GenerateRiverMap();
            CarveRivers();
            SmoothHeightMap(Mathf.Max(0, smoothingPasses / 2));
        }

        // Якщо карта зациклена, копіюємо крайні значення, щоб не було шва.
        CopyWrappedBorders();

        // Створюємо похідні карти біомів і глибини океану.
        BuildDerivedMaps();
    }

    // Згладжування карти висот.
    // Потрібне для того, щоб рельєф не виглядав надто “шумним”.
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

            // Міняємо карти місцями, щоб наступний прохід працював з оновленими даними.
            float[,] swap = _heightMap;
            _heightMap = buffer;
            buffer = swap;
        }
    }

    // Генерує карту річок:
    // шукає потенційні джерела і запускає трасування річок вниз по схилах.
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

    // Прокладає одну річку від її початкової точки.
    // Річка рухається в бік нижчих сусідніх клітинок.
    private void TraceRiver(int startX, int startY, int riverIndex)
    {
        int x = startX;
        int y = startY;
        int riverRadius = Mathf.Max(1, Mathf.RoundToInt(riverWidth * meshResolution));
        float flow = 0.35f + NoiseGenerator.Hash01(startX, startY, seed + 6000) * 0.65f;

        for (int step = 0; step < maxRiverLength; step++)
        {
            // Додаємо штамп річки на карту.
            AddRiverStamp(x, y, riverRadius, flow);

            // Якщо річка дійшла до води, зупиняємося.
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

            // Якщо кращого напрямку немає, річка закінчується.
            if (nextX == x && nextY == y)
            {
                break;
            }

            flow = Mathf.Clamp01(flow + 0.004f);
            x = nextX;
            y = nextY;
        }
    }

    // Додає круглий слід річки в карту річок.
    // Використовується для формування ширини русла.
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

    // Вирізає річки в основній карті висот,
    // щоб русла були не тільки візуальними, а й впливали на рельєф.
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

    // Будує похідні карти:
    // - глибина океану;
    // - тип біому для кожної клітинки.
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

    // Визначає, який біом має бути в точці.
    // Рішення базується на висоті, температурі, вологості та присутності річки.
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

    // Повертає налаштування конкретного біому.
    // Якщо частина даних не задана, бере запасний варіант.
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

    // Створює mesh рельєфу:
    // - вершини;
    // - UV;
    // - кольори;
    // - трикутники.
    private void GenerateTerrainMesh()
    {
        int resolution = meshResolution + 1;
        Vector3[] vertices = new Vector3[resolution * resolution];
        Color[] colors = new Color[resolution * resolution];
        Vector2[] uvs = new Vector2[resolution * resolution];

        // Extra UV channels feed the terrain shader with non-visual biome metadata.
        Vector2[] biomeData = new Vector2[resolution * resolution];
        Vector2[] waterData = new Vector2[resolution * resolution];

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
                // UV2: biome id + normalized height. UV3: river strength + ocean depth.
                biomeData[index] = new Vector2((float)_biomeMap[x, y], _heightMap[x, y]);
                waterData[index] = new Vector2(_riverMap[x, y], _oceanDepthMap[x, y]);
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
        mesh.uv2 = biomeData;
        mesh.uv3 = waterData;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        _terrainMeshFilter.sharedMesh = mesh;
        _terrainCollider.sharedMesh = mesh;
    }

    // Визначає колір вершини на основі біому, річок і глибини води.
    private Color GetVertexColor(int x, int y)
    {
        BiomeType biomeType = _biomeMap[x, y];
        BiomeData biome = GetBiomeData(biomeType);
        Color color;

        if (biome != null)
        {
            // Невеликий шум, щоб колір біому виглядав менш однотонним.
            float colorNoise = NoiseGenerator.SeamlessPerlin((float)x / meshResolution, (float)y / meshResolution, noiseScale * 8f, seed + 3000);
            color = Color.Lerp(biome.groundColor, biome.groundColorVariation, colorNoise);
        }
        else
        {
            color = DefaultBiomeColor(biomeType);
        }

        // Якщо точка під водою, змішуємо її з кольором води.
        if (_heightMap[x, y] < waterLevel)
        {
            Color shallow = new Color(0.08f, 0.38f, 0.55f);
            Color deep = new Color(0.01f, 0.04f, 0.14f);
            color = Color.Lerp(shallow, deep, _oceanDepthMap[x, y]);
        }
        // Якщо тут річка, додаємо синій відтінок.
        else if (_riverMap[x, y] > 0.05f)
        {
            color = Color.Lerp(color, new Color(0.05f, 0.36f, 0.72f), Mathf.Clamp01(_riverMap[x, y] * 1.4f));
        }

        return color;
    }

    // Базовий колір для кожного типу біому,
    // якщо окремі дані біому не задані в Inspector.
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

    // Створює воду як окремий об'єкт із власним mesh.
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

    // Будує mesh для поверхні води.
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

    // Повертає висоту в точці світу в одиницях Unity.
    public float GetHeightAt(float worldX, float worldZ)
    {
        if (_heightMap == null)
        {
            return 0f;
        }

        return SampleMapBilinear(_heightMap, worldX, worldZ) * heightScale;
    }

    // Повертає значення карти річок у конкретній точці.
    public float GetRiverAt(float worldX, float worldZ)
    {
        if (_riverMap == null)
        {
            return 0f;
        }

        return SampleMapBilinear(_riverMap, worldX, worldZ);
    }

    // Повертає відносну глибину океану в точці.
    public float GetOceanDepthAt(float worldX, float worldZ)
    {
        if (_oceanDepthMap == null)
        {
            return 0f;
        }

        return SampleMapBilinear(_oceanDepthMap, worldX, worldZ);
    }

    // Повертає тип біому в зазначених координатах.
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

    // Перевіряє, чи точка знаходиться під водою.
    public bool IsUnderwater(float worldX, float worldZ)
    {
        return GetHeightAt(worldX, worldZ) < waterLevel * heightScale;
    }

    // Білінійна інтерполяція для зчитування значення карти між клітинками.
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

    // Перетворює світову координату в значення 0..1.
    private float NormalizeWorldCoordinate(float value)
    {
        float normalized = value / Mathf.Max(1f, worldSize);
        return normalized - Mathf.Floor(normalized);
    }

    // Або зациклює індекс по X, або обмежує його межами карти.
    private int WrapOrClampX(int index)
    {
        return wrapEastWest ? WrapIndex(index) : Mathf.Clamp(index, 0, meshResolution);
    }

    // Або зациклює індекс по Y, або обмежує його межами карти.
    private int WrapOrClampY(int index)
    {
        return wrapNorthSouth ? WrapIndex(index) : Mathf.Clamp(index, 0, meshResolution);
    }

    // Зациклення індексу для безшовного переходу по краях карти.
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

    // Копіює значення з одного краю карти на інший,
    // щоб не було видимого розриву на швах.
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
    // Відладочні Gizmos у Scene View.
    // Дає змогу побачити, де розташовані біоми та річки.
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
