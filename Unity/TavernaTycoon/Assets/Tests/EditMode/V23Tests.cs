using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace Taverna.Tests
{
    /// <summary>
    /// v2.3 Paket 5 (GAME_DESIGN.md §17): yerleşim doğrulama (sınır, sabit engel, açık alan, çakışma, erişim), ızgara yolu,
    /// yerleşim satırları ve eski kaydın yükseltilmesi, bulut kaydı çakışma kuralı, bulut imzası (bozuk / imzasız / başka anahtar reddi).
    /// </summary>
    public class V23Tests
    {
        // 6 × 4 m oda; kapı alt ortada (0, −1.7)
        static readonly RectXZ Room = new(-3f, -2f, 3f, 2f);
        static readonly Vector2 Door = new(0f, -1.75f);

        static LayoutRules.Input Make(RectXZ cand, LayoutGrid g = null)
        {
            var c = new LayoutRules.Input { grid = g ?? new LayoutGrid(Room), candidate = cand };
            c.starts.Add(Door);
            return c;
        }

        [Test]
        public void Check_OutOfBounds_WhenFootprintLeavesZone()
        {
            Assert.AreEqual(PlaceResult.OutOfBounds, LayoutRules.Check(Make(RectXZ.Center(2.8f, 0f, 0.5f, 0.5f))));
            Assert.AreEqual(PlaceResult.Ok, LayoutRules.Check(Make(RectXZ.Center(2.4f, 0f, 0.5f, 0.5f))));
        }

        [Test]
        public void Check_Blocked_ByStaticObstacleAndKeepClear()
        {
            var g = new LayoutGrid(Room);
            g.BlockStatic(RectXZ.Center(-2f, 1.5f, 0.5f, 0.3f));   // makine / duvar
            Assert.AreEqual(PlaceResult.Blocked, LayoutRules.Check(Make(RectXZ.Center(-2f, 1.2f, 0.5f, 0.4f), g)));
            var c = Make(RectXZ.Center(0f, -1.3f, 0.4f, 0.4f));
            c.keepClear.Add(RectXZ.Center(0f, -1.6f, 0.7f, 0.5f));   // kapı ağzı
            Assert.AreEqual(PlaceResult.Blocked, LayoutRules.Check(c));
        }

        [Test]
        public void Check_Overlap_WithOtherObjectAndReservedSpot_EdgeTouchAllowed()
        {
            var c = Make(RectXZ.Center(1f, 0.5f, 0.5f, 0.5f));
            c.others.Add(RectXZ.Center(1.6f, 0.5f, 0.5f, 0.5f));
            Assert.AreEqual(PlaceResult.Overlap, LayoutRules.Check(c));
            var touch = Make(RectXZ.Center(0.6f, 0.5f, 0.5f, 0.5f));
            touch.others.Add(RectXZ.Center(1.6f, 0.5f, 0.5f, 0.5f));   // kenarlar değiyor, binmiyor
            Assert.AreEqual(PlaceResult.Ok, LayoutRules.Check(touch));
            var res = Make(RectXZ.Center(-1f, 0.5f, 0.5f, 0.5f));
            res.reserved.Add(RectXZ.Center(-1.2f, 0.5f, 0.5f, 0.5f));   // alınmamış masanın yeri
            Assert.AreEqual(PlaceResult.Overlap, LayoutRules.Check(res));
        }

        [Test]
        public void Check_Unreachable_WhenItSealsOffATarget_AndOwnSeatsMustBeReachable()
        {
            // Koridor: sol yarıyı ayıran duvar, ortasında 1 m boşluk; boşluğa eşya konunca soldaki oturak erişilemez kalır
            var g = new LayoutGrid(Room);
            g.BlockStatic(new RectXZ(-1.2f, -2f, -0.8f, -0.5f));
            g.BlockStatic(new RectXZ(-1.2f, 0.5f, -0.8f, 2f));
            var c = Make(RectXZ.Center(-1f, 0f, 0.3f, 0.6f), g);
            c.targets.Add(new Vector2(-2.5f, 0f));
            Assert.AreEqual(PlaceResult.Unreachable, LayoutRules.Check(c));
            var ok = Make(RectXZ.Center(1.5f, 0.8f, 0.4f, 0.4f), g);
            ok.targets.Add(new Vector2(-2.5f, 0f));
            Assert.AreEqual(PlaceResult.Ok, LayoutRules.Check(ok));
            // kendi oturağı duvarla çevrili köşede: erişilemez
            var g2 = new LayoutGrid(Room);
            g2.BlockStatic(new RectXZ(1.5f, 0.8f, 3f, 1.0f));
            g2.BlockStatic(new RectXZ(1.5f, 0.8f, 1.7f, 2f));
            var own = Make(RectXZ.Center(0f, 0f, 0.4f, 0.4f), g2);
            own.ownTargets.Add(new Vector2(2.4f, 1.6f));
            Assert.AreEqual(PlaceResult.Unreachable, LayoutRules.Check(own));
        }

        [Test]
        public void Check_FlatDecor_IgnoresKeepClearButNotOtherFlats()
        {
            var c = Make(RectXZ.Center(0f, -1.5f, 0.4f, 0.4f));
            c.flat = true;
            c.keepClear.Add(RectXZ.Center(0f, -1.6f, 0.7f, 0.5f));
            c.others.Add(RectXZ.Center(0.2f, -1.5f, 0.4f, 0.4f));
            Assert.AreEqual(PlaceResult.Ok, LayoutRules.Check(c), "halı / taş yol üstünden yürünür");
            c.otherFlats.Add(RectXZ.Center(0.3f, -1.5f, 0.4f, 0.4f));
            Assert.AreEqual(PlaceResult.Overlap, LayoutRules.Check(c));
        }

        [Test]
        public void FindPath_GoesAroundObstacle_AndEndsNearSeat()
        {
            var g = new LayoutGrid(Room);
            var blockers = new List<RectXZ> { new(-2f, -0.5f, 2f, 0f) };   // kapı ile hedef arasında uzun masa
            var blocked = g.Blocked(blockers);
            var seat = new Vector2(0f, 1.2f);
            var path = g.FindPath(blocked, Door, seat);
            Assert.IsNotNull(path);
            Assert.AreEqual(Door, path[0]);
            Assert.LessOrEqual((path[^1] - seat).magnitude, LayoutGrid.AccessRadius + 0.01f);
            for (int i = 0; i + 1 < path.Count; i++) Assert.IsTrue(g.LineClear(blocked, path[i], path[i + 1]), $"parça {i} engelden geçiyor");
            Assert.IsTrue(LayoutGrid.PolylineCrosses(new List<Vector2> { Door, seat }, blockers[0]), "düz yol masanın içinden geçerdi");
            Assert.IsNull(g.FindPath(g.Blocked(new List<RectXZ> { new(-3f, -0.5f, 3f, 0f) }), Door, seat), "duvar boydan boya: yol yok");
        }

        [Test]
        public void LayoutLines_RoundTrip_SetRemoveClear_AndSnap()
        {
            var d = new SaveData();
            Layout.Set(d, Layout.TableKey("Table_E"), new Layout.Pose(7.25f, -1.5f, 5));
            Layout.Set(d, Layout.DecorKey(12), new Layout.Pose(-3.5f, 2f, 0));
            Layout.Set(d, Layout.TableKey("Table_E"), new Layout.Pose(7.5f, -1.5f, 1));
            Assert.AreEqual(2, d.layout.Count);
            Assert.IsTrue(Layout.TryGet(d, "t:Table_E", out var p));
            Assert.AreEqual(7.5f, p.x, 1e-4f); Assert.AreEqual(1, p.quarter);
            Assert.AreEqual(1, new Layout.Pose(0, 0, 5).quarter, "çeyrek 0–3");
            Assert.AreEqual(3, new Layout.Pose(0, 0, -1).quarter);
            Layout.Remove(d, "d:12");
            Assert.IsFalse(Layout.TryGet(d, "d:12", out _));
            Assert.AreEqual(1.25f, Layout.Snap(1.3f), 1e-4f);
            Assert.AreEqual(-0.5f, Layout.Snap(-0.4f), 1e-4f);
            Assert.IsFalse(Layout.TryParse("t:X|abc|1|0", out _, out _));
            Layout.Clear(d);
            Assert.AreEqual(0, d.layout.Count);
        }

        [Test]
        public void OldSave_WithoutLayout_UpgradesToDefaultLayout_AndKeepsNewFields()
        {
            // v2.2 (saveVersion 3) kaydı: yerleşim alanı yok
            const string old = "{\"version\":1,\"saveVersion\":3,\"coins\":1234,\"level\":12,\"branch\":1,\"decor\":[\"dec_saksi:0\"]}";
            var d = GameState.FromJson(old);
            Assert.IsNotNull(d);
            Assert.AreEqual(GameState.CurrentSaveVersion, d.saveVersion);
            Assert.AreEqual(Layout.Version, d.layoutVersion);
            Assert.AreEqual(0, d.layout.Count, "eski kayıt varsayılan yerleşimle açılır");
            Assert.AreEqual(12, d.level);
            Assert.AreEqual(1, d.decor.Count);
            // yeni kayıt: yerleşim satırı yazılır, okunur, bozuk satır atılır
            d.layout.Add(Layout.Format("t:Table_A", new Layout.Pose(-1.5f, 0.25f, 1)));
            d.layout.Add("bozuk-satir");
            var back = GameState.FromJson(JsonUtility.ToJson(d));
            Assert.AreEqual(1, back.layout.Count);
            Assert.IsTrue(Layout.TryGet(back, "t:Table_A", out var p));
            Assert.AreEqual(0.25f, p.z, 1e-4f);
            Assert.IsNull(GameState.FromJson("{bozuk"));
        }

        static CloudSaveMeta M(int lv, int br, long earned, string dev = "a") => new() { level = lv, branch = br, totalEarned = earned, deviceId = dev };

        [Test]
        public void Resolve_ClearProgressWins_AmbiguousAsks()
        {
            Assert.AreEqual(CloudChoice.UseLocal, CloudSave.Resolve(M(5, 0, 100), null), "bulut boş");
            Assert.AreEqual(CloudChoice.UseCloud, CloudSave.Resolve(M(29, 2, 900000), M(3, 3, 1000, "b")), "prestij önce gelir");
            Assert.AreEqual(CloudChoice.UseLocal, CloudSave.Resolve(M(20, 1, 1000, "a"), M(16, 1, 5000, "b")), "seviye farkı ≥ 3");
            Assert.AreEqual(CloudChoice.Ask, CloudSave.Resolve(M(12, 3, 50000, "a"), M(14, 3, 60000, "b")), "Sv12 / Sv14 aynı prestij: belirsiz");
            Assert.AreEqual(CloudChoice.UseCloud, CloudSave.Resolve(M(12, 3, 40000, "a"), M(13, 3, 90000, "b")), "seviye ve kazanç bulutta açıkça büyük");
            Assert.AreEqual(CloudChoice.Ask, CloudSave.Resolve(M(14, 3, 40000, "a"), M(12, 3, 90000, "b")), "seviye ve kazanç ters yönde");
            Assert.AreEqual(CloudChoice.Same, CloudSave.Resolve(M(8, 0, 4000), M(8, 0, 4000, "b")));
            Assert.AreEqual(CloudChoice.UseLocal, CloudSave.Resolve(M(9, 0, 5000, "a"), M(8, 0, 4000, "a")), "aynı kurulumun eski yedeği");
            Assert.AreEqual(CloudChoice.Ask, CloudSave.Resolve(M(9, 0, 5000, "a"), M(8, 0, 4000, "b")), "başka cihaz, yakın ilerleme");
        }

        [Test]
        public void CloudCodec_RoundTrip_AndRejectsTamperedUnsignedOrLocalSigned()
        {
            var meta = M(14, 3, 123456, "dev");
            const string json = "{\"level\":14,\"branch\":3,\"coins\":77}";
            var bytes = CloudSaveCodec.Encode(meta, json);
            Assert.IsTrue(CloudSaveCodec.TryDecode(bytes, out var m, out var j));
            Assert.AreEqual(14, m.level); Assert.AreEqual(3, m.branch); Assert.AreEqual(json, j);

            var text = Encoding.UTF8.GetString(bytes);
            Assert.IsFalse(CloudSaveCodec.TryDecode(Encoding.UTF8.GetBytes(text.Replace("\"coins\":77", "\"coins\":99999")), out _, out _), "kayıt değiştirilmiş");
            Assert.IsFalse(CloudSaveCodec.TryDecode(Encoding.UTF8.GetBytes(text.Replace("\"level\":14,\"branch\":3,\"total", "\"level\":30,\"branch\":3,\"total")), out _, out _), "üst bilgi değiştirilmiş");
            Assert.IsFalse(CloudSaveCodec.TryDecode(Encoding.UTF8.GetBytes(json), out _, out _), "imzasız");
            Assert.IsFalse(CloudSaveCodec.TryDecode(new byte[0], out _, out _));
            Assert.IsFalse(CloudSaveCodec.TryDecode(Encoding.UTF8.GetBytes(text.Substring(0, text.Length - 5)), out _, out _), "kesilmiş");
            // yerel kayıt imzası (cihaz anahtarı) bulut biçimi sayılmaz
            var local = SaveCodec.Encode(json, SaveCodec.DeriveKey("cihaz-1"));
            Assert.IsFalse(CloudSaveCodec.TryDecode(Encoding.UTF8.GetBytes(local), out _, out _));
        }

        [Test]
        public void BuildMeta_ReadsProgressFromSave()
        {
            var d = new SaveData { level = 17, branch = 2, totalEarned = 987654, saveVersion = GameState.CurrentSaveVersion };
            var m = CloudSave.BuildMeta(d, "guid-1", new System.DateTime(2026, 11, 22, 10, 0, 0, System.DateTimeKind.Utc));
            Assert.AreEqual(17, m.level); Assert.AreEqual(2, m.branch); Assert.AreEqual(987654, m.totalEarned);
            Assert.AreEqual("guid-1", m.deviceId);
            StringAssert.StartsWith("2026-11-22T10:00:00", m.savedUtc);
        }
    }
}
