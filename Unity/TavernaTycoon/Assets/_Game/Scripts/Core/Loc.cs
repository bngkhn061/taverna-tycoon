using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Taverna
{
    /// <summary>
    /// v1.7 yerelleştirme: kaynak metin anahtarlı (gettext tarzı) sade sistem.
    /// Anahtar oyundaki Türkçe cümlenin kendisidir; <see cref="T"/> etkin dildeki çeviriyi, yoksa Türkçeyi döndürür.
    /// Çeviri tabloları <c>Resources/Loc/&lt;dil&gt;.json</c> (düz {"Türkçe": "çeviri"} nesnesi). Türkçe kaynak dil olduğu için tablosu yok.
    /// Kurallar (docs/localization.md): her cümle tek anahtar; parça birleştirme yok; Türkçe ek üreten yardımcılar
    /// yalnızca <see cref="IsTurkish"/> iken çalışır, diğer dillerde boş döner ve çeviri kalıbı eki gerektirmez.
    /// </summary>
    public static class Loc
    {
        /// <summary>Desteklenen dil kodları; sıra ayarlardaki seçicinin sırasıdır.</summary>
        public static readonly string[] Codes = { "tr", "en", "zh-Hans", "es", "pt-BR", "de", "fr", "ru", "ja", "ko" };
        /// <summary><see cref="Codes"/> ile aynı sırada, her dilin kendi dilindeki adı (seçicide gösterilir, çevrilmez).</summary>
        public static readonly string[] NativeNames = { "Türkçe", "English", "简体中文", "Español", "Português", "Deutsch", "Français", "Русский", "日本語", "한국어" };
        /// <summary>Kaynak dil (anahtarların dili).</summary>
        public const string Source = "tr";
        /// <summary>Sistem dili desteklenmiyorsa seçilen dil.</summary>
        public const string Fallback = "en";
        /// <summary>Çeviri tablolarının Resources altındaki klasörü.</summary>
        public const string ResourceDir = "Loc/";
        /// <summary>Test bayrağı: "-autotest-lang &lt;kod&gt;" dili bu süreç için seçer.</summary>
        public const string AutoTestLangArg = "-autotest-lang";

        static string _lang;
        static Dictionary<string, string> _table = new();
        static CultureInfo _culture = CultureInfo.InvariantCulture;

        /// <summary>Dil değişti (açık arayüzler metinlerini yeniden kurar).</summary>
        public static event Action Changed;

        /// <summary>Etkin dil kodu (<see cref="Codes"/>'tan biri).</summary>
        public static string Lang { get { EnsureInit(); return _lang; } }
        /// <summary>Etkin dil Türkçe mi? (Türkçe ek yardımcıları yalnızca o zaman ek üretir.)</summary>
        public static bool IsTurkish => Lang == Source;
        /// <summary>Etkin dilin kültürü (büyük harf dönüşümü için: Türkçede i → İ).</summary>
        public static CultureInfo Culture { get { EnsureInit(); return _culture; } }
        /// <summary>Etkin tablodaki çeviri sayısı (teşhis).</summary>
        public static int Count { get { EnsureInit(); return _table.Count; } }

        // ------------------------------------------------------------------ çeviri
        /// <summary>Türkçe kaynak metnin etkin dildeki karşılığı; çeviri yoksa ya da boşsa Türkçe metnin kendisi.</summary>
        public static string T(string tr)
        {
            if (string.IsNullOrEmpty(tr)) return tr;
            EnsureInit();
            if (_lang == Source) return tr;
            return _table.TryGetValue(tr, out var v) && !string.IsNullOrEmpty(v) ? v : tr;
        }

        /// <summary>Biçimli metin: önce <see cref="T"/>, sonra {0}… yer tutucuları doldurulur (değişmez kültür). Bozuk çeviride Türkçe kalıba düşer.</summary>
        public static string F(string tr, params object[] args)
        {
            var fmt = T(tr);
            try { return string.Format(CultureInfo.InvariantCulture, fmt, args); }
            catch (FormatException)
            {
                try { return string.Format(CultureInfo.InvariantCulture, tr, args); }
                catch (FormatException) { return tr; }
            }
        }

        /// <summary>
        /// Çevrilmeden döner: veri tablolarındaki (Catalog, Themes…) metinleri anahtar olarak işaretler.
        /// Anahtar toplayıcı bu çağrıları bulur; gösterirken <see cref="T"/> ile çevrilir.
        /// </summary>
        public static string N(string tr) => tr;

        /// <summary>Etkin dilin kurallarıyla büyük harf (Türkçede "i" → "İ").</summary>
        public static string Upper(string s) => string.IsNullOrEmpty(s) ? s : s.ToUpper(Culture);

        // ------------------------------------------------------------------ dil seçimi
        /// <summary>Kayıtlı tercih, test bayrağı ya da sistem dili; ilk kullanımda bir kez.</summary>
        static void EnsureInit()
        {
            if (_lang != null) return;
            string code = null;
            if (AutoTest.Enabled)
            {
                var args = Environment.GetCommandLineArgs();
                int i = Array.IndexOf(args, AutoTestLangArg);
                if (i >= 0 && i + 1 < args.Length) code = Normalize(args[i + 1]);
            }
            if (code == null) code = Normalize(GameSettings.Language);
            if (code == null) code = FromSystemLanguage(Application.systemLanguage);
            Apply(code, notify: false);
        }

        /// <summary>Oyuncunun seçimi: tercihi kaydeder, tabloyu yükler, yazı tipi sırasını ayarlar ve <see cref="Changed"/> yayınlar.</summary>
        public static void SetLanguage(string code)
        {
            code = Normalize(code) ?? Fallback;
            GameSettings.Language = code;
            if (code == _lang) return;
            Apply(code, notify: true);
        }

        static void Apply(string code, bool notify)
        {
            _lang = code;
            _table = code == Source ? new Dictionary<string, string>() : LoadTable(code);
            _culture = CultureFor(code);
            LocFonts.Apply(code);
            if (notify) Changed?.Invoke();
        }

        static Dictionary<string, string> LoadTable(string code)
        {
            var ta = Resources.Load<TextAsset>(ResourceDir + code);
            if (!ta) { Debug.LogWarning("[Loc] çeviri tablosu yok: " + code); return new Dictionary<string, string>(); }
            try { return ParseTable(ta.text); }
            catch (FormatException e) { Debug.LogError($"[Loc] {code}.json okunamadı: {e.Message}"); return new Dictionary<string, string>(); }
        }

        static CultureInfo CultureFor(string code)
        {
            try { return CultureInfo.GetCultureInfo(code == "zh-Hans" ? "zh-CN" : code); }
            catch (CultureNotFoundException) { return CultureInfo.InvariantCulture; }
        }

        /// <summary>Eski kayıtlı değerleri ("TR", "EN") ve büyük/küçük harf farkını desteklenen koda çevirir; tanınmazsa null.</summary>
        public static string Normalize(string code)
        {
            if (string.IsNullOrEmpty(code)) return null;
            foreach (var c in Codes) if (string.Equals(c, code, StringComparison.OrdinalIgnoreCase)) return c;
            var low = code.ToLowerInvariant();
            if (low == "zh" || low == "zh-cn" || low == "zh-hans-cn") return "zh-Hans";
            if (low == "pt" || low == "pt-pt") return "pt-BR";
            return null;
        }

        /// <summary>İlk açılış: sistem dilinden desteklenen dil; eşleşme yoksa İngilizce. (Geleneksel Çince basitleştirilmişe eşlenir.)</summary>
        public static string FromSystemLanguage(SystemLanguage sl) => sl switch
        {
            SystemLanguage.Turkish => "tr",
            SystemLanguage.English => "en",
            SystemLanguage.Chinese or SystemLanguage.ChineseSimplified or SystemLanguage.ChineseTraditional => "zh-Hans",
            SystemLanguage.Spanish => "es",
            SystemLanguage.Portuguese => "pt-BR",
            SystemLanguage.German => "de",
            SystemLanguage.French => "fr",
            SystemLanguage.Russian => "ru",
            SystemLanguage.Japanese => "ja",
            SystemLanguage.Korean => "ko",
            _ => Fallback,
        };

        /// <summary>Kodun <see cref="Codes"/> içindeki sırası (yoksa -1).</summary>
        public static int IndexOf(string code) => Array.IndexOf(Codes, Normalize(code));

        /// <summary>Test: tabloyu dışarıdan verir (Resources olmadan).</summary>
        public static void SetTableForTests(string code, Dictionary<string, string> table)
        {
            _lang = code;
            _table = table ?? new Dictionary<string, string>();
            _culture = CultureFor(code);
        }

        // ------------------------------------------------------------------ UXML ağacı
        sealed class Entry { public string src, applied; }
        static readonly ConditionalWeakTable<TextElement, Entry> _uxml = new();

        /// <summary>
        /// UXML'den gelen sabit metinleri çevirir. İlk çağrıda ağaçtaki dolu Label / Button metinleri kaynak olarak kaydedilir
        /// (UXML'leri elle düzenlemeden). Sonraki çağrılarda (dil değişimi) yalnızca kayıtlı ve kod tarafından değiştirilmemiş
        /// metinler yenilenir; kodun yazdığı metinleri kendi kurucuları yeniler.
        /// </summary>
        public static void Localize(VisualElement root)
        {
            if (root == null) return;
            root.Query<TextElement>().ForEach(te =>
            {
                if (_uxml.TryGetValue(te, out var e))
                {
                    if (te.text != e.applied) return;   // kod bu metni değiştirmiş: dokunma
                    e.applied = T(e.src);
                    te.text = e.applied;
                }
                else if (!string.IsNullOrEmpty(te.text))
                {
                    e = new Entry { src = te.text };
                    e.applied = T(e.src);
                    te.text = e.applied;
                    _uxml.Add(te, e);
                }
            });
        }

        // ------------------------------------------------------------------ JSON
        /// <summary>Düz bir {"anahtar": "değer"} JSON nesnesini okur (iç içe yapı yok). Bozuk girdide FormatException.</summary>
        public static Dictionary<string, string> ParseTable(string json)
        {
            var d = new Dictionary<string, string>();
            int i = 0;
            Ws(json, ref i);
            Expect(json, ref i, '{');
            Ws(json, ref i);
            if (i < json.Length && json[i] == '}') return d;
            while (true)
            {
                Ws(json, ref i);
                var k = Str(json, ref i);
                Ws(json, ref i);
                Expect(json, ref i, ':');
                Ws(json, ref i);
                var v = Str(json, ref i);
                d[k] = v;
                Ws(json, ref i);
                if (i >= json.Length) throw new FormatException("beklenmeyen son");
                if (json[i] == ',') { i++; continue; }
                if (json[i] == '}') break;
                throw new FormatException("',' ya da '}' bekleniyordu, konum " + i);
            }
            return d;
        }

        static void Ws(string s, ref int i) { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }

        static void Expect(string s, ref int i, char c)
        {
            if (i >= s.Length || s[i] != c) throw new FormatException($"'{c}' bekleniyordu, konum {i}");
            i++;
        }

        static string Str(string s, ref int i)
        {
            Expect(s, ref i, '"');
            var sb = new StringBuilder();
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"') return sb.ToString();
                if (c != '\\') { sb.Append(c); continue; }
                if (i >= s.Length) break;
                char e = s[i++];
                switch (e)
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u':
                        if (i + 4 > s.Length) throw new FormatException("kısa \\u");
                        sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16));
                        i += 4;
                        break;
                    default: sb.Append(e); break;
                }
            }
            throw new FormatException("kapanmamış metin");
        }
    }
}
