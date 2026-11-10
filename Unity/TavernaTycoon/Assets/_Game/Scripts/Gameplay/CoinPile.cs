using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>Masaya bırakılan para. Oyuncu yaklaşınca paralar ona uçar ve hesaba eklenir.</summary>
    public class CoinPile : MonoBehaviour
    {
        public static readonly List<CoinPile> All = new();
        public long amount;
        /// <summary>Bir garson bu yığını almaya gidiyor.</summary>
        [System.NonSerialized] public bool Claimed;
        Transform _collector;

        readonly List<Transform> _coins = new();
        bool _collecting;
        float _collectT, _age;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        /// <summary>Aynı noktadaki yığına ekler ya da yeni yığın açar.</summary>
        public static void Drop(Vector3 pos, long value)
        {
            var pile = All.Find(p => !p._collecting && !p.Claimed && Vector3.Distance(p.transform.position, pos) < 0.3f);
            if (!pile)
            {
                pile = new GameObject("CoinPile").AddComponent<CoinPile>();
                pile.transform.position = pos;
            }
            pile.Add(value);
        }

        void Add(long value)
        {
            amount += value;
            int visual = Mathf.Clamp((int)Mathf.Ceil(amount / 3f), 1, 12);
            var prefab = GameDatabase.I ? GameDatabase.I.coinPrefab : null;
            while (_coins.Count < visual && prefab)
            {
                var c = Instantiate(prefab, transform).transform;
                int i = _coins.Count;
                c.localPosition = new Vector3(((i % 3) - 1) * 0.07f, (i / 3) * 0.025f, ((i / 3) % 2) * 0.04f);
                c.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                c.localScale = Vector3.one * 0.9f;
                _coins.Add(c);
            }
        }

        public void Collect(Transform collector)
        {
            if (_collecting) return;
            _collecting = true;
            _collector = collector;
            GameState.AddCoins(amount, transform.position);
            Sfx.Play(SfxId.Coin);
        }

        void Update()
        {
            var p = PlayerController.I;
            // v0.9 Kasa otomasyonu: yığın kısa bir süre sonra kendiliğinden toplanır (paralar yukarı uçar)
            _age += Time.deltaTime;
            if (!_collecting && !Claimed && _age > Balance.AutoCollectDelay && Automation.Has("kasa")) Collect(null);
            if (!_collecting && p && DrinkStation.FlatDistance(p.transform.position, transform.position) < Balance.InteractRadius)
            {
                Collect(p.transform);
                GameSettings.Vibrate();
                if (p.anim) p.anim.Pop();
            }
            if (_collecting)
            {
                _collectT += Time.deltaTime * 3.5f;
                var target = _collector ? _collector.position + Vector3.up * 1.0f : transform.position + Vector3.up * 1.6f;
                for (int i = 0; i < _coins.Count; i++)
                {
                    float t = Mathf.Clamp01(_collectT - i * 0.05f);
                    var start = transform.position;
                    var mid = Vector3.Lerp(start, target, 0.5f) + Vector3.up * 0.8f;
                    _coins[i].position = Vector3.Lerp(Vector3.Lerp(start, mid, t), Vector3.Lerp(mid, target, t), t);
                    _coins[i].localScale = Vector3.one * Mathf.Lerp(0.9f, 0.3f, t);
                }
                if (_collectT > 1f + _coins.Count * 0.05f) Destroy(gameObject);
            }
        }
    }
}
