using UnityEngine;
using UnityEngine.InputSystem;

namespace Taverna
{
    /// <summary>Barlo: joystick (veya WASD/oklar) ile hareket. Etkileşimler yaklaşınca otomatik.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController I { get; private set; }

        /// <summary>HUD joystick'i buraya yazar (-1..1).</summary>
        public static Vector2 JoystickInput;

        public float speed = Balance.PlayerSpeed;
        public float turnSpeed = 900f;
        public ToyAnimator anim;
        public Carrier carrier;

        CharacterController _cc;
        Vector3 _vel;

        void Awake()
        {
            I = this;
            _cc = GetComponent<CharacterController>();
            if (!carrier) carrier = GetComponent<Carrier>();
        }

        /// <summary>Kat geçişi (merdiven): karakter denetleyicisi kapatılıp konum atanır.</summary>
        public void Teleport(Vector3 pos)
        {
            _cc.enabled = false;
            transform.position = pos;
            _cc.enabled = true;
            _hasSafe = false;   // v2.4.1: güvenlik ağı yeni yerden başlar
        }

        // ------------------------------------------------------------------ v2.4.1 güvenlik ağı
        // Oyuncu testi (2026-11-23): masa kurarken Barlo haritanın dışına itildi ve geri giremedi.
        // Neden: önde duvar yok, yalnız 0.3 m'lik görünmez sınır var. Barlo'nun üstünde beliren
        // (büyüyerek açılan) ya da düzenleme modunda üstüne sürüklenen masa, karakter denetleyicisini
        // dışarı itiyor ve bu ince sınırın öbür yanına geçirebiliyordu.
        // Önlem: (1) yeni nesne Barlo'nun üstünde açılırsa Barlo önce boş bir yere alınır,
        // (2) düzenleme modunda Barlo donar, kapanışta boş yere alınır, (3) her karede son güvenli
        // konumdan bu yana bir duvar ya da sınır geçildiyse Barlo içeri geri alınır.

        /// <summary>Çakışma yoklamasında kapsülün yarıçapı (m): denetleyicinin 0.28 m'sinden biraz dar, sürtünme sayılmasın.</summary>
        const float ProbeRadius = 0.24f;
        /// <summary>Kapsülün alt ve üst küre merkezlerinin yüksekliği (m): zemin çarpıştırıcısına değmez.</summary>
        const float ProbeLow = 0.32f, ProbeHigh = 0.8f;
        /// <summary>Duvar geçişi çizgisinin yüksekliği (m).</summary>
        const float LineHeight = 0.55f;
        /// <summary>Boş yer aramasının en büyük yarıçapı ve halka adımı (m).</summary>
        const float SearchRadius = 5f, SearchStep = 0.25f;
        /// <summary>Bu yüksekliğin altına düşen oyuncu geri alınır (m).</summary>
        const float FallY = -2f;

        static readonly Collider[] _hits = new Collider[16];
        static readonly RaycastHit[] _rays = new RaycastHit[16];
        Vector3 _lastSafe;
        bool _hasSafe;

        /// <summary>Kaç kez geri alındı (test botu ve günlük için).</summary>
        public static int Rescues { get; private set; }

        /// <summary>Bu konumda Barlo'nun gövdesi katı bir nesneyle (masa, duvar, makine) çakışıyor mu? Tetikleyiciler ve kendisi sayılmaz.</summary>
        public bool Blocked(Vector3 p)
        {
            int n = Physics.OverlapCapsuleNonAlloc(p + Vector3.up * ProbeLow, p + Vector3.up * ProbeHigh, ProbeRadius, _hits, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) if (_hits[i] && _hits[i] != _cc) return true;
            return false;
        }

        /// <summary><paramref name="a"/>'dan <paramref name="b"/>'ye giden yolda katı bir nesne (duvar, sınır) var mı? İçinden başlanan nesne sayılmaz.</summary>
        bool Crosses(Vector3 a, Vector3 b)
        {
            var from = a + Vector3.up * LineHeight;
            var d = b - a;
            d.y = 0f;
            float len = d.magnitude;
            if (len < 1e-4f) return false;
            int n = Physics.RaycastNonAlloc(from, d / len, _rays, len, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++) if (_rays[i].collider && _rays[i].collider != _cc) return true;
            return false;
        }

        /// <summary>
        /// <paramref name="from"/>'a en yakın, Barlo'nun sığdığı ve oradan duvar geçmeden varılan boş yeri arar
        /// (halka halka, <see cref="SearchRadius"/> metreye kadar).
        /// </summary>
        public bool FindFreeSpot(Vector3 from, out Vector3 spot)
        {
            spot = from;
            if (!Blocked(from)) return true;
            for (float r = SearchStep; r <= SearchRadius + 1e-3f; r += SearchStep)
            {
                int steps = Mathf.Max(8, Mathf.CeilToInt(2f * Mathf.PI * r / SearchStep));
                for (int k = 0; k < steps; k++)
                {
                    float a = k * Mathf.PI * 2f / steps;
                    var c = from + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                    if (Blocked(c) || Crosses(from, c)) continue;
                    spot = c;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Barlo katı bir nesnenin içinde kaldıysa (yeni açılan masa, taşınan eşya) en yakın boş yere alınır.</summary>
        public void EnsureFree()
        {
            Physics.SyncTransforms();
            var p = transform.position;
            if (!Blocked(p)) return;
            if (FindFreeSpot(p, out var spot)) Rescue(spot, "çakışma");
        }

        void Rescue(Vector3 to, string why)
        {
            Rescues++;
            Debug.Log($"[Taverna] Barlo güvenli yere alındı ({why}): {transform.position:F2} → {to:F2}");
            Teleport(to);
            _lastSafe = to;
            _hasSafe = true;
            _vel = Vector3.zero;
        }

        /// <summary>Her karede: düştüyse ya da son güvenli konumdan bu yana bir duvar/sınır geçtiyse içeri geri alınır.</summary>
        void Guard()
        {
            var p = transform.position;
            if (!_hasSafe)
            {
                if (p.y > FallY && !Blocked(p)) { _lastSafe = p; _hasSafe = true; }
                return;
            }
            if (p.y < FallY || Crosses(_lastSafe, p))
            {
                if (!FindFreeSpot(_lastSafe, out var spot)) spot = _lastSafe;
                Rescue(spot, p.y < FallY ? "düştü" : "duvar geçti");
                return;
            }
            if (!Blocked(p)) _lastSafe = p;
        }

        void Update()
        {
            // v2.4.1: düzenleme modunda Barlo donar (üstüne sürüklenen masa onu itmesin); kapanışta EnsureFree
            if (EditLayout.Active) { _vel = Vector3.zero; if (anim) anim.SetSpeed(0f); return; }

            Vector2 input = JoystickInput;
            var kb = Keyboard.current;
            if (kb != null && input.sqrMagnitude < 0.01f)
            {
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1;
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1;
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1;
            }
            input = Vector2.ClampMagnitude(input, 1f);
            if (Ending.Playing) input = Vector2.zero;   // v2.1: son sahnede Barlo'yu sahne yönetir
            // v2.1 Barlo'nun yetenekleri: koşu hızı ve taşıma kapasitesi kayıttan (BarloSkills)
            speed = BarloSkills.Speed;
            if (carrier) carrier.capacity = BarloSkills.Carry;

            // Kamera sabit ve +Z'ye bakıyor → girdi doğrudan dünya XZ düzlemine eşlenir.
            var move = new Vector3(input.x, 0f, input.y);
            _vel = Vector3.Lerp(_vel, move * speed, 1f - Mathf.Exp(-14f * Time.deltaTime));
            _cc.SimpleMove(_vel);
            Guard();   // v2.4.1

            if (move.sqrMagnitude > 0.01f)
            {
                var target = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
            }
            if (anim) anim.SetSpeed(_vel.magnitude / speed);
        }
    }
}
