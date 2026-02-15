using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class DMTeleportButton : UdonSharpBehaviour
{
    [HideInInspector] public VRCPlayerApi target;
    [HideInInspector] public VRCPlayerApi dm;
    [HideInInspector] public float offset = 1.5f;

    public void TeleportToTarget()
    {
        if (target == null || dm == null) return;

        Vector3 direction = (dm.GetPosition() - target.GetPosition()).normalized;
        Vector3 newPosition = target.GetPosition() + direction * offset;
        Quaternion newRotation = Quaternion.LookRotation(target.GetPosition() - newPosition);

        dm.TeleportTo(newPosition, newRotation);
    }
}
