using UnityEngine;

namespace TopDownKit
{
    // Add-on: leaves a trail of ground behind it - each tile it walks off becomes `tile`
    // for `lastFor` seconds, fading, then plain again (GroundMap.PaintFor). The trail is an
    // ordinary GroundTile, so it does whatever that tile's rules say through the one ground
    // rule: the kit's "Trail" hurts 1 a second; Red would kill; a blocking tile would leave
    // a wall. Only plain ground takes a trail (never holes, doors or crumbling tiles).
    // It paints the tile it leaves, not the one it stands on, and its own trail never hurts
    // it (it may wander back over it).
    public class TrailMaker : MonoBehaviour
    {
        [Tooltip("The ground it leaves behind.")]
        public GroundTile tile;
        [Tooltip("Seconds each trail tile lasts.")]
        [Min(0.05f)] public float lastFor = 3f;

        Rigidbody2D body;
        bool hasLast;
        Vector3Int last;
        GroundMap lastMap;
        Health health;

        void FixedUpdate()
        {
            if (body == null) { body = GetComponent<Rigidbody2D>(); health = GetComponent<Health>(); }
            Vector2 at = body != null ? GridStep.StandPoint(body) : (Vector2)transform.position;
            GroundMap map = GroundMap.Under(at);
            if (map == null) { hasLast = false; return; }
            Vector3Int cell = map.CellAt(at);
            if (hasLast && (cell != last || map != lastMap) && lastMap != null) lastMap.PaintFor(last, tile, lastFor, health);
            hasLast = true;
            last = cell;
            lastMap = map;
        }
    }
}
