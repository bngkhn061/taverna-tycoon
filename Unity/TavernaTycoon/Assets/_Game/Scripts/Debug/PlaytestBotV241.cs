#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Linq;
using UnityEngine;

namespace Taverna
{
    /// <summary>
    /// v2.4.1 "-autotest-stuck": oyuncu testi (2026-11-23) "masa kurarken haritanın dışına çıktım, içeri giremedim".
    ///   1) SIKIŞMA: ön sınıra en yakın açık masa Barlo'nun hemen arkasına (içine) itilir; denetleyici onu dışarı iterse
    ///      güvenlik ağı (PlayerController.Guard) içeri almalı. Sonuç: Barlo ön sınırın (z −5.1) içinde mi?
    ///   2) AÇILIŞ: kilitli bir masa ödeme alanında Barlo dururken satın alınır (büyüyerek belirir);
    ///      PopIn/EnsureFree onu önce boş yere almalı. Sonuç: Barlo masayla çakışıyor mu, içeride mi?
    /// </summary>
    public partial class PlaytestBot
    {
        /// <summary>Ön görünmez sınırın iç yüzü (TavernaSceneBuilder: merkez −5.25, kalınlık 0.3).</summary>
        const float FrontInnerZ = -5.1f;

        string _stuckArg;
        bool Stuck => (_stuckArg ??= HasArg("-autotest-stuck") ? "1" : "") == "1";
        int _stuckStep;
        float _stuckAt;
        Table _stuckTable;
        Vector3 _stuckTablePos;
        PayZone _stuckZone;

        /// <summary>true: bu kare bot oynamaz.</summary>
        bool UpdateStuck()
        {
            if (!Stuck) return false;
            var p = PlayerController.I;
            if (!p) return true;
            float t = _real - _stuckAt;
            switch (_stuckStep)
            {
                case 0:
                    if (_real < 3f) return true;
                    _uiStep = UiSteps.Length; _langStep = 4; _tierStep = 9; _premStep = 4; _fireStep = 2;
                    _stuckTable = Table.All.Where(x => x && x.isActiveAndEnabled && x.GetComponentInChildren<Collider>())
                                           .OrderBy(x => x.transform.position.z).FirstOrDefault();
                    if (!_stuckTable) { Log("v2.4.1 SIKIŞMA: açık masa yok, atlandı"); _stuckStep = 2; _stuckAt = _real; return true; }
                    _stuckTablePos = _stuckTable.transform.position;
                    var b = _stuckTable.GetComponentInChildren<Collider>().bounds;
                    // Barlo masanın önüne, ön sınıra yaslanır; masa sonra tam üstüne kaydırılır → denetleyici sınıra doğru iter
                    var stand = new Vector3(b.center.x, 0f, FrontInnerZ + 0.32f);
                    p.Teleport(stand);
                    Log($"v2.4.1 SIKIŞMA başlıyor: masa {_stuckTable.name} @ {_stuckTablePos:F2} (iz {b.size.x:F2}×{b.size.z:F2}) · Barlo {stand:F2} · kurtarma {PlayerController.Rescues}");
                    _stuckStep = 1; _stuckAt = _real;
                    return true;
                case 1:
                    if (t < 0.4f) return true;
                    if (t < 0.5f)
                    {
                        // masa Barlo'nun 0.15 m arkasına: çakışma çözülürken Barlo −Z'ye (dışarı) itilir
                        var b2 = _stuckTable.GetComponentInChildren<Collider>().bounds;
                        _stuckTable.transform.position += new Vector3(0f, 0f, p.transform.position.z + 0.15f - b2.center.z);
                        Physics.SyncTransforms();
                        Log($"v2.4.1 SIKIŞMA: masa Barlo'nun üstüne kaydırıldı → {_stuckTable.transform.position:F2} · Barlo çakışıyor={p.Blocked(p.transform.position)}");
                        _stuckAt = _real - 0.5f;
                        return true;
                    }
                    if (t < 3f) { PlayerController.JoystickInput = new Vector2(0f, -1f); return true; }   // dışarı doğru da yürür
                    PlayerController.JoystickInput = Vector2.zero;
                    {
                        var pos = p.transform.position;
                        bool inside = pos.z > FrontInnerZ && pos.y > -1f;
                        Log($"v2.4.1 SIKIŞMA SONUÇ: Barlo {pos:F2} · içeride={inside} · çakışıyor={p.Blocked(pos)} · kurtarma {PlayerController.Rescues} {(inside ? "✓" : "✗ DIŞARIDA")}");
                        Shot("v241_sikisma.png");
                        _stuckTable.transform.position = _stuckTablePos;
                        Physics.SyncTransforms();
                        p.Teleport(new Vector3(0.3f, 0f, 0.6f));
                    }
                    _stuckStep = 2; _stuckAt = _real;
                    return true;
                case 2:
                    if (t < 0.5f) return true;
                    _stuckZone = PayZone.All.FirstOrDefault(z => z && z.Available && z.unlockTargets != null
                                    && z.unlockTargets.Any(g => g && g.GetComponentInChildren<Table>(true) && g.GetComponentInChildren<Collider>(true)));
                    if (!_stuckZone) { Log("v2.4.1 AÇILIŞ: alınabilir masa yok, atlandı"); _stuckStep = 4; return true; }
                    GameState.AddCoins(_stuckZone.Remaining + 1000, _stuckZone.transform.position);
                    var tgt = _stuckZone.unlockTargets.First(g => g && g.GetComponentInChildren<Table>(true));
                    var tp = tgt.transform.position;
                    var zp = _stuckZone.transform.position;
                    p.Teleport(new Vector3(zp.x, 0f, zp.z));   // oyuncu gibi ödeme alanının ortasında durur
                    Log($"v2.4.1 AÇILIŞ başlıyor: {_stuckZone.id} · masa {tgt.name} @ {tp:F2} · ödeme alanı @ {_stuckZone.transform.position:F2} · Barlo {p.transform.position:F2}");
                    _stuckStep = 3; _stuckAt = _real;
                    return true;
                case 3:
                    if (GameState.IsUnlocked(_stuckZone.id) || t > 12f)
                    {
                        if (t < 12f && !_v241Unlocked) { _v241Unlocked = true; _stuckAt = _real - 0f; _v241UnlockAt = _real; Log($"v2.4.1 AÇILIŞ: açıldı · Barlo {p.transform.position:F2} · kurtarma {PlayerController.Rescues}"); }
                        if (_real - _v241UnlockAt < 1.5f && t <= 12f) { KeepOnZone(p); return true; }
                        var pos = p.transform.position;
                        bool inside = pos.z > FrontInnerZ && pos.y > -1f;
                        Log($"v2.4.1 AÇILIŞ SONUÇ: açık={GameState.IsUnlocked(_stuckZone.id)} · Barlo {pos:F2} · içeride={inside} · çakışıyor={p.Blocked(pos)} · kurtarma {PlayerController.Rescues} {(inside && !p.Blocked(pos) ? "✓" : "✗")}");
                        Shot("v241_acilis.png");
                        _stuckStep = 4;
                        return true;
                    }
                    KeepOnZone(p);
                    return true;
                case 4:
                    Log($"v2.4.1 BİTTİ · toplam kurtarma {PlayerController.Rescues}");
                    _stuckStep = 5;
                    return true;
                default:
                    return false;   // v2.5: sonra olağan bot oynar ve "-autotest-seconds" dolunca kapanır (eskiden süreç açık kalıyordu)
            }
        }

        bool _v241Unlocked;
        float _v241UnlockAt;

        /// <summary>Ödeme alanı Barlo yarıçap içindeyken akar; Barlo itilmediyse yerinde kalsın.</summary>
        void KeepOnZone(PlayerController p) => PlayerController.JoystickInput = Vector2.zero;
    }
}
#endif
