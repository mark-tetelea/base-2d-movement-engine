using System;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownKit
{
    // Small shared promises that let one system work with another without needing its
    // folder. Each names who keeps it and who reads it; delete either side and the other
    // still compiles.

    // Health points (kept by Combat's Health). Read by the HUD bars, pickups that heal,
    // the Save add-on and the map.
    public interface IHealth
    {
        int Current { get; }
        int Max { get; }
        bool Dead { get; }
        int Team { get; }
        void Heal(int amount);
        // Sets health outright (loading a save). No hit, no events.
        void Set(int amount);
    }

    // Something that can be filled up again (Combat's Gun: every magazine). Read by pickups.
    public interface IRefill
    {
        void Refill();
    }

    // Something the map draws as a dot. Kept by Health (kind = its team) and Pickup
    // (kind = MapDots.Item); read by the Map add-on.
    public interface IMapDot
    {
        Vector2 DotPosition { get; }
        int DotKind { get; }
        bool DotShown { get; }
    }

    public static class MapDots
    {
        public const int Item = -1;
        static readonly List<IMapDot> all = new List<IMapDot>();
        public static IReadOnlyList<IMapDot> All => all;
        public static void Add(IMapDot d) { if (!all.Contains(d)) all.Add(d); }
        public static void Remove(IMapDot d) { all.Remove(d); }
    }

    // Something inside a generated room that needs to know its level (enemies chase the
    // player, guns and drop tables get their own random seed). Kept by EnemyBrain, Gun,
    // Drops, BossPhases; called by World's RoomGenerator.
    public interface ILevelPart
    {
        void PlacedInLevel(Transform player, int seed);
    }

    // The seed of a generated level: the generator writes Used, Save reads it; Save sets
    // Next before reloading so the same level is built again (0 = none / random).
    public static class LevelSeed
    {
        public static int Used;
        public static int Next;
    }

    // Asks "does the ground here hurt?" without needing the Tiles folder. Tiles' GroundMap
    // answers; Enemies' path finding asks (to walk round red tiles). Null = nothing hurts.
    public static class GroundCheck
    {
        public static Func<Vector2, bool> Hurts;
        public static bool HurtsAt(Vector2 worldPoint) => Hurts != null && Hurts(worldPoint);
    }
}
