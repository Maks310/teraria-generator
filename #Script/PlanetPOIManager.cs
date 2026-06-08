using System.Collections.Generic;
using UnityEngine;

public enum PlanetPOIType
{
    AncientRuins,
    MeteorZone,
    Geyser,
    TrapField,
    RareLoot,
    FloatingIslandAnchor,
    VolcanicVent
}

[System.Serializable]
public class PlanetPOIDefinition
{
    public PlanetPOIType type = PlanetPOIType.AncientRuins;
    public GameObject prefab;
    [Range(0f, 1f)] public float threshold = 0.9f;
    [Range(0f, 1f)] public float minHeight = 0.46f;
    [Range(0f, 1f)] public float maxHeight = 1f;
    public int maxCount = 12;
}

[System.Serializable]
public class PlanetPOIRecord
{
    public PlanetPOIType type;
    public PlanetSurfaceCoordinate coordinate;
    public PlanetBiomeId biomeId;
    public string persistentId;
}

public class PlanetPOIManager : MonoBehaviour
{
    public PlanetPOIDefinition[] poiDefinitions;
    [Range(4, 96)] public int candidateSamplesPerFace = 28;
    public bool instantiatePOIPrefabs = true;

    private readonly List<PlanetPOIRecord> _records = new List<PlanetPOIRecord>();
    public IList<PlanetPOIRecord> Records { get { return _records; } }

    public void GeneratePOIs(PlanetGenerator generator)
    {
        ClearPOIs();
        if (generator == null)
        {
            return;
        }

        PlanetPOIDefinition[] definitions = poiDefinitions != null && poiDefinitions.Length > 0 ? poiDefinitions : DefaultDefinitions();
        int samples = Mathf.Max(4, candidateSamplesPerFace);
        for (int d = 0; d < definitions.Length; d++)
        {
            int placed = 0;
            PlanetPOIDefinition definition = definitions[d];
            for (int face = 0; face < CubeSphereMeshBuilder.FaceCount && placed < definition.maxCount; face++)
            {
                for (int y = 0; y < samples && placed < definition.maxCount; y++)
                {
                    for (int x = 0; x < samples && placed < definition.maxCount; x++)
                    {
                        float u = (x + 0.5f) / samples;
                        float v = (y + 0.5f) / samples;
                        Vector3 direction = CubeSphereMeshBuilder.FaceUvToDirection(face, u, v);
                        PlanetSurfaceSample sample = generator.SampleSurface(direction);
                        if (sample.underwater || sample.normalizedHeight < definition.minHeight || sample.normalizedHeight > definition.maxHeight)
                        {
                            continue;
                        }

                        float mask = MaskFor(definition.type, sample.noise);
                        float jitter = NoiseGenerator.Hash01(face * 1000 + x, y, generator.seed + (int)definition.type * 97);
                        if (mask * 0.85f + jitter * 0.15f < definition.threshold)
                        {
                            continue;
                        }

                        PlacePOI(generator, definition, direction, sample, face, u, v);
                        placed++;
                    }
                }
            }
        }
    }

    [ContextMenu("Clear Planet POIs")]
    public void ClearPOIs()
    {
        _records.Clear();
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("POI_"))
            {
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }
    }

    private void PlacePOI(PlanetGenerator generator, PlanetPOIDefinition definition, Vector3 direction, PlanetSurfaceSample sample, int face, float u, float v)
    {
        PlanetPOIRecord record = new PlanetPOIRecord();
        record.type = definition.type;
        record.coordinate = new PlanetSurfaceCoordinate(direction, sample.altitude, face, new Vector2(u, v));
        record.biomeId = sample.biome != null ? sample.biome.biomeId : PlanetBiomeId.AncientRuins;
        record.persistentId = definition.type + "_" + face + "_" + Mathf.RoundToInt(u * 10000f) + "_" + Mathf.RoundToInt(v * 10000f);
        _records.Add(record);

        if (instantiatePOIPrefabs && definition.prefab != null)
        {
            Vector3 position = generator.transform.position + direction * (generator.planetRadius + sample.altitude);
            Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction);
            GameObject instance = Instantiate(definition.prefab, position, rotation, transform);
            instance.name = "POI_" + record.persistentId;
            PlanetPlacedObject placedObject = instance.GetComponent<PlanetPlacedObject>();
            if (placedObject == null) placedObject = instance.AddComponent<PlanetPlacedObject>();
            placedObject.Initialize(record.coordinate, record.biomeId);
        }
    }

    private float MaskFor(PlanetPOIType type, PlanetNoiseSample noise)
    {
        switch (type)
        {
            case PlanetPOIType.AncientRuins: return noise.ruinMask;
            case PlanetPOIType.MeteorZone: return noise.meteorMask;
            case PlanetPOIType.Geyser: return Mathf.Max(noise.volcanicMask, noise.caveMask);
            case PlanetPOIType.TrapField: return Mathf.Max(noise.ruinMask, noise.magneticMask);
            case PlanetPOIType.RareLoot: return noise.anomalyMask;
            case PlanetPOIType.FloatingIslandAnchor: return noise.floatingIslandMask;
            case PlanetPOIType.VolcanicVent: return noise.volcanicMask;
            default: return noise.anomalyMask;
        }
    }

    private PlanetPOIDefinition[] DefaultDefinitions()
    {
        return new PlanetPOIDefinition[]
        {
            new PlanetPOIDefinition { type = PlanetPOIType.AncientRuins, threshold = 0.91f, minHeight = 0.48f, maxHeight = 0.82f, maxCount = 8 },
            new PlanetPOIDefinition { type = PlanetPOIType.MeteorZone, threshold = 0.93f, minHeight = 0.47f, maxHeight = 0.86f, maxCount = 6 },
            new PlanetPOIDefinition { type = PlanetPOIType.Geyser, threshold = 0.9f, minHeight = 0.44f, maxHeight = 0.72f, maxCount = 14 },
            new PlanetPOIDefinition { type = PlanetPOIType.TrapField, threshold = 0.94f, minHeight = 0.48f, maxHeight = 0.84f, maxCount = 5 },
            new PlanetPOIDefinition { type = PlanetPOIType.RareLoot, threshold = 0.96f, minHeight = 0.5f, maxHeight = 0.9f, maxCount = 10 }
        };
    }
}
