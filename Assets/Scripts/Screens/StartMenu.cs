using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace TopDownKit
{
    // The first screen: a title and one button per demo scene (by scene name, which must
    // be in the build list). The pause menu's "Main menu" comes back here.
    public class StartMenu : MonoBehaviour
    {
        public string title = "Top-down starter kit";
        [Tooltip("Button text and scene name, in pairs.")]
        public string[] labels = { "Playground", "Arena shooter", "Little RPG room" };
        public string[] scenes = { "Playground", "ArenaShooter", "LittleRpg" };

        VisualElement root;

        public void Open(int i)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(scenes[i]);
        }

        void Build()
        {
            root = KitUI.Group();
            KitUI.Fill(root);
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.Center;
            var t = KitUI.Label(title, 16, KitUI.Gold);
            t.style.marginBottom = 12;
            root.Add(t);
            var box = KitUI.Box(8f);
            box.style.width = 140;
            for (int i = 0; i < labels.Length && i < scenes.Length; i++)
            {
                int k = i;
                box.Add(KitUI.Button(labels[i], () => Open(k), 10));
            }
            root.Add(box);
            var hint = KitUI.Label("Esc in a demo: pause, restart, back here", 8, KitUI.Dim);
            hint.style.marginTop = 8;
            root.Add(hint);
            KitUI.Root.Add(root);
        }

        void Update() { if (root == null || root.panel == null) Build(); }
        void OnDisable() { if (root != null) root.RemoveFromHierarchy(); root = null; }
    }
}
