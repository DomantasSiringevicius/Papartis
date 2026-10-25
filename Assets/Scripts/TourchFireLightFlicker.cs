using UnityEngine;

[RequireComponent(typeof(Light))]
public class TorchFireLightFlicker : MonoBehaviour
{
    [SerializeField] float m_MinIntensity = 6f;
    [SerializeField] float m_MaxIntensity = 12f;
    [SerializeField] float m_Speed = 6f;
    [SerializeField] float m_BurnDuration = 60f;

    Light m_Light;
    ParticleSystem m_FireParticles;

    Vector3 m_StartScale;
    float m_Timer;
    float m_NoiseOffset;

    void Awake()
    {
        m_Light = GetComponent<Light>();
        m_FireParticles = GetComponentInParent<ParticleSystem>();
        m_StartScale = m_FireParticles.transform.localScale;
        m_NoiseOffset = Random.value * 100f;
    }

    void OnEnable()
    {
        m_Timer = 0f;
        m_Light.enabled = true;
        m_FireParticles.transform.localScale = m_StartScale;
        m_FireParticles.Play();
    }

    void Update()
    {
        m_Timer += Time.deltaTime;

        if (m_Timer >= m_BurnDuration)
        {
            m_Light.intensity = 0f;
            m_FireParticles.Stop();
            m_FireParticles.gameObject.SetActive(false);
            return;
        }

        float progress = m_Timer / m_BurnDuration;

        float noise = Mathf.PerlinNoise(
            m_NoiseOffset,
            Time.time * m_Speed
        );

        float flicker = Mathf.Lerp(
            m_MinIntensity,
            m_MaxIntensity,
            noise
        );

        m_Light.intensity = flicker * (1f - progress);

        m_FireParticles.transform.localScale = Vector3.Lerp(
            m_StartScale,
            Vector3.zero,
            progress
        );
    }
}