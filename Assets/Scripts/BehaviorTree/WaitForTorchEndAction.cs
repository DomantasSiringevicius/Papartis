using System;
using Unity.Behavior;
using UnityEngine;
using Unity.Properties;
using Action = Unity.Behavior.Action;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Wait For Torch", story: "Wait until the torch has 10 seconds left", category: "Action", id: "6d8f31d8c2c44d0aa1c75f2e8c0a1b44")]
public partial class WaitForTorchEndAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Torch;
    [SerializeReference] public BlackboardVariable<float> PauseSeconds = new BlackboardVariable<float>(4f);

    private float startTime;

    protected override Status OnStart()
    {
        startTime = Time.time;
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Torch == null || Torch.Value == null)
        {
            return Status.Failure;
        }

        TorchFireLightFlicker torchScript =
            Torch.Value.GetComponent<TorchFireLightFlicker>();

        if (torchScript == null)
        {
            torchScript =
                Torch.Value.GetComponentInChildren<TorchFireLightFlicker>(true);
        }

        if (torchScript == null)
        {
            return Status.Failure;
        }

        if (!torchScript.IsLit || torchScript.SecondsLeft > 10f)
        {
            return Status.Running;
        }

        if (Time.time - startTime < PauseSeconds.Value)
        {
            return Status.Running;
        }

        return Status.Success;
    }
}