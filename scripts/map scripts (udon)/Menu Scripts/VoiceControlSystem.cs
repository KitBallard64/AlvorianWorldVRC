using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

/// <summary>
/// VoiceControlSystem - UdonSharp voice range/volume controller for VRChat SDK 3.10.2.
///
/// Place one instance of this behaviour on a per-player GameObject (one per player slot
/// in the world). The player who owns that object is the one whose voice is controlled.
/// Ownership transfers automatically when a player takes the slot.
///
/// Wire three UI Buttons on the player menu to call:
///   SetWhisper()  - reduces voice distance and gain
///   SetDefault()  - restores the configured default voice settings
///   SetYell()     - increases voice distance and gain
///
/// When the owning player clicks a button:
///  1. Settings are applied to the owner immediately (the owner does NOT receive OnDeserialization).
///  2. The mode is synced to all other clients via UdonSynced serialization.
///  3. Every other client applies the settings via OnDeserialization, so everyone hears the change.
///
/// Replaces PlayerVolumeController which had the following issues:
///  - Start() crashed if attachedPlayer was null (no null check before voice API calls).
///  - OnToggleChanged() never applied settings to the owner (OnDeserialization skips the owner).
///  - _volumeControlFlag / _cachedVolumeControlFlag started as 2 (Loud) while Start() applied Normal.
///  - Required a manually assigned attachedPlayer per inspector slot (fragile setup).
/// </summary>
[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class VoiceControlSystem : UdonSharpBehaviour
{
    // -----------------------------------------------------------------------
    // Voice mode identifiers
    // -----------------------------------------------------------------------
    private const byte MODE_WHISPER = 0;
    private const byte MODE_DEFAULT = 1;
    private const byte MODE_YELL    = 2;

    // -----------------------------------------------------------------------
    // Whisper settings (inspector-adjustable)
    // -----------------------------------------------------------------------
    [Header("Whisper Settings")]
    [Tooltip("Near voice distance in metres for Whisper mode.")]
    public float whisperDistanceNear = 0f;

    [Tooltip("Far voice distance in metres for Whisper mode.")]
    public float whisperDistanceFar = 5f;

    [Tooltip("Voice gain for Whisper mode (lower = quieter).")]
    public float whisperGain = 5f;

    // -----------------------------------------------------------------------
    // Default settings (inspector-adjustable - match your world's base values)
    // -----------------------------------------------------------------------
    [Header("Default Settings")]
    [Tooltip("Near voice distance restored when returning to Default mode. " +
             "Set this to match your world's base near-distance (VRChat SDK default: 0).")]
    public float defaultDistanceNear = 0f;

    [Tooltip("Far voice distance restored when returning to Default mode. " +
             "Set this to match your world's base far-distance (VRChat SDK default: 25).")]
    public float defaultDistanceFar = 25f;

    [Tooltip("Voice gain restored when returning to Default mode. " +
             "Set this to match your world's base gain (VRChat SDK default: 15).")]
    public float defaultGain = 15f;

    // -----------------------------------------------------------------------
    // Yell settings (inspector-adjustable)
    // -----------------------------------------------------------------------
    [Header("Yell Settings")]
    [Tooltip("Near voice distance in metres for Yell mode.")]
    public float yellDistanceNear = 10f;

    [Tooltip("Far voice distance in metres for Yell mode.")]
    public float yellDistanceFar = 40f;

    [Tooltip("Voice gain for Yell mode (higher = louder).")]
    public float yellGain = 25f;

    // -----------------------------------------------------------------------
    // Debug
    // -----------------------------------------------------------------------
    [Header("Debug")]
    [Tooltip("Enable to log voice mode changes to the VRChat console.")]
    public bool debugLog = false;

    // -----------------------------------------------------------------------
    // Networked state
    // Use literal 1 = MODE_DEFAULT so both fields start in sync on join.
    // -----------------------------------------------------------------------
    [UdonSynced]
    private byte _voiceMode = 1;        // 1 = MODE_DEFAULT
    private byte _cachedVoiceMode = 1;  // 1 = MODE_DEFAULT

    // -----------------------------------------------------------------------
    // Local player reference
    // -----------------------------------------------------------------------
    private VRCPlayerApi _localPlayer;

    // -----------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------
    private void Start()
    {
        _localPlayer = Networking.LocalPlayer;
    }

    // -----------------------------------------------------------------------
    // Button handlers - wire these to UnityEngine.UI.Button OnClick events
    // -----------------------------------------------------------------------

    /// <summary>Called by the Whisper button in the player menu.</summary>
    public void SetWhisper()
    {
        if (!IsOwner()) return;

        _voiceMode = MODE_WHISPER;
        RequestSerialization();
        ApplyVoiceMode(_voiceMode);

        if (debugLog)
            Debug.Log("[VoiceControlSystem] Owner set mode to Whisper.");
    }

    /// <summary>Called by the Default button in the player menu.</summary>
    public void SetDefault()
    {
        if (!IsOwner()) return;

        _voiceMode = MODE_DEFAULT;
        RequestSerialization();
        ApplyVoiceMode(_voiceMode);

        if (debugLog)
            Debug.Log("[VoiceControlSystem] Owner set mode to Default.");
    }

    /// <summary>Called by the Yell button in the player menu.</summary>
    public void SetYell()
    {
        if (!IsOwner()) return;

        _voiceMode = MODE_YELL;
        RequestSerialization();
        ApplyVoiceMode(_voiceMode);

        if (debugLog)
            Debug.Log("[VoiceControlSystem] Owner set mode to Yell.");
    }

    // -----------------------------------------------------------------------
    // Network sync - received by every client when the owner serializes
    // -----------------------------------------------------------------------
    public override void OnDeserialization()
    {
        if (_cachedVoiceMode == _voiceMode) return;

        ApplyVoiceMode(_voiceMode);
        _cachedVoiceMode = _voiceMode;
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns true if the local player is the owner of this object.
    /// Only the owner (the player whose voice this controls) should change the mode.
    /// Falls back to Networking.LocalPlayer in case Start() has not yet run.
    /// </summary>
    private bool IsOwner()
    {
        if (_localPlayer == null)
            _localPlayer = Networking.LocalPlayer;

        return _localPlayer != null && Networking.IsOwner(_localPlayer, gameObject);
    }

    /// <summary>
    /// Applies the given voice mode to the player who owns this object.
    /// Called both on the owner (immediately) and on every other client (via OnDeserialization),
    /// so all players in the instance hear the voice change.
    /// </summary>
    private void ApplyVoiceMode(byte mode)
    {
        VRCPlayerApi targetPlayer = Networking.GetOwner(gameObject);
        if (targetPlayer == null || !targetPlayer.IsValid()) return;

        switch (mode)
        {
            case MODE_WHISPER:
                targetPlayer.SetVoiceDistanceNear(whisperDistanceNear);
                targetPlayer.SetVoiceDistanceFar(whisperDistanceFar);
                targetPlayer.SetVoiceGain(whisperGain);
                if (debugLog)
                    Debug.Log("[VoiceControlSystem] Applied Whisper to: " + targetPlayer.displayName);
                break;

            case MODE_DEFAULT:
                targetPlayer.SetVoiceDistanceNear(defaultDistanceNear);
                targetPlayer.SetVoiceDistanceFar(defaultDistanceFar);
                targetPlayer.SetVoiceGain(defaultGain);
                if (debugLog)
                    Debug.Log("[VoiceControlSystem] Applied Default to: " + targetPlayer.displayName);
                break;

            case MODE_YELL:
                targetPlayer.SetVoiceDistanceNear(yellDistanceNear);
                targetPlayer.SetVoiceDistanceFar(yellDistanceFar);
                targetPlayer.SetVoiceGain(yellGain);
                if (debugLog)
                    Debug.Log("[VoiceControlSystem] Applied Yell to: " + targetPlayer.displayName);
                break;

            default:
                if (debugLog)
                    Debug.LogWarning("[VoiceControlSystem] Unknown voice mode: " + mode);
                break;
        }
    }
}
