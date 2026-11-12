using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Taverna.EditorTools
{
    /// <summary>
    /// v1.7 yerelleştirme aracı: koddan ve UXML'den anahtarları toplar (<see cref="LocKeys"/>), her dil tablosundaki eksikleri,
    /// yer tutucu uyuşmazlıklarını ve artık kullanılmayan anahtarları raporlar; eksikleri Türkçe kaynakla
    /// <c>Library/LocMissing/&lt;dil&gt;.json</c> dosyalarına döker (çevirmene gönderilecek iş). Tablolara kendisi yazmaz.
    /// </summary>
    public static class TavernaLocTools
    {
        [MenuItem("Taverna/Yerelleştirme/Anahtarları Denetle")]
        public static void Audit()
        {
            string root = Path.Combine(Application.dataPath, "_Game");
            var keys = LocKeys.Collect(root);
            var report = new StringBuilder($"[Loc] {keys.Count} anahtar\n");
            string outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/LocMissing"));
            Directory.CreateDirectory(outDir);
            foreach (var code in Loc.Codes.Where(c => c != Loc.Source))
            {
                string path = Path.Combine(root, "Resources/Loc", code + ".json");
                var table = File.Exists(path) ? Loc.ParseTable(File.ReadAllText(path, Encoding.UTF8)) : new System.Collections.Generic.Dictionary<string, string>();
                var missing = keys.Keys.Where(k => !table.TryGetValue(k, out var v) || string.IsNullOrWhiteSpace(v)).ToList();
                var badPh = table.Where(kv => keys.ContainsKey(kv.Key) && !LocKeys.Placeholders(kv.Key).SequenceEqual(LocKeys.Placeholders(kv.Value))).Select(kv => kv.Key).ToList();
                var stale = table.Keys.Where(k => !keys.ContainsKey(k)).ToList();
                report.AppendLine($"{code}: {table.Count} çeviri · eksik {missing.Count} · yer tutucu hatası {badPh.Count} · artık {stale.Count}");
                var sb = new StringBuilder("{\n");
                for (int i = 0; i < missing.Count; i++)
                    sb.Append("  \"").Append(Esc(missing[i])).Append("\": \"").Append(Esc(missing[i])).Append(i < missing.Count - 1 ? "\",\n" : "\"\n");
                sb.Append("}\n");
                File.WriteAllText(Path.Combine(outDir, code + ".json"), sb.ToString(), new UTF8Encoding(false));
            }
            Debug.Log(report.ToString());
        }

        static string Esc(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
    }
}
