using UnityEngine;

public enum BiomeType
{
    Plains = 0,
    Desert = 1,
    Tundra = 2,
    Ocean = 3,
    Beach = 4,
    Forest = 5,
    Mountains = 6,
    Snow = 7
}

[CreateAssetMenu(fileName = "BiomeData", menuName = "World/Biome Data")]
public class BiomeData : ScriptableObject
{
    [Header("Identity")]
    public BiomeType biomeType = BiomeType.Plains;
    public string biomeName = "Plains";

    [Header("Visuals")]
    public Color groundColor = new Color(0.25f, 0.48f, 0.20f);
    public Color groundColorVariation = new Color(0.42f, 0.62f, 0.28f);

    [Header("Terrain Modifiers")]
    [Range(0.4f, 1.8f)] public float heightMultiplier = 1f;
    [Range(0f, 1f)] public float roughness = 0.5f;

    [Header("Object Spawning Defaults")]
    public bool canSpawnTrees = true;
    public bool canSpawnRocks = true;
    [Range(0f, 1f)] public float vegetationDensity = 0.35f;
    [Range(0f, 1f)] public float rockDensity = 0.12f;

    [Header("Climate Ranges")]
    public Vector2 temperatureRange = new Vector2(0.25f, 0.75f);
    public Vector2 moistureRange = new Vector2(0.25f, 0.75f);
}
