using System.Collections.Generic;
using UnityEngine;

namespace TopDownKit
{
    // A Pickup's mode: the item's own setting, or an override for this one object.
    public enum PickupChoice { FromItem, Walk, Interact, Magnet }

    // Something lying on the floor. How it is taken is the item's pickup mode (Item.pickup),
    // or this object's override: Walk and Magnet are taken by a Collector, Interact is an
    // interaction target taken with the interact key. A pickup with no item (a heart, an
    // ammo box) and no override is Magnet. What it gives: items into the Bag, health (any
    // IHealth - Combat's Health), full magazines (any IRefill - Combat's Gun). Any mix of the three.
    //
    // Every source puts items on the floor the same way, through Spawn: the hotbar and bag
    // drop, chests that spill, the shop's floor delivery, Drops on crates and enemies, loading.
    public class Pickup : Interactable, IMapDot
    {
        public ItemStack stack;
        [Tooltip("How it is picked up. FromItem = the item's own setting (no item: Magnet).")]
        public PickupChoice mode = PickupChoice.FromItem;
        [Tooltip("Health given (needs a Health on the collector).")]
        [Min(0)] public int heal;
        [Tooltip("Left on the floor while the collector is at full health.")]
        public bool onlyWhenHurt = true;
        [Tooltip("Refills every weapon of the collector's Gun.")]
        public bool refillAmmo;
        [Tooltip("Seconds after appearing before it can be collected (a dropped item is not picked straight back up).")]
        [Min(0f)] public float waitBeforeCollect;

        static readonly List<Pickup> pickups = new List<Pickup>();
        public static new IReadOnlyList<Pickup> All => pickups;
        // Every pickup that gives something (sounds, particles). The second value is who took it.
        public static event System.Action<Pickup, GameObject> AnyCollected;

        float age;
        GameObject droppedBy;   // its Collector ignores it until it has walked out of reach once

        public bool Ready => age >= waitBeforeCollect - 1e-4f;

        public PickupMode Mode => mode != PickupChoice.FromItem ? (PickupMode)((int)mode - 1)
                                : stack.item != null ? stack.item.pickup : PickupMode.Magnet;

        // The item's magnet numbers, 0 = the collector's.
        public float MagnetReach => stack.item != null ? stack.item.magnetReach : 0f;
        public float MagnetSpeed => stack.item != null ? stack.item.magnetSpeed : 0f;

        void Reset() { verb = "Take"; centre = Vector2.zero; promptOffset = new Vector2(0f, 1f); }

        protected override void OnEnable() { base.OnEnable(); if (!pickups.Contains(this)) pickups.Add(this); MapDots.Add(this); }
        protected override void OnDisable() { base.OnDisable(); pickups.Remove(this); MapDots.Remove(this); }

        public Vector2 DotPosition => transform.position;
        public int DotKind => MapDots.Item;
        public bool DotShown => true;

        void FixedUpdate() { age += Time.fixedDeltaTime; }

        public override bool CanUse(Interactor who) => Mode == PickupMode.Interact && Ready && Wants(who.gameObject);

        public override void Use(Interactor who) { Collect(who.gameObject); }

        // Whether a Collector at `who` may take it now. The one who dropped it must first be
        // out of `reach` once, so a walk or magnet item is not taken straight back.
        public bool CollectorMay(GameObject who, float distance, float reach)
        {
            if (Mode == PickupMode.Interact || !Ready) return false;
            if (droppedBy != null && who == droppedBy)
            {
                if (distance <= reach) return false;
                droppedBy = null;
            }
            return true;
        }

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
                IHealth h = who.GetComponent<IHealth>();
                if (h != null && (!onlyWhenHurt || h.Current < h.Max)) return true;
            }
            if (refillAmmo && who.GetComponent<IRefill>() != null) return true;
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
                IHealth h = who.GetComponent<IHealth>();
                if (h != null && (!onlyWhenHurt || h.Current < h.Max)) { h.Heal(heal); heal = 0; }
            }
            if (refillAmmo)
            {
                IRefill g = who.GetComponent<IRefill>();
                if (g != null) { g.Refill(); refillAmmo = false; }
            }
            AnyCollected?.Invoke(this, who);
            Cues.Raise(FeelEvent.Pickup, transform.position, gameObject);
            if (stack.Empty && heal <= 0 && !refillAmmo) Destroy(gameObject);
            return true;
        }

        // The one way items appear on the floor: a pickup holding the stack, taken by the
        // item's pickup mode (or `choice`). `droppedBy` (may be null) cannot collect it by
        // walking or magnet until it has been out of reach once.
        public static Pickup Spawn(Item item, int count, Vector2 position, int sortingOrder, float waitBeforeCollect,
                                   GameObject droppedBy = null, PickupChoice choice = PickupChoice.FromItem)
        {
            GameObject go;
            if (item != null && item.floorPrefab != null)
            {
                // The item's own floor art (any prefab); its Pickup, if it has one, is reused.
                go = Instantiate(item.floorPrefab, position, Quaternion.identity);
            }
            else
            {
                go = new GameObject();
                go.transform.position = position;
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = item != null ? item.icon : null;
                sr.sortingOrder = sortingOrder;
            }
            go.name = (droppedBy != null ? "Dropped " : "Loose ") + (item != null ? item.Name : "item");
            Pickup p = go.GetComponent<Pickup>();
            if (p == null) p = go.AddComponent<Pickup>();
            p.stack = new ItemStack(item, count);
            p.mode = choice;
            p.verb = "Take";
            p.centre = Vector2.zero;
            p.promptOffset = new Vector2(0f, 1f);
            p.waitBeforeCollect = waitBeforeCollect;
            p.droppedBy = droppedBy;
            return p;
        }

        // Puts every stack of an inventory on the floor around `at` (a chest spilling).
        public static void SpillAll(Inventory inv, Vector2 at, float radius, int sortingOrder)
        {
            int n = 0;
            for (int i = 0; i < inv.Size; i++) if (!inv.slots[i].Empty) n++;
            int k = 0;
            for (int i = 0; i < inv.Size; i++)
            {
                ItemStack s = inv.TakeFrom(i, inv.slots[i].count);
                if (s.Empty) continue;
                float a = (k++ + 0.5f) / n * Mathf.PI * 2f - Mathf.PI * 0.5f;
                Spawn(s.item, s.count, at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, sortingOrder, 0f);
            }
        }
    }
}
