# Balance Check: Ekonomi + İlerleme (2026-11-10)

## Data Sources Analyzed
- `Unity/TavernaTycoon/Assets/_Game/Scripts/Core/Balance.cs`: Ün eğrisi, yükseltme, depo, tempo ve sabır formülleri
- `Scripts/Core/Catalog.cs`: makine fiyatları, personel işe alma bedeli ve maaş, dekor
- `Editor/TavernaData.cs`: içecek fiyat ve süreleri, 30 müşteri tipi
- `Scripts/Core/Management.cs`: yıldız etkileri, maaş toplamı
- `Editor/TavernaSceneBuilder.cs`: ödeme kareleri (×1.4, v0.9)
- Hedefler: `GAME_DESIGN.md` §3.1, §7, §8.1, §8.2. `design/registry/entities.yaml` boş şablon; `assets/data/` yok (sayılar kodda).
- Ölçüm: v0.9 bot testleri (Sv1 → 470 oyun-sn'de Sv3; Sv14'te 450–520 oyun-sn, ~23 Ün/dk)

## Health Summary: CONCERNS
İlerlemeyi kilitleyen ya da tek bir seçimin her şeyi ezdiği bir durum yok. Geç oyun eğrisi hedefin çok gerisinde ve geç oyunda müşteri baskısı azalıyor.

## Outliers Detected
| Değer | Hedef | Ölçülen / hesaplanan | Sorun |
|---|---|---|---|
| Sv13'e varış | ~1.5 sa | ~1.8 sa | biraz yavaş |
| Sv21'e varış | ~5 sa | ~10.5 sa | 2× yavaş |
| Sv30'a varış | ~20 sa | ~87 sa | 4× yavaş (×1.32 Ün eğrisi) |
| Sonraki anlamlı şey | ≤10 dk | Sv14→15 ~32 dk, Sv16→18 ~110 dk | ölü bölge |
| Maaş / gelir (Sv14) | maliyet gelirden hızlı artmalı | 420 / 14.600 = %3 | maaş sabit ve anlamsız |
| Müşteri aralığı | seviyeyle zorlaşmalı | seviyeden bağımsız | personel büyüdükçe kolaylaşıyor |
| Sabır | zorlaşmalı | seviyeden bağımsız | aynı |
| Malzeme maliyeti | gerçek bir harcama | taban fiyatın %40'ı, kalite artışından etkilenmiyor | Kalite Sv10'da malzeme payı ~%10 |

## Degenerate Strategies Found
- Önce kaliteyi maksimuma çekmek: satış ×4, malzeme sabit, yükseltme ucuz → kâr marjı sınırsız büyüyor.
- Personeli doldurup bırakmak: sabit ve küçük maaş, tutmanın gerçek bir bedeli yok.

## Progression Analysis
| Bölge | Durum |
|---|---|
| Sv1–12 | sağlıklı: seviye başına 3–20 dk, her seviyede yeni bir şey |
| Sv13–16 | uzuyor (25–50 dk), oyun salonu ve han içerik veriyor |
| Sv17–30 | ölü bölge: seviye başına 1–19 sa; yalnızca 2 makine ve 12 müşteri tipi açılıyor |

## Recommendations (hepsi uygulandı, 2026-11-10)
| Öncelik | Sorun | Uygulanan | Beklenen etki |
|---|---|---|---|
| P1 | Geç oyun yavaş | Ün eğrisi Sv12 sonrası ×1.22 | Sv21 ≈ 5.7 sa, Sv30 ≈ 22 sa |
| P1 | Müşteri baskısı sabit | `SpawnLevelScale`: Sv6 sonrası ×0.97/seviye, en fazla ×1.5 | zorluk + daha çok Ün |
| P1 | Sabır sabit | `PatienceLevelScale`: Sv8 sonrası −%2/seviye, en az %70 | Barlo tek başına yetişemez |
| P2 | Tek siparişli masalar | `ExtraOrderChance`: Sv10+ %20, Sv18+ %35 | kalabalık ve kazanç |
| P2 | Maaş anlamsız | `Balance.Wage`: taban × 1.15^(Sv − açılış) (ilk 1.12, ölçüm sonrası) × (1 + 0.15 × görevli seviyesi) | hedef: maaş gelirin %10–20'si |
| P2 | Kalite marjı sınırsız | malzeme = satış fiyatının %30'u | marj sabit, depo gerçek harcama |
| P3 | Yükseltmeler ucuz | Sv5 sonrası üs 1.24 | para birikmez |
| P3 | Maaş ödenmemesinin cezası yok | ertesi gün personel küskün (−%20 hız), plan ekranında uyarı | "kötü gün" hissi |

## Values That Need Attention
- Ölçüm (bot, 2 oyun günü):
  - Sv1: 15 dk'da Sv4 (önceki sürüm: 8 dk'da Sv3). Gün geliri ~900, maaş yok.
  - Sv14: gün geliri 11.900 → 15.300; servis 141 → 153–163/gün (+%15); kızgın müşteri 0 (bot hızlı).
  - Maaş oranı %9 (1.12^ ile). Üs 1.15'e çıkarıldı.
  - 1. gün maaşın 1.040'ı ödenemedi (bot her şeyi harcıyor) → 2. gün personel küskündü. Gece başında "maaşı kasada tut" uyarısı eklendi.
- Bot kızgın müşteri vermiyor; gerçek oyuncu baskıyı daha çok hisseder. Telefonda oynanınca tekrar bakılmalı.

## Tempo ölçümü (v1.0, tüm bölümler ve kademeler eklendikten sonra)
Bot, her seviyede o seviyedeki oyuncunun açmış olacağı alanlarla 2 oyun günü (14 oyun-dk) oynadı.

| Seviye | İlk ölçüm Ün/dk | Yumuşatma sonrası Ün/dk | Altın/dk |
|---|---|---|---|
| Sv1 | 10.4 | – | 506 |
| Sv6 | 20.6 | – | 1.882 |
| Sv10 | 27.9 | – | 2.643 |
| Sv14 | 28.8 | 34.2 | 4.019 |
| Sv18 | 6.5 | 25.1 | 4.355 |
| Sv22 | 0.6 | 24.4 | 3.306 |
| Sv26 | 0.0 | 19.4 | 4.055 |

**Bulgu (CRITICAL, düzeltildi):** Sv18'den sonra müşteri baskısı (×1.5 kalabalık, %70 sabır, %35 ek sipariş) botu bile boğuyordu; kızgın her müşteri 1 Ün götürdüğü için net Ün ≈ 0, ilerleme duruyordu.
**Düzeltme:** kızgın müşteri Ün götürmez (ceza yıldızdan); kalabalık en fazla ×1.25 (×0.98/seviye); sabır en az %85 (−%1.5/seviye); Sv18+ ek sipariş %25; zengin müşteri 2 Ün.
**Eğri:** ölçülen hızlarla Sv12–16 ×1.15, Sv16 sonrası ×1.08 → bot Sv13 ≈ 1.1 sa, Sv21 ≈ 4.8 sa, Sv30 ≈ 15.7 sa; en uzun seviye ≈ 1.7 sa (Sv29). İnsan oyuncu ~×1.3 yavaş varsayımıyla hedeflere (1.5 / 5 / 20 sa) yakın.
**Bilinen:** Sv22+ ölçümlerinde bot maaşın bir kısmını ödeyemedi (her şeyi harcıyor); Sv26–30 arası yalnızca müşteri tipleri açılıyor (içerik seyrek).

Re-run `/balance-check` after fixes to verify.
