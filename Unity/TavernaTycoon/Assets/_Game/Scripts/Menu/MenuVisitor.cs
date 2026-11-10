using System.Collections;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Ana menü dioraması: kapıdan giren, boş yere oturup sevinen, sonra çıkıp
    /// başka bir müşteri olarak geri gelen ziyaretçi. Her turda modeli rastgele değişir.
    /// </summary>
    public class MenuVisitor : MonoBehaviour
    {
        /// <summary>Kapıdan oturma yerine giden yol (ilk nokta kapı, son nokta oturma yeri).</summary>
        public Vector3[] path;
        public Vector3 lookAtWhenSeated;
        public GameObject[] models;
        public float speed = 1.3f;
        public Vector2 stay = new(9f, 14f);
        public Vector2 away = new(2f, 4f);

        ToyAnimator _anim;
        GameObject _model;

        IEnumerator Start()
        {
            if (path == null || path.Length < 2 || models == null || models.Length == 0) yield break;
            while (true)
            {
                SwapModel();
                transform.position = path[0];
                yield return Scale(0f, 1f);
                for (int i = 1; i < path.Length; i++) yield return MoveTo(path[i]);
                yield return Face(lookAtWhenSeated);

                float until = Time.time + Random.Range(stay.x, stay.y);
                float nextCheer = Time.time + 1.5f;
                while (Time.time < until)
                {
                    _anim.SetSpeed(0f);
                    if (Time.time >= nextCheer) { _anim.Hop(); _anim.Pop(); nextCheer = Time.time + Random.Range(3f, 6f); }
                    yield return null;
                }

                for (int i = path.Length - 2; i >= 0; i--) yield return MoveTo(path[i]);
                yield return Scale(1f, 0f);
                yield return new WaitForSeconds(Random.Range(away.x, away.y));
            }
        }

        void SwapModel()
        {
            if (_model) Destroy(_model);
            var prefab = models[Random.Range(0, models.Length)];
            _model = Instantiate(prefab, transform);
            _model.name = "Visual";
            _model.transform.localPosition = Vector3.zero;
            _model.transform.localRotation = Quaternion.identity;
            _anim = _model.AddComponent<ToyAnimator>();
        }

        IEnumerator Scale(float from, float to)
        {
            for (float t = 0f; t < 0.3f; t += Time.deltaTime)
            {
                transform.localScale = Vector3.one * Mathf.Lerp(from, to, t / 0.3f);
                yield return null;
            }
            transform.localScale = Vector3.one * to;
        }

        IEnumerator MoveTo(Vector3 target)
        {
            target.y = transform.position.y;
            while ((target - transform.position).sqrMagnitude > 0.0004f)
            {
                var to = target - transform.position;
                float step = speed * Time.deltaTime;
                transform.position = to.magnitude <= step ? target : transform.position + to.normalized * step;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to.normalized, Vector3.up), Time.deltaTime * 10f);
                _anim.SetSpeed(1f);
                yield return null;
            }
        }

        IEnumerator Face(Vector3 point)
        {
            var dir = point - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) yield break;
            var goal = Quaternion.LookRotation(dir.normalized, Vector3.up);
            for (float t = 0f; t < 0.25f; t += Time.deltaTime)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, goal, t / 0.25f);
                _anim.SetSpeed(0f);
                yield return null;
            }
            transform.rotation = goal;
        }
    }
}
