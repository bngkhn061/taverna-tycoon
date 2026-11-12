using System.Collections.Generic;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// Görevliyi müşterilerden ayırır: HUD başının üstüne isim + rol etiketi çizer (rol rengiyle),
    /// sahne kurucu ayağının altına aynı renkte bir halka koyar. Görevlinin kök nesnesine eklenir.
    /// </summary>
    public class StaffBadge : MonoBehaviour
    {
        public static readonly List<StaffBadge> All = new();

        public string displayName = "Pip";
        /// <summary>Catalog.StaffList kimliği (seviye/etkiler için). Geçici yardımcıda boş.</summary>
        public string staffId = "";
        public Catalog.Role role = Catalog.Role.Garson;
        /// <summary>Etiketin baş üstündeki yüksekliği (metre).</summary>
        public float headHeight = 1.55f;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        /// <summary>Rol rengi (UI v3 paleti): garson camgöbeği, barmen hardal, temizlikçi çimen, ozan erik.</summary>
        public static Color RoleColor(Catalog.Role r) => r switch
        {
            Catalog.Role.Garson => new Color32(47, 143, 138, 255),
            Catalog.Role.Barmen => new Color32(242, 182, 49, 255),
            Catalog.Role.Temizlikci => new Color32(106, 168, 79, 255),
            Catalog.Role.Fenerci => new Color32(255, 138, 42, 255),
            Catalog.Role.Asci => new Color32(212, 85, 58, 255),
            Catalog.Role.Hizmetci => new Color32(62, 95, 168, 255),
            _ => new Color32(108, 74, 125, 255),
        };

        /// <summary>Etikette görünen rol adı.</summary>
        public static string RoleName(Catalog.Role r) => r switch
        {
            Catalog.Role.Garson => Loc.T("Garson"),
            Catalog.Role.Barmen => Loc.T("Barmen"),
            Catalog.Role.Temizlikci => Loc.T("Temizlikçi"),
            Catalog.Role.Fenerci => Loc.T("Fenerci"),
            Catalog.Role.Asci => Loc.T("Aşçı"),
            Catalog.Role.Hizmetci => Loc.T("Hizmetçi"),
            _ => Loc.T("Ozan"),
        };

        /// <summary>Rol ikonu (Resources/Kit/v3i_*).</summary>
        public static string RoleIcon(Catalog.Role r) => r switch
        {
            Catalog.Role.Garson => "mug",
            Catalog.Role.Barmen => "barrel",
            Catalog.Role.Temizlikci => "broom",
            Catalog.Role.Fenerci => "sun",
            Catalog.Role.Asci => "hammer",
            Catalog.Role.Hizmetci => "broom",
            _ => "music",
        };
    }
}
