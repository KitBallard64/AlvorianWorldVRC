using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class ItemReturnZone : UdonSharpBehaviour
{
    [Header("Settings")]
    public Transform itemToMonitor;
    public float maxDistance = 10f;
    public float checkInterval = 1.5f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private float timer;

    private void Start()
    {
        if (itemToMonitor == null) return;

        initialPosition = itemToMonitor.position;
        initialRotation = itemToMonitor.rotation;
        timer = checkInterval;
    }

    private void Update()
    {
        if (itemToMonitor == null) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        timer = checkInterval;

        float distance = Vector3.Distance(itemToMonitor.position, initialPosition);
        if (distance > maxDistance)
        {
            // Drop the item if it's being held
            var pickup = itemToMonitor.GetComponent<VRC_Pickup>();
            if (pickup != null && pickup.IsHeld)
            {
                pickup.Drop();
            }

            // Reset position and rotation
            itemToMonitor.position = initialPosition;
            itemToMonitor.rotation = initialRotation;
        }
    }
}
