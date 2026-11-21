# Reklam, satın alma ve ölçüm altyapısı (v1.6)

Kod hazır, gerçek hizmet hesabı henüz yok. Oyun bu üç hizmeti yalnızca birer arayüz üzerinden kullanıyor. Açılışta test uygulamaları kurulur; gerçek sağlayıcı sonradan tek satırla takılır.

| Arayüz | Dosya | Varsayılan (test) uygulama | Ne işe yarar |
|---|---|---|---|
| `IAdService` | `Assets/_Game/Scripts/Core/Services/IAdService.cs` | `TestAdService`: 5 / 15 sn sayan bekleme ekranı | Ödüllü reklam: hazır mı, göster, ödül ya da iptal geri çağrısı |
| `IStoreService` | `.../Services/IStoreService.cs` | `TestStoreService`: "TEST · gerçek ödeme alınmaz" onay ekranı | Ürün listesi, fiyat metni, satın al, geri yükle |
| `IAnalyticsService` | `.../Services/IAnalyticsService.cs` | `LocalAnalyticsService`: `persistentDataPath/analytics.jsonl` | Olay adı ve parametre sözlüğü |

Erişim noktası: `Services` (`.../Services/Services.cs`). `Services.Ads`, `Services.Store`, `Services.Analytics`, `Services.Track(...)`.

## Gerçek sağlayıcıyı takmak

1. SDK'yı (ör. Google Mobile Ads / LevelPlay, Unity IAP, Firebase Analytics) projeye ekle.
2. İlgili arayüzü uygulayan bir sınıf yaz. `IsTest` değeri `false` dönmeli.
3. Sahne yüklenmeden önce kaydet. Test uygulamaları `BeforeSceneLoad` aşamasında kurulur, gerçek olan onların yerine geçer:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
static void RegisterRealServices()
{
    Services.Register(new AdMobAdService());        // IAdService
    Services.Register(new UnityIapStoreService());  // IStoreService
    Services.Register(new FirebaseAnalyticsService()); // IAnalyticsService
}
```

`Services.Register` hangi sırada çağrılırsa çağrılsın son kaydedilen geçerli olur. Oyun kodunda başka bir yer değişmez.

- **Reklam:** `ShowRewarded(placement, reason, length, onRewarded, onCancelled)`. Ödül yalnızca reklam sonuna kadar izlenince verilir. Reklam yerleri (`placement`): `helper` (yardımcı garson), `staff_upgrade` (görevli seviyesi), `rush_delivery` (siparişi hızlandır), `rename` (taverna adı).
- **Mağaza:** ürün kimlikleri `Premium.List` ile aynı olmalı (`vip_month`, `vip_life`, `starter`, `staff_lirael`, `staff_ignis`, `thm_altin`, `dec_kristal`, `dec_ejder`, `frame_kraliyet`, `gems_100`, `gems_550`, `gems_1200`, `gems_3000`). Ödeme onaylanınca `onPurchased(id)` çağrılır. Ödülü `Premium.Grant` verir, mağaza vermez. `PriceText(id)` mağazanın yerel fiyat metnini döner. Satın alma makbuzunun sunucuda doğrulanması bu pakette yok; gerçek mağaza bağlanırken eklenmeli.
- **Ölçüm:** `Track(eventName, parameters)` ve `Flush(blocking)`. Kapanışta ve arka plana geçişte `Flush(true)` çağrılır.

## Yayın derlemesi güvenliği

`TestAdService` ve `TestStoreService` yalnızca geliştirme derlemesinde (`Debug.isDebugBuild`) çalışır. Geliştirme derlemesi olmayan bir sürümde gerçek sağlayıcı kayıtlı değilse:

- `Services.RewardedAdsEnabled = false`. Yardımcı garson düğmesi gizlenir, görevli "REKLAM" düğmesi çıkmaz, depoda "HEMEN" yerine "YOLDA" görünür, ad değiştirmede reklam düğmesi gizlenir. `HudController.ShowAd` hiçbir şey yapmaz.
- `Services.StoreEnabled = false`. Hazine'deki bütün ürünler "YAKINDA" gösterir. `Premium.Request` yalnızca "Mağaza yakında açılıyor" penceresini açar. `Premium.Grant` ürün vermeyi reddeder. Başlangıç paketi teklifi açılmaz.

Bu durum testte `-autotest-release-sim` bayrağıyla taklit edilir (kanıt: `production/qa/evidence/2026-11-11-v16-*`).

Koddaki bütün çağrı yerleri (v1.6 taraması):

- `ShowAd`: `HudController` yardımcı garson, depo "HEMEN" ve görevli seviyesi; `HudV09` ad değiştirme. Bunlardan başka yalnızca `PlaytestBotV16` test botu çağırır.
- `Premium.Request`: `HudPremium` (ürün kartı, elmas kartı, VIP aylık ve ömür boyu, başlangıç paketi) ve `PlaytestBot` (test).

## Gönderilen olaylar

Parametre adları Türkçe ve ASCII.

| Olay | Parametreler | Nerede |
|---|---|---|
| `session_start` | platform, surum, gelistirme | açılış ve arka planda 30 sn'den uzun kaldıktan sonra dönüş |
| `session_end` | sure_sn | arka plana geçiş, kapanış |
| `tutorial_start` | — | yeni oyunda rehber başlayınca (ek olay, huni için) |
| `tutorial_step` | adim (biten adım, 1–9), sure_sn | rehber adımı geçince (v1.9: `kendiliginden` kalktı, adımlar kendiliğinden geçmez) |
| `tutorial_complete` | toplam_sure_sn | rehber bitince (v1.9: `kendiliginden_gecen` kalktı) |
| `tutorial_skip` | adim, sure_sn | v1.9'da kalktı (rehber zorunlu, "Atla" yok); eski cihaz verisinde kalabilir. Huni artık başlatan → bitiren; adım adım `tutorial_step` hangi adımda bırakıldığını gösterir |
| `level_up` | seviye, gun, servis | seviye atlayınca |
| `day_end` | gun, gelir, maas, odenmeyen, servis, mutluluk, seviye | gün sonu |
| `zone_unlock` | alan, bedel, seviye | ödeme karesi açılınca |
| `machine_buy` | makine, fiyat, seviye | makine alınınca |
| `automation_buy` | otomasyon, para (altin / elmas), seviye | v1.9: otomasyon alınınca |
| `rename` | para (elmas), bedel | v1.9: ad elmasla değiştirilince |
| `staff_hire` / `staff_fire` | gorevli, ucret / hazine, izinde | işe alma, kovma ya da izne gönderme |
| `event_start` | etkinlik, fiyat, seviye | etkinlik başlayınca |
| `special_served` | seviye | Barlo'ya özel sipariş servis edilince |
| `prestige` | sube, gelir_carpani | yeni şube |
| `barlo_skill` | yetenek (hiz / tasima / servis / para), seviye, taverna_seviyesi | v2.1: Barlo'nun yeteneği alınınca |
| `game_complete` | sure_sn (oyun sahnesindeki toplam süre), gun (prestijte sıfırlanmayan toplam gün), sube, servis | v2.1: son sahne ("Krala Davet") ilk kez başlayınca; bir kez |
| `ad_shown` / `ad_rewarded` / `ad_cancelled` | neden (= placement), uzun / sebep | ödüllü reklam |
| `store_request` / `store_purchase` / `store_fail` | urun, acik / neden | Hazine |

## Yerel ölçüm (şimdiki durum)

`LocalAnalyticsService` her olayı `persistentDataPath/analytics.jsonl` dosyasına bir JSON satırı olarak yazar. Dosya 1 MB'ı geçince baştan başlar. Satırlar bellekte biriktirilir, diske 5 sn'de bir arka planda yazılır. Ayrıca `analytics_counters.json` dosyasında olay sayıları ve rehber hunisini tutar: rehberi başlatan, bitiren ve atlayan oturum sayısı, adım başına ortalama süre, en çok atlanan adım.

Ayarlar ekranında yalnızca geliştirme derlemesinde görünen bir "Ölçüm özeti" satırı vardır.

**Önemli:** Bu sayılar yalnızca o cihazdaki oturumlardır. Veri cihazdan çıkmaz. "Oyuncuların yüzde kaçı rehberi bitirdi, en çok hangi adımda bırakıyorlar" gibi gerçek sayılar ancak gerçek ölçüm hizmeti bağlanıp oyun yayımlandıktan sonra toplanır.

Windows'ta dosyaların yeri: `%USERPROFILE%\AppData\LocalLow\Bn Studio\Taverna Tycoon\`.

## Kayıt dosyası (bilgi için)

Kayıt artık `persistentDataPath/save.dat` dosyasında. İçerik JSON ve HMAC-SHA256 imzasından oluşur. Anahtar, uygulamaya gömülü sabit ile cihaz kimliğinden türetilir. Bir önceki sürüm `save.bak` olarak kalır (`Scripts/Core/Save/`). İmza tutmazsa yedek açılır. İkisi de tutmazsa dosyalar `save.corrupt-*` adıyla kenara alınır ve oyuncuya bilgi verilir.

Bu, sıradan dosya düzenlemesine karşı bir korumadır; kararlı bir saldırganı durdurmaz, çünkü anahtarın yarısı uygulamanın içindedir. Satın almalar ileride sunucu tarafında doğrulanmalı.

Kayıt cihaza bağlıdır. Android yedekten başka bir telefona geri yüklenen ya da farklı imza anahtarıyla derlenen bir sürümde açılan kayıt "bozuk" sayılır. Bulut kaydı ya da hesap bağlama gelince bu yeniden ele alınmalı.
