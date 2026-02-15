using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

public class DMRequestBroadcaster : UdonSharpBehaviour
{
    [SerializeField] private DMRegistry dmRegistry;
    [SerializeField] private PublicDiceRollHUD diceRollHUD;

    // Called by DMRequestSender
    public void RequestDM()
    {
        VRCPlayerApi localPlayer = Networking.LocalPlayer;
        if (localPlayer == null)
        {
            Debug.LogError("[DMRequestBroadcaster] LocalPlayer is null, cannot send request.");
            return;
        }

        // Make sure this object is owned by the requester,
        // so Networking.GetOwner(this.gameObject) will be them on all clients.
        if (!Networking.IsOwner(gameObject))
        {
            Networking.SetOwner(localPlayer, gameObject);
        }

        Debug.Log("[DMRequestBroadcaster] Sending DM request from " + localPlayer.displayName);

        // Broadcast to everyone; each client will decide locally if they are a DM
        SendCustomNetworkEvent(NetworkEventTarget.All, nameof(OnDMRequestNetwork));
    }

    public void OnDMRequestNetwork()
    {
        // Who sent this? The current owner of this object.
        VRCPlayerApi sender = Networking.GetOwner(gameObject);
        string requesterName = (sender != null) ? sender.displayName : "Someone";

        Debug.Log("[DMRequestBroadcaster] Received DM request from: " + requesterName);

        // Only DMs should actually see the HUD message
        if (dmRegistry == null)
        {
            Debug.LogWarning("[DMRequestBroadcaster] DMRegistry is not assigned.");
            return;
        }

        if (!dmRegistry.IsLocalPlayerDM())
        {
            // Not a DM on this client, ignore the request
            return;
        }

        string message = requesterName + " needs a DM!";
        Debug.Log("[DMRequestBroadcaster] Showing DM request: " + message);

        if (diceRollHUD != null)
        {
            diceRollHUD.ReceiveRollMessage(message);
        }
        else
        {
            Debug.LogWarning("[DMRequestBroadcaster] diceRollHUD is not assigned.");
        }
    }
}
