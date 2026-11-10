using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class LightTorch : MonoBehaviour
{
    [SerializeField] GameObject Fire;
    [SerializeField] AudioClip ignitionSound;
    [SerializeField] float secondsToIgnite = 5f;

    private AudioSource audioSource;
    private int fireSourcesTouching;
    private float timeInFire;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.Stop();
        Fire.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<FireSource>() != null)
        {
            fireSourcesTouching++;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<FireSource>() != null)
        {
            fireSourcesTouching = Mathf.Max(0, fireSourcesTouching - 1);
        }
    }

    void Update()
    {
        if (Fire.activeSelf || fireSourcesTouching == 0)
        {
            timeInFire = 0f;
            return;
        }

        timeInFire += Time.deltaTime;

        if (timeInFire >= secondsToIgnite)
        {
            Fire.SetActive(true);
            audioSource.Play();
            audioSource.PlayOneShot(ignitionSound);
        }
    }
}