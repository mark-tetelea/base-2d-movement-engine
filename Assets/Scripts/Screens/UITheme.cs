using System;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace TopDownKit
{
    // Where a HUD part sits on the 640x360 screen: a corner, an edge's middle or the centre,
    // then `offset` UI pixels in from there.
    public enum UIAnchor { TopLeft, Top, TopRight, Left, Centre, Right, BottomLeft, Bottom, BottomRight }

    [Serializable]
    public struct UIPlace
    {
        public UIAnchor anchor;
        public Vector2 offset;
        public UIPlace(UIAnchor a, float x, float y) { anchor = a; offset = new Vector2(x, y); }
    }

    // The look of every kit screen, as one asset: Resources/KitTheme. Colours, the font
    // (a pixel font is in Assets/UI/Fonts), box / button / slot / bar art (sprites with
    // 9-slice borders; empty = a flat box with a 1-pixel edge), sizes, and where each HUD
    // part sits. Swap the asset or change it - no code. Screens read it when they are
    // built (entering a scene). For anything else, every element also carries a USS class
    // (kit-box, kit-button, kit-label, kit-slot, kit-bar, hud-*) for Assets/UI/KitTheme.tss.
    // Callers (CLAUDE.md, three real callers): the HUD, pause menu, text box, chest, bag and
    // shop screens, start menu, map, touch controls and the Playground picker.
    [CreateAssetMenu(menuName = "TopDownKit/UI Theme", fileName = "KitTheme")]
    public class UITheme : ScriptableObject
    {
        [Header("Colours")]
        public Color ink = new Color32(0x18, 0x14, 0x25, 255);
        [Tooltip("Box fill. Keep it opaque (trap 36).")]
        public Color panel = new Color32(0x26, 0x2b, 0x44, 255);
        public Color edge = new Color32(0x5a, 0x69, 0x88, 255);
        public Color text = new Color32(0xff, 0xff, 0xff, 255);
        public Color dim = new Color32(0x8b, 0x9b, 0xb4, 255);
        public Color gold = new Color32(0xfe, 0xae, 0x34, 255);
        public Color red = new Color32(0xe4, 0x3b, 0x44, 255);
        public Color green = new Color32(0x63, 0xc7, 0x4d, 255);
        [Tooltip("Laid over the game behind the pause menu.")]
        public Color overlay = new Color(0f, 0f, 0f, 0.5f);
        [Tooltip("Map: a visited room, the room you are in, room edges.")]
        public Color mapRoomSeen = new Color32(0x3a, 0x44, 0x66, 255);
        public Color mapRoomHere = new Color32(0x5a, 0x69, 0x88, 255);
        public Color mapRoomEdge = new Color32(0x8b, 0x9b, 0xb4, 255);
        [Tooltip("Touch controls: stick ring and button, knob, button held.")]
        public Color touchBack = new Color(0.1f, 0.1f, 0.15f, 0.55f);
        public Color touchKnob = new Color(0.55f, 0.6f, 0.7f, 0.8f);
        public Color touchDown = new Color(0.35f, 0.4f, 0.55f, 0.75f);

        [Header("Text")]
        [Tooltip("A font asset (TextCore). Wins over Font. Empty = the default font.")]
        public FontAsset fontAsset;
        [Tooltip("A font file (.ttf / .otf). Empty = the default font.")]
        public Font font;
        [Tooltip("A pixel font's own size in pixels (8 for the kit's pixel font): every text size is rounded to a whole multiple of it, so pixels stay square. 0 = any size.")]
        [Min(0)] public int pixelFontSize;
        [Tooltip("All text sizes x this (before rounding for a pixel font).")]
        [Min(0.1f)] public float textScale = 1f;
        [Tooltip("A 1-pixel dark shadow under text, so it reads over any floor.")]
        public bool textShadow = true;

        [Header("Art (sprites with 9-slice borders; empty = flat colour box)")]
        public Sprite box;
        public Sprite button;
        public Sprite slot;
        public Sprite slotSelected;
        public Sprite barBack;
        public Sprite barFill;

        [Header("Sizes (UI pixels)")]
        [Min(8)] public int slotSize = 20;
        [Min(4)] public int iconSize = 16;
        [Min(0)] public int edgeWidth = 1;

        [Header("HUD layout")]
        [Tooltip("The health bar with the status line under it.")]
        public UIPlace hudHealth = new UIPlace(UIAnchor.TopLeft, 4, 4);
        public UIPlace hudHelp = new UIPlace(UIAnchor.TopRight, 4, 4);
        [Tooltip("Weapon and ammo.")]
        public UIPlace hudAmmo = new UIPlace(UIAnchor.BottomRight, 4, 4);
        public UIPlace hudHotbar = new UIPlace(UIAnchor.Bottom, 0, 2);
        public UIPlace hudBoss = new UIPlace(UIAnchor.Top, 0, 18);
        public UIPlace hudBanner = new UIPlace(UIAnchor.Top, 0, 90);
        [Min(8)] public int healthBarWidth = 64;
        [Min(8)] public int bossBarWidth = 200;
        [Min(1)] public int barHeight = 6;
    }
}
