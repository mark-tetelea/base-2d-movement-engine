using UnityEngine;

namespace TopDownKit
{
    // Add-on target: an NPC. Using it shows its lines in the scene's TextBox.
    public class Talker : Interactable
    {
        public DialogueLines lines;

        void Reset() { verb = "Talk"; }

        public override bool CanUse(Interactor who) => lines != null && TextBox.Current != null;

        public override void Use(Interactor who) { TextBox.Current.Show(who, lines); }
    }
}
