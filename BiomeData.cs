using UnityEngine;

public enum BiomeType
{
    Plains,
    Desert,
    Tundra
}

[CreateAssetMenu(fileName = "BiomeData", menuName = "World/Biome Data")]
public class BiomeData : ScriptableObject
{
    public BiomeType biomeType;
    public string biomeName;
    public Color groundColor = Color.green;
    public Color groundColorVariation = Color.green;

    [Header("Terrain Modifiers")]
    [Range(0f, 1f)] public float heightMultiplier = 1f;
    [Range(0f, 1f)] public float roughness = 0.5f;

    [Header("Object Spawning")]
    public bool canSpawnTrees = true;
    public bool canSpawnRocks = true;
    [Range(0f, 1f)] public float vegetationDensity = 0.5f;

    [Header("Temperature & Moisture Ranges")]
    public Vector2 temperatureRange = new Vector2(0.3f, 0.7f);
    public Vector2 moistureRange = new Vector2(0.3f, 0.7f);
}
