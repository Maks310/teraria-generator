using UnityEngine;

public struct PlanetSurfaceSample
{
    public Vector3 direction;
    public float normalizedHeight;
    public float altitude;
    public bool underwater;
    public PlanetNoiseSample noise;
    public PlanetClimateSample climate;
    public PlanetRiverSample river;
    public BiomeDefinition biome;
}

public class PlanetGenerationData
{
    private readonly PlanetGenerator _generator;

    public PlanetGenerationData(PlanetGenerator generator)
    {
        _generator = generator;
    }

    public PlanetSurfaceSample Sample(Vector3 direction)
    {
        return _generator != null ? _generator.SampleSurface(direction) : new PlanetSurfaceSample { direction = direction.normalized };
    }
}
