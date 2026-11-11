# Mağazaya çıkış listesi (Taverna Tycoon 1.0.0)

Bu liste, mağazaya çıkmadan önce yapılacakları gösterir. "Kod hazır" maddelerin kodu yazıldı. "Sahibinde" maddeleri hesap, imza, para ya da hukuki karar gerektirdiği için kodla yapılamaz.

| # | Madde | Durum |
|---|---|---|
| 1 | Uygulama kimliği `com.bnstudio.tavernatycoon`, şirket adı `Bn Studio`, sürüm `1.0.0`, Android sürüm kodu 1. Tek yer: `Editor/TavernaBuild.cs` (her yüklemede `AndroidVersionCode` ve `IosBuildNumber` bir artırılmalı) | Kod hazır |
| 2 | Google Play Console geliştirici hesabı (bir kerelik ücret, kimlik doğrulama) | Sahibinde |
| 3 | Apple Developer Program üyeliği (yıllık ücret; iOS derlemesi Mac ve Xcode ister, `docs/ios-kurulum-rehberi.md`) | Sahibinde |
| 4 | İmzalama anahtarı (keystore). Sahibi oluşturur ve güvenli bir yerde iki kopya saklar; kaybolursa güncelleme yayımlanamaz. Google Play App Signing açılması önerilir | Sahibinde |
| 5 | Yayın derlemesi: `TavernaBuild.AndroidRelease` (geliştirme derlemesi değil, IL2CPP, ARM64, AAB). İmza bilgisi ortam değişkenlerinden okunur: `TAVERNA_KEYSTORE`, `TAVERNA_KEYSTORE_PASS`, `TAVERNA_KEY_ALIAS`, `TAVERNA_KEY_PASS`. Biri eksikse derleme durur. Komut: `Unity.exe -batchmode -quit -projectPath Unity/TavernaTycoon -buildTarget Android -executeMethod Taverna.EditorTools.TavernaBuild.AndroidRelease -logFile -` | Kod hazır; anahtar sahibinde, hiç çalıştırılmadı |
| 6 | Reklam ağı hesabı (AdMob / LevelPlay / Unity Ads), uygulama ve reklam birimi kimlikleri, `IAdService` uygulaması ve kaydı (`docs/services-integration.md`) | Arayüz kodu hazır; hesap ve SDK sahibinde |
| 7 | Satın alma: Play Console ve App Store Connect'te ürünler (kimlikler `Premium.List` ile aynı), vergi ve banka bilgileri, `IStoreService` uygulaması (ör. Unity IAP), makbuz doğrulama | Arayüz kodu hazır; hesap ve ürünler sahibinde |
| 8 | Ölçüm hizmeti hesabı (ör. Firebase), `IAnalyticsService` uygulaması | Arayüz ve olaylar hazır; hesap sahibinde |
| 9 | Gerçek sağlayıcı yokken yayın derlemesinde reklam düğmeleri gizli, Hazine "Yakında", ödül verilmez | Kod hazır (testle doğrulandı) |
| 10 | Reklam ya da ölçüm SDK'sı eklendiyse: kullanıcı izni (AB için Google UMP / CMP), iOS için App Tracking Transparency izni ve `NSUserTrackingUsageDescription` | Sahibinde (SDK ile birlikte) |
| 11 | Gizlilik politikası: taslak `docs/privacy-policy-draft.md`. Hizmetler bağlanınca güncellenmeli, hukukçuya gösterilmeli ve herkese açık bir web adresinde yayımlanmalı | Taslak hazır; yayımlama sahibinde |
| 12 | Google Play "Veri güvenliği" formu ve App Store "Gizlilik etiketleri". Bağlanan SDK'ların topladığı veriye göre doldurulur | Sahibinde |
| 13 | Yaş derecelendirmesi (IARC anketi, App Store yaş sorusu). Oyunda içki temalı içerik ve gerçek parayla satın alma var; anket buna göre dürüstçe cevaplanmalı. Çocuklara yönelik ("Families") programa girilmemesi önerilir | Sahibinde |
| 14 | Mağaza görselleri: uygulama ikonu (kodda var: `UI/Images/Logo`), en az 2–8 telefon ekran görüntüsü, Play için 1024×500 tanıtım görseli, kısa ve uzun açıklama, isteğe bağlı tanıtım videosu | Sahibinde (ekran görüntüleri test botuyla alınabilir) |
| 15 | Hedef API seviyesi: Play'in o yılki zorunlu `targetSdkVersion` değeri kontrol edilmeli (şu an "en yüksek kurulu") | Sahibinde, yükleme öncesi |
| 16 | Kapalı test (Play: belirli sayıda test kullanıcısı ve gün şartı olabilir), sonra üretime yayın | Sahibinde |

Notlar:
- Windows'ta şirket adı değiştiği için eski test kayıtları yeni klasörde görünmez (`AppData/LocalLow/Bn Studio/Taverna Tycoon`). Android'de paket adı değiştiği için telefondaki eski test sürümü ayrı bir uygulama olarak kalır.
- Kayıt dosyası cihaza bağlıdır. Hata ayıklama anahtarıyla imzalanmış test sürümünün kaydı, yayın anahtarıyla imzalanmış sürümde açılmayabilir.
