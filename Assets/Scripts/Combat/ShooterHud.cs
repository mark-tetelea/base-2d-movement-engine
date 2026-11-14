using UnityEngine;

namespace TopDownKit
{
    // The Arena shooter demo's readout: health, weapon, ammo, and the room's waves.
    // A plain developer label (OnGUI), not kit UI - the real screens are phase 6.
    public class ShooterHud : MonoBehaviour
    {
        public Health player;
        public Gun gun;
        public CameraRig rig;

        GUIStyle style;
        RoomLock[] locks;

        // Every room with waves has been cleared.
        bool AllCleared()
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

        void OnGUI()
        {
            if (player == null) return;
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(14, Screen.height / 40) };
            style.fontSize = Mathf.Max(14, Screen.height / 40);
            string hp = "HP " + new string('#', player.Current) + new string('.', Mathf.Max(0, player.maxHealth - player.Current));
            string weapon = "";
            if (gun != null && gun.Current != null)
            {
                Weapon w = gun.Current;
                string ammo = w.magazine <= 0 ? "inf" : gun.Reloading ? "reloading" : gun.Ammo + "/" + w.magazine;
                weapon = "    " + (gun.CurrentIndex + 1) + " " + w.name + "  " + ammo;
            }
            string room = "";
            if (rig != null && rig.CurrentRoom != null)
            {
                RoomWaves waves = rig.CurrentRoom.GetComponent<RoomWaves>();
                RoomLock l = rig.CurrentRoom.GetComponent<RoomLock>();
                if (l != null && l.Cleared) room = "    room cleared";
                else if (waves != null && l != null && l.Locked) room = "    wave " + waves.WaveNumber + "/" + waves.WaveCount;
            }
            float h = style.fontSize * 1.6f;
            GUI.Label(new Rect(10, 6, Screen.width, h), hp + weapon + room, style);
            GUI.Label(new Rect(10, 6 + h, Screen.width, h), "WASD move   mouse aim   left click shoot   R reload   1-3 / wheel weapon   Space dash", style);
            if (!player.Dead && AllCleared()) GUI.Label(new Rect(Screen.width * 0.5f - 120, Screen.height * 0.25f, 600, h), "Arena cleared - you win", style);
            if (player.Dead) GUI.Label(new Rect(Screen.width * 0.5f - 80, Screen.height * 0.5f, 400, h), "You died", style);
        }
    }
}
