using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Taverna.Tests
{
    /// <summary>Kayıt imzası (v1.6): yaz → oku, bozulmuş / başka anahtarla imzalanmış içerik reddedilir. Dosya işlemi yok.</summary>
    public class SaveCodecTests
    {
        static readonly byte[] Key = SaveCodec.DeriveKey("test-cihazi-1");

        static string SampleJson()
        {
            var d = new SaveData { coins = 12345, gems = 7, level = 9, tavernName = "Kör Baykuş", saveVersion = GameState.CurrentSaveVersion };
            d.unlocked.Add("salon");
            return JsonUtility.ToJson(d);
        }

        [Test]
        public void EncodeDecode_RoundTripsSaveData()
        {
            var content = SaveCodec.Encode(SampleJson(), Key);
            Assert.IsTrue(SaveCodec.TryDecode(content, Key, out var json));
            var d = JsonUtility.FromJson<SaveData>(json);
            Assert.AreEqual(12345, d.coins);
            Assert.AreEqual(9, d.level);
            Assert.AreEqual("Kör Baykuş", d.tavernName);
            Assert.AreEqual(GameState.CurrentSaveVersion, d.saveVersion);
            CollectionAssert.AreEqual(new List<string> { "salon" }, d.unlocked);
        }

        [Test]
        public void TamperedBody_IsRejected()
        {
            var content = SaveCodec.Encode(SampleJson(), Key).Replace("12345", "99999");
            Assert.IsFalse(SaveCodec.TryDecode(content, Key, out var json));
            Assert.IsNull(json);
        }

        [Test]
        public void TamperedSignature_IsRejected()
        {
            var content = SaveCodec.Encode(SampleJson(), Key);
            var lines = content.Split('\n');
            var sig = lines[1].ToCharArray();
            sig[0] = sig[0] == '0' ? '1' : '0';
            lines[1] = new string(sig);
            Assert.IsFalse(SaveCodec.TryDecode(string.Join("\n", lines), Key, out _));
        }

        [Test]
        public void OtherDeviceKey_IsRejected()
        {
            var content = SaveCodec.Encode(SampleJson(), Key);
            Assert.IsFalse(SaveCodec.TryDecode(content, SaveCodec.DeriveKey("baska-cihaz"), out _));
            CollectionAssert.AreNotEqual(Key, SaveCodec.DeriveKey("baska-cihaz"));
        }

        [Test]
        public void TruncatedOrForeignContent_IsRejected()
        {
            var content = SaveCodec.Encode(SampleJson(), Key);
            Assert.IsFalse(SaveCodec.TryDecode(content.Substring(0, content.Length - 5), Key, out _));
            Assert.IsFalse(SaveCodec.TryDecode(SampleJson(), Key, out _));          // eski düz JSON (başlıksız)
            Assert.IsFalse(SaveCodec.TryDecode("", Key, out _));
            Assert.IsFalse(SaveCodec.TryDecode(null, Key, out _));
            Assert.IsFalse(SaveCodec.TryDecode(SaveCodec.Header + "\nabc\n{}", Key, out _));
        }

        [Test]
        public void Sign_IsDeterministicHex()
        {
            var a = SaveCodec.Sign("{\"coins\":1}", Key);
            Assert.AreEqual(a, SaveCodec.Sign("{\"coins\":1}", Key));
            Assert.AreEqual(64, a.Length);
            StringAssert.IsMatch("^[0-9a-f]{64}$", a);
            Assert.AreNotEqual(a, SaveCodec.Sign("{\"coins\":2}", Key));
        }
    }
}
