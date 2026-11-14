using UnityEngine;

namespace TopDownKit
{
    // The Dungeon demo's rules for the HUD: rooms cleared, coins, the layout's seed (so a
    // layout can be reported and replayed), and "you win" once the end room is cleared.
    // Writes into the shared Hud.
    public class DungeonGoal : MonoBehaviour
    {
        public Hud hud;
        public Health player;
        public RoomGenerator generator;
        [Tooltip("Coins are counted in this bag (optional).")]
        public Bag bag;
        public Item coin;

        public bool Won
        {
            get
            {
                if (generator == null || generator.EndRoom == null) return false;
                RoomLock l = generator.EndRoom.GetComponent<RoomLock>();
                return l != null && l.Cleared;
            }
        }

        void Update()
        {
            if (hud == null || generator == null) return;
            int locks = 0, cleared = 0;
            foreach (RoomPiece r in generator.Rooms)
            {
                if (r == null) continue;
                RoomLock l = r.GetComponent<RoomLock>();
                if (l == null) continue;
                locks++;
                if (l.Cleared) cleared++;
            }
            string s = "Rooms " + cleared + "/" + locks;
            if (bag != null && coin != null) s += "   Coins " + bag.Inventory.Count(coin);
            s += "   Seed " + generator.UsedSeed;
            hud.Status = s;
            hud.Banner = player != null && player.Dead ? "You died" : Won ? "Dungeon cleared - you win" : "";
        }
    }
}
