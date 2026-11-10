# Taverna Tycoon: iPhone'a kurulum rehberi (Mac + Xcode)

Bu klasörde oyunun hazır **Xcode projesi** var. Unity kurman gerekmiyor; Mac'te Xcode ile açıp iPhone'a kurabilirsin.

## Gerekenler

- Mac ve **Xcode** (App Store'dan ücretsiz; güncel sürüm önerilir).
- Bir **Apple ID** (ücretsiz hesap yeter, sadece kendi iPhone'una kuracaksan).
- **iPhone**, iOS 15 ya da üstü, ve Mac'e bağlamak için kablo.

## 1. Projeyi aç

1. Zip'i aç.
2. `Unity-iPhone.xcodeproj` dosyasına çift tıkla. Xcode açılır; ilk açılışta birkaç dakika "Indexing" yazabilir, bekle.

## 2. Apple ID'ni Xcode'a ekle

`Xcode > Settings… > Accounts` sekmesinde sol alttaki **+** düğmesine bas, **Apple ID** seç ve giriş yap.

## 3. İmzalamayı ayarla

1. Sol panelde en üstteki mavi **Unity-iPhone** proje simgesine tıkla.
2. Ortadaki **TARGETS** listesinden **Unity-iPhone**'u seç, üstten **Signing & Capabilities** sekmesine geç.
3. **Automatically manage signing** işaretli olsun.
4. **Team**: kendi Apple ID'ni seç (ücretsiz hesapta "(Personal Team)" yazar).
5. **Bundle Identifier**: `com.tavernatycoon.prototype` yazıyor. Kırmızı hata çıkarsa (ör. "Failed to register bundle identifier"), kendine özel bir şeyle değiştir: `com.seninadin.tavernatycoon`.
6. TARGETS'ta **UnityFramework** için de imza hatası görürsen aynı Team'i seç.

## 4. iPhone'u hazırla

1. iPhone'u kabloyla Mac'e bağla, telefonda **"Bu Bilgisayara Güven"** de.
2. iPhone'da **Ayarlar > Gizlilik ve Güvenlik > Geliştirici Modu**'nu aç. Telefon yeniden başlar; açılınca "Aç" de. (Bu seçenek, telefon Xcode'a bir kez bağlandıktan sonra görünür.)

## 5. Kur ve çalıştır

1. Xcode'un üst ortasındaki cihaz listesinden **kendi iPhone'unu** seç.
2. Sol üstteki **▶ (Run)** düğmesine bas (ya da `Cmd + R`). İlk derleme 5–15 dakika sürebilir.
3. iPhone'da **"Güvenilmeyen Geliştirici"** uyarısı çıkarsa: **Ayarlar > Genel > VPN ve Cihaz Yönetimi** bölümünde Apple ID'ni seç, **Güven**'e bas. Sonra oyunu tekrar aç.

> **Ücretsiz Apple ID ile:** Uygulama **7 gün** sonra açılmaz olur. Telefonu tekrar bağlayıp ▶ ile yeniden kurman yeter; kayıt silinmez.

## 6. Başkalarına göndermek: TestFlight (isteğe bağlı)

Kablo olmadan başkalarının da kurabilmesi için **Apple Developer Program** üyeliği gerekir (yıllık 99 $).

1. [App Store Connect](https://appstoreconnect.apple.com) > **Apps** > **+ New App**. Bundle ID olarak Xcode'daki kimliği seç.
2. Xcode'da cihaz listesinden **Any iOS Device (arm64)** seç.
3. **Product > Archive**. Bitince açılan **Organizer** penceresinde **Distribute App > TestFlight & App Store > Upload**.
4. App Store Connect'te **TestFlight** sekmesine geç:
   - **Internal Testing:** ekipten en fazla 100 kişi, inceleme yok.
   - **External Testing:** e-posta ya da **genel link** ile 10.000 kişiye kadar. İlk derlemede Apple kısa bir inceleme yapar, genelde 1 gün.
5. Şifreleme (Export Compliance) sorusu gelirse: oyun özel şifreleme kullanmıyor; **"None of the algorithms mentioned above"** seç.
6. Testçiler iPhone'a **TestFlight** uygulamasını kurar ve davetteki linkten oyunu yükler.

## Sık karşılaşılan hatalar

| Hata | Çözüm |
|---|---|
| "No account for team" / "Signing requires a development team" | Adım 2 ve 3: Apple ID'yi ekle, Team'i seç. |
| "Failed to register bundle identifier" | Bundle Identifier'ı kendine özel yap (adım 3.5). |
| "Untrusted Developer" | Adım 5.3: VPN ve Cihaz Yönetimi > Güven. |
| "Sandbox: rsync … deny" derleme hatası | Unity-iPhone hedefi > **Build Settings** > **User Script Sandboxing** = **No**. |
| Cihaz listede yok / "Preparing device" | Kabloyu çıkar-tak, telefonun kilidini aç, Geliştirici Modu'nu kontrol et, biraz bekle. |

## Oyun hakkında

- Oyun **yatay** oynanır.
- Bu sürümde **Hazine (gerçek para) satın almaları kapalı**, "Mağaza yakında" yazar.
- Kayıt telefonda tutulur. Uygulamayı silmek ilerlemeyi siler; aynı Bundle ID ile tekrar kurmak silmez.
