using UnityEngine;

public class PlanetMapSystem : MonoBehaviour
{
    [Header("Map Data")]
    public int mapResolution = 256;
    public bool trackDiscoveredRegions = true;
    [Range(0f, 1f)] public float defaultUndiscoveredAlpha = 0.2f;

    private PlanetGenerator _planet;
    private bool[,] _discovered;

    public void Initialize(PlanetGenerator planet)
    {
        _planet = planet;
        mapResolution = Mathf.Max(16, mapResolution);
        _discovered = new bool[mapResolution, mapResolution];
    }

    public Color SampleMapColor(Vector3 direction)
    {
        if (_planet == null)
        {
            return Color.black;
        }

        PlanetSurfaceSample sample = _planet.SampleSurface(direction);
        Color color = sample.biome != null ? Color.Lerp(sample.biome.primaryColor, sample.biome.secondaryColor, sample.noise.heightNoise) : Color.magenta;
        if (sample.underwater)
        {
            color = Color.Lerp(color, new Color(0.02f, 0.18f, 0.36f), 0.7f);
        }
        return color;
    }

    public void MarkDiscovered(Vector3 worldPosition, float angularRadiusDegrees)
    {
        if (_planet == null || _discovered == null)
        {
            return;
        }

        Vector3 direction = (worldPosition - _planet.transform.position).normalized;
        Vector2 uv = DirectionToEquirectangular(direction);
        int radiusPixels = Mathf.CeilToInt(mapResolution * Mathf.Max(0.001f, angularRadiusDegrees) / 360f);
        int cx = Mathf.RoundToInt(uv.x * (mapResolution - 1));
        int cy = Mathf.RoundToInt(uv.y * (mapResolution - 1));
        for (int y = -radiusPixels; y <= radiusPixels; y++)
        {
            for (int x = -radiusPixels; x <= radiusPixels; x++)
            {
                int px = (cx + x + mapResolution) % mapResolution;
                int py = Mathf.Clamp(cy + y, 0, mapResolution - 1);
                _discovered[px, py] = true;
            }
        }
    }

    public Texture2D BuildPreviewTexture()
    {
        Texture2D texture = new Texture2D(mapResolution, mapResolution, TextureFormat.RGBA32, false);
        for (int y = 0; y < mapResolution; y++)
        {
            for (int x = 0; x < mapResolution; x++)
            {
                float u = x / (float)(mapResolution - 1);
                float v = y / (float)(mapResolution - 1);
                Vector3 direction = EquirectangularToDirection(new Vector2(u, v));
                Color color = SampleMapColor(direction);
                if (trackDiscoveredRegions && _discovered != null && !_discovered[x, y])
                {
                    color.a = defaultUndiscoveredAlpha;
                    color = Color.Lerp(Color.black, color, defaultUndiscoveredAlpha);
                }
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        return texture;
    }

    public static Vector2 DirectionToEquirectangular(Vector3 direction)
    {
        direction = direction.normalized;
        float longitude = Mathf.Atan2(direction.z, direction.x);
        float latitude = Mathf.Asin(Mathf.Clamp(direction.y, -1f, 1f));
        return new Vector2(longitude / (Mathf.PI * 2f) + 0.5f, latitude / Mathf.PI + 0.5f);
    }

    public static Vector3 EquirectangularToDirection(Vector2 uv)
    {
        float longitude = (uv.x - 0.5f) * Mathf.PI * 2f;
        float latitude = (uv.y - 0.5f) * Mathf.PI;
        float cosLat = Mathf.Cos(latitude);
        return new Vector3(Mathf.Cos(longitude) * cosLat, Mathf.Sin(latitude), Mathf.Sin(longitude) * cosLat).normalized;
    }
}
