using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Return : UdonSharpBehaviour
{
    [Header("Target Transform to teleport to")]
    public Transform landingPoint;

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (landingPoint == null) return;

        // Only teleport the player who entered the trigger
        if (player.isLocal)
        {
            player.TeleportTo(landingPoint.position, landingPoint.rotation);
        }
    }
}
