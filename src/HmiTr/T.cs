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
        static Dictionary<string, bool> logged = new Dictionary<string, bool>();
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
                    }
                    string r;
                    if (map.TryGetValue(text, out r)) return r;
                    if (HasCjk(text) && !logged.ContainsKey(text))
                        { logged[text] = true; File.AppendAllText(Dir + "hmi.log", text + "\r\n", Encoding.UTF8); }
                }
            }
            catch { }
            return null;
        }

        static bool HasCjk(string s)
        {
            foreach (char c in s) if (c >= 0x2E80) return true;
            return false;
        }
    }
}

