using UnityEngine;

public enum PlanetLiquidType
{
    Ocean,
    RedWater,
    Acid,
    Lava,
    HotSpring
}

public class PlanetWaterSystem : MonoBehaviour
{
    [Header("Ocean Shell")]
    public Material waterMaterial;
    [Range(4, 96)] public int waterResolutionPerFace = 32;
    public float waterSurfaceOffset = 0.35f;
    public bool buildOceanShell = true;

    [Header("Liquid Colors")]
    public Color oceanColor = new Color(0.04f, 0.24f, 0.46f, 0.72f);
    public Color redWaterColor = new Color(0.58f, 0.03f, 0.02f, 0.78f);
    public Color acidColor = new Color(0.42f, 1f, 0.04f, 0.78f);
    public Color lavaColor = new Color(1f, 0.22f, 0.02f, 0.95f);

    private GameObject _waterRoot;
    private static readonly int ShallowWaterColorId = Shader.PropertyToID("_ShallowWaterColor");
    private static readonly int DeepWaterColorId = Shader.PropertyToID("_DeepWaterColor");
    private static readonly int WorldSizeId = Shader.PropertyToID("_WorldSize");

    public void BuildWaterShell(PlanetGenerator generator)
    {
        ClearWater();
        if (!buildOceanShell || generator == null)
        {
            return;
        }

        Material material = waterMaterial != null ? waterMaterial : generator.waterMaterial;
        _waterRoot = new GameObject("PlanetWater");
        _waterRoot.transform.SetParent(generator.transform, false);
        float radius = generator.planetRadius + waterSurfaceOffset;

        for (int face = 0; face < CubeSphereMeshBuilder.FaceCount; face++)
        {
            GameObject faceObject = new GameObject("WaterFace_" + face);
            faceObject.transform.SetParent(_waterRoot.transform, false);
            MeshFilter filter = faceObject.AddComponent<MeshFilter>();
            MeshRenderer rendererComponent = faceObject.AddComponent<MeshRenderer>();
            if (material != null)
            {
                rendererComponent.sharedMaterial = material;
            }
            filter.sharedMesh = BuildWaterFace(face, waterResolutionPerFace, radius);
        }

        ApplyMaterialSettings(material, generator.planetRadius * 2f);
        WaterController controller = _waterRoot.AddComponent<WaterController>();
        controller.Initialize(material, generator.planetRadius * 2f);
    }

    public PlanetLiquidType ResolveLiquid(PlanetSurfaceSample sample)
    {
        if (sample.biome == null)
        {
            return PlanetLiquidType.Ocean;
        }

        switch (sample.biome.biomeId)
        {
            case PlanetBiomeId.CrimsonSwamp: return PlanetLiquidType.RedWater;
            case PlanetBiomeId.AcidLakes: return PlanetLiquidType.Acid;
            case PlanetBiomeId.VolcanicFields: return PlanetLiquidType.Lava;
            case PlanetBiomeId.SteamValleys: return PlanetLiquidType.HotSpring;
            default: return PlanetLiquidType.Ocean;
        }
    }

    [ContextMenu("Clear Planet Water")]
    public void ClearWater()
    {
        Transform existing = transform.Find("PlanetWater");
        if (existing != null)
        {
            if (Application.isPlaying) Destroy(existing.gameObject);
            else DestroyImmediate(existing.gameObject);
        }
        _waterRoot = null;
    }

    private Mesh BuildWaterFace(int face, int resolution, float radius)
    {
        int vertsPerLine = resolution + 1;
        Vector3[] vertices = new Vector3[vertsPerLine * vertsPerLine];
        Vector3[] normals = new Vector3[vertices.Length];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[resolution * resolution * 6];
        int tri = 0;
        for (int y = 0; y <= resolution; y++)
        {
            for (int x = 0; x <= resolution; x++)
            {
                float u = x / (float)resolution;
                float v = y / (float)resolution;
                Vector3 direction = CubeSphereMeshBuilder.FaceUvToDirection(face, u, v);
                int index = y * vertsPerLine + x;
                vertices[index] = direction * radius;
                normals[index] = direction;
                uvs[index] = new Vector2(u, v);
                if (x < resolution && y < resolution)
                {
                    int a = index;
                    int b = index + vertsPerLine + 1;
                    int c = index + vertsPerLine;
                    int d = index + 1;
                    triangles[tri++] = a; triangles[tri++] = b; triangles[tri++] = c;
                    triangles[tri++] = a; triangles[tri++] = d; triangles[tri++] = b;
                }
            }
        }
        Mesh mesh = new Mesh();
        mesh.name = "PlanetWaterFace_" + face;
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    private void ApplyMaterialSettings(Material material, float worldSize)
    {
        if (material == null)
        {
            return;
        }
        if (material.HasProperty(ShallowWaterColorId)) material.SetColor(ShallowWaterColorId, oceanColor);
        if (material.HasProperty(DeepWaterColorId)) material.SetColor(DeepWaterColorId, Color.Lerp(oceanColor, Color.black, 0.65f));
        if (material.HasProperty(WorldSizeId)) material.SetFloat(WorldSizeId, worldSize);
    }
}
