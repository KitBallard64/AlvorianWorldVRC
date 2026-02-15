using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class PlayerNameRegistry : UdonSharpBehaviour
{
    [Header("Settings")]
    public int maxPlayers = 64;

    [Header("Debug Output")]
    public bool debugLog = false;

    private VRCPlayerApi[] trackedPlayers;
    private string[] playerNames;
    private int playerCount = 0;

    private void Start()
    {
        trackedPlayers = new VRCPlayerApi[maxPlayers];
        playerNames = new string[maxPlayers];
    }

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        int index = GetPlayerIndex(player);
        if (index == -1 && playerCount < maxPlayers)
        {
            trackedPlayers[playerCount] = player;
            playerNames[playerCount] = player.displayName;
            playerCount++;

            if (debugLog)
                Debug.Log($"[NameRegistry] Added player: {player.displayName}");
        }
    }

    public void UpdateName(VRCPlayerApi player, string newName)
    {
        int index = GetPlayerIndex(player);
        if (index != -1)
        {
            playerNames[index] = newName;

            if (debugLog)
                Debug.Log($"[NameRegistry] Updated {player.displayName} to {newName}");
        }
    }

    public string GetDisplayName(VRCPlayerApi player)
    {
        int index = GetPlayerIndex(player);
        if (index != -1)
        {
            return playerNames[index];
        }

        return player != null ? player.displayName : "Unknown";
    }

    private int GetPlayerIndex(VRCPlayerApi player)
    {
        for (int i = 0; i < playerCount; i++)
        {
            if (trackedPlayers[i] != null && trackedPlayers[i].playerId == player.playerId)
            {
                return i;
            }
        }
        return -1;
    }
}
