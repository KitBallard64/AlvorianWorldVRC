using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;   // For UdonInputEventArgs if needed later

public class AvatarHeightEnforcer : UdonSharpBehaviour
{
    [Header("DM Permissions")]
    [Tooltip("DMRegistry used to determine if the local player is a DM (DMs are exempt).")]
    public DMRegistry dmRegistry;

    [Header("Height Settings")]
    [Tooltip("Maximum allowed avatar eye height in meters for non-DMs.")]
    public float maxEyeHeightMeters = 2.2f;

    [Tooltip("How often (in seconds) to re-check height as a safety net.")]
    public float recheckInterval = 5f;

    private VRCPlayerApi _localPlayer;
    private float _nextCheckTime = 0f;

    private void Start()
    {
        _localPlayer = Networking.LocalPlayer;
        // First check a little after start to let avatar init
        _nextCheckTime = Time.time + 1.0f;
    }

    private bool IsLocalDM()
    {
        if (dmRegistry == null) return false;
        return dmRegistry.IsLocalPlayerDM();
    }

    private void Update()
    {
        if (_localPlayer == null)
        {
            _localPlayer = Networking.LocalPlayer;
            if (_localPlayer == null) return;
        }

        // DMs are always exempt
        if (IsLocalDM()) return;

        if (Time.time >= _nextCheckTime)
        {
            EnforceHeightLimit();
            _nextCheckTime = Time.time + recheckInterval;
        }
    }

    public override void OnAvatarChanged(VRCPlayerApi player)
    {
        // Only care about local avatar changes
        if (player == null || !player.isLocal) return;

        // DMs are exempt
        if (IsLocalDM()) return;

        // Force an immediate check next frame
        _nextCheckTime = Time.time;
    }

    private void EnforceHeightLimit()
    {
        if (_localPlayer == null) return;

        // Safety: if dmRegistry says we're DM now, bail
        if (IsLocalDM()) return;

        // Get current avatar eye height in meters
        float eyeHeight = _localPlayer.GetAvatarEyeHeightAsMeters();

        if (eyeHeight <= 0f)
        {
            // Avatar might not be initialized yet, try again later
            _nextCheckTime = Time.time + 1.0f;
            return;
        }

        if (eyeHeight <= maxEyeHeightMeters)
        {
            // Within limit, nothing to do
            return;
        }

        // World-authoritative scaling: clamp to maxEyeHeightMeters
        // First, ensure world can control avatar scaling
        _localPlayer.SetAvatarEyeHeightMaximumByMeters(maxEyeHeightMeters);
        _localPlayer.SetAvatarEyeHeightByMeters(maxEyeHeightMeters);

        // Optional: you *could* also lower the minimum so users can't scale back up beyond the cap
        // _localPlayer.SetAvatarEyeHeightMinimumByMeters(0.5f);
    }
}
