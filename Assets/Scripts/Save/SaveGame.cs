using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TopDownKit
{
    // What a save holds is chosen per system.
    [Flags]
    public enum SaveParts
    {
        None = 0,
        Scene = 1, Player = 2, Bag = 4, Coins = 8, Chests = 16, Doors = 32, Rooms = 64, Pickups = 128, Shops = 256,
        All = Scene | Player | Bag | Coins | Chests | Doors | Rooms | Pickups | Shops
    }

    // One setting saved in the settings file: a field of a component (a volume, a mode).
    [Serializable]
    public struct SavedSetting
    {
        public Component target;
        [Tooltip("The field's name (bool, int, float, enum or string).")]
        public string field;
    }

    // Add-on, one per scene: saves and loads the game in slots. Delete the Save folder and
    // the game simply has no saving.
    //  - Save(slot) writes what `parts` lists (SaveData) through SaveFiles: a temp file,
    //    then the old file kept as the backup.
    //  - Load(slot) reads the file (falling back to the backup), then reloads the saved
    //    scene (a generated level with its seed) and puts the saved state on top of the
    //    fresh scene in Start. A broken or newer file changes nothing.
    //  - Settings (sound, minimap, touch...) are one file for all slots, read in Awake and
    //    written when a slot is saved and when the scene ends.
    // Objects are found by their place in the hierarchy (IdOf), worked out when the scene
    // starts, so a renamed or moved object loses its saved state (it is logged).
    [DefaultExecutionOrder(1000)]
    public class SaveGame : MonoBehaviour
    {
        public const int Version = 1;

        [Tooltip("What a save holds. Parts left out stay as the scene starts on loading.")]
        public SaveParts parts = SaveParts.All;
        [Tooltip("Number of save slots.")]
        [Min(1)] public int slots = 3;
        [Tooltip("Folder under the game's data folder.")]
        public string folder = "Saves";
        [Tooltip("Position, health and bag are taken from here.")]
        public Transform player;
        [Tooltip("The item counted as coins (the Coins part).")]
        public Item coin;
        [Tooltip("Every item a save may name. Items in the scene at the start are known anyway.")]
        public Item[] items = new Item[0];

        [Header("Settings file")]
        public bool saveSettings = true;
        public SavedSetting[] settings = new SavedSetting[0];

        static SaveGame current;
        public static SaveGame Current => current;
        // Every save written (sounds). The int is the slot.
        public static event Action<SaveGame, int> AnySaved;

        // A loaded save waiting for its scene (static: it crosses a scene load).
        static SaveData pending;
        static string pendingNote = "", pendingScene = "";

        public string LastNote { get; private set; } = "";
        public bool Applied { get; private set; }

        readonly Dictionary<UnityEngine.Object, string> ids = new Dictionary<UnityEngine.Object, string>();
        readonly Dictionary<string, UnityEngine.Object> byId = new Dictionary<string, UnityEngine.Object>();
        readonly Dictionary<Pickup, int> startCounts = new Dictionary<Pickup, int>();
        readonly HashSet<string> taken = new HashSet<string>();
        readonly Dictionary<string, Item> itemsByName = new Dictionary<string, Item>();
        string settingsAtStart;
        // The settings as last seen while the scene ran. Written on exit: by then the
        // components they live on may already be destroyed (a scene change), and reading
        // them then would write an empty list over the player's settings.
        SettingSave[] lastSettings = new SettingSave[0];
        string lastSettingsText;
        float nextSettingsLook;

        public string Folder => SaveFiles.Folder(folder);
        public string SlotPath(int slot) => Path.Combine(Folder, "slot" + (slot + 1) + ".json");
        public string SettingsPath => Path.Combine(Folder, "settings.json");

        void OnEnable() { current = this; }
        void OnDisable()
        {
            if (current == this) current = null;
            if (!saveSettings || settingsAtStart == null) return;
            bool allThere = true;
            if (settings != null) foreach (SavedSetting s in settings) if (s.target == null) allThere = false;
            if (allThere) LookAtSettings();   // a change made in the last half second
            if (lastSettingsText != settingsAtStart) WriteSettings(lastSettings);
        }

        void Awake()
        {
            if (saveSettings) ReadSettings();
            LookAtSettings();
            settingsAtStart = lastSettingsText;
        }

        // Settings change from menus and the picker; looked at twice a second (real time,
        // so also while paused).
        void Update()
        {
            if (!saveSettings || Time.unscaledTime < nextSettingsLook) return;
            nextSettingsLook = Time.unscaledTime + 0.5f;
            LookAtSettings();
        }

        void LookAtSettings()
        {
            SettingSave[] now = CaptureSettings();
            string text = SettingsText(now);
            if (text == lastSettingsText) return;
            lastSettings = now;
            lastSettingsText = text;
        }

        void Start()
        {
            Register();
            if (pending == null) return;
            SaveData d = pending;
            pending = null;
            if (pendingScene != gameObject.scene.name) return;   // loaded somewhere else: not ours
            Apply(d);
            LastNote = pendingNote;
            pendingNote = "";
        }

        // ---- ids -----------------------------------------------------------------------

        // The object's path from its scene root; same-named siblings get #1, #2...
        public static string IdOf(Transform t)
        {
            var names = new List<string>();
            for (; t != null; t = t.parent)
            {
                int k = 0;
                if (t.parent != null)
                {
                    for (int i = 0; i < t.GetSiblingIndex(); i++) if (t.parent.GetChild(i).name == t.name) k++;
                }
                else if (t.gameObject.scene.IsValid())
                {
                    foreach (GameObject g in t.gameObject.scene.GetRootGameObjects())
                    {
                        if (g.transform == t) break;
                        if (g.name == t.name) k++;
                    }
                }
                names.Add(k > 0 ? t.name + "#" + k : t.name);
            }
            names.Reverse();
            return string.Join("/", names);
        }

        void Register()
        {
            ids.Clear(); byId.Clear(); startCounts.Clear(); taken.Clear();
            void Add(Component c)
            {
                if (c == null || ids.ContainsKey(c)) return;
                string id = c.GetType().Name + ":" + IdOf(c.transform);
                if (byId.ContainsKey(id)) { Debug.LogWarning("SaveGame: two objects share the id " + id + "; only the first is saved.", c); return; }
                ids[c] = id;
                byId[id] = c;
            }
            const FindObjectsInactive all = FindObjectsInactive.Include;
            foreach (Chest c in FindObjectsByType<Chest>(all, FindObjectsSortMode.None)) Add(c);
            foreach (LockedDoor d in FindObjectsByType<LockedDoor>(all, FindObjectsSortMode.None)) Add(d);
            foreach (Lever l in FindObjectsByType<Lever>(all, FindObjectsSortMode.None)) Add(l);
            foreach (RoomLock l in FindObjectsByType<RoomLock>(all, FindObjectsSortMode.None)) Add(l);
            foreach (Room r in FindObjectsByType<Room>(all, FindObjectsSortMode.None)) Add(r);
            foreach (Shop s in FindObjectsByType<Shop>(all, FindObjectsSortMode.None)) Add(s);
            foreach (Pickup p in FindObjectsByType<Pickup>(all, FindObjectsSortMode.None)) { Add(p); startCounts[p] = p.stack.count; }

            itemsByName.Clear();
            void Know(Item i) { if (i != null && !itemsByName.ContainsKey(i.name)) itemsByName[i.name] = i; }
            if (items != null) foreach (Item i in items) Know(i);
            Know(coin);
            foreach (Bag b in FindObjectsByType<Bag>(all, FindObjectsSortMode.None))
                if (b.inventory != null && b.inventory.slots != null) foreach (ItemStack s in b.inventory.slots) Know(s.item);
            foreach (var kv in startCounts) Know(kv.Key.stack.item);
            foreach (Shop s in FindObjectsByType<Shop>(all, FindObjectsSortMode.None))
            {
                Know(s.currency);
                for (int i = 0; i < s.LineCount; i++) Know(s.Line(i).item);
            }
        }

        T Find<T>(string id) where T : UnityEngine.Object => id != null && byId.TryGetValue(id, out UnityEngine.Object o) ? o as T : null;

        Item ItemNamed(string n)
        {
            if (string.IsNullOrEmpty(n)) return null;
            if (itemsByName.TryGetValue(n, out Item i)) return i;
            Debug.LogWarning("SaveGame: the save names an item this game does not know: " + n + " (add it to SaveGame.items).", this);
            return null;
        }

        // ---- capture -------------------------------------------------------------------

        bool Has(SaveParts p) => (parts & p) != 0;

        Bag PlayerBag => player != null ? player.GetComponent<Bag>() : null;
        Health PlayerHealth => player != null ? player.GetComponent<Health>() : null;

        // Why saving is not allowed right now ("" = it is).
        public string CannotSave()
        {
            Health h = PlayerHealth;
            if (h != null && h.Dead) return "Cannot save while dead";
            return "";
        }

        public SaveData Capture()
        {
            var d = new SaveData
            {
                version = Version,
                savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
                scene = "", bag = new SlotSave[0], chests = new ChestSave[0], openDoors = new string[0], leversOn = new string[0],
                clearedRooms = new string[0], visitedRooms = new string[0], pickups = new PickupSave[0],
                floorItems = new FloorItemSave[0], shops = new ShopSave[0]
            };
            var names = new List<string>();
            foreach (SaveParts p in Enum.GetValues(typeof(SaveParts)))
                if (p != SaveParts.None && p != SaveParts.All && Has(p)) names.Add(p.ToString());
            d.parts = names.ToArray();

            if (Has(SaveParts.Scene))
            {
                d.scene = SceneManager.GetActiveScene().name;
                RoomGenerator g = FindFirstObjectByType<RoomGenerator>();
                d.seed = g != null && g.Built ? g.UsedSeed : 0;
            }
            if (Has(SaveParts.Player) && player != null)
            {
                Rigidbody2D body = player.GetComponent<Rigidbody2D>();
                d.playerPosition = body != null ? body.position : (Vector2)player.position;
                d.playerHealth = PlayerHealth != null ? PlayerHealth.Current : 0;
            }
            Bag bag = PlayerBag;
            if (Has(SaveParts.Bag) && bag != null) d.bag = Slots(bag.Inventory);
            if (Has(SaveParts.Coins) && bag != null && coin != null) d.coins = bag.Inventory.Count(coin);

            var chests = new List<ChestSave>();
            var doors = new List<string>();
            var levers = new List<string>();
            var cleared = new List<string>();
            var visited = new List<string>();
            var pickups = new List<PickupSave>();
            var shops = new List<ShopSave>();
            foreach (var kv in ids)
            {
                UnityEngine.Object o = kv.Key;
                string id = kv.Value;
                if (Has(SaveParts.Chests) && o is Chest c && c != null)
                    chests.Add(new ChestSave { id = id, opened = c.Opened, slots = Slots(c.Bag.Inventory) });
                else if (Has(SaveParts.Doors) && o is LockedDoor ld && ld != null && ld.IsOpen) doors.Add(id);
                else if (Has(SaveParts.Doors) && o is Lever lv && lv != null && lv.on) levers.Add(id);
                else if (Has(SaveParts.Rooms) && o is RoomLock rl && rl != null && rl.Cleared) cleared.Add(id);
                else if (Has(SaveParts.Rooms) && o is Room r && r != null && r.Visited) visited.Add(id);
                else if (Has(SaveParts.Shops) && o is Shop s && s != null)
                {
                    var lines = new ShopLineSave[s.LineCount];
                    for (int i = 0; i < lines.Length; i++) lines[i] = new ShopLineSave { item = ItemName(s.Line(i).item), left = s.Left(i) };
                    shops.Add(new ShopSave { id = id, lines = lines });
                }
            }
            if (Has(SaveParts.Pickups))
            {
                foreach (var kv in startCounts)
                {
                    Pickup p = kv.Key;
                    string id = ids[p];
                    bool gone = p == null || taken.Contains(id);
                    if (gone) pickups.Add(new PickupSave { id = id, taken = true, count = 0 });
                    else if (p.stack.count != kv.Value) pickups.Add(new PickupSave { id = id, taken = false, count = p.stack.count });
                }
                var loose = new List<FloorItemSave>();
                foreach (Pickup p in Pickup.All)
                {
                    if (p == null || startCounts.ContainsKey(p) || p.stack.Empty) continue;
                    loose.Add(new FloorItemSave { item = ItemName(p.stack.item), count = p.stack.count, position = p.transform.position, auto = p.auto });
                }
                d.floorItems = loose.ToArray();
            }
            // A fixed order, so the same state always writes the same file.
            chests.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            doors.Sort(string.CompareOrdinal); levers.Sort(string.CompareOrdinal);
            cleared.Sort(string.CompareOrdinal); visited.Sort(string.CompareOrdinal);
            pickups.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            shops.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
            d.chests = chests.ToArray(); d.openDoors = doors.ToArray(); d.leversOn = levers.ToArray();
            d.clearedRooms = cleared.ToArray(); d.visitedRooms = visited.ToArray();
            d.pickups = pickups.ToArray(); d.shops = shops.ToArray();
            return d;
        }

        static string ItemName(Item i) => i != null ? i.name : "";

        static SlotSave[] Slots(Inventory inv)
        {
            var s = new SlotSave[inv.Size];
            for (int i = 0; i < s.Length; i++)
                s[i] = inv.slots[i].Empty ? new SlotSave { item = "", count = 0 } : new SlotSave { item = ItemName(inv.slots[i].item), count = inv.slots[i].count };
            return s;
        }

        // ---- apply ---------------------------------------------------------------------

        bool In(SaveData d, SaveParts p) => Has(p) && d.parts != null && Array.IndexOf(d.parts, p.ToString()) >= 0;

        // Puts a save on top of the scene as it started. Called on a fresh scene by Load.
        public void Apply(SaveData d)
        {
            if (In(d, SaveParts.Player) && player != null)
            {
                Rigidbody2D body = player.GetComponent<Rigidbody2D>();
                if (body != null) { body.position = d.playerPosition; body.linearVelocity = Vector2.zero; }
                player.position = new Vector3(d.playerPosition.x, d.playerPosition.y, player.position.z);
                if (PlayerHealth != null && d.playerHealth > 0) PlayerHealth.Set(d.playerHealth);
                Camera cam = Camera.main;
                CameraRig rig = cam != null ? cam.GetComponent<CameraRig>() : null;
                if (rig != null) rig.Snap();
            }
            Bag bag = PlayerBag;
            if (In(d, SaveParts.Bag) && bag != null) Fill(bag.Inventory, d.bag, "the bag");
            if (In(d, SaveParts.Coins) && bag != null && coin != null)
            {
                Inventory inv = bag.Inventory;
                inv.Remove(coin, inv.Count(coin));
                int lost = inv.Add(coin, d.coins);
                if (lost > 0) Debug.LogWarning("SaveGame: " + lost + " coins did not fit in the bag.", this);
            }
            if (In(d, SaveParts.Chests))
                foreach (ChestSave c in d.chests)
                {
                    Chest chest = Find<Chest>(c.id);
                    if (chest == null) { Missing(c.id); continue; }
                    chest.SetOpened(c.opened);
                    Fill(chest.Bag.Inventory, c.slots, c.id);
                }
            if (In(d, SaveParts.Doors))
            {
                foreach (string id in d.openDoors) { LockedDoor x = Find<LockedDoor>(id); if (x != null) x.Open(true); else Missing(id); }
                foreach (string id in d.leversOn) { Lever x = Find<Lever>(id); if (x != null) x.Set(true); else Missing(id); }
            }
            if (In(d, SaveParts.Rooms))
            {
                foreach (string id in d.clearedRooms) { RoomLock x = Find<RoomLock>(id); if (x != null) x.Clear(); else Missing(id); }
                foreach (string id in d.visitedRooms) { Room x = Find<Room>(id); if (x != null) x.Visited = true; else Missing(id); }
            }
            if (In(d, SaveParts.Pickups))
            {
                foreach (PickupSave p in d.pickups)
                {
                    Pickup x = Find<Pickup>(p.id);
                    if (x == null) { Missing(p.id); continue; }
                    if (p.taken) { taken.Add(p.id); x.gameObject.SetActive(false); Destroy(x.gameObject); }
                    else x.stack.count = Mathf.Max(0, p.count);
                }
                int order = 0;
                Hotbar hb = player != null ? player.GetComponent<Hotbar>() : null;
                if (hb != null) order = hb.dropSortingOrder;
                foreach (FloorItemSave f in d.floorItems)
                {
                    Item item = ItemNamed(f.item);
                    if (item == null || f.count <= 0) continue;
                    Pickup p = Pickup.Drop(item, f.count, f.position, order, 0f);
                    p.auto = f.auto;
                }
            }
            if (In(d, SaveParts.Shops))
                foreach (ShopSave s in d.shops)
                {
                    Shop shop = Find<Shop>(s.id);
                    if (shop == null) { Missing(s.id); continue; }
                    // By item, not by line number: a stock list reordered later still loads (trap 47).
                    foreach (ShopLineSave l in s.lines)
                    {
                        int i = shop.LineOf(ItemNamed(l.item));
                        if (i >= 0) shop.SetLeft(i, l.left);
                    }
                }
            Applied = true;
        }

        void Missing(string id) { Debug.LogWarning("SaveGame: the save names " + id + ", which this scene does not have (renamed or moved?).", this); }

        // Slot by slot; anything that no longer has a slot (the bag got smaller) goes in
        // wherever it fits, so nothing is dropped quietly.
        void Fill(Inventory inv, SlotSave[] saved, string what)
        {
            inv.Fit();
            for (int i = 0; i < inv.Size; i++) inv.slots[i] = default;
            var extra = new List<SlotSave>();
            for (int i = 0; i < saved.Length; i++)
            {
                if (string.IsNullOrEmpty(saved[i].item) || saved[i].count <= 0) continue;
                if (i < inv.Size)
                {
                    Item item = ItemNamed(saved[i].item);
                    if (item != null) inv.slots[i] = new ItemStack(item, saved[i].count);
                }
                else extra.Add(saved[i]);
            }
            foreach (SlotSave s in extra)
            {
                int lost = inv.Add(ItemNamed(s.item), s.count);
                if (lost > 0) Debug.LogWarning("SaveGame: " + lost + " x " + s.item + " did not fit in " + what + ".", this);
            }
        }

        // ---- slots ---------------------------------------------------------------------

        // Upgrade steps, run on EVERY file (trap 49), by what the values mean (trap 47).
        // Version 1 is the first shape: nothing to do yet. When version 2 changes the shape,
        // add here e.g.: if (!root.ContainsKey("newField")) { root["newField"] = ...; changed = true; }
        public static bool UpgradeSteps(Dictionary<string, object> root, int fileVersion)
        {
            bool changed = false;
            return changed;
        }

        public SaveFiles.State Peek(int slot, out SaveData data, out string note) =>
            SaveFiles.Read(SlotPath(slot), Version, UpgradeSteps, out data, out note);

        // Writes the slot. Returns "" or what went wrong.
        public string Save(int slot)
        {
            string why = CannotSave();
            if (why != "") return LastNote = why;
            if (slot < 0 || slot >= slots) return LastNote = "No such slot";
            string err = SaveFiles.Write(SlotPath(slot), Capture(), p => SaveFiles.ReadOne(p, Version, UpgradeSteps, out SaveData _, out _) == SaveFiles.State.Ok);
            if (err != "") { Debug.LogError("SaveGame: could not save slot " + (slot + 1) + ": " + err, this); return LastNote = "Could not save: " + err; }
            if (saveSettings) { LookAtSettings(); WriteSettings(lastSettings); }
            LastNote = "Saved to slot " + (slot + 1);
            AnySaved?.Invoke(this, slot);
            return "";
        }

        // Reads the slot and, if it is good, reloads its scene with the save on top.
        // Returns "" or what went wrong (then nothing changed).
        public string Load(int slot)
        {
            SaveFiles.State st = Peek(slot, out SaveData d, out string note);
            if (st == SaveFiles.State.Empty) return LastNote = "Slot " + (slot + 1) + " is empty";
            if (st == SaveFiles.State.Newer) return LastNote = "Slot " + (slot + 1) + " was " + note;
            if (st == SaveFiles.State.Broken) { Debug.LogWarning("SaveGame: slot " + (slot + 1) + ": " + note, this); return LastNote = "Slot " + (slot + 1) + " is broken"; }

            string scene = SceneManager.GetActiveScene().name;
            bool other = In(d, SaveParts.Scene) && !string.IsNullOrEmpty(d.scene) && d.scene != scene;
            if (other && !Application.CanStreamedLevelBeLoaded(d.scene)) return LastNote = "The saved scene " + d.scene + " is not in the build";
            pending = d;
            pendingScene = other ? d.scene : scene;
            pendingNote = st == SaveFiles.State.FromBackup ? "Loaded slot " + (slot + 1) + " (" + note + ")" : "Loaded slot " + (slot + 1);
            if (In(d, SaveParts.Scene) && d.seed != 0) RoomGenerator.NextSeed = d.seed;
            if (PauseMenu.Current != null) PauseMenu.Current.Resume();
            Time.timeScale = 1f;
            SceneManager.LoadScene(other ? d.scene : scene);
            return "";
        }

        // ---- settings ------------------------------------------------------------------

        static string SettingsText(SettingSave[] values)
        {
            var b = new System.Text.StringBuilder();
            foreach (SettingSave s in values) b.Append(s.key).Append('=').Append(s.value).Append('\n');
            return b.ToString();
        }

        static string Key(SavedSetting s) => s.target.GetType().Name + "." + s.field;

        FieldInfo FieldOf(SavedSetting s)
        {
            if (s.target == null || string.IsNullOrEmpty(s.field)) return null;
            FieldInfo f = s.target.GetType().GetField(s.field, BindingFlags.Instance | BindingFlags.Public);
            if (f == null) Debug.LogWarning("SaveGame: " + s.target.GetType().Name + " has no public field " + s.field + ".", this);
            return f;
        }

        SettingSave[] CaptureSettings()
        {
            var list = new List<SettingSave>();
            if (settings == null) return list.ToArray();
            foreach (SavedSetting s in settings)
            {
                FieldInfo f = FieldOf(s);
                if (f == null) continue;
                object v = f.GetValue(s.target);
                string text = v is IFormattable fm ? fm.ToString(null, CultureInfo.InvariantCulture) : v != null ? v.ToString() : "";
                if (f.FieldType.IsEnum) text = v.ToString();
                list.Add(new SettingSave { key = Key(s), value = text });
            }
            return list.ToArray();
        }

        void ReadSettings()
        {
            if (!File.Exists(SettingsPath) && !File.Exists(SaveFiles.BackupOf(SettingsPath))) return;
            SaveFiles.State st = SaveFiles.Read(SettingsPath, Version, null, out SettingsData d, out string note);
            if (st != SaveFiles.State.Ok && st != SaveFiles.State.FromBackup) { Debug.LogWarning("SaveGame: settings file not used: " + note, this); return; }
            foreach (SavedSetting s in settings)
            {
                FieldInfo f = FieldOf(s);
                if (f == null) continue;
                string key = Key(s);
                foreach (SettingSave v in d.values)
                {
                    if (v.key != key) continue;
                    try
                    {
                        Type t = f.FieldType;
                        object value = t.IsEnum ? Enum.Parse(t, v.value) : t == typeof(string) ? v.value
                            : Convert.ChangeType(v.value, t, CultureInfo.InvariantCulture);
                        f.SetValue(s.target, value);
                    }
                    catch (Exception) { Debug.LogWarning("SaveGame: setting " + key + " = " + v.value + " not used.", this); }
                }
            }
        }

        // Writes these values over the file's, keeping keys this scene does not have (a
        // scene without a minimap must not drop the minimap's setting).
        void WriteSettings(SettingSave[] values)
        {
            var merged = new List<SettingSave>();
            SaveFiles.State st = SaveFiles.Read(SettingsPath, Version, null, out SettingsData old, out _);
            if ((st == SaveFiles.State.Ok || st == SaveFiles.State.FromBackup) && old.values != null)
                foreach (SettingSave o in old.values)
                    if (Array.FindIndex(values, v => v.key == o.key) < 0) merged.Add(o);
            merged.AddRange(values);
            var d = new SettingsData { version = Version, values = merged.ToArray() };
            string err = SaveFiles.Write(SettingsPath, d, p => SaveFiles.ReadOne(p, Version, null, out SettingsData _, out _) == SaveFiles.State.Ok);
            if (err != "") Debug.LogWarning("SaveGame: settings not saved: " + err, this);
            else settingsAtStart = SettingsText(values);
        }
    }
}
