using UnityEngine;

namespace Taverna
{
    /// <summary>Havaya zıplayıp dönen, sonra küçülüp kaybolan para (menü dioraması süsü).</summary>
    public class CoinHop : MonoBehaviour
    {
        public float life = 0.9f;
        public float height = 0.7f;

        Vector3 _start, _drift;
        float _t;

        /// <summary><paramref name="prefab"/>'dan bir para yaratır ve zıplatır.</summary>
        public static void Spawn(GameObject prefab, Vector3 pos)
        {
            var go = Instantiate(prefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            go.transform.localScale = Vector3.one * 1.4f;
            go.AddComponent<CoinHop>();
        }

        void Start()
        {
            _start = transform.position;
            var r = Random.insideUnitCircle * 0.25f;
            _drift = new Vector3(r.x, 0f, r.y);
        }

        void Update()
        {
            _t += Time.deltaTime / life;
            if (_t >= 1f) { Destroy(gameObject); return; }
            transform.position = _start + _drift * _t + Vector3.up * (Mathf.Sin(_t * Mathf.PI) * height);
            transform.Rotate(0f, 720f * Time.deltaTime, 0f, Space.World);
            if (_t > 0.7f) transform.localScale = Vector3.one * 1.4f * Mathf.InverseLerp(1f, 0.7f, _t);
        }
    }
}
