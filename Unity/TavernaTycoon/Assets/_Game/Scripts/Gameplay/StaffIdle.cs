using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Ozan: yerinde durur, canlı görünür (etkileri Management'ta).
    /// v2.0 (oyuncu testi madde 18): "Ozan Sahnesi" (stageZone ödeme karesi) açıksa sahneye yürür, sahnenin üstünde
    /// seyirciye dönüp çalar (ToyAnimator.SetPlay); sahne yoksa bugünkü yerinde (home) durur. Konser ozanı da aynı.
    /// </summary>
    public class StaffIdle : MonoBehaviour
    {
        public ToyAnimator anim;
        public float sway = 0.35f;
        /// <summary>Sahne noktası (zemin hizasında) ve sahnenin yüksekliği; stageZone boşsa sahne yok.</summary>
        public Vector3 stagePoint;
        public float stageHeight = 0.3f, stageFace = 180f;
        public string stageZone = "";
        public float walkSpeed = 1.8f;

        float _t;
        Vector3 _home;
        Quaternion _homeRot;
        bool _homeSet;

        /// <summary>Ozan şu an sahnede çalıyor mu (test günlüğü)?</summary>
        public bool OnStage { get; private set; }

        void OnEnable()
        {
            if (_homeSet) return;
            _homeSet = true;
            _home = transform.position;
            _homeRot = transform.rotation;
        }

        bool StageOpen => !string.IsNullOrEmpty(stageZone) && GameState.IsUnlocked(stageZone);

        void Update()
        {
            _t += Time.deltaTime;
            bool stage = StageOpen;
            var goal = stage ? stagePoint : _home;
            var flat = new Vector3(transform.position.x, 0f, transform.position.z);
            var to = new Vector3(goal.x, 0f, goal.z) - flat;
            if (to.magnitude > ArriveDistance)
            {
                // Yürürken yere in (sahneden inerken) ve hedefe dön
                OnStage = false;
                float y = Mathf.MoveTowards(transform.position.y, 0f, Time.deltaTime * StepSpeed);
                StaffMove.ToRouted(transform, new Vector3(goal.x, transform.position.y, goal.z), walkSpeed, ArriveDistance, anim);
                transform.position = new Vector3(transform.position.x, y, transform.position.z);
                if (anim) { anim.SetPlay(0f); anim.SetSpeed(1f); }
                return;
            }
            float targetY = stage ? stageHeight : 0f;
            transform.position = new Vector3(goal.x, Mathf.MoveTowards(transform.position.y, targetY, Time.deltaTime * StepSpeed), goal.z);
            var face = stage ? Quaternion.Euler(0f, stageFace, 0f) : _homeRot;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, face, 360f * Time.deltaTime);
            OnStage = stage && Mathf.Abs(transform.position.y - stageHeight) < 0.01f;
            if (!anim) return;
            if (stage) { anim.SetSpeed(0f); anim.SetPlay(1f); }
            else { anim.SetPlay(0f); anim.SetSpeed(Mathf.Abs(Mathf.Sin(_t * 1.3f)) * sway); }
        }

        const float ArriveDistance = 0.12f, StepSpeed = 1.5f;
    }
}
