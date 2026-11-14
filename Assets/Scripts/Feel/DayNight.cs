using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace TopDownKit
{
    public enum DayNightMode { Off, Fixed, Cycle }

    // Add-on: tints the world through the scene's global 2D light (UI is not touched).
    // Off = the light as the scene set it; Fixed = one tint; Cycle = round the colours in
    // `cycle` (evenly spaced, blended) every `cycleSeconds`. The cycle runs on the fixed
    // step, so it stops with the pause and a game can read TimeOfDay in its rules.
    public class DayNight : MonoBehaviour
    {
        public DayNightMode mode = DayNightMode.Off;
        [Tooltip("The tint for Fixed.")]
        public Color tint = new Color(0.55f, 0.6f, 0.9f, 1f);
        [Tooltip("Seconds for one whole day.")]
        [Min(1f)] public float cycleSeconds = 60f;
        [Tooltip("Colours of the day in order, evenly spaced, back to the first at the end.")]
        public Color[] cycle =
        {
            new Color(1f, 1f, 1f, 1f),        // day
            new Color(1f, 0.75f, 0.55f, 1f),  // evening
            new Color(0.4f, 0.45f, 0.75f, 1f),// night
            new Color(0.85f, 0.7f, 0.85f, 1f) // morning
        };
        [Tooltip("Where in the day a scene starts, 0..1.")]
        [Range(0f, 1f)] public float startAt;
        [Tooltip("The light to tint. Empty = the scene's global 2D light.")]
        public Light2D globalLight;

        // 0..1 through the day (0 = the first colour).
        public float TimeOfDay { get; private set; }

        Color baseColor;
        bool found;

        void Awake() { TimeOfDay = startAt; }

        bool Find()
        {
            if (found && globalLight != null) return true;
            if (globalLight == null)
                foreach (Light2D l in FindObjectsByType<Light2D>(FindObjectsSortMode.None))
                    if (l.lightType == Light2D.LightType.Global) { globalLight = l; break; }
            if (globalLight == null) return false;
            baseColor = globalLight.color;
            found = true;
            return true;
        }

        void FixedUpdate()
        {
            if (mode == DayNightMode.Cycle) TimeOfDay = Mathf.Repeat(TimeOfDay + Time.fixedDeltaTime / cycleSeconds, 1f);
        }

        // Looks only: the colour follows the time on each frame.
        void LateUpdate()
        {
            if (!Find()) return;
            globalLight.color = Tint() * baseColor;
        }

        public Color Tint()
        {
            switch (mode)
            {
                case DayNightMode.Fixed: return tint;
                case DayNightMode.Cycle: return CycleColor(cycle, TimeOfDay);
                default: return Color.white;
            }
        }

        // The colour at t (0..1) of evenly spaced colours that wrap round.
        public static Color CycleColor(Color[] colors, float t)
        {
            if (colors == null || colors.Length == 0) return Color.white;
            float x = Mathf.Repeat(t, 1f) * colors.Length;
            int i = Mathf.FloorToInt(x) % colors.Length;
            return Color.Lerp(colors[i], colors[(i + 1) % colors.Length], x - Mathf.Floor(x));
        }

        void OnDisable() { if (found && globalLight != null) globalLight.color = baseColor; found = false; }
    }
}
