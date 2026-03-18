using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

/// <summary>
/// VoiceControlSystem - Centralized, single-instance voice controller for VRChat SDK 3.10.2.
///
/// OVERVIEW
/// --------
/// Place ONE instance of this behaviour on a single GameObject in the world.
/// All players interact with this same object — no per-player slots needed.
/// Similar pattern to PlayerMenu.cs and DMRegistry.cs.
///
/// DYNAMIC PLAYER TRACKING (DMRegistry pattern)
/// ---------------------------------------------
/// playerIds  - tracks which players are registered (by VRChat playerId)
/// voiceModes - their current voice mode (0=Whisper, 1=Default, 2=Yell)
/// Both arrays grow/shrink dynamically when players click buttons or leave.
/// Both are [UdonSynced] so all clients stay in sync.
///
/// BUTTON INTEGRATION
/// ------------------
/// Wire whisperButton, defaultButton, yellButton OnClick events directly to
/// SetWhisper(), SetDefault(), SetYell() on this component.
/// VoiceControlManager is no longer needed.
/// Active button = Green, inactive = Dark Red (local UI feedback only).
///
/// NETWORK FLOW
/// ------------
/// 1. Local player clicks a button → SetWhisper/SetDefault/SetYell called.
/// 2. Script fetches Networking.LocalPlayer fresh → gets their playerId.
/// 3. Finds or adds them to the arrays → updates their voiceMode.
/// 4. Applies voice settings immediately → updates button colors (local only).
/// 5. Calls RequestSerialization() → all other clients receive OnDeserialization.
/// 6. OnDeserialization loops through all tracked players and applies their modes.
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
    // Button references (Inspector - wire OnClick to SetWhisper/SetDefault/SetYell)
    // -----------------------------------------------------------------------
    [Header("Button References")]
    [Tooltip("Button component for Whisper mode. Wire its OnClick to SetWhisper().")]
    public Button whisperButton;

    [Tooltip("Button component for Default mode. Wire its OnClick to SetDefault().")]
    public Button defaultButton;

    [Tooltip("Button component for Yell mode. Wire its OnClick to SetYell().")]
    public Button yellButton;

    // -----------------------------------------------------------------------
    // HUD reference (drag in via Inspector — not found at runtime)
    // -----------------------------------------------------------------------
    [Header("HUD Reference")]
    [Tooltip("Drag the PublicDiceRollHUD component here. Not looked up at runtime.")]
    public PublicDiceRollHUD hud;

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
    // Button colors (local UI feedback only — not synced)
    // -----------------------------------------------------------------------
    private Color _colorActive   = new Color(0f,   0.55f, 0f,   1f); // Green
    private Color _colorInactive = new Color(0.4f, 0f,    0f,   1f); // Dark Red

    // -----------------------------------------------------------------------
    // Networked state — dynamic parallel arrays (DMRegistry pattern)
    // -----------------------------------------------------------------------
    [UdonSynced]
    private int[] playerIds = new int[0];

    [UdonSynced]
    private byte[] voiceModes = new byte[0];

    // -----------------------------------------------------------------------
    // Button handlers — wire directly to UI button OnClick events
    // -----------------------------------------------------------------------

    /// <summary>Called by the Whisper button OnClick.</summary>
    public void SetWhisper() => ApplyMode(MODE_WHISPER, "🎤 Whispering");

    /// <summary>Called by the Default button OnClick.</summary>
    public void SetDefault() => ApplyMode(MODE_DEFAULT, "🎤 Normal Voice");

    /// <summary>Called by the Yell button OnClick.</summary>
    public void SetYell() => ApplyMode(MODE_YELL, "🎤 Yelling");

    /// <summary>
    /// Common implementation for all three button handlers.
    /// Fetches LocalPlayer fresh, registers them, applies settings, and syncs.
    /// Taking ownership first ensures this client can serialize the updated arrays.
    /// </summary>
    private void ApplyMode(byte mode, string hudMessage)
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (localPlayer == null) return;

        int idx = FindOrAddPlayer(localPlayer.playerId);
        voiceModes[idx] = mode;
        ApplyVoiceSettingsToPlayer(localPlayer, mode);
        UpdateButtonColors(mode);

        Networking.SetOwner(localPlayer, gameObject);
        RequestSerialization();

        if (hud != null)
            hud.ReceiveRollMessage(hudMessage);

        if (debugLog)
            Debug.Log("[VoiceControlSystem] " + localPlayer.displayName + " set mode to " + hudMessage + ".");
    }

    // -----------------------------------------------------------------------
    // Network sync — received by every client when the arrays are serialized
    // -----------------------------------------------------------------------

    /// <summary>
    /// Loops through all tracked players and applies their current voice mode.
    /// Called on every client (except the sender) after RequestSerialization().
    /// </summary>
    public override void OnDeserialization()
    {
        for (int i = 0; i < playerIds.Length; i++)
        {
            VRCPlayerApi player = VRCPlayerApi.GetPlayerById(playerIds[i]);
            if (player == null || !player.IsValid()) continue;
            ApplyVoiceSettingsToPlayer(player, voiceModes[i]);
        }
    }

    // -----------------------------------------------------------------------
    // Player leave handling
    // -----------------------------------------------------------------------

    /// <summary>
    /// Removes the leaving player from the tracking arrays and compacts them,
    /// matching the shift pattern used in DMRegistry.
    /// Only the current owner serializes the updated arrays to all clients.
    /// </summary>
    public override void OnPlayerLeft(VRCPlayerApi player)
    {
        if (player == null) return;
        RemovePlayer(player.playerId);
        if (Networking.IsOwner(gameObject))
            RequestSerialization();
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the index of a player in the tracking arrays.
    /// If not found, adds them with DEFAULT mode and returns their new index.
    /// </summary>
    private int FindOrAddPlayer(int playerId)
    {
        for (int i = 0; i < playerIds.Length; i++)
        {
            if (playerIds[i] == playerId)
                return i;
        }

        // Not found — grow arrays by 1 and add with DEFAULT mode
        int newLength = playerIds.Length + 1;
        int[]  newPlayerIds  = new int[newLength];
        byte[] newVoiceModes = new byte[newLength];

        for (int i = 0; i < playerIds.Length; i++)
        {
            newPlayerIds[i]  = playerIds[i];
            newVoiceModes[i] = voiceModes[i];
        }

        newPlayerIds[newLength - 1]  = playerId;
        newVoiceModes[newLength - 1] = MODE_DEFAULT;

        playerIds  = newPlayerIds;
        voiceModes = newVoiceModes;

        if (debugLog)
            Debug.Log("[VoiceControlSystem] Added player ID " + playerId + " to tracking.");

        return newLength - 1;
    }

    /// <summary>
    /// Removes a player from the tracking arrays, shifting remaining entries to
    /// keep the arrays compact — same pattern as DMRegistry.RemoveTempDM().
    /// </summary>
    private void RemovePlayer(int playerId)
    {
        if (playerIds.Length == 0) return;

        int countToKeep = 0;
        for (int i = 0; i < playerIds.Length; i++)
        {
            if (playerIds[i] != playerId)
                countToKeep++;
        }

        if (countToKeep == playerIds.Length) return; // player not found

        int[]  newPlayerIds  = new int[countToKeep];
        byte[] newVoiceModes = new byte[countToKeep];
        int idx = 0;

        for (int i = 0; i < playerIds.Length; i++)
        {
            if (playerIds[i] != playerId)
            {
                newPlayerIds[idx]  = playerIds[i];
                newVoiceModes[idx] = voiceModes[i];
                idx++;
            }
        }

        playerIds  = newPlayerIds;
        voiceModes = newVoiceModes;

        if (debugLog)
            Debug.Log("[VoiceControlSystem] Removed player ID " + playerId + " from tracking.");
    }

    /// <summary>Applies voice distance and gain settings for the given mode to a player.</summary>
    private void ApplyVoiceSettingsToPlayer(VRCPlayerApi player, byte mode)
    {
        if (player == null || !player.IsValid()) return;

        switch (mode)
        {
            case MODE_WHISPER:
                player.SetVoiceDistanceNear(whisperDistanceNear);
                player.SetVoiceDistanceFar(whisperDistanceFar);
                player.SetVoiceGain(whisperGain);
                if (debugLog)
                    Debug.Log("[VoiceControlSystem] Applied Whisper to: " + player.displayName);
                break;

            case MODE_DEFAULT:
                player.SetVoiceDistanceNear(defaultDistanceNear);
                player.SetVoiceDistanceFar(defaultDistanceFar);
                player.SetVoiceGain(defaultGain);
                if (debugLog)
                    Debug.Log("[VoiceControlSystem] Applied Default to: " + player.displayName);
                break;

            case MODE_YELL:
                player.SetVoiceDistanceNear(yellDistanceNear);
                player.SetVoiceDistanceFar(yellDistanceFar);
                player.SetVoiceGain(yellGain);
                if (debugLog)
                    Debug.Log("[VoiceControlSystem] Applied Yell to: " + player.displayName);
                break;

            default:
                if (debugLog)
                    Debug.LogWarning("[VoiceControlSystem] Unknown voice mode: " + mode);
                break;
        }
    }

    /// <summary>
    /// Updates button background colors: active mode = green, others = dark red.
    /// Color changes are local only — pure UI feedback, not synced over the network.
    /// </summary>
    private void UpdateButtonColors(byte activeMode)
    {
        if (whisperButton != null)
        {
            ColorBlock cb = whisperButton.colors;
            cb.normalColor = (activeMode == MODE_WHISPER) ? _colorActive : _colorInactive;
            whisperButton.colors = cb;
        }

        if (defaultButton != null)
        {
            ColorBlock cb = defaultButton.colors;
            cb.normalColor = (activeMode == MODE_DEFAULT) ? _colorActive : _colorInactive;
            defaultButton.colors = cb;
        }

        if (yellButton != null)
        {
            ColorBlock cb = yellButton.colors;
            cb.normalColor = (activeMode == MODE_YELL) ? _colorActive : _colorInactive;
            yellButton.colors = cb;
        }
    }
}
