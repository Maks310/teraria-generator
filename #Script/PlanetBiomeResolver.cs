using UnityEngine;

public class PlanetBiomeResolver : MonoBehaviour
{
    [Header("Biome Assets")]
    public BiomeDefinition[] biomeDefinitions;
    [Tooltip("Existing flat-world biome assets can be assigned here and reused for colors and default spawn settings.")]
    public BiomeData[] legacyBiomeData;

    [Header("Region Thresholds")]
    [Range(0f, 1f)] public float waterLevel = 0.46f;
    [Range(0f, 1f)] public float rareAnomalyThreshold = 0.86f;
    [Range(0f, 1f)] public float mountainHeight = 0.72f;

    private BiomeDefinition[] _runtimeDefaults;

    public BiomeDefinition ResolveBiome(Vector3 direction, float normalizedHeight, PlanetNoiseSample noise, PlanetClimateSample climate)
    {
        BiomeDefinition special = ResolveSpecialBiome(normalizedHeight, noise, climate);
        if (special != null)
        {
            return special;
        }

        BiomeDefinition[] source = GetDefinitions();
        BiomeDefinition best = null;
        float bestScore = -999f;
        for (int i = 0; i < source.Length; i++)
        {
            BiomeDefinition definition = source[i];
            if (definition == null || definition.poiDriven || definition.rareRegion)
            {
                continue;
            }

            float score = Score(definition, climate, normalizedHeight, noise.anomalyMask);
            if (score > bestScore)
            {
                best = definition;
                bestScore = score;
            }
        }

        return best != null ? best : GetDefinition(PlanetBiomeId.VioletForest);
    }

    public BiomeDefinition GetDefinition(PlanetBiomeId id)
    {
        BiomeDefinition[] source = GetDefinitions();
        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] != null && source[i].biomeId == id)
            {
                return source[i];
            }
        }
        return source.Length > 0 ? source[0] : null;
    }

    public BiomeDefinition[] GetDefinitions()
    {
        if (biomeDefinitions != null && biomeDefinitions.Length > 0)
        {
            return biomeDefinitions;
        }

        if (_runtimeDefaults == null || _runtimeDefaults.Length == 0)
        {
            _runtimeDefaults = CreateDefaultDefinitions();
        }
        return _runtimeDefaults;
    }

    private BiomeDefinition ResolveSpecialBiome(float height, PlanetNoiseSample noise, PlanetClimateSample climate)
    {
        if (noise.ruinMask > rareAnomalyThreshold + 0.04f && height > waterLevel + 0.02f)
        {
            return GetDefinition(PlanetBiomeId.AncientRuins);
        }

        if (noise.meteorMask > rareAnomalyThreshold + 0.03f && climate.temperature > 0.4f)
        {
            return GetDefinition(PlanetBiomeId.MeteorZone);
        }

        if (noise.floatingIslandMask > rareAnomalyThreshold + 0.02f && height > mountainHeight)
        {
            return GetDefinition(PlanetBiomeId.FloatingIslands);
        }

        if (noise.magneticMask > rareAnomalyThreshold && height > mountainHeight - 0.06f)
        {
            return GetDefinition(PlanetBiomeId.MagneticCliffs);
        }

        if (noise.crystalMask > rareAnomalyThreshold && climate.temperature > 0.45f && climate.moisture < 0.55f)
        {
            return GetDefinition(PlanetBiomeId.EnergyCrystalFields);
        }

        if (noise.volcanicMask > rareAnomalyThreshold && climate.temperature > 0.56f)
        {
            return GetDefinition(PlanetBiomeId.VolcanicFields);
        }

        if (noise.caveMask > 0.9f && height < waterLevel + 0.18f && height > waterLevel - 0.03f)
        {
            return GetDefinition(PlanetBiomeId.FireflyCaves);
        }

        return null;
    }

    private float Score(BiomeDefinition definition, PlanetClimateSample climate, float height, float anomaly)
    {
        float score = 0f;
        score += RangeScore(climate.temperature, definition.temperatureRange) * 2f;
        score += RangeScore(climate.moisture, definition.moistureRange) * 1.6f;
        score += RangeScore(climate.humidity, definition.humidityRange);
        score += RangeScore(height, definition.normalizedHeightRange) * 1.4f;
        score += anomaly >= definition.minimumAnomaly ? 0.2f : -2f;
        return score;
    }

    private float RangeScore(float value, Vector2 range)
    {
        if (value < range.x || value > range.y)
        {
            float distance = value < range.x ? range.x - value : value - range.y;
            return -distance * 4f;
        }

        float center = (range.x + range.y) * 0.5f;
        float halfWidth = Mathf.Max(0.0001f, (range.y - range.x) * 0.5f);
        return 1f - Mathf.Clamp01(Mathf.Abs(value - center) / halfWidth) * 0.35f;
    }

    private BiomeDefinition[] CreateDefaultDefinitions()
    {
        return new BiomeDefinition[]
        {
            Create(PlanetBiomeId.VioletForest, "Violet Forest", 0.35f, 0.72f, 0.48f, 1f, 0.47f, 0.78f, new Color(0.26f, 0.08f, 0.38f), new Color(0.54f, 0.18f, 0.76f), "Giant mushrooms and violet undergrowth glow softly at night."),
            Create(PlanetBiomeId.CrystalDesert, "Crystal Desert", 0.62f, 1f, 0f, 0.34f, 0.46f, 0.76f, new Color(0.78f, 0.58f, 0.32f), new Color(0.64f, 0.9f, 1f), "Hot dry sand broken by refractive crystal clusters."),
            Create(PlanetBiomeId.CrimsonSwamp, "Crimson Swamp", 0.5f, 0.86f, 0.68f, 1f, 0.38f, 0.56f, new Color(0.28f, 0.02f, 0.04f), new Color(0.78f, 0.05f, 0.03f), "Low wet basin with red water and poisonous plants."),
            Create(PlanetBiomeId.BioluminescentJungle, "Bioluminescent Jungle", 0.62f, 1f, 0.62f, 1f, 0.48f, 0.74f, new Color(0.02f, 0.34f, 0.16f), new Color(0.16f, 1f, 0.75f), "Dense glowing jungle for high humidity equatorial regions."),
            Create(PlanetBiomeId.GiantFlowerForest, "Forest of Giant Flowers", 0.42f, 0.76f, 0.48f, 0.86f, 0.46f, 0.72f, new Color(0.16f, 0.42f, 0.13f), new Color(1f, 0.46f, 0.78f), "Temperate fertile regions where flowers replace tree canopies."),
            Create(PlanetBiomeId.FrozenWasteland, "Frozen Wasteland", 0f, 0.28f, 0f, 0.72f, 0.42f, 1f, new Color(0.72f, 0.84f, 0.9f), Color.white, "Snow, ice, and sparse survival resources near poles and highlands."),
            Create(PlanetBiomeId.VolcanicFields, "Volcanic Fields", 0.56f, 1f, 0f, 0.58f, 0.48f, 0.9f, new Color(0.09f, 0.07f, 0.06f), new Color(1f, 0.22f, 0.02f), "Rare hot anomaly fields with lava glow and fire creatures."),
            Create(PlanetBiomeId.AshPlains, "Ash Plains", 0.42f, 0.8f, 0f, 0.46f, 0.46f, 0.72f, new Color(0.18f, 0.17f, 0.16f), new Color(0.42f, 0.39f, 0.36f), "Dry plains downwind of volcanic belts with ash fog."),
            Create(PlanetBiomeId.SteamValleys, "Steam Valleys", 0.48f, 0.86f, 0.5f, 1f, 0.4f, 0.68f, new Color(0.24f, 0.34f, 0.28f), new Color(0.78f, 0.86f, 0.78f), "Warm wet valleys with geysers and hot water."),
            Create(PlanetBiomeId.MagneticCliffs, "Magnetic Cliffs", 0.28f, 0.72f, 0f, 0.62f, 0.66f, 1f, new Color(0.12f, 0.13f, 0.16f), new Color(0.42f, 0.52f, 0.74f), "High rare regions with floating stones and magnetic effects."),
            Create(PlanetBiomeId.StoneTreeForest, "Stone Tree Forest", 0.32f, 0.72f, 0.28f, 0.72f, 0.52f, 0.84f, new Color(0.25f, 0.29f, 0.25f), new Color(0.55f, 0.62f, 0.57f), "Mineralized forests on rocky plateaus."),
            Create(PlanetBiomeId.AcidLakes, "Acid Lakes", 0.46f, 0.88f, 0.56f, 1f, 0.36f, 0.54f, new Color(0.06f, 0.22f, 0.06f), new Color(0.45f, 1f, 0.05f), "Wet lowlands with green liquid hazards."),
            Create(PlanetBiomeId.FireflyCaves, "Firefly Caves", 0.35f, 0.8f, 0.45f, 1f, 0.38f, 0.66f, new Color(0.04f, 0.05f, 0.08f), new Color(0.9f, 1f, 0.28f), "Cave-mouth and basin regions full of glowing insects."),
            Create(PlanetBiomeId.MeteorZone, "Meteor Zone", 0.36f, 0.86f, 0f, 0.62f, 0.44f, 0.82f, new Color(0.12f, 0.09f, 0.08f), new Color(0.96f, 0.38f, 0.12f), "Rare impact fields and falling meteor encounters."),
            Create(PlanetBiomeId.Glassland, "Glassland", 0.58f, 1f, 0f, 0.38f, 0.42f, 0.72f, new Color(0.72f, 0.66f, 0.5f), new Color(0.85f, 0.95f, 1f), "Desert edge where heat transformed sand into glass."),
            Create(PlanetBiomeId.EnergyCrystalFields, "Energy Crystal Fields", 0.42f, 0.86f, 0.18f, 0.72f, 0.48f, 0.88f, new Color(0.09f, 0.16f, 0.22f), new Color(0.2f, 0.75f, 1f), "Rare large crystal basins controlled by anomaly masks."),
            Create(PlanetBiomeId.BlackForest, "Black Forest", 0.24f, 0.58f, 0.45f, 1f, 0.42f, 0.78f, new Color(0.02f, 0.025f, 0.02f), new Color(0.09f, 0.1f, 0.08f), "Almost no light; dense hostile forest."),
            Create(PlanetBiomeId.MushroomOcean, "Mushroom Ocean", 0.44f, 0.78f, 0.72f, 1f, 0.34f, 0.54f, new Color(0.12f, 0.1f, 0.18f), new Color(0.72f, 0.26f, 0.92f), "Shallow humid coasts with mushroom canopies instead of trees."),
            Create(PlanetBiomeId.FloatingIslands, "Floating Islands", 0.36f, 0.82f, 0.28f, 0.82f, 0.72f, 1f, new Color(0.2f, 0.32f, 0.2f), new Color(0.62f, 0.82f, 1f), "Sky islands over high anomaly regions."),
            Create(PlanetBiomeId.AncientRuins, "Ancient Ruins", 0.24f, 0.86f, 0.18f, 0.86f, 0.42f, 0.84f, new Color(0.22f, 0.2f, 0.17f), new Color(0.1f, 0.65f, 1f), "POI-led ancient civilization zone with robots, traps, and rare loot.")
        };
    }

    private BiomeDefinition Create(PlanetBiomeId id, string name, float tempMin, float tempMax, float moistMin, float moistMax, float heightMin, float heightMax, Color primary, Color secondary, string notes)
    {
        BiomeDefinition definition = ScriptableObject.CreateInstance<BiomeDefinition>();
        definition.biomeId = id;
        definition.displayName = name;
        definition.temperatureRange = new Vector2(tempMin, tempMax);
        definition.moistureRange = new Vector2(moistMin, moistMax);
        definition.humidityRange = new Vector2(Mathf.Max(0f, moistMin - 0.2f), 1f);
        definition.normalizedHeightRange = new Vector2(heightMin, heightMax);
        definition.primaryColor = primary;
        definition.secondaryColor = secondary;
        definition.visualTint = Color.Lerp(primary, secondary, 0.35f);
        definition.designNotes = notes;
        definition.rareRegion = id == PlanetBiomeId.MeteorZone || id == PlanetBiomeId.MagneticCliffs || id == PlanetBiomeId.EnergyCrystalFields || id == PlanetBiomeId.FloatingIslands || id == PlanetBiomeId.VolcanicFields;
        definition.poiDriven = id == PlanetBiomeId.AncientRuins || id == PlanetBiomeId.MeteorZone;
        definition.minimumAnomaly = definition.rareRegion || definition.poiDriven ? rareAnomalyThreshold : 0f;
        definition.emissionStrength = id == PlanetBiomeId.VioletForest || id == PlanetBiomeId.BioluminescentJungle || id == PlanetBiomeId.EnergyCrystalFields || id == PlanetBiomeId.AncientRuins ? 1.4f : 0f;
        definition.fogDensity = id == PlanetBiomeId.AshPlains || id == PlanetBiomeId.BlackForest || id == PlanetBiomeId.CrimsonSwamp ? 0.16f : 0.04f;
        return definition;
    }
}
