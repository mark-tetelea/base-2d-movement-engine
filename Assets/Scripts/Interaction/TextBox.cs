using UnityEngine;
using UnityEngine.UIElements;

namespace TopDownKit
{
    // Add-on: a simple text box along the bottom of the screen. Shows lines one at a
    // time; the interact key goes to the next one and closes it after the last. The
    // player's walking is paused while it is open (the Interactor does that).
    // Drawn on the kit's UI panel (KitUI).
    public class TextBox : MonoBehaviour, IUsePanel
    {
        static TextBox current;
        // The one in the scene (the last enabled), for targets that want to say something.
        public static TextBox Current => current;

        string speaker;
        string[] lines;
        int index;
        Interactor who;

        public bool IsOpen => lines != null && index < lines.Length;   // a play-mode recompile leaves an empty array
        public string Line => IsOpen ? lines[index] : null;
        public int LineIndex => index;

        void OnEnable() { current = this; }
        void OnDisable() { if (current == this) current = null; }

        public void Show(Interactor interactor, string speakerName, params string[] text)
        {
            if (text == null || text.Length == 0) return;
            speaker = speakerName;
            lines = text;
            index = 0;
            who = interactor;
            if (who != null) who.OpenPanel(this);
        }

        public void Show(Interactor interactor, DialogueLines d) { if (d != null) Show(interactor, d.speaker, d.lines); }

        public void UsePressed(Interactor interactor)
        {
            if (!IsOpen) return;
            index++;
            if (index < lines.Length) return;
            lines = null;
            if (who != null) who.ClosePanel(this);
            who = null;
        }

        VisualElement root;
        Label nameLabel, textLabel, moreLabel;

        void Build()
        {
            root = KitUI.Box(6f);
            root.name = "TextBox";
            root.style.position = Position.Absolute;
            root.style.left = 64; root.style.right = 64; root.style.bottom = 8;
            root.style.minHeight = 64;
            nameLabel = KitUI.Label("", 9, KitUI.Gold);
            textLabel = KitUI.Label("", 10);
            textLabel.style.whiteSpace = WhiteSpace.Normal;
            textLabel.style.marginTop = 2;
            textLabel.style.flexGrow = 1f;
            moreLabel = KitUI.Label("", 8, KitUI.Dim);
            moreLabel.style.unityTextAlign = TextAnchor.LowerRight;
            root.Add(nameLabel); root.Add(textLabel); root.Add(moreLabel);
            KitUI.Root.Add(root);
        }

        void LateUpdate()
        {
            if (root == null || root.panel == null) Build();   // trap 18
            root.style.display = IsOpen ? DisplayStyle.Flex : DisplayStyle.None;
            if (!IsOpen) return;
            nameLabel.text = speaker ?? "";
            nameLabel.style.display = string.IsNullOrEmpty(speaker) ? DisplayStyle.None : DisplayStyle.Flex;
            textLabel.text = lines[index];
            moreLabel.text = index < lines.Length - 1 ? "[E] next" : "[E] close";
        }

        void OnDestroy() { if (root != null) root.RemoveFromHierarchy(); }
    }
}
