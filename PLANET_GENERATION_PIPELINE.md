# Planet Survival World Generation Pipeline

This project now has a modular planet-first generation path that lives beside the older flat preview generator. The gameplay path is `PlanetGenerator`, not square wrapping or torus teleporting.

## Runtime order

1. `PlanetGenerator` owns the seed, radius, water level, height scale, shared materials, and references to all subsystems.
2. `PlanetNoise` samples spherical noise from a normalized surface direction. It produces continent, height, ridge, climate, cave, floating-island, volcanic, magnetic, crystal, meteor, ruin, and combined anomaly masks.
3. `PlanetClimate` turns latitude, elevation, ocean proximity, volcanic heat, and climate noise into temperature, moisture, humidity, and broad climate zones.
4. `PlanetBiomeResolver` applies layered rules: broad climate bands first, then height and moisture ranges, then rare anomaly masks and POI-driven overrides.
5. `CubeSphereMeshBuilder` builds the six-face cube-sphere in chunks. Each vertex samples the same global planet data, so terrain stays continuous across chunk seams.
6. `PlanetWaterSystem` creates a spherical water shell and resolves biome-specific liquid types such as red swamp water, acid, lava, and hot springs.
7. `PlanetSurfaceSpawner` samples chunks instead of the whole planet and stores `PlanetSurfaceCoordinate` data on spawned `PlanetPlacedObject` components for future persistence and building support.
8. `PlanetPOIManager` records ruins, meteor zones, geysers, traps, loot, floating island anchors, and volcanic vents separately from mesh chunks.
9. `ChunkStreamingManager` loads local chunks around the player while keeping world data global.
10. `PlayerPlanetController` disables Unity's global gravity and moves the player tangent to the planet surface with gravity toward the center.
11. `PlanetMapSystem` samples a separate equirectangular map from planet data and can track discovered regions without depending on rendered mesh chunks.

## Biome concepts included

The default resolver creates runtime `BiomeDefinition` entries for all requested concepts: Violet Forest, Crystal Desert, Crimson Swamp, Bioluminescent Jungle, Forest of Giant Flowers, Frozen Wasteland, Volcanic Fields, Ash Plains, Steam Valleys, Magnetic Cliffs, Stone Tree Forest, Acid Lakes, Firefly Caves, Meteor Zone, Glassland, Energy Crystal Fields, Black Forest, Mushroom Ocean, Floating Islands, and Ancient Ruins.

For production tuning, create `BiomeDefinition` assets from **Create > Planet > Biome Definition** and assign them to `PlanetBiomeResolver.biomeDefinitions`. Existing `BiomeData` assets can still be assigned through `legacyBiomeData` or per-biome `legacyBiomeData` fields.

## Notes for future expansion

- Do not attach core survival gameplay to the old flat `WorldGenerator` wrapping flags. Keep that path for previews or legacy scenes only.
- Persist buildings and player-placed objects by `PlanetSurfaceCoordinate`, not by terrain chunk GameObject.
- Add pooling or GPU instancing under `PlanetSurfaceSpawner` without changing biome resolution.
- Add neighbor-face streaming in `ChunkStreamingManager` when the player approaches cube-face borders; planet coordinates already remain continuous.
- Add LOD by replacing `CubeSphereMeshBuilder.BuildChunk` resolution per chunk key while continuing to sample `PlanetGenerator.SampleSurface`.
