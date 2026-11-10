using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(Rigidbody), typeof(XRGrabInteractable))]
public class FeatherFall : MonoBehaviour
{
    [SerializeField] float m_GravityScale = 0.15f;
    [SerializeField] float m_AirResistance = 2.5f;
    [SerializeField] float m_SpinResistance = 2f;
    [SerializeField] float m_SwayStrength = 0.6f;
    [SerializeField] float m_SwaySpeed = 0.8f;

    Rigidbody m_Body;
    XRGrabInteractable m_Grab;
    float m_NoiseOffset;

    void Awake()
    {
        m_Body = GetComponent<Rigidbody>();
        m_Grab = GetComponent<XRGrabInteractable>();
        m_Body.useGravity = false;
        m_Body.linearDamping = m_AirResistance;
        m_Body.angularDamping = m_SpinResistance;
        m_NoiseOffset = Random.value * 100f;
    }

    void OnEnable()
    {
        m_Grab.selectExited.AddListener(OnReleased);
    }

    void OnDisable()
    {
        m_Grab.selectExited.RemoveListener(OnReleased);
    }

    void OnReleased(SelectExitEventArgs args)
    {
        if (!(args.interactorObject is XRSocketInteractor))
            m_Body.isKinematic = false;
    }

    void FixedUpdate()
    {
        if (m_Body.isKinematic)
            return;

        m_Body.AddForce(Physics.gravity * m_GravityScale, ForceMode.Acceleration);

        var time = Time.time * m_SwaySpeed;
        var sway = new Vector3(
            Mathf.PerlinNoise(m_NoiseOffset, time) - 0.5f,
            0f,
            Mathf.PerlinNoise(m_NoiseOffset + 50f, time) - 0.5f);
        m_Body.AddForce(sway * m_SwayStrength * 2f, ForceMode.Acceleration);
        m_Body.AddTorque(sway * m_SwayStrength, ForceMode.Acceleration);
    }
}
