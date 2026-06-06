//using UnityEngine;
//using System.Collections.Generic;

//public class WorldObjectSpawner : MonoBehaviour
//{
//    public WorldGenerator worldGenerator;
//    public GameObject[] treePrefabs;
//    public GameObject[] rockPrefabs;

//    [Range(0f, 1f)] public float treeDensity = 0.01f;
//    [Range(0f, 1f)] public float rockDensity = 0.005f;

//    public void SpawnObjects()
//    {
//        if (worldGenerator == null || worldGenerator.BiomeMap == null) return;

//        int worldSize = worldGenerator.WorldSize;
//        float waterLevel = worldGenerator.WaterLevel * worldGenerator.HeightScale;
            
//        for (int z = 0; z < worldSize; z += 16) // Крок для оптимізації
//        {
//            for (int x = 0; x < worldSize; x += 16)
//            {
//                float height = worldGenerator.GetHeightAt(x, z);
//                if (height < waterLevel) continue; // Не спавнимо під водою

//                BiomeType biome = worldGenerator.GetBiomeAt(x, z);
//                BiomeData biomeData = GetBiomeData(biome);

//                if (biomeData == null) continue;

//                // Дерева
//                if (biomeData.canSpawnTrees && Random.value < treeDensity * biomeData.vegetationDensity)
//                {
//                    SpawnObject(treePrefabs, new Vector3(x, height, z));
//                }

//                // Каміння
//                if (biomeData.canSpawnRocks && Random.value < rockDensity)
//                {
//                    SpawnObject(rockPrefabs, new Vector3(x, height, z));
//                }
//            }
//        }
//    }
        
//    private void SpawnObject(GameObject[] prefabs, Vector3 position)
//    {
//        if (prefabs == null || prefabs.Length == 0) return;

//        GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
//        Instantiate(prefab, position, Quaternion.Euler(0, Random.Range(0, 360), 0), transform);
//    }

//    private BiomeData GetBiomeData(BiomeType type)
//    {
//        // Тут можна кешувати або брати з WorldGenerator
//        return null; // Імплементуй через посилання
//    }
//}
