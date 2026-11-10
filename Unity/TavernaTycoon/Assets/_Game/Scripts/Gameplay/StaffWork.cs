using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v1.1 (oyuncu testi: "aşçı sabit durmasın, gerçekten ekmek yapıyormuş gibi dursun; barmen de boş durmasın"):
    /// barmen ve aşçı iş yerlerinde dolaşır, her durakta yüzünü işe döner ve çalışma hareketi yapar
    /// (ToyAnimator.SetWork). Aşçının durakları: açık mutfak makinelerinin önü + hamur tezgâhı.
    /// Etkileri (makine hızı) değişmez; Management'ta.
    /// </summary>
    public class StaffWork : MonoBehaviour
    {
        public ToyAnimator anim;
        /// <summary>Sabit duraklar (dünya konumu) ve her durağın bakış açısı (Y derecesi).</summary>
        public Vector3[] spots = new Vector3[0];
        public float[] faces = new float[0];
        /// <summary>Aşçı: açık mutfak makinelerinin önünü de durak say.</summary>
        public bool kitchenStations;
        public float walkSpeed = 1.4f;
        public Vector2 workSeconds = new(2.5f, 5f);

        readonly List<(Vector3 pos, float face)> _options = new();
        Vector3 _target;
        float _face, _workT;
        bool _walking;
        int _last = -1;

        void OnEnable() => Next();

        void Next()
        {
            _options.Clear();
            for (int i = 0; i < spots.Length; i++) _options.Add((spots[i], i < faces.Length ? faces[i] : 0f));
            if (kitchenStations)
                foreach (var s in DrinkStation.All)
                    if (s && s.drink && s.drink.isFood && KitchenLayout.InKitchen(s.transform.position))
                        _options.Add((s.transform.position + new Vector3(0f, 0f, -0.85f), 0f));
            if (_options.Count == 0) { _walking = false; _workT = 3f; return; }
            int pick = Random.Range(0, _options.Count);
            if (pick == _last && _options.Count > 1) pick = (pick + 1) % _options.Count;
            _last = pick;
            (_target, _face) = _options[pick];
            _target.y = transform.position.y;
            _walking = true;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            if (_walking)
            {
                var to = _target - transform.position;
                to.y = 0f;
                float step = walkSpeed * Management.MoraleMultiplier * dt;
                if (to.magnitude <= step)
                {
                    transform.position = _target;
                    _walking = false;
                    _workT = Random.Range(workSeconds.x, workSeconds.y);
                }
                else
                {
                    transform.position += to.normalized * step;
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), 540f * dt);
                }
            }
            else
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.Euler(0f, _face, 0f), 360f * dt);
                _workT -= dt;
                if (_workT <= 0f) Next();
            }
            if (!anim) return;
            anim.SetSpeed(_walking ? 0.8f : 0f);
            anim.SetWork(_walking ? 0f : 1f);
        }
    }
}
