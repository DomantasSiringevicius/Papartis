using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRSimpleInteractable))]
public class GateOpen : MonoBehaviour
{
    [SerializeField] float openAngle = 90f;
    [SerializeField] float openSpeed = 4f;
    [SerializeField] float unlockZonePadding = 1.5f;

    XRSimpleInteractable interactable;
    Transform hand;
    Quaternion closedRotation;
    float handStartAngle;
    float gateStartAngle;
    float targetAngle;
    bool pulling;
    bool unlocked;

    void Awake()
    {
        interactable = GetComponentInChildren<XRSimpleInteractable>();
        interactable.selectEntered.AddListener(OnGrab);
        interactable.selectExited.AddListener(OnRelease);

        closedRotation = transform.localRotation;
        CreateUnlockZone();
    }

    void CreateUnlockZone()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer gateRenderer in renderers)
            bounds.Encapsulate(gateRenderer.bounds);

        GameObject zone = new GameObject("Gate Unlock Zone");
        zone.transform.position = bounds.center;
        BoxCollider box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = bounds.size + Vector3.one * unlockZonePadding * 2f;
        zone.AddComponent<GateUnlockZone>().Gate = this;
    }

    public void Unlock()
    {
        unlocked = true;
    }

    void OnDestroy()
    {
        interactable.selectEntered.RemoveListener(OnGrab);
        interactable.selectExited.RemoveListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        if (!unlocked || args.interactorObject.handedness != InteractorHandedness.Right)
            return;

        hand = args.interactorObject.GetAttachTransform(interactable);
        handStartAngle = HandAngle();
        gateStartAngle = targetAngle;
        pulling = true;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        if (hand == args.interactorObject.GetAttachTransform(interactable))
        {
            pulling = false;
            hand = null;
        }
    }

    float HandAngle()
    {
        Quaternion parentRotation = transform.parent != null ? transform.parent.rotation : Quaternion.identity;
        Vector3 offset = Quaternion.Inverse(parentRotation * closedRotation) * (hand.position - transform.position);
        return Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
    }

    void Update()
    {
        if (pulling && hand != null)
        {
            float turned = Mathf.DeltaAngle(handStartAngle, HandAngle());
            targetAngle = Mathf.Clamp(gateStartAngle + turned, -openAngle, openAngle);
        }

        Quaternion target = closedRotation * Quaternion.Euler(0f, targetAngle, 0f);

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation, target, openSpeed * Time.deltaTime);
    }
}
