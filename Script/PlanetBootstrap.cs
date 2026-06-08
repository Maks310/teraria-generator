using UnityEngine;

namespace TerariaGenerator.Planets
{
    public sealed class PlanetBootstrap : MonoBehaviour
    {
        [SerializeField] private PlanetSettings settings;
        [SerializeField] private PlanetPreset preset = PlanetPreset.Balanced;
        [SerializeField] private bool createPlanetOnAwake = true;

        private void Awake()
        {
            if (!createPlanetOnAwake || FindObjectOfType<CubeSpherePlanetGenerator>() != null)
            {
                return;
            }

            PlanetSettings activeSettings = settings != null ? settings : PlanetSettings.CreateRuntimeDefault();
            if (settings == null)
            {
                activeSettings.ApplyPreset(preset);
            }

            GameObject planet = new GameObject("Procedural Cube Sphere Planet");
            CubeSpherePlanetGenerator generator = planet.AddComponent<CubeSpherePlanetGenerator>();
            generator.Settings = activeSettings;
            generator.GeneratePlanet();
        }
    }
}
