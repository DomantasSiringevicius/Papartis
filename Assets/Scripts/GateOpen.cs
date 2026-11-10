using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRSimpleInteractable))]
public class GateOpen : MonoBehaviour
{
    [SerializeField] float pushDistance = 0.5f;
    [SerializeField] float openAngle = 90f;
    [SerializeField] float openSpeed = 4f;

    XRSimpleInteractable interactable;
    Transform hand;
    Vector3 handStart;
    Vector3 doorNormal;
    Quaternion closedRotation;
    float openAmount;
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
        handStart = hand.position;
        doorNormal = transform.forward;
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

    void Update()
    {
        if (pulling && hand != null)
        {
            float push = -Vector3.Dot(hand.position - handStart, doorNormal);
            openAmount = Mathf.Clamp01(push / pushDistance);
        }

        Quaternion openRotation =
            closedRotation * Quaternion.Euler(0f, openAngle, 0f);
        Quaternion target = Quaternion.Slerp(closedRotation, openRotation, openAmount);

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation, target, openSpeed * Time.deltaTime);
    }
}