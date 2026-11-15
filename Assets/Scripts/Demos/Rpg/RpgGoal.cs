using UnityEngine;

namespace TopDownKit
{
    // The Little RPG room demo's rules for the HUD: the coin count, and "you win" once the
    // goal item is in the bag. Writes into the shared Hud.
    public class RpgGoal : MonoBehaviour
    {
        public Hud hud;
        public Hotbar hotbar;
        public Item coin;
        [Tooltip("Having this item wins the demo.")]
        public Item goal;

        public bool Won => goal != null && hotbar != null && hotbar.Inventory.Has(goal);

        void Update()
        {
            if (hud == null || hotbar == null) return;
            hud.Status = coin != null ? "Coins " + hotbar.Inventory.Count(coin) : "";
            hud.Banner = Won ? "You found the crown - you win" : "";
        }
    }
}
