using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Ödeme karesi (GAME_DESIGN.md §2, §6.1): oyuncu üstünde durunca para akar;
    /// tamamlanınca hedef nesneler (yeni istasyon, masa, duvar yıkımı…) açılır.
    /// </summary>
    public class PayZone : MonoBehaviour
    {
        public static readonly List<PayZone> All = new();

        public string id = "zone";
        public string label = Loc.N("Yeni Masa");
        /// <summary>Oda numaralı etiket ("Oda 3", sahne kurucusu üretir) ayrıştırılır.</summary>
        const string RoomPrefix = "Oda ";
        /// <summary>v1.7: etkin dildeki etiket (sahnedeki <see cref="label"/> Türkçe anahtar; "Oda N" kalıbı ayrı çevrilir).</summary>
        public string DisplayLabel => label != null && label.StartsWith(RoomPrefix) && int.TryParse(label.Substring(RoomPrefix.Length), out var n)
            ? Loc.F("Oda {0}", n) : Loc.T(label);
        public long cost = 50;
        public int requiredLevel = 1;
        [Tooltip("Bu alan, önce şu alan açılınca görünür (ör. 'salon').")]
        public string requires = "";
        public GameObject[] unlockTargets;
        public GameObject[] hideOnUnlock;
        public float radius = 0.7f;
        public float drainDuration = 1.6f;

        public long Paid { get; private set; }

        static PayZone _goal;
        static int _goalUnlocks = -1, _goalLevel = -1;

        /// <summary>
        /// Sıradaki hedef (HUD hedef kutusu, rehber ve test botu ortak). v1.9 (oyuncu testi "Yeni masa aç hedefi başka masa açınca
        /// gitmiyor"): seviyesi yeten alanlar önce gelir, aralarından kalan bedeli en az olan; hiçbiri yoksa en düşük seviyeli.
        /// Hedef herhangi bir şey açılınca (alan, makine, personel) ya da seviye atlayınca yeniden hesaplanır; açılmış ya da
        /// görünmez bir kareyi asla göstermez.
        /// </summary>
        public static PayZone NextGoal()
        {
            int unlocks = GameState.Data.unlocked.Count;
            if (_goal && _goal.enabled && _goal.Visible && unlocks == _goalUnlocks && GameState.Level == _goalLevel) return _goal;
            _goalUnlocks = unlocks;
            _goalLevel = GameState.Level;
            _goal = Pick();
            return _goal;
        }

        static PayZone Pick()
        {
            PayZone best = null;
            foreach (var z in All)
            {
                if (!z.enabled || !z.Visible) continue;
                if (!best || Better(z, best)) best = z;
            }
            return best;
        }

        /// <summary>a, b'den daha iyi hedef mi: seviyesi yeten önce, sonra kalan bedel, sonra seviye.</summary>
        static bool Better(PayZone a, PayZone b)
        {
            if (a.LevelOk != b.LevelOk) return a.LevelOk;
            if (a.LevelOk) return a.Remaining < b.Remaining || (a.Remaining == b.Remaining && a.requiredLevel < b.requiredLevel);
            return a.requiredLevel < b.requiredLevel || (a.requiredLevel == b.requiredLevel && a.cost < b.cost);
        }
        public long Remaining => cost - Paid;
        public bool Visible => !GameState.IsUnlocked(id) && (string.IsNullOrEmpty(requires) || GameState.IsUnlocked(requires));
        public bool LevelOk => GameState.Level >= requiredLevel;
        public bool Available => Visible && LevelOk;

        float _carry, _tick;
        GameObject _visual;

        /// <summary>
        /// v1.9: sahnedeki bütün ödeme kareleri kimlikle (açılmış olanlar da; açılan kare kapanınca <see cref="All"/>'dan çıkar).
        /// Görev metni ve "alan mı?" sorusu için.
        /// </summary>
        static readonly Dictionary<string, PayZone> ById = new();

        /// <summary>Kimliği verilen ödeme karesi (açılmış olsa da), yoksa null.</summary>
        public static PayZone Find(string id) => id != null && ById.TryGetValue(id, out var z) && z ? z : null;

        void OnDestroy() { if (ById.TryGetValue(id, out var z) && z == this) ById.Remove(id); }

        void Awake()
        {
            ById[id] = this;
            _visual = transform.childCount > 0 ? transform.GetChild(0).gameObject : null;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Start()
        {
            Paid = GameState.ZonePaid(id);
            if (GameState.IsUnlocked(id)) ApplyUnlocked(false);
        }

        void Update()
        {
            bool avail = Available;
            if (_visual) _visual.SetActive(Visible);
            if (!avail) return;

            var p = PlayerController.I;
            if (!p || DrinkStation.FlatDistance(p.transform.position, transform.position) > radius) { _carry = 0; return; }

            // Toplam bedel drainDuration sürede akar; para yetmezse olduğu kadar akar ve kaldığı yeri hatırlar
            _carry += cost / drainDuration * Time.deltaTime;
            long step = (long)_carry;
            if (step <= 0) return;
            step = System.Math.Min(step, System.Math.Min(Remaining, GameState.Coins));
            if (step <= 0) { _carry = 0; return; }
            _carry -= step;
            GameState.TrySpend(step);
            Paid += step;
            _tick -= Time.deltaTime;
            if (_tick <= 0f) { _tick = 0.07f; Sfx.Play(SfxId.Tick, 0.8f + 0.6f * (Paid / (float)cost)); }
            GameState.SetZonePaid(id, Paid);
            if (Paid >= cost)
            {
                Services.Track("zone_unlock", ("alan", id), ("bedel", cost), ("seviye", GameState.Level));
                GameState.Unlock(id);
                ApplyUnlocked(true);
                Sfx.Play(SfxId.Unlock);
                GameSettings.Vibrate();
            }
        }

        /// <summary>Test botu (v1.5 prestij testi): alanı parasız açar.</summary>
        public void DebugUnlock()
        {
            if (GameState.IsUnlocked(id)) return;
            GameState.Unlock(id);
            ApplyUnlocked(false);
        }

        void ApplyUnlocked(bool animate)
        {
            foreach (var t in unlockTargets) if (t) t.SetActive(true);
            foreach (var h in hideOnUnlock) if (h) h.SetActive(false);
            // v2.4.1: açılan masa Barlo'nun üstünde belirirse onu dışarı (ince ön sınırın ötesine) itebiliyordu;
            // nesne tam boyuttayken Barlo önce boş bir yere alınır, sonra büyüyerek belirme başlar
            if (animate && PlayerController.I) PlayerController.I.EnsureFree();
            if (animate) foreach (var t in unlockTargets) if (t) t.AddComponent<PopIn>();
            if (_visual) _visual.SetActive(false);
            enabled = false;
        }
    }
}
