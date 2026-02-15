using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class PlayerMenu : UdonSharpBehaviour
{
    [Header("Assign the menu GameObject (Canvas parent)")]
    public GameObject menuObject;

    [Header("Menu Placement Settings")]
    public float distanceFromHead = 1.2f;
    public float autoCloseDistance = 2.5f;
    public Vector3 menuOffset = new Vector3(0f, 0f, 0f);

    [Header("Optional Menu Animation")]
    public MenuScaler menuScaler; // Link to the MenuScaler script

    [Header("Linked Map Panel Manager")]
    
    [Header("Debug Options")]
    public KeyCode debugOpenKey = KeyCode.M; // Debug key to open menu

    [HideInInspector] public bool menuActive = false;
    private Vector3 menuSpawnPoint;

    private VRCPlayerApi localPlayer;

    void Start()
    {
        localPlayer = Networking.LocalPlayer;
        if (menuObject != null)
        {
            menuObject.SetActive(false);
        }
    }

    void Update()
    {
        if (localPlayer == null || menuObject == null) return;

        // Debug open key (for desktop testing)
        if (Input.GetKeyDown(debugOpenKey) && !menuActive)
        {
            Interact();
        }

        if (!menuActive) return;

        Vector3 playerPos = localPlayer.GetPosition();
        Vector3 checkPos = new Vector3(playerPos.x, menuSpawnPoint.y, playerPos.z); // Flatten height
        float flatDistance = Vector3.Distance(checkPos, new Vector3(menuSpawnPoint.x, menuSpawnPoint.y, menuSpawnPoint.z));

        if (flatDistance > autoCloseDistance)
        {
            if (menuScaler != null)
            {
                menuScaler.PlayClose();
            }
            else
            {
                menuObject.SetActive(false);
            }

            menuActive = false;
            Debug.LogError("Menu auto-closed due to distance.");
        }
    }

    public override void Interact()
    {
        if (localPlayer == null || menuObject == null || menuActive)
        {
            Debug.LogError("Interact blocked: either no localPlayer, no menuObject, or menu is already active.");
            return;
        }

        Vector3 headPos = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
        Quaternion headRot = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;

        Vector3 flatForward = headRot * Vector3.forward;
        flatForward.y = 0f;
        flatForward.Normalize();

        Vector3 spawnPos = headPos + (flatForward * distanceFromHead) + menuOffset;
        Quaternion facePlayer = Quaternion.LookRotation(flatForward, Vector3.up) * Quaternion.Euler(0, 180f, 0);

        menuObject.transform.SetPositionAndRotation(spawnPos, facePlayer);
        menuSpawnPoint = spawnPos;

        if (menuScaler != null)
        {
            menuScaler.PlayOpen();
        }
        else
        {
            menuObject.SetActive(true);
        }

        menuActive = true;
        Debug.LogError("Menu activated.");
    }

    public void CloseMenuFromButton()
    {
        if (menuObject == null) return;

        if (menuScaler != null)
        {
            menuScaler.PlayClose();
        }
        else
        {
            menuObject.SetActive(false);
        }

        menuActive = false;
        Debug.LogError("Menu manually closed by button.");
    }

    // ✅ Optional external access method if needed again
    public void ForceCloseMenuFromExternal()
    {
        if (menuScaler != null)
        {
            menuScaler.PlayClose();
        }
        else
        {
            if (menuObject != null)
                menuObject.SetActive(false);
        }

        menuActive = false;
    }
}
