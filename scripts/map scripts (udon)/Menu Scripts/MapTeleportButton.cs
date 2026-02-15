using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

public class MapTeleportButton : UdonSharpBehaviour
{
    [Header("Teleport Settings")]
    public Transform targetLocation;
    public bool forceFacingZ = true;

    [Header("Access Control")]
    public GameObject dmRegistryObject; // GameObject with DMRegistry.cs
    public GameObject spawnZoneObject;  // GameObject with SpawnRoomSpeedBoost.cs

    private DMRegistry dmRegistry;
    private SpawnRoomSpeedBoost spawnZone;
    private VRCPlayerApi localPlayer;

    void Start()
    {
        localPlayer = Networking.LocalPlayer;

        if (dmRegistryObject != null)
            dmRegistry = dmRegistryObject.GetComponent<DMRegistry>();

        if (spawnZoneObject != null)
            spawnZone = spawnZoneObject.GetComponent<SpawnRoomSpeedBoost>();
    }

    public void TeleportPlayer()
    {
        if (localPlayer == null || targetLocation == null) return;

        // DM bypass
        if (dmRegistry != null && dmRegistry.IsLocalPlayerDM())
        {
            DoTeleport();
            return;
        }

        // Spawn room access
        if (spawnZone == null || !IsInSpawnZone())
        {
            Debug.LogError("MapTeleportButton: Access denied. Not a DM or not in spawn zone.");
            return;
        }

        DoTeleport();
    }

    private bool IsInSpawnZone()
    {
        Collider zoneCollider = spawnZone.GetComponent<Collider>();
        if (zoneCollider == null) return false;

        return zoneCollider.bounds.Contains(localPlayer.GetPosition());
    }

    private void DoTeleport()
    {
        Quaternion faceDirection = forceFacingZ
            ? Quaternion.LookRotation(Vector3.forward)
            : targetLocation.rotation;

        localPlayer.TeleportTo(targetLocation.position, faceDirection);
    }
}
