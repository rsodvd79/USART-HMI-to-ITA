using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HmiTr
{
    public static class T
    {
        const string Dir = @"c:\devel\";
        static readonly object Lock = new object();
        static Dictionary<string, string> map;
        static Dictionary<string, string> composed = new Dictionary<string, string>();
        static List<string> keys = new List<string>();
        static DateTime stamp;

        public static string Tr(string text)
        {
            try
            {
                if (string.IsNullOrEmpty(text)) return null;
                string file = Dir + "hmi_translation.txt";
                lock (Lock)
                {
                    if (!File.Exists(file)) return null;
                    DateTime w = File.GetLastWriteTimeUtc(file);
                    if (map == null || w != stamp)
                    {
                        var d = new Dictionary<string, string>();
                        foreach (var line in File.ReadAllLines(file, Encoding.UTF8))
                        {
                            var p = line.Split('\t');
                            if (p.Length >= 2 && p[0].Length > 0 && !d.ContainsKey(p[0])) d[p[0]] = p[1];
                        }
                        map = d;
                        stamp = w;
                        keys = new List<string>();
                        foreach (var k in d.Keys)
                        {
                            if (k.Length < 2 || !HasCjk(k)) continue;
                            int i = keys.Count;
                            keys.Add(k);
                            while (i > 0 && keys[i - 1].Length < k.Length) { keys[i] = keys[i - 1]; i--; }
                            keys[i] = k;
                        }
                        composed.Clear();
                    }
                    string r;
                    if (map.TryGetValue(text, out r)) return r;
                    if (!HasCjk(text)) return null;
                    if (!composed.TryGetValue(text, out r))
                    {
                        r = Compose(text);
                        composed[text] = r;
                        if (r == null || HasCjk(r))
                            File.AppendAllText(Dir + "hmi.log", text + "\r\n", Encoding.UTF8);
                    }
                    return r;
                }
            }
            catch { }
            return null;
        }

        // Messaggi composti a runtime (es. "错误:页面:..."): sostituisce le parti note.
        // Le stringhe delle tendine (Nome:0-a;1-b) vanno tradotte solo per intero.
        static string Compose(string text)
        {
            if (text.IndexOf('~') >= 0 || IsEnum(text)) return null;
            string r = text;
            foreach (var k in keys)
                if (r.IndexOf(k, StringComparison.Ordinal) >= 0) r = r.Replace(k, map[k]);
            return r == text ? null : r;
        }

        static bool IsEnum(string s)
        {
            for (int i = 0; i + 2 < s.Length; i++)
            {
                if (s[i] != ':' && s[i] != '：') continue;
                int j = i + 1;
                while (j < s.Length && s[j] >= '0' && s[j] <= '9') j++;
                if (j > i + 1 && j < s.Length && s[j] == '-') return true;
            }
            return false;
        }

        static bool HasCjk(string s)
        {
            foreach (char c in s) if (c >= 0x2E80) return true;
            return false;
        }
    }
}

