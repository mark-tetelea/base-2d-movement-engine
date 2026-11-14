using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TopDownKit
{
    // What a piece of ground does to whoever is on it. The same shape for every tile and
    // every state of a tile (normal, broken, closed), so one rule (Ground) reads them all.
    [Serializable]
    public struct GroundRules
    {
        [Tooltip("Walkers cannot step here - free movement and grid steps alike.")]
        public bool blocksWalking;
        [Tooltip("With blocksWalking: bullets stop here too (like a wall). Off: bullets and sight pass over it.")]
        public bool blocksBullets;
        [Tooltip("Whoever stands here dies (a dodge roll that protects from damage protects here too).")]
        public bool kills;
        [Tooltip("When it does not kill: damage dealt once a second to whoever stands here, the first on stepping in. 0 = none.")]
        [Min(0)] public int damagePerSecond;
    }

    public enum GroundState { Normal, Warning, Broken, Closed }

    // One kind of ground as an asset: its art and what it does. Painted on a tilemap that
    // has a GroundMap. A game adds a new kind by making a new asset (Create > Top-Down Kit >
    // Ground Tile), not by writing code: normal, deadly (red), low barrier (blue),
    // crumbling and door ground are all this one class with different settings.
    [CreateAssetMenu(menuName = "Top-Down Kit/Ground Tile", fileName = "GroundTile")]
    public class GroundTile : TileBase
    {
        public Sprite sprite;
        public Color color = Color.white;
        public GroundRules rules;

        [Header("Crumbling (break after 0 = never)")]
        [Tooltip("Seconds after someone steps on it until it breaks. 0 = it never crumbles.")]
        [Min(0f)] public float breakAfter;
        [Tooltip("Seconds it stays broken before it comes back. 0 = it stays broken.")]
        [Min(0f)] public float backAfter;
        [Tooltip("Shown between the step and the break (the warning).")]
        public Sprite warningSprite;
        public Sprite brokenSprite;
        [Tooltip("While broken: a hole that kills, or one that blocks walking.")]
        public GroundRules whenBroken = new GroundRules { kills = true };

        [Header("Door ground (opened and closed by a TileDoor)")]
        public bool isDoor;
        public Sprite closedSprite;
        public GroundRules whenClosed = new GroundRules { blocksWalking = true, blocksBullets = true };

        public bool Crumbles => breakAfter > 0f;

        public GroundRules RulesIn(GroundState s) =>
            s == GroundState.Broken ? whenBroken : s == GroundState.Closed ? whenClosed : rules;

        public Sprite SpriteIn(GroundState s)
        {
            Sprite x = s == GroundState.Warning ? warningSprite : s == GroundState.Broken ? brokenSprite
                : s == GroundState.Closed ? closedSprite : null;
            return x != null ? x : sprite;
        }

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData data)
        {
            GroundState s = GroundMap.StateAt(tilemap.GetComponent<Tilemap>(), position);
            data.sprite = SpriteIn(s);
            data.color = color;
            data.transform = Matrix4x4.identity;
            data.flags = TileFlags.LockColor | TileFlags.LockTransform;
            data.colliderType = Tile.ColliderType.None;   // GroundMap makes the colliders
        }
    }
}
