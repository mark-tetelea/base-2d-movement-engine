using UnityEngine.UIElements;

namespace TopDownKit
{
    // An item stack drawn in a kit UI slot (bag, chest and shop screens).
    public static class ItemSlots
    {
        public static void Show(VisualElement slot, ItemStack s, bool selected, string number = "")
            => KitUI.SetSlot(slot, s.Empty ? null : s.item.icon, s.Empty ? 0 : s.count, selected, number);
    }
}
