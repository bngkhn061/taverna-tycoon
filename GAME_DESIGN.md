# Oyun Tasarımı — Taverna Tycoon (çalışma adı)

> Görsel kurallar: [DESIGN_RULES.md](DESIGN_RULES.md) · Karakterler: [CHARACTERS.md](CHARACTERS.md)
> Bu belge oyunun **nasıl oynandığını, nasıl ilerlediğini ve nasıl dengelendiğini** tanımlar.

---

## 1. Tasarım Hedefi

**"Hep bir sonraki hedef görünür, hiçbiri uzak değil."**

- **Ne zor ne kolay:** Kaybetme durumu yok. Kızgın müşteri sadece parasını vermeden gider. Zorluk hız ve kalabalıktan gelir, ceza yoktur.
- **Sürekli gelişme isteği:** Aktif oyunda **en fazla 2–5 dakikada bir** anlamlı bir şey satın alınabilir olmalı. Ekranda her zaman "neredeyse alabildiğim" bir yükseltme durur.
- **Rol değişimi:** Oyuncu önce **koşturan bir tavernacıdır**, sonra **işi yöneten bir patron** olur. Otomasyon arttıkça oyuncunun işi servis etmekten büyütmeye ve düzenlemeye kayar.

---

## 2. Çekirdek Döngü

```
Müşteri gelir → masaya oturur → istek baloncuğu çıkar (içecek ikonu + sabır halkası)
      ↓
Barlo üretim istasyonuna yürür → tepsiye içecek alır (taşıma kapasitesi kadar)
      ↓
Masaya götürür → müşteri içer → parayı masaya bırakır → Barlo parayı toplar
      ↓
Para → yükseltme / personel / genişletme / dekor   ·   Ün (XP) → taverna seviyesi → yeni içerik
```

- **Kontrol:** Tek parmak, ekranın herhangi bir yerinde beliren joystick. Diğer her şey **yaklaşınca otomatik** olur: almak, servis, para toplama.
- **Ödeme alanları (arcade-idle imzası):** Satın almalar, yere çizili **ödeme karelerinin** üstünde durunca yapılır. Paralar Barlo'dan kareye akar. Menüden alınan yükseltmeler ise ayrıca var (§5).

---

## 3. İlerleme Yapısı

### 3.1 Taverna Seviyesi (Ün)
- Mutlu ayrılan her müşteri **Ün** verir. Ün dolunca taverna seviye atlar.
- Her seviye **mutlaka bir şey açar** (tablo §3.3). Seviye atlamak ödül ekranıyla kutlanır.
- Gereken Ün: Sv12'ye kadar `ün(sv) = 20 × 1.32^(sv-1)`, Sv12–16 `×1.15`, Sv16 sonrası `×1.08` → Sv2: 20, Sv10: ~240, Sv20: ~1.000, Sv29: ~2.000 (v1.0 tempo ölçümü 2026-11-10, `design/balance/balance-check-economy-2026-11-10.md`)
- Zengin (ödeme ★4+) müşteri servis başına 2 Ün verir. Kızgın müşteri Ün götürmez, ceza yıldızdan.

### 3.2 Aşamalar (tavernanın fiziksel büyümesi)
| Aşama | Ad | Seviye | Alan | Ne değişir |
|---|---|---|---|---|
| 1 | **Yol Kenarı Meyhanesi** | 1–5 | Tek oda, 3 masa | Her şeyi Barlo yapar. Öğretici aşama. |
| 2 | **Köy Tavernası** | 6–12 | + Ana salon, 8 masa | Pip ilk garson olur. Temizlikçi ve barmen açılır. |
| 3 | **Kasaba Hanı** | 13–20 | + Arka bahçe, şarap mahzeni | 4 garsona kadar personel, fedai, tam dekor modu. |
| 4 | **Maceracılar Hanı** | 21–27 | + Sahne, büyü köşesi | Büyülü içecekler, ozan sahnesi, gece döngüsü. |
| 5 | **Efsanevi Han** | 28–30 | + Kraliyet salonu | Ejder Ateşi, Gizli Kral, prestij (yeni şube). |

Her aşama geçişi, kilitli duvarın önündeki **büyük ödeme alanına** para dökülerek yapılır. Duvar yıkılır, yeni alan açılır. Bu, oyunun en tatmin edici anlarından biri olmalı (toz bulutu, konfeti, kamera kayması).

### 3.2.1 Harita (uygulanan)
```
 [ BAHÇE  x −11..−3 ]  [ MEYHANE  x −3..3 ]  [ ANA SALON  x 3..11 ]
 ───────────────────────── SOKAK (müşteriler iki uçtan gelir) ─────────────────────────
```
- Kilitli bölgelerin zemini ve dekoru **baştan görünür**. Oyuncu ne açacağını sürekli görür.
- Bölgeler arası duvar, ödeme karesine para dökülünce kalkar.
- Kamera geniş açılı başlar (mesafe 13, FOV 42). İki parmakla ya da fare tekerleğiyle 8–18 arasında yakınlaştırılır.

### 3.2.2 Hedef Zinciri (ödeme kareleri, sırayla)
Ekranın üstündeki **"Sıradaki Hedef"** kutusu ve zıplayan ok, her an bu zincirdeki bir sonraki adımı gösterir.

| # | Alan | Bedel | Seviye | Bölge |
|---|---|---|---|---|
| 1 | Elma Presi | 25 | 1 | Meyhane |
| 2 | Yeni Masa | 50 | 2 | Meyhane |
| 3 | Bal Kazanı | 150 | 3 | Meyhane |
| 4 | Yeni Masa | 110 | 3 | Meyhane |
| 5 | **Ana Salonu Aç** | 350 | 4 | → Salon |
| 6 | **Garson: Pip** | 200 | 4 | Salon |
| 7 | Uzun Masa (6 kişi) | 300 | 5 | Salon |
| 8 | Şarap Rafı | 420 | 5 | Salon |
| 9 | Süt Güğümü | 480 | 6 | Salon |
| 10 | Yeni Masa | 380 | 6 | Salon |
| 11 | Yeni Masa | 420 | 7 | Salon |
| 12 | **Garson: Tobi** | 900 | 7 | Salon |
| 13 | Koyu Bira Kazanı | 1.100 | 8 | Salon |
| 14 | **Bahçeyi Aç** | 1.600 | 8 | → Bahçe |
| 15–16 | Bahçe Masaları | 650 / 750 | 9 | Bahçe |
| 17 | Çaydanlık Ocağı | 1.500 | 10 | Bahçe |
| 18 | Bahçe Masası | 850 | 10 | Bahçe |
| 19 | Ozan Sahnesi (+20 Ambiyans) | 2.500 | 11 | Bahçe |

### 3.2.3 Oyuncuyu Tutan Döngüler (uygulanan)
| Süre | Döngü |
|---|---|
| Saniyeler | Servis → para yığını → paralar sayaca uçar (yükselen "şıngır" sesi, sayaç zıplar) |
| Dakikalar | Sıradaki Hedef + ok → ödeme karesi → konfeti + "Yeni Alan Açıldı!" |
| 5–7 dk | **Yoğun Saat** (45 sn, müşteri ×2, bahşiş ×2, boru sesi + kırmızı bant) |
| Seviye | Seviye atlama: büyük bildirim, yeni içecek ve müşteri listesi, konfeti |
| Gün | 3 günlük görev. Toplanan görevin yerine hemen yenisi gelir. Rozet bekleyen ödülü gösterir. |
| Dönüş | "Sen yokken…" penceresi: garsonların ürettiğinin %30'u, en fazla 2 saat |

### 3.2.4 Yönetim Katmanı (v0.4 — Tavern Master'dan esinlenen)
Oyuncu Barlo'yu yönetmeye devam eder. Bunun üstüne bir yönetim katmanı eklenir:

| Sistem | Nasıl çalışır |
|---|---|
| **Makineler** (Mağaza › Makineler) | Her içecek makinesi kendi seviyesinde açılır ve satın alınınca yerine kurulur. Bölgesi açık değilse "Önce Ana Salon/Bahçe" uyarısı çıkar. Ödeme kareleri artık sadece alan ve masa açmak için. |
| **Personel** (Mağaza › Personel) | Garson ×3 (Pip, Mina, Tobi), Barmen (tüm makineler +%40 hız), Temizlikçi (kirli masaları temizler), Ozan (herkesin sabrı +%25). Tek seferlik işe alma bedeli + **günlük maaş**. |
| **Dekor** (Mağaza › Dekor) | Saksı, mumluk, halı, flama, fıçı yığını, kalkan, geyik boynuzu, avize. Satın alınca uygun ilk boş yere yerleşir. Her biri Ambiyans puanı verir. |
| **Taverna puanı** (★1–5) | Ambiyans ve temizlik ile hesaplanır. Etkileri: müşteri gelme hızı, bahşiş (+%5 / yıldız) ve zengin müşterilerin gelme ağırlığı. |
| **Kirli masa** | Müşteri gidince masada bardak kalır. Kirli masaya yeni müşteri oturmaz. Barlo yanında durunca ya da temizlikçi gelince temizlenir. |
| **Gün döngüsü** | 1 gün = 5 dk. Gün sonunda gelir, maaş ve puan özeti gösterilir, maaşlar ödenir. |

### 3.3 Seviye Açılımları
| Sv | Açılan |
|---|---|
| 1 | Bira, Şıra · Çiftçi, Balıkçı · 3 masa |
| 2 | Oduncu · ilk istasyon yükseltmeleri |
| 3 | Bal Şarabı · Ozan |
| 4 | Fırıncı · **Dekor modu (temel)** |
| 5 | Kırmızı Şarap · Tüccar · Aşama 2 ödeme alanı belirir |
| 6 | Süt · Çoban Kız · **Pip garson olur** |
| 7 | Kunduracı · **Temizlikçi** |
| 8 | Kara Bira · Demirci |
| 9 | Muhafız · Yaver |
| 10 | Bitki Çayı · Keşiş · **Barmen** |
| 11 | Cüce Madenci · 2. garson |
| 12 | Buçukluk · 4 kişilik masalar |
| 13 | Vergi Memuru · Aşama 3 ödeme alanı |
| 14 | Goblin Grogu · Şövalye · Goblin |
| 15 | Ork · Avcı · **Fedai** |
| 16 | Büyücü · 3. garson |
| 17 | Cadı · **Tam dekor modu** (döndürme, duvar süsleri) |
| 18 | Elf Çiyi · Elf Okçu |
| 19 | Simyacı · 6 kişilik uzun masa |
| 20 | Kervancı (haftalık nadir dekor mağazası) · 4. garson |
| 21 | Korsan Kaptan · Aşama 4 ödeme alanı · **Ozan sahnesi** |
| 22 | Trol |
| 23 | **Gece döngüsü** (gece müşterileri) |
| 24 | Ejder Ateşi · Ejderha Avcısı |
| 25 | Vampir Kont |
| 26 | Hayalet Şövalye |
| 27 | 2. barmen · altın istasyon görünümleri |
| 28 | Gizli Kral · Aşama 5 ödeme alanı |
| 29 | Kraliyet Mührü bonusları |
| 30 | **Prestij: Yeni Şube** |

---

## 4. İçecek Üretimi

Her içeceğin bir **Üretim İstasyonu** vardır. İstasyon kendi kendine üretir ve hazır bardakları stokta tutar. Oyuncu ya da barmen istasyondan alır.

| İçecek | İstasyon | Taban fiyat | Taban süre |
|---|---|---|---|
| Bira | Bira Fıçısı | 3 | 2 sn |
| Şıra | Elma Presi | 4 | 3 sn |
| Bal Şarabı | Bal Kazanı | 7 | 4 sn |
| Kırmızı Şarap | Şarap Fıçısı Rafı | 10 | 5 sn |
| Süt | Süt Güğümü | 6 | 2 sn |
| Kara Bira | Koyu Bira Kazanı | 14 | 6 sn |
| Bitki Çayı | Çaydanlık Ocağı | 12 | 4 sn |
| Goblin Grogu | Goblin Kazanı | 22 | 6 sn |
| Elf Çiyi | Kristal Çeşme | 35 | 7 sn |
| Ejder Ateşi | Ejder Ocağı | 60 | 9 sn |

### 4.1 İstasyon Yükseltmeleri (3 eksen × 10 seviye)
- **Hız:** Üretim süresi her seviyede %10 azalır.
- **Stok:** 3 bardaktan başlar, seviye başına +1 artar (en fazla 12).
- **Kalite:** Satış fiyatı her seviyede %15 artar.
- **Maliyet:** `fiyat = taban × 1.18^seviye`. Taban: Hız 15, Stok 10, Kalite 25 (pahalı içeceklerde ×4 ve ×10).
- **Görsel kademe:** 4. ve 8. seviyede istasyonun modeli değişir: **Ahşap → Pirinç → Altın**. Oyuncu ilerlemesini tavernada görür.

### 4.2 Otomasyon
| Seviye | Kim yapar? |
|---|---|
| Başlangıç | Barlo her şeyi taşır |
| Sv6+ | **Garson** istasyondan alıp masaya götürür |
| Sv10+ | **Barmen** istasyonlardan alıp **servis tezgâhına** dizer, garsonlar oradan alır (zincir kısalır) |
| Her istasyon için | **"Otomatik Dolum"** yükseltmesi: Stok boşalınca beklemeden tekrar üretir (hız ×1.5) |

---

## 5. Personel

| Personel | Açılış | Ne yapar | Yükseltmeler |
|---|---|---|---|
| **Garson** | Sv6 (Pip) | İçecek taşır, para toplar | Hız, Taşıma (1→4 bardak) |
| **Temizlikçi** | Sv7 | Masaları temizler. Kirli masaya müşteri oturmaz. | Hız |
| **Barmen** | Sv10 | İstasyonlardan servis tezgâhına doldurur | Hız, Aynı anda istasyon sayısı |
| **Fedai** | Sv15 | Kavgacı müşterileri sakinleştirir (Ork, Goblin olayları) | Menzil |
| **Ozan** | Sv21 (sahne) | Sahnede çalar. Herkesin sabrı +%25. | Repertuvar (bahşiş bonusu) |

- **İşe alma:** Tek seferlik ödeme, oyuncu personel odasındaki ödeme karesine basar. Maaş yok (mobil için sade tutuyoruz).
- **Sayı sınırı:** Garson Aşama 2'de 1, Aşama 3'te 4, Aşama 4'te 6. Barmen en fazla 2. Diğerleri birer tane.
- **Yükseltme maliyeti:** `taban × 1.25^seviye`, her yükseltme için en fazla 10 seviye.

---

## 6. Genişletme ve Düzenleme

### 6.1 Genişletme
- Tavernanın zemini **1 m'lik karolardan** oluşan bir ızgaradır. Kilitli alanlar ip çitle ve **"Satılık" tabelasıyla** gösterilir.
- Küçük genişletmeler (birkaç karolık), aşamalar arasında da satın alınabilir. Böylece aşama geçişleri arasında da sürekli büyüme hissi olur.

### 6.2 Düzenleme Modu
- Ekrandaki **çekiç butonu** düzenleme moduna geçirir. Kamera yukarı kalkar, ızgara görünür.
- Mobilya ve dekor **sürükle-bırak** ile taşınır, **tek dokunuşla 90°** döner. Envanterden yenisi yerleştirilir.
- **Kurallar:** Kapıdan tezgâha bir yol açık kalmalı (yol hesaplaması). İstasyonlar duvara bitişik olmalı.

### 6.3 Ambiyans Puanı
Her dekor ve mobilyanın bir **Ambiyans** değeri vardır. Toplam Ambiyans şunları etkiler:
- **Müşteri gelme hızı:** +%0.5 / puan (en fazla +%50)
- **Sabır:** +%0.3 / puan
- **Özel müşteriler:** Belirli eşik puanlarda Vampir, Hayalet ve Gizli Kral gibi nadir müşterilerin gelme şansı açılır.

| Eşya grubu | Örnekler | Ambiyans |
|---|---|---|
| Oturma | Tabure 0 · Sandalye 1 · Minderli Koltuk 3 | 0–3 |
| Masa | Yuvarlak (2 kişi) · Kare (4 kişi) · Uzun (6 kişi) | 1–4 |
| Işık | Mum · Duvar Meşalesi · Avize | 2–6 |
| Duvar | Kalkan & Kılıç · Geyik Boynuzu · Flama | 2–5 |
| Zemin | Halı · Saksı · Fıçı Yığını | 1–4 |
| Büyük | Şömine · Ozan Sahnesi · Kraliyet Mührü | 8–20 |

---

## 7. Müşteri Sistemi

- **Gelme hızı:** `taban 1 müşteri / 6 sn × (1 + Ambiyans bonusu)`. Masa doluysa kapıda kısa süre bekler, sonra gider.
- **Müşteri karışımı:** Açık tiplerden ağırlıklı rastgele seçilir. Yeni açılan tip, ilk 3 dakika ×3 ağırlıkla gelir (yeniliği göstermek için).
- **Sabır:** ★1 = 15 sn, ★5 = 45 sn. Sabır halkası yeşil → sarı → kırmızı. Sarıdayken servis edilirse bahşiş yok.
- **Seviyeyle zorlaşma (v1.0):** Sv6'dan sonra müşteri aralığı seviye başına ×0.98 kısalır (en fazla ×1.25 kalabalık). Sv8'den sonra sabır seviye başına %1,5 kısalır (en az %85). Sv10+ müşterilerin %20'si, Sv18+ %25'i fazladan 1 sipariş verir. Barlo tek başına yetişemez; personel ve otomasyon gerekir.
- **Yoğun Saat (olay):** Her 5–7 dakikada bir, 45 sn boyunca müşteri gelişi ×2 ve bahşiş ×2. Zorluk zirvesi budur. Kaçırınca ceza yoktur, yakalayınca büyük kazanç vardır.
- **v1.2 gelme hızı:** taban aralık 5,2 sn (eskiden 6). Açık oturak sayısıyla çarpılır: `SeatSpawnScale = clamp(√(oturak / 12), 1, 2.2)` (40 oturak ≈ ×1.83). Ayrıca ÷ Mutluluk çarpanı, ÷ etkinlik çarpanı.

### 7.2 Mutluluk (v1.2)

Tek değer `M` (0–100, kayıtta; yeni oyunda 60). HUD'da altının altında yüzlü bar; dokununca açıklama.

| Olay | Etki |
|---|---|
| Servis, sabır ≥ %66 | M + 4 |
| Servis, sabır ≥ %33 | M + 2 |
| Servis, sabır < %33 | 0 |
| Kızgın giden müşteri / misafir | M − 8 |
| Her kirli masa | saniyede M − 0,12 |
| Boşta | saniyede 0,04 hızla 50'ye döner |

Müşteri gelişi çarpanı: `M < 50 → lerp(0.6, 1.0, M/50)`, `M ≥ 50 → lerp(1.0, 1.5, (M−50)/50)`.
★ yıldızlar artık sadece ambiyanstan gelir (`1 + ambiyans/30`, 1–5): zengin müşteri ağırlığı ve bahşiş. Kirli masa ve kızgın müşteri yıldızı değil mutluluğu düşürür.

### 7.3 Etkinlikler (v1.2)

Altınla başlar, 180 sn sürer, her biri günde bir kez, aynı anda tek etkinlik; gün bitince söner. Fiyat `taban + seviye × artış`.

| Etkinlik | Açılış | Fiyat | Etki |
|---|---|---|---|
| Ozan Konseri | Sv4 | 120 + 60·Sv | müşteri gelişi ×1.6, ozan sahneye çıkar |
| Bira Festivali | Sv5 | 150 + 80·Sv | satış fiyatı ×1.5 |
| Tüccar Kervanı | Sv6, Ana Salon | 200 + 100·Sv | zengin müşteri ×5, bahşiş ×2, gelme ×1.2 |
| Zar Turnuvası | Sv13, Oyun Salonu | 250 + 120·Sv | oyun masası ücreti ×2.5, tur ×1.4 hızlı |

### 7.4 Rehber v2 (v1.2)

9 adım: bira al → bardak dolsun → ilk müşteriye götür → parayı topla → **Görevler** butonu açılır → ödeme karesine para ver → **Dükkân** açılır → **Personel** açılır → **Depo** açılır. Rehber sürerken gün saati ve yoğun saat durur, ilk müşteri bardak dolunca gelir, en fazla 2 müşteri olur; elmas, yardımcı ve etkinlik butonu rehber bitince görünür. Her an atlanabilir.

### 7.6 Oyuncuya hep iş kalır (v1.3)

Oyuncu testi: "oyun kendi kendine oynuyor". Personel yardım eder, oyunu bitirmez.
- **Barlo'ya özel sipariş:** Sv3–9 yeni siparişlerin %18'i, Sv10+ %22'si (sadece zemin kat). Balon altın renkte ve büyük. Garsonlar almaz, yalnızca oyuncu servis eder. Ödeme ×2.5, mutluluk +3 (normal servis artışına ek), sabır ×1.4.
- **Para toplama oyuncunun işi:** garsonlar masadaki parayı toplamaz. Kasa otomasyonu Sv22'de 25.000 altına açılır (eskiden Sv8, 5.000).
- **Kovma:** Dükkân › Personel kartında KOV (iki dokunuşla onay). Kovulan görevli sahneden çıkar, maaşı ve rol bonusu durur, seviyesi sıfırlanır, yeniden işe alınabilir; reklamla geliştirme hakkı sıfırlanmaz. Hazine görevlisi (Lirael, Ignis) kovulmaz: İZNE GÖNDER / GERİ ÇAĞIR.

### 7.5 Masa planı ve tabela (v1.2)

Ön cephe sokağa doğru 3 m büyüdü (z −2 → −5), sokak geri çekildi. Masa yerleri 21 → 40: Meyhane +2 (Sv3–4), Ana Salon +6 (Sv5–7), Bahçe +5 (Sv8–10, bahçeyle birlikte satın alınabilir), Mahzen +2, Teras +1, Avlu +2, İskele +1. Tabela Sv1 / 5 / 10 / 15 / 20'de kademe atlar (tahta → boyalı → demir → yaldızlı → görkemli), yazı rengi de değişir.

---

## 8. Ekonomi ve Denge

### 8.1 Tempo Hedefleri (aktif oyun süresi)
| Hedef | Süre |
|---|---|
| İlk yükseltme | 30 sn |
| Aşama 2 | ~15 dk |
| Aşama 3 | ~1.5 saat |
| Aşama 4 | ~5 saat |
| Aşama 5 | ~12 saat |
| Sv30 / ilk prestij | ~20 saat |

### 8.2 Denge Kuralları
- **Gelir eğrisi, maliyet eğrisinin biraz gerisinde kalır:** Gelir seviye başına ~%12 artar, maliyet ~%18. Oyuncu sürekli "biraz daha" ister ama bunalmaz. Her aşama geçişi gelir eğrisini yukarı sıçratır (yeni içecek, yeni masa).
- **Bekleme sınırı:** Aktif oyunda bir sonraki anlamlı satın almaya hiçbir zaman **10 dakikadan uzun** bekletmiyoruz. Testlerde bu aşılırsa ilgili maliyet düşürülür.
- **Maliyetler (v1.0):** Maaş = taban × 1.15^(Sv − personelin açıldığı seviye) × (1 + 0.15 × görevli seviyesi). Maaş ödenemezse ertesi gün personel küskün (−%20 hız). Malzeme = kalite dahil satış fiyatının %30'u. İstasyon yükseltmelerinde Sv5 sonrası üs 1.24.
- **Çevrimdışı gelir:** Oyuncu yokken personelin ürettiği gelirin %30'u kazanılır. Sınır 2 saattir, yükseltmeyle 8 saate çıkar. Dönüşte bir "Sen yokken…" ekranı gösterilir.
- **Ödüllü reklam (isteğe bağlı):** 5 dk ×2 gelir, çevrimdışı ödülü ×3, ödeme alanında tek seferlik %25 indirim. **Zorunlu reklam yok.**
- **Elmas (premium para):** Görevlerden ve seviye ödüllerinden düşer. Kozmetik dekor ve zaman hızlandırma için kullanılır. Satın alınabilir ama ilerlemek için **gerekmez**.

### 8.3 Görevler
- **3 günlük görev:** Örneğin "20 Bira sat", "Bir Cüce'yi mutlu et", "Yoğun Saati tamamla". Ödül para ve elmas.
- **Kilometre taşları:** "İlk 100 müşteri", "Tüm köy halkını ağırla" gibi. Kalıcı küçük bonuslar verir.
- **Koleksiyon defteri:** 30 müşteri tipinin her biri ilk kez ağırlandığında deftere işlenir. Hepsi tamamlanınca kalıcı +%10 bahşiş.

---

## 9. Prestij: Yeni Şube (Sv30)
- Oyuncu yeni bir yerde, sıfırdan yeni bir taverna açar: **Liman Kenti**, sonra **Dağ Geçidi**.
- Yeni haritada farklı görünüm ve 2–3 yeni müşteri tipi olur.
- **Kalıcı:** Kraliyet Mührü bonusları, koleksiyon defteri, kozmetik dekorlar.
- **Sıfırlanan:** Para, seviyeler, istasyonlar.
- Yeni şube daha hızlı ilerler (+%25 gelir, şube başına artar).
- *Lansman sonrası içerik. İlk sürümde sadece altyapısı ve "Yakında" ekranı olacak.*

---

## 10. Menüler ve Ekranlar

| Ekran | İçerik |
|---|---|
| **Ana Menü** | Oyun logosu + canlı taverna arka planı (Barlo ve Pip). Butonlar: **Oyna**, **Ayarlar**, **Çıkış** (iOS'ta Çıkış gizlenir) |
| **Ayarlar** | Müzik, Ses Efektleri, Titreşim, Dil (TR/EN), Grafik kalitesi (Düşük/Orta/Yüksek), Bildirimler, Kaydı Sıfırla, Gizlilik |
| **HUD (oyun içi)** | Üstte: Para · Elmas · Seviye çubuğu. Sağda: Görevler, Mağaza, Düzenleme (çekiç). Altta: Joystick alanı |
| **Yükseltme Paneli** | İstasyon ya da personele dokununca açılan parşömen panel. Hız / Stok / Kalite satırları, fiyat butonları |
| **Seviye Atladın** | Açılan içerik kartları + "Topla" butonu |
| **Sen Yokken…** | Çevrimdışı kazanç, "Topla" ve "×3 (reklam)" butonları |
| **Koleksiyon Defteri** | 30 müşteri kartı. Görülmeyenler siluet olarak durur. |

---

## 11. Varlık Listesi (üretilecek tüm tasarımlar)

| Grup | Varlıklar | Durum |
|---|---|---|
| Karakterler | Barlo, Pip, 30 müşteri | ✅ |
| Personel | Garson Mina, Garson Tobi, Barmen Gus, Temizlikçi Nana, Fedai Brak, Sahne Ozanı Lir | ✅ |
| İçecekler | 10 bardak (dolu) | ✅ |
| Üretim istasyonları | 10 istasyon × 3 görsel kademe (Ahşap / Pirinç / Altın) | ✅ |
| Mobilya | Tezgâh, Servis tezgâhı, 3 masa, 3 oturak, Bardak leğeni, Para sandığı | ✅ |
| Dekor | Şömine, Avize, Meşale, Mum, Saksı, Halı, Flama, Kalkan & Kılıç, Geyik Boynuzu, Fıçı Yığını, Ozan Sahnesi, Kraliyet Mührü | ✅ |
| Yapı | Zemin karosu, Duvar, Köşe, Kapı, Pencere, Kilitli alan çiti + Satılık tabelası, Ödeme karesi | ✅ |
| Oyun içi objeler | Altın para, para yığını, elmas | ✅ |
| UI | Ana menü, Ayarlar, HUD, Yükseltme paneli (`design/ui/ui_mockups.html`) + menü arka planı | ✅ |
| UI (sonra) | Seviye Atladın, Sen Yokken…, Koleksiyon Defteri, Mağaza, Düzenleme modu | ⬜ |
| Hareket | İskelet (BODY_S/M/L) + temel animasyon seti | ⬜ |
| Ses (v1.1) | 38 efekt (Kenney CC0 + katmanlama), 3 müzik (RandomMind CC0), 6 ses tipi × 14 oyuncak-dil konuşma, 5 ortam döngüsü (`tools/audio/build_audio.py`, önizleme `design/audio/ses_onizleme.html`) | ✅ |

### 11.1 Ses kuralları (v1.1)

- **Müzik:** (RandomMind, CC0 — `assets/audio/CREDITS.md`) gündüz `day` (Minstrel Dance); hava kararınca ve gün sonu ekranında `evening` (The Old Tower Inn); yoğun saatte `rush` (Market Day). Geçiş 2,5 sn çapraz; dönülen parça kaldığı yerden sürer. Büyük anlarda (seviye, yoğun saat, yeni alan, şafak, akşam) müzik efekt süresince %35'e iner.
- **Kalabalık:** ayakta/oturan müşteri sayısı `n` (çıkanlar hariç). Az uğultu = `clamp01(n/4)·0.32`, dolu uğultu = `clamp01((n−4)/10)·0.45` (yoğun saatte en az 0.18). Gün bitince ikisi de söner.
- **Gevezelik:** `n ≥ 2` iken her `U(1.0, 3.2) · lerp(1.4, 0.45, n/14)` saniyede içen bir müşteri konuşur (%22 kahkaha). Sipariş anında %30 “sipariş” sesi, serviste %45 (bahşişli) / %20 “sevinç”, kızgın çıkışta “söylenme”. Aynı anda en fazla 4 konuşma; ekran dışındakiler %35 sesle, ekrandaki yerine göre sağ/sol.
- **Ses tipleri:** kalın (Oduncu, Demirci, Muhafız, Cüce, Şövalye, Ork), kadın (Çoban Kız, Elf, Cadı, Simyacı), minik (Buçukluk, Goblin), yaşlı (Keşiş, Büyücü), kibar (Tüccar, Vergi Memuru), diğerleri erkek. Her tipin kendi sabit perdesi var (0,93–1,05).

