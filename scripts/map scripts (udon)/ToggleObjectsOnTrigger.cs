using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ToggleObjectsOnTrigger : UdonSharpBehaviour
{
    [Tooltip("Objects to toggle when the player enters/exits the trigger.")]
    public GameObject[] objectsToToggle;

    [Tooltip("If true, the objects will start disabled at runtime.")]
    public bool startDisabled = false;

    private void Start()
    {
        if (startDisabled && objectsToToggle != null)
        {
            foreach (GameObject obj in objectsToToggle)
            {
                if (obj != null)
                    obj.SetActive(false);
            }
        }
    }

    public override void OnPlayerTriggerEnter(VRCPlayerApi player)
    {
        if (!player.isLocal) return; // Local only — avoids network spam
        ToggleObjects(true);
    }

    public override void OnPlayerTriggerExit(VRCPlayerApi player)
    {
        if (!player.isLocal) return;
        ToggleObjects(false);
    }

    private void ToggleObjects(bool state)
    {
        if (objectsToToggle == null) return;

        foreach (GameObject obj in objectsToToggle)
        {
            if (obj != null)
                obj.SetActive(state);
        }
    }
}
