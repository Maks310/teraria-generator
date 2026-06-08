using UnityEngine;

namespace TerariaGenerator.Planets
{
    [CreateAssetMenu(menuName = "Teraria/Biome Definition", fileName = "BiomeDefinition")]
    public sealed class BiomeDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string biomeName = "Temperate Grassland";

        [Header("Climate Ranges")]
        [Range(0f, 1f)] public float minimumTemperature = 0.35f;
        [Range(0f, 1f)] public float maximumTemperature = 0.75f;
        [Range(0f, 1f)] public float minimumMoisture = 0.35f;
        [Range(0f, 1f)] public float maximumMoisture = 0.75f;
        [Range(0f, 1f)] public float minimumHeight = 0.2f;
        [Range(0f, 1f)] public float maximumHeight = 0.85f;

        [Header("Terrain Shading")]
        public Texture2D albedoTexture;
        public Texture2D normalTexture;
        public Color tint = new Color(0.32f, 0.48f, 0.22f, 1f);
        [Range(0f, 1f)] public float smoothness = 0.25f;
        [Range(0.1f, 64f)] public float textureScale = 12f;

        [Header("Future Weather")]
        [Range(0f, 1f)] public float rainChance = 0.35f;
        [Range(0f, 1f)] public float stormChance = 0.08f;
        [Range(0f, 1f)] public float fogChance = 0.12f;
        [Min(0f)] public float windStrength = 4f;
        public float averageTemperature = 16f;

        public bool Contains(float temperature, float moisture, float height)
        {
            return temperature >= minimumTemperature && temperature <= maximumTemperature
                && moisture >= minimumMoisture && moisture <= maximumMoisture
                && height >= minimumHeight && height <= maximumHeight;
        }

        public float EvaluateWeight(float temperature, float moisture, float height, float blendDistance)
        {
            float safeBlend = Mathf.Max(0.0001f, blendDistance);
            return RangeWeight(temperature, minimumTemperature, maximumTemperature, safeBlend)
                * RangeWeight(moisture, minimumMoisture, maximumMoisture, safeBlend)
                * RangeWeight(height, minimumHeight, maximumHeight, safeBlend);
        }

        private static float RangeWeight(float value, float minimum, float maximum, float blendDistance)
        {
            if (maximum < minimum)
            {
                float swap = minimum;
                minimum = maximum;
                maximum = swap;
            }

            float enter = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(minimum - blendDistance, minimum + blendDistance, value));
            float exit = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(maximum - blendDistance, maximum + blendDistance, value));
            return Mathf.Clamp01(enter * exit);
        }
    }
}
