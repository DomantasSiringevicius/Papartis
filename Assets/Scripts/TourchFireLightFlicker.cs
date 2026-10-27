using UnityEngine;

[RequireComponent(typeof(Light))]
public class TorchFireLightFlicker : MonoBehaviour
{
    [SerializeField] float burnDuration = 60f;
    [SerializeField] float minimumLight = 6f;
    [SerializeField] float maximumLight = 12f;
    [SerializeField] float flickerSpeed = 6f;

    public bool IsLit = false;
    public float SecondsLeft = 0f;

    Light fireLight;
    ParticleSystem fireParticles;
    Vector3 startingScale;
    float timer;
    float randomOffset;

    void Awake()
    {
        fireLight = GetComponent<Light>();
        fireParticles = GetComponentInParent<ParticleSystem>();

        startingScale = fireParticles.transform.localScale;
        randomOffset = Random.value * 100f;
    }

    void OnEnable()
    {
        timer = 0f;
        SecondsLeft = burnDuration;
        IsLit = true;

        fireLight.enabled = true;
        fireParticles.transform.localScale = startingScale;
        fireParticles.Play();
    }

    void OnDisable()
    {
        IsLit = false;
        SecondsLeft = 0f;
    }

    void Update()
    {
        timer += Time.deltaTime;
        SecondsLeft = burnDuration - timer;

        if (SecondsLeft <= 0f)
        {
            SecondsLeft = 0f;
            IsLit = false;

            fireLight.intensity = 0f;
            fireParticles.Stop();
            fireParticles.gameObject.SetActive(false);

            return;
        }

        float flicker = Mathf.PerlinNoise(
            randomOffset,
            Time.time * flickerSpeed
        );

        float lightAmount = Mathf.Lerp(
            minimumLight,
            maximumLight,
            flicker
        );

        float fadeAmount = SecondsLeft / burnDuration;

        fireLight.intensity = lightAmount * fadeAmount;

        fireParticles.transform.localScale = Vector3.Lerp(
            Vector3.zero,
            startingScale,
            fadeAmount
        );
    }
}