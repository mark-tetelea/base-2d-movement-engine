using System;
using UnityEngine;

namespace TopDownKit
{
    // What a shop sells: item, price and how many (an asset, so shops share or swap stock
    // with no code). The shop buys back only what is on its list.
    [CreateAssetMenu(menuName = "TopDownKit/Shop Stock", fileName = "ShopStock")]
    public class ShopStock : ScriptableObject
    {
        [Serializable]
        public struct Line
        {
            public Item item;
            [Tooltip("Price of one, in the shop's currency.")]
            [Min(0)] public int price;
            [Tooltip("How many the shop has at the start. -1 = never runs out.")]
            [Min(-1)] public int amount;
        }

        public Line[] lines = new Line[0];
    }
}
