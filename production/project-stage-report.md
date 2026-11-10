# Project Stage Analysis

**Date**: 2026-11-10
**Stage**: Production
**Stage Confidence**: PASS — clearly detected
**Analysis Scope**: Full project (oyun kalitesi, hatalar, kesilecek ve eklenecek özellikler dahil)

## Completeness Overview

Yüzdeler tahmindir; ölçüm değildir.

- **Design: ~85%** — `design/game-brief.md` + kökte `GAME_DESIGN.md` (§1–11, v1.3'e kadar güncel), 6 onaylı UI mockup sayfası, 1 denge raporu. Eksik: Sv26–30 ve prestij tasarlanmış (§9) ama uygulanmamış.
- **Code: ~80%** — 80 çalışma zamanı dosyası (9.656 satır) + 6 editör dosyası (2.346 satır). Core 27, Gameplay 36, UI 9, Debug 2. Çekirdek döngü, 10 bölüm, han, oyun salonu, kademeler, Hazine, ses, rehber, mutluluk, etkinlikler çalışıyor.
- **Architecture: değerlendirilmedi** — `workflow: minimal` kademesinde ADR ve mimari belge istenmez; yokluğu eksik sayılmaz.
- **Production: ~70%** — sprint planı yok (bu kademede istenmez). 2 commit, GitHub'da gizli depo. v1.3 işi (98 dosya) commit edilmedi.
- **Tests: ~10%** — otomatik birim testi yok; `com.unity.test-framework` paketi kurulu değil, `project.yaml`'daki test komutu bu yüzden çalışmaz. Doğrulama tamamen `PlaytestBot` (Windows autotest) ve ekran kanıtlarıyla yapılıyor. 1 insan oyuncu testi raporu (`production/qa/playtests/`).

## Stage Classification Rationale

**Configured stage**: not set (`project.yaml`'da `stage` yok, `production/stage.txt` yok)
**Observed stage**: Production

Motor kurulu ve sabitlenmiş (Unity 6000.6.8f1), kod kökünde 10'dan çok kaynak dosyası var, geliştirme aktif. Polish ve Release yalnızca `/gate-check` ile açıkça atanır; `workflow: minimal` kademesinde yapılandırılmış aşama ilerletilmez, bu yüzden "not set" beklenen durumdur ve çelişki değildir.

`bash .claude/scripts/artifact-check.sh`: 49 adımdan 25'i `NO_CHECK` (varlık kontrolüyle ölçülemez). `ABSENT` çıkan belge adımları (GDD, art bible, ADR, epic, sprint) bu kademede eksik sayılmaz. Tek anlamlı sayım: oyuncu testi `SHORT count=1 min=3`.

## Gaps Identified

### A. Doğrulanmış hatalar ve denge açıkları (kodda ya da test kaydında görüldü)

1. **Rehber oyunu dondurabiliyor.** `Tutorial.HoldsTime => Active`; rehber sürerken `DayCycle` ve `RushHour` durur. Oyuncu son adımdaki Depo butonuna basmazsa gün hiç bitmez (maaş, gece, yoğun saat yok).
2. **Mutluluk barı doyuyor.** Testlerde %93–100. Hızlı servis +4, kızgın −8, boşta dönüş 0,04/sn: müşterilerin üçte biri kızgın gitmedikçe düşmez.
3. **Yıldızlar erken doluyor.** `Stars = 1 + ambiyans/30`: Sv9'da 5★; Hazine altın temasıyla Sv1'de 4,5★. Sonraki dekor alımları etkisiz.
4. **Kov / yeniden işe al açığı.** Maaş `×1.15^(Sv − işe alım Sv)` büyür, işe alım ücreti sabit. Sv20: Nana maaş 148 / işe alım 80; Pip 194 / 150.
5. **Tellal ile Ozan Konseri çakışıyor.** İkisi de müşteri ×1.6; Tellal ucuz (60 + 40·Sv), sınırsız ve konserle çarpılıyor.
6. **Periyodik takılma (muhtemel neden).** Her testte ~10 sn'de bir 125–175 ms. `GameBootstrap` 10 sn'de bir `PlayerPrefs.Save` yapıyor, zamanlar örtüşüyor; kesin ölçülmedi. Sahibi telefonda sorun görmediğini bildirdi (2026-11-10).
7. **Çarpanlar sınırsız çarpılıyor.** Mutluluk 1.5 × masa 1.83 × tellal 1.6 × konser 1.6 × yoğun saat 2.2 × Usta 1.3 × seviye 1.25 ≈ ×25. Masalar hep dolu olduğundan hiçbiri hissedilmiyor.

Ölçülmedi: bot 21 özel siparişin 9'unu yetiştirdi; %22 oranının insan için fazla olup olmadığı bilinmiyor.

### B. Yayın eksikleri

1. Reklam ve satın alma paketi yok; reklam 5 sn'lik bekleme ekranı, Hazine yalnızca `Debug.isDebugBuild` iken çalışıyor.
2. Uygulama kimliği `com.tavernatycoon.prototype`, şirket `DefaultCompany`, sürüm 1.0 (kod 1). Kimlik mağazaya çıktıktan sonra değişmez.
3. Kayıt `PlayerPrefs` içinde düz JSON; altın, elmas ve `premium` listesi elle değiştirilebilir.
4. Oyun sonu yok: son içerik Sv26, `Balance.MaxLevel` 30, prestij (§9) yazılmamış.
5. Ayarlardaki Dil (TR/EN) ve Bildirim düğmeleri yalnızca tercihi kaydediyor; hiçbir sistem okumuyor. Tüm arayüz metinleri kodda Türkçe gömülü.
6. Ölçüm (analytics) yok.
7. `.gitignore`'da `__pycache__` yok; 7 önbellek / yedek dosyası depoda izleniyor.

### C. Sahibinin kararları (2026-11-10)

- Tellal: Etkinlikler'e taşınacak ya da kaldırılacak.
- Dil ve Bildirim düğmeleri çalışır hale getirilecek; Çince dahil çok kullanılan diller eklenecek. Oyun birkaç dilde çıkacak.
- Müşteri çarpanlarının toplamına üst sınır konacak.
- Sv26–30 içeriği ve prestij yapılacak.
- Özel siparişe bağlı görevler eklenecek.
- Rehberi bitirenleri gösteren ölçüm eklenecek.
- Telefonda performans sahibi tarafından deneniyor; sorun yok. Performans bu rapordaki öncelik listesinden çıkarıldı.
- Sıra: önce yayına hazırlık, en son denge.

## Recommended Next Steps

1. **Paket A — hatalar:** A1–A5, A7, Tellal'ın Etkinlikler'e taşınması, özel sipariş görevleri.
2. **Paket B — diller:** yerelleştirme altyapısı, Türkçe gömülü metinlerin ayrılması, çeviriler, CJK yazı tipleri, bildirimler.
3. **Paket C — oyun sonu:** Sv26–30 içeriği ve prestij ("Yeni Şube").
4. **Paket D — yayın:** uygulama kimliği, reklam / satın alma / ölçüm, kayıt güvenliği, `.gitignore`.
5. **Son denge geçişi** (`/balance-check`) ve yayın öncesi karar (`/gate-check`).
6. İki insan oyuncu testi daha (özellikle Sv17–26 bölümleri: mahzen, teras, avlu, iskele, Saray Hanı neredeyse hiç oynanarak denenmedi).

Model planı (`.claude/docs/model-tiers.md`): uygulama işleri Opus'ta, tarama ve karar Fable'da.

## Follow-Up Skills to Run

- `/balance-check` — paketler bitince, son denge geçişi.
- `/gate-check` — yayın öncesi karar.
- `/content-audit` — Sv26–30 eklendikten sonra planlanan / yapılan karşılaştırması.

## Appendix: File Counts by Directory

```
design/
  game-brief.md            1 (minimal kademede tasarım kaydı)
  gdd/                     0 (bu kademede istenmez)
  ui/                      6 mockup sayfası
  balance/                 1 rapor
GAME_DESIGN.md             kökte, §1–11

Unity/TavernaTycoon/Assets/_Game/
  Scripts/Core/            27 dosya
  Scripts/Gameplay/        36 dosya
  Scripts/UI/               9 dosya
  Scripts/Debug/            2 dosya
  Editor/                   6 dosya

docs/architecture/         0 ADR (bu kademede istenmez)
production/
  qa/playtests/            1 rapor
  qa/evidence/             ekran kanıtları (v0.7–v1.3)
  sprints/                 0 (bu kademede istenmez)

Assets/Tests/              0 test dosyası
prototypes/                yok
```

*Generated by `/project-stage-detect`*
