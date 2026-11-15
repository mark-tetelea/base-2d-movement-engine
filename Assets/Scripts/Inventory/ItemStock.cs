namespace TopDownKit
{
    // Items kept as counted lines outside any bag - a shop's stock. The Save add-on keeps
    // what is left of each line through this, so it needs no Shop folder. Kept by Shop.
    public interface IItemStock
    {
        int LineCount { get; }
        Item LineItem(int line);
        int Left(int line);
        void SetLeft(int line, int amount);
        int LineOf(Item item);
        // The item paid with (may be null).
        Item Currency { get; }
    }
}
