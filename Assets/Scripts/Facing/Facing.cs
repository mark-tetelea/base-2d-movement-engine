using System;
using UnityEngine;

namespace TopDownKit
{
    public enum FacingCount { Four = 4, Eight = 8 }
    public enum FaceTowards { Walk, Aim }

    [Serializable]
    public struct FacingSettings
    {
        [Tooltip("4: right, up, left, down. 8: the diagonals too.")]
        public FacingCount directions;
        [Tooltip("Walk: face where you walk. Aim: face where you aim, whichever way you walk.")]
        public FaceTowards faceTowards;

        public FacingSettings(FacingCount directions, FaceTowards faceTowards)
        {
            this.directions = directions;
            this.faceTowards = faceTowards;
        }
    }

    // The one facing rule. A facing is an index 0-7 going counter-clockwise from right:
    // 0 right, 1 up-right, 2 up, 3 up-left, 4 left, 5 down-left, 6 down, 7 down-right.
    // With 4 directions only the even ones are used. Input arrives as data (directions);
    // a zero direction keeps the current facing, so standing still keeps the last one.
    public static class Facing
    {
        public const int Right = 0, UpRight = 1, Up = 2, UpLeft = 3, Left = 4, DownLeft = 5, Down = 6, DownRight = 7;

        public static int Step(int current, Vector2 walk, Vector2 aim, FacingSettings s)
        {
            Vector2 d = s.faceTowards == FaceTowards.Aim ? aim : walk;
            return Snap(d, s.directions, current);
        }

        // Nearest facing to a direction. With 4 directions an exact diagonal (keys W+D)
        // keeps the current facing if it is one of the two nearest, else goes sideways,
        // so walking diagonally never flickers between two facings.
        public static int Snap(Vector2 d, FacingCount n, int current)
        {
            if (d.sqrMagnitude < 1e-8f) return current;
            if (n == FacingCount.Eight)
            {
                float a = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
                return ((Mathf.RoundToInt(a / 45f) % 8) + 8) % 8;
            }
            int h = d.x >= 0f ? Right : Left, v = d.y >= 0f ? Up : Down;
            float ax = Mathf.Abs(d.x), ay = Mathf.Abs(d.y);
            if (Mathf.Abs(ax - ay) <= 1e-4f * (ax + ay))
                return current == v ? v : h;
            return ax > ay ? h : v;
        }

        public static Vector2 ToVector(int facing)
        {
            float a = facing * 45f * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(a), Mathf.Sin(a));
        }

        // Facings 3, 4, 5 point left. Their mirror on the right is 1, 0, 7.
        public static bool PointsLeft(int facing) => facing >= UpLeft && facing <= DownLeft;
        public static int Mirror(int facing) => (12 - facing) % 8;
    }
}
