#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v1.8 denge ölçümü ("-autotest-measure"): botun test adımları (Hazine, zorla etkinlik, kovma, panel turu, dil, reklam,
    /// ekran görüntüsü) kapanır, bot yalnızca oyuncu gibi oynar ve saniyede bir örnek alır: oturak doluluğu, mutluluk,
    /// müşteri çarpanı, masa başına bekleyen sipariş, makine aç kalma süresi, sipariş dağılımı (Kral Şerbeti payı),
    /// özel siparişlerin sonucu. Sonunda tek satırlık ÖLÇÜM özetleri yazar.
    /// "-autotest-event &lt;anahtar&gt;": 200. oyun saniyesinde o etkinliği başlatır (para yetmezse test parası eklenir);
    /// 200–380 sn penceresindeki altın, etkinliksiz koşunun aynı penceresiyle karşılaştırılır.
    /// "-autotest-quiet": yalnızca kare süresi sayımı (günlük, ekran görüntüsü, örnekleme ve test adımları kapalı).
    /// </summary>
    public partial class PlaytestBot
    {
        bool? _measure, _quiet;
        string _eventArg;

        /// <summary>Denge ölçümü modu.</summary>
        bool Measure => _measure ??= HasArg("-autotest-measure");
        /// <summary>Yalnızca kare süresi sayan sessiz mod (botun teşhis adımları kapalı).</summary>
        bool Quiet => _quiet ??= HasArg("-autotest-quiet");
        /// <summary>Test adımları kapalı (ölçüm ya da sessiz mod).</summary>
        bool NoTests => Measure || Quiet;
        bool? _noShots;
        /// <summary>Yalnızca ekran görüntüsü kapalı (yavaş karelerin kaynağını ayırmak için).</summary>
        bool NoShots => _noShots ??= HasArg("-autotest-noshots");

        static bool HasArg(string a) => Array.IndexOf(Environment.GetCommandLineArgs(), a) >= 0;

        string EventArg
        {
            get
            {
                if (_eventArg != null) return _eventArg;
                var args = Environment.GetCommandLineArgs();
                int i = Array.IndexOf(args, "-autotest-event");
                _eventArg = i >= 0 && i + 1 < args.Length ? args[i + 1] : "";
                return _eventArg;
            }
        }

        // ---- örnekleme
        /// <summary>Örnekleme aralığı (oyun saniyesi).</summary>
        const float SampleEvery = 1f;
        /// <summary>Etkinlik ölçüm penceresi (oyun saniyesi): başlangıç ve bitiş.</summary>
        const float WindowFrom = 200f, WindowTo = 380f;

        float _sampleT;
        int _samples, _samplesBusy, _happy95, _happy50;
        double _seatOccN, _seatTotN, _seatDirtyN, _seatOccB, _seatTotB, _happySum, _boostSum, _intervalSum;
        double _waitTablesSum, _waitOrdersSum; int _waitMax, _waitTableSamples;
        readonly Dictionary<DrinkId, float> _starved = new(), _full = new(), _outage = new();
        readonly Dictionary<DrinkId, int> _ordersBy = new(), _servedBy = new();
        readonly Dictionary<Customer, bool> _wasWaiting = new();
        readonly HashSet<Customer> _angrySeen = new();
        readonly Dictionary<Table, int> _waitPerTable = new();
        int _ordersAll, _ordersFood, _ordersSpecial, _angrySpecial, _angryNormal;
        long _windowGold0 = -1, _windowGold1 = -1;
        bool _v18Hooked, _eventStarted;
        long _eventCost;
        int _served0 = -1;

        void UpdateV18()
        {
            if (!_v18Hooked)
            {
                _v18Hooked = true;
                _served0 = GameState.Data.customersServed;
                GameState.DrinkServed += d => _servedBy[d] = (_servedBy.TryGetValue(d, out var n) ? n : 0) + 1;
                Log($"v1.8 ÖLÇÜM: başladı · Sv{GameState.Level} · kademe {Tiers.CurrentIndex} · açılan [{string.Join(",", GameState.Data.unlocked)}] · etkinlik testi '{EventArg}'");
            }
            // Sipariş geçişleri (yeni Waiting) — her kare, ucuz
            foreach (var c in Customer.All)
            {
                bool w = c.state == Customer.State.Waiting;
                _wasWaiting.TryGetValue(c, out var before);
                if (w && !before)
                {
                    _ordersAll++;
                    _ordersBy[c.wanted] = (_ordersBy.TryGetValue(c.wanted, out var n) ? n : 0) + 1;
                    var dd = GameDatabase.I ? GameDatabase.I.Drink(c.wanted) : null;
                    if (dd && dd.isFood) _ordersFood++;
                    if (c.special) _ordersSpecial++;
                }
                _wasWaiting[c] = w;
            }
            // Etkinlik penceresi
            if (_windowGold0 < 0 && _t >= WindowFrom) _windowGold0 = _earned;
            if (_windowGold1 < 0 && _t >= WindowTo) _windowGold1 = _earned;
            if (!_eventStarted && EventArg != "" && _t >= WindowFrom) StartTestEvent();

            _sampleT += Time.deltaTime;
            if (_sampleT < SampleEvery) return;
            _sampleT -= SampleEvery;
            _samples++;
            bool busy = RushHour.Active || TavernEvents.Active.HasValue;
            if (busy) _samplesBusy++;
            // Oturaklar: müşterinin oturabileceği (fener yakılı, teras barı olan) masalar
            int tot = 0, occ = 0, dirty = 0;
            bool upBar = DrinkStation.All.Exists(s => s && Inn.Upstairs(s.transform.position));
            _waitPerTable.Clear();
            foreach (var t in Table.All)
            {
                if (!t || !t.isActiveAndEnabled) continue;
                if (!Night.ZoneLit(Night.ZoneOf(t.transform.position))) continue;
                if (Inn.Upstairs(t.transform.position) && !upBar) continue;
                foreach (var s in t.seats)
                {
                    if (!s) continue;
                    tot++;
                    if (s.occupant) occ++;
                    else if (t.Dirty) dirty++;
                }
            }
            if (busy) { _seatOccB += occ; _seatTotB += tot; }
            else { _seatOccN += occ; _seatTotN += tot; _seatDirtyN += dirty; }
            // Bekleyen siparişler (masa başına)
            int waitingAll = 0;
            foreach (var c in Customer.All)
            {
                if (c.state != Customer.State.Waiting) continue;
                waitingAll++;
                var seat = FindSeat(c);
                if (seat && seat.table) _waitPerTable[seat.table] = (_waitPerTable.TryGetValue(seat.table, out var k) ? k : 0) + 1;
            }
            if (_waitPerTable.Count > 0)
            {
                _waitTableSamples++;
                _waitTablesSum += _waitPerTable.Count;
                _waitOrdersSum += waitingAll;
                foreach (var kv in _waitPerTable) _waitMax = Mathf.Max(_waitMax, kv.Value);
            }
            // Mutluluk ve çarpan
            float hv = Happiness.Value;
            _happySum += hv;
            if (hv >= 95f) _happy95++;
            if (hv < 50f) _happy50++;
            float boost = CustomerSpawner.BoostMultiplier;
            _boostSum += boost;
            _intervalSum += Balance.BaseSpawnInterval * Balance.SpawnLevelScale(GameState.Level) / boost / Balance.SeatSpawnScale(SeatsOpen());
            // Makineler: sipariş beklerken stok yok (aç) / stok dolu
            foreach (var st in DrinkStation.All)
            {
                if (!st || !st.drink) continue;
                int want = 0;
                foreach (var c in Customer.All) if (c.state == Customer.State.Waiting && c.wanted == st.drink.id) want++;
                if (want > 0 && st.Stock == 0) _starved[st.drink.id] = (_starved.TryGetValue(st.drink.id, out var a) ? a : 0f) + SampleEvery;
                if (st.Stock >= st.MaxStock) _full[st.drink.id] = (_full.TryGetValue(st.drink.id, out var b) ? b : 0f) + SampleEvery;
                if (Depot.Supply(st.drink.id) <= 0) _outage[st.drink.id] = (_outage.TryGetValue(st.drink.id, out var o) ? o : 0f) + SampleEvery;
            }
        }

        /// <summary>Kızgın ayrılan müşteri (Update'teki sayımdan): özel mi?</summary>
        void OnAngryV18(Customer c)
        {
            if (c.special) _angrySpecial++; else _angryNormal++;
            // Bölge kırılımı: kızgınlar nerede (karanlık oda, teras, uzak bölümler)?
            var pos = c.transform.position;
            string z = Inn.Upstairs(pos) ? "teras" : Night.ZoneOf(pos);
            if (string.IsNullOrEmpty(z)) z = "meyhane";
            if (!Inn.Upstairs(pos) && Night.IsDark && !Night.ZoneLit(Night.ZoneOf(pos))) z += "*karanlık";
            if (c.wanted != default && GameDatabase.I && GameDatabase.I.Drink(c.wanted) && GameDatabase.I.Drink(c.wanted).isFood) _angryFood++;
            _angryZones[z] = (_angryZones.TryGetValue(z, out var n) ? n : 0) + 1;
        }

        readonly Dictionary<string, int> _angryZones = new();
        int _angryFood;

        static Seat FindSeat(Customer c)
        {
            foreach (var t in Table.All)
                if (t) foreach (var s in t.seats) if (s && s.occupant == c) return s;
            return null;
        }

        static int SeatsOpen()
        {
            int n = 0;
            foreach (var t in Table.All) if (t) n += t.seats.Length;
            return n;
        }

        void StartTestEvent()
        {
            _eventStarted = true;
            var d = Array.Find(TavernEvents.List, e => e.key == EventArg);
            if (string.IsNullOrEmpty(d.key)) { Log($"v1.8 ETKİNLİK: '{EventArg}' bulunamadı"); return; }
            _eventCost = TavernEvents.Cost(d);
            long added = 0;
            if (GameState.Coins < _eventCost) { added = _eventCost - GameState.Coins; GameState.Data.coins += added; }
            var r = TavernEvents.Start(d);
            Log($"v1.8 ETKİNLİK: {d.key} → {r} · fiyat {_eventCost} · test parası {added} · {TavernEvents.Seconds:0} gerçek sn (bot ×{timeScale:0} hızda oyun-sn olarak daha uzun; ölçüm {WindowFrom:0}–{WindowTo:0} oyun-sn)");
        }

        // ---- v1.8 hedef kutusu 10 dilde (prestij testinin hedef adımında; uzun metin "Yeni Şube için: … kademesi")
        int _glIdx = -1;
        float _glAt;
        string _glLang0;

        /// <summary>Her dil için: dili seç, 0.8 sn bekle, hedef kutusunun görüntüsünü al ve sığıp sığmadığını yaz. Bitince true.</summary>
        bool GoalLangsDone(HudController hud)
        {
            if (_glIdx >= Loc.Codes.Length) return true;
            if (_glIdx < 0) { _glIdx = 0; _glLang0 = Loc.Lang; Loc.SetLanguage(Loc.Codes[0]); _glAt = _real; return false; }
            if (_real < _glAt + 0.8f) return false;
            string code = Loc.Codes[_glIdx];
            Log($"v1.8 HEDEF KUTUSU [{code}]: \"{hud.DebugGoalText}\" · {hud.DebugGoalFit}");
            Shot($"goal_{code}.png");
            _glIdx++;
            if (_glIdx < Loc.Codes.Length) Loc.SetLanguage(Loc.Codes[_glIdx]);
            else Loc.SetLanguage(_glLang0);
            _glAt = _real;
            return _glIdx >= Loc.Codes.Length;
        }

        void FinishV18()
        {
            float min = Mathf.Max(0.01f, _t / 60f);
            long un = GameState.Data.un - _startUn;
            for (int l = _startLv; l < GameState.Level; l++) un += Balance.UnForLevel(l);
            float unRate = un / min;
            int need = Balance.UnForLevel(GameState.Level), left = need - GameState.Data.un;
            int served = GameState.Data.customersServed - _served0;
            int resolved = served + _angryLeaves;
            float goldRate = _earned / min;
            long wages = Management.DailyWages;
            float dayIncome = goldRate * Balance.DayLength / 60f;
            var sb = new StringBuilder();
            sb.Append($"v1.8 ÖLÇÜM TEMPO: Sv{_startLv}→Sv{GameState.Level} · {min:0.0} oyun-dk · Ün/dk {unRate:0.0} · seviye Ün {need} → bot tam seviye {need / Mathf.Max(0.1f, unRate):0.0} dk (insan ×1.3 {1.3f * need / Mathf.Max(0.1f, unRate):0.0} dk) · kalan {left} Ün");
            Log(sb.ToString());
            Log($"v1.8 ÖLÇÜM EKONOMİ: altın/dk {goldRate:0} · servis/dk {served / min:0.0} · günlük maaş {wages} · gün geliri (tahmin) {dayIncome:0} · maaş/gelir %{(dayIncome > 0 ? 100f * wages / dayIncome : 0f):0.0} · personel [{string.Join(",", Array.ConvertAll(Array.FindAll(Catalog.StaffList, s => Management.Working(s.id)), s => s.name + ":" + StaffUpgrades.Level(s.id)))}]");
            Log($"v1.8 ÖLÇÜM BASKI: kızgın {_angryLeaves} / sonuçlanan {resolved} = %{(resolved > 0 ? 100f * _angryLeaves / resolved : 0f):0.0} (özel {_angrySpecial}, normal {_angryNormal}) · mutluluk ort. %{(_samples > 0 ? _happySum / _samples : 0):0.0} · aralık %{_happyMin:0}–%{_happyMax:0} · ≥%95 zamanın %{(_samples > 0 ? 100f * _happy95 / _samples : 0):0} · <%50 zamanın %{(_samples > 0 ? 100f * _happy50 / _samples : 0):0}");
            Log($"v1.8 ÖLÇÜM OTURAK: normal doluluk %{(_seatTotN > 0 ? 100 * _seatOccN / _seatTotN : 0):0} (kirli boş %{(_seatTotN > 0 ? 100 * _seatDirtyN / _seatTotN : 0):0}) · yoğun/etkinlik %{(_seatTotB > 0 ? 100 * _seatOccB / _seatTotB : 0):0} ({_samplesBusy} sn) · oturak {SeatsOpen()} · masa başına bekleyen sipariş ort. {(_waitTablesSum > 0 ? _waitOrdersSum / _waitTablesSum : 0):0.00} (en çok {_waitMax}) · bekleyen masa ort. {(_waitTableSamples > 0 ? _waitTablesSum / _waitTableSamples : 0):0.0}");
            Log($"v1.8 ÖLÇÜM ÇARPAN: uygulanan müşteri çarpanı ort. ×{(_samples > 0 ? _boostSum / _samples : 0):0.00} (en çok ×{_capMax:0.00}, ham en çok ×{_rawMax:0.00}) · seviye ölçeği ×{1f / Balance.SpawnLevelScale(GameState.Level):0.00} · oturak ölçeği ×{Balance.SeatSpawnScale(SeatsOpen()):0.00} · ort. müşteri aralığı {(_samples > 0 ? _intervalSum / _samples : 0):0.00} sn");
            var zs = new List<string>();
            foreach (var kv in _angryZones) zs.Add($"{kv.Key}:{kv.Value}");
            Log($"v1.8 ÖLÇÜM KIZGIN YER: [{string.Join(" ", zs)}] · yemek bekleyen {_angryFood} · maaş ödenmeyen gün küskün={Management.Sulky}");
            Log($"v1.8 ÖLÇÜM ÖZEL: açılan {_ordersSpecial} · oyuncu yetiştirdi {Customer.SpecialServed} · kızgın giden {_angrySpecial} · oran %{(_ordersSpecial > 0 ? 100f * Customer.SpecialServed / _ordersSpecial : 0f):0}");
            int drinkOrders = _ordersAll - _ordersFood;
            int king = _ordersBy.TryGetValue(DrinkId.KralSerbeti, out var kq) ? kq : 0;
            var parts = new List<string>();
            foreach (var kv in _ordersBy) parts.Add($"{kv.Key}:{kv.Value}/{(_servedBy.TryGetValue(kv.Key, out var sv) ? sv : 0)}");
            Log($"v1.8 ÖLÇÜM SİPARİŞ: toplam {_ordersAll} · yemek {_ordersFood} · içecek {drinkOrders} · Kral Şerbeti {king} (içeceklerin %{(drinkOrders > 0 ? 100f * king / drinkOrders : 0f):0.0}) · [sipariş/servis] {string.Join(" ", parts)}");
            var ms = new List<string>();
            foreach (var st in DrinkStation.All)
            {
                if (!st || !st.drink) continue;
                var s = st.Save;
                ms.Add($"{st.drink.id}(h{s.speed} s{s.stock} k{s.quality} raf{s.depotLv} f{st.Price}) aç {(_starved.TryGetValue(st.drink.id, out var a) ? a : 0):0}sn dolu {(_full.TryGetValue(st.drink.id, out var f) ? f : 0):0}sn malzemesiz {(_outage.TryGetValue(st.drink.id, out var o) ? o : 0):0}sn");
            }
            Log($"v1.8 ÖLÇÜM MAKİNE: {string.Join(" · ", ms)} · teslimat sv{GameState.Data.deliveryLv}");
            Log($"v1.8 ÖLÇÜM PENCERE: {WindowFrom:0}–{WindowTo:0} oyun-sn altın {(_windowGold0 >= 0 && _windowGold1 >= 0 ? _windowGold1 - _windowGold0 : -1)} · etkinlik '{EventArg}' fiyat {_eventCost}");
        }
    }
}
#endif
