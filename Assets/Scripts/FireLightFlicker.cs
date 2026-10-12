using UnityEngine;

/// <summary>
/// Makes a light flicker like a fire by varying its intensity with smooth noise.
/// </summary>
[RequireComponent(typeof(Light))]
public class FireLightFlicker : MonoBehaviour
{
    [SerializeField] float m_MinIntensity = 2f;
    [SerializeField] float m_MaxIntensity = 3.5f;
    [SerializeField] float m_Speed = 6f;

    Light m_Light;
    float m_NoiseOffset;

    void Awake()
    {
        m_Light = GetComponent<Light>();
        // Different offset per fire so several braziers do not flicker in sync.
        m_NoiseOffset = Random.value * 100f;
    }

    void Update()
    {
        var noise = Mathf.PerlinNoise(m_NoiseOffset, Time.time * m_Speed);
        m_Light.intensity = Mathf.Lerp(m_MinIntensity, m_MaxIntensity, noise);
    }
}
