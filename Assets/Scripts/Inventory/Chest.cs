using UnityEngine;

namespace TopDownKit
{
    // Add-on target: a chest. It is just a Bag (the same Inventory as the player's) that
    // opens the ChestScreen to move items between the two. Shows its open art once opened.
    // Spill on open: the first opening throws everything out onto the floor around it as
    // floor items taken by their own pickup mode (Pickup.Spawn); after that it opens the
    // screen as usual (empty, a place to store things).
    [RequireComponent(typeof(Bag))]
    public class Chest : Interactable
    {
        public SpriteRenderer art;
        public Sprite closedSprite;
        public Sprite openSprite;
        [Tooltip("The first opening spills the contents on the floor instead of opening the screen.")]
        public bool spillOnOpen;
        [Tooltip("Spill: how far from the chest's pivot the items land (tiles).")]
        [Min(0f)] public float spillRadius = 1.2f;
        [Tooltip("Spill: sorting order of the floor items.")]
        public int spillSortingOrder = 1;

        public bool Opened { get; private set; }
        public Bag Bag => GetComponent<Bag>();

        void Reset() { verb = "Open"; }

        public override bool CanUse(Interactor who) =>
            (spillOnOpen && !Opened) || (who.GetBag() != null && ChestScreen.Current != null);

        public override void Use(Interactor who)
        {
            bool first = !Opened;
            SetOpened(true);
            if (spillOnOpen && first) Pickup.SpillAll(Bag.Inventory, transform.position, spillRadius, spillSortingOrder);
            else ChestScreen.Current.Show(who, this);
        }

        // Shows it opened or closed (loading a save); does not open the screen.
        public void SetOpened(bool value)
        {
            Opened = value;
            Sprite s = value ? openSprite : closedSprite;
            if (art != null && s != null) art.sprite = s;
        }
    }
}
