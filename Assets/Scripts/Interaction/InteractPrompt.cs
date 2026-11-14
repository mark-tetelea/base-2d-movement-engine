using UnityEngine;

namespace TopDownKit
{
    // The small "E" over the current target, and a bar filling while a Hold use builds up.
    // Looks only: it reads the Interactor and moves two sprites (children of this object,
    // which should not be a child of the player).
    public class InteractPrompt : MonoBehaviour
    {
        public Interactor interactor;
        [Tooltip("The key cap sprite.")]
        public SpriteRenderer icon;
        [Tooltip("A white square stretched as the hold bar (optional).")]
        public SpriteRenderer holdBar;
        [Tooltip("Full width of the hold bar, tiles.")]
        [Min(0f)] public float holdBarWidth = 1f;

        void LateUpdate()
        {
            Interactable t = interactor != null ? interactor.Target : null;
            bool show = t != null && !interactor.PanelOpen;
            if (icon != null) icon.enabled = show;
            if (show)
            {
                // Whole pixels, like the camera (16 px per tile).
                Vector2 p = (Vector2)t.transform.position + t.promptOffset;
                p = new Vector2(Mathf.Round(p.x * 16f) / 16f, Mathf.Round(p.y * 16f) / 16f);
                transform.position = p;
            }
            if (holdBar == null) return;
            float f = show ? interactor.HoldProgress : 0f;
            holdBar.enabled = f > 0f;
            holdBar.transform.localScale = new Vector3(Mathf.Max(1f / 16f, f * holdBarWidth), 2f / 16f, 1f);
            holdBar.transform.localPosition = new Vector3(-holdBarWidth * 0.5f + f * holdBarWidth * 0.5f, -0.5f, 0f);
        }
    }
}
