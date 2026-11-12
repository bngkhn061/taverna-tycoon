#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Taverna
{
    /// <summary>
    /// v1.7 anahtar toplayıcı (yalnızca editör): kaynak koddaki <c>Loc.T("…")</c>, <c>Loc.F("…")</c>, <c>Loc.N("…")</c> çağrılarının
    /// metinleri ve UXML'lerdeki <c>text="…"</c> değerleri (Loc.Localize'ın çevirdikleri). Veri tabloları (Catalog, Themes, TavernaData…)
    /// metinlerini Loc.N ile işaretler, böylece bu toplayıcı onları da bulur. Birim testi ve "Taverna/Yerelleştirme" menüsü kullanır.
    /// </summary>
    public static class LocKeys
    {
        static readonly Regex Call = new(@"Loc\.(?:T|F|N)\(\s*""((?:[^""\\]|\\.)*)""", RegexOptions.Compiled);
        static readonly Regex UxmlText = new(@"\btext=""([^""]*)""", RegexOptions.Compiled);
        static readonly Regex HasLetter = new(@"\p{L}", RegexOptions.Compiled);
        /// <summary>Taranmayan dosyalar (sistemin kendisi).</summary>
        static readonly HashSet<string> SkipFiles = new() { "Loc.cs", "LocKeys.cs" };

        /// <summary>Anahtar → ilk bulunduğu yer ("Dosya.cs:satır"). <paramref name="gameRoot"/> = Assets/_Game klasörü.</summary>
        public static Dictionary<string, string> Collect(string gameRoot)
        {
            var keys = new Dictionary<string, string>();
            var files = new List<string>(Directory.GetFiles(gameRoot, "*.*", SearchOption.AllDirectories));
            files.Sort(System.StringComparer.Ordinal);
            foreach (var path in files)
            {
                string name = Path.GetFileName(path);
                bool cs = path.EndsWith(".cs"), uxml = path.EndsWith(".uxml");
                if ((!cs && !uxml) || SkipFiles.Contains(name)) continue;
                var lines = File.ReadAllLines(path, Encoding.UTF8);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (cs)
                        foreach (Match m in Call.Matches(lines[i]))
                        {
                            var k = Regex.Unescape(m.Groups[1].Value);
                            if (!keys.ContainsKey(k)) keys[k] = $"{name}:{i + 1}";
                        }
                    else
                        foreach (Match m in UxmlText.Matches(lines[i]))
                        {
                            var k = System.Net.WebUtility.HtmlDecode(m.Groups[1].Value);
                            if (HasLetter.IsMatch(k) && !keys.ContainsKey(k)) keys[k] = $"{name}:{i + 1}";
                        }
                }
            }
            return keys;
        }

        /// <summary>Yer tutucu numaraları ({0}, {1:0.0} → "0","1"), sıralı. Test: çeviri kaynakla aynı kümeyi kullanmalı.</summary>
        public static List<string> Placeholders(string s)
        {
            var r = new List<string>();
            foreach (Match m in Regex.Matches(s, @"\{(\d+)(?:[:,][^}]*)?\}")) r.Add(m.Groups[1].Value);
            r.Sort(System.StringComparer.Ordinal);
            return r;
        }
    }
}
#endif
