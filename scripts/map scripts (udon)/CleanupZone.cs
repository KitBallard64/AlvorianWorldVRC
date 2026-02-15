using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class CleanupZone : UdonSharpBehaviour
{
    [Header("Settings")]
    public string matchNameFragment = "Mug"; // Items containing this will be deleted
    public Transform trashPoint; // Where discarded items are sent

    private void OnTriggerEnter(Collider other)
    {
        GameObject obj = other.gameObject;
        if (!obj.name.Contains(matchNameFragment)) return;

        VRC_Pickup pickup = obj.GetComponent<VRC_Pickup>();
        if (pickup != null && pickup.IsHeld)
        {
            pickup.Drop();
        }

        if (trashPoint != null)
        {
            obj.transform.position = trashPoint.position;
            obj.transform.rotation = trashPoint.rotation;
        }

        Debug.LogError($"CleanupZone: Sent '{obj.name}' to the void.");
    }
}
