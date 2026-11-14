using UnityEngine;

namespace TopDownKit
{
    // How the world is cut up for the camera (Docs/ROADMAP.md, section 3).
    public enum CameraLayout
    {
        Open,        // one big space, the camera follows
        RoomsSlide,  // the camera stays in the current room and slides to the next one
        RoomsFade    // crossing into another room fades out, cuts, fades in
    }

    [System.Serializable]
    public struct FollowSettings
    {
        [Tooltip("Seconds to close most of the gap to the target (0 = locked on).")]
        [Min(0f)] public float smoothTime;
        [Tooltip("Half size of the box (tiles) the target can move in before the camera follows.")]
        public Vector2 deadZone;
        [Tooltip("How far (tiles) the camera leans toward the aim direction.")]
        [Min(0f)] public float lookAhead;
    }

    // The one camera rule: plain functions of numbers, no scene reads, so the camera
    // component, editor previews and checks all use the same maths (CLAUDE.md, one copy).
    public static class CameraFollow
    {
        // Moves the anchor the least amount that puts the target back inside the dead zone.
        public static Vector2 DeadZone(Vector2 anchor, Vector2 target, Vector2 half)
        {
            Vector2 d = target - anchor;
            if (d.x > half.x) anchor.x += d.x - half.x; else if (d.x < -half.x) anchor.x += d.x + half.x;
            if (d.y > half.y) anchor.y += d.y - half.y; else if (d.y < -half.y) anchor.y += d.y + half.y;
            return anchor;
        }

        // Frame-rate independent ease: after smoothTime seconds about 63% of the gap is closed.
        public static Vector2 Smooth(Vector2 current, Vector2 goal, float smoothTime, float dt)
        {
            if (smoothTime <= 0f) return goal;
            return Vector2.Lerp(current, goal, 1f - Mathf.Exp(-dt / smoothTime));
        }

        // Keeps a view of halfView (tiles) inside the room. A room smaller than the view on
        // one axis is centred on that axis.
        public static Vector2 Clamp(Vector2 center, Rect room, Vector2 halfView)
        {
            center.x = room.width <= halfView.x * 2f ? room.center.x
                : Mathf.Clamp(center.x, room.xMin + halfView.x, room.xMax - halfView.x);
            center.y = room.height <= halfView.y * 2f ? room.center.y
                : Mathf.Clamp(center.y, room.yMin + halfView.y, room.yMax - halfView.y);
            return center;
        }

        // Rounds to whole screen pixels (pixelsPerTile, 16 in the kit) so nothing shimmers.
        public static Vector2 SnapToPixel(Vector2 p, float pixelsPerTile)
        {
            return new Vector2(Mathf.Round(p.x * pixelsPerTile) / pixelsPerTile,
                               Mathf.Round(p.y * pixelsPerTile) / pixelsPerTile);
        }
    }
}
