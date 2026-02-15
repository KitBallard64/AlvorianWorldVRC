using UdonSharp;
using UnityEngine;

public class MenuCloseRelay : UdonSharpBehaviour
{
    [Header("Optional Panel Managers")]
    public MapTeleportPanelManager mapPanelManager;
    public DMTeleportPanelManager dmPanelManager;

    [Header("Player Menu View Controller")]
    public PlayerMenuController playerMenuController;

    public void OnMenuClosed()
    {
        if (mapPanelManager != null)
        {
            mapPanelManager.OnMenuClosed();
        }

        if (dmPanelManager != null)
        {
            dmPanelManager.OnMenuClosed();
        }

        if (playerMenuController != null)
        {
            playerMenuController.OnMenuClosed();
        }
    }
}
