using UnityEngine;

namespace Taverna
{
    /// <summary>Barmen / Ozan: yerinde durur, canlı görünür (etkileri Management'ta).</summary>
    public class StaffIdle : MonoBehaviour
    {
        public ToyAnimator anim;
        public float sway = 0.35f;
        float _t;
        void Update()
        {
            _t += Time.deltaTime;
            if (anim) anim.SetSpeed(Mathf.Abs(Mathf.Sin(_t * 1.3f)) * sway);
        }
    }
}
