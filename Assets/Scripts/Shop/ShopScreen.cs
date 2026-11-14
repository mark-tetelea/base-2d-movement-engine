using UnityEngine;
using UnityEngine.UIElements;

namespace TopDownKit
{
    // The buy/sell screen of a Shop, on the kit's panel (KitUI), like the ChestScreen:
    // the stock on the left (click a line to buy one), the player's bag on the right
    // (click a slot to sell one), the interact key closes it. Every trade goes through
    // Shop.BuyLine / SellSlot.
    public class ShopScreen : MonoBehaviour, IUsePanel
    {
        static ShopScreen current;
        public static ShopScreen Current => current;

        [Tooltip("Bag slots per row on screen.")]
        [Min(1)] public int columns = 5;
        [Tooltip("Seconds a message (not enough coins...) stays.")]
        [Min(0f)] public float messageTime = 1.5f;

        Interactor who;
        Shop shop;
        string message = "";
        float messageLeft;

        public bool IsOpen => shop != null;
        public Shop Shop => shop;
        public string Message => messageLeft > 0f ? message : "";

        void OnEnable() { current = this; }
        void OnDisable() { if (current == this) current = null; }

        public void Show(Interactor interactor, Shop s)
        {
            who = interactor;
            shop = s;
            message = "";
            lineRows = null;
            if (who != null) who.OpenPanel(this);
        }

        public void UsePressed(Interactor interactor) { Close(); }

        public void Close()
        {
            shop = null;
            if (who != null) who.ClosePanel(this);
            who = null;
        }

        // Data entry points (the buttons call these; checks call them directly).
        public TradeResult Buy(int line) => Say(IsOpen ? shop.BuyLine(who.Bag.Inventory, line) : TradeResult.Nothing, true);
        public TradeResult Sell(int slot) => Say(IsOpen ? shop.SellSlot(who.Bag.Inventory, slot) : TradeResult.Nothing, false);

        TradeResult Say(TradeResult r, bool buying)
        {
            switch (r)
            {
                case TradeResult.NoMoney: message = "Not enough " + (shop.currency != null ? shop.currency.Name.ToLower() + "s" : "money"); break;
                case TradeResult.NoRoom: message = buying ? "Your bag is full" : "No room for the money"; break;
                case TradeResult.SoldOut: message = "Sold out"; break;
                case TradeResult.NotWanted: message = "The shop does not buy that"; break;
                case TradeResult.Done: message = buying ? "Bought" : "Sold"; break;
                default: message = ""; break;
            }
            messageLeft = messageTime;
            return r;
        }

        VisualElement root, stockList, bagGrid;
        Label title, money, note;
        VisualElement[] lineRows, bagSlots;

        void Build()
        {
            root = KitUI.Box(6f);
            root.name = "ShopScreen";
            var head = KitUI.Group(FlexDirection.Row);
            head.style.justifyContent = Justify.SpaceBetween;
            title = KitUI.Label("", 9, KitUI.Gold);
            money = KitUI.Label("", 9, KitUI.Gold);
            head.Add(title); head.Add(money);
            root.Add(head);

            var row = KitUI.Group(FlexDirection.Row);
            row.style.marginTop = 3;
            var left = KitUI.Group();
            left.style.width = 150;
            left.style.marginRight = 8;
            left.Add(KitUI.Label("Buy", 8, KitUI.Dim));
            stockList = KitUI.Group();
            left.Add(stockList);
            row.Add(left);
            var right = KitUI.Group();
            right.Add(KitUI.Label("Sell (you)", 8, KitUI.Dim));
            bagGrid = KitUI.Group(FlexDirection.Row);
            bagGrid.style.flexWrap = Wrap.Wrap;
            bagGrid.style.width = columns * (KitUI.SlotSize + 2);
            right.Add(bagGrid);
            row.Add(right);
            root.Add(row);

            note = KitUI.Label("", 8, KitUI.Text);
            note.style.marginTop = 3;
            root.Add(note);
            var hint = KitUI.Label("click a line to buy, a slot to sell one   [E] close", 8, KitUI.Dim);
            hint.style.marginTop = 2;
            root.Add(hint);

            var wrap = KitUI.Group();
            KitUI.Fill(wrap);
            wrap.style.alignItems = Align.Center;
            wrap.style.paddingTop = 50;
            wrap.Add(root);
            KitUI.Root.Add(wrap);
            lineRows = bagSlots = null;
        }

        void FillStock()
        {
            int n = shop.LineCount;
            if (lineRows == null || lineRows.Length != n)
            {
                stockList.Clear();
                lineRows = new VisualElement[n];
                for (int i = 0; i < n; i++)
                {
                    int k = i;
                    var line = KitUI.Group(FlexDirection.Row);
                    line.style.alignItems = Align.Center;
                    line.pickingMode = PickingMode.Position;
                    line.RegisterCallback<ClickEvent>(e => Buy(k));
                    line.Add(KitUI.Slot(null));
                    var name = KitUI.Label("", 8); name.name = "name"; name.style.flexGrow = 1f;
                    line.Add(name);
                    var price = KitUI.Label("", 8, KitUI.Gold); price.name = "price";
                    line.Add(price);
                    stockList.Add(line);
                    lineRows[i] = line;
                }
            }
            for (int i = 0; i < n; i++)
            {
                ShopStock.Line l = shop.Line(i);
                int left = shop.Left(i);
                VisualElement line = lineRows[i];
                KitUI.SetSlot(line[0], new ItemStack(l.item, left < 0 ? 1 : Mathf.Max(left, 1)), false);
                line[0].Q<Label>("count").text = left < 0 ? "" : left.ToString();
                line.Q<Label>("name").text = l.item != null ? l.item.Name : "-";
                line.Q<Label>("price").text = left == 0 ? "sold out" : l.price.ToString();
                line.style.opacity = left == 0 ? 0.5f : 1f;
            }
        }

        void FillBag(Inventory inv)
        {
            if (bagSlots == null || bagSlots.Length != inv.Size)
            {
                bagGrid.Clear();
                bagSlots = new VisualElement[inv.Size];
                for (int i = 0; i < inv.Size; i++)
                {
                    int k = i;
                    bagSlots[i] = KitUI.Slot(() => Sell(k));
                    bagGrid.Add(bagSlots[i]);
                }
            }
            for (int i = 0; i < inv.Size; i++)
            {
                ItemStack s = inv.slots[i];
                int pay = s.Empty ? 0 : shop.SellPrice(s.item);
                KitUI.SetSlot(bagSlots[i], s, false, pay > 0 && s.item != shop.currency ? pay.ToString() : "");
            }
        }

        void Update() { if (messageLeft > 0f) messageLeft -= Time.unscaledDeltaTime; }

        void LateUpdate()
        {
            if (root == null || root.panel == null) Build();   // trap 18
            bool show = IsOpen && who != null && who.Bag != null;
            root.parent.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (!show) return;
            Inventory bag = who.Bag.Inventory;
            title.text = shop.Title;
            money.text = shop.currency != null ? shop.currency.Name + "s " + bag.Count(shop.currency) : "";
            note.text = Message;
            FillStock();
            FillBag(bag);
        }

        void OnDestroy() { if (root != null && root.parent != null) root.parent.RemoveFromHierarchy(); }
    }
}
