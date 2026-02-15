using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class MenuScaler : UdonSharpBehaviour
{
    public float openDuration = 0.25f;
    public float closeSquishDuration = 0.35f;
    public float closeDotDuration = 0.45f;

    [Header("Optional Callbacks")]
    public UdonSharpBehaviour closeCallbackTarget; // <-- NEW
    public string closeCallbackEvent = "OnMenuClosed"; // <-- NEW

    private Vector3 originalScale;
    private float elapsed = 0f;
    private bool isOpening = false;
    private bool isSquishing = false;
    private bool isDotting = false;

    void Start()
    {
        originalScale = transform.localScale;
        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        if (isOpening)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / openDuration);
            transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, t);

            if (t >= 1f)
                isOpening = false;
        }
        else if (isSquishing)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / closeSquishDuration);
            Vector3 squishScale = Vector3.Lerp(originalScale, new Vector3(originalScale.x, 0f, originalScale.z), t);
            transform.localScale = squishScale;

            if (t >= 1f)
            {
                isSquishing = false;
                isDotting = true;
                elapsed = 0f;
            }
        }
        else if (isDotting)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / closeDotDuration);
            Vector3 dotScale = Vector3.Lerp(new Vector3(originalScale.x, 0f, originalScale.z), Vector3.zero, t);
            transform.localScale = dotScale;

            if (t >= 1f)
            {
                isDotting = false;
                gameObject.SetActive(false);

                // Trigger callback
                if (closeCallbackTarget != null)
                {
                    closeCallbackTarget.SendCustomEvent(closeCallbackEvent); // <-- NEW
                }
            }
        }
    }

    public void PlayOpen()
    {
        elapsed = 0f;
        transform.localScale = Vector3.zero;
        isOpening = true;
        isSquishing = false;
        isDotting = false;
        gameObject.SetActive(true);
    }

    public void PlayClose()
    {
        elapsed = 0f;
        isSquishing = true;
        isOpening = false;
        isDotting = false;
    }
}
