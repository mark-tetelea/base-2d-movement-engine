using System;
using UnityEngine;

namespace TopDownKit
{
    [Serializable]
    public struct ItemStack
    {
        public Item item;
        [Min(0)] public int count;

        public ItemStack(Item item, int count) { this.item = item; this.count = count; }
        public bool Empty => item == null || count <= 0;
    }

    [Serializable]
    public struct InventorySettings
    {
        [Tooltip("Number of slots.")]
        [Min(1)] public int size;
        [Tooltip("The first this many slots are the hotbar (0 = no hotbar, e.g. a chest).")]
        [Min(0)] public int hotbarSlots;
        [Tooltip("Caps every item's stack size (0 = each item's own maxStack).")]
        [Min(0)] public int stackLimit;

        public InventorySettings(int size, int hotbarSlots, int stackLimit)
        {
            this.size = size;
            this.hotbarSlots = hotbarSlots;
            this.stackLimit = stackLimit;
        }
    }

    // The one inventory rule: a row of slots, each empty or holding a stack of one item.
    // The player's bag, a chest and anything else that holds items use this same class
    // (CLAUDE.md, one copy of each core rule), so a chest transfer is just Move between two.
    // Plain C#: no Unity callbacks, so the checks run it directly.
    [Serializable]
    public class Inventory
    {
        public InventorySettings settings;
        public ItemStack[] slots;

        public Inventory(InventorySettings settings)
        {
            this.settings = settings;
            slots = new ItemStack[Mathf.Max(1, settings.size)];
        }

        public int Size => slots.Length;

        // Grows or shrinks the slots to the size setting (after a change in the inspector).
        // Shrinking drops what was in the cut slots.
        public void Fit()
        {
            int n = Mathf.Max(1, settings.size);
            if (slots == null) slots = new ItemStack[n];
            else if (slots.Length != n) Array.Resize(ref slots, n);
        }

        public int StackSize(Item item)
        {
            if (item == null) return 0;
            int s = Mathf.Max(1, item.maxStack);
            return settings.stackLimit > 0 ? Mathf.Min(s, settings.stackLimit) : s;
        }

        // Adds up to count of item: first topping up stacks of the same item, then into empty
        // slots, both in slot order. Returns how many did not fit (0 = all went in).
        public int Add(Item item, int count)
        {
            if (item == null || count <= 0) return Mathf.Max(0, count);
            Fit();
            int max = StackSize(item);
            for (int i = 0; i < slots.Length && count > 0; i++)
            {
                if (slots[i].item != item || slots[i].count <= 0 || slots[i].count >= max) continue;
                int put = Mathf.Min(count, max - slots[i].count);
                slots[i].count += put;
                count -= put;
            }
            for (int i = 0; i < slots.Length && count > 0; i++)
            {
                if (!slots[i].Empty) continue;
                int put = Mathf.Min(count, max);
                slots[i] = new ItemStack(item, put);
                count -= put;
            }
            return count;
        }

        // How many of item would fit right now.
        public int RoomFor(Item item)
        {
            if (item == null) return 0;
            Fit();
            int max = StackSize(item), room = 0;
            foreach (ItemStack s in slots)
            {
                if (s.Empty) room += max;
                else if (s.item == item) room += Mathf.Max(0, max - s.count);
            }
            return room;
        }

        public int Count(Item item)
        {
            int n = 0;
            if (slots == null || item == null) return 0;
            foreach (ItemStack s in slots) if (s.item == item) n += Mathf.Max(0, s.count);
            return n;
        }

        public bool Has(Item item, int count = 1) => Count(item) >= count;

        // Takes up to count of item, from the last slots first (the hotbar keeps its stacks
        // longest). Returns how many were taken.
        public int Remove(Item item, int count)
        {
            if (item == null || count <= 0 || slots == null) return 0;
            int taken = 0;
            for (int i = slots.Length - 1; i >= 0 && taken < count; i--)
            {
                if (slots[i].item != item) continue;
                int t = Mathf.Min(count - taken, slots[i].count);
                slots[i].count -= t;
                taken += t;
                if (slots[i].count <= 0) slots[i] = default;
            }
            return taken;
        }

        // Takes up to count from one slot. Returns what was taken (empty if nothing).
        public ItemStack TakeFrom(int slot, int count)
        {
            if (slots == null || slot < 0 || slot >= slots.Length || slots[slot].Empty || count <= 0) return default;
            int t = Mathf.Min(count, slots[slot].count);
            ItemStack taken = new ItemStack(slots[slot].item, t);
            slots[slot].count -= t;
            if (slots[slot].count <= 0) slots[slot] = default;
            return taken;
        }

        // Moves one slot's stack into another inventory (a chest transfer). What does not
        // fit stays where it was. Returns how many moved.
        public static int Move(Inventory from, int slot, Inventory to)
        {
            if (from == null || to == null || from.slots == null || slot < 0 || slot >= from.slots.Length) return 0;
            ItemStack s = from.slots[slot];
            if (s.Empty) return 0;
            int left = to.Add(s.item, s.count);
            int moved = s.count - left;
            from.slots[slot].count = left;
            if (left <= 0) from.slots[slot] = default;
            return moved;
        }

        // Moves every slot it can (a "take all" button). Returns how many items moved.
        public static int MoveAll(Inventory from, Inventory to)
        {
            int moved = 0;
            if (from == null || from.slots == null) return 0;
            for (int i = 0; i < from.slots.Length; i++) moved += Move(from, i, to);
            return moved;
        }
    }
}
