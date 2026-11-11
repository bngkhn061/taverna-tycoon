using System;
using System.Security.Cryptography;
using System.Text;

namespace Taverna
{
    /// <summary>
    /// Kayıt dosyası biçimi (v1.6): ilk satır başlık, ikinci satır JSON'un HMAC-SHA256 imzası (onaltılık), sonrası JSON.
    /// Anahtar uygulamaya gömülü sabit ile cihaz kimliğinden türetilir (<see cref="DeriveKey"/>).
    /// Bu, kayıt dosyasının elle düzenlenmesine karşı sıradan bir korumadır; kararlı bir saldırganı durdurmaz
    /// (anahtarın yarısı uygulamanın içinde). Dosya / Unity bağımlılığı yok: birim testleri doğrudan çağırır.
    /// </summary>
    public static class SaveCodec
    {
        /// <summary>Dosyanın ilk satırı (biçim sürümü).</summary>
        public const string Header = "TAVERNA-SAVE-1";

        /// <summary>Uygulamaya gömülü sabit (anahtarın cihazdan bağımsız yarısı). Değişirse eski kayıtlar açılmaz.</summary>
        const string EmbeddedSecret = "Barlo|Taverna|2026|meyhane-defteri|7f3c9a";

        const int SignatureHexLength = 64;

        /// <summary>Cihaz kimliği + gömülü sabitten 32 baytlık HMAC anahtarı (SHA-256).</summary>
        public static byte[] DeriveKey(string deviceId)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(EmbeddedSecret + "\n" + (deviceId ?? "")));
        }

        /// <summary>JSON'un HMAC-SHA256 imzası (küçük harf onaltılık).</summary>
        public static string Sign(string json, byte[] key)
        {
            using var h = new HMACSHA256(key);
            var mac = h.ComputeHash(Encoding.UTF8.GetBytes(json ?? ""));
            var sb = new StringBuilder(SignatureHexLength);
            foreach (var b in mac) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        /// <summary>Dosya içeriği: başlık + imza + JSON.</summary>
        public static string Encode(string json, byte[] key) => Header + "\n" + Sign(json, key) + "\n" + json;

        /// <summary>
        /// İçeriği çözer: başlık doğru ve imza tutuyorsa true ve JSON. Bozuk, kesilmiş, elle düzenlenmiş ya da başka
        /// cihazın anahtarıyla imzalanmış içerik false döner.
        /// </summary>
        public static bool TryDecode(string content, byte[] key, out string json)
        {
            json = null;
            if (string.IsNullOrEmpty(content)) return false;
            int a = content.IndexOf('\n');
            if (a < 0 || content.Substring(0, a).TrimEnd('\r') != Header) return false;
            int b = content.IndexOf('\n', a + 1);
            if (b < 0) return false;
            string sig = content.Substring(a + 1, b - a - 1).TrimEnd('\r');
            if (sig.Length != SignatureHexLength) return false;
            string body = content.Substring(b + 1);
            if (!FixedTimeEquals(sig, Sign(body, key))) return false;
            json = body;
            return true;
        }

        static bool FixedTimeEquals(string x, string y)
        {
            if (x.Length != y.Length) return false;
            int diff = 0;
            for (int i = 0; i < x.Length; i++) diff |= char.ToLowerInvariant(x[i]) ^ y[i];
            return diff == 0;
        }
    }
}
