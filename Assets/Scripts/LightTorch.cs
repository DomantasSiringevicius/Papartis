using UnityEngine;

public class LightTorch : MonoBehaviour
{
    [SerializeField] GameObject Fire;

    void Awake()
    {
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
        }
    }
}
