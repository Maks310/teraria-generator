using System;
using UnityEngine;

namespace TerariaGenerator.Planets
{
    public enum PlanetObjectSpawnCategory
    {
        Flora,
        Geology,
        Crystal,
        Hazard,
        Structure
    }

    public enum PlanetStructureCategory
    {
        AncientRuins,
        SpecialLocations,
        FutureDungeons
    }

    [Serializable]
    public sealed class PlanetObjectSpawnDefinition
    {
        [Header("Identity")]
        public string objectName = "Tree";
        public PlanetObjectSpawnCategory category = PlanetObjectSpawnCategory.Flora;
        public GameObject prefab;

        [Header("Placement Rules")]
        [Tooltip("Candidate spawn chance per sampled terrain cell.")]
        [Range(0f, 1f)] public float spawnDensity = 0.08f;
        [Range(0f, 1f)] public float minHeight = 0.18f;
        [Range(0f, 1f)] public float maxHeight = 0.9f;
        [Range(0f, 1f)] public float minSlope = 0f;
        [Range(0f, 1f)] public float maxSlope = 0.45f;
        [Range(0f, 1f)] public float minTemperature = 0f;
        [Range(0f, 1f)] public float maxTemperature = 1f;
        [Min(0f)] public float minimumDistanceBetweenObjects = 1.5f;

        [Header("Instance Transform")]
        [Min(0.05f)] public float minScale = 0.85f;
        [Min(0.05f)] public float maxScale = 1.25f;
        [Tooltip("Extra offset along the resolved planet-surface up direction.")]
        public float surfaceOffset = 0f;
        public bool alignToSurfaceNormal = true;

        public bool CanSpawn(float height, float slope, float temperature)
        {
            return height >= minHeight && height <= maxHeight
                && slope >= minSlope && slope <= maxSlope
                && temperature >= minTemperature && temperature <= maxTemperature;
        }
    }

    [Serializable]
    public sealed class PlanetStructureSpawnDefinition
    {
        [Header("Identity")]
        public string structureName = "Ancient Ruins";
        public PlanetStructureCategory category = PlanetStructureCategory.AncientRuins;
        public GameObject prefab;

        [Header("Biome Rules")]
        [Tooltip("If empty, every non-water biome can host this structure.")]
        public BiomeDefinition[] allowedBiomes;
        [Range(0f, 1f)] public float spawnChance = 0.08f;

        [Header("Distance Rules")]
        [Min(0f)] public float minimumDistanceBetweenStructures = 80f;
        [Min(0f)] public float minimumDistanceFromPlayerSpawn = 45f;
        [Min(0f)] public float footprintRadius = 8f;

        [Header("Terrain Rules")]
        [Range(0f, 1f)] public float minHeight = 0.2f;
        [Range(0f, 1f)] public float maxHeight = 0.88f;
        [Range(0f, 1f)] public float maxSlope = 0.28f;
        [Range(0f, 1f)] public float minTemperature = 0f;
        [Range(0f, 1f)] public float maxTemperature = 1f;
        public bool alignToSurfaceNormal = true;

        public bool AllowsBiome(BiomeDefinition biome)
        {
            if (allowedBiomes == null || allowedBiomes.Length == 0)
            {
                return true;
            }

            for (int i = 0; i < allowedBiomes.Length; i++)
            {
                if (allowedBiomes[i] == biome)
                {
                    return true;
                }
            }

            return false;
        }

        public bool CanSpawn(BiomeDefinition biome, float height, float slope, float temperature)
        {
            return AllowsBiome(biome)
                && height >= minHeight && height <= maxHeight
                && slope <= maxSlope
                && temperature >= minTemperature && temperature <= maxTemperature;
        }
    }
}
