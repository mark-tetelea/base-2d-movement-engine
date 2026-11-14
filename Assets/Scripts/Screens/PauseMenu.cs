using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace TopDownKit
{
    // Esc pauses: Time.timeScale = 0 (the fixed step stops, so all gameplay stops), and a
    // menu offers Resume, Restart and Main menu. Keyboard readers check IsPaused so a key
    // pressed while paused is not kept for later. Only this menu runs while paused.
    public class PauseMenu : MonoBehaviour
    {
        [Tooltip("Build index of the start menu scene. -1 hides the Main menu button.")]
        public int menuScene = 0;

        static PauseMenu current;
        public static bool IsPaused => current != null && current.open;

        bool open;
        VisualElement root;

        void OnEnable() { current = this; }
        void OnDisable()
        {
            if (open) Time.timeScale = 1f;
            open = false;
            if (current == this) current = null;
            if (root != null) root.RemoveFromHierarchy();
            root = null;
        }

        void Build()
        {
            root = KitUI.Group();
            KitUI.Fill(root);
            root.name = "Pause";
            root.pickingMode = PickingMode.Position;   // blocks clicks to the game while open
            root.style.backgroundColor = new Color(0f, 0f, 0f, 0.5f);
            root.style.alignItems = Align.Center;
            root.style.justifyContent = Justify.Center;
            var box = KitUI.Box(8f);
            box.style.width = 120;
            var title = KitUI.Label("Paused", 12, KitUI.Gold);
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.marginBottom = 4;
            box.Add(title);
            box.Add(KitUI.Button("Resume", Resume));
            box.Add(KitUI.Button("Restart", Restart));
            if (menuScene >= 0 && menuScene != SceneManager.GetActiveScene().buildIndex)
                box.Add(KitUI.Button("Main menu", MainMenu));
            root.Add(box);
            KitUI.Root.Add(root);
        }

        // Entry points (Esc and the buttons call these; checks call them directly).
        public void Pause()
        {
            open = true;
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            open = false;
            Time.timeScale = 1f;
        }

        public void Restart()
        {
            Resume();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        public void MainMenu()
        {
            Resume();
            SceneManager.LoadScene(menuScene);
        }

        void Update()
        {
            Keyboard k = Keyboard.current;
            if (k != null && k.escapeKey.wasPressedThisFrame) { if (open) Resume(); else Pause(); }
            if (root == null || root.panel == null) Build();   // trap 18
            root.style.display = open ? DisplayStyle.Flex : DisplayStyle.None;
            root.BringToFront();
        }
    }
}
