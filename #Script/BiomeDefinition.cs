using UnityEngine;

public enum PlanetBiomeId
{
    VioletForest,
    CrystalDesert,
    CrimsonSwamp,
    BioluminescentJungle,
    GiantFlowerForest,
    FrozenWasteland,
    VolcanicFields,
    AshPlains,
    SteamValleys,
    MagneticCliffs,
    StoneTreeForest,
    AcidLakes,
    FireflyCaves,
    MeteorZone,
    Glassland,
    EnergyCrystalFields,
    BlackForest,
    MushroomOcean,
    FloatingIslands,
    AncientRuins
}

public enum PlanetPrefabCategory
{
    Grass,
    Tree,
    GiantMushroom,
    Crystal,
    Rock,
    AmbientParticle,
    Mob,
    Ruin,
    Trap,
    Loot,
    Decoration
}

[System.Serializable]
public class PlanetSpawnEntry
{
    public PlanetPrefabCategory category = PlanetPrefabCategory.Decoration;
    public GameObject prefab;
    [Range(0f, 1f)] public float probability = 0.05f;
    [Range(0f, 8f)] public float density = 1f;
    public Vector2 scaleRange = new Vector2(0.85f, 1.25f);
    public bool alignToSurface = true;
}

[CreateAssetMenu(fileName = "PlanetBiomeDefinition", menuName = "Planet/Biome Definition")]
public class BiomeDefinition : ScriptableObject
{
    [Header("Identity")]
    public PlanetBiomeId biomeId = PlanetBiomeId.VioletForest;
    public string displayName = "Violet Forest";
    [TextArea] public string designNotes;

    [Header("Climate And Height Rules")]
    public Vector2 temperatureRange = new Vector2(0f, 1f);
    public Vector2 moistureRange = new Vector2(0f, 1f);
    public Vector2 humidityRange = new Vector2(0f, 1f);
    public Vector2 normalizedHeightRange = new Vector2(0f, 1f);
    [Range(0f, 1f)] public float minimumAnomaly = 0f;
    public bool rareRegion;
    public bool poiDriven;

    [Header("Terrain Visuals")]
    public Material groundMaterial;
    public Color primaryColor = Color.green;
    public Color secondaryColor = Color.gray;
    public Color visualTint = Color.white;
    [Range(0f, 8f)] public float emissionStrength;
    [Range(0f, 1f)] public float fogDensity = 0.05f;
    public Color fogColor = Color.gray;
    [Range(0f, 1f)] public float rareEventChance = 0.01f;

    [Header("Spawning")]
    [Range(0f, 4f)] public float vegetationDensity = 1f;
    [Range(0f, 4f)] public float rockDensity = 1f;
    [Range(0f, 4f)] public float mobDensity = 0.3f;
    public PlanetSpawnEntry[] spawnEntries;

    [Header("Legacy Compatibility")]
    public BiomeData legacyBiomeData;

    public bool MatchesClimate(PlanetClimateSample climate, float normalizedHeight, float anomaly)
    {
        if (!InRange(climate.temperature, temperatureRange) || !InRange(climate.moisture, moistureRange))
        {
            return false;
        }

        return InRange(climate.humidity, humidityRange) && InRange(normalizedHeight, normalizedHeightRange) && anomaly >= minimumAnomaly;
    }

    public static bool InRange(float value, Vector2 range)
    {
        return value >= range.x && value <= range.y;
    }
}
