using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Han odası (v0.8): konfor kademesine göre yatak görünümü, misafir rezervasyonu, sabah dağınıklığı.
    /// Dağınık oda Barlo yanına gelince (ya da Hizmetçi) toplanır; toplanmadan misafir almaz.
    /// </summary>
    public class Room : MonoBehaviour
    {
        public enum State { Free, Reserved, Occupied, Dirty }

        public static readonly List<Room> All = new();

        public int index;
        public GameObject[] beds = new GameObject[3];
        public GameObject messy;
        /// <summary>v2.0 (madde 19): konfor eşyaları ve her birinin göründüğü en düşük konfor seviyesi (kilim, komodin, lamba, dolap, şömine, tablo).</summary>
        public GameObject[] items = new GameObject[0];
        public int[] itemLevels = new int[0];
        public Transform bedPoint;
        public float cleanSeconds = 1.2f;

        public State state { get; private set; }
        int _shownComfort = -1;
        float _cleanT;

        void OnEnable()
        {
            All.Add(this);
            All.Sort((a, b) => a.index.CompareTo(b.index));
            if (messy) messy.SetActive(state == State.Dirty);
        }

        void OnDisable() => All.Remove(this);

        public static Room FirstFree()
        {
            foreach (var r in All) if (r.isActiveAndEnabled && r.state == State.Free) return r;
            return null;
        }

        /// <summary>v2.0: en yüksek konforlu boş oda (eşitlikte sıradaki ilk).</summary>
        public static Room BestFree()
        {
            Room best = null;
            foreach (var r in All)
                if (r.isActiveAndEnabled && r.state == State.Free && (!best || Inn.Comfort(r.index) > Inn.Comfort(best.index))) best = r;
            return best;
        }

        /// <summary>Konfor seviyesinin yatak modeli: 0 saman, 1–3 yün, 4 kuştüyü (beds dizisinin indeksi).</summary>
        public static int BedFor(int comfort) => comfort <= 0 ? 0 : comfort >= Inn.Fees.Length - 1 ? 2 : 1;

        public int Fee => Inn.Fee(index);

        void Update()
        {
            int c = Inn.Comfort(index);
            if (c != _shownComfort)
            {
                int bed = BedFor(c);
                for (int i = 0; i < beds.Length; i++) if (beds[i]) beds[i].SetActive(i == bed);
                for (int i = 0; i < items.Length && i < itemLevels.Length; i++)
                {
                    if (!items[i]) continue;
                    bool on = c >= itemLevels[i];
                    if (on && !items[i].activeSelf && _shownComfort >= 0) items[i].AddComponent<PopIn>();   // yeni eşya zıplayarak gelir
                    items[i].SetActive(on);
                }
                _shownComfort = c;
            }
            if (state != State.Dirty) return;
            var p = PlayerController.I;
            if (p && DrinkStation.FlatDistance(p.transform.position, bedPoint.position) < 1.4f)
            {
                _cleanT += Time.deltaTime;
                if (_cleanT >= cleanSeconds) Clean();
            }
            else _cleanT = 0f;
        }

        public void Reserve() { if (state == State.Free) state = State.Reserved; }
        public void Release() { if (state == State.Reserved || state == State.Occupied) state = State.Free; }
        public void Occupy() => state = State.Occupied;

        public void MakeDirty()
        {
            state = State.Dirty;
            if (messy) messy.SetActive(true);
        }

        public void Clean()
        {
            if (state != State.Dirty) return;
            state = State.Free;
            Inn.RoomsCleaned++;
            _cleanT = 0f;
            if (messy) messy.SetActive(false);
            Sfx.Play(SfxId.Wipe, 0.95f);
            GameState.NotifyChanged();
        }
    }
}
