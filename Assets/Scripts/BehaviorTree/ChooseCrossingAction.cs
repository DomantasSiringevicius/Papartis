using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Choose Crossing", story: "Position [Character] in front of [Player] toward [Destination]", category: "Action", id: "a7a1b315db7186d6b2f6ce196deae0c6")]
public partial class ChooseCrossingAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Character;
    [SerializeReference] public BlackboardVariable<GameObject> Player;
    [SerializeReference] public BlackboardVariable<Vector3> Destination;

    protected override Status OnStart()
    {
        if (Character == null || Player == null || Destination == null ||
            !Character.Value || !Player.Value)
            return Status.Failure;

        float minDistance = 8f;
        float maxDistance = 12f;

        float halfWidth = 6f;

        Vector3 forward = Vector3.ProjectOnPlane(
            Player.Value.transform.forward, Vector3.up);

        if (forward.sqrMagnitude < 0.01f)
            return Status.Failure;

        forward.Normalize();
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        Vector3 centre = Player.Value.transform.position
            + forward * UnityEngine.Random.Range(minDistance, maxDistance);

        float side = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        Vector3 start = centre + right * halfWidth * side;
        Vector3 end = centre - right * halfWidth * side;

        if (!Physics.Raycast(
                start + Vector3.up * 5f, Vector3.down,
                out RaycastHit startHit, 20f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            || !Physics.Raycast(
                end + Vector3.up * 5f, Vector3.down,
                out RaycastHit endHit, 20f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return Status.Failure;

        Character.Value.transform.SetPositionAndRotation(
            startHit.point + Vector3.up,
            Quaternion.LookRotation(end - start));

        Destination.Value = endHit.point + Vector3.up;
        MimicSpace.Movement movement = Character.Value.GetComponent<MimicSpace.Movement>();
        if (!movement)
            return Status.Failure;

        Character.Value.SetActive(true);
        movement.SetDestination(Destination.Value);

        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (!Character.Value)
            return Status.Failure;

        MimicSpace.Movement movement = Character.Value.GetComponent<MimicSpace.Movement>();
        if (!movement)
            return Status.Failure;

        return movement.IsMoving ? Status.Running : Status.Success;
    }

    protected override void OnEnd()
    {
    }
}
