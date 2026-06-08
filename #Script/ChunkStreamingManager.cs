using System.Collections.Generic;
using UnityEngine;

public class ChunkStreamingManager : MonoBehaviour
{
    [Header("References")]
    public PlanetGenerator planet;
    public Transform player;

    [Header("Streaming")]
    [Range(0, 8)] public int chunkRadius = 1;
    public bool streamAtRuntime = true;
    public bool keepAllChunksInEditorPreview;
    public float updateInterval = 0.5f;

    private float _nextUpdate;
    private readonly HashSet<string> _wanted = new HashSet<string>();

    private void Update()
    {
        if (!streamAtRuntime || planet == null || player == null || !Application.isPlaying)
        {
            return;
        }

        if (Time.time >= _nextUpdate)
        {
            _nextUpdate = Time.time + Mathf.Max(0.05f, updateInterval);
            UpdateStreaming();
        }
    }

    [ContextMenu("Update Chunk Streaming")]
    public void UpdateStreaming()
    {
        if (planet == null || player == null)
        {
            return;
        }

        PlanetSurfaceCoordinate coordinate = planet.WorldToSurfaceCoordinate(player.position);
        int chunksPerFace = Mathf.Max(1, planet.chunksPerFace);
        int centerX = Mathf.Clamp(Mathf.FloorToInt(coordinate.faceUv.x * chunksPerFace), 0, chunksPerFace - 1);
        int centerY = Mathf.Clamp(Mathf.FloorToInt(coordinate.faceUv.y * chunksPerFace), 0, chunksPerFace - 1);
        _wanted.Clear();

        for (int dy = -chunkRadius; dy <= chunkRadius; dy++)
        {
            for (int dx = -chunkRadius; dx <= chunkRadius; dx++)
            {
                int x = centerX + dx;
                int y = centerY + dy;
                int face = coordinate.faceIndex;
                if (x < 0 || x >= chunksPerFace || y < 0 || y >= chunksPerFace)
                {
                    continue; // Neighbor-face stitching can be added without changing persisted planet coordinates.
                }
                CubeSphereMeshBuilder.PlanetChunkKey key = new CubeSphereMeshBuilder.PlanetChunkKey(face, x, y);
                _wanted.Add(key.ToString());
                planet.BuildOrLoadChunk(key);
            }
        }

        if (!keepAllChunksInEditorPreview)
        {
            List<CubeSphereMeshBuilder.PlanetChunkKey> unload = new List<CubeSphereMeshBuilder.PlanetChunkKey>();
            foreach (CubeSphereMeshBuilder.PlanetChunk chunk in planet.LoadedChunks)
            {
                if (!_wanted.Contains(chunk.key.ToString()))
                {
                    unload.Add(chunk.key);
                }
            }
            for (int i = 0; i < unload.Count; i++)
            {
                planet.UnloadChunk(unload[i]);
            }
        }
    }
}
