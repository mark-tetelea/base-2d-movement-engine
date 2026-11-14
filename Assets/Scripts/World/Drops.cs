using System;
using UnityEngine;

namespace TopDownKit
{
    // Add-on: when this object's Health reaches 0 it drops things from a table - a crate
    // spilling coins, an enemy leaving a heart. Each roll picks one line by weight (the
    // "nothing" weight is the chance of no drop); a line places `count` copies of its
    // prefab (a Pickup, usually) scattered a little around the object.
    //
    // A destructible crate is no special class: a sprite, a solid collider with no body
    // (blocks walking, bullets and enemy paths), a Health on its own team, and this.
    [RequireComponent(typeof(Health))]
    public class Drops : MonoBehaviour
    {
        [Serializable]
        public struct Line
        {
            [Tooltip("What drops (a Pickup prefab, or anything).")]
            public GameObject prefab;
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
        [Tooltip("Random seed (the generator sets one per object).")]
        public int seed = 1;

        Health health;

        void OnEnable()
        {
            health = GetComponent<Health>();
            health.Died += OnDied;
        }

        void OnDisable() { if (health != null) health.Died -= OnDied; }

        void OnDied(Health h)
        {
            var random = new System.Random(seed);
            Vector2 at = transform.position;
            for (int r = 0; r < rolls; r++)
            {
                int line = Roll(table, nothingWeight, random.NextDouble());
                if (line < 0 || table[line].prefab == null) continue;
                for (int i = 0; i < table[line].count; i++)
                {
                    double a = random.NextDouble() * Math.PI * 2.0, d = Math.Sqrt(random.NextDouble()) * scatter;
                    Vector2 p = at + new Vector2((float)(Math.Cos(a) * d), (float)(Math.Sin(a) * d));
                    Instantiate(table[line].prefab, new Vector3(p.x, p.y, 0f), Quaternion.identity);
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
    }
}
