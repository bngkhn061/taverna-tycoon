using System;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Han / otel odaları (v0.8, onaylı taslak 2026-11-10). Salon köşesindeki merdivenle üst kata çıkılır
    /// (üst kat dünyada ayrı bir yerde durur: z &gt; UpZ). Akşam misafirler resepsiyona gelir; Barlo yanlarına
    /// gidince gecelik ücreti öderler ve odalarına çıkarlar, gece uyurlar, sabah ayrılırlar ve oda dağınık kalır.
    /// Oda toplanmadan yeni misafir almaz. Konfor (yatak kademesi) ücreti belirler. Sayılar burada.
    /// </summary>
    public static class Inn
    {
        /// <summary>z bu değerin üstündeyse üst kattır.</summary>
        public const float UpZ = 30f;
        /// <summary>v2.0 (madde 20): 6 → 8 oda (yeni ikisi üst katın solunda).</summary>
        public const int RoomCount = 8;
        /// <summary>
        /// v2.0 (madde 19) konfor seviyeleri 0–4, her biri odada görünür eşyayla: 0 saman yatak · 1 yün yatak ·
        /// 2 kilim + komodin · 3 lamba + dolap · 4 kuştüyü yatak + şömine + tablo. Gecelik ücret ve yükseltme bedeli.
        /// </summary>
        public static readonly int[] Fees = { 40, 65, 95, 130, 180 };
        public static readonly int[] ComfortCosts = { 500, 900, 1600, 2600 };
        /// <summary>Açık odalardaki her konfor seviyesi misafir gelişini bu kadar hızlandırır (+%4).</summary>
        public const float ComfortArrivalStep = 0.04f;
        /// <summary>Seviye başına eşya adları (dükkân kartı).</summary>
        public static readonly string[] ComfortNames = { Loc.N("Saman yatak"), Loc.N("Yün yatak"), Loc.N("Kilim + komodin"), Loc.N("Lamba + dolap"), Loc.N("Kuştüyü yatak + şömine + tablo") };
        public const float GuestPatience = 45f;
        public const float GuestSpawnEvery = 14f;
        public const int MaxWaitingGuests = 2;
        /// <summary>Gecenin bu kısmından sonra yeni misafir gelmez.</summary>
        public const float LastArrivalPhase = 0.9f;

        // Yerleşim (TavernaSceneBuilder.BuildInn ile aynı, yeni harita koordinatı)
        public static readonly Vector3 StairsFoot = new(13.45f, 0f, 3.6f);
        public static readonly Vector3 SalonArrive = new(12.55f, 0f, 3.6f);
        public static readonly Vector3 UpHole = new(-5.2f, 0f, 38.0f);
        public static readonly Vector3 UpArrive = new(-4.0f, 0f, 38.0f);
        public static readonly Vector3 Reception = new(13.35f, 0f, 1.5f);
        public static readonly Vector3[] ArrivalPath =
            { new(17.5f, 0f, -6.5f), new(9.5f, 0f, -5.45f), new(9.5f, 0f, -2.0f), new(11.6f, 0f, -1.2f), new(12.0f, 0f, 1.0f), Reception };
        public static readonly Vector3[] ToStairsPath = { new(12.8f, 0f, 2.6f), StairsFoot };

        public static event Action<bool> StairsUsed;
        public static void RaiseStairs(bool up) => StairsUsed?.Invoke(up);

        public static bool Open => GameState.IsUnlocked("merdiven");
        public static bool Upstairs(Vector3 p) => p.z > UpZ;

        public static int Comfort(int room)
        {
            var l = GameState.Data.roomLvs;
            while (l.Count < RoomCount) l.Add(0);
            return Mathf.Clamp(l[room], 0, Fees.Length - 1);
        }

        public static int Fee(int room) => Fees[Comfort(room)];

        /// <summary>Oda açık mı (ilk iki oda merdivenle, diğerleri ödeme kareleriyle)?</summary>
        public static bool RoomOpen(int room) => room < 2 || GameState.IsUnlocked($"oda{room + 1}");

        /// <summary>Misafir geliş çarpanı: 1 + <see cref="ComfortArrivalStep"/> × açık odaların konfor seviyeleri toplamı.</summary>
        public static float ArrivalMultiplier
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < RoomCount; i++) if (RoomOpen(i)) sum += Comfort(i);
                return ArrivalMultiplierFor(sum);
            }
        }

        /// <summary>Konfor toplamından geliş çarpanı (birim testi için saf işlev).</summary>
        public static float ArrivalMultiplierFor(int comfortSum) => 1f + ComfortArrivalStep * Mathf.Max(0, comfortSum);

        /// <summary>v2.0 kayıt göçü: eski 3 kademeli konfor (0 saman, 1 yün, 2 kuştüyü) → yeni 5 seviye (2 → 4).</summary>
        public static int MigrateComfortV2(int old) => old >= 2 ? Fees.Length - 1 : Mathf.Max(0, old);
        public static bool CanUpgrade(int room) => Comfort(room) < Fees.Length - 1;
        public static int UpgradeCost(int room) => CanUpgrade(room) ? ComfortCosts[Comfort(room)] : 0;

        public static bool UpgradeComfort(int room)
        {
            if (!CanUpgrade(room) || !GameState.TrySpend(UpgradeCost(room))) return false;
            GameState.Data.roomLvs[room] = Comfort(room) + 1;
            GameState.Save();
            GameState.NotifyChanged();
            return true;
        }

        /// <summary>Test botu için: hedef öbür kattaysa önce merdivene.</summary>
        public static Vector3 Route(Vector3 from, Vector3 to)
        {
            bool a = Upstairs(from), b = Upstairs(to);
            if (a == b) return to;
            return a ? UpHole : StairsFoot;
        }

        static float _spawnT;
        /// <summary>Oturumdaki giriş / ayrılış sayaçları (test ve günlük raporu için).</summary>
        public static int CheckIns, AngryGuests, RoomsCleaned;

        /// <summary>Akşam misafir gönderir. GameBootstrap her kare çağırır.</summary>
        public static void Tick(float dt)
        {
            if (!Open || !Night.IsDark || Night.Phase > LastArrivalPhase) { _spawnT = 2f; return; }
            _spawnT -= dt;
            if (_spawnT > 0f) return;
            _spawnT = GuestSpawnEvery / ArrivalMultiplier;   // v2.0: konforlu han daha sık misafir alır
            int waiting = 0;
            foreach (var g in Guest.All) if (g.state == Guest.State.WalkIn || g.state == Guest.State.Waiting) waiting++;
            if (waiting >= MaxWaitingGuests) return;
            var room = Room.BestFree();   // v2.0: misafir en konforlu boş odayı seçer
            if (room) Guest.Spawn(room);
        }
    }
}
