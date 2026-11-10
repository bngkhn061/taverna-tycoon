using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Masa: oturma yerleri, para noktası ve kirlilik (GAME_DESIGN.md §3.2.4).
    /// Müşteri gidince bardağı masada kalır; kirli masaya yeni müşteri oturmaz.
    /// Barlo yanında kısa süre durunca ya da temizlikçi gelince temizlenir.
    /// </summary>
    public class Table : MonoBehaviour
    {
        public static readonly List<Table> All = new();
        public Seat[] seats;
        public Transform coinPoint;
        public float ambiance = 1f;
        [Tooltip("Masanın bölgesinin giriş ve toplanma noktaları (müşteri yolu).")]
        public Transform entrance, hub;
        [System.NonSerialized] public bool claimedByCleaner;

        readonly List<GameObject> _dirt = new();
        float _cleanT;

        public bool Dirty => _dirt.Count > 0;
        public bool Busy { get { foreach (var s in seats) if (s && s.occupant) return true; return false; } }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public Vector3 CoinPos => coinPoint ? coinPoint.position : transform.position + Vector3.up * 0.65f;

        /// <summary>Boş bardağı masaya bırak (müşteri giderken).</summary>
        public void AddDirt(GameObject cup)
        {
            if (!cup) return;
            cup.transform.SetParent(transform, true);
            cup.transform.rotation = Quaternion.Euler(0f, Random.Range(0, 360), Random.Range(0f, 1f) < 0.3f ? 90f : 0f);
            _dirt.Add(cup);
        }

        public void Clean()
        {
            foreach (var d in _dirt) if (d) Destroy(d);
            _dirt.Clear();
            claimedByCleaner = false;
            Sfx.Play(SfxId.Wipe);
        }

        void Update()
        {
            if (!Dirty || Busy) { _cleanT = 0f; return; }
            var p = PlayerController.I;
            if (p && DrinkStation.FlatDistance(p.transform.position, transform.position) < Balance.InteractRadius + 0.2f)
            {
                _cleanT += Time.deltaTime;
                if (_cleanT > 0.6f) { Clean(); if (p.anim) p.anim.Pop(); }
            }
            else _cleanT = 0f;
        }
    }
}
