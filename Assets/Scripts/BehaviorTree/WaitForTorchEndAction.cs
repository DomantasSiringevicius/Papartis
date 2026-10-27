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
    
     private bool alreadyTriggered;

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

        // The torch is out.
        // Allow the monster to run again after the torch is relit.
        if (!torchScript.IsLit)
        {
            alreadyTriggered = false;
            return Status.Running;
        }

        // The torch has been relit with more than 10 seconds remaining.
        if (torchScript.SecondsLeft > 10f)
        {
            alreadyTriggered = false;
            return Status.Running;
        }

        // Prevent the monster from running repeatedly
        // while the torch remains under 10 seconds.
        if (alreadyTriggered)
        {
            return Status.Running;
        }

        // Torch has 10 seconds or less remaining.
        alreadyTriggered = true;
        return Status.Success;
    }
}