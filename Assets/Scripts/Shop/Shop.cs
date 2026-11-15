using System;
using UnityEngine;

namespace TopDownKit
{
    public enum TradeResult { Done, NoMoney, NoRoom, SoldOut, NotWanted, Nothing }

    // Where a bought item goes: straight into the bag, or onto the floor by the shop as a
    // floor item taken by its own pickup mode (Pickup.Spawn).
    public enum ShopDelivery { IntoBag, OnFloor }

    // Add-on target: a shop. Using it opens the ShopScreen (buy from the stock, sell from
    // the bag). Money is just an item (the currency, coins) in the same Bag, and every
    // trade goes through the one inventory rule: Buy and Sell try the whole trade on a
    // copy of the bag first and only keep it if all of it fits, so a trade never half
    // happens. Sell price = the stock price x sellRate, rounded down.
    public class Shop : Interactable
    {
        public ShopStock stock;
        [Tooltip("The item paid with (Coin).")]
        public Item currency;
        [Tooltip("Sell price as a part of the buy price (0.5 = half). 0 = the shop does not buy.")]
        [Range(0f, 1f)] public float sellRate = 0.5f;
        [Tooltip("Shown at the top of the shop screen. Empty = the object's name.")]
        public string title = "";
        [Tooltip("IntoBag: a bought item goes straight into the bag (no room = no sale). OnFloor: it lands by the shop and is picked up by its own pickup mode.")]
        public ShopDelivery delivery = ShopDelivery.IntoBag;
        [Tooltip("OnFloor: where bought items land, from the shop's pivot (tiles).")]
        public Vector2 deliverAt = new Vector2(0f, -1.5f);
        [Tooltip("OnFloor: sorting order of the floor items.")]
        public int deliverSortingOrder = 1;

        int[] left;

        // Every trade that happens (sounds). True = the player bought.
        public static event Action<Shop, bool> AnyTrade;

        public string Title => string.IsNullOrEmpty(title) ? name : title;
        public int LineCount => stock != null && stock.lines != null ? stock.lines.Length : 0;
        public ShopStock.Line Line(int i) => stock.lines[i];

        void Reset() { verb = "Shop"; }

        // How many of line i are left (-1 = endless).
        public int Left(int i)
        {
            Fit();
            return i >= 0 && i < left.Length ? left[i] : 0;
        }

        // Sets how many are left (loading a save). Endless lines stay endless.
        public void SetLeft(int i, int amount)
        {
            Fit();
            if (i >= 0 && i < left.Length && left[i] >= 0) left[i] = Mathf.Max(0, amount);
        }

        void Fit()
        {
            int n = LineCount;
            if (left != null && left.Length == n) return;
            left = new int[n];
            for (int i = 0; i < n; i++) left[i] = stock.lines[i].amount;
        }

        public int LineOf(Item item)
        {
            for (int i = 0; i < LineCount; i++) if (stock.lines[i].item == item && item != null) return i;
            return -1;
        }

        // What the shop pays for one (0 = it does not buy it).
        public int SellPrice(Item item)
        {
            int i = LineOf(item);
            return i < 0 ? 0 : Mathf.FloorToInt(stock.lines[i].price * sellRate + 1e-4f);
        }

        public override bool CanUse(Interactor who) => who.Bag != null && ShopScreen.Current != null && stock != null;

        public override void Use(Interactor who) { ShopScreen.Current.Show(who, this); }

        // Buys one of line i into bag.
        public TradeResult BuyLine(Inventory bag, int i)
        {
            if (bag == null || i < 0 || i >= LineCount) return TradeResult.Nothing;
            if (Left(i) == 0) return TradeResult.SoldOut;
            ShopStock.Line l = stock.lines[i];
            TradeResult r = delivery == ShopDelivery.OnFloor ? Pay(bag, currency, l.item, l.price) : Buy(bag, currency, l.item, l.price);
            if (r != TradeResult.Done) return r;
            if (delivery == ShopDelivery.OnFloor)
                Pickup.Spawn(l.item, 1, (Vector2)transform.position + deliverAt, deliverSortingOrder, 0f);
            if (left[i] > 0) left[i]--;
            AnyTrade?.Invoke(this, true);
            return r;
        }

        // Sells one from the bag's slot.
        public TradeResult SellSlot(Inventory bag, int slot)
        {
            if (bag == null || slot < 0 || slot >= bag.Size || bag.slots[slot].Empty) return TradeResult.Nothing;
            Item item = bag.slots[slot].item;
            int i = LineOf(item);
            int price = SellPrice(item);
            if (i < 0 || item == currency || price <= 0) return TradeResult.NotWanted;
            TradeResult r = Sell(bag, slot, currency, price);
            if (r != TradeResult.Done) return r;
            if (left[i] >= 0) left[i]++;
            AnyTrade?.Invoke(this, false);
            return r;
        }

        // ---- the trade rule (plain data, no shop needed) ----

        // Pays price of currency and puts one item in, or changes nothing.
        public static TradeResult Buy(Inventory bag, Item currency, Item item, int price)
        {
            if (bag == null || item == null) return TradeResult.Nothing;
            if (price > 0 && (currency == null || !bag.Has(currency, price))) return TradeResult.NoMoney;
            Inventory trial = Copy(bag);
            if (price > 0) trial.Remove(currency, price);
            if (trial.Add(item, 1) > 0) return TradeResult.NoRoom;
            Keep(trial, bag);
            return TradeResult.Done;
        }

        // Pays price of currency for an item delivered elsewhere (onto the floor), or changes nothing.
        public static TradeResult Pay(Inventory bag, Item currency, Item item, int price)
        {
            if (bag == null || item == null) return TradeResult.Nothing;
            if (price > 0 && (currency == null || !bag.Has(currency, price))) return TradeResult.NoMoney;
            if (price > 0) bag.Remove(currency, price);
            return TradeResult.Done;
        }

        // Takes one item from the slot and puts price of currency in, or changes nothing.
        public static TradeResult Sell(Inventory bag, int slot, Item currency, int price)
        {
            if (bag == null || slot < 0 || slot >= bag.Size || bag.slots[slot].Empty) return TradeResult.Nothing;
            if (currency == null) return TradeResult.NotWanted;
            Inventory trial = Copy(bag);
            trial.TakeFrom(slot, 1);
            if (price > 0 && trial.Add(currency, price) > 0) return TradeResult.NoRoom;
            Keep(trial, bag);
            return TradeResult.Done;
        }

        static Inventory Copy(Inventory bag)
        {
            bag.Fit();
            var c = new Inventory(bag.settings);
            c.slots = (ItemStack[])bag.slots.Clone();
            return c;
        }

        static void Keep(Inventory trial, Inventory bag) { Array.Copy(trial.slots, bag.slots, bag.slots.Length); }
    }
}
