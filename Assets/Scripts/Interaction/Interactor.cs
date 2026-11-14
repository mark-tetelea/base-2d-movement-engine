using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownKit
{
    // A screen the interact key talks to while it is open (the text box, the chest
    // screen). While one is open the key goes to it, targets are not picked, and the
    // player's walking is paused.
    public interface IUsePanel
    {
        void UsePressed(Interactor who);
    }

    // The player's interact key. On the fixed step it picks the best target in reach with
    // the one rule (Interaction.Pick) and uses it on a press, or after a hold. Input
    // arrives as data: SetUseHeld (the E key when readKeyboard is on; a touch button later).
    public class Interactor : MonoBehaviour
    {
        public InteractSettings settings = new InteractSettings(1.5f, BestTarget.Facing, UseMode.Press, 0.5f);
        [Tooltip("On: the E key drives it. Off: something else calls SetUseHeld.")]
        public bool readKeyboard = true;
        [Tooltip("Where reach and facing are measured from, from this object's pivot (tiles).")]
        public Vector2 centre = Vector2.zero;

        bool held, pressed, usedThisHold;
        float holdFor;
        PlayerMover mover;
        PlayerFacing facing;
        Bag bag;
        readonly List<Vector2> points = new List<Vector2>();
        readonly List<Interactable> candidates = new List<Interactable>();

        public Interactable Target { get; private set; }
        public IUsePanel Panel { get; private set; }
        public bool PanelOpen => Panel != null;
        // 0-1 while holding towards a use (Hold mode); 0 otherwise.
        public float HoldProgress => settings.use == UseMode.Hold && Target != null && !usedThisHold
            ? Mathf.Clamp01(holdFor / Mathf.Max(settings.holdTime, 1e-4f)) : 0f;
        public Bag Bag { get { if (bag == null) bag = GetComponent<Bag>(); return bag; } }
        public Vector2 FacingDirection
        {
            get
            {
                if (facing == null) facing = GetComponent<PlayerFacing>();
                if (facing != null) return Facing.ToVector(facing.Current);
                if (mover == null) mover = GetComponent<PlayerMover>();
                return mover != null ? mover.LastMoveDirection : Vector2.zero;
            }
        }

        // Held: the key is down. A press between two steps is kept until a step uses it.
        public void SetUseHeld(bool down)
        {
            if (down && !held) pressed = true;
            held = down;
        }

        public void OpenPanel(IUsePanel p) { Panel = p; Pause(true); }
        public void ClosePanel(IUsePanel p)
        {
            if (Panel != p) return;
            Panel = null;
            Pause(false);
            usedThisHold = true;   // the press that closed it must not use the target too
        }

        void Pause(bool on)
        {
            if (mover == null) mover = GetComponent<PlayerMover>();
            if (mover != null) mover.inputPaused = on;
        }

        void Update()
        {
            if (!readKeyboard) return;
            Keyboard k = Keyboard.current;
            SetUseHeld(k != null && k.eKey.isPressed);
        }

        void FixedUpdate()
        {
            bool press = pressed;
            pressed = false;
            if (!held) usedThisHold = false;

            if (Panel != null)
            {
                Pause(true);   // the camera may have unpaused after a slide
                Target = null;
                holdFor = 0f;
                if (press) Panel.UsePressed(this);
                return;
            }

            Interactable before = Target;
            Target = FindTarget();
            if (Target != before) holdFor = 0f;
            if (Target == null) { holdFor = 0f; return; }

            if (settings.use == UseMode.Press)
            {
                if (press) Use();
                return;
            }
            if (!held || usedThisHold) { holdFor = 0f; return; }
            holdFor += Time.fixedDeltaTime;
            if (holdFor >= settings.holdTime - 1e-4f) Use();   // trap 79
        }

        void Use()
        {
            Interactable t = Target;
            usedThisHold = true;
            holdFor = 0f;
            t.Use(this);
        }

        Interactable FindTarget()
        {
            points.Clear();
            candidates.Clear();
            Vector2 from = transform.position;
            float far = settings.reach + 4f;
            foreach (Interactable i in Interactable.All)
            {
                if (i == null || !i.isActiveAndEnabled) continue;
                Vector2 p = i.Point;
                if ((p - from).sqrMagnitude > far * far || !i.CanUse(this)) continue;
                candidates.Add(i);
                points.Add(p);
            }
            int best = Interaction.Pick(from + centre, FacingDirection, points, settings);
            return best >= 0 ? candidates[best] : null;
        }
    }
}
