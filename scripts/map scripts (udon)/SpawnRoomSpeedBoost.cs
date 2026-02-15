using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class SpawnRoomSpeedBoost : UdonSharpBehaviour
{
    [Header("Normal Speeds (Outside Spawn)")]
    public float normalWalkSpeed = 4f;
    public float normalRunSpeed = 6f;
    public float normalStrafeSpeed = 4f;

    [Header("Boosted Speeds (Inside Spawn)")]
    public float boostedWalkSpeed = 8f;
    public float boostedRunSpeed = 12f;
    public float boostedStrafeSpeed = 8f;

    private VRCPlayerApi localPlayer;
    private Collider triggerZone;

    private bool speedIsBoosted = false;

    private float initDelayTimer = 0.25f;
    private bool needsInitialCheck = true;

    private void Start()
    {
        localPlayer = Networking.LocalPlayer;
        triggerZone = GetComponent<Collider>();

        needsInitialCheck = true;
        initDelayTimer = 0.25f;
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        if (!player.isLocal) return;

        localPlayer = player;
        needsInitialCheck = true;
        initDelayTimer = 0.25f;
    }

    private void Update()
    {
        if (localPlayer == null || triggerZone == null)
            return;

        // Initial delay so the player is fully initialized
        if (needsInitialCheck)
        {
            initDelayTimer -= Time.deltaTime;
            if (initDelayTimer > 0f)
                return;

            needsInitialCheck = false;

            // Apply correct state immediately after init delay
            ApplyCorrectSpeedsForCurrentPosition(forceApply: true);
            return;
        }

        ApplyCorrectSpeedsForCurrentPosition(forceApply: false);
    }

    private void ApplyCorrectSpeedsForCurrentPosition(bool forceApply)
    {
        // If the collider is disabled, treat it as being "outside" the zone
        if (!triggerZone.enabled)
        {
            if (speedIsBoosted || forceApply)
            {
                ApplyNormalSpeeds();
                speedIsBoosted = false;
            }
            return;
        }

        bool inside = triggerZone.bounds.Contains(localPlayer.GetPosition());

        if (inside)
        {
            if (!speedIsBoosted || forceApply)
            {
                ApplyBoostSpeeds();
                speedIsBoosted = true;
            }
        }
        else
        {
            if (speedIsBoosted || forceApply)
            {
                ApplyNormalSpeeds();
                speedIsBoosted = false;
            }
        }
    }

    private void ApplyBoostSpeeds()
    {
        if (localPlayer == null) return;

        localPlayer.SetWalkSpeed(boostedWalkSpeed);
        localPlayer.SetRunSpeed(boostedRunSpeed);
        localPlayer.SetStrafeSpeed(boostedStrafeSpeed);
    }

    private void ApplyNormalSpeeds()
    {
        if (localPlayer == null) return;

        localPlayer.SetWalkSpeed(normalWalkSpeed);
        localPlayer.SetRunSpeed(normalRunSpeed);
        localPlayer.SetStrafeSpeed(normalStrafeSpeed);
    }
}
