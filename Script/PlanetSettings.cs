using UnityEngine;

namespace TerariaGenerator.Planets
{
    public enum PlanetPreset
    {
        Balanced,
        OceanWorld,
        SuperContinent,
        Archipelago,
        MountainWorld,
        PlainsWorld
    }

    [CreateAssetMenu(menuName = "Teraria/Planet Settings", fileName = "PlanetSettings")]
    public sealed class PlanetSettings : ScriptableObject
    {
        [Header("Core")]
        public int seed = 12345;
        [Min(1f)] public float radiusPlanet = 25f;
        [Tooltip("Height, relative to radius, used by both oceans and rivers.")]
        public float oceanLevel = 0f;

        [Header("Continents")]
        [Min(0.001f)] public float continentScale = 0.75f;
        [Min(0f)] public float continentStrength = 4.5f;
        [Range(1, 12)] public int continentCount = 4;

        [Header("Mountains")]
        [Min(0.001f)] public float mountainScale = 4f;
        [Min(0f)] public float mountainHeight = 3f;
        [Range(0.5f, 8f)] public float mountainSharpness = 2.2f;

        [Header("Hills And Details")]
        [Min(0.001f)] public float hillScale = 10f;
        [Min(0f)] public float hillHeight = 0.8f;
        [Min(0f)] public float detailScale = 36f;
        [Min(0f)] public float detailHeight = 0.18f;

        [Header("Shaping")]
        [Range(0.01f, 1f)] public float terrainSmoothness = 0.42f;
        [Range(0.01f, 1f)] public float coastSmoothness = 0.18f;

        [Header("Mesh")]
        [Range(1, 16)] public int chunksPerFace = 4;
        [Range(2, 128)] public int chunkResolution = 24;
        public bool generateCollider = true;
        public bool generateOnStart = true;

        [Header("Climate Maps")]
        [Min(0.001f)] public float temperatureNoiseScale = 2.4f;
        [Range(0f, 1f)] public float temperatureNoiseStrength = 0.16f;
        [Range(0.1f, 4f)] public float latitudeTemperatureFalloff = 1.35f;
        [Min(0f)] public float temperatureHeightFalloff = 0.08f;
        [Range(-1f, 1f)] public float temperatureOffset = 0f;
        [Min(0.001f)] public float moistureNoiseScale = 3.2f;
        [Range(0f, 1f)] public float moistureNoiseStrength = 0.22f;
        [Range(0.01f, 1f)] public float oceanMoistureDistance = 0.28f;
        [Range(0.01f, 1f)] public float riverMoistureDistance = 0.16f;
        [Range(0f, 1f)] public float oceanMoistureStrength = 0.72f;
        [Range(0f, 1f)] public float riverMoistureStrength = 0.38f;
        [Range(-1f, 1f)] public float moistureOffset = 0.08f;

        [Header("Biomes")]
        [Tooltip("Blend width used when a climate value approaches a biome boundary.")]
        [Range(0.001f, 0.5f)] public float biomeBlendDistance = 0.08f;
        [Tooltip("First four biomes are passed to the default terrain shader as texture layers.")]
        public BiomeDefinition[] biomes;

        [Header("Rivers")]
        [Range(0, 128)] public int riverCount = 18;
        [Range(0.55f, 1f)] public float riverSourceMinHeight01 = 0.72f;
        [Min(0.001f)] public float riverWidth = 0.035f;
        [Min(0.001f)] public float riverDepth = 0.18f;
        [Range(16, 4096)] public int maxRiverSteps = 768;

        [Header("Materials")]
        public Material terrainMaterial;
        public Material waterMaterial;

        public int BiomeCountForShader => Mathf.Clamp(biomes != null && biomes.Length > 0 ? biomes.Length : 4, 1, 4);

        public BiomeDefinition GetBiome(int index)
        {
            if (biomes != null && index >= 0 && index < biomes.Length && biomes[index] != null)
            {
                return biomes[index];
            }

            return RuntimeBiomeLibrary.GetDefaultBiome(index);
        }

        public static PlanetSettings CreateRuntimeDefault()
        {
            PlanetSettings settings = CreateInstance<PlanetSettings>();
            settings.name = "Runtime Planet Settings";
            return settings;
        }

        public void ApplyPreset(PlanetPreset preset)
        {
            switch (preset)
            {
                case PlanetPreset.OceanWorld:
                    oceanLevel = 0.8f; continentScale = 0.58f; continentStrength = 3.1f; continentCount = 3; mountainHeight = 1.5f; hillHeight = 0.45f; riverCount = 10; break;
                case PlanetPreset.SuperContinent:
                    oceanLevel = -0.15f; continentScale = 0.34f; continentStrength = 5.2f; continentCount = 1; mountainHeight = 2.4f; hillHeight = 0.75f; riverCount = 28; break;
                case PlanetPreset.Archipelago:
                    oceanLevel = 0.45f; continentScale = 1.55f; continentStrength = 3.6f; continentCount = 9; mountainHeight = 1.2f; hillHeight = 0.55f; riverCount = 12; break;
                case PlanetPreset.MountainWorld:
                    oceanLevel = -0.05f; continentScale = 0.9f; continentStrength = 4.8f; continentCount = 5; mountainScale = 7.5f; mountainHeight = 5.5f; mountainSharpness = 3.8f; hillHeight = 1.2f; riverCount = 30; break;
                case PlanetPreset.PlainsWorld:
                    oceanLevel = 0.05f; continentScale = 0.82f; continentStrength = 2.5f; continentCount = 4; mountainHeight = 0.35f; hillHeight = 0.25f; detailHeight = 0.05f; riverCount = 22; break;
                default:
                    oceanLevel = 0f; continentScale = 0.75f; continentStrength = 4.5f; continentCount = 4; mountainHeight = 3f; hillHeight = 0.8f; riverCount = 18; break;
            }
        }
        private static class RuntimeBiomeLibrary
        {
            private static readonly BiomeDefinition[] Defaults =
            {
                Create("Ocean Shore", 0f, 1f, 0.45f, 1f, 0f, 0.28f, new Color(0.78f, 0.67f, 0.42f, 1f), 0.45f, 0.05f, 0.1f, 6f, 18f),
                Create("Temperate Grassland", 0.35f, 0.82f, 0.28f, 0.78f, 0.2f, 0.72f, new Color(0.28f, 0.48f, 0.18f, 1f), 0.38f, 0.08f, 0.12f, 4f, 16f),
                Create("Cold Tundra", 0f, 0.42f, 0.12f, 0.82f, 0.18f, 0.86f, new Color(0.58f, 0.62f, 0.54f, 1f), 0.22f, 0.04f, 0.18f, 7f, -4f),
                Create("Arid Desert", 0.58f, 1f, 0f, 0.34f, 0.18f, 0.72f, new Color(0.77f, 0.61f, 0.31f, 1f), 0.04f, 0.01f, 0.02f, 5f, 28f)
            };

            public static BiomeDefinition GetDefaultBiome(int index)
            {
                return Defaults[Mathf.Clamp(index, 0, Defaults.Length - 1)];
            }

            private static BiomeDefinition Create(string name, float minT, float maxT, float minM, float maxM, float minH, float maxH, Color tint, float rain, float storm, float fog, float wind, float averageTemperature)
            {
                BiomeDefinition biome = ScriptableObject.CreateInstance<BiomeDefinition>();
                biome.name = name;
                biome.biomeName = name;
                biome.minimumTemperature = minT;
                biome.maximumTemperature = maxT;
                biome.minimumMoisture = minM;
                biome.maximumMoisture = maxM;
                biome.minimumHeight = minH;
                biome.maximumHeight = maxH;
                biome.tint = tint;
                biome.rainChance = rain;
                biome.stormChance = storm;
                biome.fogChance = fog;
                biome.windStrength = wind;
                biome.averageTemperature = averageTemperature;
                return biome;
            }
        }
    }
}
