using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Teleport : UdonSharpBehaviour
{
    [Header("Target location to teleport the player")]
    public Transform landingPoint;

    [Header("Audio source to play immediately on click")]
    public AudioSource teleportSound;

    private bool isTeleporting = false;

    public override void Interact()
    {
        if (landingPoint == null || isTeleporting) return;

        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (localPlayer == null || !localPlayer.isLocal) return;

        isTeleporting = true;

        // Play sound immediately
        if (teleportSound != null)
        {
            teleportSound.Play();
        }

        // Schedule the actual teleport in 3 seconds
        SendCustomEventDelayedSeconds(nameof(DoTeleport), 3f);
    }

    public void DoTeleport()
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;

        if (localPlayer != null && landingPoint != null)
        {
            localPlayer.TeleportTo(landingPoint.position, landingPoint.rotation);
        }

        // Optional: allow reuse
        isTeleporting = false;
    }
}
