using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class FlowerAssembly : MonoBehaviour
{
    [SerializeField] XRSocketInteractor[] m_Sockets;
    [Tooltip("The flower's own petal in each socket's place, hidden until that petal is brought.")]
    [SerializeField] Renderer[] m_PetalPlaces;
    [Tooltip("Colliders used to pick up the finished flower.")]
    [SerializeField] Collider[] m_FlowerColliders;
    [SerializeField] XRGrabInteractable m_FlowerGrab;

    int m_PlacedCount;

    public bool IsComplete => m_PlacedCount == m_Sockets.Length;

    void Awake()
    {
        foreach (var place in m_PetalPlaces)
            place.enabled = false;
        foreach (var flowerCollider in m_FlowerColliders)
            flowerCollider.enabled = false;
        m_FlowerGrab.enabled = false;
    }

    void OnEnable()
    {
        foreach (var socket in m_Sockets)
            socket.selectEntered.AddListener(OnPetalPlaced);
        m_FlowerGrab.selectExited.AddListener(OnFlowerReleased);
    }

    void OnDisable()
    {
        foreach (var socket in m_Sockets)
            socket.selectEntered.RemoveListener(OnPetalPlaced);
        m_FlowerGrab.selectExited.RemoveListener(OnFlowerReleased);
    }

    void OnPetalPlaced(SelectEnterEventArgs args)
    {
        var index = System.Array.IndexOf(m_Sockets, args.interactorObject as XRSocketInteractor);
        if (index >= 0)
            StartCoroutine(LockPetal(index, args.interactableObject.transform.gameObject));
    }

    IEnumerator LockPetal(int index, GameObject petal)
    {
        yield return null;

        petal.SetActive(false);
        m_Sockets[index].gameObject.SetActive(false);
        m_PetalPlaces[index].enabled = true;
        m_PlacedCount++;

        if (IsComplete)
        {
            foreach (var flowerCollider in m_FlowerColliders)
                flowerCollider.enabled = true;
            m_FlowerGrab.enabled = true;
        }
    }

    void OnFlowerReleased(SelectExitEventArgs args)
    {
        var body = m_FlowerGrab.GetComponent<Rigidbody>();
        body.isKinematic = false;
        body.useGravity = true;
    }
}
