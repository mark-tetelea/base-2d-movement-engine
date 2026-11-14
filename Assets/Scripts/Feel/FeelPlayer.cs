using System;
using System.Collections.Generic;
using UnityEngine;

namespace TopDownKit
{
    // Add-on piece, one per scene: plays the sound, effect and hit-stop of each event from
    // a FeelSet. It listens to the kit's own events (Health, Gun, Dash, Pickup, doors,
    // crumbling ground, UI buttons), so nothing in the core knows it exists - delete this
    // folder and the game is simply silent. A game plays its own events with Play().
    //
    // Hit-stop: Time.timeScale = 0 for a few real-time milliseconds, exactly like the
    // pause. No fixed step runs during it, so the gameplay rules, their dt and their
    // results are untouched - the game just waits. It counts down on unscaled time (it is
    // a look, like menus), never ends a pause, and overlapping hits take the longest
    // freeze, they do not add up. It starts at the end of the current frame's steps.
    public class FeelPlayer : MonoBehaviour
    {
        public FeelSet set;

        [Header("On / off")]
        public bool sounds = true;
        public bool effects = true;
        public bool hitStop = true;
        [Tooltip("Whether damage from the ground (red tiles, trails) also freezes the game.")]
        public bool groundHitsFreeze;

        [Header("Volume")]
        [Range(0f, 1f)] public float master = 1f;
        [Range(0f, 1f)] public float effectsVolume = 1f;
        [Range(0f, 1f)] public float music = 0.6f;

        [Tooltip("Hits on this team use the PlayerHurt slot (-1 = none). The player is team 0 in the kit.")]
        public int playerTeam = 0;
        [Tooltip("The same event is not played again within this many seconds (a shotgun hitting five at once).")]
        [Min(0f)] public float sameEventGap = 0.03f;
        [Tooltip("Sounds that can play at the same time.")]
        [Range(1, 32)] public int voices = 8;

        static FeelPlayer current;
        public static FeelPlayer Current => current;

        public bool Freezing => freezeLeft > 0f;

        AudioSource[] sources;
        AudioSource musicSource;
        int nextSource;
        float[] lastPlayed;
        float freezeLeft, scaleBefore = 1f;
        readonly Dictionary<GameObject, List<GameObject>> pool = new Dictionary<GameObject, List<GameObject>>();
        readonly List<(GameObject go, float left)> timed = new List<(GameObject, float)>();

        // Plays an event's slot at a world position (the position matters for effects;
        // sounds are flat 2D). Nothing happens without a FeelPlayer in the scene.
        public static void Play(FeelEvent id, Vector2 position)
        {
            if (current != null) current.PlayHere(id, position);
        }

        void OnEnable()
        {
            current = this;
            Health.AnyDamaged += OnDamaged;
            Health.AnyDied += OnDied;
            Gun.AnyShot += OnShot;
            Dash.AnyStarted += OnDash;
            Pickup.AnyCollected += OnPickup;
            RoomLock.AnyDoors += OnRoomDoors;
            LockedDoor.AnyOpened += OnLockedDoor;
            Lever.AnyPulled += OnLever;
            GroundMap.TileChanged += OnTile;
            KitUI.Clicked += OnClick;
            Shop.AnyTrade += OnTrade;
            SaveGame.AnySaved += OnSaved;
        }

        void OnDisable()
        {
            Health.AnyDamaged -= OnDamaged;
            Health.AnyDied -= OnDied;
            Gun.AnyShot -= OnShot;
            Dash.AnyStarted -= OnDash;
            Pickup.AnyCollected -= OnPickup;
            RoomLock.AnyDoors -= OnRoomDoors;
            LockedDoor.AnyOpened -= OnLockedDoor;
            Lever.AnyPulled -= OnLever;
            GroundMap.TileChanged -= OnTile;
            KitUI.Clicked -= OnClick;
            Shop.AnyTrade -= OnTrade;
            SaveGame.AnySaved -= OnSaved;
            if (Freezing) EndFreeze();
            if (current == this) current = null;
        }

        void Start() { SetUpAudio(); }

        // ---- kit events -> slots ----

        static Vector2 Centre(Component c)
        {
            Collider2D col = c.GetComponent<Collider2D>();
            return col != null ? (Vector2)col.bounds.center : (Vector2)c.transform.position;
        }

        void OnDamaged(Health h)
        {
            if (h.Dead) return;   // the death plays instead
            FeelEvent id;
            FeelOverride o = h.GetComponent<FeelOverride>();
            if (o != null) id = o.onHit;
            else if (h.team == playerTeam) id = FeelEvent.PlayerHurt;
            else id = h.LastSource == HitSource.Melee ? FeelEvent.MeleeHit : FeelEvent.Hit;
            PlayHere(id, Centre(h), h.LastSource != HitSource.Ground || groundHitsFreeze);
        }

        void OnDied(Health h)
        {
            FeelOverride o = h.GetComponent<FeelOverride>();
            PlayHere(o != null ? o.onDeath : FeelEvent.Death, Centre(h), h.LastSource != HitSource.Ground || groundHitsFreeze);
        }

        void OnShot(Gun g)
        {
            Weapon w = g.Current;
            PlayHere(w != null && w.kind == WeaponKind.Melee ? FeelEvent.Swing : FeelEvent.Shot, g.transform.position);
        }

        void OnDash(Dash d) { PlayHere(FeelEvent.Dash, Centre(d)); }
        void OnPickup(Pickup p, GameObject who) { PlayHere(FeelEvent.Pickup, p.transform.position); }
        void OnRoomDoors(RoomLock l, bool shut) { PlayHere(shut ? FeelEvent.DoorShut : FeelEvent.DoorOpen, l.transform.position); }
        void OnLockedDoor(LockedDoor d) { PlayHere(FeelEvent.DoorOpen, d.transform.position); }
        void OnLever(Lever l) { PlayHere(FeelEvent.DoorOpen, l.transform.position); }
        void OnClick() { PlayHere(FeelEvent.UiClick, Vector2.zero); }
        void OnTrade(Shop s, bool bought) { PlayHere(bought ? FeelEvent.Buy : FeelEvent.Sell, s.transform.position); }
        void OnSaved(SaveGame g, int slot) { PlayHere(FeelEvent.Save, Vector2.zero); }

        void OnTile(GroundMap map, Vector3Int cell, GroundState state)
        {
            if (state == GroundState.Broken) PlayHere(FeelEvent.Crumble, map.Tilemap.GetCellCenterWorld(cell));
        }

        // ---- playing a slot ----

        void PlayHere(FeelEvent id, Vector2 position, bool mayFreeze = true)
        {
            if (set == null || !set.Find(id, out FeelSlot slot)) return;
            int k = (int)id;
            if (lastPlayed == null || k >= lastPlayed.Length)
            {
                var grown = new float[Mathf.Max(k + 1, Enum.GetValues(typeof(FeelEvent)).Length)];
                for (int i = 0; i < grown.Length; i++) grown[i] = -1f;
                if (lastPlayed != null) Array.Copy(lastPlayed, grown, lastPlayed.Length);
                lastPlayed = grown;
            }
            float now = Time.unscaledTime;
            bool again = lastPlayed[k] >= 0f && now - lastPlayed[k] < sameEventGap;
            lastPlayed[k] = now;

            if (mayFreeze && hitStop && slot.hitStop > 0f) Freeze(slot.hitStop);
            if (again) return;
            if (sounds) Sound(slot);
            if (effects && slot.effect != null) Effect(slot, position);
        }

        void SetUpAudio()
        {
            if (sources != null) return;
            // The start menu has no camera listener; sounds need one somewhere.
            if (FindAnyObjectByType<AudioListener>() == null) gameObject.AddComponent<AudioListener>();
            sources = new AudioSource[Mathf.Max(1, voices)];
            for (int i = 0; i < sources.Length; i++)
            {
                sources[i] = gameObject.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
                sources[i].spatialBlend = 0f;
            }
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.spatialBlend = 0f;
        }

        void Sound(FeelSlot slot)
        {
            if (slot.clips == null || slot.clips.Length == 0) return;
            AudioClip clip = slot.clips[UnityEngine.Random.Range(0, slot.clips.Length)];
            if (clip == null) return;
            SetUpAudio();
            AudioSource s = sources[nextSource];
            nextSource = (nextSource + 1) % sources.Length;
            s.pitch = 1f + UnityEngine.Random.Range(-slot.pitchJitter, slot.pitchJitter);
            s.clip = clip;
            s.volume = master * effectsVolume * slot.volume;
            s.Play();
        }

        // Effects are reused: an instance that has switched itself off (or whose life ran
        // out) is switched on again at the new place.
        void Effect(FeelSlot slot, Vector2 position)
        {
            if (!pool.TryGetValue(slot.effect, out List<GameObject> list))
            {
                list = new List<GameObject>();
                pool[slot.effect] = list;
            }
            GameObject go = null;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i] == null) { list.RemoveAt(i); continue; }
                if (!list[i].activeSelf) { go = list[i]; break; }
            }
            if (go == null)
            {
                go = Instantiate(slot.effect, transform);
                go.SetActive(false);
                list.Add(go);
            }
            go.transform.position = new Vector3(position.x, position.y, 0f);
            go.SetActive(true);
            if (slot.effectLife > 0f) timed.Add((go, slot.effectLife));
        }

        // ---- hit-stop ----

        public void Freeze(float seconds)
        {
            if (seconds <= 0f || PauseMenu.IsPaused) return;
            if (!Freezing)
            {
                if (Time.timeScale <= 0f) return;   // stopped by something else
                scaleBefore = Time.timeScale;
                Time.timeScale = 0f;
            }
            freezeLeft = Mathf.Max(freezeLeft, seconds);
        }

        void EndFreeze()
        {
            freezeLeft = 0f;
            // Never ends a pause, and leaves alone a scale someone else set meanwhile.
            if (Time.timeScale == 0f && !PauseMenu.IsPaused) Time.timeScale = scaleBefore;
        }

        void Update()
        {
            if (Freezing)
            {
                freezeLeft -= Time.unscaledDeltaTime;
                if (freezeLeft <= 0f) EndFreeze();
            }
            for (int i = timed.Count - 1; i >= 0; i--)
            {
                var t = timed[i];
                t.left -= Time.deltaTime;
                if (t.left > 0f && t.go != null) { timed[i] = t; continue; }
                if (t.go != null) t.go.SetActive(false);
                timed.RemoveAt(i);
            }
            UpdateMusic();
        }

        void UpdateMusic()
        {
            if (musicSource == null) return;
            AudioClip clip = set != null && sounds ? set.music : null;
            musicSource.volume = master * music;
            if (clip == null) { if (musicSource.isPlaying) musicSource.Stop(); return; }
            if (musicSource.clip != clip) musicSource.clip = clip;
            if (!musicSource.isPlaying) musicSource.Play();
        }
    }
}
