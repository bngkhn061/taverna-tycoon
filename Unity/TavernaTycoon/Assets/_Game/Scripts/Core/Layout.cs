using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v2.3 masa ve dekor düzenleme modu (GAME_DESIGN.md §17): kayıttaki yerleşim tercihleri.
    /// Her taşınmış nesne için bir satır: <c>"anahtar|x|z|çeyrek"</c> (anahtar: masa "t:Table_E", dekor yuvası "d:12";
    /// x / z dünya konumu (m), çeyrek: 90°'lik dönüş sayısı 0–3). Varsayılan yerindeki nesnenin satırı yoktur.
    /// Satır yoksa nesne varsayılan yuvasındadır (eski kayıtlar ve yeni alınan masa / dekor).
    /// Prestijte silinmez: bölgesi kapalı nesnenin satırı bekler, bölge (ya da masa) açılınca uygulanır.
    /// Unity sahnesine bağımlı değil (birim testli).
    /// </summary>
    public static class Layout
    {
        /// <summary>Yerleşim biçiminin sürümü (<see cref="SaveData.layoutVersion"/>).</summary>
        public const int Version = 1;
        /// <summary>Düzenleme modunun açıldığı taverna seviyesi (seviye kartında "Düzenleme modu").</summary>
        public const int UnlockLevel = 5;
        /// <summary>Izgara adımı (m): sürüklenen nesnenin konumu bu adıma oturur.</summary>
        public const float GridStep = 0.25f;

        /// <summary>Bir nesnenin yerleşimi: dünya XZ konumu ve 90°'lik dönüş sayısı.</summary>
        public struct Pose
        {
            public float x, z;
            public int quarter;
            public Pose(float x, float z, int quarter) { this.x = x; this.z = z; this.quarter = ((quarter % 4) + 4) % 4; }
            public Vector2 Pos => new(x, z);
            /// <summary>Konumlar ızgara hassasiyetinde (1 cm) aynı mı?</summary>
            public bool Same(Pose o) => Mathf.Abs(x - o.x) < 0.01f && Mathf.Abs(z - o.z) < 0.01f && quarter == o.quarter;
        }

        /// <summary>Masa anahtarı (sahnedeki masa adı).</summary>
        public static string TableKey(string tableName) => "t:" + tableName;
        /// <summary>Dekor anahtarı (TavernRegistry dekor yuvası indeksi; kayıttaki "eşya:yuva" ile aynı yuva).</summary>
        public static string DecorKey(int slot) => "d:" + slot;

        /// <summary>Değeri ızgaraya oturtur.</summary>
        public static float Snap(float v) => Mathf.Round(v / GridStep) * GridStep;

        /// <summary>Satırı yazar: "anahtar|x|z|çeyrek" (nokta ondalık, kültürden bağımsız).</summary>
        public static string Format(string key, Pose p) =>
            key + "|" + p.x.ToString("0.###", CultureInfo.InvariantCulture) + "|" + p.z.ToString("0.###", CultureInfo.InvariantCulture) + "|" + p.quarter;

        /// <summary>Satırı okur; bozuk satır false döner (yok sayılır).</summary>
        public static bool TryParse(string line, out string key, out Pose pose)
        {
            key = null;
            pose = default;
            if (string.IsNullOrEmpty(line)) return false;
            var parts = line.Split('|');
            if (parts.Length != 4 || parts[0].Length == 0) return false;
            if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)) return false;
            if (!float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float z)) return false;
            if (!int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int q)) return false;
            if (float.IsNaN(x) || float.IsNaN(z) || float.IsInfinity(x) || float.IsInfinity(z)) return false;
            key = parts[0];
            pose = new Pose(x, z, q);
            return true;
        }

        /// <summary>Kayıttaki yerleşim (yoksa false: varsayılan yuva).</summary>
        public static bool TryGet(SaveData d, string key, out Pose pose)
        {
            pose = default;
            if (d?.layout == null) return false;
            foreach (var line in d.layout)
                if (TryParse(line, out var k, out var p) && k == key) { pose = p; return true; }
            return false;
        }

        /// <summary>Yerleşimi yazar (aynı anahtarın eski satırı silinir).</summary>
        public static void Set(SaveData d, string key, Pose pose)
        {
            Remove(d, key);
            d.layout.Add(Format(key, pose));
        }

        /// <summary>Anahtarın satırını siler (nesne varsayılan yuvasına döner).</summary>
        public static void Remove(SaveData d, string key)
        {
            if (d?.layout == null) return;
            d.layout.RemoveAll(line => !TryParse(line, out var k, out _) || k == key);
        }

        /// <summary>"Varsayılana dön": bütün yerleşim satırları silinir (kapalı bölgelerdekiler de).</summary>
        public static void Clear(SaveData d) => d?.layout?.Clear();

        /// <summary>
        /// Kayıt yükseltmesi: yerleşim alanı olmayan eski kayıt (saveVersion &lt; 4) boş yerleşimle (varsayılan düzen) açılır;
        /// bozuk satırlar atılır, sürüm alanı yazılır.
        /// </summary>
        public static void Migrate(SaveData d)
        {
            d.layout ??= new List<string>();
            if (d.layoutVersion <= 0) { d.layout.Clear(); d.layoutVersion = Version; }
            d.layout.RemoveAll(line => !TryParse(line, out _, out _));
        }
    }
}
