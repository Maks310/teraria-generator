using UnityEngine;

public class WaterController : MonoBehaviour
{
    [Header("Wave Settings")]
    public float waveSpeed = 0.5f;
    public float waveScale = 0.1f;
    public float waveHeight = 0.5f;

    [Header("Material Settings")]
    public bool animateMaterial = true;

    private Material _waterMaterial;
    private float _timeOffset;

    private static readonly int WaveOffsetID = Shader.PropertyToID("_WaveOffset");
    private static readonly int WaveScaleID = Shader.PropertyToID("_WaveScale");
    private static readonly int WaveHeightID = Shader.PropertyToID("_WaveHeight");

    public void Initialize(Material waterMaterial)
    {
        _waterMaterial = waterMaterial;
        ApplyWaveSettings();
    }

    private void OnValidate()
    {
        waveSpeed = Mathf.Max(0f, waveSpeed);
        waveScale = Mathf.Max(0.0001f, waveScale);
        waveHeight = Mathf.Max(0f, waveHeight);
        ApplyWaveSettings();
    }

    private void Update()
    {
        if (_waterMaterial == null || !animateMaterial)
        {
            return;
        }

        _timeOffset += Time.deltaTime * waveSpeed;

        if (_waterMaterial.HasProperty(WaveOffsetID))
        {
            _waterMaterial.SetFloat(WaveOffsetID, _timeOffset);
        }
    }

    public Vector3 GetWaveDisplacement(Vector3 position, float time)
    {
        float wave1 = Mathf.Sin(position.x * waveScale + time * waveSpeed) *
                      Mathf.Cos(position.z * waveScale * 0.7f + time * waveSpeed * 0.8f);
        float wave2 = Mathf.Sin(position.x * waveScale * 1.3f + time * waveSpeed * 1.2f) *
                      Mathf.Cos(position.z * waveScale * 1.1f + time * waveSpeed);

        return new Vector3(0f, (wave1 + wave2 * 0.5f) * waveHeight, 0f);
    }

    private void ApplyWaveSettings()
    {
        if (_waterMaterial == null)
        {
            return;
        }

        if (_waterMaterial.HasProperty(WaveScaleID))
        {
            _waterMaterial.SetFloat(WaveScaleID, waveScale);
        }

        if (_waterMaterial.HasProperty(WaveHeightID))
        {
            _waterMaterial.SetFloat(WaveHeightID, waveHeight);
        }
    }
}
