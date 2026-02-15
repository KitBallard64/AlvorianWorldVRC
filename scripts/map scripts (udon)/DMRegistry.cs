using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class DMRegistry : UdonSharpBehaviour
{
    [Header("Permanent DM Display Names")]
    public string[] dmDisplayNames;

    [Header("Temporary DM Display Names (runtime only, not saved)")]
    public string[] tempDMDisplayNames = new string[0];

    // ------------------------
    // Public API
    // ------------------------

    public bool IsLocalPlayerDM()
    {
        return IsPlayerDM(Networking.LocalPlayer);
    }

    public bool IsPlayerDM(VRCPlayerApi player)
    {
        if (player == null) return false;
        string displayName = player.displayName;

        // Check permanent list
        for (int i = 0; i < dmDisplayNames.Length; i++)
        {
            if (dmDisplayNames[i] == displayName)
                return true;
        }

        // Check temp list
        for (int i = 0; i < tempDMDisplayNames.Length; i++)
        {
            if (tempDMDisplayNames[i] == displayName)
                return true;
        }

        return false;
    }

    public bool IsTargetPlayerDM(GameObject target)
    {
        VRCPlayerApi player = Networking.GetOwner(target);
        return IsPlayerDM(player);
    }

    // ------------------------
    // TEMP DM MANAGEMENT
    // ------------------------

    // Convenience for "make myself a temp DM"
    public void AddLocalPlayerAsTempDM()
    {
        VRCPlayerApi local = Networking.LocalPlayer;
        if (local == null) return;
        AddTempDM(local.displayName);
    }

    public void AddTempDM(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName)) return;

        // Already in permanent list? nothing to do
        for (int i = 0; i < dmDisplayNames.Length; i++)
        {
            if (dmDisplayNames[i] == playerName)
                return;
        }

        // Already in temp list? nothing to do
        for (int i = 0; i < tempDMDisplayNames.Length; i++)
        {
            if (tempDMDisplayNames[i] == playerName)
                return;
        }

        // Grow temp array by 1
        string[] newList = new string[tempDMDisplayNames.Length + 1];
        for (int i = 0; i < tempDMDisplayNames.Length; i++)
        {
            newList[i] = tempDMDisplayNames[i];
        }
        newList[newList.Length - 1] = playerName;
        tempDMDisplayNames = newList;

        Debug.Log("[DMRegistry] Added TEMP DM: " + playerName);
    }

    public void RemoveTempDM(string playerName)
    {
        if (tempDMDisplayNames.Length == 0) return;

        int countToKeep = 0;
        for (int i = 0; i < tempDMDisplayNames.Length; i++)
        {
            if (tempDMDisplayNames[i] != playerName)
                countToKeep++;
        }

        if (countToKeep == tempDMDisplayNames.Length)
            return; // nothing removed

        string[] newList = new string[countToKeep];
        int idx = 0;
        for (int i = 0; i < tempDMDisplayNames.Length; i++)
        {
            if (tempDMDisplayNames[i] != playerName)
            {
                newList[idx++] = tempDMDisplayNames[i];
            }
        }

        tempDMDisplayNames = newList;

        Debug.Log("[DMRegistry] Removed TEMP DM: " + playerName);
    }
}
