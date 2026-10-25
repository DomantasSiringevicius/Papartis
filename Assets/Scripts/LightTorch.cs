using UnityEngine;

public class LightTorch : MonoBehaviour
{
    [SerializeField] GameObject Fire;

    bool isLit;

    private void Awake()
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
