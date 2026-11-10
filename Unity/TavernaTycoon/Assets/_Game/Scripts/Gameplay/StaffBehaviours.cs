using UnityEngine;

namespace Taverna
{
    // Personel MonoBehaviour'ları kendi dosyalarında (Cleaner.cs, StaffIdle.cs): Unity, sahneye eklenen
    // bir bileşenin scriptini dosya adından bulur; ad uyuşmazsa sahnede "script eksik" kalır ve
    // oyun sürümü sahneyi okurken çöker.

    /// <summary>Personel için ortak yürüme yardımcısı.</summary>
    public static class StaffMove
    {
        /// <summary>Arka odalar (mutfak, oyun salonu) için kapıdan dolaşarak yürütür (KitchenLayout.Route); hedefe vardıysa true.</summary>
        public static bool ToRouted(Transform tr, Vector3 p, float speed, float stopDist, ToyAnimator anim)
        {
            var via = KitchenLayout.Route(tr.position, p);
            via.y = p.y;
            if ((via - p).sqrMagnitude > 0.0001f) { To(tr, via, speed, 0.12f, anim); return false; }
            return To(tr, p, speed, stopDist, anim);
        }

        /// <summary><paramref name="tr"/>'yi <paramref name="p"/>'ye doğru yürütür; vardıysa true.</summary>
        public static bool To(Transform tr, Vector3 p, float speed, float stopDist, ToyAnimator anim)
        {
            var to = p - tr.position;
            to.y = 0f;
            if (to.magnitude <= stopDist) { if (anim) anim.SetSpeed(0f); return true; }
            tr.position += to.normalized * Mathf.Min(speed * Time.deltaTime, to.magnitude);
            tr.rotation = Quaternion.RotateTowards(tr.rotation, Quaternion.LookRotation(to), 600f * Time.deltaTime);
            if (anim) anim.SetSpeed(1f);
            return false;
        }
    }
}
