using UnityEngine;

[ExecuteAlways]
public class WaterController : MonoBehaviour
{
    [Header("Wave Settings")]
    public float waveSpeed = 0.45f;
    public float waveScale = 6f;
    public float waveHeight = 0.45f;

    [Header("Runtime")]
    public bool animateMaterial = true;

    private Material _waterMaterial;
    private float _timeOffset;

    private static readonly int WaveOffsetId = Shader.PropertyToID("_WaveOffset");
    private static readonly int WaveScaleId = Shader.PropertyToID("_WaveScale");
    private static readonly int WaveHeightId = Shader.PropertyToID("_WaveHeight");
    private static readonly int WorldSizeId = Shader.PropertyToID("_WorldSize");

    public void Initialize(Material waterMaterial, float worldSize)
    {
        _waterMaterial = waterMaterial;
        ApplyWaveSettings(worldSize);
    }

    private void OnValidate()
    {
        waveSpeed = Mathf.Max(0f, waveSpeed);
        waveScale = Mathf.Max(0.01f, waveScale);
        waveHeight = Mathf.Max(0f, waveHeight);
        ApplyWaveSettings(transform.lossyScale.x);
    }

    private void Update()
    {
        if (_waterMaterial == null || !animateMaterial)
        {
            return;
        }

        _timeOffset += Time.deltaTime * waveSpeed;
        if (_waterMaterial.HasProperty(WaveOffsetId))
        {
            _waterMaterial.SetFloat(WaveOffsetId, _timeOffset);
        }
    }

    public Vector3 GetWaveDisplacement(Vector3 position, float time)
    {
        float worldSize = Mathf.Max(1f, transform.lossyScale.x);
        float angleX = position.x / worldSize * Mathf.PI * 2f;
        float angleZ = position.z / worldSize * Mathf.PI * 2f;
        float waveA = Mathf.Sin(angleX * waveScale + time * waveSpeed) * Mathf.Cos(angleZ * (waveScale * 0.7f) + time * waveSpeed * 0.8f);
        float waveB = Mathf.Sin(angleX * (waveScale * 1.7f) - time * waveSpeed * 1.1f) * Mathf.Cos(angleZ * (waveScale * 1.3f) + time * waveSpeed);
        return new Vector3(0f, (waveA + waveB * 0.5f) * waveHeight, 0f);
    }

    private void ApplyWaveSettings(float worldSize)
    {
        if (_waterMaterial == null)
        {
            Renderer rendererComponent = GetComponent<Renderer>();
            if (rendererComponent != null)
            {
                _waterMaterial = Application.isPlaying ? rendererComponent.material : rendererComponent.sharedMaterial;
            }
        }

        if (_waterMaterial == null)
        {
            return;
        }

        if (_waterMaterial.HasProperty(WaveScaleId))
        {
            _waterMaterial.SetFloat(WaveScaleId, waveScale);
        }

        if (_waterMaterial.HasProperty(WaveHeightId))
        {
            _waterMaterial.SetFloat(WaveHeightId, waveHeight);
        }

        if (_waterMaterial.HasProperty(WorldSizeId))
        {
            _waterMaterial.SetFloat(WorldSizeId, Mathf.Max(1f, worldSize));
        }
    }
}
