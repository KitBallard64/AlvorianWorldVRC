using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class ModernToggleController : UdonSharpBehaviour
{
    [Header("Objects to Toggle")]
    [Tooltip("All objects considered 'Modern'. These will be enabled/disabled for everyone.")]
    public GameObject[] modernObjects;

    [Header("Optional UI")]
    [Tooltip("Optional: a TMP/Text label to show current state (e.g., Modern: ON/OFF).")]
    public TMPro.TextMeshProUGUI statusLabel;

    public string labelOnText = "Modern: ON";
    public string labelOffText = "Modern: OFF";

    [Header("State")]
    [UdonSynced] private bool modernEnabled = true;

    // --- Button entry point (call this from your DM board button) ---
    public void ToggleModern()
    {
        // Safety: ensure we own this object before changing synced vars
        var lp = Networking.LocalPlayer;
        if (lp != null && !Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(lp, gameObject);
        }

        modernEnabled = !modernEnabled;

        // Sync the new state to others (for late joiners / consistency)
        RequestSerialization();

        // Apply instantly to everyone currently in instance
        SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ApplyModernState));
    }

    // --- Applies the current synced state locally (runs on every client) ---
    public void ApplyModernState()
    {
        bool enabled = modernEnabled;

        if (modernObjects != null)
        {
            for (int i = 0; i < modernObjects.Length; i++)
            {
                GameObject obj = modernObjects[i];
                if (obj != null)
                {
                    obj.SetActive(enabled);
                }
            }
        }

        if (statusLabel != null)
        {
            statusLabel.text = enabled ? labelOnText : labelOffText;
        }
    }

    // Late joiners / state updates
    public override void OnDeserialization()
    {
        ApplyModernState();
    }

    private void Start()
    {
        // Apply whatever the current synced value is when this client starts
        ApplyModernState();
    }
}
