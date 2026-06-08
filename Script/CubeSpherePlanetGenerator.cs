using System.Collections.Generic;
using UnityEngine;

namespace TerariaGenerator.Planets
{
    [ExecuteAlways]
    public sealed class CubeSpherePlanetGenerator : MonoBehaviour
    {
        [SerializeField] private PlanetSettings settings;
        [SerializeField] private PlanetPreset quickPreset = PlanetPreset.Balanced;
        [SerializeField] private bool applyPresetBeforeGenerate;

        private readonly List<GameObject> generatedObjects = new List<GameObject>();
        private float[,,] heights;
        private bool[,,] rivers;
        private ClimateMaps climateMaps;
        private int faceResolution;

        public PlanetSettings Settings
        {
            get => settings;
            set => settings = value;
        }

        public float[,,] HeightMap => climateMaps?.height;
        public float[,,] TemperatureMap => climateMaps?.temperature;
        public float[,,] MoistureMap => climateMaps?.moisture;

        private void Start()
        {
            if (settings != null && settings.generateOnStart)
            {
                GeneratePlanet();
            }
        }

        [ContextMenu("Generate Planet")]
        public void GeneratePlanet()
        {
            if (settings == null)
            {
                settings = PlanetSettings.CreateRuntimeDefault();
            }

            if (applyPresetBeforeGenerate)
            {
                settings.ApplyPreset(quickPreset);
            }

            ClearGeneratedObjects();
            faceResolution = settings.chunksPerFace * settings.chunkResolution + 1;
            heights = new float[6, faceResolution, faceResolution];
            BuildHeightMap();
            rivers = new RiverNetwork(settings, faceResolution, heights).Generate();
            climateMaps = ClimateMaps.Generate(settings, faceResolution, heights, rivers);

            for (int face = 0; face < 6; face++)
            for (int cx = 0; cx < settings.chunksPerFace; cx++)
            for (int cy = 0; cy < settings.chunksPerFace; cy++)
            {
                CreateChunk(face, cx, cy);
            }

            CreateWaterSphere();
        }

        [ContextMenu("Clear Planet")]
        public void ClearGeneratedObjects()
        {
            for (int i = generatedObjects.Count - 1; i >= 0; i--)
            {
                if (generatedObjects[i] == null) continue;
                if (Application.isPlaying)
                {
                    Destroy(generatedObjects[i]);
                }
                else
                {
                    DestroyImmediate(generatedObjects[i]);
                }
            }
            generatedObjects.Clear();

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child.name.StartsWith("Planet Chunk") || child.name == "Unified Water")
                {
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                }
            }
        }

        private void BuildHeightMap()
        {
            for (int face = 0; face < 6; face++)
            for (int x = 0; x < faceResolution; x++)
            for (int y = 0; y < faceResolution; y++)
            {
                float u = x / (faceResolution - 1f);
                float v = y / (faceResolution - 1f);
                Vector3 point = CubeSphereUtility.PointOnFace(face, u, v);
                heights[face, x, y] = PlanetNoise.EvaluateTerrain(point, settings);
            }
        }

        private void CreateChunk(int face, int chunkX, int chunkY)
        {
            int resolution = settings.chunkResolution;
            Vector3[] vertices = new Vector3[(resolution + 1) * (resolution + 1)];
            Vector3[] normals = new Vector3[vertices.Length];
            Vector2[] uvs = new Vector2[vertices.Length];
            List<Vector4> climateUvs = new List<Vector4>(vertices.Length);
            List<Vector4> biomeUvs = new List<Vector4>(vertices.Length);
            Color[] colors = new Color[vertices.Length];
            int[] triangles = new int[resolution * resolution * 6];

            for (int x = 0; x <= resolution; x++)
            for (int y = 0; y <= resolution; y++)
            {
                int globalX = chunkX * resolution + x;
                int globalY = chunkY * resolution + y;
                float u = globalX / (faceResolution - 1f);
                float v = globalY / (faceResolution - 1f);
                int index = x * (resolution + 1) + y;
                Vector3 normal = CubeSphereUtility.PointOnFace(face, u, v);
                float height = heights[face, globalX, globalY];
                vertices[index] = normal * (settings.radiusPlanet + height);
                normals[index] = normal;
                uvs[index] = new Vector2(u, v);
                colors[index] = EncodeVertexData(height, normal, face, globalX, globalY);
                climateUvs.Add(EncodeClimateData(face, globalX, globalY));
                biomeUvs.Add(climateMaps != null ? climateMaps.biomeWeights[face, globalX, globalY] : new Vector4(1f, 0f, 0f, 0f));
            }

            int triangleIndex = 0;
            for (int x = 0; x < resolution; x++)
            for (int y = 0; y < resolution; y++)
            {
                int a = x * (resolution + 1) + y;
                int b = (x + 1) * (resolution + 1) + y;
                int c = x * (resolution + 1) + y + 1;
                int d = (x + 1) * (resolution + 1) + y + 1;
                triangles[triangleIndex++] = a;
                triangles[triangleIndex++] = c;
                triangles[triangleIndex++] = b;
                triangles[triangleIndex++] = b;
                triangles[triangleIndex++] = c;
                triangles[triangleIndex++] = d;
            }

            Mesh mesh = new Mesh { name = $"Cube Sphere Face {face} Chunk {chunkX},{chunkY}" };
            mesh.indexFormat = vertices.Length > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.SetUVs(1, climateUvs);
            mesh.SetUVs(2, biomeUvs);
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            GameObject chunk = new GameObject($"Planet Chunk F{face} {chunkX},{chunkY}");
            chunk.transform.SetParent(transform, false);
            MeshFilter filter = chunk.AddComponent<MeshFilter>();
            MeshRenderer renderer = chunk.AddComponent<MeshRenderer>();
            filter.sharedMesh = mesh;
            renderer.sharedMaterial = ConfigureTerrainMaterial(settings.terrainMaterial != null ? settings.terrainMaterial : CreateRuntimeTerrainMaterial());
            if (settings.generateCollider)
            {
                chunk.AddComponent<MeshCollider>().sharedMesh = mesh;
            }
            generatedObjects.Add(chunk);
        }

        private Color EncodeVertexData(float height, Vector3 normal, int face, int x, int y)
        {
            float normalizedHeight = Mathf.InverseLerp(settings.oceanLevel - settings.continentStrength, settings.oceanLevel + settings.mountainHeight + settings.continentStrength, height);
            float slope = 1f - Mathf.Clamp01(Mathf.Abs(Vector3.Dot(normal, EstimateSmoothedNormal(face, x, y))));
            float coast = 1f - Mathf.Clamp01(Mathf.Abs(height - settings.oceanLevel) / Mathf.Max(0.001f, settings.coastSmoothness));
            float river = rivers != null && rivers[face, x, y] ? 1f : 0f;
            return new Color(normalizedHeight, slope, Mathf.Max(coast, river), river);
        }

        private Vector4 EncodeClimateData(int face, int x, int y)
        {
            if (climateMaps == null)
            {
                return Vector4.zero;
            }

            return new Vector4(
                climateMaps.temperature[face, x, y],
                climateMaps.moisture[face, x, y],
                climateMaps.height[face, x, y],
                climateMaps.primaryBiome[face, x, y] / 3f);
        }

        private Vector3 EstimateSmoothedNormal(int face, int x, int y)
        {
            int x0 = Mathf.Clamp(x - 1, 0, faceResolution - 1);
            int x1 = Mathf.Clamp(x + 1, 0, faceResolution - 1);
            int y0 = Mathf.Clamp(y - 1, 0, faceResolution - 1);
            int y1 = Mathf.Clamp(y + 1, 0, faceResolution - 1);
            Vector3 px0 = CubeSphereUtility.PointOnFace(face, x0 / (faceResolution - 1f), y / (faceResolution - 1f)) * (settings.radiusPlanet + heights[face, x0, y]);
            Vector3 px1 = CubeSphereUtility.PointOnFace(face, x1 / (faceResolution - 1f), y / (faceResolution - 1f)) * (settings.radiusPlanet + heights[face, x1, y]);
            Vector3 py0 = CubeSphereUtility.PointOnFace(face, x / (faceResolution - 1f), y0 / (faceResolution - 1f)) * (settings.radiusPlanet + heights[face, x, y0]);
            Vector3 py1 = CubeSphereUtility.PointOnFace(face, x / (faceResolution - 1f), y1 / (faceResolution - 1f)) * (settings.radiusPlanet + heights[face, x, y1]);
            return Vector3.Cross(py1 - py0, px1 - px0).normalized;
        }

        private void CreateWaterSphere()
        {
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            water.name = "Unified Water";
            water.transform.SetParent(transform, false);
            float diameter = (settings.radiusPlanet + settings.oceanLevel) * 2f;
            water.transform.localScale = Vector3.one * diameter;
            MeshRenderer renderer = water.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = settings.waterMaterial != null ? settings.waterMaterial : CreateRuntimeWaterMaterial();
            Collider collider = water.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying) Destroy(collider);
                else DestroyImmediate(collider);
            }
            generatedObjects.Add(water);
        }

        private Material ConfigureTerrainMaterial(Material material)
        {
            if (material == null)
            {
                return null;
            }

            for (int i = 0; i < 4; i++)
            {
                BiomeDefinition biome = settings.GetBiome(i);
                material.SetColor($"_BiomeTint{i}", biome.tint);
                material.SetFloat($"_BiomeSmoothness{i}", biome.smoothness);
                material.SetFloat($"_BiomeScale{i}", biome.textureScale);
                if (biome.albedoTexture != null)
                {
                    material.SetTexture($"_BiomeTex{i}", biome.albedoTexture);
                }
                if (biome.normalTexture != null)
                {
                    material.SetTexture($"_BiomeNormal{i}", biome.normalTexture);
                }
            }

            material.SetFloat("_OceanLevel01", Mathf.InverseLerp(settings.oceanLevel - settings.continentStrength, settings.oceanLevel + settings.mountainHeight + settings.continentStrength, settings.oceanLevel));
            material.SetFloat("_CoastBlend", settings.coastSmoothness);
            return material;
        }

        private static Material CreateRuntimeTerrainMaterial()
        {
            Shader shader = Shader.Find("Teraria/Biome Terrain");
            if (shader == null)
            {
                shader = Shader.Find("Teraria/Planet Terrain Debug");
            }
            return new Material(shader != null ? shader : Shader.Find("Standard"));
        }

        private static Material CreateRuntimeWaterMaterial()
        {
            Shader shader = Shader.Find("Teraria/Planet Water");
            Material material = new Material(shader != null ? shader : Shader.Find("Standard"));
            material.SetFloat("_Mode", 3f);
            material.SetColor("_Color", new Color(0.05f, 0.35f, 0.7f, 0.45f));
            return material;
        }
    }
}
