using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v2.5 (oyuncu testi: son sahnede tabela kameranın önüne girip görüntünün solunu kapatıyordu): sinema çekimi sürerken kamerayla
    /// odak arasına giren büyük sahne nesnelerini (tabela, sancak, sütun, ağaç, fener direği, duvar parçası) gizler; sahne bitince
    /// hepsini geri açar. Kademe, şube ve yerleşimden bağımsız çalışır (nesne listesi değil, görüntüdeki yer ölçülür).
    /// <para>Kural: nesne konudan en az <see cref="FocusMargin"/> m kameraya yakın, en az <see cref="MinHeight"/> m yüksek, görüntünün
    /// en az <see cref="MinCoverage"/>'ını kaplıyor ve konunun görüntüdeki çevresine giriyorsa (ya da görüntünün
    /// <see cref="HugeCoverage"/>'ından fazlasını kaplıyorsa) gizlenir. Karakterler (<see cref="ToyAnimator"/>), masalar ve Kraliyet Fermanı
    /// hiç gizlenmez. Bir kez gizlenen nesne çekim bitene kadar gizli kalır (gidip gelip yanıp sönmesin).</para>
    /// </summary>
    public class CinemaOccluders
    {
        /// <summary>Nesnenin kameraya en yakın noktası odaktan en az bu kadar (m) önde olmalı (odaktaki tezgâh ve duvar kalsın).</summary>
        public const float FocusMargin = 1.4f;
        /// <summary>Bu yükseklikten (m) alçak nesneler (zemin, halı, sandalye, masa) hiç gizlenmez.</summary>
        public const float MinHeight = 0.9f;
        /// <summary>Görüntü alanının bu oranından fazlasını kaplayan nesne gizlenir (0..1).</summary>
        public const float MinCoverage = 0.035f;
        /// <summary>Konunun (kral, ferman) görüntüdeki çevresi: yarı genişlik ve yarı yükseklik (görüntü oranı). Nesne bu alana
        /// girmiyorsa konuyu örtmüyor sayılır (yanda duran tabela kalır).</summary>
        public const float SubjectHalfW = 0.22f, SubjectHalfH = 0.3f;
        /// <summary>Konuya değmese de görüntünün bu oranından fazlasını kaplayan yakın nesne yine gizlenir (kameranın burnunun dibindeki ağaç).</summary>
        public const float HugeCoverage = 0.25f;

        readonly List<Renderer> _candidates = new();
        readonly List<Renderer> _hidden = new();
        readonly Plane[] _planes = new Plane[6];
        readonly Vector3[] _corners = new Vector3[8];

        /// <summary>Gizlenen nesne sayısı (test günlüğü).</summary>
        public int HiddenCount => _hidden.Count;
        /// <summary>Gizlenen nesnelerin adları (test günlüğü, en çok 8).</summary>
        public string HiddenNames
        {
            get
            {
                var names = new List<string>();
                foreach (var r in _hidden) if (r && names.Count < 8) names.Add(r.name);
                return string.Join(", ", names);
            }
        }

        /// <summary>Sahnedeki aday nesneleri toplar (<paramref name="stageMinX"/>'ten ötesi, giriş sahnesi, hariç).</summary>
        public CinemaOccluders(float stageMinX = 60f)
        {
            foreach (var r in Object.FindObjectsByType<Renderer>())
            {
                if (!r || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                var b = r.bounds;
                if (b.size.y < MinHeight || b.center.x > stageMinX) continue;
                if (r.GetComponentInParent<ToyAnimator>() || r.GetComponentInParent<Table>() || r.GetComponentInParent<RoyalDecreeDecor>()
                    || r.GetComponentInParent<PlayerController>() || r.GetComponentInParent<Customer>() || r.GetComponentInParent<Seat>()) continue;
                _candidates.Add(r);
            }
        }

        /// <summary>
        /// Verilen kamera duruşunda (konum, dönüş) konuyu (<paramref name="focus"/>) örten nesneleri gizler. Gerçek kamera konumu için de, kameranın
        /// gitmekte olduğu hedef duruş için de çağrılabilir (nesne kamera oraya varmadan kalksın).
        /// </summary>
        public void Check(Camera cam, Vector3 camPos, Quaternion camRot, Vector3 focus)
        {
            if (!cam) return;
            var view = Matrix4x4.Scale(new Vector3(1f, 1f, -1f)) * Matrix4x4.TRS(camPos, camRot, Vector3.one).inverse;
            var vp = cam.projectionMatrix * view;
            GeometryUtility.CalculateFrustumPlanes(vp, _planes);
            float focusDist = Vector3.Distance(camPos, focus);
            var fh = vp * new Vector4(focus.x, focus.y, focus.z, 1f);
            float fx = fh.w > 0.01f ? fh.x / fh.w * 0.5f + 0.5f : 0.5f, fy = fh.w > 0.01f ? fh.y / fh.w * 0.5f + 0.5f : 0.5f;
            for (int i = _candidates.Count - 1; i >= 0; i--)
            {
                var r = _candidates[i];
                if (!r) { _candidates.RemoveAt(i); continue; }
                if (!r.enabled) continue;
                var b = r.bounds;
                if (!GeometryUtility.TestPlanesAABB(_planes, b)) continue;
                float near = Mathf.Sqrt(b.SqrDistance(camPos));
                if (near > focusDist - FocusMargin) continue;
                if (b.Contains(focus)) continue;
                float cov = Coverage(vp, b);
                if (cov < MinCoverage) continue;
                bool onSubject = _x0 < fx + SubjectHalfW && _x1 > fx - SubjectHalfW && _y0 < fy + SubjectHalfH && _y1 > fy - SubjectHalfH;
                if (!onSubject && cov < HugeCoverage) continue;
                r.enabled = false;
                _hidden.Add(r);
                _candidates.RemoveAt(i);
            }
        }

        /// <summary>Kutunun görüntüde kapladığı alan (0..1); bir köşesi kameranın arkasındaysa çok yakın sayılır (1).</summary>
        float _x0, _y0, _x1, _y1;

        float Coverage(Matrix4x4 vp, Bounds b)
        {
            Vector3 mn = b.min, mx = b.max;
            int k = 0;
            for (int x = 0; x < 2; x++)
                for (int y = 0; y < 2; y++)
                    for (int z = 0; z < 2; z++)
                        _corners[k++] = new Vector3(x == 0 ? mn.x : mx.x, y == 0 ? mn.y : mx.y, z == 0 ? mn.z : mx.z);
            float x0 = 1f, y0 = 1f, x1 = 0f, y1 = 0f;
            foreach (var c in _corners)
            {
                var h = vp * new Vector4(c.x, c.y, c.z, 1f);
                if (h.w <= 0.01f) { _x0 = _y0 = 0f; _x1 = _y1 = 1f; return 1f; }
                float sx = h.x / h.w * 0.5f + 0.5f, sy = h.y / h.w * 0.5f + 0.5f;
                x0 = Mathf.Min(x0, sx); x1 = Mathf.Max(x1, sx);
                y0 = Mathf.Min(y0, sy); y1 = Mathf.Max(y1, sy);
            }
            x0 = Mathf.Clamp01(x0); x1 = Mathf.Clamp01(x1); y0 = Mathf.Clamp01(y0); y1 = Mathf.Clamp01(y1);
            _x0 = x0; _x1 = x1; _y0 = y0; _y1 = y1;
            return Mathf.Max(0f, x1 - x0) * Mathf.Max(0f, y1 - y0);
        }

        /// <summary>Yeni çekim: gizlenenler geri açılır ve yeniden aday olur (aynı karede yeni çekimin yoklaması yeniden gizler).</summary>
        public void Reset()
        {
            foreach (var r in _hidden) if (r) { r.enabled = true; _candidates.Add(r); }
            _hidden.Clear();
        }

        /// <summary>Gizlenen her şeyi geri açar.</summary>
        public void Restore()
        {
            foreach (var r in _hidden) if (r) r.enabled = true;
            _hidden.Clear();
        }
    }
}
