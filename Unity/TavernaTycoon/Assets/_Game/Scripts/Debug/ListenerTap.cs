#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v2.4 ses teşhisi: AudioListener'a takılır; dinleyicinin son karışımını <see cref="AudioCapture"/>'a verir ve çıkışı sıfırlar
    /// (gizli test hoparlörden duyulmaz). Yalnızca geliştirme derlemesinde otomatik testte eklenir.
    /// </summary>
    public class ListenerTap : MonoBehaviour
    {
        void OnAudioFilterRead(float[] data, int channels)
        {
            var c = AudioCapture.I;
            if (!ReferenceEquals(c, null)) c.Feed(data);   // ses iş parçacığı: Unity nesne karşılaştırması kullanılmaz
            Array.Clear(data, 0, data.Length);
        }
    }
}
#endif
