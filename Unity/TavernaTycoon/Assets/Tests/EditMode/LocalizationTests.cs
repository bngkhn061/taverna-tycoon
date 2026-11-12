using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Taverna.Tests
{
    /// <summary>
    /// v1.7 yerelleştirme: çeviri tabloları kaynak koddaki bütün anahtarları içeriyor mu, yer tutucular aynı mı, boş çeviri var mı;
    /// Loc'un saf parçaları (JSON okuma, dil eşleme, geri düşme). Anahtar toplayıcı: <see cref="LocKeys"/>.
    /// </summary>
    public class LocalizationTests
    {
        static string GameRoot => Path.Combine(Application.dataPath, "_Game");
        static string TableDir => Path.Combine(GameRoot, "Resources", "Loc");

        static Dictionary<string, string> _keys;
        static Dictionary<string, string> Keys => _keys ??= LocKeys.Collect(GameRoot);

        static Dictionary<string, string> Table(string code) =>
            Loc.ParseTable(File.ReadAllText(Path.Combine(TableDir, code + ".json"), System.Text.Encoding.UTF8));

        static IEnumerable<string> Targets => Loc.Codes.Where(c => c != Loc.Source);

        [TearDown]
        public void Restore() => Loc.SetTableForTests(Loc.Source, null);

        [Test]
        public void Collector_FindsCodeDataAndUxmlKeys()
        {
            Assert.Greater(Keys.Count, 500, "anahtar toplayıcı beklenenden az anahtar buldu");
            Assert.IsTrue(Keys.ContainsKey("Yeterli altın yok"), "Loc.T çağrısı");
            Assert.IsTrue(Keys.ContainsKey("Seviye {0} gerekli"), "Loc.F çağrısı");
            Assert.IsTrue(Keys.ContainsKey("Kristal Avize"), "Loc.N veri tablosu");
            Assert.IsTrue(Keys.ContainsKey("Bira"), "TavernaData içecek adı");
            Assert.IsTrue(Keys.ContainsKey("AYARLAR"), "UXML metni");
        }

        [Test]
        public void EveryLanguage_HasEveryKey_NonEmpty([ValueSource(nameof(Targets))] string code)
        {
            var t = Table(code);
            var missing = Keys.Keys.Where(k => !t.TryGetValue(k, out var v) || string.IsNullOrWhiteSpace(v)).ToList();
            Assert.IsEmpty(missing, $"{code}: {missing.Count} eksik/boş çeviri, ilki: {missing.FirstOrDefault()} ({(missing.Count > 0 ? Keys[missing[0]] : "")})");
        }

        [Test]
        public void EveryLanguage_PlaceholdersMatchSource([ValueSource(nameof(Targets))] string code)
        {
            var t = Table(code);
            var bad = new List<string>();
            foreach (var kv in t)
                if (Keys.ContainsKey(kv.Key) && !LocKeys.Placeholders(kv.Key).SequenceEqual(LocKeys.Placeholders(kv.Value)))
                    bad.Add($"{kv.Key} → {kv.Value}");
            Assert.IsEmpty(bad, $"{code}: yer tutucu uyuşmazlığı: {string.Join(" | ", bad.Take(3))}");
        }

        [Test]
        public void EveryLanguage_HasNoStaleKeys([ValueSource(nameof(Targets))] string code)
        {
            var stale = Table(code).Keys.Where(k => !Keys.ContainsKey(k)).ToList();
            Assert.IsEmpty(stale, $"{code}: kodda olmayan anahtar: {stale.FirstOrDefault()}");
        }

        [Test]
        public void EveryLanguage_FormatsWithoutException([ValueSource(nameof(Targets))] string code)
        {
            var args = new object[] { 1, 2, 3, 4 };
            foreach (var kv in Table(code))
                Assert.DoesNotThrow(() => string.Format(System.Globalization.CultureInfo.InvariantCulture, kv.Value, args), $"{code}: {kv.Value}");
        }

        [Test]
        public void ParseTable_ReadsEscapesAndUnicode()
        {
            var d = Loc.ParseTable("{ \"a\\\"b\": \"x\\ny\", \"\\u00e7\": \"\\u4e2d\" , \"k\":\"\"}");
            Assert.AreEqual("x\ny", d["a\"b"]);
            Assert.AreEqual("中", d["ç"]);
            Assert.AreEqual("", d["k"]);
            Assert.Throws<System.FormatException>(() => Loc.ParseTable("{\"a\": 1}"));
        }

        [Test]
        public void T_FallsBackToTurkish_F_FormatsAndSurvivesBrokenTranslation()
        {
            Loc.SetTableForTests("en", new Dictionary<string, string> { ["Seviye {0}"] = "Level {0}", ["Bozuk {0}"] = "Broken {1}" });
            Assert.AreEqual("Level 5", Loc.F("Seviye {0}", 5));
            Assert.AreEqual("Çevrilmemiş", Loc.T("Çevrilmemiş"));
            Assert.AreEqual("Bozuk 3", Loc.F("Bozuk {0}", 3), "bozuk çeviri Türkçe kalıba düşmeli");
            Assert.IsFalse(Loc.IsTurkish);
            Loc.SetTableForTests("tr", null);
            Assert.AreEqual("Seviye 5", Loc.F("Seviye {0}", 5));
        }

        [Test]
        public void SystemLanguage_MapsToSupported_ElseEnglish()
        {
            Assert.AreEqual("tr", Loc.FromSystemLanguage(SystemLanguage.Turkish));
            Assert.AreEqual("zh-Hans", Loc.FromSystemLanguage(SystemLanguage.ChineseSimplified));
            Assert.AreEqual("pt-BR", Loc.FromSystemLanguage(SystemLanguage.Portuguese));
            Assert.AreEqual("ja", Loc.FromSystemLanguage(SystemLanguage.Japanese));
            Assert.AreEqual("en", Loc.FromSystemLanguage(SystemLanguage.Finnish));
            Assert.AreEqual("tr", Loc.Normalize("TR"), "eski kayıt değeri");
            Assert.AreEqual("en", Loc.Normalize("EN"));
            Assert.IsNull(Loc.Normalize("xx"));
            Assert.AreEqual(Loc.Codes.Length, Loc.NativeNames.Length);
        }

        [Test]
        public void Upper_UsesLanguageCulture()
        {
            Loc.SetTableForTests("tr", null);
            Assert.AreEqual("İZMİR", Loc.Upper("izmir"));
            Loc.SetTableForTests("en", new Dictionary<string, string>());
            Assert.AreEqual("IZMIR", Loc.Upper("izmir"));
        }

        [Test]
        public void CjkFallback_ActiveLanguageFirst()
        {
            var s = ScriptableObject.CreateInstance<LocFontSet>();
            s.sc = ScriptableObject.CreateInstance<UnityEngine.TextCore.Text.FontAsset>();
            s.jp = ScriptableObject.CreateInstance<UnityEngine.TextCore.Text.FontAsset>();
            s.kr = ScriptableObject.CreateInstance<UnityEngine.TextCore.Text.FontAsset>();
            Assert.AreSame(s.jp, LocFonts.Order("ja", s)[0]);
            Assert.AreSame(s.kr, LocFonts.Order("ko", s)[0]);
            Assert.AreSame(s.sc, LocFonts.Order("zh-Hans", s)[0]);
            Assert.AreSame(s.sc, LocFonts.Order("en", s)[0]);
        }
    }
}
