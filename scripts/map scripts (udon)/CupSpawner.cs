using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class CupSpawner : UdonSharpBehaviour
{
    [Header("Item Pool (mugs / swords / shields)")]
    public GameObject[] mugs;

    [Header("Spawn Target")]
    public Transform spawnTarget;

    // Local index only; we don't need to sync this because the items themselves are networked
    private int currentIndex = -1;

    private VRCPlayerApi LocalPlayer
    {
        get { return Networking.LocalPlayer; }
    }

    public override void Interact()
    {
        SpawnNext();
    }

    private void SpawnNext()
    {
        if (mugs == null || mugs.Length == 0)
        {
            Debug.LogWarning("[CupSpawner] No items assigned in the pool.");
            return;
        }

        if (spawnTarget == null)
        {
            Debug.LogWarning("[CupSpawner] No spawnTarget assigned.");
            return;
        }

        VRCPlayerApi lp = LocalPlayer;
        if (lp == null)
        {
            Debug.LogWarning("[CupSpawner] LocalPlayer is null.");
            return;
        }

        // Advance index in a simple, predictable round-robin
        currentIndex++;
        if (currentIndex >= mugs.Length)
            currentIndex = 0;

        GameObject mug = mugs[currentIndex];
        if (mug == null)
        {
            Debug.LogWarning("[CupSpawner] Pool element at index " + currentIndex + " is null.");
            return;
        }

        // Take ownership of this specific item so our move is networked to others
        Networking.SetOwner(lp, mug);

        // Move the item to the spawn point
        mug.transform.SetPositionAndRotation(spawnTarget.position, spawnTarget.rotation);
    }
}
