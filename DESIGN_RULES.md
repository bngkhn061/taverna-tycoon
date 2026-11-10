# Tasarım Kuralları — Taverna Tycoon (çalışma adı)

> Bu dosya projenin **görsel anayasası**. Her model, her karakter, her UI ikonu bu kurallara uymak zorunda.
> Bir tasarım bu kurallarla çelişiyorsa ya tasarım değişir ya da önce bu dosya güncellenir.

---

## 1. Oyun Özeti (tasarımı yönlendiren kısmı)

- **Tür:** Mobil 3D arcade-idle taverna tycoon. Ortaçağ-fantastik bir hanı işletip içecek satıyorsun.
- **İlham:** Steam'deki *Tavern Master* gibi taverna kur–büyüt–personel tut döngüsü. **Sadece türden ve mekanikten ilham alıyoruz**. Onun karakterlerini, modellerini, logosunu ya da görünüşünü kopyalamıyoruz. Bizim çizgimiz aşağıda tanımlanan **"Oyuncak Taverna"** stili.
- **Oynanış:** Oyuncu ana karakteri 3. şahıs kamerayla yürütür. Fıçıdan içecek doldurur, müşterinin masasına götürür, parayı toplar. Zamanla personel tutar, iş otomatiğe biner (idle), oyuncu da genişlemeye odaklanır.
- **Temel his:** Sıcak, komik, rahat. Biraz telaşlı ama asla stresli değil.

---

## 2. Boyut ve Kamera Kararı

| Seçenek | Karar | Neden |
|---|---|---|
| 2D | ❌ | 3. şahıs kamera ve Blender iş akışıyla uyuşmuyor. 30 müşteri × animasyon, 2D'de çok pahalı. |
| 2.5D (sprite + 3D zemin) | ❌ | Karakterin dönüşleri ve kamera açısı sınırlı kalır. |
| **3D low-poly** | ✅ | Blender'la üretim, karakterler arası ortak iskelet ve animasyon, arcade-idle türünün standardı. |

**Ekran yönü: YATAY (landscape)** — kullanıcı kararı (v0.3). Telefon iki yatay yöne döner, dikeye dönmez.
Arayüz referansı 844×390 dp, yüksekliğe göre ölçeklenir. Kaynak tasarım: `design/ui/ui_v2_landscape.html`.

**Kamera (mobil 3. şahıs, uzak):** başlangıç mesafesi 14, FOV 40, eğim 52°. İki parmakla / tekerlekle 9–22 arası yakınlaştırma.
*(Aşağıdaki ilk değerler dikey dönemden kalmadır.)*
- Yüksek açılı **takip kamerası**, omuz üstü değil. Oyuncu hem karakterini hem masaları görmeli.
- Eğim **50°**, FOV **35°** (hafif tele, oyuncak gibi görünür), karaktere uzaklık **~9 m**.
- Kamera dönmez, hep sabit yönden bakar. Taverna duvarlarının kameraya bakan yüzleri otomatik saydamlaşır / kesilir (cutaway).
- Kontrol: Ekranın her yerinde çalışan sanal joystick (floating joystick). Servis, müşteriye yaklaşınca otomatik yapılır.

---

## 3. Görsel Stil: "Oyuncak Taverna"

Tek cümleyle: **Elle boyanmış ahşap oyuncak figürler gibi. Tıknaz, yuvarlak hatlı, düz renkli, sıcak ışıklı.**

### 3.1 Altın Kurallar
1. **Siluet her şeydir.** Her müşteri tipi, renk olmadan, siyah siluetiyle bile tanınabilmeli (şapka, sırt eşyası, vücut tipi).
2. **Detayı geometriyle ver, dokuyla değil.** Çizgi, desen ya da gerçekçi doku yok. Bir düğme gerekiyorsa küçük bir küp ya da silindirdir.
3. **Sert ama yumuşatılmış kenarlar.** Low-poly yüzeyler kullan, ama ana kenarlarda 1 segmentlik bevel olsun. Sivri, keskin üçgen görünüm yok.
4. **Az renk, güçlü renk.** Her karakter en fazla **4 ana renk** + ten + göz.
5. **Abartı.** Büyük kafa, büyük eller, büyük ayakkabılar, küçük gövde. İçecek bardakları da gerçekte olduğundan büyük.
6. **Sıcaklık.** Ortam turuncu-amber ışıkla aydınlanır. Soğuk renkler sadece vurgu ya da özel müşteriler içindir.

### 3.2 Karakter Oranları
- **Ana karakterler:** 2.5 kafa boyu. **Müşteriler:** 2–3 kafa boyu (vücut tipine göre).
- Oyun içi boy: standart insan müşteri **1.1 m**. Kapı, masa ve tezgâh bu ölçüye göre kurulur.
- Gözler: Basit **siyah oval** (iki küçük elipsoid), içinde tek beyaz parlama noktası. Ağız sadece ifade blend-shape'inde görünür.
- Burun: Basit bir top ya da küçük bir koni. Kulaklar ırka göre değişir.
- Eller: 4 parmaklı **eldiven tipi** (başparmak + birleşik 3 parmak). Bardak tutmaya uygun pozda modellenir.

### 3.2.1 Personel Üniforması
Tüm personelde tavernanın **Tavernacı Kırmızısı** (`#C8443A`) bir önlük, fular ya da bant olarak bulunur. Oyuncu, müşteriyle personeli tek bakışta ayırır. Müşteriler kırmızıyı ana renk olarak kullanabilir, ama önlük ya da fular olarak kullanamaz.

### 3.3 Vücut Tabanları (ortak iskelet için)
Müşterilerin hepsi **3 taban gövdeden** türetilir. Böylece animasyonlar paylaşılır:

| Taban | Boy | Kullanım |
|---|---|---|
| `BODY_S` (Küçük) | 0.75 m | Cüce, goblin, buçukluk, çocuk |
| `BODY_M` (Orta) | 1.1 m | İnsanların çoğu, elf, ana karakterler |
| `BODY_L` (İri) | 1.45 m | Ork, trol, şövalye, deniz kurdu |

Hepsi **aynı kemik isimlerini** taşır (Humanoid uyumlu). Taban farkları sadece kemik uzunluğu ve ölçek farkıdır.

---

## 4. Renk Paleti

Tüm modeller **tek bir 256×256 palet dokusunu** (`T_Palette_Main.png`) paylaşır. UV'ler renk karelerine yerleştirilir. Böylece tüm sahne tek materyal ve tek draw call batch'i olur.

### 4.1 Ortam (taverna)
| İsim | Hex | Kullanım |
|---|---|---|
| Meşe | `#8B5A2B` | Ana ahşap: masa, zemin |
| Koyu Meşe | `#5C3A1E` | Kirişler, gölgeli ahşap |
| Bal | `#E0A040` | Fıçı çemberi, cila parlaması |
| Taş | `#9C9488` | Duvar, şömine |
| Koyu Taş | `#6B6560` | Taş derzleri, döşeme |
| Şömine Ateşi | `#FF8A2A` | Ateş, mum, ışık |
| Bez | `#D9C7A3` | Önlük, perde, çuval |

### 4.2 Karakter Vurguları
| İsim | Hex |
|---|---|
| Tavernacı Kırmızısı | `#C8443A` |
| Orman Yeşili | `#4F7F3A` |
| Kraliyet Mavisi | `#3E5FA8` |
| Mor Büyü | `#7A4FA8` |
| Altın | `#F2C14E` |
| Gece Lacivert | `#2B2F4A` |
| Krem | `#F4E8D0` |

### 4.3 Tenler
`#F6D3B3` · `#E8B48A` · `#C68A5E` · `#8D5A3B` · `#5E3A26` + fantastik: Ork `#7FA05A`, Goblin `#9CC45A`, Trol `#7E8C9A`, Elf açık `#F3E4CF`

### 4.4 Saç ve Göz
| İsim | Hex |
|---|---|
| Kızıl Saç | `#B5482A` |
| Kahve Saç | `#5A3A22` |
| Sarı Saç | `#E8C070` |
| Gri Saç | `#B8B4AE` |
| Koyu Saç | `#2E221C` |
| Göz | `#2A2220` |
| Göz Parlaması | `#FFFFFF` (tek izin verilen saf beyaz) |

> Paletin tek kaynağı `tools/blender/toylib.py` içindeki `PALETTE` listesi. `T_Palette_Main.png` oradan üretilir.
> Yeni renk **sadece listenin sonuna** eklenir. Araya eklenirse var olan modellerin UV'leri kayar.

### 4.5 Yasaklar
- Saf siyah `#000000` ve saf beyaz `#FFFFFF` modellerde kullanılmaz (göz parlaması hariç).
- Neon / fosforlu renk yok (özel "büyülü" içecek efektleri hariç).

---

## 5. Işık ve Render

- **Unlit değil, basit lit.** Tek directional light (sıcak, `#FFE2B8`) + ambient (`#6A5A7A`, morumsu gölge).
- Gölgeler: Sadece karakterlerin altında **blob shadow** (yumuşak siyah daire). Gerçek zamanlı gölge sadece üst cihazlarda.
- **Outline yok.** Siluet renk kontrastı ve rim light ile ayrılır.
- Post-process: Hafif bloom (sadece ateş ve içecek parıltıları için) + hafif vinyet.

---

## 6. Teknik Bütçe (Mobil)

| Varlık | Üçgen sınırı | Doku |
|---|---|---|
| Ana karakter | 4.000 | Palet + 1 adet 128px yüz atlası |
| Müşteri | 2.000 | Sadece palet |
| Mobilya (masa, sandalye) | 300–600 | Palet |
| Fıçı / tezgâh | 800 | Palet |
| Bardak / içecek | 100–200 | Palet |
| Sahnedeki toplam (hedef) | < 100.000 | — |

- Kemik sayısı: Karakter başına **en fazla 30**. Yüz kemiği yok, ifadeler blend-shape ile (en fazla 4: mutlu, kızgın, şaşkın, sarhoş).
- LOD: Müşterilerde LOD1 = %50 üçgen, 12 m'den sonra devreye girer.

---

## 7. Animasyon Kuralları

- Stil: **Zıplamalı ve abartılı** (squash & stretch hissi). Yürürken kafa hafifçe sallanır.
- Tüm müşteriler aynı temel seti paylaşır:
  `Idle` · `Walk` · `Sit` · `Sit_Idle` · `Drink` · `Wait_Impatient` · `Happy_Cheer` · `Angry_Leave` · `Pay`
- Her müşteri tipinin **1 adet kendine özel** animasyonu olabilir (`Special_<Ad>`), örn. cücenin göbek atması.
- Ana karakterlere ek olarak: `Carry_Walk` · `Pour` · `Serve` · `Collect_Coin` · `Run`
- FPS: 30, döngüler tam kare sayısıyla kapanır.

---

## 8. İçecekler (müşteri isteklerinin ortak dili)

Her içeceğin **tek bakışta tanınan** bir bardak siluetiyle birlikte rengi ve ikonu vardır. Müşterinin baloncuğunda bu ikon çıkar.

| # | İçecek | Bardak | Renk | Açılış |
|---|---|---|---|---|
| 1 | Bira (Ale) | Kulplu tahta maşrapa | Amber `#E0A040` | Başlangıç |
| 2 | Elma Şırası (Cider) | Kısa cam kupa | Açık sarı `#F2D06B` | Başlangıç |
| 3 | Bal Şarabı (Mead) | Boynuz | Bal `#D88A2A` | Sv 3 |
| 4 | Kırmızı Şarap | Ayaklı kadeh | Bordo `#8A1E2E` | Sv 5 |
| 5 | Kara Bira (Stout) | Kalın bardak, köpüklü | Koyu kahve `#3A2418` | Sv 8 |
| 6 | Bitki Çayı | Küçük fincan | Yeşil `#7FAF5A` | Sv 10 |
| 7 | Goblin Grogu | Yamuk kupa | Çamur yeşili `#6E8A2A` | Sv 14 |
| 8 | Elf Çiyi | İnce uzun flüt | Açık mavi, parıltılı `#A8E0F0` | Sv 18 |
| 9 | Ejder Ateşi | Metal kadeh, alevli | Turuncu-kırmızı `#FF5A1E` | Sv 24 |
| 10 | Süt | Toprak testi | Krem `#F4E8D0` | Sv 6 |

---

## 9. UI Stili

- Malzeme dili: **Ahşap tabela + parşömen.** Butonlar oyma tahta plaka, paneller parşömen.
- Font: Başlık **Lilita One**, gövde **Nunito** (800–900 kalınlık). İkisi de Google Fonts'ta, açık lisanslı (OFL).
- Referans tasarım: `design/ui/ui_mockups.html` (renk jetonları ve buton stilleri burada tanımlı).
- İstek baloncukları: Beyaz-krem yuvarlak baloncuk, içinde içecek ikonu, altında **sabır halkası** (yeşil → sarı → kırmızı).
- Ekran düzeni (yatay): Sol üst para/elmas · üst orta Sıradaki Hedef · sağ üst seviye kalkanı + ayarlar · sağ kenar Görevler/Mağaza · sol alt joystick · sağ alt eylem (YÜKSELT). Paneller ortayı kapatmaz: yükseltme sağdan, görevler soldan çekmece.
- UI görselleri taslaktan üretilir: `python tools/ui/make_ui_kit.py` → `blender -b --python tools/ui/crop_ui_kit.py` → `assets/ui/*.png` (9-dilim, 2x).

---

## 10. Blender İş Akışı

- Blender sürümü: **5.2** (bu makinede kurulu).
- Ölçek: **1 Blender birimi = 1 metre**. Karakterin orijini ayakların arasında, zeminde.
- Yön: Karakter **-Y yönüne** bakar (Blender'ın önden görünümü).
- Dosya adları:
  - `CH_Main_<Ad>.blend` (ana karakterler)
  - `CH_Cust_<NN>_<Ad>.blend` (müşteriler, örn. `CH_Cust_07_Dwarf.blend`)
  - `PR_<Kategori>_<Ad>.blend` (eşyalar, örn. `PR_Furniture_TableRound.blend`)
  - Animasyonlar: `<Karakter>@<Anim>` (örn. `BODY_M@Walk`)
- Dışa aktarma: **FBX** (motor varsayılanı Unity), uygula: Scale 1.0, "Apply Transform" açık, sadece Deform kemikleri.
- Her yeni karakter `BODY_S/M/L` taban dosyasından kopyalanarak başlar. Sıfırdan iskelet kurulmaz.

### 10.1 Script ile üretim
Modeller elle değil, **Python scriptleriyle** üretilir. Böylece her model tekrar üretilebilir ve kurallara otomatik uyar.
- `tools/blender/toylib.py`: Palet, primitifler (`sphere`, `dome`, `box`, `cyl`, `torus`, `limb`), birleştirme, FBX export, önizleme render'ı.
- `tools/blender/build_*.py`: Her karakter grubunun üretim scripti.
- Çalıştırma: `blender -b --factory-startup --python tools/blender/build_main_characters.py`
- Çıktılar:
  - `assets/models/characters/*.blend`
  - `assets/export/fbx/*.fbx`
  - `assets/renders/*.png` (önden görünüm + oyun kamerasından görünüm)
- Her üretimde script üçgen sayısını yazdırır. Bütçe (§6) aşılırsa model reddedilir.

---

## 11. Kontrol Listesi (her yeni tasarım için)

- [ ] Siyah siluetiyle diğerlerinden ayırt edilebiliyor mu?
- [ ] En fazla 4 ana renk mi, hepsi paletten mi?
- [ ] Üçgen bütçesinin altında mı?
- [ ] Doğru taban gövdeyi ve iskeleti mi kullanıyor?
- [ ] 50° kamera açısından ve telefonda ~2 cm boyutunda okunuyor mu?
- [ ] Sıcak, komik, "oyuncak" hissi veriyor mu?
- [ ] Başka bir oyunun karakterine benzemiyor mu?
