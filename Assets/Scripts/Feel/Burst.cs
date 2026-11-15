using UnityEngine;

namespace TopDownKit
{
    // The kit's placeholder effect: a few square pixels flying out from a point and
    // fading, then it switches itself off (FeelPlayer reuses it). Looks only, frame time.
    // A game puts its own effect prefab in the slot instead (a ParticleSystem, anything).
    public class Burst : MonoBehaviour
    {
        [Range(1, 64)] public int count = 8;
        [Tooltip("Tiles per second, picked between these two for each bit.")]
        public Vector2 speed = new Vector2(2f, 5f);
        [Tooltip("Seconds the bits live.")]
        [Min(0.01f)] public float life = 0.3f;
        [Tooltip("Size of a bit in game pixels (16 = one tile).")]
        [Range(1, 8)] public int pixels = 1;
        public Color colorA = Color.white;
        public Color colorB = Color.white;
        [Tooltip("How much the bits slow down per second (0 = not at all).")]
        [Min(0f)] public float drag = 6f;
        [Tooltip("Spread in degrees around up (360 = all round).")]
        [Range(0f, 360f)] public float arc = 360f;
        public int sortingOrder = 60;
        [Tooltip("The art of one bit (a spark, a leaf, a drop), at its own size. Empty = a square of Pixels game pixels.")]
        public Sprite bit;

        SpriteRenderer[] bits;
        Vector2[] velocity;
        Vector2[] offset;
        float age;
        static Sprite pixel;

        void OnEnable()
        {
            if (bits == null || bits.Length != count) Build();
            age = 0f;
            for (int i = 0; i < bits.Length; i++)
            {
                float a = (90f + Random.Range(-arc, arc) * 0.5f) * Mathf.Deg2Rad;
                velocity[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Random.Range(speed.x, speed.y);
                offset[i] = Vector2.zero;
                bits[i].color = Color.Lerp(colorA, colorB, Random.value);
                bits[i].transform.localPosition = Vector3.zero;
            }
        }

        void Build()
        {
            if (bits != null) foreach (SpriteRenderer b in bits) if (b != null) Destroy(b.gameObject);
            if (pixel == null)
            {
                // Texture2D.whiteTexture is 4x4: at 64 per unit it is one game pixel.
                Texture2D t = Texture2D.whiteTexture;
                pixel = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), t.width * 16f);
            }
            bits = new SpriteRenderer[count];
            velocity = new Vector2[count];
            offset = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                var go = new GameObject("Bit");
                go.transform.SetParent(transform, false);
                go.transform.localScale = Vector3.one * (bit != null ? 1f : pixels);
                bits[i] = go.AddComponent<SpriteRenderer>();
                bits[i].sprite = bit != null ? bit : pixel;
                bits[i].sortingOrder = sortingOrder;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            age += dt;
            if (age >= life) { gameObject.SetActive(false); return; }
            float fade = 1f - age / life;
            float slow = Mathf.Exp(-drag * dt);
            for (int i = 0; i < bits.Length; i++)
            {
                velocity[i] *= slow;
                offset[i] += velocity[i] * dt;
                // Whole game pixels, so the bits stay crisp.
                bits[i].transform.localPosition = new Vector3(Mathf.Round(offset[i].x * 16f) / 16f, Mathf.Round(offset[i].y * 16f) / 16f, 0f);
                Color c = bits[i].color;
                c.a = fade;
                bits[i].color = c;
            }
        }
    }
}
