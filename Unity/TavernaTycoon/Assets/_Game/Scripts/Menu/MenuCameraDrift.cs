using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Ana menü kamerası: hedefin etrafında çok yavaş sağa-sola kayar ve hafifçe nefes alır.
    /// Görüş açısı yatay olarak sabitlenir; böylece her en-boy oranında oda aynı genişlikte görünür.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class MenuCameraDrift : MonoBehaviour
    {
        public Vector3 target = new(0f, 0.5f, 0.7f);
        public float yaw = 18f;
        public float pitch = 38f;
        public float distance = 8.4f;
        /// <summary>Yatay görüş açısı (derece).</summary>
        public float horizontalFov = 46f;
        public float yawSwing = 5f;
        public float distSwing = 0.35f;

        Camera _cam;

        void Awake() => _cam = GetComponent<Camera>();

        void LateUpdate()
        {
            float t = Time.time;
            float y = yaw + Mathf.Sin(t * 0.13f) * yawSwing;
            float d = distance + Mathf.Sin(t * 0.09f + 1f) * distSwing;
            var rot = Quaternion.Euler(pitch, y, 0f);
            transform.SetPositionAndRotation(target - rot * Vector3.forward * d, rot);
            float aspect = Mathf.Max(0.1f, _cam.aspect);
            _cam.fieldOfView = 2f * Mathf.Atan(Mathf.Tan(horizontalFov * 0.5f * Mathf.Deg2Rad) / aspect) * Mathf.Rad2Deg;
        }
    }
}
