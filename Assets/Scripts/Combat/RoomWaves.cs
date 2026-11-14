using UnityEngine;

namespace TopDownKit
{
    // Add-on to a locking room: once the doors shut, the waves come one after another;
    // when the last wave's enemies are all dead the room is cleared (RoomLock.Clear opens
    // the doors). A wave is a switched-off object whose children are the enemies; it is
    // switched on when its turn comes.
    [RequireComponent(typeof(RoomLock))]
    public class RoomWaves : MonoBehaviour
    {
        [Tooltip("Switched-off objects, one per wave, holding that wave's enemies.")]
        public GameObject[] waves;

        RoomLock roomLock;
        int index = -1;

        public int WaveNumber => index + 1;
        public int WaveCount => waves == null ? 0 : waves.Length;

        void Start()
        {
            if (waves != null) foreach (GameObject w in waves) if (w != null) w.SetActive(false);
        }

        void FixedUpdate()
        {
            if (roomLock == null) roomLock = GetComponent<RoomLock>();
            if (roomLock.Cleared || !roomLock.Locked) return;
            if (index >= 0 && Living(waves[index]) > 0) return;
            index++;
            if (waves == null || index >= waves.Length) { roomLock.Clear(); return; }
            if (waves[index] != null) waves[index].SetActive(true);
        }

        // Enemies of a wave still alive (dead ones are destroyed, or report Dead).
        public static int Living(GameObject wave)
        {
            if (wave == null) return 0;
            int n = 0;
            Transform t = wave.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                Health h = t.GetChild(i).GetComponent<Health>();
                if (h != null && !h.Dead) n++;
            }
            return n;
        }
    }
}
