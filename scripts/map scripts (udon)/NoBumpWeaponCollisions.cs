using UdonSharp;
using UnityEngine;

public class NoBumpWeaponCollisions : UdonSharpBehaviour
{
    [Header("All weapon colliders that should ignore each other")]
    public Collider[] weaponColliders;

    void Start()
    {
        // Make all weapon colliders ignore each other
        for (int i = 0; i < weaponColliders.Length; i++)
        {
            for (int j = i + 1; j < weaponColliders.Length; j++)
            {
                if (weaponColliders[i] != null && weaponColliders[j] != null)
                    Physics.IgnoreCollision(weaponColliders[i], weaponColliders[j], true);
            }
        }
    }
}
