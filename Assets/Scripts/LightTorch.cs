using UnityEngine;

public class LightTorch : MonoBehaviour
{
    [SerializeField] GameObject Fire;

    bool isLit;

    private void Awake()
    {
        Fire.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(isLit)
        {
            return;
        }
        if (other.GetComponent<FireSource>() == null)
        {
            return;
        }

        isLit = true;
        Fire.SetActive(true);
    }
}
