using UnityEngine;

namespace TopDownKit
{
    // Draws one inventory slot with OnGUI: a box, the item icon, the count. Shared by the
    // chest screen and the hotbar readout. A placeholder look; the real screens are phase 6.
    public static class InventoryGui
    {
        static GUIStyle countStyle;

        public static void Slot(Rect r, ItemStack s, bool selected)
        {
            Color old = GUI.color;
            if (selected) GUI.color = new Color(1f, 0.85f, 0.3f, 1f);
            GUI.Box(r, GUIContent.none);
            GUI.color = old;
            if (s.Empty) return;
            Sprite icon = s.item.icon;
            if (icon != null && icon.texture != null)
            {
                Rect t = icon.textureRect;
                Texture tex = icon.texture;
                Rect uv = new Rect(t.x / tex.width, t.y / tex.height, t.width / tex.width, t.height / tex.height);
                // Whole-number scale so the pixels stay square.
                float k = Mathf.Max(1f, Mathf.Floor(Mathf.Min(r.width, r.height) * 0.75f / Mathf.Max(t.width, t.height)));
                Rect ir = new Rect(r.center.x - t.width * k * 0.5f, r.center.y - t.height * k * 0.5f, t.width * k, t.height * k);
                GUI.DrawTextureWithTexCoords(ir, tex, uv);
            }
            else GUI.Label(r, s.item.Name);
            if (s.count > 1)
            {
                if (countStyle == null) countStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerRight, fontStyle = FontStyle.Bold };
                countStyle.fontSize = Mathf.Max(10, (int)(r.height * 0.3f));
                GUI.Label(new Rect(r.x, r.y, r.width - 3f, r.height - 1f), s.count.ToString(), countStyle);
            }
        }
    }
}
