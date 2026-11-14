using System.Collections.Generic;
using UnityEngine;

namespace TopDownKit
{
    // Optional add-on: a floor area that changes how movers inside it move - a speed pad,
    // mud, an ice patch. Needs a trigger collider. Built the agreed way (LOG 2026-10-18):
    // the mover passes Movement.Step a copy of its settings with the speed scaled, so the
    // times stay the same and acceleration scales with the speed.
    [RequireComponent(typeof(Collider2D))]
    public class SpeedZone : MonoBehaviour
    {
        [Tooltip("Top speed is multiplied by this inside the zone. 1.5 = speed pad, 0.5 = mud.")]
        [Min(0f)] public float speedScale = 1f;

        [Tooltip("Optional: the feel used inside the zone instead of the mover's own (an ice patch).")]
        public MoveFeel feel;

        void OnTriggerEnter2D(Collider2D other)
        {
            PlayerMover m = MoverOf(other);
            if (m != null) m.EnterZone(this);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            PlayerMover m = MoverOf(other);
            if (m != null) m.ExitZone(this);
        }

        static PlayerMover MoverOf(Collider2D c)
        {
            Rigidbody2D b = c.attachedRigidbody;
            return b == null ? null : b.GetComponent<PlayerMover>();
        }

        // The settings a mover uses inside `zones` (in the order entered). The last zone
        // entered that has a feel replaces the basic settings; every zone's speedScale then
        // multiplies the speed, so mud on ice is slow ice. Missing or disabled zones count
        // for nothing.
        public static MoveSettings Apply(MoveSettings basic, List<SpeedZone> zones)
        {
            MoveSettings s = basic;
            for (int i = 0; i < zones.Count; i++)
                if (zones[i] != null && zones[i].isActiveAndEnabled && zones[i].feel != null) s = zones[i].feel.settings;
            // A zone never switches a mover between free and grid-step movement: the grid
            // settings stay the mover's own (on grid step, a zone's speedScale changes the pace).
            s.gridStep = basic.gridStep; s.stepTime = basic.stepTime;
            s.holdToWalk = basic.holdToWalk; s.eightDirections = basic.eightDirections;
            for (int i = 0; i < zones.Count; i++)
                if (zones[i] != null && zones[i].isActiveAndEnabled) s.speed *= zones[i].speedScale;
            return s;
        }
    }
}
