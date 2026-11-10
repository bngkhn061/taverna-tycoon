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

        public int Fee => Inn.Fee(index);

        void Update()
        {
            int c = Inn.Comfort(index);
            if (c != _shownComfort)
            {
                for (int i = 0; i < beds.Length; i++) if (beds[i]) beds[i].SetActive(i == c);
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
