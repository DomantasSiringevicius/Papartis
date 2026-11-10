using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRSimpleInteractable))]
public class GateOpen : MonoBehaviour
{
    [SerializeField] float openAngle = 90f;
    [SerializeField] float openSpeed = 4f;

    XRSimpleInteractable interactable;
    Transform hand;
    Quaternion closedRotation;
    float handStartAngle;
    float gateStartAngle;
    float targetAngle;
    bool pulling;

    void Awake()
    {
        interactable = GetComponentInChildren<XRSimpleInteractable>();
        interactable.selectEntered.AddListener(OnGrab);
        interactable.selectExited.AddListener(OnRelease);

        closedRotation = transform.localRotation;
    }

    void OnDestroy()
    {
        interactable.selectEntered.RemoveListener(OnGrab);
        interactable.selectExited.RemoveListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        if (args.interactorObject.handedness != InteractorHandedness.Right)
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
