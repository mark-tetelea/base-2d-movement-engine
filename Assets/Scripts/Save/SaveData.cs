using System;
using UnityEngine;

namespace TopDownKit
{
    // What a save slot holds. The shape is versioned: change any field here (add, remove,
    // rename, change its meaning) -> raise SaveGame.Version and add an upgrade step in
    // SaveGame.UpgradeSteps that brings older files to the new shape by what their values
    // mean (traps 46, 47). Every field must be in the file; a file missing one is refused.
    // Objects in the scene are named by their path in the hierarchy (SaveGame.IdOf).
    [Serializable]
    public class SaveData
    {
        public int version;
        public string savedAt;
        [Tooltip("Which parts were saved (SaveParts names). Parts not listed are left as the scene starts.")]
        public string[] parts;

        // Scene
        public string scene;
        [Tooltip("The RoomGenerator's seed (0 = none), so a generated level is rebuilt the same.")]
        public int seed;

        // Player
        public Vector2 playerPosition;
        public int playerHealth;

        // Bag (the player's) and coins (the coin item's count in it)
        public SlotSave[] bag;
        public int coins;

        // Chests
        public ChestSave[] chests;

        // Doors: key doors opened, levers that are on
        public string[] openDoors;
        public string[] leversOn;

        // Rooms: cleared (RoomLock) and visited (Room.Visited, for the minimap)
        public string[] clearedRooms;
        public string[] visitedRooms;

        // Pickups: placed pickups taken or partly taken, and items lying loose
        public PickupSave[] pickups;
        public FloorItemSave[] floorItems;

        // Shops: what is left of each stock line
        public ShopSave[] shops;
    }

    [Serializable] public struct SlotSave { public string item; public int count; }

    [Serializable] public struct ChestSave { public string id; public bool opened; public SlotSave[] slots; }

    [Serializable] public struct PickupSave { public string id; public bool taken; public int count; }

    [Serializable] public struct FloorItemSave { public string item; public int count; public Vector2 position; public bool auto; }

    [Serializable] public struct ShopLineSave { public string item; public int left; }

    [Serializable] public struct ShopSave { public string id; public ShopLineSave[] lines; }

    // The settings file (one for all slots): chosen fields of chosen components, as text.
    [Serializable]
    public class SettingsData
    {
        public int version;
        public SettingSave[] values;
    }

    [Serializable] public struct SettingSave { public string key; public string value; }
}
