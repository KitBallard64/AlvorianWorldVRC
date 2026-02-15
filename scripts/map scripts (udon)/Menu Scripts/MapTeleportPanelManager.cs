using UdonSharp;
using UnityEngine;

public class MapTeleportPanelManager : UdonSharpBehaviour
{
    [Header("Panels")]
    public GameObject playerPanel;
    public GameObject mapPanel;
    [Header("Player Menu Reference")]
    public PlayerMenu playerMenuScript;

    public void OpenMapPanel()
    {
        if (playerPanel != null) playerPanel.SetActive(false);
        if (mapPanel != null) mapPanel.SetActive(true);
    }

    public void OnMenuClosed()
    {
        if (mapPanel != null && mapPanel.activeSelf)
        {
            mapPanel.SetActive(false);
        }

        if (playerPanel != null && !playerPanel.activeSelf)
        {
            playerPanel.SetActive(true);
        }

        if (playerMenuScript != null)
        {
            playerMenuScript.menuActive = false;
        }
    }


}
