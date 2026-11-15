using UnityEngine;

namespace TopDownKit
{
    // The Arena shooter demo's rules for the HUD: the wave in the current room, "you win"
    // once every room with waves is cleared, "you died". Writes into the shared Hud.
    public class ArenaGoal : MonoBehaviour
    {
        public Hud hud;
        public Health player;
        public CameraRig rig;

        RoomLock[] locks;

        // Every room with waves has been cleared.
        public bool AllCleared()
        {
            if (locks == null)
            {
                RoomWaves[] waves = FindObjectsByType<RoomWaves>(FindObjectsSortMode.None);
                locks = new RoomLock[waves.Length];
                for (int i = 0; i < waves.Length; i++) locks[i] = waves[i].GetComponent<RoomLock>();
            }
            if (locks.Length == 0) return false;
            foreach (RoomLock l in locks) if (l == null || !l.Cleared) return false;
            return true;
        }

        void Update()
        {
            if (hud == null || player == null) return;
            string room = "";
            if (rig != null && rig.CurrentRoom != null)
            {
                RoomWaves waves = rig.CurrentRoom.GetComponent<RoomWaves>();
                RoomLock l = rig.CurrentRoom.GetComponent<RoomLock>();
                if (l != null && l.Cleared) room = "room cleared";
                else if (waves != null && l != null && l.Locked) room = "wave " + waves.WaveNumber + "/" + waves.WaveCount;
            }
            hud.Status = room;
            hud.Banner = player.Dead ? "You died" : AllCleared() ? "Arena cleared - you win" : "";
        }
    }
}
