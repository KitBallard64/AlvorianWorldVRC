using UdonSharp;
using UnityEngine;

/// <summary>
/// VoiceControlManager - single entry point for the Whisper / Default / Yell UI buttons.
///
/// MULTI-PLAYER DESIGN
/// -------------------
/// Every player in the instance has their own VoiceControlSystem component living on a
/// dedicated "slot" GameObject.  Each slot is owned (via Networking.SetOwner) by the
/// player occupying that slot.
///
/// Problem: Unity UI Button OnClick events must target a fixed GameObject, so you cannot
/// wire three buttons to 40 different VoiceControlSystem slots.
///
/// Solution: wire the three buttons to this manager instead.
/// SetWhisper / SetDefault / SetYell iterate all registered slots and call the matching
/// method on each one.  Inside VoiceControlSystem every handler starts with an IsOwner()
/// check, so only the slot owned by the LOCAL player does anything; all others return
/// immediately.
///
/// Result: Player A clicks Whisper  → only A's slot responds → A whispers for everyone.
///         Player B clicks Yell     → only B's slot responds → B yells for everyone.
///         Player C hears both independently and simultaneously.
///
/// SETUP
/// -----
/// 1. Create one VoiceControlSystem slot GameObject per player slot in your world
///    (e.g. 40 slots for a 40-player world).  Assign ownership of each slot to the
///    appropriate player via Networking.SetOwner in a join/slot-assignment script.
/// 2. Add this VoiceControlManager to a single GameObject in the scene.
/// 3. Populate the 'slots' array in the Inspector with every VoiceControlSystem instance.
/// 4. Wire each UI Button's OnClick to this manager:
///       Whisper button → VoiceControlManager.SetWhisper()
///       Default button → VoiceControlManager.SetDefault()
///       Yell button    → VoiceControlManager.SetYell()
/// </summary>
public class VoiceControlManager : UdonSharpBehaviour
{
    [Header("Voice Control Slots")]
    [Tooltip("Add every VoiceControlSystem slot object here (one entry per player slot).")]
    public VoiceControlSystem[] slots;

    // ------------------------------------------------------------------
    // Button handlers — wire these to the three UI buttons in the menu
    // ------------------------------------------------------------------

    /// <summary>
    /// Called by the Whisper button.  Broadcasts to all slots; only the slot
    /// owned by the local player will act on the call.
    /// </summary>
    public void SetWhisper()
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].SetWhisper();
        }
    }

    /// <summary>
    /// Called by the Default button.  Broadcasts to all slots; only the slot
    /// owned by the local player will act on the call.
    /// </summary>
    public void SetDefault()
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].SetDefault();
        }
    }

    /// <summary>
    /// Called by the Yell button.  Broadcasts to all slots; only the slot
    /// owned by the local player will act on the call.
    /// </summary>
    public void SetYell()
    {
        if (slots == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].SetYell();
        }
    }
}
