using UnityEngine;

namespace TopDownKit
{
    // Add-on: a simple text box along the bottom of the screen. Shows lines one at a
    // time; the interact key goes to the next one and closes it after the last. The
    // player's walking is paused while it is open (the Interactor does that).
    // Drawn with OnGUI as a placeholder; the real screens are phase 6.
    public class TextBox : MonoBehaviour, IUsePanel
    {
        static TextBox current;
        // The one in the scene (the last enabled), for targets that want to say something.
        public static TextBox Current => current;

        string speaker;
        string[] lines;
        int index;
        Interactor who;
        GUIStyle style, nameStyle;

        public bool IsOpen => lines != null;
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

        void OnGUI()
        {
            if (!IsOpen) return;
            int fs = Mathf.Max(14, Screen.height / 30);
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, wordWrap = true };
                nameStyle = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold };
            }
            style.fontSize = fs;
            nameStyle.fontSize = fs;
            float w = Screen.width * 0.8f, h = fs * 5f;
            Rect r = new Rect((Screen.width - w) * 0.5f, Screen.height - h - fs, w, h);
            GUI.Box(r, "\n" + lines[index] + "\n\n" + (index < lines.Length - 1 ? "[E] next" : "[E] close"), style);
            if (!string.IsNullOrEmpty(speaker)) GUI.Label(new Rect(r.x + fs * 0.5f, r.y + fs * 0.2f, w, fs * 1.6f), speaker, nameStyle);
        }
    }
}
