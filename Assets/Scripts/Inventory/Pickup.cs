using System.Collections.Generic;
using UnityEngine;

namespace TopDownKit
{
    // Something lying on the floor. Auto: a Collector picks it up by walking near (coins,
    // ammo, health), pulled in by its magnet. Manual: an interaction target, taken with the
    // interact key (weapons, key items). What it gives: items into the Bag, health (the
    // phase 4 Health), full magazines (Gun.Refill). Any mix of the three.
    public class Pickup : Interactable
    {
        public ItemStack stack;
        [Tooltip("On: walked over (Collector). Off: taken with the interact key.")]
        public bool auto = true;
        [Tooltip("Health given (needs a Health on the collector).")]
        [Min(0)] public int heal;
        [Tooltip("Left on the floor while the collector is at full health.")]
        public bool onlyWhenHurt = true;
        [Tooltip("Refills every weapon of the collector's Gun.")]
        public bool refillAmmo;
        [Tooltip("Seconds after appearing before it can be collected (a dropped item is not picked straight back up).")]
        [Min(0f)] public float waitBeforeCollect;

        static readonly List<Pickup> pickups = new List<Pickup>();
        public static IReadOnlyList<Pickup> All => pickups;
        // Every pickup that gives something (sounds, particles). The second value is who took it.
        public static event System.Action<Pickup, GameObject> AnyCollected;

        float age;

        public bool Ready => age >= waitBeforeCollect - 1e-4f;

        void Reset() { verb = "Take"; centre = Vector2.zero; promptOffset = new Vector2(0f, 1f); }

        protected override void OnEnable() { base.OnEnable(); if (!pickups.Contains(this)) pickups.Add(this); }
        protected override void OnDisable() { base.OnDisable(); pickups.Remove(this); }

        void FixedUpdate() { age += Time.fixedDeltaTime; }

        public override bool CanUse(Interactor who) => !auto && Ready && Wants(who.gameObject);

        public override void Use(Interactor who) { Collect(who.gameObject); }

        // Whether collecting now would give the collector anything.
        public bool Wants(GameObject who)
        {
            if (who == null) return false;
            if (!stack.Empty)
            {
                Bag bag = who.GetComponent<Bag>();
                if (bag != null && bag.Inventory.RoomFor(stack.item) > 0) return true;
            }
            if (heal > 0)
            {
                Health h = who.GetComponent<Health>();
                if (h != null && (!onlyWhenHurt || h.Current < h.maxHealth)) return true;
            }
            if (refillAmmo && who.GetComponent<Gun>() != null) return true;
            return false;
        }

        // Gives what it can; the rest stays on the floor. Returns whether anything was given.
        public bool Collect(GameObject who)
        {
            if (!Ready || !Wants(who)) return false;
            if (!stack.Empty)
            {
                Bag bag = who.GetComponent<Bag>();
                if (bag != null) stack.count = bag.Inventory.Add(stack.item, stack.count);
            }
            if (heal > 0)
            {
                Health h = who.GetComponent<Health>();
                if (h != null && (!onlyWhenHurt || h.Current < h.maxHealth)) { h.Heal(heal); heal = 0; }
            }
            if (refillAmmo)
            {
                Gun g = who.GetComponent<Gun>();
                if (g != null) { g.Refill(); refillAmmo = false; }
            }
            AnyCollected?.Invoke(this, who);
            if (stack.Empty && heal <= 0 && !refillAmmo) Destroy(gameObject);
            return true;
        }

        // A manual pickup on the floor holding a stack (the Hotbar's drop).
        public static Pickup Drop(Item item, int count, Vector2 position, int sortingOrder, float waitBeforeCollect)
        {
            GameObject go = new GameObject("Dropped " + (item != null ? item.Name : "item"));
            go.transform.position = position;
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = item != null ? item.icon : null;
            sr.sortingOrder = sortingOrder;
            Pickup p = go.AddComponent<Pickup>();
            p.stack = new ItemStack(item, count);
            p.auto = false;
            p.verb = "Take";
            p.centre = Vector2.zero;
            p.promptOffset = new Vector2(0f, 1f);
            p.waitBeforeCollect = waitBeforeCollect;
            return p;
        }
    }
}
