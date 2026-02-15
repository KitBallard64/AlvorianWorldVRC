using UdonSharp;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class DMRequestButtonUI : UdonSharpBehaviour
{
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private Button unityUIButton; // Reference to Unity UI Button
    [SerializeField] private float cooldownTime = 3f;

    private string defaultText = "Request DM";

    public void OnClickRequest()
    {
        if (buttonText != null)
            buttonText.text = "Sent!";

        if (unityUIButton != null)
            unityUIButton.interactable = false;

        SendCustomEventDelayedSeconds("ResetButton", cooldownTime);
    }

    public void ResetButton()
    {
        if (buttonText != null)
            buttonText.text = defaultText;

        if (unityUIButton != null)
            unityUIButton.interactable = true;
    }
}
