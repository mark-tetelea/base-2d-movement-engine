using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace TopDownKit
{
    // Reads a save file's JSON into a plain tree (objects, lists, strings, numbers, bools)
    // so it can be checked BEFORE JsonUtility sees it. JsonUtility quietly keeps a field's
    // starting value when the file does not mention it (trap 46), so a save with a whole
    // part cut out would load as if nothing were wrong. Check() refuses any object that
    // does not name every saved field of its type, at every depth, and skips fields that
    // are never saved (trap 48). Write() turns a tree back into text, for upgrade steps.
    public static class SaveJson
    {
        // ---- parse ------------------------------------------------------------------

        // Throws FormatException on anything that is not exactly one JSON value.
        public static object Parse(string text)
        {
            if (text == null) throw new FormatException("no text");
            int i = 0;
            object v = Value(text, ref i, 0);
            Space(text, ref i);
            if (i != text.Length) throw new FormatException("text after the end at " + i);
            return v;
        }

        static void Space(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

        static object Value(string s, ref int i, int depth)
        {
            if (depth > 64) throw new FormatException("nested too deep");
            Space(s, ref i);
            if (i >= s.Length) throw new FormatException("ends early");
            char c = s[i];
            if (c == '{')
            {
                var o = new Dictionary<string, object>();
                i++; Space(s, ref i);
                if (i < s.Length && s[i] == '}') { i++; return o; }
                while (true)
                {
                    Space(s, ref i);
                    if (i >= s.Length || s[i] != '"') throw new FormatException("expected a name at " + i);
                    string key = Str(s, ref i);
                    Space(s, ref i);
                    if (i >= s.Length || s[i] != ':') throw new FormatException("expected : at " + i);
                    i++;
                    o[key] = Value(s, ref i, depth + 1);
                    Space(s, ref i);
                    if (i < s.Length && s[i] == ',') { i++; continue; }
                    if (i < s.Length && s[i] == '}') { i++; return o; }
                    throw new FormatException("expected , or } at " + i);
                }
            }
            if (c == '[')
            {
                var l = new List<object>();
                i++; Space(s, ref i);
                if (i < s.Length && s[i] == ']') { i++; return l; }
                while (true)
                {
                    l.Add(Value(s, ref i, depth + 1));
                    Space(s, ref i);
                    if (i < s.Length && s[i] == ',') { i++; continue; }
                    if (i < s.Length && s[i] == ']') { i++; return l; }
                    throw new FormatException("expected , or ] at " + i);
                }
            }
            if (c == '"') return Str(s, ref i);
            if (Word(s, ref i, "true")) return true;
            if (Word(s, ref i, "false")) return false;
            if (Word(s, ref i, "null")) return null;
            int start = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            if (i == start || !double.TryParse(s.Substring(start, i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                throw new FormatException("bad value at " + start);
            return d;
        }

        static bool Word(string s, ref int i, string w)
        {
            if (string.CompareOrdinal(s, i, w, 0, w.Length) != 0) return false;
            i += w.Length;
            return true;
        }

        static string Str(string s, ref int i)
        {
            var b = new StringBuilder();
            i++;   // opening quote
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') return b.ToString();
                if (c != '\\') { b.Append(c); continue; }
                if (i >= s.Length) break;
                char e = s[i++];
                switch (e)
                {
                    case 'n': b.Append('\n'); break;
                    case 't': b.Append('\t'); break;
                    case 'r': b.Append('\r'); break;
                    case 'b': b.Append('\b'); break;
                    case 'f': b.Append('\f'); break;
                    case 'u':
                        if (i + 4 > s.Length) throw new FormatException("bad escape");
                        b.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        i += 4;
                        break;
                    default: b.Append(e); break;
                }
            }
            throw new FormatException("string never ends");
        }

        // ---- check the shape ----------------------------------------------------------

        // The fields JsonUtility saves: public or [SerializeField], instance, not
        // [NonSerialized] (trap 48), not readonly or const.
        public static IEnumerable<FieldInfo> SavedFields(Type t)
        {
            foreach (FieldInfo f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (f.IsNotSerialized || f.IsInitOnly || f.IsLiteral) continue;
                if (!f.IsPublic && f.GetCustomAttribute<SerializeField>() == null) continue;
                yield return f;
            }
        }

        // Adds a line to problems for every field the tree lacks or holds the wrong kind
        // of value for. Empty problems = the file names every field of type t.
        public static void Check(object node, Type t, string path, List<string> problems)
        {
            if (t == typeof(string)) { if (!(node is string)) problems.Add(path + ": expected text"); return; }
            if (t == typeof(bool)) { if (!(node is bool)) problems.Add(path + ": expected true/false"); return; }
            if (t.IsPrimitive || t.IsEnum) { if (!(node is double)) problems.Add(path + ": expected a number"); return; }
            Type element = t.IsArray ? t.GetElementType()
                : t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>) ? t.GetGenericArguments()[0] : null;
            if (element != null)
            {
                if (!(node is List<object> list)) { problems.Add(path + ": expected a list"); return; }
                for (int i = 0; i < list.Count; i++) Check(list[i], element, path + "[" + i + "]", problems);
                return;
            }
            if (!(node is Dictionary<string, object> obj)) { problems.Add(path + ": expected an object"); return; }
            foreach (FieldInfo f in SavedFields(t))
            {
                if (!obj.TryGetValue(f.Name, out object v)) { problems.Add(path + "." + f.Name + ": missing"); continue; }
                Check(v, f.FieldType, path + "." + f.Name, problems);
            }
        }

        // ---- write a tree back ----------------------------------------------------------

        public static string Write(object node)
        {
            var b = new StringBuilder();
            Write(node, b);
            return b.ToString();
        }

        static void Write(object node, StringBuilder b)
        {
            switch (node)
            {
                case null: b.Append("null"); break;
                case bool v: b.Append(v ? "true" : "false"); break;
                case double d: b.Append(d.ToString("R", CultureInfo.InvariantCulture)); break;
                case string s:
                    b.Append('"');
                    foreach (char c in s)
                    {
                        if (c == '"' || c == '\\') b.Append('\\').Append(c);
                        else if (c < ' ') b.Append("\\u").Append(((int)c).ToString("x4"));
                        else b.Append(c);
                    }
                    b.Append('"');
                    break;
                case Dictionary<string, object> o:
                {
                    b.Append('{');
                    bool first = true;
                    foreach (var kv in o)
                    {
                        if (!first) b.Append(',');
                        first = false;
                        Write(kv.Key, b);
                        b.Append(':');
                        Write(kv.Value, b);
                    }
                    b.Append('}');
                    break;
                }
                case IList l:
                {
                    b.Append('[');
                    for (int i = 0; i < l.Count; i++) { if (i > 0) b.Append(','); Write(l[i], b); }
                    b.Append(']');
                    break;
                }
                default: throw new FormatException("cannot write " + node.GetType().Name);
            }
        }
    }
}
