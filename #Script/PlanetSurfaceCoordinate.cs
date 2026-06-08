using UnityEngine;

[System.Serializable]
public struct PlanetSurfaceCoordinate
{
    public Vector3 direction;
    public float altitude;
    public int faceIndex;
    public Vector2 faceUv;

    public PlanetSurfaceCoordinate(Vector3 direction, float altitude, int faceIndex, Vector2 faceUv)
    {
        this.direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.up;
        this.altitude = altitude;
        this.faceIndex = faceIndex;
        this.faceUv = faceUv;
    }

    public Vector3 ToWorldPosition(Vector3 planetCenter, float planetRadius)
    {
        return planetCenter + direction * (planetRadius + altitude);
    }
}

public class PlanetPlacedObject : MonoBehaviour
{
    public PlanetSurfaceCoordinate surfaceCoordinate;
    public PlanetBiomeId biomeId;
    public string persistentId;

    public void Initialize(PlanetSurfaceCoordinate coordinate, PlanetBiomeId biome)
    {
        surfaceCoordinate = coordinate;
        biomeId = biome;
        persistentId = coordinate.faceIndex + ":" + Mathf.RoundToInt(coordinate.faceUv.x * 100000f) + ":" + Mathf.RoundToInt(coordinate.faceUv.y * 100000f);
    }
}
