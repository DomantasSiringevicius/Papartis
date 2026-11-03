using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class LightTorch : MonoBehaviour
{
    [SerializeField] GameObject Fire;
    [SerializeField] AudioClip ignitionSound;

    private AudioSource audioSource;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.Stop();
        Fire.SetActive(false);
    }

    void OnTriggerStay(Collider other)
    {
        if (other.GetComponentInParent<FireSource>() == null)
        {
            return;
        }

        if (!Fire.activeSelf)
        {
            Fire.SetActive(true);
            audioSource.Play();
            audioSource.PlayOneShot(ignitionSound);
        }
    }
}
