using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Mutfak yerleşimi (v0.7): Meyhane'nin arka duvarının (z = 5.1) arkasında, kapı sol uçta (x ≈ −3.55).
    /// Düz çizgide yürüyen görevliler (ve test botu) duvarın içinden geçmesin diye kapıdan dolaşan rota verir.
    /// Sayılar TavernaSceneBuilder'daki mutfak kurulumuyla aynıdır.
    /// </summary>
    public static class KitchenLayout
    {
        public const float WallZ = 5.1f;
        public const float DoorX = -3.55f;
        /// <summary>Oyun salonu kapısı (v0.9, Salon arka duvarı).</summary>
        public const float GameDoorX = 9.5f;
        /// <summary>Şarap mahzeni kapısı (v1.0, bahçenin arka duvarı).</summary>
        public const float CellarDoorX = -10.5f;
        /// <summary>Mutfak ile oyun salonu arasındaki bölme duvarı.</summary>
        public const float SplitX = 4.1f;
        /// <summary>Mahzen ile mutfak arasındaki bölme duvarı.</summary>
        public const float CellarSplitX = -4.1f;

        /// <summary>v1.0 Avlu kapısı (salon sağ duvarı x = 15.1) ve İskele kapısı (bahçe sol çiti x = −15.1).</summary>
        public const float SideWallX = 15.1f, YardDoorZ = -1.5f, DockDoorZ = 1.5f;

        /// <summary>−1 iskele, 0 ana bina ve bahçe, 1 avlu (zemin kat).</summary>
        public static int SideOf(Vector3 p) => Inn.Upstairs(p) || p.z > WallZ + 0.05f ? 0 : p.x < -SideWallX ? -1 : p.x > SideWallX ? 1 : 0;

        public static bool InKitchen(Vector3 p) => InBack(p) && p.x < SplitX;
        /// <summary>Arka odalardan birinde mi (mutfak ya da oyun salonu; üst kat sayılmaz)?</summary>
        public static bool InBack(Vector3 p) => p.z > WallZ + 0.05f && p.z < Inn.UpZ;
        static int RoomOf(Vector3 back) => back.x < CellarSplitX ? 0 : back.x < SplitX ? 1 : 2;
        static float DoorFor(Vector3 back) => RoomOf(back) switch { 0 => CellarDoorX, 1 => DoorX, _ => GameDoorX };

        /// <summary>
        /// Hedef duvarın öbür yanındaysa: kapıya uzaksa kapının bu yanındaki ağız noktası,
        /// kapının hizasına gelince öbür yandaki ağız noktası. Aynı yandaysa hedefin kendisi.
        /// </summary>
        public static Vector3 Route(Vector3 from, Vector3 to)
        {
            // Yan bölümler: önce kapısından çık / kapısından gir
            int sa = SideOf(from), sb = SideOf(to);
            if (sa != sb)
            {
                int side = sa != 0 ? sa : sb;
                float dz = side > 0 ? YardDoorZ : DockDoorZ, wx = side * SideWallX;
                bool inside = sa != 0;   // yan bölümdeysek çıkıyoruz
                var sNear = new Vector3(wx + side * (inside ? 0.6f : -0.6f), to.y, dz);
                var sFar = new Vector3(wx + side * (inside ? -0.7f : 0.7f), to.y, dz);
                bool atSideDoor = Mathf.Abs(from.z - dz) < 0.35f && Mathf.Abs(from.x - wx) < 0.9f;
                return atSideDoor ? sFar : sNear;
            }
            bool a = InBack(from), b = InBack(to);
            // İki ayrı arka oda arasında: önce bulunduğun odanın kapısından çık
            if (a && b && RoomOf(from) != RoomOf(to)) { b = false; to = new Vector3(DoorFor(from), to.y, WallZ - 0.8f); }
            if (a == b) return to;
            float door = DoorFor(a ? from : to);
            var near = new Vector3(door, to.y, a ? WallZ + 0.55f : WallZ - 0.55f);
            var far = new Vector3(door, to.y, a ? WallZ - 0.6f : WallZ + 0.6f);
            bool atDoor = Mathf.Abs(from.x - door) < 0.3f && Mathf.Abs(from.z - WallZ) < 0.85f;
            return atDoor ? far : near;
        }
    }
}
