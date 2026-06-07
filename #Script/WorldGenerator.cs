using UnityEngine;

// Ïðîöåäóðíèé ãåíåðàòîð ñâ³òó.
// Â³äïîâ³äàº çà:
// - ñòâîðåííÿ êàðòè âèñîò;
// - ãåíåðàö³þ êîíòèíåíò³â, ã³ð, ð³÷îê ³ âîäè;
// - âèçíà÷åííÿ á³îì³â;
// - ïîáóäîâó mesh äëÿ çåìë³.
[ExecuteInEditMode]
public class WorldGenerator : MonoBehaviour
{
    [Header("World Settings")]
    // Çàãàëüíèé ðîçì³ð ñâ³òó â îäèíèöÿõ Unity.
    // Çá³ëüøåííÿ çíà÷åííÿ ðîáèòü êàðòó á³ëüøîþ, àëå ìîæå ï³äâèùèòè íàâàíòàæåííÿ.
    [Range(32, 8192)] public int worldSize = 2048;

    // Ðîçä³ëüíà çäàòí³ñòü ñ³òêè ðåëüºôó.
    // ×èì á³ëüøå çíà÷åííÿ, òèì á³ëüøå âåðøèí ³ òèì ïëàâí³øèé ðåëüºô.
    [Range(32, 1024)] public int meshResolution = 512;

    // Ìíîæíèê âèñîòè ëàíäøàôòó.
    // Âïëèâàº íà òå, íàñê³ëüêè âèñîêèìè áóäóòü ãîðè òà ïàãîðáè.
    [Range(1f, 1000f)] public float heightScale = 100f;

    // Seed çàäàº ïîâòîðþâàí³ñòü ãåíåðàö³¿.
    // Ïðè îäíàêîâîìó seed êàðòà áóäå ñòâîðþâàòèñÿ îäíàêîâî.
    public int seed = 42;

    // ßêùî true, êðà¿ êàðòè ïî îñ³ X çøèâàþòüñÿ ì³æ ñîáîþ.
    // Êîðèñíî äëÿ öèêë³÷íèõ àáî ïëàíåòàðíèõ ñâ³ò³â.
    public bool wrapEastWest = true;

    // ßêùî true, êðà¿ êàðòè ïî îñ³ Y çøèâàþòüñÿ ì³æ ñîáîþ.
    // Äîçâîëÿº óíèêàòè âèäèìèõ øâ³â íà ìåæàõ êàðòè.
    public bool wrapNorthSouth = true;

    [Header("Continent Shape")]
    // ×àñòêà ñóø³ â óñüîìó ñâ³ò³.
    // Ìåíøå çíà÷åííÿ = á³ëüøå îêåàíó, á³ëüøå çíà÷åííÿ = á³ëüøå ñóø³.
    [Range(0.05f, 1f)] public float landMass = 0.48f;

    // Øèðèíà ïåðåõîäó ì³æ ñóøåþ òà îêåàíîì.
    // ×èì âèùå çíà÷åííÿ, òèì ì'ÿêø³ áåðåãè.
    [Range(0.01f, 0.5f)] public float coastBlend = 0.12f;

    // Ìàñøòàá îñíîâíî¿ ôîðìè ìàòåðèê³â.
    // Âïëèâàº íà ðîçì³ð ³ õàðàêòåð âåëèêèõ ìàñèâ³â ñóø³.
    public float continentScale = 1.65f;

    // Ìàñøòàá âèêðèâëåííÿ øóìó.
    // Âèêîðèñòîâóºòüñÿ äëÿ òîãî, ùîá êîíòèíåíòè âèãëÿäàëè ïðèðîäí³øå.
    public float domainWarpScale = 1.25f;

    // Ñèëà âèêðèâëåííÿ øóìó.
    // ×èì á³ëüøå çíà÷åííÿ, òèì ñèëüí³øå äåôîðìóþòüñÿ êîíòóðè ìàòåðèê³â.
    [Range(0f, 0.35f)] public float domainWarpStrength = 0.08f;

    // Äîäàòêîâèé çñóâ îêåàíó á³ëÿ ïîëþñ³â.
    // Äîïîìàãàº çðîáèòè ïîëÿðí³ îáëàñò³ õîëîäí³øèìè òà âîäÿíèñò³øèìè.
    [Range(0f, 1f)] public float polarOceanBias = 0.18f;

    [Header("Height Noise")]
    // Áàçîâèé ìàñøòàá øóìó ðåëüºôó.
    // Ìåíøå çíà÷åííÿ = á³ëüø³, ïëàâí³ø³ ôîðìè; á³ëüøå = äð³áí³øà äåòàë³çàö³ÿ.
    public float noiseScale = 4f;

    // Ê³ëüê³ñòü îêòàâ øóìó.
    // Á³ëüøå îêòàâ = á³ëüøå äð³áíèõ äåòàëåé ó ðåëüºô³.
    [Range(1, 10)] public int octaves = 5;

    // Ïàðàìåòð çàòóõàííÿ àìïë³òóäè ì³æ îêòàâàìè.
    // Âèçíà÷àº, íàñê³ëüêè ñèëüíî äð³áí³ äåòàë³ âïëèâàþòü íà ï³äñóìêîâèé øóì.
    [Range(0f, 1f)] public float persistence = 0.48f;

    // Ïàðàìåòð çá³ëüøåííÿ ÷àñòîòè ì³æ îêòàâàìè.
    // Âèùèé ïîêàçíèê îçíà÷àº øâèäø³ çì³íè øóìó.
    public float lacunarity = 2f;

    // Ñèëà çãëàäæóâàííÿ ðåëüºôó.
    // ×èì á³ëüøå çíà÷åííÿ, òèì ìåíøå ð³çêèõ ïåðåõîä³â.
    [Range(0f, 1f)] public float terrainSmoothness = 0.35f;

    // Ê³ëüê³ñòü ïðîõîä³â çãëàäæóâàííÿ.
    // Á³ëüøå ïðîõîä³â = ì'ÿêøèé ³ ïðèðîäí³øèé ëàíäøàôò.
    [Range(0, 6)] public int smoothingPasses = 2;

    [Header("Mountains")]
    // Ìàñøòàá øóìó äëÿ ã³ð.
    // Âïëèâàº íà ôîðìó òà ðîçïîä³ë ã³ðñüêèõ õðåáò³â.
    public float mountainScale = 5.5f;

    // Íàñè÷åí³ñòü àáî ê³ëüê³ñòü ã³ðñüêèõ çîí.
    // Ìåíøå çíà÷åííÿ ìîæå çðîáèòè ãîðè á³ëüø ð³äê³ñíèìè.
    [Range(0f, 1f)] public float mountainAmount = 0.52f;

    // Âèñîòà ã³ð íàä áàçîâèì ð³âíåì ðåëüºôó.
    // Çá³ëüøåííÿ çíà÷åííÿ ðîáèòü ãîðè âèùèìè.
    [Range(0f, 2f)] public float mountainHeight = 0.55f;

    // Ð³çê³ñòü ã³ðñüêèõ âåðøèí.
    // Á³ëüøå çíà÷åííÿ = á³ëüø ãîñòð³, “çóá÷àñò³” ï³êè.
    [Range(0.5f, 4f)] public float mountainSharpness = 1.8f;

    [Header("Water")]
    // Ð³âåíü âîäè.
    // Âñå, ùî íèæ÷å öüîãî çíà÷åííÿ, áóäå ï³ä âîäîþ.
    [Range(0f, 1f)] public float waterLevel = 0.34f;

    // Ãëèáèíà îêåàíó.
    // Âïëèâàº íà òå, ÿê øâèäêî òåìí³øàº âîäà âãëèá ñóø³.
    [Range(0f, 1f)] public float oceanDepth = 0.42f;

    // Øèðèíà ì³ëêîâîäíîãî øåëüôó á³ëÿ áåðåã³â.
    // Á³ëüøå çíà÷åííÿ = ïëàâí³øèé ïåðåõ³ä â³ä ñóø³ äî ãëèáèíè.
    [Range(0f, 1f)] public float shelfBlend = 0.18f;

    // Ìàòåð³àë äëÿ âîäè.
    public Material waterMaterial;

    // Ðîçä³ëüíà çäàòí³ñòü âîäíî¿ ñ³òêè.
    // ×èì á³ëüøå çíà÷åííÿ, òèì ð³âí³øîþ ³ äåòàëüí³øîþ áóäå ïîâåðõíÿ âîäè.
    [Range(8, 256)] public int waterResolution = 96;

    [Header("Rivers")]
    // Óâ³ìêíåííÿ ãåíåðàö³¿ ð³÷îê.
    public bool generateRivers = true;

    // Ìàêñèìàëüíà ê³ëüê³ñòü âèòîê³â ð³÷îê.
    // Á³ëüøå çíà÷åííÿ = á³ëüøå ð³÷îê íà êàðò³.
    [Range(0, 256)] public int maxRiverSources = 72;

    // Ì³í³ìàëüíà âèñîòà äëÿ ïîÿâè âèòîêó ð³÷êè.
    // Ïîòð³áíî, ùîá ð³÷êè ïî÷èíàëèñÿ íå íàäòî íèçüêî.
    [Range(0f, 1f)] public float riverSourceMinHeight = 0.62f;

    // Øèðèíà ðóñëà ð³÷êè.
    // Âïëèâàº íà òå, íàñê³ëüêè øèðîêèìè áóäóòü ð³÷êè.
    [Range(0.001f, 0.08f)] public float riverWidth = 0.012f;

    // Ãëèáèíà âð³çàííÿ ð³÷êè â ðåëüºô.
    // Çá³ëüøåííÿ çíà÷åííÿ ðîáèòü ðóñëà ïîì³òí³øèìè.
    [Range(0f, 0.2f)] public float riverCarveDepth = 0.055f;

    // Ìàêñèìàëüíà äîâæèíà ð³÷êè â êðîêàõ.
    // Âèùèé ë³ì³ò äîçâîëÿº ð³÷êàì òÿãíóòèñÿ äàë³.
    [Range(64, 8192)] public int maxRiverLength = 1800;

    [Header("Climate & Biomes")]
    // Äàí³ äëÿ á³îìó îêåàíó.
    public BiomeData oceanBiome;

    // Äàí³ äëÿ á³îìó ïëÿæó.
    public BiomeData beachBiome;

    // Äàí³ äëÿ á³îìó ð³âíèí.
    public BiomeData plainsbiome;

    // Äàí³ äëÿ á³îìó ë³ñó.
    public BiomeData forestBiome;

    // Width of the soft color transition between neighboring climate biomes.
    // Larger values remove square biome borders, smaller values keep sharper borders.
    [Range(0.005f, 0.2f)] public float biomeBlendWidth = 0.08f;

    // Äàí³ äëÿ á³îìó ïóñòåë³.
    public BiomeData desertBiome;

    // Äàí³ äëÿ á³îìó òóíäðè.
    public BiomeData tundraBiome;

    // Äàí³ äëÿ á³îìó ã³ð.
    public BiomeData mountainsBiome;

    // Äàí³ äëÿ á³îìó ñí³ãó.
    public BiomeData snowBiome;

    // Ìàñøòàá øóìó êë³ìàòó.
    // Âïëèâàº íà ðîçïîä³ë òåìïåðàòóðè òà âîëîãîñò³.
    public float biomeScale = 2f;

    // Íàñê³ëüêè ñèëüíî òåìïåðàòóðà çàëåæèòü â³ä øèðîòè.
    // Á³ëüøå çíà÷åííÿ = ñèëüí³øèé ïåðåïàä ì³æ åêâàòîðîì ³ ïîëþñàìè.
    [Range(0f, 1f)] public float latitudeTemperatureInfluence = 0.65f;

    // Âèñîòà, ï³ñëÿ ÿêî¿ õîëîäí³ ä³ëÿíêè ìîæóòü ñòàòè ñí³ãîâèìè.
    [Range(0f, 1f)] public float snowHeight = 0.76f;

    [Header("Materials")]
    // Ìàòåð³àë äëÿ ðåëüºôó.
    public Material terrainMaterial;

    [Header("Debug")]
    // ßêùî true, ãåíåðàö³ÿ àâòîìàòè÷íî îíîâëþºòüñÿ â ðåäàêòîð³.
    public bool autoUpdate = true;

    // Ïîêàçóº â³äëàäêó á³îì³â ó Scene View.
    public bool showBiomeDebug = false;

    // Ïîêàçóº â³äëàäêó ð³÷îê ó Scene View.
    public bool showRiverDebug = false;

    // Ïîñèëàííÿ íà êîìïîíåíòè ìåøà ðåëüºôó.
    private MeshFilter _terrainMeshFilter;
    private MeshRenderer _terrainMeshRenderer;
    private MeshCollider _terrainCollider;

    // Ñòâîðåíèé îá'ºêò âîäè.
    private GameObject _waterObject;

    // Îñíîâí³ êàðòè äàíèõ, íà ÿêèõ áóäóºòüñÿ ñâ³ò.
    private float[,] _heightMap;
    private float[,] _riverMap;
    private float[,] _oceanDepthMap;
    private BiomeType[,] _biomeMap;
    private float[,] _temperatureMap;
    private float[,] _moistureMap;

        biomeBlendWidth = Mathf.Clamp(biomeBlendWidth, 0.005f, 0.2f);
    // Çîâí³øí³é äîñòóï äî êàðò äëÿ ³íøèõ ñêðèïò³â.
    public float[,] HeightMap { get { return _heightMap; } }
    public float[,] RiverMap { get { return _riverMap; } }
    public float[,] OceanDepthMap { get { return _oceanDepthMap; } }
    public BiomeType[,] BiomeMap { get { return _biomeMap; } }
    public int WorldSize { get { return worldSize; } }
    public float HeightScale { get { return heightScale; } }
    public float WaterLevel { get { return waterLevel; } }

    // Âèêëèêàºòüñÿ â ðåäàêòîð³, êîëè çì³íþþòüñÿ çíà÷åííÿ â Inspector.
    // Òóò ìè îáìåæóºìî ïàðàìåòðè â áåçïå÷íèõ ìåæàõ ³, çà ïîòðåáè, çàïóñêàºìî àâòîîíîâëåííÿ.
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
        // Â ðåäàêòîð³ çàïóñêàºìî ïåðåáóäîâó ñâ³òó ³ç çàòðèìêîþ,
        // ùîá íå âèêëèêàòè ãåíåðàö³þ íàäòî ÷àñòî ï³ä ÷àñ ðåäàãóâàííÿ ïîë³â.
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

    // Êîíòåêñòíà êîìàíäà ç Inspector.
    // Äîçâîëÿº âðó÷íó âèêëèêàòè ãåíåðàö³þ ñâ³òó áåç çàïóñêó ãðè.
    [ContextMenu("Generate World")]
    public void GenerateWorld()
    {
        // Ñïî÷àòêó ïðèáèðàºìî ñòàð³ ðåçóëüòàòè ïîïåðåäíüî¿ ãåíåðàö³¿.
        CleanupPreviousGeneration();

        // Ïåðåêîíóºìîñÿ, ùî íà îá'ºêò³ º ïîòð³áí³ êîìïîíåíòè.
        SetupComponents();

        // Ôîðìóºìî êàðòè âèñîò, òåìïåðàòóðè, âîëîãîñò³ òà á³îì³â.
        GenerateMaps();

        // Ñòâîðþºìî mesh ëàíäøàôòó íà îñíîâ³ êàðòè âèñîò.
        GenerateTerrainMesh();

        // Áóäóºìî ïîâåðõíþ âîäè.
        GenerateWater();

        Debug.Log($"World generated: {worldSize}x{worldSize}, mesh resolution: {meshResolution}x{meshResolution}, seed: {seed}");
    }

    // Âèäàëÿº ïîïåðåäíüî çãåíåðîâàíó âîäó, ùîá íå íàêîïè÷óâàëèñÿ äóáë³êàòè.
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

            UpdateTerrainMaterialParameters();
        }
    }

    private void UpdateTerrainMaterialParameters()
    {
        if (terrainMaterial == null)
        {
            return;

        terrainMaterial.SetFloat("_WaterLevel", waterLevel);
        terrainMaterial.SetFloat("_SnowHeight", snowHeight);
        terrainMaterial.SetFloat("_BiomeBlendWidth", biomeBlendWidth);
    // Ñòâîðþº àáî çíàõîäèòü êîìïîíåíòè, ÿê³ ïîòð³áí³ äëÿ ïîáóäîâè terrain mesh.
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

        // ßêùî çàäàíî ìàòåð³àë ðåëüºôó, çàñòîñîâóºìî éîãî äî renderer.
        if (terrainMaterial != null)
        {
            _terrainMeshRenderer.sharedMaterial = terrainMaterial;
        }
    }

    // Ãåíåðóº îñíîâí³ êàðòè äëÿ ñâ³òó:
    // - âèñîòà;
    // - ð³÷êè;
    // - ãëèáèíà îêåàíó;
    // - á³îìè;
    // - òåìïåðàòóðà;
    // - âîëîã³ñòü.
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
                // Íîðìàë³çîâàí³ êîîðäèíàòè òî÷êè íà êàðò³.
                float normX = (float)x / meshResolution;
                float normY = (float)y / meshResolution;

                // Âèêðèâëåííÿ øóìó, ùîá ôîðìè áóëè ìåíø “êâàäðàòí³” ³ á³ëüø ïðèðîäí³.
                Vector2 warped = NoiseGenerator.SeamlessDomainWarp(normX, normY, domainWarpScale, domainWarpStrength, seed + 41);

                // Øèðîòà: 0 á³ëÿ åêâàòîðà, 1 á³ëÿ ïîëþñ³â.
                float latitude = Mathf.Abs(normY - 0.5f) * 2f;
                float polarMask = Mathf.Pow(latitude, 2.2f);
                float equatorWarmth = 1f - polarMask;

                // Ôîðìóâàííÿ âåëèêî¿ êîíòèíåíòàëüíî¿ ìàñè.
                float continentNoise = NoiseGenerator.SeamlessOctavePerlin(warped.x, warped.y, 4, 0.55f, 2f, continentScale, seed + 100);
                continentNoise -= polarMask * polarOceanBias;
                float landMask = NoiseGenerator.SmoothStep(landMass - coastBlend, landMass + coastBlend, continentNoise);

                // Îñíîâíèé øóì ðåëüºôó.
                float rollingTerrain = NoiseGenerator.SeamlessOctavePerlin(warped.x, warped.y, octaves, persistence, lacunarity, noiseScale, seed + 200);

                // Õðåáòè äëÿ ã³ðñüêèõ çîí.
                float ridges = NoiseGenerator.RidgedOctavePerlin(warped.x, warped.y, 5, 0.55f, 2.05f, mountainScale, seed + 300);
                float mountainMask = NoiseGenerator.SmoothStep(mountainAmount, 1f, ridges);
                float mountains = Mathf.Pow(mountainMask, mountainSharpness) * mountainHeight;

                // Áàçîâà âèñîòà ñóø³.
                float landHeight = Mathf.Lerp(0.38f, 0.68f, rollingTerrain) + mountains;

                // Äíî îêåàíó.
                float seaFloorNoise = NoiseGenerator.SeamlessOctavePerlin(warped.x, warped.y, 4, 0.5f, 2f, noiseScale * 0.75f, seed + 400);
                float seaFloor = waterLevel - oceanDepth * Mathf.Lerp(0.35f, 1f, seaFloorNoise);

                // Ïåðåõ³ä ì³æ ñóøåþ òà îêåàíîì.
                float shelf = NoiseGenerator.SmoothStep(0f, Mathf.Max(0.0001f, shelfBlend), landMask);
                float height = Mathf.Lerp(seaFloor, landHeight, shelf);
                height = Mathf.Lerp(height, waterLevel + (height - waterLevel) * (1f - terrainSmoothness * 0.45f), 1f - landMask);

                // Çáåð³ãàºìî çíà÷åííÿ ó êàðòè.
                _heightMap[x, y] = Mathf.Clamp01(height);
                _temperatureMap[x, y] = Mathf.Clamp01(Mathf.Lerp(NoiseGenerator.SeamlessPerlin(warped.x, warped.y, biomeScale, seed + 1000), equatorWarmth, latitudeTemperatureInfluence));
                _moistureMap[x, y] = Mathf.Clamp01(NoiseGenerator.SeamlessOctavePerlin(warped.x, warped.y, 4, 0.55f, 2f, biomeScale * 1.4f, seed + 2000));
            }
        }

        // Çãëàäæóºìî êàðòó âèñîò, ùîá çìåíøèòè ð³çê³ ïåðåïàäè.
        SmoothHeightMap(smoothingPasses);

        // ßêùî ð³÷êè óâ³ìêíåí³, áóäóºìî ¿õ ³ äîäàòêîâî êîðèãóºìî ðåëüºô.
        if (generateRivers)
        {
            GenerateRiverMap();
            CarveRivers();
            SmoothHeightMap(Mathf.Max(0, smoothingPasses / 2));
        }

        // ßêùî êàðòà çàöèêëåíà, êîï³þºìî êðàéí³ çíà÷åííÿ, ùîá íå áóëî øâà.
        CopyWrappedBorders();

        // Ñòâîðþºìî ïîõ³äí³ êàðòè á³îì³â ³ ãëèáèíè îêåàíó.
        BuildDerivedMaps();
    }

    // Çãëàäæóâàííÿ êàðòè âèñîò.
    // Ïîòð³áíå äëÿ òîãî, ùîá ðåëüºô íå âèãëÿäàâ íàäòî “øóìíèì”.
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

            // Ì³íÿºìî êàðòè ì³ñöÿìè, ùîá íàñòóïíèé ïðîõ³ä ïðàöþâàâ ç îíîâëåíèìè äàíèìè.
            float[,] swap = _heightMap;
            _heightMap = buffer;
            buffer = swap;
        }
    }

    // Ãåíåðóº êàðòó ð³÷îê:
    // øóêàº ïîòåíö³éí³ äæåðåëà ³ çàïóñêàº òðàñóâàííÿ ð³÷îê âíèç ïî ñõèëàõ.
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

    // Ïðîêëàäàº îäíó ð³÷êó â³ä ¿¿ ïî÷àòêîâî¿ òî÷êè.
    // Ð³÷êà ðóõàºòüñÿ â á³ê íèæ÷èõ ñóñ³äí³õ êë³òèíîê.
    private void TraceRiver(int startX, int startY, int riverIndex)
    {
        int x = startX;
        int y = startY;
        int riverRadius = Mathf.Max(1, Mathf.RoundToInt(riverWidth * meshResolution));
        float flow = 0.35f + NoiseGenerator.Hash01(startX, startY, seed + 6000) * 0.65f;

        for (int step = 0; step < maxRiverLength; step++)
        {
            // Äîäàºìî øòàìï ð³÷êè íà êàðòó.
            AddRiverStamp(x, y, riverRadius, flow);

            // ßêùî ð³÷êà ä³éøëà äî âîäè, çóïèíÿºìîñÿ.
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

            // ßêùî êðàùîãî íàïðÿìêó íåìàº, ð³÷êà çàê³í÷óºòüñÿ.
            if (nextX == x && nextY == y)
            {
                break;
            }

            flow = Mathf.Clamp01(flow + 0.004f);
            x = nextX;
            y = nextY;
        }
    }

    // Äîäàº êðóãëèé ñë³ä ð³÷êè â êàðòó ð³÷îê.
    // Âèêîðèñòîâóºòüñÿ äëÿ ôîðìóâàííÿ øèðèíè ðóñëà.
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

    // Âèð³çàº ð³÷êè â îñíîâí³é êàðò³ âèñîò,
    // ùîá ðóñëà áóëè íå ò³ëüêè â³çóàëüíèìè, à é âïëèâàëè íà ðåëüºô.
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

    // Áóäóº ïîõ³äí³ êàðòè:
    // - ãëèáèíà îêåàíó;
    // - òèï á³îìó äëÿ êîæíî¿ êë³òèíêè.
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

    // Âèçíà÷àº, ÿêèé á³îì ìàº áóòè â òî÷ö³.
    // Ð³øåííÿ áàçóºòüñÿ íà âèñîò³, òåìïåðàòóð³, âîëîãîñò³ òà ïðèñóòíîñò³ ð³÷êè.
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

    // Ïîâåðòàº íàëàøòóâàííÿ êîíêðåòíîãî á³îìó.
    // ßêùî ÷àñòèíà äàíèõ íå çàäàíà, áåðå çàïàñíèé âàð³àíò.
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

        Vector2[] climateData = new Vector2[resolution * resolution];
                // UV2: legacy biome id + normalized height. UV3: river strength + ocean depth.
                // UV4: continuous temperature + moisture, so the shader can blend biome palettes smoothly.
                climateData[index] = new Vector2(_temperatureMap[x, y], _moistureMap[x, y]);
        mesh.uv4 = climateData;
    // - âåðøèíè;
    // - UV;
    // - êîëüîðè;
    // - òðèêóòíèêè.
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

    // Âèçíà÷àº êîë³ð âåðøèíè íà îñíîâ³ á³îìó, ð³÷îê ³ ãëèáèíè âîäè.
    private Color GetVertexColor(int x, int y)
    {
        BiomeType biomeType = _biomeMap[x, y];
        BiomeData biome = GetBiomeData(biomeType);
        Color color;

        if (biome != null)
        {
            // Íåâåëèêèé øóì, ùîá êîë³ð á³îìó âèãëÿäàâ ìåíø îäíîòîííèì.
            float colorNoise = NoiseGenerator.SeamlessPerlin((float)x / meshResolution, (float)y / meshResolution, noiseScale * 8f, seed + 3000);
            color = Color.Lerp(biome.groundColor, biome.groundColorVariation, colorNoise);
        }
        else
        {
            color = DefaultBiomeColor(biomeType);
        }

        // ßêùî òî÷êà ï³ä âîäîþ, çì³øóºìî ¿¿ ç êîëüîðîì âîäè.
        if (_heightMap[x, y] < waterLevel)
        {
            Color shallow = new Color(0.08f, 0.38f, 0.55f);
            Color deep = new Color(0.01f, 0.04f, 0.14f);
            color = Color.Lerp(shallow, deep, _oceanDepthMap[x, y]);
        }
        // ßêùî òóò ð³÷êà, äîäàºìî ñèí³é â³äò³íîê.
        else if (_riverMap[x, y] > 0.05f)
        {
            color = Color.Lerp(color, new Color(0.05f, 0.36f, 0.72f), Mathf.Clamp01(_riverMap[x, y] * 1.4f));
        }

        return color;
    }

    // Áàçîâèé êîë³ð äëÿ êîæíîãî òèïó á³îìó,
    // ÿêùî îêðåì³ äàí³ á³îìó íå çàäàí³ â Inspector.
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

    // Ñòâîðþº âîäó ÿê îêðåìèé îá'ºêò ³ç âëàñíèì mesh.
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

    // Áóäóº mesh äëÿ ïîâåðõí³ âîäè.
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

    // Ïîâåðòàº âèñîòó â òî÷ö³ ñâ³òó â îäèíèöÿõ Unity.
    public float GetHeightAt(float worldX, float worldZ)
    {
        if (_heightMap == null)
        {
            return 0f;
        }

        return SampleMapBilinear(_heightMap, worldX, worldZ) * heightScale;
    }

    // Ïîâåðòàº çíà÷åííÿ êàðòè ð³÷îê ó êîíêðåòí³é òî÷ö³.
    public float GetRiverAt(float worldX, float worldZ)
    {
        if (_riverMap == null)
        {
            return 0f;
        }

        return SampleMapBilinear(_riverMap, worldX, worldZ);
    }

    // Ïîâåðòàº â³äíîñíó ãëèáèíó îêåàíó â òî÷ö³.
    public float GetOceanDepthAt(float worldX, float worldZ)
    {
        if (_oceanDepthMap == null)
        {
            return 0f;
        }

        return SampleMapBilinear(_oceanDepthMap, worldX, worldZ);
    }

    // Ïîâåðòàº òèï á³îìó â çàçíà÷åíèõ êîîðäèíàòàõ.
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

    // Ïåðåâ³ðÿº, ÷è òî÷êà çíàõîäèòüñÿ ï³ä âîäîþ.
    public bool IsUnderwater(float worldX, float worldZ)
    {
        return GetHeightAt(worldX, worldZ) < waterLevel * heightScale;
    }

    // Á³ë³í³éíà ³íòåðïîëÿö³ÿ äëÿ ç÷èòóâàííÿ çíà÷åííÿ êàðòè ì³æ êë³òèíêàìè.
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

    // Ïåðåòâîðþº ñâ³òîâó êîîðäèíàòó â çíà÷åííÿ 0..1.
    private float NormalizeWorldCoordinate(float value)
    {
        float normalized = value / Mathf.Max(1f, worldSize);
        return normalized - Mathf.Floor(normalized);
    }

    // Àáî çàöèêëþº ³íäåêñ ïî X, àáî îáìåæóº éîãî ìåæàìè êàðòè.
    private int WrapOrClampX(int index)
    {
        return wrapEastWest ? WrapIndex(index) : Mathf.Clamp(index, 0, meshResolution);
    }

    // Àáî çàöèêëþº ³íäåêñ ïî Y, àáî îáìåæóº éîãî ìåæàìè êàðòè.
    private int WrapOrClampY(int index)
    {
        return wrapNorthSouth ? WrapIndex(index) : Mathf.Clamp(index, 0, meshResolution);
    }

    // Çàöèêëåííÿ ³íäåêñó äëÿ áåçøîâíîãî ïåðåõîäó ïî êðàÿõ êàðòè.
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

    // Êîï³þº çíà÷åííÿ ç îäíîãî êðàþ êàðòè íà ³íøèé,
    // ùîá íå áóëî âèäèìîãî ðîçðèâó íà øâàõ.
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
    // Â³äëàäî÷í³ Gizmos ó Scene View.
    // Äàº çìîãó ïîáà÷èòè, äå ðîçòàøîâàí³ á³îìè òà ð³÷êè.
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
