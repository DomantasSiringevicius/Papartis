using UnityEngine;

public class GateUnlockZone : MonoBehaviour
{
    public GateOpen Gate;

    void OnTriggerEnter(Collider other)
    {
        FlowerAssembly flower = other.GetComponentInParent<FlowerAssembly>();
        if (flower != null && flower.IsComplete)
            Gate.Unlock();
    }
}
