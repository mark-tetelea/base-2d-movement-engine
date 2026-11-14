using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TopDownKit
{
    // Reading and writing save files safely (traps 46-50, 53). Plain C#: checks call it
    // directly.
    //  - Read: parse -> version -> upgrade steps -> every field present -> JsonUtility.
    //    A broken main file falls back to its backup; nothing throws, nothing is deleted.
    //  - Write: text to a temp file, read it back, then the current file (if it reads as a
    //    good save) becomes the backup, then the temp file replaces it. A file that does
    //    NOT read as good (broken, or from a newer game) is never overwritten and never
    //    made the backup: it is set aside as "<name>.kept-<time>.json" so the last good
    //    backup survives and nothing the player made is lost.
    public static class SaveFiles
    {
        public enum State { Ok, FromBackup, Empty, Broken, Newer }

        // Checks point this at a scratch folder so they never touch real saves (trap 50).
        public static string FolderOverride;

        public static string Folder(string sub) =>
            !string.IsNullOrEmpty(FolderOverride) ? FolderOverride : Path.Combine(Application.persistentDataPath, sub);

        public static string BackupOf(string path) => Path.ChangeExtension(path, ".bak.json");

        // An upgrade step edits the parsed tree of a file whose version is older, by what
        // its values MEAN (trap 47), and must be safe to run on any file (trap 49). It
        // returns whether it changed anything.
        public delegate bool Upgrade(Dictionary<string, object> root, int fileVersion);

        // ---- read ----------------------------------------------------------------------

        // Reads path, or its backup when path is broken. Never throws.
        public static State Read<T>(string path, int version, Upgrade upgrade, out T data, out string note) where T : class
        {
            data = null;
            note = "";
            bool mainExists = File.Exists(path);
            State main = mainExists ? ReadOne(path, version, upgrade, out data, out note) : State.Empty;
            if (main == State.Ok) return State.Ok;
            string bak = BackupOf(path);
            if (main != State.Newer && File.Exists(bak))
            {
                State b = ReadOne(bak, version, upgrade, out T fromBak, out string bakNote);
                if (b == State.Ok)
                {
                    data = fromBak;
                    note = (mainExists ? "save file broken (" + note + "), " : "save file missing, ") + "loaded the backup";
                    return State.FromBackup;
                }
                if (!mainExists) { note = "backup broken (" + bakNote + ")"; return State.Broken; }
            }
            return main;
        }

        // Reads one file. Never throws.
        public static State ReadOne<T>(string path, int version, Upgrade upgrade, out T data, out string note) where T : class
        {
            data = null;
            note = "";
            string text;
            try { text = File.ReadAllText(path); }
            catch (Exception e) { note = "cannot read: " + e.Message; return File.Exists(path) ? State.Broken : State.Empty; }
            return FromText(text, version, upgrade, out data, out note);
        }

        public static State FromText<T>(string text, int version, Upgrade upgrade, out T data, out string note) where T : class
        {
            data = null;
            note = "";
            try
            {
                if (string.IsNullOrWhiteSpace(text)) { note = "empty file"; return State.Broken; }
                if (!(SaveJson.Parse(text) is Dictionary<string, object> root)) { note = "not a save"; return State.Broken; }
                if (!root.TryGetValue("version", out object v) || !(v is double d) || d != Math.Floor(d) || d < 1)
                {
                    note = "no version number";
                    return State.Broken;
                }
                int fileVersion = (int)d;
                if (fileVersion > version) { note = "made by a newer version (" + fileVersion + ")"; return State.Newer; }
                // Every file goes through the upgrade steps, not only older ones (trap 49).
                if (upgrade != null && upgrade(root, fileVersion)) text = SaveJson.Write(root);
                var problems = new List<string>();
                SaveJson.Check(root, typeof(T), "save", problems);
                if (problems.Count > 0)
                {
                    note = problems.Count + " problem(s): " + string.Join("; ", problems.GetRange(0, Mathf.Min(3, problems.Count)));
                    return State.Broken;
                }
                data = JsonUtility.FromJson<T>(text);
                if (data == null) { note = "not a save"; return State.Broken; }
                return State.Ok;
            }
            catch (Exception e)
            {
                data = null;
                note = e.Message;
                return State.Broken;
            }
        }

        // ---- write ---------------------------------------------------------------------

        // Writes data to path safely. Returns "" on success, else what went wrong (the old
        // file is then untouched). isGood tells whether a file on disk reads as a good save.
        public static string Write<T>(string path, T data, Func<string, bool> isGood)
        {
            string tmp = path + ".tmp";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(tmp, json);
                if (File.ReadAllText(tmp) != json) { TryDelete(tmp); return "the written file did not read back the same"; }

                if (File.Exists(path))
                {
                    if (isGood(path)) File.Copy(path, BackupOf(path), true);
                    else File.Move(path, Path.ChangeExtension(path, ".kept-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".json"));
                }
                if (File.Exists(path))
                {
                    try { File.Replace(tmp, path, null); }
                    catch (Exception) { File.Copy(tmp, path, true); TryDelete(tmp); }
                }
                else File.Move(tmp, path);
                return "";
            }
            catch (Exception e)
            {
                TryDelete(tmp);
                return e.Message;
            }
        }

        static void TryDelete(string p) { try { if (File.Exists(p)) File.Delete(p); } catch (Exception) { } }
    }
}
