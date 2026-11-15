using System;
using UnityEngine;

namespace TopDownKit
{
    // Add-on: when this object's Health reaches 0 it drops things from a table - a crate
    // spilling coins, an enemy leaving a heart. Each roll picks one line by weight (the
    // "nothing" weight is the chance of no drop); a line places `count` copies of its
    // prefab (a Pickup, usually), or `count` of its item as floor items that use the item's
    // pickup mode (Pickup.Spawn), scattered a little around the object.
    //
    // A destructible crate is no special class: a sprite, a solid collider with no body
    // (blocks walking, bullets and enemy paths), a Health on its own team, and this.
    [RequireComponent(typeof(Health))]
    public class Drops : MonoBehaviour, ILevelPart
    {
        [Serializable]
        public struct Line
        {
            [Tooltip("What drops (a Pickup prefab, or anything). Empty: the item below.")]
            public GameObject prefab;
            [Tooltip("Used when prefab is empty: dropped as floor items with the item's own pickup mode.")]
            public Item item;
            [Min(0f)] public float weight;
            [Min(1)] public int count;
        }

        public Line[] table = new Line[0];
        [Tooltip("Weight of dropping nothing on a roll.")]
        [Min(0f)] public float nothingWeight;
        [Tooltip("How many times the table is rolled.")]
        [Min(1)] public int rolls = 1;
        [Tooltip("Tiles: drops land up to this far from the centre.")]
        [Min(0f)] public float scatter = 0.35f;
        [Tooltip("Random seed (the generator sets one per object). 0 = by where it dies, so copies of one prefab drop differently.")]
        public int seed = 1;
        [Tooltip("Sorting order of item drops (match the floor items).")]
        public int itemSortingOrder = 1;

        Health health;

        void OnEnable()
        {
            health = GetComponent<Health>();
            health.Died += OnDied;
        }

        void OnDisable() { if (health != null) health.Died -= OnDied; }

        void OnDied(Health h)
        {
            Vector2 at = transform.position;
            int s = seed != 0 ? seed : Mathf.RoundToInt(at.x * 16f) * 73856093 ^ Mathf.RoundToInt(at.y * 16f) * 19349663;
            var random = new System.Random(s);
            for (int r = 0; r < rolls; r++)
            {
                int line = Roll(table, nothingWeight, random.NextDouble());
                if (line < 0) continue;
                Line l = table[line];
                if (l.prefab == null && l.item == null) continue;
                for (int i = 0; i < l.count; i++)
                {
                    double a = random.NextDouble() * Math.PI * 2.0, d = Math.Sqrt(random.NextDouble()) * scatter;
                    Vector2 p = at + new Vector2((float)(Math.Cos(a) * d), (float)(Math.Sin(a) * d));
                    if (l.prefab != null) Instantiate(l.prefab, new Vector3(p.x, p.y, 0f), Quaternion.identity);
                    else Pickup.Spawn(l.item, 1, p, itemSortingOrder, 0f);
                }
            }
        }

        // The line picked by a number in [0, 1), or -1 for nothing.
        public static int Roll(Line[] table, float nothingWeight, double u)
        {
            double total = nothingWeight;
            foreach (Line l in table) total += l.weight;
            if (total <= 0.0) return -1;
            double x = u * total;
            for (int i = 0; i < table.Length; i++)
            {
                if (x < table[i].weight) return i;
                x -= table[i].weight;
            }
            return -1;
        }

        // In a generated room: its own random seed.
        public void PlacedInLevel(Transform player, int levelSeed) { seed = levelSeed; }
    }
}
