using UdonSharp;
using UnityEngine;

public class PlayerMenuController : UdonSharpBehaviour
{
    [Header("Panels")]
    [Tooltip("Root object for the standard player menu view.")]
    public GameObject playerMenuPanel;

    [Tooltip("Root object for the Options menu view.")]
    public GameObject optionsMenuPanel;

    private void OnEnable()
    {
        // Every time the menu GameObject is enabled, start in Player menu view
        ShowPlayerMenu();
    }

    // Called by the "Options" button in the Player menu
    public void ShowOptionsMenu()
    {
        Debug.Log("[PlayerMenuController] ShowOptionsMenu() called");

        if (playerMenuPanel != null)
        {
            Debug.Log("[PlayerMenuController] Hiding player panel: " + playerMenuPanel.name);
            playerMenuPanel.SetActive(false);
        }

        if (optionsMenuPanel != null)
        {
            Debug.Log("[PlayerMenuController] Showing options panel: " + optionsMenuPanel.name);
            optionsMenuPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[PlayerMenuController] optionsMenuPanel is NOT assigned!");
        }
    }

    // Called by the "Back" / "Return" button in the Options menu
    public void ShowPlayerMenu()
    {
        Debug.Log("[PlayerMenuController] ShowPlayerMenu() called");

        if (optionsMenuPanel != null)
        {
            Debug.Log("[PlayerMenuController] Hiding options panel: " + optionsMenuPanel.name);
            optionsMenuPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[PlayerMenuController] optionsMenuPanel is NOT assigned!");
        }

        if (playerMenuPanel != null)
        {
            Debug.Log("[PlayerMenuController] Showing player panel: " + playerMenuPanel.name);
            playerMenuPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[PlayerMenuController] playerMenuPanel is NOT assigned!");
        }
    }

    // Called when the *entire* menu is closed (walk away / close button)
    public void OnMenuClosed()
    {
        // Always reset back to the Player view
        ShowPlayerMenu();
    }
}
