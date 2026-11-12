using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace Taverna
{
    /// <summary>
    /// v1.7: yazı tipi yedek zincirinin sonu. Zincir (TavernaImport.BuildFonts): Nunito → NunitoExt → Rubik (Kiril) → CJK.
    /// Çince ile Japonca aynı Han kod noktalarını farklı çizer; <see cref="host"/>'un (Rubik) yedek listesindeki CJK sırası
    /// etkin dile göre değiştirilir (zh-Hans → SC, ja → JP, ko → KR önce). Varlık <c>Resources/Loc/LocFonts.asset</c>.
    /// </summary>
    public class LocFontSet : ScriptableObject
    {
        /// <summary>CJK yazı tiplerini yedek olarak taşıyan yazı tipi (Rubik).</summary>
        public FontAsset host;
        /// <summary>host'un CJK dışındaki yedekleri (varsa), CJK'dan önce gelir.</summary>
        public List<FontAsset> baseFallbacks = new();
        /// <summary>Noto Sans SC / JP / KR (Bold) dinamik yazı tipleri.</summary>
        public FontAsset sc, jp, kr;
    }

    /// <summary>Etkin dile göre CJK yedek sırasını ayarlar (<see cref="Loc"/> dil değişince çağırır).</summary>
    public static class LocFonts
    {
        /// <summary>Resources yolu.</summary>
        public const string ResourcePath = "Loc/LocFonts";
        static LocFontSet _set;
        static bool _loaded;

        /// <summary>Etkin dilin CJK yazı tipini öne alır. Varlık yoksa (eski derleme) sessizce geçer.</summary>
        public static void Apply(string code)
        {
            if (!_loaded) { _loaded = true; _set = Resources.Load<LocFontSet>(ResourcePath); }
            if (!_set || !_set.host) return;
            var list = new List<FontAsset>(_set.baseFallbacks);
            foreach (var f in Order(code, _set)) if (f) list.Add(f);
            _set.host.fallbackFontAssetTable = list;
        }

        /// <summary>CJK yedek sırası: etkin dilinki önce, sonra SC, JP, KR.</summary>
        public static FontAsset[] Order(string code, LocFontSet s) => code switch
        {
            "ja" => new[] { s.jp, s.sc, s.kr },
            "ko" => new[] { s.kr, s.sc, s.jp },
            _ => new[] { s.sc, s.jp, s.kr },
        };
    }
}
