using UnityEngine;

[System.Serializable]
public struct PlanetClimateSample
{
    public float latitude;
    public float temperature;
    public float moisture;
    public float humidity;
    public float oceanProximity;
    public float normalizedHeight;
    public PlanetClimateZone zone;
}

public enum PlanetClimateZone
{
    Polar,
    Cold,
    Temperate,
    Arid,
    Tropical,
    Volcanic,
    Anomaly
}

[System.Serializable]
public class PlanetClimateSettings
{
    [Range(0f, 1f)] public float latitudeTemperatureInfluence = 0.62f;
    [Range(0f, 1f)] public float heightTemperatureInfluence = 0.28f;
    [Range(0f, 1f)] public float oceanMoistureInfluence = 0.36f;
    [Range(0f, 1f)] public float volcanicHeatInfluence = 0.38f;
    public Vector3 northPole = Vector3.up;
}

public static class PlanetClimate
{
    public static PlanetClimateSample Evaluate(Vector3 direction, float normalizedHeight, PlanetNoiseSample noise, float waterLevel, PlanetClimateSettings settings, PlanetRiverSample? river = null)
    {
        if (settings == null)
        {
            settings = new PlanetClimateSettings();
        }

        direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.up;
        Vector3 pole = settings.northPole.sqrMagnitude > 0f ? settings.northPole.normalized : Vector3.up;
        float latitudeSigned = Vector3.Dot(direction, pole);
        float equatorWarmth = 1f - Mathf.Abs(latitudeSigned);
        float oceanProximity = Mathf.Clamp01(1f - Mathf.Abs(normalizedHeight - waterLevel) / 0.28f);

        float temperature = Mathf.Lerp(noise.climateNoise, equatorWarmth, settings.latitudeTemperatureInfluence);
        temperature -= Mathf.Max(0f, normalizedHeight - waterLevel) * settings.heightTemperatureInfluence;
        temperature += Mathf.Pow(noise.volcanicMask, 3f) * settings.volcanicHeatInfluence;
        temperature = Mathf.Clamp01(temperature);

        float moisture = Mathf.Clamp01(noise.climateNoise * 0.48f + (1f - noise.ridgeMask) * 0.22f + oceanProximity * settings.oceanMoistureInfluence);
        float humidity = Mathf.Clamp01((moisture + oceanProximity) * 0.5f - Mathf.Max(0f, normalizedHeight - waterLevel) * 0.12f);

        if (river.HasValue)
        {
            PlanetRiverSample riverSample = river.Value;
            float riverInfluence = Mathf.Clamp01(Mathf.Max(riverSample.riverMask, 1f - riverSample.riverDistance));
            float lakeInfluence = Mathf.Clamp01(riverSample.lakeMask);
            moisture = Mathf.Clamp01(moisture + riverInfluence * 0.24f + lakeInfluence * 0.32f);
            humidity = Mathf.Clamp01(humidity + riverInfluence * 0.28f + lakeInfluence * 0.38f);
        }

        PlanetClimateSample sample = new PlanetClimateSample();
        sample.latitude = latitudeSigned;
        sample.temperature = temperature;
        sample.moisture = moisture;
        sample.humidity = humidity;
        sample.oceanProximity = oceanProximity;
        sample.normalizedHeight = normalizedHeight;
        sample.zone = ResolveZone(temperature, moisture, normalizedHeight, waterLevel, noise);
        return sample;
    }

    private static PlanetClimateZone ResolveZone(float temperature, float moisture, float height, float waterLevel, PlanetNoiseSample noise)
    {
        if (noise.volcanicMask > 0.82f && temperature > 0.55f)
        {
            return PlanetClimateZone.Volcanic;
        }

        if (noise.anomalyMask > 0.88f)
        {
            return PlanetClimateZone.Anomaly;
        }

        if (temperature < 0.24f)
        {
            return PlanetClimateZone.Polar;
        }

        if (temperature < 0.42f)
        {
            return PlanetClimateZone.Cold;
        }

        if (temperature > 0.62f && moisture < 0.36f)
        {
            return PlanetClimateZone.Arid;
        }

        if (temperature > 0.58f && moisture > 0.55f)
        {
            return PlanetClimateZone.Tropical;
        }

        return PlanetClimateZone.Temperate;
    }
}
