using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using TMPro;

public class PublicDiceRollManager : UdonSharpBehaviour
{
    [Header("Character Name Input (Optional)")]
    public TMP_InputField characterNameInput;

    [Header("Roll Settings")]
    public float rollCooldown = 2f;
    public DiceProximityDetector[] registeredDetectors = new DiceProximityDetector[16];

    [UdonSynced] private string syncedRollMessage = "";
    [UdonSynced] private Vector3 syncedRollOrigin = Vector3.zero;

    private float lastRollTime = -999f;
    private int detectorCount = 0;
    private string lastMessageSent = "";

    public void RegisterDetector(DiceProximityDetector detector)
    {
        if (detectorCount >= registeredDetectors.Length) return;
        registeredDetectors[detectorCount++] = detector;
    }

    private string GetPlayerName()
    {
        if (characterNameInput != null)
        {
            string typed = characterNameInput.text;
            if (!string.IsNullOrWhiteSpace(typed))
            {
                return typed;
            }
        }

        VRCPlayerApi player = Networking.LocalPlayer;
        return player != null ? player.displayName : "Player";
    }

    public void RollD20()
    {
        if (Time.time - lastRollTime < rollCooldown) return;

        Networking.SetOwner(Networking.LocalPlayer, gameObject);

        string rollerName = GetPlayerName();
        int result = Random.Range(1, 21);
        syncedRollMessage = $"{rollerName} rolled a d20: {result}";
        syncedRollOrigin = Networking.LocalPlayer.GetPosition();

        RequestSerialization();

        // Show immediately for local player only
        ShowRollMessage();

        lastRollTime = Time.time;
    }

    public void Roll2D20()
    {
        if (Time.time - lastRollTime < rollCooldown) return;

        Networking.SetOwner(Networking.LocalPlayer, gameObject);

        string rollerName = GetPlayerName();
        int result1 = Random.Range(1, 21);
        int result2 = Random.Range(1, 21);
        syncedRollMessage = $"{rollerName} rolled 2d20: {result1}, {result2}";
        syncedRollOrigin = Networking.LocalPlayer.GetPosition();

        RequestSerialization();

        // Show immediately for local player only
        ShowRollMessage();

        lastRollTime = Time.time;
    }

    public override void OnDeserialization()
    {
        // All other players wait 1 frame, then fire
        if (!Networking.IsOwner(gameObject))
        {
            SendCustomEventDelayedFrames("DeferredShowRollMessage", 1);
        }
    }

    public void DeferredShowRollMessage()
    {
        ShowRollMessage();
    }

    public void ShowRollMessage()
    {
        if (syncedRollMessage == lastMessageSent) return;

        lastMessageSent = syncedRollMessage;

        for (int i = 0; i < detectorCount; i++)
        {
            DiceProximityDetector detector = registeredDetectors[i];
            if (detector != null)
            {
                detector.TryDisplayRoll(syncedRollOrigin, syncedRollMessage);
            }
        }
    }
}
