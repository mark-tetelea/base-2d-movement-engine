using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace TopDownKit.EditorTools
{
    // The removable-systems check (phase 14, vital - keep it). Each system folder under
    // Assets/Scripts has its own assembly definition. For every optional system in turn it
    // compiles the kit's code WITHOUT that folder (and without the folders that say they
    // need it - their assembly definitions reference it), as one scratch assembly in Temp/.
    // Nothing in Assets is touched. Passing means: delete those folders and the rest still
    // compiles. A self-test first leaves a needed folder in and expects the compile to FAIL,
    // so a check that cannot fail is caught.
    // Run: menu TopDownKit > Check removable systems, or from the command line:
    //     unity command eval --code "TopDownKit.EditorTools.RemovalCheck.Start(); return 0;"
    //     then poll: unity command eval --code "return TopDownKit.EditorTools.RemovalCheck.Report;"
    public static class RemovalCheck
    {
        // Never removed: everything builds on them.
        public static readonly string[] Core = { "Core", "Movement", "Facing", "Camera", "Screens" };
        const string Prefix = "TopDownKit.";
        const string ScriptsRoot = "Assets/Scripts";
        const string OutDir = "Temp/RemovalCheck";

        class Sys
        {
            public string name;          // short name: "Combat", "Demos.Playground"
            public string folder;
            public List<string> refs = new List<string>();     // short names of kit systems it needs
            public List<string> packages = new List<string>(); // other assemblies it needs
            public List<string> sources = new List<string>();
        }

        class Job { public string label; public HashSet<string> removed; public bool expectFail; }

        static List<Sys> systems;
        static readonly Queue<Job> jobs = new Queue<Job>();
        static readonly StringBuilder log = new StringBuilder();
        static int passed, failed;
        static AssemblyBuilder running;
        static Job current;

        static bool busy;
        // True from Start until the summary line is written.
        public static bool Running => busy;
        // The result so far; ends with "ALL PASS" or "FAILED" when done.
        public static string Report => log.ToString();

        [MenuItem("TopDownKit/Check removable systems")]
        static void Menu() { Start(); }

        public static void Start()
        {
            if (busy) return;
            log.Clear(); passed = failed = 0;
            busy = true;
            systems = Find();
            if (systems.Count == 0) { log.AppendLine("No system folders found - FAILED"); busy = false; return; }
            log.AppendLine("Systems (" + systems.Count + "): " + string.Join(", ", systems.Select(s => s.name)));

            // Self-test: Combat gone but its dependants kept -> must not compile.
            Sys combat = systems.FirstOrDefault(s => s.name == "Combat");
            if (combat != null)
                jobs.Enqueue(new Job { label = "self-test: Combat removed, its users kept (must FAIL)", removed = new HashSet<string> { "Combat" }, expectFail = true });
            jobs.Enqueue(new Job { label = "nothing removed", removed = new HashSet<string>() });
            foreach (Sys s in systems)
            {
                if (Core.Contains(s.name)) continue;
                var gone = WithUsers(s.name);
                jobs.Enqueue(new Job { label = "remove " + s.name + (gone.Count > 1 ? " (+ " + string.Join(", ", gone.Where(g => g != s.name).OrderBy(g => g)) + ")" : ""), removed = gone });
            }
            EditorApplication.update -= Pump;
            EditorApplication.update += Pump;
        }

        // The system and every system that needs it, directly or through another.
        static HashSet<string> WithUsers(string name)
        {
            var gone = new HashSet<string> { name };
            bool grew = true;
            while (grew)
            {
                grew = false;
                foreach (Sys s in systems)
                    if (!gone.Contains(s.name) && s.refs.Any(gone.Contains)) { gone.Add(s.name); grew = true; }
            }
            return gone;
        }

        // What must go with each optional system (for the README and the log).
        public static string Users()
        {
            if (systems == null) systems = Find();
            var b = new StringBuilder();
            foreach (Sys s in systems)
            {
                if (Core.Contains(s.name)) continue;
                var g = WithUsers(s.name); g.Remove(s.name);
                b.AppendLine(s.name + ": " + (g.Count == 0 ? "nothing else" : string.Join(", ", g.OrderBy(x => x))));
            }
            return b.ToString();
        }

        static List<Sys> Find()
        {
            var list = new List<Sys>();
            var dirs = new Dictionary<string, Sys>();
            foreach (string path in Directory.GetFiles(ScriptsRoot, "*.asmdef", SearchOption.AllDirectories))
            {
                var data = JsonUtility.FromJson<AsmDef>(File.ReadAllText(path));
                if (data == null || string.IsNullOrEmpty(data.name) || !data.name.StartsWith(Prefix)) continue;
                var s = new Sys { name = data.name.Substring(Prefix.Length), folder = Path.GetDirectoryName(path).Replace('\\', '/') };
                foreach (string r in data.references ?? new string[0])
                {
                    if (r.StartsWith(Prefix)) s.refs.Add(r.Substring(Prefix.Length));
                    else s.packages.Add(r);
                }
                list.Add(s);
                dirs[s.folder] = s;
            }
            // Each .cs belongs to the nearest folder above it that has an assembly definition.
            foreach (string file in Directory.GetFiles(ScriptsRoot, "*.cs", SearchOption.AllDirectories))
            {
                string f = file.Replace('\\', '/');
                string d = Path.GetDirectoryName(f).Replace('\\', '/');
                while (d != null && d.Length >= ScriptsRoot.Length && !dirs.ContainsKey(d)) d = Path.GetDirectoryName(d)?.Replace('\\', '/');
                if (d != null && dirs.TryGetValue(d, out Sys owner)) owner.sources.Add(f);
                else log.AppendLine("  note: " + f + " is in no system folder");
            }
            return list;
        }

        [Serializable] class AsmDef { public string name; public string[] references; }

        static void Pump()
        {
            if (running != null) return;
            if (EditorApplication.isCompiling) return;
            if (jobs.Count == 0)
            {
                EditorApplication.update -= Pump;
                log.AppendLine((failed == 0 ? "ALL PASS" : "FAILED") + " (" + passed + " passed, " + failed + " failed)");
                Debug.Log("Removable systems check:\n" + log);
                busy = false;
                return;
            }
            current = jobs.Dequeue();
            var files = new List<string>();
            var packages = new HashSet<string>();
            foreach (Sys s in systems)
            {
                if (current.removed.Contains(s.name)) continue;
                files.AddRange(s.sources);
                foreach (string p in s.packages) packages.Add(p);
            }
            Directory.CreateDirectory(OutDir);
            string output = OutDir + "/Check" + (passed + failed) + ".dll";
            var ab = new AssemblyBuilder(output, files.ToArray());
            ab.referencesOptions = ReferencesOptions.UseEngineModules;
            // Only Unity and package assemblies: never the kit's own compiled ones, or a
            // missing type would quietly come from them.
            var compiled = Directory.GetFiles("Library/ScriptAssemblies", "*.dll");
            ab.excludeReferences = compiled.Where(p => Path.GetFileName(p).StartsWith(Prefix) || Path.GetFileName(p).StartsWith("Assembly-CSharp"))
                                           .Select(p => p.Replace('\\', '/')).ToArray();
            ab.additionalReferences = compiled.Where(p => packages.Contains(Path.GetFileNameWithoutExtension(p)))
                                              .Select(p => p.Replace('\\', '/')).ToArray();
            ab.buildFinished += Finished;
            running = ab;
            if (!ab.Build()) { running = null; jobs.Enqueue(current); }   // the editor is busy: try again
        }

        static void Finished(string path, CompilerMessage[] messages)
        {
            running = null;
            var errors = messages.Where(m => m.type == CompilerMessageType.Error).ToList();
            bool compiled = errors.Count == 0;
            bool ok = compiled != current.expectFail;
            if (ok) passed++; else failed++;
            log.AppendLine((ok ? "pass  " : "FAIL  ") + current.label + (compiled ? "" : " - " + errors.Count + " errors"));
            if (!ok)
                foreach (var e in errors.Take(5)) log.AppendLine("      " + e.message);
            if (current.expectFail && compiled) log.AppendLine("      the check compiled code that cannot compile - it is not checking anything");
        }
    }
}
