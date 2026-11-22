using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v2.3 düzenleme modu: masaya ait ama masanın altında olmayan parçalar (kademe örtüsü, masa üstü dolgu ışığı) masayla birlikte
    /// taşınır ve döner. Sahne kurucu masanın yerel uzayındaki konumu ve dönüşü yazar.
    /// </summary>
    public class TableFollower : MonoBehaviour
    {
        /// <summary>İzlenen masa.</summary>
        public Transform table;
        /// <summary>Masanın yerel uzayındaki konum.</summary>
        public Vector3 localPos;
        /// <summary>Masaya göre dönüş (derece, Y ekseni).</summary>
        public float localYaw;

        void OnEnable() => Sync();
        void LateUpdate() => Sync();

        /// <summary>Konumu ve dönüşü masaya göre yeniler.</summary>
        public void Sync()
        {
            if (!table) return;
            transform.SetPositionAndRotation(table.TransformPoint(localPos), table.rotation * Quaternion.Euler(0f, localYaw, 0f));
        }
    }
}
