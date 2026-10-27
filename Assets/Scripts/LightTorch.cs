using UnityEngine;

public class LightTorch : MonoBehaviour
{
    [SerializeField] GameObject Fire;

    void Awake()
    {
        Fire.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<FireSource>() == null)
        {
            return;
        }

        Fire.SetActive(true);
    }
}