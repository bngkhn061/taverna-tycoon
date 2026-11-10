using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Kat geçişi (v0.8): Barlo bu noktaya gelince öbür kata ışınlanır, kamera oraya oturur.
    /// Ping-pong olmasın diye varış noktası tetikleyicinin dışındadır ve kısa bir bekleme vardır.
    /// </summary>
    public class Stairs : MonoBehaviour
    {
        public Vector3 arrive;
        public bool goesUp = true;
        public float radius = 0.55f;
        static float _cooldownUntil;

        void Update()
        {
            var p = PlayerController.I;
            if (!p || Time.time < _cooldownUntil) return;
            if (DrinkStation.FlatDistance(p.transform.position, transform.position) > radius) return;
            _cooldownUntil = Time.time + 1f;
            p.Teleport(arrive);
            var cam = Camera.main ? Camera.main.GetComponent<CameraFollow>() : null;
            if (cam) cam.SnapNow();
            Sfx.Play(SfxId.Pop, goesUp ? 1.3f : 0.9f);
            Inn.RaiseStairs(goesUp);
        }
    }
}
