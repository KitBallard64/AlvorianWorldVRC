using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class DMRequestSender : UdonSharpBehaviour
{
    [SerializeField] private DMRequestBroadcaster dmBroadcaster;

    public void SendDMRequest()
    {
        if (dmBroadcaster == null)
        {
            Debug.LogError("DMRequestSender: Broadcaster is null!");
            return;
        }

        if (Networking.LocalPlayer == null)
        {
            Debug.LogError("DMRequestSender: LocalPlayer is null!");
            return;
        }

        Debug.LogError("DMRequestSender: Button clicked by " + Networking.LocalPlayer.displayName);

        dmBroadcaster.RequestDM();
    }
}
