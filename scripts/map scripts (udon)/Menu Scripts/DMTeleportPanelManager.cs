using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRC.SDKBase;

public class DMTeleportPanelManager : UdonSharpBehaviour
{
    [Header("Panels")]
    public GameObject playerPanel;
    public GameObject dmPanel;

    [Header("DM Button")]
    public GameObject dmButton;

    [Header("Player List UI")]
    public GameObject scrollContent;
    public GameObject buttonPrefab;

    [Header("Teleport Settings")]
    public float offsetDistance = 1.5f;

    [Header("DM Registry Reference")]
    public DMRegistry registry;

    private VRCPlayerApi localPlayer;

    void Start()
    {
        localPlayer = Networking.LocalPlayer;

        // Use the shared helper so keypad can call the same logic
        RefreshDMButton();

        if (dmPanel != null)
            dmPanel.SetActive(false);
    }

    public void RefreshDMButton()
    {
        if (dmButton == null)
            return;

        if (registry != null && registry.IsLocalPlayerDM())
        {
            dmButton.SetActive(true);
        }
        else
        {
            dmButton.SetActive(false);
        }
    }

    public void OpenDMPanel()
    {
        if (playerPanel != null) playerPanel.SetActive(false);
        if (dmPanel != null) dmPanel.SetActive(true);

        if (scrollContent != null)
        {
            foreach (Transform child in scrollContent.transform)
            {
                GameObject.Destroy(child.gameObject);
            }
        }

        VRCPlayerApi[] allPlayers = new VRCPlayerApi[VRCPlayerApi.GetPlayerCount()];
        VRCPlayerApi.GetPlayers(allPlayers);

        Debug.LogError("Detected " + allPlayers.Length + " players.");

        foreach (VRCPlayerApi player in allPlayers)
        {
            if (player == null) continue;
            if (player == localPlayer) continue;

            if (buttonPrefab == null || scrollContent == null)
                continue;

            GameObject newButton = GameObject.Instantiate(buttonPrefab, scrollContent.transform);
            newButton.transform.localScale = Vector3.one;
            newButton.SetActive(true);

            TextMeshProUGUI label = newButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = player.displayName;
                Debug.LogError("Created button for player: " + player.displayName);
            }
            else
            {
                Debug.LogError("⚠ TMP label missing!");
            }

            DMTeleportButton teleportHandler = newButton.GetComponent<DMTeleportButton>();
            if (teleportHandler != null)
            {
                teleportHandler.target = player;
                teleportHandler.dm = localPlayer;
                teleportHandler.offset = offsetDistance;
            }
        }
    }

    public void OnMenuClosed()
    {
        if (dmPanel != null && dmPanel.activeSelf)
        {
            dmPanel.SetActive(false);
            if (playerPanel != null) playerPanel.SetActive(true);
        }
    }
}
