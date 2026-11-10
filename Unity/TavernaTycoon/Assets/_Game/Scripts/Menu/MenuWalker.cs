using System.Collections;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Ana menü dioraması: durak listesini sonsuz döngüde dolaşan karakter (Barlo, Pip).
    /// Her durakta bekler, istenirse tepsisini açar/kapar, para zıplatır ve yakındaki müşterileri sevindirir.
    /// Kök nesneye eklenir; görsel model alt nesnedeki <see cref="ToyAnimator"/>'dır.
    /// </summary>
    public class MenuWalker : MonoBehaviour
    {
        /// <summary>Tek bir durak. <c>wait</c> = 0 ise sadece ara noktadır (durmadan geçer).</summary>
        [System.Serializable]
        public struct Stop
        {
            public Vector3 pos;
            public float wait;
            /// <summary>Bu duraktan sonra elindeki taşınacak nesne görünsün mü?</summary>
            public bool carrying;
            /// <summary>Durakta para zıplatılsın mı?</summary>
            public bool coin;
            /// <summary>Durakta yakındaki müşteriler sevinsin mi?</summary>
            public bool cheer;
            /// <summary>Durakta bakılacak nokta (sıfır vektör = hareket yönünde kal).</summary>
            public Vector3 lookAt;
        }

        public Stop[] stops;
        public float speed = 1.5f;
        public ToyAnimator anim;
        /// <summary>Taşınan nesne (tepsi + bardaklar, para yığını...). Duraklarda açılıp kapanır.</summary>
        public GameObject carried;
        public GameObject coinPrefab;

        IEnumerator Start()
        {
            if (stops == null || stops.Length == 0) yield break;
            transform.position = stops[0].pos;
            int i = 0;
            while (true)
            {
                var s = stops[i];
                yield return MoveTo(s.pos);
                if (s.wait > 0f)
                {
                    if (anim) { anim.SetSpeed(0f); anim.Pop(); }
                    if (s.lookAt != Vector3.zero) yield return Face(s.lookAt);
                    if (carried) carried.SetActive(s.carrying);
                    if (s.coin && coinPrefab) CoinHop.Spawn(coinPrefab, s.pos + transform.forward * 0.55f + Vector3.up * 0.7f);
                    if (s.cheer) MenuPatron.CheerNear(s.pos, 1.8f);
                    float t = 0f;
                    while (t < s.wait) { t += Time.deltaTime; if (anim) anim.SetSpeed(0f); yield return null; }
                }
                i = (i + 1) % stops.Length;
            }
        }

        IEnumerator MoveTo(Vector3 target)
        {
            target.y = transform.position.y;
            while ((target - transform.position).sqrMagnitude > 0.0004f)
            {
                var to = target - transform.position;
                float step = speed * Time.deltaTime;
                transform.position = to.magnitude <= step ? target : transform.position + to.normalized * step;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to.normalized, Vector3.up), Time.deltaTime * 10f);
                if (anim) anim.SetSpeed(1f);
                yield return null;
            }
        }

        IEnumerator Face(Vector3 point)
        {
            var dir = point - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) yield break;
            var goal = Quaternion.LookRotation(dir.normalized, Vector3.up);
            for (float t = 0f; t < 0.25f; t += Time.deltaTime)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, goal, t / 0.25f);
                yield return null;
            }
            transform.rotation = goal;
        }
    }
}
