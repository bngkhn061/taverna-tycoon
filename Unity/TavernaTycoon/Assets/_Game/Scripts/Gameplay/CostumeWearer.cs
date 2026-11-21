using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v2.2 kostüm gösterici: karakterin görsel nesnesine (Visual, ToyAnimator'lı) eklenir. Kendi modeli ve kostüm modelleri aynı
    /// Visual'in altındadır (sahne kurucu ekler, kostümler kapalı); giyilen kostümün modeli açılır, kendi modeli kapanır.
    /// Böylece zıplama / eğilme animasyonu ve tepsi aynen sürer. Oyun sahnesi, son sahne ve ana menü dioraması kullanır.
    /// </summary>
    public class CostumeWearer : MonoBehaviour
    {
        /// <summary>Kimin kostümleri: "barlo" ya da personel kimliği.</summary>
        public string owner = "barlo";
        public GameObject baseModel;
        public string[] ids = new string[0];
        public GameObject[] models = new GameObject[0];

        /// <summary>Test günlüğü: şu an görünen kostüm ("" = kendi kıyafeti).</summary>
        public string Showing { get; private set; } = "";

        void OnEnable()
        {
            Costumes.Changed += Apply;
            Apply();
        }

        void OnDisable() => Costumes.Changed -= Apply;

        /// <summary>Kayıt bu nesneden sonra yüklenmiş olabilir (ana menü OnEnable'da yükler): bütün OnEnable'lardan sonra yeniden uygula.</summary>
        void Start() => Apply();

        /// <summary>Giyilen kostümü gösterir (modeli yoksa kendi kıyafeti).</summary>
        public void Apply()
        {
            string worn = Costumes.Worn(owner);
            bool any = false;
            for (int i = 0; i < ids.Length && i < models.Length; i++)
            {
                bool on = models[i] && ids[i] == worn;
                if (models[i]) models[i].SetActive(on);
                any |= on;
            }
            if (baseModel) baseModel.SetActive(!any);
            Showing = any ? worn : "";
        }
    }
}
