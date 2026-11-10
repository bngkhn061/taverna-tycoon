using UnityEngine;

namespace Taverna
{
    /// <summary>Yeni açılan nesne için küçük "büyüyerek belirme" efekti.</summary>
    public class PopIn : MonoBehaviour
    {
        float _t;
        Vector3 _target;
        void Awake() { _target = transform.localScale; transform.localScale = Vector3.zero; }
        void Update()
        {
            _t += Time.deltaTime * 2.5f;
            float s = _t < 1f ? 1f + Mathf.Sin(_t * Mathf.PI) * 0.25f * (1f - _t) : 1f;
            transform.localScale = _target * Mathf.Min(1f, _t) * s;
            if (_t >= 1f) { transform.localScale = _target; Destroy(this); }
        }
    }
}
