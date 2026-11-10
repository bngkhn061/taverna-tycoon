using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// İskeletsiz "oyuncak" animasyonu: yürürken zıplama + öne eğilme + esneme, dururken nefes alma.
    /// DESIGN_RULES §7'deki "zıplamalı ve abartılı" hissi kemik olmadan verir.
    /// Görsel modeli taşıyan alt nesneye (Visual) eklenir.
    /// </summary>
    public class ToyAnimator : MonoBehaviour
    {
        public float hopHeight = 0.07f;
        public float hopFrequency = 9f;
        public float leanAngle = 8f;
        public float breathe = 0.02f;

        /// <summary><see cref="Hop"/> zıplamasının yüksekliği (metre). Sabit: kaydedilen alan düzeni değişmesin.</summary>
        const float JumpHeight = 0.28f;

        float _speed01, _phase, _pop, _jump, _work, _workPhase;

        /// <summary>v1.1 çalışma hareketi: öne eğilip ritmik bastırma (hamur yoğurma, bardak doldurma). 0 = yok.</summary>
        public void SetWork(float w01) => _work = Mathf.MoveTowards(_work, Mathf.Clamp01(w01), 0.08f);
        Vector3 _baseScale, _basePos;
        Quaternion _baseRot;

        void Awake()
        {
            _baseScale = transform.localScale;
            _basePos = transform.localPosition;
            _baseRot = transform.localRotation;
            _phase = Random.value * 10f;
        }

        public void SetSpeed(float speed01) => _speed01 = Mathf.Lerp(_speed01, Mathf.Clamp01(speed01), 0.25f);

        /// <summary>Küçük bir "zıp" (para toplama, servis, satın alma anında).</summary>
        public void Pop() => _pop = 1f;

        /// <summary>Sevinç zıplaması: havaya sıçrar ve iner (menü ve kutlamalar için). Çağrılmazsa etkisi yoktur.</summary>
        public void Hop() => _jump = 1f;

        void LateUpdate()
        {
            _phase += Time.deltaTime * hopFrequency * Mathf.Lerp(0.25f, 1f, _speed01);
            float hop = Mathf.Abs(Mathf.Sin(_phase)) * hopHeight * _speed01;
            float squash = 1f + (Mathf.Abs(Mathf.Sin(_phase)) - 0.5f) * 0.08f * _speed01 + Mathf.Sin(_phase * 0.35f) * breathe * (1f - _speed01);
            _pop = Mathf.MoveTowards(_pop, 0f, Time.deltaTime * 4f);
            float popS = 1f + Mathf.Sin(_pop * Mathf.PI) * 0.12f;
            _jump = Mathf.MoveTowards(_jump, 0f, Time.deltaTime * 2.2f);
            hop += Mathf.Sin(_jump * Mathf.PI) * JumpHeight;
            _workPhase += Time.deltaTime * 7f;
            float press = 0.5f + 0.5f * Mathf.Sin(_workPhase);
            hop -= _work * press * 0.03f;
            squash -= _work * press * 0.05f;
            transform.localPosition = _basePos + Vector3.up * hop;
            transform.localRotation = _baseRot * Quaternion.Euler(leanAngle * _speed01 + _work * (10f + 10f * press), 0f,
                                                                   Mathf.Sin(_phase) * 3f * _speed01 + _work * Mathf.Sin(_workPhase * 0.5f) * 4f);
            transform.localScale = new Vector3(_baseScale.x / Mathf.Sqrt(squash) * popS, _baseScale.y * squash * popS, _baseScale.z / Mathf.Sqrt(squash) * popS);
        }
    }
}
