using UnityEngine;

namespace TopDownKit
{
    // Marks the solid colliders on this object as low: they stop walkers but bullets fly
    // over them (water, a fence, blue ground tiles). Bullets read it; walkers do not, so
    // free movement and grid steps are blocked as by any wall. Sight passes too: nothing
    // in the kit checks line of sight yet (enemies see through walls), and whatever does
    // later should skip these the same way.
    public class ShootOver : MonoBehaviour { }
}
