using UnityEngine;

public class CubeSphereMeshBuilder : MonoBehaviour
{
    public const int FaceCount = 6;

    [System.Serializable]
    public struct PlanetChunkKey
    {
        public int face;
        public int x;
        public int y;

        public PlanetChunkKey(int face, int x, int y)
        {
            this.face = face;
            this.x = x;
            this.y = y;
        }

        public override string ToString()
        {
            return "F" + face + "_" + x + "_" + y;
        }
    }

    [System.Serializable]
    public class PlanetChunk
    {
        public PlanetChunkKey key;
        public GameObject gameObject;
        public Mesh mesh;
        public Bounds bounds;
    }

    public PlanetChunk BuildChunk(PlanetGenerator generator, PlanetChunkKey key, int chunkResolution, int chunksPerFace, Transform parent, Material terrainMaterial)
    {
        chunkResolution = Mathf.Max(2, chunkResolution);
        chunksPerFace = Mathf.Max(1, chunksPerFace);

        GameObject chunkObject = new GameObject("PlanetChunk_" + key);
        chunkObject.transform.SetParent(parent, false);
        MeshFilter filter = chunkObject.AddComponent<MeshFilter>();
        MeshRenderer rendererComponent = chunkObject.AddComponent<MeshRenderer>();
        MeshCollider colliderComponent = chunkObject.AddComponent<MeshCollider>();
        if (terrainMaterial != null)
        {
            rendererComponent.sharedMaterial = terrainMaterial;
        }

        Mesh mesh = BuildMesh(generator, key, chunkResolution, chunksPerFace);
        filter.sharedMesh = mesh;
        colliderComponent.sharedMesh = mesh;

        PlanetChunk chunk = new PlanetChunk();
        chunk.key = key;
        chunk.gameObject = chunkObject;
        chunk.mesh = mesh;
        chunk.bounds = mesh.bounds;
        return chunk;
    }

    public Mesh BuildMesh(PlanetGenerator generator, PlanetChunkKey key, int chunkResolution, int chunksPerFace)
    {
        int vertsPerLine = chunkResolution + 1;
        Vector3[] vertices = new Vector3[vertsPerLine * vertsPerLine];
        Vector3[] normals = new Vector3[vertices.Length];
        Color[] colors = new Color[vertices.Length];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[chunkResolution * chunkResolution * 6];

        int triIndex = 0;
        for (int y = 0; y <= chunkResolution; y++)
        {
            for (int x = 0; x <= chunkResolution; x++)
            {
                float localU = x / (float)chunkResolution;
                float localV = y / (float)chunkResolution;
                float faceU = (key.x + localU) / chunksPerFace;
                float faceV = (key.y + localV) / chunksPerFace;
                Vector3 direction = FaceUvToDirection(key.face, faceU, faceV);
                PlanetSurfaceSample sample = generator.SampleSurface(direction);
                int index = y * vertsPerLine + x;
                vertices[index] = direction * (generator.planetRadius + sample.altitude);
                normals[index] = direction;
                colors[index] = sample.biome != null ? Color.Lerp(sample.biome.primaryColor, sample.biome.secondaryColor, sample.noise.heightNoise) : Color.white;
                uvs[index] = new Vector2(faceU, faceV);

                if (x < chunkResolution && y < chunkResolution)
                {
                    int a = index;
                    int b = index + vertsPerLine + 1;
                    int c = index + vertsPerLine;
                    int d = index + 1;
                    triangles[triIndex++] = a;
                    triangles[triIndex++] = b;
                    triangles[triIndex++] = c;
                    triangles[triIndex++] = a;
                    triangles[triIndex++] = d;
                    triangles[triIndex++] = b;
                }
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "CubeSphere_" + key;
        if (vertices.Length > 65000)
        {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.colors = colors;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    public static Vector3 FaceUvToDirection(int face, float u, float v)
    {
        float x = u * 2f - 1f;
        float y = v * 2f - 1f;
        Vector3 point;
        switch (face)
        {
            case 0: point = new Vector3(1f, y, -x); break;
            case 1: point = new Vector3(-1f, y, x); break;
            case 2: point = new Vector3(x, 1f, -y); break;
            case 3: point = new Vector3(x, -1f, y); break;
            case 4: point = new Vector3(x, y, 1f); break;
            default: point = new Vector3(-x, y, -1f); break;
        }
        return CubeToSphere(point).normalized;
    }

    public static PlanetSurfaceCoordinate DirectionToCoordinate(Vector3 direction, float altitude)
    {
        direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.up;
        Vector3 abs = new Vector3(Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z));
        int face;
        Vector2 uv;
        if (abs.x >= abs.y && abs.x >= abs.z)
        {
            if (direction.x >= 0f)
            {
                face = 0;
                uv = new Vector2((-direction.z / abs.x + 1f) * 0.5f, (direction.y / abs.x + 1f) * 0.5f);
            }
            else
            {
                face = 1;
                uv = new Vector2((direction.z / abs.x + 1f) * 0.5f, (direction.y / abs.x + 1f) * 0.5f);
            }
        }
        else if (abs.y >= abs.x && abs.y >= abs.z)
        {
            if (direction.y >= 0f)
            {
                face = 2;
                uv = new Vector2((direction.x / abs.y + 1f) * 0.5f, (-direction.z / abs.y + 1f) * 0.5f);
            }
            else
            {
                face = 3;
                uv = new Vector2((direction.x / abs.y + 1f) * 0.5f, (direction.z / abs.y + 1f) * 0.5f);
            }
        }
        else
        {
            if (direction.z >= 0f)
            {
                face = 4;
                uv = new Vector2((direction.x / abs.z + 1f) * 0.5f, (direction.y / abs.z + 1f) * 0.5f);
            }
            else
            {
                face = 5;
                uv = new Vector2((-direction.x / abs.z + 1f) * 0.5f, (direction.y / abs.z + 1f) * 0.5f);
            }
        }
        return new PlanetSurfaceCoordinate(direction, altitude, face, uv);
    }

    private static Vector3 CubeToSphere(Vector3 p)
    {
        float x2 = p.x * p.x;
        float y2 = p.y * p.y;
        float z2 = p.z * p.z;
        return new Vector3(
            p.x * Mathf.Sqrt(1f - y2 * 0.5f - z2 * 0.5f + y2 * z2 / 3f),
            p.y * Mathf.Sqrt(1f - z2 * 0.5f - x2 * 0.5f + z2 * x2 / 3f),
            p.z * Mathf.Sqrt(1f - x2 * 0.5f - y2 * 0.5f + x2 * y2 / 3f));
    }
}
