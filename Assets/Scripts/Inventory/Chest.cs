using UnityEngine;

namespace TopDownKit
{
    // Add-on target: a chest. It is just a Bag (the same Inventory as the player's) that
    // opens the ChestScreen to move items between the two. Shows its open art once opened.
    [RequireComponent(typeof(Bag))]
    public class Chest : Interactable
    {
        public SpriteRenderer art;
        public Sprite closedSprite;
        public Sprite openSprite;

        public bool Opened { get; private set; }
        public Bag Bag => GetComponent<Bag>();

        void Reset() { verb = "Open"; }

        public override bool CanUse(Interactor who) => who.Bag != null && ChestScreen.Current != null;

        public override void Use(Interactor who)
        {
            SetOpened(true);
            ChestScreen.Current.Show(who, this);
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
