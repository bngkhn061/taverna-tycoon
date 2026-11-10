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
        public const int RoomCount = 6;
        public static readonly int[] Fees = { 40, 80, 150 };
        public static readonly int[] ComfortCosts = { 600, 1500 };
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
            { new(17.5f, 0f, -3.5f), new(9.5f, 0f, -2.45f), new(11.6f, 0f, -1.2f), new(12.0f, 0f, 1.0f), Reception };
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
            _spawnT = GuestSpawnEvery;
            int waiting = 0;
            foreach (var g in Guest.All) if (g.state == Guest.State.WalkIn || g.state == Guest.State.Waiting) waiting++;
            if (waiting >= MaxWaitingGuests) return;
            var room = Room.FirstFree();
            if (room) Guest.Spawn(room);
        }
    }
}
