using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Oturum yaşam döngüsü (sahneler arası kalıcı, görünmez): açılışta session_start, arka plana geçişte / kapanışta
    /// session_end ve ölçüm kuyruğunu diske yazma.
    /// </summary>
    public class ServicesRunner : MonoBehaviour
    {
        /// <summary>Uygulama bu kadar saniyeden uzun arka planda kalırsa dönüşte yeni oturum sayılır.</summary>
        public const float NewSessionAfterSeconds = 30f;
        /// <summary>Ölçüm kuyruğu bu aralıkla (gerçek saniye) diske yazılır.</summary>
        public const float FlushIntervalSeconds = 5f;

        static ServicesRunner _i;
        float _flushT, _pausedAt = -1f;
        bool _ended;

        /// <summary>Tek örneği kurar (yoksa).</summary>
        public static void Ensure()
        {
            if (_i) return;
            var go = new GameObject("Services");
            go.hideFlags = HideFlags.HideInHierarchy;
            DontDestroyOnLoad(go);
            _i = go.AddComponent<ServicesRunner>();
            Services.BeginSession();
        }

        void Update()
        {
            CloudSave.Tick(Time.unscaledDeltaTime);   // v2.3: 10 dakikada bir otomatik bulut yedeği (sağlayıcı varsa)
            _flushT += Time.unscaledDeltaTime;
            if (_flushT < FlushIntervalSeconds) return;
            _flushT = 0f;
            Services.Analytics?.Flush(false);
        }

        void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                _pausedAt = Time.realtimeSinceStartup;
                Services.Analytics?.Flush(true);
                CloudSave.OnPause();   // v2.3: arka plana geçerken bulut yedeği (sağlayıcı varsa)
                return;
            }
            if (_pausedAt >= 0f && Time.realtimeSinceStartup - _pausedAt >= NewSessionAfterSeconds)
            {
                Services.EndSession(false);
                Services.BeginSession();
            }
            _pausedAt = -1f;
        }

        void OnApplicationQuit()
        {
            if (_ended) return;
            _ended = true;
            Services.EndSession(true);
        }
    }
}
