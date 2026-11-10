using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Ana menü dioraması: yerinde duran müşteri. Ara ara kendiliğinden sevinir (zıplar),
    /// servis gelince <see cref="CheerNear"/> ile topluca sevinir. <c>sway</c> açıksa (ozan) sallanır.
    /// </summary>
    public class MenuPatron : MonoBehaviour
    {
        static readonly List<MenuPatron> All = new();

        public ToyAnimator anim;
        public Vector2 cheerEvery = new(4f, 9f);
        /// <summary>Derece cinsinden yana sallanma (0 = yok). Ozan için.</summary>
        public float sway;
        public float swaySpeed = 2.2f;

        float _next;
        Quaternion _baseRot;

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Start()
        {
            _baseRot = transform.rotation;
            _next = Time.time + Random.Range(cheerEvery.x, cheerEvery.y);
        }

        void Update()
        {
            if (sway > 0f)
                transform.rotation = _baseRot * Quaternion.Euler(0f, Mathf.Sin(Time.time * swaySpeed * 0.5f) * sway, Mathf.Sin(Time.time * swaySpeed) * sway);
            if (Time.time >= _next)
            {
                Cheer();
                _next = Time.time + Random.Range(cheerEvery.x, cheerEvery.y);
            }
        }

        /// <summary>Bir kez zıplar.</summary>
        public void Cheer()
        {
            if (!anim) return;
            anim.Hop();
            anim.Pop();
        }

        /// <summary><paramref name="pos"/> çevresindeki tüm müşteriler sevinir (kısa gecikmelerle).</summary>
        public static void CheerNear(Vector3 pos, float radius)
        {
            float delay = 0f;
            foreach (var p in All)
            {
                var d = p.transform.position - pos;
                d.y = 0f;
                if (d.sqrMagnitude > radius * radius) continue;
                p.Invoke(nameof(Cheer), delay);
                delay += 0.12f;
            }
        }
    }
}
