# Taverna Tycoon — Unity Projesi

Unity **6000.6.8f1** · URP (mobil) · UI Toolkit · Input System · Dikey (portrait)

## Açma ve oynama
1. Unity Hub → **Add → Add project from disk** → `Unity/TavernaTycoon` klasörünü seç.
2. `Assets/_Game/Scenes/MainMenu.unity` sahnesini aç ve **Play**'e bas.
3. Game penceresinin çözünürlüğünü **1080x1920 (dikey)** yap.
4. Kontroller:
   - Fareyle ekrana basılı tutup sürükle → joystick
   - Klavyeden **WASD** ya da **ok tuşları** da çalışır
5. Hazır derlenmiş test sürümü: `Unity/Builds/Windows/TavernaTycoon.exe`

## Oyun döngüsü (prototip — Aşama 1)
1. Fıçının önüne git → bekleyen sipariş varsa tepsiye içecek alırsın (akıllı alma).
2. Müşterinin yanına git → servis otomatik yapılır.
3. Müşteri içer ve parayı masaya bırakır → yanına gidince para toplanır.
4. Para şunlara harcanır:
   - **Ödeme kareleri:** Elma Presi, yeni masalar, Bal Kazanı
   - **YÜKSELT butonu:** İstasyonun Hız / Stok / Kalite seviyeleri
5. Mutlu müşteriler Ün kazandırır. Ün dolunca seviye atlanır ve yeni müşteriler ile içecekler açılır (GAME_DESIGN.md §3.3).

## Her şey koddan üretilir
Blender tarafı modelleri `assets/` klasörüne üretir. Unity tarafı bunları içe aktarıp sahneleri kurar. Elle yapılan bir sahne düzenlemesi, bir sonraki kurulumda üzerine yazılır. Kalıcı bir değişiklik istiyorsan `Assets/_Game/Editor/TavernaSceneBuilder.cs` dosyasını düzenle.

| Menü (Unity üst çubuğu → **Taverna**) | Ne yapar |
|---|---|
| Her Şeyi Kur | Blender çıktılarını kopyalar, prefabları, verileri (içecek/müşteri) ve iki sahneyi kurar |
| Sadece Sahneleri Yeniden Kur | Sadece MainMenu ve Game sahnelerini yeniden kurar |
| Önizleme Görüntüsü Al | `Unity/captures/` klasörüne sahne görüntüsü kaydeder |
| Otomatik Oyun Testi | Bot oynar ve `captures/playtest_report.txt` raporunu yazar |
| Derle ▸ Windows Test Sürümü / Android APK | Derleme alır |

Komut satırından (Unity kapalıyken):
```
Unity.exe -batchmode -quit -projectPath Unity/TavernaTycoon -executeMethod Taverna.EditorTools.TavernaSceneBuilder.RunAll
Unity.exe -batchmode -quit -projectPath Unity/TavernaTycoon -executeMethod Taverna.EditorTools.TavernaBuild.WindowsDev
Builds/Windows/TavernaTycoon.exe -autotest     # ekran görüntüleri + rapor üretir, sonra kapanır
```

## Kod yapısı (`Assets/_Game`)
| Klasör | İçerik |
|---|---|
| `Scripts/Core` | `Balance` (tüm denge formülleri, GAME_DESIGN.md ile birebir), `GameState` (para/ün/seviye/kayıt), `GameSettings`, veri tipleri |
| `Scripts/Gameplay` | Oyuncu, Taşıyıcı (tepsi), İstasyon, Masa/Koltuk, Müşteri, Müşteri Üretici, Para Yığını, Ödeme Karesi, Kamera, ToyAnimator (iskeletsiz zıplama animasyonu) |
| `Scripts/UI` | Ana menü, HUD (joystick, baloncuklar, yükseltme paneli), Ayarlar, sabır halkası |
| `Scripts/Debug` | Otomatik test botu (sadece editör ve geliştirme sürümünde çalışır) |
| `UI` | `Theme.uss` (design/ui/ui_mockups.html'den), `MainMenu.uxml`, `Hud.uxml`, `Settings.uxml` |
| `Data` | 10 içecek + 30 müşteri tanımı (kurulumda üretilir) |
| `Editor` | İçe aktarma, veri, sahne kurucu, önizleme, test, derleme |

## v0.2 ile gelenler
- **Büyük harita:** Bahçe | Meyhane | Ana Salon + sokak. Kilitli bölgeler baştan görünür.
- **Kamera:** Geniş açılı. İki parmakla ya da fare tekerleğiyle yakınlaştırılır.
- **Hedef zinciri:** 19 ödeme karesi. "Sıradaki Hedef" kutusu ve hedefe giden zıplayan ok (hedef ekran dışındaysa ok kenardan yönü gösterir).
- **Garsonlar:** Pip ve Tobi siparişleri üstlenir, servis eder, paraları toplar.
- **Yoğun Saat:** Müşteri ×2, bahşiş ×2.
- **Günlük görevler:** Rozet ve ödül.
- **"Sen yokken…":** Çevrimdışı kazanç penceresi.
- **Ses:** Koddan sentezlenen ses efektleri ve döngü müziği.
- **Efektler:** Uçan paralar ve konfeti.

## Bilinen eksikler / sıradakiler
- **Hareket:** Karakterlerde henüz iskelet yok. Hareket kodla verilen zıplama animasyonundan (ToyAnimator) geliyor. Müşteriler oturmuyor, masanın yanında ayakta duruyor.
- **Fontlar:** Henüz varsayılan font kullanılıyor. Tasarımdaki fontlar Lilita One ve Nunito (indirilmesi gerekiyor).
- **Personel:** Pip garson, barmen ve temizlikçi henüz oyunda yok (GAME_DESIGN §5).
- **Ses:** Ses ve müzik yok.
- **Ekranlar:** Seviye atlama ekranı şimdilik sadece bir bildirim. Mağaza, görevler ve koleksiyon defteri yapılmadı.
- **iOS:** iOS derlemesi için Unity Hub'dan iOS modülünün eklenmesi gerekiyor.
