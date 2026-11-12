using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Sokaktaki tellal görseli ve reklamla çağrılan geçici yardımcı garson.
    /// v1.4: parayla "Tellal tut" satın alımı kaldırıldı; tellal artık bir etkinlik (TavernEvents.Id.Tellal).
    /// Etkinlik sürerken sokakta zil çalan tellal görünür, bitince kaybolur.
    /// </summary>
    public static class Campaigns
    {
        public const float HelperSeconds = 180f;
        public const float HelperCooldownSeconds = 600f;
        /// <summary>Tellalın sokaktaki yeri (tavernanın önü).</summary>
        public static readonly Vector3 CrierPos = new(0.5f, 0f, -6.4f);   // v1.2: sokak 3 m geride
        /// <summary>Tellalın zıplama aralığı (saniye).</summary>
        public const float CrierHopInterval = 1.1f;

        static float _helperUntil, _helperReadyAt;
        static GameObject _crier, _helper;
        static ToyAnimator _crierAnim;
        static float _hopT;

        /// <summary>Tellal etkinliği sürüyor mu?</summary>
        public static bool CrierActive => TavernEvents.Is(TavernEvents.Id.Tellal);
        /// <summary>Tellal karakteri sokakta görünüyor mu (test günlüğü).</summary>
        public static bool CrierVisible => _crier;

        public static bool HelperActive => _helper;
        public static float HelperRemaining => Mathf.Max(0f, _helperUntil - Time.unscaledTime);
        public static float HelperWait => Mathf.Max(0f, _helperReadyAt - Time.unscaledTime);

        static void SpawnCrier()
        {
            var db = GameDatabase.I;
            if (!db) return;
            // Yaver (15) tellal olur; yoksa ilk müşteri modeli
            GameObject prefab = null;
            foreach (var c in db.customers) if (c && c.number == 15 && c.modelPrefab) prefab = c.modelPrefab;
            if (!prefab) foreach (var c in db.customers) if (c && c.modelPrefab) { prefab = c.modelPrefab; break; }
            if (!prefab) return;
            _crier = new GameObject("Tellal");
            _crier.transform.SetPositionAndRotation(CrierPos, Quaternion.Euler(0f, 180f, 0f));
            var m = Object.Instantiate(prefab, _crier.transform);
            m.transform.localPosition = Vector3.zero;
            m.transform.localRotation = Quaternion.identity;
            _crierAnim = m.AddComponent<ToyAnimator>();
        }

        /// <summary>Reklam ödülü: mevcut bir garsonun kopyası olarak geçici yardımcı.</summary>
        public static bool CallHelper()
        {
            if (_helper || HelperWait > 0f) return false;
            var reg = TavernRegistry.I;
            if (!reg) return false;
            GameObject template = null;
            foreach (var id in new[] { "staff_tobi", "staff_mina", "staff_pip" })
            {
                var e = System.Array.Find(reg.staff, x => x.id == id);
                if (e != null && e.root && e.root.GetComponent<Waiter>()) { template = e.root; break; }
            }
            if (!template) return false;
            bool wasActive = template.activeSelf;
            template.SetActive(false); // kopya kapalı doğsun, ayarlandıktan sonra açılsın
            _helper = Object.Instantiate(template, template.transform.parent);
            template.SetActive(wasActive);
            _helper.name = "Yardimci_Garson";
            _helper.transform.position = PlayerController.I ? PlayerController.I.transform.position + Vector3.right : template.transform.position;
            var badge = _helper.GetComponent<StaffBadge>();
            if (badge) { badge.displayName = Loc.N("Yardımcı"); badge.staffId = ""; }
            _helper.SetActive(true);
            _helperUntil = Time.unscaledTime + HelperSeconds;
            _helperReadyAt = _helperUntil + HelperCooldownSeconds;
            GameState.NotifyChanged();
            return true;
        }

        /// <summary>Süreler: GameBootstrap her kare çağırır. Tellal etkinlik sürerken sokakta durur.</summary>
        public static void Tick()
        {
            if (CrierActive && !_crier) SpawnCrier();
            if (_crier)
            {
                if (!CrierActive) { Object.Destroy(_crier); _crier = null; }
                else if (_crierAnim)
                {
                    _hopT -= Time.deltaTime;
                    if (_hopT <= 0f) { _hopT = CrierHopInterval; _crierAnim.Hop(); _crierAnim.Pop(); }
                }
            }
            if (_helper && Time.unscaledTime >= _helperUntil) { Object.Destroy(_helper); _helper = null; }
        }
    }
}
