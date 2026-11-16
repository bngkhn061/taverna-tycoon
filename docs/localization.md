# Yerelleştirme (v1.7)

Taverna Tycoon 10 dilde oynanır: Türkçe (`tr`, kaynak dil), İngilizce (`en`), Basitleştirilmiş Çince (`zh-Hans`), İspanyolca (`es`), Brezilya Portekizcesi (`pt-BR`), Almanca (`de`), Fransızca (`fr`), Rusça (`ru`), Japonca (`ja`), Korece (`ko`).

> **Çeviriler model üretimidir.** Yayından önce her dili anadili konuşan biri gözden geçirmelidir: ton (kısa, sıcak, mobil oyun dili), terim tutarlılığı (altın, elmas, Ün, kademe, şube), düğme uzunlukları ve kültürel uygunluk. Gözden geçirme yapılmadan mağaza metinleri ve ekran görüntüleri o dilde yayımlanmamalı.

## Nasıl çalışır

- **Anahtar = Türkçe cümle** (gettext tarzı). Kod `Loc.T("Yeterli altın yok")` yazar; etkin dilde çeviri varsa o, yoksa Türkçe döner.
- **Biçimli metin:** `Loc.F("Seviye {0} gerekli", lv)`. Yer tutucular `{0}`, `{1:0.0}` gibi; çeviri aynı numaraları kullanmalı, sırası değişebilir. Biçim değişmez kültürle yapılır (ondalık nokta).
- **Veri tabloları** (Catalog, Themes, Tiers, Premium, TavernEvents, Tutorial.Texts, TavernaData içecek ve müşteri adları, sahne kurucusundaki ödeme karesi etiketleri) metinlerini `Loc.N("…")` ile işaretler. `N` çevirmez, yalnızca anahtar toplayıcıya "bu bir metin" der. Gösterilirken `Loc.T(alan)` ile çevrilir. Kayıt ve kimlikler (`id`, `Unlock("salon")`) hiç değişmez.
- **UXML metinleri** elle düzenlenmez: `Loc.Localize(root)` görsel ağacı gezer, Label / Button metinlerini kaynak olarak kaydeder ve çevirir. Dil değişince yalnızca kodun değiştirmediği metinler yenilenir.
- **Dil değişimi** (`Loc.SetLanguage`) tabloyu yükler ve `Loc.Changed` olayını yayar. Ana menü ve HUD UXML'i, ayarları, dünya etiketlerini ve açık panelleri yeniden kurar; sahne yeniden yüklenmez, oyuncu ayar ekranından çıkmadan sonucu görür.
- **İlk açılış:** `Application.systemLanguage` desteklenen bir dile eşlenir (Geleneksel Çince basitleştirilmişe), eşleşme yoksa İngilizce. Sonra oyuncunun seçimi geçerli (`GameSettings.Language`, eski `TR`/`EN` değerleri okunur).
- **Türkçe ekler:** ek üreten yardımcılar (`HudV12.Locative`, yıldız ipucundaki `'ye/'e`) yalnızca Türkçede ek döndürür, diğer dillerde boş. Bu yüzden çeviri kalıbında o yer tutucu (`{1}`) boş gelir; cümleyi ona göre kurun.
- **Oyuncunun yazdığı taverna adı** çevrilmez; varsayılan ad (`Barlo'nun Tavernası`) çevrilir.
- **Sayılar:** `HudController.FormatNum` dilden bağımsız (bin ayırıcı nokta). Kısaltmalar çevrilir: `{0}B` (bin) → en `{0}K`, de `{0} Tsd.`, ru `{0} тыс.`; `{0}M` → `{0} Mio.`, `{0} млн`…
- **Fiyatlar:** gerçek mağaza fiyatı mağazanın yerel metnidir. Aylık VIP'nin "/ay" eki `{0}/ay` anahtarıyla çevrilir.

## Dosyalar

| Yer | Ne |
|---|---|
| `Assets/_Game/Scripts/Core/Loc.cs` | Sistem: `T`, `F`, `N`, `Upper`, `Localize`, dil seçimi, JSON okuyucu |
| `Assets/_Game/Scripts/Core/LocKeys.cs` | Anahtar toplayıcı (yalnızca editör) |
| `Assets/_Game/Scripts/Core/LocFontSet.cs` | CJK yazı tiplerinin sırası (dile göre) |
| `Assets/_Game/Resources/Loc/<dil>.json` | Çeviri tabloları: düz `{"Türkçe": "çeviri"}`, satır başına bir anahtar |
| `Assets/Tests/EditMode/LocalizationTests.cs` | Her dilde her anahtar var, boş değil, yer tutucular aynı, artık anahtar yok |
| `Editor/TavernaLocTools.cs` | Menü: Taverna › Yerelleştirme › Anahtarları Denetle (eksikleri `Library/LocMissing/<dil>.json`'a döker) |

## Yazı tipleri

Zincir (`TavernaImport.BuildFonts`, döngüsüz): Nunito → [NunitoExt, Rubik]; Rubik → [NunitoExt (₺), Noto Sans SC, JP, KR]; LilitaOne → [Nunito].

- Rusça gövde metni Nunito'da yok, Rubik'ten düşer (Rubik Latin + Kiril içerir).
- CJK: Noto Sans SC / JP / KR Bold (SIL OFL 1.1, `UI/Fonts/OFL-NotoSansCJK.txt`). Dinamik, çoklu atlas, 1024² atlas, 48 px örnekleme; derlemede atlas verisi temizlenir, yalnızca kullanılan karakterler çalışırken çizilir.
- Çince ile Japonca aynı Han kod noktalarını farklı çizer: etkin dil zh-Hans → SC, ja → JP, ko → KR önce gelir (`LocFonts.Apply`).
- Noto CJK yazı tipleri ★ → ✓ gibi sembolleri de kapsar. ✗ (U+2717) ve ➜ (U+279C) hiçbir yazı tipinde yok; v1.8: kademe ve prestij kartlarında ok ve onay işareti yazı karakteri yerine ikon görseli (`Kit/v3i_arrow`, `Kit/v3i_check`).

## Yeni metin (anahtar) ekleme

1. Kodda metni `Loc.T("…")` ya da `Loc.F("… {0} …", x)` ile yaz. Veri tablosundaysa `Loc.N("…")` ile işaretle ve gösterdiğin yerde `Loc.T(alan)` kullan. UXML'e yazılan metin kendiliğinden anahtardır.
2. Cümleyi parçalama: "X" + " açık" yerine `Loc.F("{0} açık", x)`. Her cümle tek anahtar.
3. Kimlik, USS sınıfı, `Resources.Load` yolu, `[Playtest]` ve `Debug.Log` metni sarılmaz.
4. Editörde Taverna › Yerelleştirme › Anahtarları Denetle → `Library/LocMissing/<dil>.json` eksikleri listeler. Her dil tablosuna ekle (aynı yer tutucular).
5. EditMode testlerini çalıştır (`LocalizationTests`): eksik, boş, yer tutucu uyuşmazlığı ya da kodda artık olmayan anahtar varsa test düşer.
6. Uzun dillerde (Almanca, Rusça) dar alanlara bak: botla `-autotest-lang de` görüntüsü al.

## Yeni dil ekleme

1. `Loc.Codes` ve `Loc.NativeNames`'e kodu ve dilin kendi adını ekle; gerekiyorsa `Loc.FromSystemLanguage` eşlemesine.
2. `Resources/Loc/<kod>.json` oluştur (Denetle aracının çıktısı başlangıç olabilir), bütün anahtarları çevir.
3. Alfabe mevcut yazı tiplerinde yoksa (ör. Arapça, Tayca, Hintçe) uygun bir OFL yazı tipi ekle, `BuildFonts` zincirine bağla. Sağdan sola diller için ayrıca düzen çalışması gerekir (bu sürümde yok).
4. Testleri çalıştır, `-autotest-lang <kod>` ile temel ekranların görüntüsünü al ve taşma kontrolü yap.
5. Mağaza metinlerini ve gizlilik politikasını o dilde hazırla (`docs/release-checklist.md`).

## Taşma kontrolü

Bot bayrağı: `-autotest-lang <kod>`. Ana menü koşusu ayarlar ve dil seçici (`app_lang.png`) dahil görüntü alır; oyun koşusu HUD, dükkân sekmeleri, depo, görevler, etkinlikler, mutluluk kartı, seviye kartı ve Hazine görüntülerini alır; `-autotest-v15 prestige` şube kartını gösterir. Kanıtlar `production/qa/evidence/2026-11-11-v17-*`.
