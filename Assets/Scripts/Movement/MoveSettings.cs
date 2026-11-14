using System;
using UnityEngine;

namespace TopDownKit
{
    // How moving feels. A struct, so the movement rule, tests and editor previews can make
    // one without an asset, and a caller can change a copy without touching the asset.
    // All numbers are in tiles and seconds, never pixels (CLAUDE.md, Decision 3).
    [Serializable]
    public struct MoveSettings
    {
        [Tooltip("Top speed, in tiles per second.")]
        [Min(0f)] public float speed;

        [Tooltip("Seconds from standing still to top speed. 0 = instant.")]
        [Min(0f)] public float timeToFullSpeed;

        [Tooltip("Seconds from top speed to standing still after letting go. 0 = instant.")]
        [Min(0f)] public float timeToStop;

        [Tooltip("How fast a knockback push dies away, in tiles per second lost every second. "
            + "Higher = shrugs hits off sooner. 0 = pushes stop at once.")]
        [Min(0f)] public float knockbackStopRate;

        [Header("Grid step (one tile per press)")]
        [Tooltip("On: the mover goes one whole tile at a time and lands on tile centres (Pokemon style); "
            + "speed is then tiles per second while walking, and the two times above are not used. Off: free movement.")]
        public bool gridStep;

        [Tooltip("Grid step: seconds the slide from one tile to the next takes - the smoothness. "
            + "0 = jumps. Never longer than one tile at the speed above.")]
        [Min(0f)] public float stepTime;

        [Tooltip("Grid step: holding a direction keeps walking. Off: one press, one tile.")]
        public bool holdToWalk;

        [Tooltip("Grid step: diagonal steps allowed (8 directions). Off: 4 directions.")]
        public bool eightDirections;
    }
}
