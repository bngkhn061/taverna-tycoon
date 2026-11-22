# Oyun Tasarımı — Taverna Tycoon (çalışma adı)

> Görsel kurallar: [DESIGN_RULES.md](DESIGN_RULES.md) · Karakterler: [CHARACTERS.md](CHARACTERS.md)
> Bu belge oyunun **nasıl oynandığını, nasıl ilerlediğini ve nasıl dengelendiğini** tanımlar.
> Sürüm: **v2.3** (2026-11-22, Paket 5 — sahibin seçtiği eklemeler: masa ve dekor düzenleme modu (Sv5; Meyhane, Ana Salon, Bahçe; ızgara, döndürme, geçersiz yerleşim kırmızı, kayıtta yerleşim) ve bulut kaydı altyapısı (sağlayıcı arayüzü, sahte bulut, imzalı bulut kaydı, çakışma kuralı ve seçim ekranı, otomatik eşitleme); ayrıntı §17).
> Önceki: v2.2 (2026-11-21, Paket 4 — sahibin seçtiği eklemeler: sezonluk etkinlikler (Hasat Şenliği, Kış Festivali, Bahar Bayramı; süreli menü, süsleme, müşteri, sezon jetonu, görevler, kalıcı sezon dekoru) ve Barlo ile personelin kostümleri (Gardırop); ayrıntı §16).
> Önceki: v2.1 (2026-11-21, oyuncu testi Paket 3, maddeler 10 ve 25: Barlo'nun kendi yetenekleri — koşu hızı, taşıma, servis ve para menzili; gerçek bir son — 3. tavernada "Krala Davet", son sahne, jenerik, serbest oyun ve "Efsanevi Han" rozeti; ayrıntı §15).
> Önceki: v2.0 (2026-11-17, oyuncu testi Paket 2, maddeler 14, 15, 17–21: kademe avizeleri kalktı ve avizeler küçüldü, gün sonu kapanışı ve personelin işe gidip gelmesi, bahçe uzun masası, sahnede çalan ozan, bahçe feneri ve taş yol, han konfor seviyeleri, 2 yeni oda ve 2 yeni oyun masası; ayrıntı §14).
> Önceki: v1.9 (2026-11-17, oyuncu testi Paket 1, `production/qa/playtests/feedback-2026-11-17.md`: yükseltme satırları, ses karışımı, hedef kutusu, salon garsonları, zorunlu rehber, durum görevleri, açıklamalar, seviye kartında açılanlar ve "YENİ!" rozetleri, maaş borcu, elmaslı otomasyon, elmas ekonomisi).
> Önceki: v1.8 (2026-11-16, son denge geçişi + cila: ölçülmüş Ün tablosu, müşteri / sabır / içme süresi, maaş, etkinlik fiyatı, özel sipariş, Kral Şerbeti, prestij Ün'ü; `design/balance/balance-check-final-2026-11-12.md`).
> Önceki: v1.5 (2026-11-11, Paket C: oyun sonu — Sv27 Kral Şerbeti, Sv28 Kraliyet Ziyareti etkinliği, Sv29 Şölen Tabağı, Sv30 prestij "Yeni Şube").
> Önceki: v1.4 (2026-11-10, Paket A: rehber zamanı, mutluluk ortalaması, yıldız eşikleri, işe alım ücreti, tellal etkinliği, müşteri çarpanı sınırı, özel sipariş görevleri).

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
- Gereken Ün (v1.8, ölçülmüş tablo `Balance.UnTable`): Sv1–10: 12, 16, 25, 35, 50, 75, 95, 125, 155, 170 · Sv11–20: 190, 205, 230, 240, 260, 290, 320, 350, 380, 410 · Sv21–30: 460, 480, 490, 500, 510, 520, 530, 540, 550, 560. Her değer = o seviyedeki iyi oyuncunun (bot) Ün/dk hızı × hedef insan süresi ÷ 1.3. Hedef insan süresi: Sv1–5 2.4–3.8 dk, Sv6–12 5–8.6 dk, Sv13–20 9.4–13.4 dk, Sv21–29 14.2–19 dk; ilk şube ≈ 5 sa. (Eski üstel eğri Hazine'li botla ölçülmüştü; Hazine'siz oyuncuda Sv15 seviyesi ~60 dk sürüyordu.)
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
| **Dekor** (Mağaza › Dekor) | Saksı, mumluk, halı, flama, fıçı yığını, kalkan, geyik boynuzu, avize, taş yol ve bahçe feneri (v2.0, bahçe). Satın alınca uygun ilk boş yere yerleşir. Her biri Ambiyans puanı verir. |
| **Taverna puanı** (★1–5) | v1.4: yalnızca ambiyanstan, artan eşiklerle (§7.2). Eski metin: ambiyans ve temizlik ile hesaplanır. Etkileri: müşteri gelme hızı, bahşiş (+%5 / yıldız) ve zengin müşterilerin gelme ağırlığı. |
| **Kirli masa** | Müşteri gidince masada bardak kalır. Kirli masaya yeni müşteri oturmaz. Barlo yanında durunca ya da temizlikçi gelince temizlenir. |
| **Gün döngüsü** | 1 gün = 5 dk. Gün sonunda gelir, maaş ve puan özeti gösterilir, maaşlar ödenir. |

### 3.3 Seviye Açılımları
> **v1.9 seviye kartı:** "Seviye atladın" kartı o seviyede açılan her şeyi ikonuyla listeler: ödül elması, makineler, personel, alanlar (aynı adlı kareler tek kartta, ör. "Yeni Masa ×2"), dekor ve temalar, etkinlikler, otomasyon (altın / elmas seviyesi ayrı), müşteri tipleri, kademe ve prestij şartı. 4'ten fazlaysa kartlar küçülüp iki satıra iner, en fazla 10 (fazlası "+N"). Dükkânda yeni açılmış, henüz görülmemiş ürünlerde ve sekmelerinde **"YENİ!"** rozeti; sekme açılınca ürünler görüldü sayılır (kayıtta `seenItems`, eski kayıtta o anki seviyeye kadar her şey görülmüş sayılır).

| Sv | Açılan |
|---|---|
| 1 | Bira, Şıra · Çiftçi, Balıkçı · 3 masa |
| 2 | Oduncu · ilk istasyon yükseltmeleri |
| 3 | Bal Şarabı · Ozan |
| 4 | Fırıncı · **Dekor modu (temel)** |
| 5 | Kırmızı Şarap · Tüccar · Aşama 2 ödeme alanı belirir · **Düzenleme modu** (v2.3, §17) |
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
| **Kral Şerbeti** (v1.5, Sv27, Ana Salon, makine 14.000) | Şerbet Çeşmesi (mermer kaide, altın havuz, taç) | 95 | 13 sn |
| **Şölen Tabağı** (v1.5, Sv29, Mutfak, yemek, makine 16.000) | Şölen Ocağı (şişte kuzu) | 110 | 15 sn |

v1.5 eğrisi: Kral Şerbeti en pahalı içecekten (Yıllanmış Şarap 80 / 12 sn) bir kademe yukarı; Şölen Tabağı en pahalı yemekten (Izgara Balık 50) ve en yavaş yemekten (Elmalı Turta 14 sn) bir kademe yukarı. Makine bedelleri en pahalı makineden (12.000) 2.000'er artar. Kral Şerbeti'ni yalnızca zengin müşteriler ister: Ejderha Avcısı, Vampir Kont, Gizli Kral. Şölen Tabağı'nı mutfak açıkken her müşteri yemek siparişi olarak isteyebilir (%35). Mutfakta beş ocak arka duvarda 1.5 m aralıkla durur (x −3, −1.5, 0, 1.5, 3; z 9.45); Şerbet Çeşmesi Salon arka duvarının sol ucunda (x 5.05), Salon meşalesi Süt ile oyun salonu kapısı arasına, oradaki saksı yeri salonun ön sağ köşesine taşındı. Tüm içecek ve yemekler `Editor/TavernaData.cs`'te.

### 4.1 İstasyon Yükseltmeleri (3 eksen × 10 seviye)
- **Hız:** Üretim süresi her seviyede %10 azalır.
- **Stok:** 3 bardaktan başlar, seviye başına +1 artar (v1.9: en fazla 12 sınırı kalktı → Sv10'da 13; eskiden Sv9 → Sv10 "12 → 12").
- **Kalite:** Satış fiyatı her seviyede %15 artar; v1.9: her seviye en az +1 altın (ucuz içeceklerde yuvarlama "3 → 3" gösteriyordu; Bira 3, 4, 5 … 13).
- **v1.9 yükseltme paneli:** her satır önce → sonra ve ne işe yaradığını yazar (Hız: "bir bardak x sn'de · dakikada y bardak", Stok: "makinede hazır bekleyen bardak", Kalite: "her bardağın satış fiyatı"). Birim testi (`V19Tests.UpgradeRows_EveryLevelChangesShownValue`) bütün içeceklerde hiçbir satırın önce = sonra göstermediğini denetler.
- **Maliyet:** `fiyat = taban × 1.18^seviye`. Taban: Hız 15, Stok 10, Kalite 25 (pahalı içeceklerde ×4 ve ×10).
- **Görsel kademe:** 4. ve 8. seviyede istasyonun modeli değişir: **Ahşap → Pirinç → Altın**. Oyuncu ilerlemesini tavernada görür.

### 4.2 Otomasyon
> **v1.9 (Dükkân › Otomasyon, `Automation.cs`):** her otomasyon altınla (eski seviye şartı) ya da elmasla (daha erken) alınır; elmas fiyatı erken almayı lüks yapacak kadar yüksek. Han ve Oyun salonu bölgesi açılmadan alınamaz.
>
> | Otomasyon | Altın | Elmas |
> |---|---|---|
> | Kasa (para kendiliğinden toplanır) | 25.000 · Sv22 | 150 · Sv16 |
> | Depo (otomatik sipariş) | 900 · Sv7 | 30 · Sv3 |
> | Han resepsiyonu | 6.000 · Sv12 | 80 · Sv8 (Han açılınca) |
> | Oyun salonu | 8.000 · Sv14 | 100 · Sv9 (Oyun Salonu açılınca) |
>
> Usta geliştirmeleri değişmedi (yalnızca altın). Aşağıdaki tablo v0.x taslağıdır.

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
- **v1.9 bölge kuralı:** Mina ve Tobi salon garsonu (`Catalog.Staff.zone = "salon"`, başlangıç yerleri salonda): Ana Salon açılmadan işe alınamaz, alınmışsa salon açılana kadar sahnede yok ve maaş almaz (aşçı / hizmetçi kuralı). Diğer görevlilerin başlangıç yerleri meyhanede (Pip, Gus, Nana, Brak, Lir, Lirael), mutfakta (Ayla, Ignis, bölge "mutfak") ve handa (Sema, "merdiven"); denetlendi.
- **Sayı sınırı:** Garson Aşama 2'de 1, Aşama 3'te 4, Aşama 4'te 6. Barmen en fazla 2. Diğerleri birer tane.
- **Yükseltme maliyeti:** `taban × 1.25^seviye`, her yükseltme için en fazla 10 seviye.

---

## 6. Genişletme ve Düzenleme

### 6.1 Genişletme
- Tavernanın zemini **1 m'lik karolardan** oluşan bir ızgaradır. Kilitli alanlar ip çitle ve **"Satılık" tabelasıyla** gösterilir.
- Küçük genişletmeler (birkaç karolık), aşamalar arasında da satın alınabilir. Böylece aşama geçişleri arasında da sürekli büyüme hissi olur.

### 6.2 Düzenleme Modu
> **v2.3:** uygulanan biçim §17'dedir (Sv5, "DÜZENLE" düğmesi, 0.25 m ızgara, 90° döndürme). Aşağıdaki ilk taslağın envanterden yerleştirme ve duvar süsü maddeleri uygulanmadı.
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
- **Sabır:** ★1 = 20 sn, ★5 = 50 sn (v1.8; eskiden 15 / 45). Sabır halkası yeşil → sarı → kırmızı. Sarıdayken servis edilirse bahşiş yok.
- **Seviyeyle zorlaşma (v1.0, v1.8 sayıları):** Sv1–5 aralık ×1.1 (v1.8, eskiden ×1.25: biraz daha çok müşteri). Sv6'dan sonra müşteri aralığı seviye başına ×0.98 kısalır (en fazla ×1.15 kalabalık, v1.8; eskiden ×1.25). Sv8'den sonra sabır seviye başına %1,5 kısalır (en az %90, v1.8; eskiden %85). Sv10+ müşterilerin %20'si, Sv18+ %25'i fazladan 1 sipariş verir. Barlo tek başına yetişemez; personel ve otomasyon gerekir.
- **Yoğun Saat (olay):** Her 5–7 dakikada bir, 45 sn boyunca müşteri gelişi ×2 ve bahşiş ×2. Zorluk zirvesi budur. Kaçırınca ceza yoktur, yakalayınca büyük kazanç vardır.
- **v1.2 gelme hızı:** taban aralık 5,2 sn (eskiden 6). Açık oturak sayısıyla çarpılır: `SeatSpawnScale = clamp(√(oturak / 12), 1, 1.7)` (v1.8: üst sınır 2.2 → 1.7; garson sayısı sabit, yol uzuyor).
- **v1.8 içme süresi ve garson:** müşteri içkisini seviyeyle uzayan sürede içer: `lerp(7, 22, (Sv−1)/19)` sn (eskiden sabit 3). Masalar sipariş hızını artırmadan daha dolu görünür. Garsonlar ×1.3 hızlı yürür (`Balance.WaiterSpeedScale`). Makinelerin taban üretim süresi ×2 (`ProduceTimeScale`): yemek makineleri yoğunlukta boşalır, hız yükseltmesi ve aşçı anlam kazanır.
- **v1.8 Kral Şerbeti:** çeşme aynı kattaysa her içecek siparişinin %12'si Kral Şerbeti olur (`SignatureOrderChance`; eskiden yalnızca 3 nadir zengin müşteri istiyordu, ölçüm %1.5 → %8–12).
- **v1.4 müşteri çarpanı sınırı:** geçici / kazanılan çarpanların çarpımı `mutluluk × etkinlik × yoğun saat (×2.2) × Usta` en fazla **×3.0** (`Balance.SpawnBoostCap`). Oturak ve seviye ölçeği bu sınırın dışında. Aralık = `5,2 × seviye ölçeği × rastgele(0.75–1.25) ÷ min(3, çarpım) ÷ SeatSpawnScale`. Mutluluk kartında "Toplam müşteri çarpanı ×N (en fazla ×3)".

### 7.2 Mutluluk (v1.4)

`M` = son müşterilerin memnuniyetinin **üstel hareketli ortalaması** (0–100, kayıtta; yeni oyunda 60). Her sonuçlanan müşteri bir puan getirir ve `M ← M + α·(puan − M)`, **α = 0.08** (yaklaşık son 12 müşteri ağırlıklı).

| Sonuç | Puan |
|---|---|
| Servis, sabır ≥ %66 | 100 |
| Servis, sabır ≥ %33 | 65 |
| Servis, sabır < %33 | 35 |
| Kızgın giden müşteri / han misafiri | 0 |
| Oyuncunun servis ettiği "Barlo'ya özel" sipariş | 100, **iki kez** sayılır |

Gösterilen / etkin değer `E = clamp(M − min(20, 4 × kirli masa), 0, 100)`: kirli masa ortalamayı bozmaz, yalnızca etkin değeri düşürür. Boşta dönüş yok. HUD barı ve uçan "+N mutluluk" yazısı E'nin değişimini gösterir (1'in altı tek ondalık).
Müşteri gelişi çarpanı (E'den): `E < 50 → lerp(0.6, 1.0, E/50)`, `E ≥ 50 → lerp(1.0, 1.5, (E−50)/50)`.
Ölçüm (bot, 2026-11-10): yeni oyunda %59–99, Sv9 orta oyunda etkin %58–97 arasında dolaştı (eski modelde hep %93–100).

**★ yıldızlar (v1.4)** yalnızca ambiyanstan gelir, artan eşikli ölçek (`Balance.StarThresholds`). Hazine olmadan elde edilebilecek toplam ambiyans **436** (v2.0: **487**, §14) (masalar 217, dekor yerleri 127, ozan sahnesi 20, en iyi zemin + duvar 12, Saray Hanı kademesi 60).

| ★ | Ambiyans | Premium'suz toplamın |
|---|---|---|
| 1 | 0 | — |
| 2 | 35 | %8 |
| 3 | 110 | %25 |
| 4 | 220 | %50 |
| 5 | 350 | %80 |

Aradaki yarım yıldızlar doğrusal (aşağı yuvarlanır). Dükkân › Dekor kartları "+N ambiyans · ★3'e 23", not satırı "sonraki yıldıza N ambiyans" gösterir. Yıldız zengin müşteri ağırlığı ve bahşiş getirir; kirli masa ve kızgın müşteri yıldızı değil mutluluğu düşürür.

### 7.3 Etkinlikler (v1.2, Tellal v1.4)

Altınla başlar, 180 sn sürer, her biri günde bir kez, aynı anda tek etkinlik; gün bitince söner. Fiyat (v1.8) `pay × o seviyenin 3 dakikalık geliri`, gelir dayanağı `660 × 1.135^(Sv−1)` (`Balance.EventIncomeRef`); pay ölçülen ek gelirin yaklaşık yarısıdır: Tellal 0.07, Konser 0.45, Festival 0.25, Kervan 0.25, Turnuva 0.07, Kraliyet 5 (Sv28 ≈ 101.000). Tablodaki eski fiyat sütunu v1.7'ye kadar geçerliydi. Etkinlik butonu **Sv2'de** (rehber bittikten sonra) görünür.

| Etkinlik | Açılış | Fiyat | Etki |
|---|---|---|---|
| Tellal (v1.4) | Sv2 | 40 + 30·Sv | müşteri gelişi ×1.3, sürerken sokakta zil çalan tellal görünür |
| Ozan Konseri | Sv4 | 120 + 60·Sv | müşteri gelişi ×1.6, ozan sahneye çıkar |
| Bira Festivali | Sv5 | 150 + 80·Sv | satış fiyatı ×1.5 |
| Tüccar Kervanı | Sv6, Ana Salon | 200 + 100·Sv | zengin müşteri ×5, bahşiş ×2, gelme ×1.2 |
| Zar Turnuvası | Sv13, Oyun Salonu | 250 + 120·Sv | oyun masası ücreti ×2.5, tur ×1.4 hızlı |
| **Kraliyet Ziyareti** (v1.5) | Sv28, Ana Salon | 2.000 + 500·Sv (Sv28 16.000, Sv30 17.000) | tüm satış fiyatları ×2, bahşiş ×2, Gizli Kral'ın gelme ağırlığı ×40 (taban 0.05 → 2) |

v1.5: çarpanlar etkinlik başına tanımlı (`TavernEvents.PriceMultiplier / TipMultiplier / CustomerWeight`); aynı anda tek etkinlik kuralı sayesinde Kraliyet Ziyareti Festival'in fiyatıyla ya da Kervan'ın bahşişiyle birleşmez. Ölçüm (bot, Sv29): Kral Şerbeti fiyatı 284 → 567; Gizli Kral etkinlikten önceki 60 oyun-sn'de 0, etkinlikteki 120 oyun-sn'de 8 kez geldi.

v1.4: parayla "Tellal tut" (Dükkân › Hızlandır) kaldırıldı; şeridi de etkinlik şeridi oldu. Reklamla gelen yardımcı garson aynen kalır.

### 7.4 Rehber v2 (v1.2)

9 adım: bira al → bardak dolsun → ilk müşteriye götür → parayı topla → **Görevler** butonu açılır → ödeme karesine para ver → **Dükkân** açılır → **Personel** açılır → **Depo** açılır. **v1.4:** gün saati ve yoğun saat yalnızca ilk dört adımda (bira → bardak → servis → para) durur, Görevler adımından itibaren oyun akar. İlk müşteri bardak dolunca gelir, rehberin ilk dört adımında en fazla 2 müşteri olur; elmas, yardımcı ve etkinlik butonu rehber bitince görünür.

**v1.9 zorunlu rehber (oyuncu testi madde 11):** "Atla" düğmesi yok, buton adımları kendiliğinden geçmez (v1.4'ün 25 sn kuralı kalktı). Rehber sürerken yalnızca gösterilen HUD düğmesi ve (Personel adımında) dükkânın Personel sekmesi dokunuşu alır; diğer HUD düğmeleri, yıldızlar ve dükkân sekmeleri soluk ve kilitli ("Önce rehberdeki adımı yap"). Panel kapatma ve ayarlar (ses, dil) açık. Dükkân açıkken halka içindeki karşılığı gösterir (Personel sekmesi / kapat). Buton adımlarında zaman akar ama yeni müşteri gelmez, içerdekiler oturup gider. Ölçüm: `tutorial_skip` artık gönderilmez, `tutorial_step` olayından `kendiliginden` alanı kalktı.

### 7.6 Oyuncuya hep iş kalır (v1.3)

Oyuncu testi: "oyun kendi kendine oynuyor". Personel yardım eder, oyunu bitirmez.
- **Barlo'ya özel sipariş:** Sv3–9 yeni siparişlerin %10'u, Sv10+ %7'si (v1.8; eskiden %18 / %22) (sadece zemin kat). Balon altın renkte ve büyük. Garsonlar almaz, yalnızca oyuncu servis eder. Ödeme ×2.5, mutluluğa 100 puanla iki kez sayılır (v1.4, §7.2), sabır ×2 (v1.8; eskiden ×1.4).
- **Özel sipariş görevleri (v1.4):** Sv3'ten itibaren günlük görev havuzunda "5 özel sipariş yetiştir" (normal görev ödülü: `25 × (1 + 0.35·Sv)²` altın, `1 + Sv/4` elmas) ve "12 özel sipariş yetiştir" (iki kat altın, +1 elmas). Aynı anda tek özel sipariş görevi. Sayaç yalnızca oyuncunun servis ettiği özel siparişte artar.
- **Para toplama oyuncunun işi:** garsonlar masadaki parayı toplamaz. Kasa otomasyonu Sv22'de 25.000 altına açılır (eskiden Sv8, 5.000).
- **Kovma:** Dükkân › Personel kartında KOV (iki dokunuşla onay). Kovulan görevli sahneden çıkar, maaşı ve rol bonusu durur, seviyesi sıfırlanır, yeniden işe alınabilir; reklamla geliştirme hakkı sıfırlanmaz. Hazine görevlisi (Lirael, Ignis) kovulmaz: İZNE GÖNDER / GERİ ÇAĞIR.
- **İşe alım ücreti (v1.4):** `max(katalogdaki ücret, 2 × görevlinin şu anki günlük maaşı)` (`Management.HireCost`). Maaş seviyeyle büyüdüğü için akşam kovup sabah geri almak maaştan kâr ettirmez (ücret her zaman bir günlük maaşın en az iki katı). Örnek: Nana Sv9'da 80, Sv20'de 298, Sv30'da 1.202; Pip Sv20'de 388.

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
- **Maliyetler (v1.0, v1.8 sayıları):** Maaş = taban × 2 × 1.12^(Sv − personelin açıldığı seviye) × (1 + 0.15 × görevli seviyesi) (v1.8: ×2 ve üs 1.15 → 1.12; hedef maaş / gelir Sv8–30 %15–35). Usta: 6.000 / 14.000 / 30.000 (v1.8). Yıldız başına bahşiş +%10 (v1.8, eskiden %5); dekor fiyatları v1.8'de düştü (flama 80, fıçı 80, kalkan 150, boynuz 170, avize 250, mühür 1.500). Maaş ödenemezse ertesi gün personel küskün (−%20 hız). Malzeme = kalite dahil satış fiyatının %30'u. İstasyon yükseltmelerinde Sv5 sonrası üs 1.24.
- **Çevrimdışı gelir:** Oyuncu yokken personelin ürettiği gelirin %30'u kazanılır. Sınır 2 saattir, yükseltmeyle 8 saate çıkar. Dönüşte bir "Sen yokken…" ekranı gösterilir.
- **Ödüllü reklam (isteğe bağlı):** 5 dk ×2 gelir, çevrimdışı ödülü ×3, ödeme alanında tek seferlik %25 indirim. **Zorunlu reklam yok.**
- **Elmas (premium para):** Görevlerden ve seviye ödüllerinden düşer. Kozmetik dekor ve zaman hızlandırma için kullanılır. Satın alınabilir ama ilerlemek için **gerekmez**.
- **v1.9 elmas ekonomisi (oyuncu testi madde 13, 23 "başlangıç elmasıyla oyun çabuk bitiyor"):** v1.8'de ilk saatte (≈ Sv11) ≈ 50 elmas toplanıyordu (seviye 2 + Sv/5, görev 1 + Sv/4) ve ×2 gelir 10 elmastı: ilk saatin neredeyse tamamı ×2 geçebiliyordu. v1.9:

  | Kalem | v1.8 | v1.9 |
  |---|---|---|
  | Seviye ödülü | 2 + Sv/5 | 1 + Sv/10 |
  | Görev ödülü | 1 + Sv/4 | 1 + Sv/8 (büyük özel görev +1) |
  | Günlük ödül 7. gün | 10 | 8 (1–6. gün aynı: 1–4) |
  | ×2 Gelir (10 dk) | 10 | 30 |
  | Anında Dolum | 5 | 15 |
  | Yoğun Saati Başlat | 8 | 20 |
  | Altın Kesesi | 15 | 40 |
  | "Sen yokken" ×2 | 5 | 12 |
  | Ad değiştir | — | 20 (altın 500 ya da 2 reklam da var) |

  İlk saat ≈ 25 elmas (seviye 12 + görev ≈ 12 + günlük 1): en fazla bir ×2 gelir. Başlangıç elması 0 (değişmedi). Hazine elması (gerçek para) değişmedi.
- **v1.9 maaş borcu (oyuncu testi madde 7):** gün sonunda maaş her durumda düşer (`GameState.ForceSpend`); altın yetmezse bakiye eksiye iner. Borçluyken satın alma ve ödeme kareleri kapalı ("Önce borcu öde": dükkân mesajı, kare etiketi, hedef kutusu), bakiye kırmızı. Malzeme siparişi borca yazılabilir (makineler durup oyun kilitlenmesin; malzeme satışın %30'u). Kazanılan altın önce borcu kapatır. Maaş tamamı ödenemediyse personel ertesi gün küskün (değişmedi).

### 8.3 Görevler
> **v1.9 durum görevleri (oyuncu testi madde 24):** "Yeni bir alan aç" sayacının yerine belirli hedefli görevler: **İşe al: X**, **Makine al: X**, **Alanı aç: X** (seviyesi ve bölgesi uygun, henüz yapılmamış adaylardan). Bunlar oyunun durumuna bakar: görev oluşurken, oyun açılırken ve her satın alma / açılışta koşul tutuyorsa görev tamamlanmış (ödül alınabilir) olur. Sayaç görevleri (X bira sat, X müşteri, X altın, X yükseltme, yoğun saat, özel sipariş) sıfırdan sayar.

- **3 günlük görev:** Örneğin "20 Bira sat", "Bir Cüce'yi mutlu et", "Yoğun Saati tamamla". Ödül para ve elmas.
- **Kilometre taşları:** "İlk 100 müşteri", "Tüm köy halkını ağırla" gibi. Kalıcı küçük bonuslar verir.
- **Koleksiyon defteri:** 30 müşteri tipinin her biri ilk kez ağırlandığında deftere işlenir. Hepsi tamamlanınca kalıcı +%10 bahşiş.

---

## 9. Prestij: Yeni Şube (Sv30) — v1.5 uygulandı

Oyunun sonu (Sv30) artık bir döngüye bağlanır: oyuncu tavernasını bırakıp sıfırdan yeni bir şube açar ve kalıcı bir hız kazanır. Kod: `Scripts/Core/Prestige.cs`, arayüz `Scripts/UI/HudPrestige.cs`.

**Şartlar** (üçü birden): **Seviye 30** · **Saray Hanı** kademesi (en üst kademe, `Tiers`) · kasada **150.000 altın** (`Prestige.RequiredCoins`; harcanır, zaten sıfırlanır).

**Akış:**
- Sv30'a gelen oyuncunun "Sıradaki Hedef" kutusu (açılabilir alan hedefi yokken) prestij şartlarından eksik olanı gösterir: kademe eksikse "Yeni Şube için: Saray Hanı kademesi" ve kademe bedeli, altın eksikse "Yeni Şube: Liman Kenti · 89,4B / 150B", hepsi tamamsa "Yeni Şube hazır! Büyüt karesine git". Ok "Tavernayı Büyüt" karesini gösterir.
- Kademeler bitince aynı kare (Meyhane ön koridoru) **Yeni Şube kartını** açar. Kartta: önce / sonra (şimdiki şube adı ve seviye → yeni şube adı · Sv1), şart çipleri, **KALIR** ve **SIFIRLANIR** sütunları, kazanımlar, "SONRA" ve "150B · ŞUBE AÇ" düğmeleri.
- **İki dokunuşla onay:** ilk dokunuş düğmeyi kırmızı "EMİN MİSİN? ONAYLA"ya çevirir (not: "Son dokunuş: şube açılır, geri dönüşü yok"); ikinci dokunuş şubeyi açar. Şart tutmazken düğme kilitli görünür, dokunuş "Şartlar henüz tamam değil" der. Kart yeniden açılırsa onay sıfırlanır.
- Şube açılınca kayıt sıfırlanır, oyun sahnesi **yeniden yüklenir**; "YENİ ŞUBE · LİMAN KENTİ" şeridi ve konfeti. Rehber tekrar başlamaz (rehber görüldü bilgisi kalır).

| Sıfırlanır | Kalır |
|---|---|
| Altın (yeni şube `500 × şube no` ile başlar), seviye ve Ün, gün sayısı (1) | Elmas |
| Açılan alanlar, masalar, han odaları, makineler | Hazine ürünleri (`premium`), izindeki Hazine görevlileri (`benched`), VIP |
| Makine yükseltmeleri ve depo (raflar, teslimat, tedarikçi anlaşması) | Hazine görevlileri (Lirael, Ignis) ve seviyeleri |
| Personel: işe alımlar ve seviyeleri | Koleksiyon defteri (görülen müşteriler) |
| Otomasyon ve Usta | Taverna adı, ayarlar, rehber görüldü bilgisi |
| Barlo'nun yetenekleri (hız, taşıma, menzil; v2.1, §15.1) | Son sahne görüldü bilgisi ve "Efsanevi Han" rozeti (v2.1, §15.2) |
| Taverna kademesi (Ahşap Meyhane) | Reklam hakkı sayaçları (ad değiştirme, görevli reklam seviyeleri) |
| Mutluluk (%60'a döner) | Zemin / duvar temaları (satın alınmış ve seçili) |
| Günlük etkinlik kullanımı, maaş küskünlüğü | Satın alınmış dekorlar: kayıtta kalır, **bölgesi yeniden açılınca** yerine gelir (o yer başka dekora verilmez) |
| Günlük görevler (yeni şubenin seviyesine göre yeniden üretilir) | Günlük ödül serisi, toplam servis sayacı |

**Kazanım** (kayıtta `branch`, 0 = ilk taverna):
- Kalıcı gelir **+%25 / şube** (`Prestige.IncomeMultiplier = 1 + 0.25 × şube`), `Boosts.IncomeMultiplier` zincirine girer (servis ödemesi, han, oyun masaları).
- Ün kazanımı **+%35 / şube** (v1.8, eskiden %15; hedef ikinci şube %25–35 kısa) (`Prestige.UnMultiplier = 1 + 0.35 × şube`, `GameState.AddUn`'da; küsurat sonraki kazanca taşınır).
- Başlangıç altını `500 × şube no` (Liman Kenti 500, Dağ Geçidi 1.000).

**Şube adları:** 0 Köy Meyhanesi · 1 Liman Kenti · 2 Dağ Geçidi · sonrası "N. Şube". HUD'da seviye kalkanının solunda küçük sarı şube rozeti (şube numarası, şube ≥ 1 iken); gün başı ve giriş şeridinde şube adı ("GÜN 3 · LİMAN KENTİ").

**Harita aynı kalır.** v1.5'te her şube aynı haritada kurulur; ayrı görünümlü yeni şube haritaları ve yeni müşteri tipleri **lansman sonrası** içeriktir.

Ölçüm (bot, 2026-11-11): şube 1'de yükleme sonrası 60 gerçek saniyede 36 servis, rehber açılmadı; 600 oyun-sn'de Sv5'e çıktı. "×2 GELİR" şeridi artık yalnızca elmasla alınan 10 dakikalık hızlandırmada görünür (kalıcı çarpanlar — Usta, VIP, şube — şerit açmaz).

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
| İçecekler | 10 bardak (dolu) · v1.5: Kral Şerbeti (altın taçlı kadeh), Şölen Tabağı (kızarmış but, üzüm, ekmek) + ikonları (`tools/blender/build_sections3.py`) | ✅ |
| Üretim istasyonları | 10 istasyon × 3 görsel kademe (Ahşap / Pirinç / Altın) · v1.5: Şerbet Çeşmesi ve Şölen Ocağı × 3 kademe (`PR_Station_Serbet_T1..3`, `PR_Station_Solen_T1..3`, önizleme `assets/renders/Props_Sections3.png`) | ✅ |
| Mobilya | Tezgâh, Servis tezgâhı, 3 masa, 3 oturak, Bardak leğeni, Para sandığı | ✅ |
| Dekor | Şömine, Avize, Meşale, Mum, Saksı, Halı, Flama, Kalkan & Kılıç, Geyik Boynuzu, Fıçı Yığını, Ozan Sahnesi, Kraliyet Mührü | ✅ |
| Yapı | Zemin karosu, Duvar, Köşe, Kapı, Pencere, Kilitli alan çiti + Satılık tabelası, Ödeme karesi | ✅ |
| Oyun içi objeler | Altın para, para yığını, elmas | ✅ |
| UI | Ana menü, Ayarlar, HUD, Yükseltme paneli (`design/ui/ui_mockups.html`) + menü arka planı | ✅ |
| UI (v1.5) | Yeni Şube kartı (önce / sonra, kalır / sıfırlanır, iki dokunuşla onay), şube rozeti, şube şeridi, Kraliyet Ziyareti satırı (ikon: Gizli Kral portresi) | ✅ |
| UI (sonra) | Seviye Atladın, Sen Yokken…, Koleksiyon Defteri, Mağaza, Düzenleme modu | ⬜ |
| Hareket | İskelet (BODY_S/M/L) + temel animasyon seti | ⬜ |
| Ses (v1.1) | 38 efekt (Kenney CC0 + katmanlama), 3 müzik (RandomMind CC0), 6 ses tipi × 14 oyuncak-dil konuşma, 5 ortam döngüsü (`tools/audio/build_audio.py`, önizleme `design/audio/ses_onizleme.html`) | ✅ |

### 11.1 Ses kuralları (v1.1)

> **v1.9 (oyuncu testi madde 2 "ses ve müzik cızırtılı", Android ve iPhone):** ölçüm (`production/qa/evidence/2026-11-17-v19-log.txt`): dosyalarda kırpılma, DC kayması, yanlış örnekleme hızı yok. Asıl kaynak **ocak döngüsü** (`amb/fire`): sn'de 46 yüksek frekanslı dürtü (tepe/RMS 29.8 dB), oyunun en yüksek ortam sesi (akşam 0.7) — telefon hoparlöründe müziğin altında sürekli çıtırtı. Yeniden üretildi (yumuşak odun çıtırtısı, 0 dürtü, tepe/RMS 14.3 dB). Ek olarak: başı sıfırda olmayan 8 efekt 2 ms / 5 ms sönümlendi (tık), `day` ve `evening` müziğinin döngü ek yerindeki sıçrama (0.07 / 0.13) giderildi, çalan efekt kaynağı yenisi için kesilmiyor (boş kaynak yoksa atlanır). Karışım: müzik 0.45 (0.6), ortam toplamı ≤ 0.35 (akşam 1.67'ye çıkabiliyordu; ocak 0.28 / 0.4), en fazla 6 efekt (10) ve 3 konuşma (4), aynı efekt üst üste binmez, aynı anda çalan efektlerin düzey toplamı ≤ 1.0, efektler ×0.8. İçe aktarma: müzik akış yerine sıkıştırılmış bellek, ortam Vorbis kalitesi 0.5, bütün klipler 44.1 kHz sabit. `AudioManager`: DSP tamponu 1024 (Best Performance), gerçek ses 24, sanal 64. Cihazda doğrulanmadı.

- **Müzik:** (RandomMind, CC0 — `assets/audio/CREDITS.md`) gündüz `day` (Minstrel Dance); hava kararınca ve gün sonu ekranında `evening` (The Old Tower Inn); yoğun saatte `rush` (Market Day). Geçiş 2,5 sn çapraz; dönülen parça kaldığı yerden sürer. Büyük anlarda (seviye, yoğun saat, yeni alan, şafak, akşam) müzik efekt süresince %35'e iner.
- **Kalabalık:** ayakta/oturan müşteri sayısı `n` (çıkanlar hariç). Az uğultu = `clamp01(n/4)·0.32`, dolu uğultu = `clamp01((n−4)/10)·0.45` (yoğun saatte en az 0.18). Gün bitince ikisi de söner.
- **Gevezelik:** `n ≥ 2` iken her `U(1.0, 3.2) · lerp(1.4, 0.45, n/14)` saniyede içen bir müşteri konuşur (%22 kahkaha). Sipariş anında %30 “sipariş” sesi, serviste %45 (bahşişli) / %20 “sevinç”, kızgın çıkışta “söylenme”. Aynı anda en fazla 4 konuşma; ekran dışındakiler %35 sesle, ekrandaki yerine göre sağ/sol.
- **Ses tipleri:** kalın (Oduncu, Demirci, Muhafız, Cüce, Şövalye, Ork), kadın (Çoban Kız, Elf, Cadı, Simyacı), minik (Buçukluk, Goblin), yaşlı (Keşiş, Büyücü), kibar (Tüccar, Vergi Memuru), diğerleri erkek. Her tipin kendi sabit perdesi var (0,93–1,05).


## 12. Yayın altyapısı (v1.6)

Reklam, satın alma ve ölçüm için şimdilik yalnızca altyapı var (sahip kararı, 2026-11-11). Gerçek hizmet hesapları sonra açılıp bağlanacak.

- **Hizmetler:** oyun reklamı `IAdService`, Hazine ödemesini `IStoreService`, olayları `IAnalyticsService` üzerinden ister (`Services`). Varsayılan uygulamalar test içindir: 5 / 15 sn reklam ekranı, "TEST" onaylı mağaza, cihazdaki `analytics.jsonl`. Gerçek sağlayıcı tek satırla takılır (`docs/services-integration.md`).
- **Yayın güvenliği:** geliştirme derlemesi olmayan sürümde gerçek sağlayıcı yoksa reklam düğmeleri (yardımcı garson, görevli seviyesi, siparişi hızlandır, ad değiştirme) gizlenir, Hazine ürünleri "YAKINDA" olur. Hiçbir ödül ya da ürün verilmez.
- **Ölçüm olayları:** rehber (başla, adım, bitir, atla), seviye, gün sonu, alan, makine, işe alma ve kovma, etkinlik, özel sipariş, prestij, reklam, mağaza, oturum. Geliştirme derlemesinde Ayarlar'da "Ölçüm özeti" satırı görünür; bu satır yalnızca o cihazın sayılarını gösterir.
- **Kayıt:** `save.dat` dosyası JSON ve HMAC-SHA256 imzası içerir (cihaz kimliğine bağlı). Bir önceki sürüm `save.bak` olarak kalır. İmza tutmazsa yedek açılır; ikisi de bozuksa dosyalar kenara alınır ve oyuncuya bilgi verilir. Eski PlayerPrefs kaydı ilk açılışta dosyaya taşınır (`saveVersion` 2). Oyun 30 sn'de bir ve önemli olaylarda (satın alma, seviye, gün sonu, alan) arka planda kaydeder; arka plana geçişte ve kapanışta eşzamanlı kaydeder. Bu koruma sıradan dosya düzenlemeye karşıdır, kararlı bir hileciyi durdurmaz.
- **Kimlik:** Bn Studio, `com.bnstudio.tavernatycoon`, 1.0.0 (Android sürüm kodu 1). Android yayın derlemesi AAB çıktısı verir ve imza anahtarını ortam değişkenlerinden okur (`docs/release-checklist.md`).

## 13. Diller ve bildirimler (v1.7)

- **Diller:** Türkçe (kaynak), İngilizce, Basitleştirilmiş Çince, İspanyolca, Brezilya Portekizcesi, Almanca, Fransızca, Rusça, Japonca, Korece. Ayarlar › Dil, 10 dili kendi adıyla gösteren bir seçici açar (ana menüden de Ayarlar ile erişilir); seçim hemen uygulanır, sahne yeniden yüklenmez. İlk açılışta dil sistem dilinden seçilir, eşleşme yoksa İngilizce.
- **Sistem:** anahtar Türkçe cümlenin kendisidir (`Loc.T` / `Loc.F` / veri tablolarında `Loc.N`), tablolar `Resources/Loc/<dil>.json`. UXML metinleri yüklenirken çevrilir. Oyuncunun yazdığı taverna adı çevrilmez, varsayılan ad çevrilir. Sayılar dilden bağımsız; "B / M" kısaltmaları dile göre. Ayrıntı ve anahtar / dil ekleme adımları: `docs/localization.md`. Çeviriler model üretimidir, yayından önce anadili konuşan biri gözden geçirmeli.
- **Yazı tipleri:** Rusça Rubik'ten, Çince / Japonca / Korece Noto Sans SC / JP / KR Bold'dan (OFL) çizilir; etkin dilin CJK yazı tipi öne geçer (Han karakterleri dile uygun biçimde).
- **Bildirimler** (`com.unity.mobile.notifications` 2.5.0): Ayarlar › Bildirimler varsayılan kapalı. Oyuncu açınca izin istenir (Android 13+, iOS), açılışta istenmez. Uygulama arka plana geçince planlanır, ön plana dönünce iptal edilir:
  - Kasa doldu: şimdi + çevrimdışı üst sınırı (`Offline.CapMinutes`, 120 dk).
  - Günlük ödül hazır: ertesi gün aynı saat.
  - Hatırlatma: 3 gün sonra.
  - 22:00–09:00 arası bildirim yok (09:00'a kayar); günde en fazla bir bildirim (aynı güne düşen sonraki atlanır). Metinler etkin dilde. Planlama saf bir işlev (`Notifications.Plan`), birim testli; cihazda henüz denenmedi.

---

## 14. v2.0 Paket 2 (oyuncu testi 2026-11-17, maddeler 14, 15, 17–21)

Kaynak: `production/qa/playtests/feedback-2026-11-17.md`. Kanıt: `production/qa/evidence/2026-11-17-v20-*.png`, `2026-11-17-v20-log.txt`; birim testleri `V20Tests` (7).

### 14.1 Avizeler ve kademe ışığı (madde 14)
- Kademe değişince her Meyhane / Salon masasının üstüne konan **Demir Avize / Saray Avizesi kalktı**. Kademe ışığını sütunların odaya bakan yüzüne asılı **aplikler** verir (Taş Han demir aplik, Saray Hanı iki kollu altın aplik; 8 sütun, gece ışığı 2.4 / 3.2, menzil 6 / 7.5 m). Odanın kademeyle belirgin aydınlanması için masaların üstünde **gövdesiz sıcak dolgu ışığı** kaldı (2.4 m, gece 1.7 / 2.3). Sütun, kemer, örtü, perde ve halı aynı.
- Satın alınan **Avize** (250 altın, +8) ve Hazine **Kristal Avize** küçültülüp yükseltildi: çap 0.9 → 0.44 m (kristal 0.76 → 0.56 m), gövde dünyada ≈ 2.4–2.8 m, ince tek zincir, 4 mum. Işık gövde hizasında (avize gece 2.8, menzil 6.5 m; kristal 3.6, 8 m). Modeller `tools/blender/build_props.py x_avize`, `build_premium.py kristal_avize`, aplikler `build_v20.py`.

### 14.2 Gün sonu: son siparişler ve kapanış (madde 15)
- Günün son **%10**'unda (`Balance.LastCallPhase` 0.9, 420 sn'lik günde son 42 sn) yeni müşteri gelmez; oturan müşteri içtiğinin parasını bırakır ve **yeni sipariş vermeden** kalkar. Başlarken bir kez "Son siparişler! Yeni müşteri gelmiyor" bildirimi.
- Gün süresi dolunca **kapanış**: saat "GÜN n · KAPANIŞ" der. İçmekte olan müşteri hesabını hemen öder; siparişini bekleyen ya da yolda olan müşteri **kızmadan** çıkar (mutluluk, yıldız, ceza yok, para kaybı yok; masadaki para yığınları masada kalır, ertesi sabah toplanır). Müşteriler kapanışta %50 hızlı yürür.
- **Personel** (garsonlar, barmen, aşçılar, temizlikçi, fenerci, hizmetçi, ozan) işi bırakıp bölgesinin kapısından (meyhane, salon, bahçe, avlu, iskele; üst kattakiler merdivenden salon kapısına) sokağa çıkar ve sokak boyunca 7 m yürüyüp kaybolur (`StaffCommute`, `Commuter`, hız 3.2 m/sn).
- "Gün Bitti / Yarını Planla" ekranı, tavernada müşteri ve kaldırıma çıkmamış görevli kalmayınca ya da en geç **12 oyun saniyesi** sonra açılır; sokakta kalan müşteri ve görevli o anda kaybolur. Plan ekranında tavernada kimse yoktur. **Han misafirleri** (geceleyen) odalarında kalır.
- "Sonraki Güne Geç": görevliler sokağın ucunda belirir, kapıdan girip akşam bıraktıkları yere yürür ve işe başlar. İlk müşteri en geç 3 sn sonra gelir. Gece işe alınan görevli hemen yerinde başlar.
- Ölçüm (`dayend` koşusu): son siparişten sonra gelen yeni müşteri 0, kapanışta kızgın ayrılan 0, plan ekranında müşteri 0 / görevli 0, kapanış ≈ 6 oyun sn, sabah 6 sn sonra 9 görevlinin hepsi işinde.

### 14.3 Bahçe (madde 17, 18, 21)
- **Bahçe Uzun Masası** (`masaBU`): Salon'daki Uzun Masa gibi 6 oturak, bahçenin sol yarısında (x −12.4, z −0.3; I masası ile ön sıra arasında). Ödeme karesi 1.600 taban → **2.240 altın**, **Sv10**, bahçe açılınca (diğer bahçe masaları 630–1.890). Ambiyans 8. Müşteri ön koridordan (z −3.55) bahçenin sol kenarına (x −13.75), oradan masanın önü (z −1.7) ya da arkası (z 1.05) boyunca oturağına yürür; mevcut masa, yol, sahne ve iskele kapısıyla çakışmaz.
- **Ozan sahnede** (madde 18): Ozan Sahnesi (`sahne`) açıksa ozan Lir (ya da Ozan Konseri etkinliğinde gelen ozan) meyhanedeki yerinden sahneye yürür, sahnenin üstüne çıkar (0.3 m), seyirciye döner ve ritimle çalar (`ToyAnimator.SetPlay`: saniyede 2 vuruş, iki yana sallanma ve sekme). Sahne yoksa eski yerinde durur.
- **Dekor** (madde 21): **Taş Yol** (120 altın, Sv8, +3, 4 yer: girişten ön koridora, koridor boyunca, sahnenin önü) ve **Bahçe Feneri** (300 altın, Sv9, +6, 3 yer: köşeler ve yolların dışı). Bahçe feneri gece kendiliğinden yanar (`NightLight`, gündüz sönük, gece 3.4, menzil 6 m); bölge fenerleri (`Lantern`, Barlo / fenerci yakar) sayımına girmez. Yeni yerler kayıttaki yer indeksleri kaymasın diye dekor yeri listesinin sonunda.

### 14.4 Han (madde 19, 20)
- **8 oda** (6 → 8): üst kat sola 4 m uzadı (x −10.1..6.1), oda 7–8 solda (x −7, −9; ödeme kareleri 5.000 / 6.000 taban → 7.000 / 8.400 altın, Sv18 / Sv20). Merdiven ağzı koridorda kalır. Kamera üst katta x −5'e kadar kayar.
- **Konfor 0–4**, her seviye odada görünür eşya ekler (yeni eşya zıplayarak gelir) ve gecelik ücreti ile misafir gelişini artırır:

| Konfor | Odada | Gecelik | Yükseltme bedeli |
|---|---|---|---|
| 0 | Saman yatak (sade oda) | 40 | — |
| 1 | Yün yatak | 65 | 500 |
| 2 | + kilim + komodin | 95 | 900 |
| 3 | + ayaklı lamba (oda ışığı) + dolap | 130 | 1.600 |
| 4 | Kuştüyü (cibinlikli) yatak + şömine + tablo | 180 | 2.600 |

- **Misafir gelişi:** geliş aralığı 14 sn / (1 + 0.04 × açık odaların konfor toplamı); 8 oda Konfor 4'te ×2.28 (≈ 6.1 sn). Misafir en konforlu boş odayı seçer.
- Dükkân › Dekor › **Han odaları** kartı: "Oda n · Konfor c/4" ve "Sonraki: <eşyalar> · gecelik a → b · misafir %4 artar" (en yüksekte "<eşyalar> · gecelik b · en yüksek konfor").
- **Kayıt göçü** (sürüm 2 → 3): eski 3 kademeli konfordaki 2 (kuştüyü) → 4; 0 ve 1 aynı.

### 14.5 Oyun salonu (madde 20)
- 3 → **5 masa**: arka sırada **Zar Masası 2** (x 5.5, z 9.25; 2 oturak, 5.000 taban → 7.000 altın, Sv17, oyun salonu açılınca) ve **Kart Masası 2** (x 12.9, z 9.25; 2 oturak, 6.000 → 8.400 altın, Sv19, kart masası açılınca). Yanlardaki sütunlardan (x 4.65, 5.5, 11.9) oturulur; saksı ön sağ köşeye taşındı. Ücretler aynı (zar 6, kart 10 / oyuncu). Ölçüm: 280 oyun sn'de beş masanın her birinde 6–12 tur.

### 14.6 Ambiyans
Hazine olmadan toplam ambiyans 436 → **487** (+ uzun masa 8, iki oyun masası 13, taş yol 12, bahçe feneri 18). Yıldız eşikleri değişmedi.

### 14.7 Test altyapısı
- Bot: `-autotest-v20 tiers|garden|inn|game|dayend` (`PlaytestBotV20`): Barlo'yu sabit noktaya ışınlayıp görüntü alır, kapanış / sabah durumunu günlüğe yazar.
- `-autotest-hidden` (yalnızca Windows geliştirme derlemesi, `AutoTestWindow`): oyun penceresi hiç etkinleşmeden ekran dışına ve en alta itilir (user32 `ShowWindow(SW_SHOWNOACTIVATE)` + `SetWindowPos(HWND_BOTTOM, −4000, −4000, NOSIZE|NOACTIVATE)`, ilk 12 sn'de yarım saniyede bir), arka planda çalışır, gerçek fare ve klavye kapatılır. Koşular `Start-Process -WindowStyle Minimized` + `-popupwindow` ile başlatılır; ekran görüntüsü (arayüz dahil) ekran dışında da alınıyor.

---

## 15. v2.1 Paket 3 (oyuncu testi 2026-11-17, maddeler 10 ve 25)

Kaynak: `production/qa/playtests/feedback-2026-11-17.md`. Kanıt: `production/qa/evidence/2026-11-21-v21-*.png`, `2026-11-21-v21-log.txt`; birim testleri `V21Tests` (7).

### 15.1 Barlo'nun yetenekleri (madde 10)
Oyuncunun karakteri için dört kalıcı yükseltme, her biri **5 seviye**, altınla (`Scripts/Core/BarloSkills.cs`, sayılar `Balance.Barlo*`). Her yetenek seviyesi belirli bir taverna seviyesinde açılır. Fiyat dayanağı: açıldığı seviyedeki iyi oyuncunun 3 dakikalık geliri (`Balance.EventIncomeRef`): ilk seviyeler bunun 0.2–0.35 katı (yarım dakikadan kısa), son seviyeler 1.5–2 katı (geç oyun hedefi, zorunlu değil).

| Yetenek | Etki (Sv0 → Sv5) | Açıldığı taverna seviyesi (Sv1–5) | Fiyat (Sv1–5, altın) |
|---|---|---|---|
| **Koşu hızı** | 3.4 → 3.7 → 4.0 → 4.3 → 4.6 → 4.9 m/sn | 2 · 6 · 11 · 17 · 24 | 150 · 900 · 3.000 · 8.000 · 22.000 |
| **Taşıma kapasitesi** | 2 → 3 → 4 → 5 → 6 → 7 bardak | 3 · 8 · 14 · 20 · 26 | 250 · 1.500 · 4.500 · 11.000 · 28.000 |
| **Servis menzili** | 1.35 → 1.50 → 1.65 → 1.80 → 1.95 → 2.10 m | 4 · 9 · 15 · 21 · 27 | 300 · 1.800 · 5.000 · 12.000 · 30.000 |
| **Para toplama menzili** | 1.35 → 2.0 → 2.6 → 3.2 → 3.8 → 4.5 m | 3 · 7 · 13 · 19 · 25 | 200 · 1.200 · 3.500 · 9.000 · 24.000 |

- Toplam: 5 seviyenin hepsi 166.300 altın (Sv30 prestij şartının ≈ 1.1 katı; dört yeteneğin ilk seviyeleri Sv2–4'te toplam 900 altın).
- **Servis menzili** Barlo'nun müşteriye servis, makineden bardak alma, kirli masa temizleme (+0.2), han misafiri karşılama ve oyun masası başlatma (+0.3) mesafesidir (eskiden sabit `Balance.InteractRadius`). **Para menzili** yalnızca para yığınlarını çeker. Garsonlar etkilenmez.
- **Dükkân:** ayrı sekme yerine Personel sekmesinin başında "BARLO" başlığı ve dört kart (dar ekranda 6 sekme zaten sığmaya sınırda; rehber sürerken kartlar listenin sonunda, işe alım kartı önde kalsın diye). Kart: "Koşu hızı · Sv 1/5", "Hız 3.4 → 3.7 m/sn · Barlo daha hızlı koşar", düğme fiyat / kilitli "SV n" / "MAX". Alınca konfeti, Barlo zıplar, bildirim ("Koşu hızı: Hız 3.7 m/sn").
- **"YENİ!"** rozeti her yetenek seviyesini ayrı ürün sayar (`barlo_<yetenek>_<seviye>`): yeni açılan seviye kartta ve Personel sekmesinde rozet çıkarır. **Seviye kartı** açılanlarda "Barlo yeteneği · Servis menzili 1" gösterir.
- **Prestij:** yetenekler **sıfırlanır** (taverna gibi; şube kartının "Sıfırlanır" sütununda). Şube çarpanı gelir hızını artırdığı için yeniden almak hızlıdır.
- Ölçüm: `barlo_skill` (yetenek, seviye, taverna_seviyesi). Kayıtta `barloSpeed`, `barloCarry`, `barloServe`, `barloCoin`.
- Test ölçümü (bot): Sv15'te üç seviye alınınca ölçülen en yüksek koşu hızı 4.30 m/sn (tablo 4.30), en uzak servis 1.79 m (menzil 1.80), en uzak para toplama 3.20 m (menzil 3.20); Sv3'te 3.70 m/sn, para 1.99 m (menzil 2.00). Bot tek siparişlik yol izlediği için tepside en çok 1–2 bardak taşıdı (kapasite değeri 3 / 5 kayıtta ve tepside doğrulandı).

### 15.2 Gerçek bir son (madde 25)
- **Tetik:** 3. taverna (şube 2, Dağ Geçidi) **Sv30** ve **Saray Hanı** kademesi; son henüz görülmemiş (`Scripts/Core/Ending.cs`). Altın şartı yok. Bu durumda hedef kutusu "Krala Davet hazır! Büyüt karesine git" (Gizli Kral ikonu) gösterir; "Tavernayı Büyüt" karesi prestij kartı yerine **Krala Davet** kartını açar (üç şart çipi, "SONRA" / "KRALI DAVET ET").
- **Son sahne** (oyun içinde, kodla: `Scripts/Gameplay/EndingScene.cs`, arayüz `Scripts/UI/HudV21.cs`): siyah sinema şeritleri, alt yazı; HUD gizli, gün durur, yeni müşteri gelmez, oturanların sabrı azalmaz, joystick kapalı; zaman ölçeği 1; müzik en coşkulu parça (yoğun saat, "Market Day"), başta borazan.
  1. **Geliş** (≈ 6.5 sn): Kral (Gizli Kral modeli, ×1.15) önde şövalye, arkada iki muhafızla sokağın batısından kapıya yürür; kamera sokakta alçak açıyla izler. "Kral maiyetiyle geliyor!"
  2. **Karşılama** (3.2 sn): Barlo kapının içinde bekler, iki kez zıplar; kral karşılık verir. "Barlo kralı kapıda karşılıyor"
  3. **Masaya** (≈ 2 sn): kral meyhanede kapıya en yakın boş oturağa yürür. "Kral en iyi masaya buyuruyor"
  4. **Kadeh** (4 sn): kralın elinde Kral Şerbeti kadehi yükselir, "şerefe" sallanır, Barlo ve kral zıplar. "Şerefe! Kral kadeh kaldırıyor"
  5. **Tur** (9 sn): kamera tavernanın üstünde yükselip yavaşça döner (−22° → 55°, 56° eğim, 19 m), havai fişek patlamaları (0.42 sn'de bir) ve konfeti, ortada "EFSANEVİ HAN" unvan şeridi ve "<taverna adı> artık krallığın en ünlü hanı".
  6. **Jenerik** (14 sn, dokununca geçilir): Taverna Tycoon · Bir Bn Studio oyunu · Tasarım, kod ve modeller: Bn Studio · Müzik: RandomMind (Minstrel Dance, The Old Tower Inn, Market Day; CC0, opengameart.org) · Ses efektleri: Kenney (Casino, Impact, Interface, RPG Audio; CC0) · Yazı tipleri: Noto Sans SC/JP/KR, Nunito, Rubik, Lilita One (SIL OFL 1.1) · Araçlar: Unity, Blender · "Oynadığın için teşekkürler!"
  Toplam ≈ 39 sn (jenerik geçilirse daha kısa; bot ölçümü 24.6 sn sahne + jenerik).
- **Sonra serbest oyun:** kral ve maiyeti kaybolur, oyun aynı tavernada sürer; "EFSANEVİ HAN" şeridi ve "Serbest oyun: taverna senin. Yeni Şube istersen hâlâ açık" bildirimi. Prestij yine açık (büyüt karesi artık şube kartını açar, 4. şube "3. Şube"). **"Efsanevi Han" rozeti** kalıcıdır: HUD'da seviye kalkanının solunda (şube rozetinin altında) yıldızlı kırmızı daire, ana menüde logonun altında yıldızlı şerit.
- Son sahne **bir kez** kendiliğinden oynar; Ayarlar'da (yalnızca oyun içinde, görüldükten sonra) **"Son sahneyi yeniden izle"** düğmesi (gün sonu ekranındayken "Yeni gün başlayınca izlenebilir").
- Ölçüm: `game_complete` (sure_sn = oyun sahnesindeki toplam süre, gun = prestijte sıfırlanmayan toplam gün, sube, servis); ilk izlemede bir kez. Kayıtta `endingSeen`, `playSeconds`, `daysTotal`.

### 15.3 Test altyapısı
- Bot `-autotest-v21 barlo` (`-autotest-start-level 3` / `15` ile): Personel sekmesi önce / sonra, açık seviyelerin hepsi alınır, ölçülen hız (ışınlanma süzgeçli 1 sn pencere), tepsi, servis ve toplama mesafesi; Sv3'te bir sonraki seviyenin kartı ve "YENİ!" rozeti.
- Bot `-autotest-v21 ending` (`-autotest-start-level 30 -autotest-tier 2` ile; bot şubeyi 2 yapar, bütün alanları açar): hedef kutusu, Krala Davet kartı, altı aşamanın her biri, jenerik (dokunuşla geçilir), serbest oyunda şube kartı, Ayarlar, Barlo kartları ve 30+ sn oyun. Sonraki koşunun ana menü görüntüsü (`app_menu.png`) kalıcı rozeti gösterir.

## 16. v2.2 Paket 4 (sahibin seçtiği eklemeler: sezonluk etkinlikler ve kostümler)

Kaynak: `production/qa/playtests/feedback-2026-11-17.md` › "Sahibinin seçtiği eklemeler". Kod: `Scripts/Core/Seasons.cs`, `Scripts/Core/Costumes.cs`,
`Scripts/Gameplay/SeasonScenery.cs`, `Scripts/Gameplay/CostumeWearer.cs`, arayüz `Scripts/UI/HudV22.cs`; modeller `tools/blender/build_v22.py`.

### 16.1 Sezon takvimi
| Sezon | Takvim (cihazın yerel tarihi) | Sezon içeceği | Sezon müşterisi | Süsleme (kendiliğinden) | Kalıcı sezon dekoru (jeton) | Sezon kostümü (jeton) |
|---|---|---|---|---|---|---|
| **Hasat Şenliği** | 1 Eylül – 31 Ekim | Elma Şarabı | Hasat Köylüsü | saman balyası, kabaklar, başak, dökülen yapraklar | Kabak Fenerleri 60 (+6, gece yanar) · Başak Demeti 45 (+5) | Barlo · Hasat Çiftçisi 120 |
| **Kış Festivali** | 1 Aralık – 31 Ocak | Baharatlı Sıcak Şarap | Kış Gezgini | kar yığınları, süslü çam, hediyeler | Kardan Adam 60 (+6) · Süslü Çam 80 (+8) | Barlo · Kış Festivali 120 |
| **Bahar Bayramı** | 1 Nisan – 31 Mayıs | Çiçek Şerbeti | Bahar Çiçekçisi | çiçekli fıçı saksı, sulama kabı, kelebekler | Çiçek Arabası 70 (+7) · Kuş Evi 45 (+5) | Nana · Bahar Çiçeği 100 |

- Sezon dışında (Şubat–Mart, Haziran–Ağustos, Kasım) hiçbir sezon öğesi yoktur: tezgâh, süsleme, sezon müşterisi, rozet, sekme, jeton görünmez. Kış sezonu yılı aşar; anahtarı başladığı yıldır (`winter-2026` = Aralık 2026 – Ocak 2027).
- **Süsleme** takvim sürdükçe görünür (sokakta kapının iki yanı ve bahçe kapısı tarafı, Meyhane'nin ön sol köşesi ve sol duvarı). **Tezgâh, müşteri, jeton ve görevler** Sv2'den (etkinliklerle aynı) ve rehber bittikten sonra.
- **Sezon tezgâhı** (geçici makine, en sade çözüm: mevcut makine sistemi): Meyhane'nin ön sağ köşesinde tekerlekli, çizgili tenteli tezgâh; depo malzemesi harcamaz, satın alınmaz, sezon bitince kalkar. Yükseltmeleri (hız / stok / kalite) normal makine gibi altınla; kayıtta kalır, gelecek yıl aynı sezonda geri gelir. Fiyat 8 / 9 / 8 altın, 4 sn (ucuz; asıl ödül jeton). Depo panelinde yer almaz, günlük görevler sezon içeceği istemez.
- **Sezon müşterisi:** gelen müşterinin %15'i (tezgâh aynı kattaysa); oturuşta 2 sipariş, yalnızca sezon içeceği; sabır ★4, ödeme ★2. Koleksiyon defterine girmez. Sıradan müşteriler de siparişlerinin %12'sinde sezon içeceğini ister.
- **Bildirim:** uygulama arka plana geçince sıradaki sezonun ilk günü 10:00 için "<Sezon> başladı!" planlanır (Ayarlar'daki düğme açıksa; sessiz saat 22:00–09:00 yok; günde en fazla bir bildirim — o gün diğer bildirimlerin önüne geçer). Oyun içinde sezonun ilk açılışında "<SEZON> BAŞLADI!" şeridi, konfeti ve "Sezon tezgâhı açıldı" bildirimi (sezon başına bir kez).

### 16.2 Sezon jetonu (ekonomi)
- **Kazanç:** sezon içeceği satışı başına 1 jeton (oyuncu ya da garson servis etsin) + sezon görevleri.
- **Görevler** (sezon başına bir kez, toplam 100 jeton): sezon içeceğinden 25 sat (+30) · sezon müşterisine 10 kez servis (+25) · 150 müşteriye servis (+25) · sezonda 3 gün tamamla (+20).
- **Harcama:** sezonun iki kalıcı dekoru (105–150) + sezon kostümü (100–120) = sezon başına 225–270 jeton. Görevler 100 getirir; kalanı ≈ 130–170 sezon içeceği satışı. Bot ölçümü (Sv6, ~110 oyun-sn): 7–12 jeton, sezon müşterisi 3–6 kez geldi → bir oyun günü (420 sn) ≈ 25–40 jeton; sezonun tamamı yaklaşık 4–6 oyun günü (≈ 30–45 dk oyun) ister; iki aylık sezona rahat yayılır, zorlamaz.
- **Sona erme:** jetonlar sezona bağlıdır. Sezon bitince (ya da oyuncu bir sonraki sezonda dönünce) kullanılmamış jetonlar **sıfırlanır, sonraki sezona taşınmaz**. Bu, Sezon sekmesinde kırmızı uyarıyla sürekli yazılır; sezon bitince ilk açılışta pencere çıkar: "<Sezon> bitti: N sezon jetonu sona erdi. Jetonlar sonraki sezona taşınmaz." (+ "Aldığın sezon dekoru ve kostümleri kalıcıdır."). Görevler de yeni sezonda sıfırdan.
- Kalıcı sezon dekoru normal dekor yuvalarına girer (Ana Salon cephesinin önü, sokakta; bölge şartı yok), ambiyans verir, sezon bittiğinde ve prestijte kalır. Dekor sekmesinde satılmaz (`Catalog.Decor.seasonal`, fiyatı jeton).

### 16.3 Arayüz
- **HUD:** etkinlik düğmesinin altında sezon renginde rozet ("HASAT ŞENLİĞİ · 42 gün", sezon ikonu; toplanacak görev varsa "!"). Dokununca Etkinlikler › Sezon sekmesi.
- **Etkinlikler paneli:** sezon sürerken üstte iki sekme (Etkinlikler / <sezon adı>), içerik kaydırılabilir. Sezon sekmesi: başlık kartı (ad, takvim, kalan gün, jeton), sona erme uyarısı, sezon menüsü ve müşterisi, SEZON GÖREVLERİ (TOPLA / TOPLANDI), SEZON DEKORU · KALICI, SEZON KOSTÜMÜ, "GARDIROBA GİT".
- **Ana menü:** logonun altında sezon renginde şerit ("KIŞ FESTİVALİ · 43 gün", ikonlu; Efsanevi Han rozeti varsa altında).

### 16.4 Kostümler
| Kostüm | Kimin | Edinim | Fiyat |
|---|---|---|---|
| Korsan (üç köşeli şapka, göz bandı, kaptan ceketi) | Barlo | Hazine | 49,99 ₺ |
| Şövalye (sorguçlu miğfer, zırh) | Barlo | Hazine | 59,99 ₺ |
| Kış Festivali (ponponlu kış şapkası, atkı) | Barlo | Kış Festivali jetonu | 120 jeton |
| Hasat Çiftçisi (hasır şapka, tulum, başak) | Barlo | Hasat Şenliği jetonu | 120 jeton |
| Kral'ın Dostu (mor bere, pelerin, madalyon) | Barlo | Ödül: son sahne ("Krala Davet", Efsanevi Han) görülünce | — |
| Zarif Yelek | Garson Pip | Hazine | 29,99 ₺ |
| Şık Barmen (silindir şapka, tek gözlük) | Barmen Gus | Hazine | 29,99 ₺ |
| Büyük Şef (kocaman şef şapkası) | Aşçı Ayla | Hazine | 29,99 ₺ |
| Bahar Çiçeği (çiçek tacı, pembe şal) | Temizlikçi Nana | Bahar Bayramı jetonu | 100 jeton |

- **Yalnızca görünüm:** hız, kapasite, gelir değişmez. Aynı gövde (Blender'da mevcut karakter kurucuları + kıyafet / şapka parçaları; `CH_Costume_<ad>`), sahnede kendi modelinin yanında kapalı durur; giyilince açılır, oyuncak animasyonu ve tepsi aynen sürer.
- **Gardırop:** Dükkân › Hazine sekmesinin başında (yeni sekme yok; dar ekranda 6 sekme sınırda). Kart: portre (dokununca büyük önizleme), "<kostüm> · <sahip>", edinim yolu, düğme: GİY / ÇIKAR / fiyat / YAKINDA / SEZONDA (kendi sezonu dışında) / ÖDÜL. Sahip başına bir kostüm; alınca kendiliğinden giyilir.
- **"Yakında" kuralı:** Hazine kostümleri `Premium` ürünüdür (`Kind.Costume`); yayında gerçek mağaza yokken düğme "YAKINDA" ve devre dışı, istenirse "Mağaza yakında açılıyor" penceresi, ürün verilmez. Satın almaları geri yükle kostümleri de geri verir.
- **Kayıt:** `costumes` (edinilen), `wornCostumes` ("sahip=kostüm"). Prestijte sıfırlanmaz; oyun boyunca, son sahnede (Barlo) ve ana menü dioramasında görünür.
- Ölçüm: `costume_purchase`, `costume_equip`; sezon: `season_start`, `season_token`, `season_purchase` (`docs/services-integration.md`).

### 16.5 Test altyapısı
- `-autotest-season harvest|winter|spring|none` (geliştirme derlemesi): takvim o sezonun 20. gününe (none: 15 Temmuz) sabitlenir; ana menüde de geçerli.
- Bot `-autotest-v22 season`: rozet ve duyuru, tezgâh ve süsleme, ~110 oyun-sn servis (sezon müşterisi, sezon içeceği siparişi, jeton), görev ödülü, dekor ve kostüm jetonla, Sezon sekmesi (üst ve alt), sahnede kostüm, sonra sezon bitişi (jeton sona erer, dekor ve kostüm kalır, pencere). `offseason`: hiçbir sezon öğesi yok. `wardrobe`: Gardırop, önizleme, Barlo ve dört personelin kostümü sahnede, ödül kostümü, çıkar; `-autotest-release-sim` ile Hazine kostümleri "YAKINDA".
- Kanıt: `production/qa/evidence/2026-11-21-v22-*.png`, `2026-11-21-v22-log.txt`; birim testleri `Assets/Tests/EditMode/V22Tests.cs`.

## 17. v2.3 Paket 5 (sahibin seçtiği eklemeler: masa düzenleme modu ve bulut kaydı)

Kaynak: `production/qa/playtests/feedback-2026-11-17.md` › "Sahibinin seçtiği eklemeler". Kod: `Scripts/Core/Layout.cs`, `Scripts/Core/LayoutGrid.cs`,
`Scripts/Gameplay/EditLayout.cs`, `Scripts/Gameplay/TableFollower.cs`, arayüz `Scripts/UI/HudV23.cs`; bulut `Scripts/Core/CloudSave.cs`,
`Scripts/Core/Services/ICloudSaveService.cs` (+ `NoneCloudSaveService`, `LocalCloudSaveService`), `Scripts/UI/CloudSavePanel.cs`. Kanıt: `production/qa/evidence/2026-11-22-v23-*`; birim testleri `V23Tests` (11).

### 17.1 Düzenleme modu: giriş ve kontroller
- **Açılış:** **Sv5** (`Layout.UnlockLevel`). Sol sütunda (yardımcı / etkinlik / sezon düğmelerinin altında) **"DÜZENLE"** düğmesi (çekiç ikonu); ilk görüldüğünde "YENİ!" rozeti ve bir kez ipucu bildirimi ("Yeni: DÜZENLE ile masaları ve dekoru istediğin yere taşıyabilirsin"). Seviye kartının açılanlarında "yeni özellik · Düzenleme modu".
- Rehber sürerken, gün sonu / kapanışta ve son sahnede açılmaz.
- **Modda:** zaman durur (müşteri akışı, gün saati, sabır; zaman ölçeği 0, kapanınca önceki değer), HUD gizlenir, kamera 0.4 sn'de üstten bakan açıya geçer (eğim 52° → 66°, uzaklık 15 m, iki parmak / tekerlekle 11–19 m). Açık bölgelerin zemininde 0.5 m'lik ızgara çizgileri.
- **Taşınabilir nesneler** sarı izle vurgulanır. Dokun-sürükle ile taşınır; konum **0.25 m'lik ızgaraya** oturur (`Layout.GridStep`). Seçili nesne yeşil, geçersiz yerdeyse **kırmızı**; boş zemini sürüklemek kamerayı kaydırır.
- Alt çubuk: **DÖNDÜR** (seçili nesne 90°; yeni yer geçersizse kırmızı yanıp söner, dönmez) · **VARSAYILANA DÖN** (her şey varsayılan yuvasına, kaydetmek için BİTTİ) · **İPTAL** (mod açılırkenki hâline döner) · **BİTTİ** (kaydeder). Üstte başlık, ipucu ve "Buraya konabilir / Buraya konamaz".

### 17.2 Geçersiz yerleşim (kırmızı, bırakılamaz)
Bırakınca son geçerli yerine döner ve nedeni yazılır (`LayoutRules`, `PlaceResult`):

| Neden | Kural |
|---|---|
| Bölge dışı | İz (masa + oturaklar) bölgenin iç sınırına sığmalı: Meyhane x −3.95..3.95, Salon 4.25..14.95, Bahçe −14.95..−4.25; z −4.95..4.95. Nesne bölge değiştirmez. |
| Duvar, tezgâh, makine, kapı, yürüme yolu | Sabit engel: sahnedeki çarpıştırıcılar (duvar, köşe, şömine, sahne, makineler) 0.25 m kare kare okunur. Açık tutulan alanlar: ön kapı ağzı (1.4 × 1.1 m), toplanma noktaları (0.9 m), iç kapılar (mutfak, oyun salonu, mahzen, avlu, iskele; 1.2 m), merdiven ağzı, alınmamışlar dahil her makinenin ve sezon tezgâhının önü (servis noktasıyla birlikte, +0.45 m), alınmamış ödeme kareleri, "Tavernayı Büyüt" karesi, kademe sütunları. |
| Başka bir nesneyle çakışma | Diğer masa / dekorun izi; **alınmamış masanın varsayılan yeri ayrılmıştır** (yeni masa alınınca oraya gelir). |
| Erişilemez kalma | Kapıdan 4 yönlü yayılma: nesne yokken ulaşılan her oturak, servis noktası, iç kapı ve toplanma noktası, nesne yeni yerindeyken de ulaşılmalı; taşınan masanın kendi oturakları da (0.55 m yakınında erişilen kare). |

- **Düz eşyalar** (taş yol, masa altında olmayan halı) üstünden yürünür: yalnızca sınır, sabit engel ve birbirleriyle çakışma denetlenir.
- Varsayılan yer her zaman geçerlidir.

### 17.3 Kapsam
- **Taşınır:** Meyhane, Ana Salon ve Bahçe'deki bütün (alınmış) masalar — ön sıra ve uzun masalar dahil — ve yer dekorları: saksı, flama, fıçı yığını, bahçe feneri, taş yol, halı. Masa üstü **mum** ve masa altı **halı** masaya bağlanır, masayla taşınır. Oturaklar, sandalyeler, para noktası (masadaki para yığınları da), kirli bardaklar, oturan müşteri ve bardağı masayla gelir; kademe örtüsü ve masa üstü dolgu ışığı `TableFollower` ile.
- **Taşınmaz:** tezgâh ve makineler (sezon tezgâhı dahil), mutfak, şömine, Ozan Sahnesi, merdiven; Han (odalar, konfor eşyaları; üst kat ayrı dünya, oda yerleşimi konfora bağlı), Oyun Salonu (oyun masalarının oturakları sütunlara bağlı), Mahzen, Teras, Avlu, İskele; duvar süsleri (kalkan, boynuz, mühür), avizeler, sokaktaki vitrin ve sezon dekoru.

### 17.4 Yollar (NavMesh yok)
Projede NavMesh yok: müşteri sokak → kapı → masanın toplanma noktası → oturağın yaklaşma noktaları → oturak çizgisinde yürür; garsonlar ve personel müşteriye / masaya düz yürür (arka odalara `KitchenLayout` kapı rotası). Taşınan masanın — ve varsayılan yolu taşınmış bir nesnenin içinden geçen masanın — yaklaşma noktaları bölge ızgarasında **A\*** ile (8 yön, köşe kesmeden, görüş hattıyla sadeleştirilmiş) kapıdan oturağa yeniden kurulur; o masanın toplanma noktası kapı olur. Yürüyen müşteri yeni yoldan devam eder, kalkan müşteri yeni yolun tersinden çıkar. Kapanışta Barlo taşınan bir eşyanın içinde kaldıysa en yakın boş kareye alınır. Garsonlar eskisi gibi düz yürür (masaların içinden geçebilir; değişmedi).

### 17.5 Kayıt ve prestij
- `SaveData.layout`: satır başına `"anahtar|x|z|çeyrek"` (masa `t:<masa adı>`, dekor `d:<yuva>`), `layoutVersion` 1; **kayıt sürümü 3 → 4**. Varsayılan yerindeki nesnenin satırı yoktur. Eski kayıt (alan yok) varsayılan yerleşimle açılır; bozuk satır atılır.
- **Yeni masa / dekor** önce varsayılan yuvasına gelir, sonra taşınabilir.
- **Prestij:** yerleşim tercihi silinmez. Bölgesi kapalı (ya da masası alınmamış) nesnenin satırı bekler; masa / bölge yeniden açılınca kayıtlı yerine gelir. VARSAYILANA DÖN kapalı bölgelerdeki satırları da siler.
- Ölçüm: `edit_mode_open` (taşınabilir, taşınmış, seviye), `edit_mode_save` (taşınmış, sıfırlama, seviye).

### 17.6 Bulut kaydı (altyapı)
Hesap yok; reklam / mağaza yaklaşımıyla aynı: önce altyapı, sağlayıcı (Google Play Games Saved Games / iCloud / Unity Cloud Save) sonra (`docs/services-integration.md` › Bulut kaydı).
- **Hizmet:** `ICloudSaveService` (IsAvailable, SignedIn, ProviderName, LastSyncUtc, SignIn, Upload(bayt, üst bilgi), Download), geri çağrılı. Yayında gerçek sağlayıcı yokken `NoneCloudSaveService` (kullanılamaz → Ayarlar "Yakında", düğmeler kapalı); geliştirmede `LocalCloudSaveService` (`persistentDataPath/cloud_fake/`).
- **Biçim:** `TAVERNA-CLOUD-1` + HMAC-SHA256 imzası + üst bilgi + kayıt; imza cihazdan bağımsız anahtarla (başka cihaz doğrulayabilsin). İndirilen kaydın imzası tutmazsa, kesik ya da imzasızsa **reddedilir**, hiçbir şey değişmez. Yüklenen kayıt güncel sürüme yükseltilir ve bu cihazın anahtarıyla hemen yazılır, sahne yeniden yüklenir.
- **Üst bilgi:** kayıt zamanı, seviye, prestij sayısı, toplam kazanç (`SaveData.totalEarned`, yeni alan, prestijte sıfırlanmaz; eski kayıtta 0'dan başlar), cihaz kimliği (ilk açılışta rastgele GUID, PlayerPrefs'te; kişisel veri yok).
- **Çakışma kuralı** (`CloudSave.Resolve`): ilerlemesi açıkça büyük olan kazanır — prestij sayısı farklıysa yüksek olan; aynıysa seviye farkı ≥ 3; seviyeler yakınsa toplam kazanç ≥ 1,5 kat (seviye ters yönde değilse); bu kurulumun eski yedeğinden ileride olan cihaz. Değilse **KAYIT SEÇ** ekranı: "Bu cihaz: Sv12, 3 prestij — Bulut: Sv14, 3 prestij", iki sütun (seviye, prestij, toplam kazanç, kayıt zamanı), **BU CİHAZI TUT** (bulutun üstüne yazar) / **BULUTU YÜKLE**; "Seçilmeyen kayıt silinir."
  - Şimdi yedekle: bulut boş / bozuk / geride / aynıysa yükler; bulut ileride ya da belirsizse seçim ekranı.
  - Buluttan yükle: bulut ileride → yükler; aynı → "Bu cihaz zaten güncel."; bu cihaz ileride ya da belirsiz → seçim ekranı.
- **Ayarlar:** "Bulut kaydı" satırı (Dil ile Grafik arası; durum: "Son: 22.11 14:02" / "Henüz eşitlenmedi" / "Yakında", **YÖNET**) → pencere: sağlayıcı, son eşitleme, bu cihazın özeti, otomatik yedek notu, **ŞİMDİ YEDEKLE**, **BULUTTAN YÜKLE**, sonuç satırı. Ana menüde de aynı.
- **Otomatik eşitleme:** yalnızca sağlayıcı varsa ve girişliyse; uygulama arka plana geçerken ve **10 dakikada bir** (`CloudSave.AutoSyncSeconds`). Bulutta açıkça ileri ya da belirsiz kayıt varsa yüklemez, Ayarlar'da uyarı çıkar. Kayıt diskten yüklenmeden hiçbir şey gönderilmez.
- Ölçüm: `cloud_backup`, `cloud_restore`, `cloud_conflict`. Gizlilik taslağına bulut kaydı notu eklendi (`docs/privacy-policy-draft.md` §5a).

### 17.7 Test altyapısı
- Bot `-autotest-v23 edit` (`-autotest-start-level 9 -autotest-unlock salon,masa3,masa4,masaE,masaF,masaG` ile): DÜZENLE, bir masa ve bir dekor geçerli yere, geçersiz yer (makine önü, kırmızı, geri alınır), döndürme, BİTTİ; kayıt diske yazılıp sahne diskten yeniden yüklenir ve yerleşim doğrulanır; taşınan masaya müşterinin oturup servis aldığı izlenir.
- `-autotest-keep-save` (yeni bayrak): test kaydı sıfırlanmadan açılır. `-autotest-v23 editcheck` ile ikinci süreçte yerleşimin geldiği doğrulanır.
- Bot `-autotest-v23 cloud` (`-autotest-start-level 12`): Ayarlar satırı, pencere, yedekle, aynı ilerlemede yükle, başka cihazın belirsiz kaydıyla çakışma ekranı → bulutu yükle (sahne yeniden), bozuk imza reddi, yedeklemede çakışma → bu cihazı tut. `-autotest-release-sim` ile "Yakında".
