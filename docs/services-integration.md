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
| `season_start` | sezon (anahtar, ör. winter-2026), seviye, sube | v2.2: kayıt yeni bir sezona girince (açılışta ya da oyun sürerken), sezon başına bir kez |
| `season_token` | sezon, miktar, kaynak (servis / gorev / test), toplam | v2.2: sezon jetonu kazanılınca |
| `season_purchase` | urun, tur (dekor / kostum), jeton, sezon | v2.2: sezon jetonuyla alım |
| `costume_purchase` | kostum, kaynak (hazine / jeton), jeton | v2.2: kostüm edinilince (Hazine ödemesi onaylanınca ya da jetonla) |
| `costume_equip` | kostum ("" = çıkar), sahip, kaynak (hazine / jeton / odul / cikar) | v2.2: kostüm giyilince ya da çıkarılınca (alınca kendiliğinden giyilir) |
| `edit_mode_open` | tasinabilir (taşınabilir nesne sayısı), tasinmis (kayıttaki yerleşim satırı), seviye | v2.3: DÜZENLE ile düzenleme modu açılınca |
| `edit_mode_save` | tasinmis (varsayılan yerinde olmayan nesne), sifirlama (VARSAYILANA DÖN basıldı mı), seviye | v2.3: BİTTİ ile yerleşim kaydedilince (İPTAL'de gönderilmez) |
| `cloud_backup` | otomatik (true / false), sonuc (tamam / hata / bulut_ileride) | v2.3: bulut yedeği denendi (Şimdi yedekle, arka plana geçiş, 10 dk'da bir) |
| `cloud_restore` | otomatik, sonuc (tamam / guncel / bulut_bos / imza_red / okunamadi / hata) | v2.3: Buluttan yükle |
| `cloud_conflict` | yer (yedekle / yukle), kural (UseCloud / UseLocal / Ask), cihaz_sv, bulut_sv · seçimde: secim (cihaz / bulut / vazgec) | v2.3: çakışma ekranı açıldı ve oyuncunun seçimi |
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

Kayıt cihaza bağlıdır. Android yedekten başka bir telefona geri yüklenen ya da farklı imza anahtarıyla derlenen bir sürümde açılan kayıt "bozuk" sayılır. Cihazlar arası taşıma bulut kaydıyla yapılır (aşağıda): bulut kaydı cihazdan bağımsız anahtarla imzalanır, yüklenince bu cihazın anahtarıyla yeniden yazılır.

Kayıt sürümü 4 (v2.3): yerleşim satırları (`layout`, `layoutVersion`) ve toplam kazanç (`totalEarned`) eklendi. Eski kayıtta bu alanlar yoktur; varsayılan yerleşimle açılır (`Layout.Migrate`).

## Bulut kaydı (v2.3)

Hesap henüz yok. Reklam ve mağazadaki gibi önce altyapı kuruldu; sağlayıcı sonra bağlanacak (GAME_DESIGN.md §17.6).

**Kod tarafı:**

| Parça | Dosya | Görev |
|---|---|---|
| `ICloudSaveService` | `Scripts/Core/Services/ICloudSaveService.cs` | Sağlayıcı köprüsü: `IsTest`, `IsAvailable`, `SignedIn`, `ProviderName`, `LastSyncUtc`, `SignIn(done)`, `Upload(bytes, meta, done)`, `Download(done)`. Geri çağrılar ana iş parçacığında, en fazla bir kez. |
| `NoneCloudSaveService` | `.../NoneCloudSaveService.cs` | Yayında gerçek sağlayıcı yokken: `IsAvailable = false`. Ayarlar "Yakında" der, düğmeler kapalı, otomatik eşitleme çalışmaz. |
| `LocalCloudSaveService` | `.../LocalCloudSaveService.cs` | Geliştirme ve test: `persistentDataPath/cloud_fake/` klasörüne yazar (`cloud.dat`, `cloud.meta.json`, `last_sync.txt`). Yalnızca geliştirme derlemesinde ve `-autotest-release-sim` kapalıyken çalışır. |
| `CloudSaveCodec` | `Scripts/Core/CloudSave.cs` | Bulut baytları: `TAVERNA-CLOUD-1` başlığı, HMAC-SHA256 imzası, üst bilgi JSON'u, kayıt JSON'u. İmza cihazdan bağımsız anahtarla atılır (başka cihaz da doğrulayabilsin). İmzası tutmayan, kesilmiş ya da imzasız içerik reddedilir. |
| `CloudSave` | `Scripts/Core/CloudSave.cs` | Yedekle, buluttan yükle, çakışma kuralı (`Resolve`), otomatik eşitleme (`Tick`, `OnPause`), ölçüm olayları. Sağlayıcıya dokunan tek yer. |
| `CloudSavePanel` | `Scripts/UI/CloudSavePanel.cs` | Ayarlar › Bulut kaydı satırı ve penceresi, çakışma ekranı. |

Hizmet açılışta kurulur (`Services.InstallDefaults`): geliştirmede sahte bulut, yayında boş uygulama. `Services.CloudEnabled` gerçek sağlayıcı ya da izinli sahte bulut varsa true olur.

**Üst bilgi (`CloudSaveMeta`):** kayıt zamanı (UTC), oyuncu seviyesi, prestij sayısı, toplam kazanç (`SaveData.totalEarned`, prestijte sıfırlanmaz), cihaz kimliği, kayıt sürümü. Cihaz kimliği ilk açılışta üretilen rastgele bir GUID'dir (`PlayerPrefs` › `taverna_cloud_device_id`); donanım kimliği ya da kişisel veri değildir. Sağlayıcı üst bilgiyi kendi alanlarına (açıklama, oynama süresi) yazabilir; asıl karar imzalı içeriktekine göre verilir.

**Çakışma kuralı** (`CloudSave.Resolve`, birim testli): önce prestij sayısı; aynıysa seviye farkı en az 3 ise yüksek olan; seviyeler yakınsa toplam kazanç en az 1,5 kat (ve seviye ters yönde değil) olan; aynı kurulumun eski yedeğinden ileride olan bu cihaz. Bunların hiçbiri tutmazsa oyuncuya iki kayıt yan yana gösterilir ("Bu cihaz: Sv12, 3 prestij — Bulut: Sv14, 3 prestij"), oyuncu seçer: **BU CİHAZI TUT** (bulutun üstüne yazar) ya da **BULUTU YÜKLE**.

**Otomatik eşitleme:** yalnızca sağlayıcı varsa ve oyuncu girişliyse; uygulama arka plana geçerken (`ServicesRunner.OnApplicationPause`) ve 10 dakikada bir (`CloudSave.AutoSyncSeconds`). Otomatik yedek önce bulutu indirir; bulutta açıkça ileri ya da belirsiz bir kayıt varsa yüklemez, Ayarlar'da not çıkar. Kayıt diskten yüklenmeden (boş varsayılan kayıtla) hiçbir şey gönderilmez (`GameState.IsLoaded`).

**Geri yükleme:** indirilen kayıt doğrulanır, `GameState.FromJson` ile güncel sürüme yükseltilir, bellekteki kaydın yerine geçer ve bu cihazın anahtarıyla hemen `save.dat`'a yazılır. Oyun sahnesindeyse sahne yeniden yüklenir.

### Gerçek sağlayıcıyı bağlamak

1. `ICloudSaveService`'i uygulayan bir sınıf yaz (örn. `Scripts/Core/Services/PlayGamesCloudSaveService.cs`). `IsTest => false`. Bayt dizisini olduğu gibi sakla ve geri ver; içeriği çözme, imzayı `CloudSave` kontrol eder.
2. Açılışta, ilk sahne yüklenmeden takın: `Services.Register(new PlayGamesCloudSaveService());` (Android ve iOS için `#if UNITY_ANDROID` / `#if UNITY_IOS` ile ayrı sınıflar). Başka yerde değişiklik gerekmez; Ayarlar kendiliğinden "Yakında"dan gerçek düğmelere geçer.
3. Girişi oyunun başında sessizce dene (`SignIn`); başarısızsa Ayarlar'daki düğme yine çalışır ve girişi ister.
4. Paketler ve hesaplar sahibin işidir (bu depoda eklenmedi): mağaza konsolunda Saved Games / iCloud yetkisi açılmalı.

**Google Play Games Saved Games:** Google Play Games Plugin for Unity (GPGS v2). `PlayGamesPlatform.Instance.SavedGame.OpenWithAutomaticConflictResolution("taverna", DataSource.ReadCacheOrNetwork, ConflictResolutionStrategy.UseLongestPlaytime, ...)`, sonra `CommitUpdate(meta, SavedGameMetadataUpdate.Builder().WithUpdatedDescription(CloudSave.Summary(meta)).WithUpdatedPlayedTime(...).Build(), bytes, ...)`; okuma `ReadBinaryData`. Play Console'da "Saved Games" açılmalı. Eklentinin kendi çakışma çözümü yalnızca anlık görüntüler arasıdır; oyuncuya sorulacak karar yine `CloudSave.Resolve`'dan gelir.

**iCloud (iOS):** Yerleşik Unity API'si yok. En sade yol küçük bir yerel eklentiyle `NSUbiquitousKeyValueStore` (1 MB'a kadar; kayıt şu an birkaç KB) ya da iCloud Drive belge kapsayıcısına `cloud.dat` yazmak. Xcode'da iCloud yeteneği (Key-Value storage) açılmalı; `NSUbiquitousKeyValueStoreDidChangeExternallyNotification` gelince `Download` yeniden denenebilir. Apple ID girişi sistemindir: `SignedIn` = iCloud hesabı var mı.

**Unity Cloud Save:** `com.unity.services.cloudsave` + `com.unity.services.authentication` paketleri (eklenmedi; eklenmesi gerekirse önce sahibe sorulmalı). `UnityServices.InitializeAsync()`, `AuthenticationService.Instance.SignInAnonymouslyAsync()` (ya da Google / Apple bağlantısı), `CloudSaveService.Instance.Files.Player.SaveAsync("taverna", bytes)` ve `LoadBytesAsync("taverna")`. Anonim giriş cihaz değişince kaybolur; cihazlar arası taşıma için hesap bağlama gerekir. Async çağrıların sonucu ana iş parçacığında geri çağrıya verilmeli.

**Deneme (geliştirme derlemesi):** Ayarlar › Bulut kaydı › YÖNET. Bot: `-autotest-v23 cloud` (yedekle, aynı ilerlemede yükle, başka cihazın belirsiz kaydıyla çakışma, bozuk imza reddi, "bu cihazı tut"); `-autotest-release-sim` ile "Yakında".

