using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace TopDownKit
{
    // The Pause control (Esc unless rebound) pauses: Time.timeScale = 0 (the fixed step stops, so all gameplay stops), and a
    // menu offers Resume, Restart and Main menu. Keyboard readers check IsPaused so a key
    // pressed while paused is not kept for later. Only this menu runs while paused.
    // Add-ons put their own buttons in it (Save, Load) with AddButton, so the menu never
    // knows about them and they can be deleted.
    public class PauseMenu : MonoBehaviour
    {
        [Tooltip("Build index of the start menu scene. -1 hides the Main menu button.")]
        public int menuScene = 0;
        [Tooltip("A Controls button that opens the rebind screen (every key can be changed).")]
        public bool controlsButton = true;

        static PauseMenu current;
        public static bool IsPaused => GamePause.IsPaused;
        public static PauseMenu Current => current;

        // Extra buttons from add-ons, shown after Resume and Restart.
        static readonly List<(string label, Action click)> extras = new List<(string, Action)>();
        static int extrasVersion;

        public static void AddButton(string label, Action click)
        {
            RemoveButton(label);
            extras.Add((label, click));
            extrasVersion++;
        }

        // Removes the button; with click given, only if it is still that one (a scene
        // being unloaded must not remove the button the next scene just added).
        public static void RemoveButton(string label, Action click = null)
        {
            if (extras.RemoveAll(e => e.label == label && (click == null || e.click == click)) > 0) extrasVersion++;
        }

        bool open;
        VisualElement root;
        int builtVersion = -1;

        void OnEnable() { current = this; }
        void OnDisable()
        {
            if (open) { Time.timeScale = 1f; GamePause.IsPaused = false; }
            open = false;
            if (rebind != null) rebind.Close();
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
            root.style.backgroundColor = KitUI.Theme.overlay;
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
            foreach (var e in extras) { Action click = e.click; box.Add(KitUI.Button(e.label, () => click())); }
            if (controlsButton) box.Add(KitUI.Button("Controls", OpenControls));
            builtVersion = extrasVersion;
            if (menuScene >= 0 && menuScene != SceneManager.GetActiveScene().buildIndex)
                box.Add(KitUI.Button("Main menu", MainMenu));
            root.Add(box);
            KitUI.Root.Add(root);
        }

        // Entry points (Esc and the buttons call these; checks call them directly).
        public void Pause()
        {
            open = true;
            GamePause.IsPaused = true;
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            if (rebind != null) rebind.Close();
            open = false;
            GamePause.IsPaused = false;
            Time.timeScale = 1f;
        }

        // The rebind screen, over the menu; its Back (or the Pause key) comes back here.
        ControlsScreen rebind;
        public void OpenControls()
        {
            if (rebind == null) rebind = new ControlsScreen();
            rebind.Open();
        }
        public bool ControlsOpen => rebind != null && rebind.IsOpen;

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
            if (rebind != null && rebind.IsOpen) rebind.Update();
            else if (Controls.Pressed(Controls.Pause)) { if (open) Resume(); else Pause(); }
            if (root != null && builtVersion != extrasVersion) { root.RemoveFromHierarchy(); root = null; }
            if (root == null || root.panel == null) Build();   // trap 18
            root.style.display = open && !ControlsOpen ? DisplayStyle.Flex : DisplayStyle.None;
            root.BringToFront();
            if (ControlsOpen) rebind.BringToFront();
        }
    }
}
