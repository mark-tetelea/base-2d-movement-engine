using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownKit.TestArena
{
    // Test arena only, not part of the kit: number keys 1-9 give the player feel 1-9 from
    // the list, and the top-left corner names the feel in use, so feels can be compared
    // by playing within seconds of each other.
    public class FeelKeys : MonoBehaviour
    {
        public PlayerMover player;
        public MoveFeel[] feels;

        void Update()
        {
            Keyboard k = Keyboard.current;
            if (k == null || player == null || feels == null) return;
            for (int i = 0; i < feels.Length && i < 9; i++)
                if (k[Key.Digit1 + i].wasPressedThisFrame && feels[i] != null) player.feel = feels[i];
        }

        void OnGUI()
        {
            if (player == null || player.feel == null) return;
            MoveSettings s = player.CurrentSettings;
            string list = "";
            for (int i = 0; i < feels.Length && i < 9; i++)
                if (feels[i] != null) list += "  " + (i + 1) + " " + feels[i].name;
            GUI.Label(new Rect(8, 8, 1200, 24),
                player.feel.name + " (now " + s.speed + " tiles/s, " + s.timeToFullSpeed + " s to full speed, "
                + s.timeToStop + " s to stop)    Keys:" + list + "   Space dash   K knockback from mouse");
        }
    }
}
