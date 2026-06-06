using UnityEngine;

public class WaterController : MonoBehaviour
{
    [Header("Wave Settings")]
    public float waveSpeed = 0.5f;
    public float waveScale = 0.1f;
    public float waveHeight = 0.5f;

    private Material _waterMaterial;
    private float _timeOffset;

    // Shader property IDs для кешування
    private static readonly int WaveOffsetID = Shader.PropertyToID("_WaveOffset");
    private static readonly int TimeID = Shader.PropertyToID("_Time");

    public void Initialize(Material waterMaterial)
    {
        _waterMaterial = waterMaterial;
    }

    private void Update()
    {
        if (_waterMaterial == null) return;

        _timeOffset += Time.deltaTime * waveSpeed;

        // Оновлюємо параметри шейдера
        if (_waterMaterial.HasProperty(WaveOffsetID))
        {
            _waterMaterial.SetFloat(WaveOffsetID, _timeOffset);
        }
    }

    /// <summary>
    /// Архітектура для майбутніх Gerstner waves
    /// </summary>
    public Vector3 GetWaveDisplacement(Vector3 position, float time)
    {
        // Проста синусоїдальна хвиля (замінити на Gerstner пізніше)
        float wave = Mathf.Sin(position.x * waveScale + time * waveSpeed) *
                     Mathf.Cos(position.z * waveScale + time * waveSpeed * 0.7f);

        return new Vector3(0, wave * waveHeight, 0);
    }
}
