using UdonSharp;
using UnityEngine;
using TMPro;
using VRC.SDKBase;

public class PublicDiceRollHUD : UdonSharpBehaviour
{
    public TextMeshProUGUI outputField;
    public float messageDuration = 6f;
    public int maxLines = 6;
    public float displayRange = 2f; // Distance required to see messages
    public VRCPlayerApi owner;


    private string[] messageBuffer;
    private float[] messageTimers;

    private void Start()
    {
        messageBuffer = new string[maxLines];
        messageTimers = new float[maxLines];
        UpdateDisplay();
    }

    private void Update()
    {
        bool changed = false;
        for (int i = 0; i < maxLines; i++)
        {
            if (!string.IsNullOrEmpty(messageBuffer[i]))
            {
                messageTimers[i] -= Time.deltaTime;
                if (messageTimers[i] <= 0f)
                {
                    messageBuffer[i] = "";
                    changed = true;
                }
            }
        }
        if (changed) UpdateDisplay();
    }

    public void ReceiveRollMessage(string message)
    {
        // Shift up
        for (int i = 1; i < maxLines; i++)
        {
            messageBuffer[i - 1] = messageBuffer[i];
            messageTimers[i - 1] = messageTimers[i];
        }
        Debug.LogError($"🧾 HUD received message: {message}");

        // Add new message
        messageBuffer[maxLines - 1] = message;
        messageTimers[maxLines - 1] = messageDuration;

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        string result = "";
        for (int i = 0; i < maxLines; i++)
        {
            if (!string.IsNullOrEmpty(messageBuffer[i]))
            {
                result += messageBuffer[i] + "\n";
            }
        }
        outputField.text = result;
    }
    public void OnFlyEnabled()
    {
        ReceiveRollMessage("[System] Fly Enabled");
    }

    public void OnFlyDisabled()
    {
        ReceiveRollMessage("[System] Fly Disabled");
    }


}
