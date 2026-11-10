using UnityEngine;
using UnityEngine.InputSystem;

namespace Taverna
{
    /// <summary>
    /// Yüksek açılı, sabit yönlü takip kamerası (DESIGN_RULES §2).
    /// Geniş varsayılan görüş + iki parmakla / fare tekerleğiyle yakınlaştırma.
    /// </summary>
    [DefaultExecutionOrder(-50)]   // v1.1: kamera HUD'dan önce yerleşsin; yoksa dünya etiketleri (tabela adı) bir kare geriden gelip titriyor
    public class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public float pitch = 52f;
        public float distance = 11f;
        /// <summary>Yakınlaştırma sınırı: oyuncu testi "çok fazla yakınlaşıp uzaklaşıyor" (2026-11-10) → 9–14.</summary>
        public Vector2 zoomRange = new(9f, 14f);
        public float smooth = 6f;
        public Vector2 minXZ = new(-9f, -6f);
        public Vector2 maxXZ = new(9f, 2.2f);
        /// <summary>Üst kat (han odaları, v0.8) sınırları; hedef z &gt; Inn.UpZ iken kullanılır.</summary>
        public Vector2 upMinXZ = new(-1.5f, 37.5f);
        public Vector2 upMaxXZ = new(1.5f, 40.0f);

        Vector3 _focus;
        float _introT = -1f, _introDur = 2f;

        /// <summary>v0.9 giriş animasyonu: kamera sokağın uzağından, alçak açıyla tavernaya süzülür.</summary>
        public void Intro(float seconds) { _introDur = Mathf.Max(0.1f, seconds); _introT = 0f; }
        public void SkipIntro() => _introT = -1f;
        float _zoomTarget;
        float _pinchStart = -1f, _pinchDist;

        void Start() => SnapNow();

        /// <summary>Kamerayı yumuşatmadan hedefe oturtur (sahne başlangıcı, editör önizlemesi).</summary>
        public void SnapNow()
        {
            _zoomTarget = distance;
            if (target) _focus = Clamp(target.position);
            Apply();
        }

        void LateUpdate()
        {
            HandleZoom();
            distance = Mathf.Lerp(distance, _zoomTarget, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
            if (target) _focus = Vector3.Lerp(_focus, Clamp(target.position), 1f - Mathf.Exp(-smooth * Time.deltaTime));
            Apply();
        }

        void HandleZoom()
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                float s = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(s) > 0.01f) _zoomTarget = Mathf.Clamp(_zoomTarget - Mathf.Sign(s) * 1.2f, zoomRange.x, zoomRange.y);
            }
            var ts = Touchscreen.current;
            if (ts == null) return;
            int active = 0;
            Vector2 a = default, b = default;
            foreach (var t in ts.touches)
            {
                if (!t.press.isPressed) continue;
                if (active == 0) a = t.position.ReadValue(); else if (active == 1) b = t.position.ReadValue();
                active++;
            }
            if (active >= 2)
            {
                float d = Vector2.Distance(a, b);
                if (_pinchStart < 0f) { _pinchStart = d; _pinchDist = _zoomTarget; }
                else _zoomTarget = Mathf.Clamp(_pinchDist * _pinchStart / Mathf.Max(1f, d), zoomRange.x, zoomRange.y);
                PlayerController.JoystickInput = Vector2.zero;
            }
            else _pinchStart = -1f;
        }

        /// <summary>Teras açıksa üst kat kamerası sağa (terasa) da kayar.</summary>
        float UpMaxX => GameState.IsUnlocked("teras") ? 9.5f : upMaxXZ.x;

        Vector3 Clamp(Vector3 p) => Inn.Upstairs(p)
            ? new(Mathf.Clamp(p.x, upMinXZ.x, UpMaxX), 0.4f, Mathf.Clamp(p.z, upMinXZ.y, upMaxXZ.y))
            : new(Mathf.Clamp(p.x, GameState.IsUnlocked("iskele") ? minXZ.x - 6f : minXZ.x, GameState.IsUnlocked("avlu") ? maxXZ.x + 5f : maxXZ.x), 0.4f,
                  Mathf.Clamp(p.z, minXZ.y, maxXZ.y));   // v1.0: avlu (sağ) ve iskele (sol) açılınca kamera oralara da kayar

        void Apply()
        {
            var focus = _focus;
            float dist = distance, pt = pitch;
            if (_introT >= 0f)
            {
                _introT += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(_introT / _introDur);
                float e = 1f - (1f - k) * (1f - k) * (1f - k);
                focus += new Vector3(0f, 0f, -8f) * (1f - e);
                dist += 8f * (1f - e);
                pt = Mathf.Lerp(30f, pitch, e);
                if (k >= 1f) _introT = -1f;
            }
            var rot = Quaternion.Euler(pt, 0f, 0f);
            transform.SetPositionAndRotation(focus - rot * Vector3.forward * dist, rot);
        }
    }
}
