using System;
using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>XZ düzleminde eksene hizalı dikdörtgen (dünya metresi).</summary>
    [Serializable]
    public struct RectXZ
    {
        public float minX, minZ, maxX, maxZ;
        public RectXZ(float minX, float minZ, float maxX, float maxZ) { this.minX = minX; this.minZ = minZ; this.maxX = maxX; this.maxZ = maxZ; }
        /// <summary>Merkez ve yarı boyutlardan.</summary>
        public static RectXZ Center(float cx, float cz, float hx, float hz) => new(cx - hx, cz - hz, cx + hx, cz + hz);
        public Vector2 Mid => new((minX + maxX) * 0.5f, (minZ + maxZ) * 0.5f);
        public float Area => Mathf.Max(0f, maxX - minX) * Mathf.Max(0f, maxZ - minZ);
        public bool Contains(Vector2 p) => p.x >= minX && p.x <= maxX && p.y >= minZ && p.y <= maxZ;
        /// <summary>İç dikdörtgen tamamen içinde mi?</summary>
        public bool Contains(RectXZ r) => r.minX >= minX - 0.001f && r.maxX <= maxX + 0.001f && r.minZ >= minZ - 0.001f && r.maxZ <= maxZ + 0.001f;
        /// <summary>Gerçekten üst üste biniyorlar mı (kenar teması sayılmaz)?</summary>
        public bool Overlaps(RectXZ o) => minX < o.maxX - 0.01f && o.minX < maxX - 0.01f && minZ < o.maxZ - 0.01f && o.minZ < maxZ - 0.01f;
        public RectXZ Expand(float m) => new(minX - m, minZ - m, maxX + m, maxZ + m);
        public override string ToString() => $"[{minX:0.##},{minZ:0.##} → {maxX:0.##},{maxZ:0.##}]";
    }

    /// <summary>Yerleşim denetiminin sonucu (geçersizse nedeni).</summary>
    public enum PlaceResult
    {
        /// <summary>Bırakılabilir.</summary>
        Ok,
        /// <summary>Bölge sınırı dışı.</summary>
        OutOfBounds,
        /// <summary>Duvar, tezgâh / makine, kapı, merdiven, ödeme karesi ya da yürüme yolu.</summary>
        Blocked,
        /// <summary>Başka bir masa / dekor ya da henüz alınmamış masanın yeri.</summary>
        Overlap,
        /// <summary>Bir sandalye ya da servis noktası erişilemez kalır.</summary>
        Unreachable,
    }

    /// <summary>
    /// v2.3 düzenleme modu ızgarası (GAME_DESIGN.md §17): bir bölgenin zemini <see cref="Cell"/> m'lik karelere bölünür.
    /// Kareler sabit engel (duvar, makine... sahneden çarpıştırıcıyla okunur) ya da eşya izi (dikdörtgen) ile kapanır.
    /// Erişim: kapıdan 4 yönlü yayılma; yol: 8 yönlü A* (köşe kesmeden) + görüş hattıyla sadeleştirme.
    /// Unity sahnesine bağımlı değil (birim testli).
    /// </summary>
    public class LayoutGrid
    {
        /// <summary>Kare kenarı (m).</summary>
        public const float Cell = 0.25f;
        /// <summary>Oturak ya da servis noktası bu uzaklıkta (m) erişilen bir kare varsa erişilebilir sayılır.</summary>
        public const float AccessRadius = 0.55f;

        public readonly RectXZ bounds;
        public readonly int w, h;
        readonly bool[] _static;

        public LayoutGrid(RectXZ bounds)
        {
            this.bounds = bounds;
            w = Mathf.Max(1, Mathf.CeilToInt((bounds.maxX - bounds.minX) / Cell - 0.001f));
            h = Mathf.Max(1, Mathf.CeilToInt((bounds.maxZ - bounds.minZ) / Cell - 0.001f));
            _static = new bool[w * h];
        }

        public Vector2 CenterOf(int i, int j) => new(bounds.minX + (i + 0.5f) * Cell, bounds.minZ + (j + 0.5f) * Cell);
        public bool CellOf(Vector2 p, out int i, out int j)
        {
            i = Mathf.FloorToInt((p.x - bounds.minX) / Cell);
            j = Mathf.FloorToInt((p.y - bounds.minZ) / Cell);
            return i >= 0 && j >= 0 && i < w && j < h;
        }

        /// <summary>Sabit engel: kare merkezi dikdörtgenin içinde kalan kareler kapanır.</summary>
        public void BlockStatic(RectXZ r)
        {
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                    if (r.Contains(CenterOf(i, j))) _static[j * w + i] = true;
        }

        /// <summary>Sabit engeli kare kare bir sorguyla doldurur (oyunda: fizik çarpıştırıcısı var mı).</summary>
        public void FillStatic(Func<Vector2, bool> blocked)
        {
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                    if (blocked(CenterOf(i, j))) _static[j * w + i] = true;
        }

        public bool IsStatic(int i, int j) => _static[j * w + i];

        /// <summary>Dikdörtgenin içine düşen herhangi bir kare sabit engel mi?</summary>
        public bool StaticBlocked(RectXZ r)
        {
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                    if (_static[j * w + i] && r.Contains(CenterOf(i, j))) return true;
            return false;
        }

        /// <summary>Engel haritası: sabit engel ya da merkezi bir izin içinde kalan kare.</summary>
        public bool[] Blocked(IList<RectXZ> footprints)
        {
            var b = (bool[])_static.Clone();
            if (footprints == null) return b;
            foreach (var r in footprints)
            {
                int i0 = Mathf.Max(0, Mathf.FloorToInt((r.minX - bounds.minX) / Cell - 0.5f)), i1 = Mathf.Min(w - 1, Mathf.CeilToInt((r.maxX - bounds.minX) / Cell));
                int j0 = Mathf.Max(0, Mathf.FloorToInt((r.minZ - bounds.minZ) / Cell - 0.5f)), j1 = Mathf.Min(h - 1, Mathf.CeilToInt((r.maxZ - bounds.minZ) / Cell));
                for (int j = j0; j <= j1; j++)
                    for (int i = i0; i <= i1; i++)
                        if (r.Contains(CenterOf(i, j))) b[j * w + i] = true;
            }
            return b;
        }

        /// <summary>Başlangıç noktalarından 4 yönlü yayılma; ulaşılan kareler true.</summary>
        public bool[] Flood(bool[] blocked, IList<Vector2> starts)
        {
            var reach = new bool[w * h];
            var q = new Queue<int>();
            if (starts != null)
                foreach (var s in starts)
                    if (CellOf(s, out int i, out int j) && !blocked[j * w + i] && !reach[j * w + i]) { reach[j * w + i] = true; q.Enqueue(j * w + i); }
            while (q.Count > 0)
            {
                int c = q.Dequeue(), ci = c % w, cj = c / w;
                Visit(ci + 1, cj); Visit(ci - 1, cj); Visit(ci, cj + 1); Visit(ci, cj - 1);
            }
            return reach;

            void Visit(int i, int j)
            {
                if (i < 0 || j < 0 || i >= w || j >= h) return;
                int k = j * w + i;
                if (reach[k] || blocked[k]) return;
                reach[k] = true;
                q.Enqueue(k);
            }
        }

        /// <summary>Nokta erişilebilir mi: <paramref name="radius"/> içinde ulaşılmış bir kare var mı?</summary>
        public bool Reached(bool[] reach, Vector2 p, float radius = AccessRadius)
        {
            int r = Mathf.CeilToInt(radius / Cell) + 1;
            CellOf(p, out int pi, out int pj);
            for (int j = pj - r; j <= pj + r; j++)
                for (int i = pi - r; i <= pi + r; i++)
                {
                    if (i < 0 || j < 0 || i >= w || j >= h || !reach[j * w + i]) continue;
                    if ((CenterOf(i, j) - p).sqrMagnitude <= radius * radius) return true;
                }
            return false;
        }

        /// <summary>
        /// Kapıdan hedefe yol (8 yönlü A*, köşe kesmeden): başlangıç noktasından hedefin <see cref="AccessRadius"/>
        /// yakınındaki en yakın erişilebilir kareye; görüş hattıyla sadeleştirilmiş ara noktalar (başlangıç dahil, hedef hariç).
        /// Yol yoksa null.
        /// </summary>
        public List<Vector2> FindPath(bool[] blocked, Vector2 from, Vector2 to, float goalRadius = AccessRadius)
        {
            if (!CellOf(from, out int si, out int sj) || blocked[sj * w + si]) return null;
            int start = sj * w + si;
            var g = new float[w * h];
            var parent = new int[w * h];
            var closed = new bool[w * h];
            for (int k = 0; k < g.Length; k++) { g[k] = float.MaxValue; parent[k] = -1; }
            g[start] = 0f;
            var open = new List<(float f, int k)> { (Heur(start), start) };
            int goal = -1;
            float gr2 = goalRadius * goalRadius;
            while (open.Count > 0)
            {
                int best = 0;
                for (int n = 1; n < open.Count; n++) if (open[n].f < open[best].f) best = n;
                int c = open[best].k;
                open.RemoveAt(best);
                if (closed[c]) continue;
                closed[c] = true;
                int ci = c % w, cj = c / w;
                if ((CenterOf(ci, cj) - to).sqrMagnitude <= gr2) { goal = c; break; }
                for (int dj = -1; dj <= 1; dj++)
                    for (int di = -1; di <= 1; di++)
                    {
                        if (di == 0 && dj == 0) continue;
                        int ni = ci + di, nj = cj + dj;
                        if (ni < 0 || nj < 0 || ni >= w || nj >= h) continue;
                        int nk = nj * w + ni;
                        if (blocked[nk] || closed[nk]) continue;
                        if (di != 0 && dj != 0 && (blocked[cj * w + ni] || blocked[nj * w + ci])) continue;   // köşe kesme yok
                        float ng = g[c] + (di != 0 && dj != 0 ? 1.4142f : 1f);
                        if (ng >= g[nk]) continue;
                        g[nk] = ng;
                        parent[nk] = c;
                        open.Add((ng + Heur(nk), nk));
                    }
            }
            if (goal < 0) return null;
            var cells = new List<Vector2>();
            for (int k = goal; k >= 0; k = parent[k]) cells.Add(CenterOf(k % w, k / w));
            cells.Reverse();
            cells[0] = from;
            // görüş hattıyla sadeleştir
            var path = new List<Vector2> { cells[0] };
            int anchor = 0;
            for (int n = 2; n < cells.Count; n++)
                if (!LineClear(blocked, cells[anchor], cells[n])) { anchor = n - 1; path.Add(cells[anchor]); }
            if (cells.Count > 1) path.Add(cells[^1]);
            return path;

            float Heur(int k) => (CenterOf(k % w, k / w) - to).magnitude / Cell;
        }

        /// <summary>İki nokta arasındaki düz çizgi kapalı kareden geçmiyor mu (0.1 m adımla örnekler)?</summary>
        public bool LineClear(bool[] blocked, Vector2 a, Vector2 b)
        {
            float len = (b - a).magnitude;
            int steps = Mathf.Max(1, Mathf.CeilToInt(len / 0.1f));
            for (int s = 0; s <= steps; s++)
            {
                var p = Vector2.Lerp(a, b, s / (float)steps);
                if (!CellOf(p, out int i, out int j)) return false;
                if (blocked[j * w + i]) return false;
            }
            return true;
        }

        /// <summary>Kırık çizgi (yürüme yolu) dikdörtgenin içinden geçiyor mu (0.1 m adımla)?</summary>
        public static bool PolylineCrosses(IList<Vector2> pts, RectXZ r)
        {
            for (int n = 0; n + 1 < pts.Count; n++)
            {
                var a = pts[n]; var b = pts[n + 1];
                int steps = Mathf.Max(1, Mathf.CeilToInt((b - a).magnitude / 0.1f));
                for (int s = 0; s <= steps; s++) if (r.Contains(Vector2.Lerp(a, b, s / (float)steps))) return true;
            }
            return false;
        }
    }

    /// <summary>
    /// v2.3 yerleşim kuralları (GAME_DESIGN.md §17.2). Sürüklenen nesnenin yeni izi şunlardan biriyse bırakılamaz (kırmızı):
    /// bölge sınırı dışı; sabit engel (duvar, makine, tezgâh); açık tutulacak alan (kapı, merdiven, makine önü, ödeme karesi, toplanma noktası);
    /// başka bir nesne ya da alınmamış masanın yeri; ya da bir oturak / servis noktası erişilemez kalıyor.
    /// Düz eşyalar (halı, taş yol) üstünden yürünür: yalnızca sınır, sabit engel ve diğer düz eşyalarla çakışma denetlenir.
    /// </summary>
    public static class LayoutRules
    {
        /// <summary>Yerleşim denetimi girdisi (bir bölge, bir sürüklenen nesne).</summary>
        public class Input
        {
            public LayoutGrid grid;
            /// <summary>Sürüklenen nesnenin yeni izi.</summary>
            public RectXZ candidate;
            /// <summary>Düz eşya (halı, taş yol)?</summary>
            public bool flat;
            /// <summary>Açık tutulacak alanlar (yürünebilir ama üstüne eşya konmaz).</summary>
            public List<RectXZ> keepClear = new();
            /// <summary>Alınmamış masaların varsayılan yerleri (ayrılmış).</summary>
            public List<RectXZ> reserved = new();
            /// <summary>Diğer (düz olmayan) nesnelerin izleri.</summary>
            public List<RectXZ> others = new();
            /// <summary>Diğer düz eşyaların izleri (yalnızca düz eşya sürüklenirken denetlenir).</summary>
            public List<RectXZ> otherFlats = new();
            /// <summary>Müşterinin girdiği noktalar (kapı içi).</summary>
            public List<Vector2> starts = new();
            /// <summary>Erişilebilir kalması gereken noktalar: diğer masaların oturakları, servis noktaları, iç kapılar.</summary>
            public List<Vector2> targets = new();
            /// <summary>Sürüklenen masanın kendi oturakları (yeni yerde erişilebilir olmalı).</summary>
            public List<Vector2> ownTargets = new();
        }

        /// <summary>Kuralları sırayla uygular; ilk tutmayan kuralın nedenini döner.</summary>
        public static PlaceResult Check(Input c)
        {
            if (!c.grid.bounds.Contains(c.candidate)) return PlaceResult.OutOfBounds;
            if (c.grid.StaticBlocked(c.candidate)) return PlaceResult.Blocked;
            if (c.flat)
            {
                foreach (var r in c.otherFlats) if (r.Overlaps(c.candidate)) return PlaceResult.Overlap;
                return PlaceResult.Ok;
            }
            foreach (var r in c.keepClear) if (r.Overlaps(c.candidate)) return PlaceResult.Blocked;
            foreach (var r in c.reserved) if (r.Overlaps(c.candidate)) return PlaceResult.Overlap;
            foreach (var r in c.others) if (r.Overlaps(c.candidate)) return PlaceResult.Overlap;
            // Erişim: nesne yokken erişilen her hedef, nesne yeni yerindeyken de erişilmeli; kendi oturakları da
            var without = c.grid.Flood(c.grid.Blocked(c.others), c.starts);
            var withList = new List<RectXZ>(c.others) { c.candidate };
            var with = c.grid.Flood(c.grid.Blocked(withList), c.starts);
            foreach (var t in c.targets)
                if (c.grid.Reached(without, t) && !c.grid.Reached(with, t)) return PlaceResult.Unreachable;
            foreach (var t in c.ownTargets)
                if (!c.grid.Reached(with, t)) return PlaceResult.Unreachable;
            return PlaceResult.Ok;
        }
    }
}
