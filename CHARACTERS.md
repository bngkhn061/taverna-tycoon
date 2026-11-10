# Karakter Tasarımları — Taverna Tycoon

> Tüm karakterler [DESIGN_RULES.md](DESIGN_RULES.md) kurallarına uyar: "Oyuncak Taverna" stili, en fazla 4 ana renk, ortak `BODY_S/M/L` iskeleti.
> İçecek numaraları DESIGN_RULES.md §8'deki tabloya göredir.

---

## A. Ana Karakterler

### 1. Barlo — Taverna Ustası (oyuncunun karakteri)
- **Rol:** Oyuncunun joystick'le yönettiği karakter. İçecek doldurur, servis yapar, parayı toplar.
- **Taban:** `BODY_M`, ama göğüs ve kollar %15 daha geniş (tıknaz, fıçı gibi bir gövde).
- **Siluet anahtarı:** Kocaman, iki yana kıvrık **kızıl bıyık** + başında **bağlı bandana**, kafasının üstünde düğüm. Bandanadan dolayı kafa yukarıdan yuvarlak görünür.
- **Kıyafet:** Krem gömlek (kollar dirseğe kadar sıvalı), uzun **kırmızı önlük** (Tavernacı Kırmızısı), kahverengi bot. Belinde asılı bir bez.
- **Renkler:** Tavernacı Kırmızısı `#C8443A` · Krem `#F4E8D0` · Koyu Meşe `#5C3A1E` · Bal `#E0A040` (kemer tokası). Ten `#E8B48A`.
- **Kişilik:** Neşeli, biraz telaşlı, gururlu. Her satıştan sonra küçük bir "ha!" sevinci.
- **Özel animasyonlar:** `Pour` (fıçının musluğunu açıp maşrapayı doldurur), `Carry_Walk` (elinde 1–4 bardaklık tepsi, bardak sayısı arttıkça yürüyüşü yalpalar), `Special_Barlo` (bıyığını burup göz kırpar, idle'da).
- **Gelişim görünümü:** Taverna seviyesi arttıkça önlük gelişir: bez → deri → altın işlemeli.

### 2. Pip — Çırak
- **Rol:** Barlo'nun yardımcısı. Oyunun başında rehber (tutorial anlatıcısı), sonra **ilk otomatik personel** olur. Boş masaları toplar, bardakları yıkar.
- **Taban:** `BODY_S` (0.8 m). Hızlı ve enerjik.
- **Siluet anahtarı:** Kafasından büyük, **yamuk düşmüş kahverengi kasket** + sırtında **kendisinden büyük bir tepsi** (kalkan gibi taşır). Kasketin yanından fırlamış tek bir tutam saç.
- **Kıyafet:** Yeşil yelek, kısa pantolon, tek askısı düşmüş tulum, çizgili çorap, kocaman ayakkabılar.
- **Renkler:** Orman Yeşili `#4F7F3A` · Bez `#D9C7A3` · Meşe `#8B5A2B` · Altın `#F2C14E` (yelek düğmeleri). Ten `#F6D3B3`, çillerle (geometri: küçük düz diskler).
- **Kişilik:** Meraklı, sakar, hiç durmuyor. Bazen bir bardağı düşürür ama havada yakalar.
- **Özel animasyonlar:** `Run` (koşarken kasketi tutar), `Special_Pip` (tepsiyi parmağında çevirir), `Trip_Save` (tökezler, bardağı yakalar).
- **Not:** Pip, Barlo'yla yan yana durduğunda **büyük–küçük, kırmızı–yeşil** zıtlığı yaratır. İkili marketing görsellerinin yüzü olacak.

---

## B. Müşteri Tipleri (30)

**Sütun açıklamaları**
- **Sabır:** Bekleme süresi (★1 = çok sabırsız, ★5 = çok sabırlı)
- **Ödeme:** Kazandırdığı para (💰1–5)
- **Açılış:** Hangi taverna seviyesinde gelmeye başladığı

### Kademe 1 — Köy Ahalisi (Sv 1–7)
Basit, sabırlı, az ödeyen. Oyuncuya temel döngüyü öğretir.

| # | Müşteri | Taban | Siluet anahtarı | Ana renkler | İçecek | Sabır | Ödeme | Davranış / Özellik | Açılış |
|---|---|---|---|---|---|---|---|---|---|
| 1 | **Çiftçi** | M | Geniş hasır şapka, ağzında saman | Bez, Orman Yeşili, Meşe | 1 Bira | ★★★★ | 💰1 | Standart müşteri, öğretici | 1 |
| 2 | **Balıkçı** | M | Sarı yağmurluk şapkası, omzunda olta | Altın, Kraliyet Mavisi, Krem | 2 Şıra | ★★★★ | 💰1 | Bazen bahşiş olarak balık bırakır (dekor ödülü) | 1 |
| 3 | **Oduncu** | L | Ekose gömlek, sırtında dev balta, gür sakal | Tavernacı Kırmızısı, Gece Lacivert, Meşe | 1 Bira | ★★★ | 💰2 | İki dikişte içer, masayı hızlı boşaltır | 2 |
| 4 | **Ozan** | M | Tüylü bere, sırtında lavta | Mor Büyü, Altın, Krem | 3 Bal Şarabı | ★★★★ | 💰1 | Çalgı çalar: yakındaki müşterilerin sabrını +%20 artırır | 3 |
| 5 | **Fırıncı** | M | Uzun beyaz aşçı başlığı, un lekeli yanaklar | Krem, Bez, Bal | 2 Şıra | ★★★ | 💰2 | Mutlu ayrılırsa ekmek bırakır (ileride yemek sistemine bağlanır) | 4 |
| 6 | **Çoban Kız** | M | İki örgülü saç, elinde kıvrık çoban asası | Kraliyet Mavisi, Krem, Orman Yeşili | 10 Süt | ★★★★★ | 💰1 | Sabırlı ve tatlı, arkasından bazen bir kuzu gelir | 6 |
| 7 | **Kunduracı** | S | Burnunda yuvarlak gözlük, boynunda ayakkabı bağı demeti | Meşe, Bal, Krem | 1 Bira | ★★★★ | 💰1 | Masanın altına bakar, bazen düşürülmüş bir altın bulur | 7 |

### Kademe 2 — Kasaba Halkı (Sv 5–13)
Daha çeşitli istekler, ilk "özel davranışlı" müşteriler.

| # | Müşteri | Taban | Siluet anahtarı | Ana renkler | İçecek | Sabır | Ödeme | Davranış / Özellik | Açılış |
|---|---|---|---|---|---|---|---|---|---|
| 8 | **Demirci** | L | Deri önlük, omzunda çekiç, kocaman kollar | Koyu Meşe, Taş, Şömine Ateşi | 5 Kara Bira | ★★★ | 💰2 | Bardağı çok güçlü bırakır, bazen kırar (yenisi gerekir) | 8 |
| 9 | **Tüccar** | M (şişman) | Yuvarlak göbek, belinde kocaman kese, bere | Kraliyet Mavisi, Altın, Krem | 4 Şarap | ★★ | 💰4 | Sabırsız ama cömert. Hızlı servise ×2 bahşiş verir | 5 |
| 10 | **Muhafız** | M | Yuvarlak miğfer, elinde mızrak, tabard | Tavernacı Kırmızısı, Taş, Altın | 1 Bira | ★★★ | 💰2 | İçerideyken kavgacı müşteriler sakin kalır | 9 |
| 11 | **Keşiş** | M | Kel kafa, kapüşonlu kahverengi cüppe, boynunda tespih | Meşe, Bez, Orman Yeşili | 6 Bitki Çayı | ★★★★★ | 💰2 | Hep bahşiş bırakır, sessiz köşe masayı sever | 10 |
| 12 | **Cüce Madenci** | S | Üstünde mum yanan maden kaskı, yerlere kadar sakal, kazma | Bal, Taş, Gece Lacivert | 3 Bal Şarabı | ★★★ | 💰3 | **Hep 2 tane** ister, mutluysa göbek atar (`Special`) | 11 |
| 13 | **Buçukluk Gezgin** | S | Kıvırcık saç, yalınayak, kocaman tüylü ayaklar, sırt çantası | Orman Yeşili, Bal, Bez | 2 Şıra → 10 Süt | ★★★★ | 💰2 | İki içeceği arka arkaya ister ("ikinci kahvaltı") | 12 |
| 14 | **Vergi Memuru** | M (sıska) | Uzun silindir şapka, elinde parşömen, burnunun ucunda gözlük | Gece Lacivert, Krem, Taş | 4 Şarap | ★ | 💰0 | **Negatif müşteri:** Mutlu ayrılırsa vergi az, kızgın ayrılırsa günlük gelirin %10'unu alır | 13 |
| 15 | **Çırak Şövalye (Yaver)** | S | Büyük gelen miğfer gözlerine düşmüş, tahta kılıç | Kraliyet Mavisi, Taş, Krem | 10 Süt | ★★★★ | 💰1 | Bir Şövalye ile gelirse yanına oturur (grup bonusu) | 9 |

### Kademe 3 — Maceracılar ve Fantastik Irklar (Sv 14–22)
Taverna şöhret kazandıkça gelen renkli müşteriler. Yeni içecekler ve özel mekanikler.

| # | Müşteri | Taban | Siluet anahtarı | Ana renkler | İçecek | Sabır | Ödeme | Davranış / Özellik | Açılış |
|---|---|---|---|---|---|---|---|---|---|
| 16 | **Şövalye** | L | Tam zırh, miğferde uzun kırmızı tüy, kalkan | Taş, Tavernacı Kırmızısı, Altın | 3 Bal Şarabı | ★★★ | 💰3 | Zırhı yüzünden yavaş oturur. Yaver ile gelirse +%50 ödeme | 14 |
| 17 | **Goblin Ayak Takımı** | S | Sivri uzun kulaklar, yamalı kapüşon, sırıtış | Goblin teni, Meşe, Bez | 7 Goblin Grogu | ★★ | 💰2 | Masası uzun süre boş kalırsa yan masadan bir altın çalar | 14 |
| 18 | **Ork Savaşçı** | L | Alt dişleri dışarı çıkık, tek omuzluk, kemik kolye | Ork teni, Koyu Meşe, Tavernacı Kırmızısı | 5 Kara Bira ×2 | ★★ | 💰3 | Kızgın ayrılırsa sandalyeyi devirir (tamir gerekir) | 15 |
| 19 | **Elf Okçu** | M (ince) | Uzun sivri kulaklar, yaprak pelerin, sırtında uzun yay | Orman Yeşili, Elf teni, Altın | 8 Elf Çiyi | ★★★★ | 💰3 | Sadece **temiz** masaya oturur | 18 |
| 20 | **Büyücü** | M | Yıldızlı sivri şapka, belinde sakal, tepesi parlayan asa | Mor Büyü, Gece Lacivert, Altın | ❓ Gizemli | ★★★ | 💰4 | Baloncuğunda "?" vardır. Yaklaşınca hangi içeceği istediği belli olur | 16 |
| 21 | **Cadı** | M | Kıvrık uçlu dev şapka, süpürge, kocaman burun | Gece Lacivert, Mor Büyü, Orman Yeşili | 6 Bitki Çayı | ★★★ | 💰3 | Mutluysa rastgele bir müşterinin sabrını yeniler (iksir) | 17 |
| 22 | **Simyacı** | M | Alnında büyük pirinç gözlük, kemerde fokurdayan şişeler | Bal, Krem, Kraliyet Mavisi | 8 Elf Çiyi | ★★★ | 💰3 | Bazen yeni içecek tarifinin araştırma süresini kısaltır | 19 |
| 23 | **Avcı** | M | Kürk başlık, sırtında boynuz süslemesi, uzun bıçak | Koyu Meşe, Bez, Orman Yeşili | 3 Bal Şarabı | ★★★★ | 💰2 | Bazen dekor ödülü olarak av ganimeti (geyik boynuzu duvar süsü) bırakır | 15 |
| 24 | **Kervancı** | M | Sarık, uzun yelek, omzunda halı rulosu | Altın, Tavernacı Kırmızısı, Kraliyet Mavisi | 6 Bitki Çayı | ★★★★ | 💰3 | Haftada bir gelir, nadir dekor satar (mağaza olayı) | 20 |
| 25 | **Korsan Kaptan** | L | Üç köşeli şapka, göz bandı, **omzunda papağan** | Gece Lacivert, Tavernacı Kırmızısı, Altın | 1 Bira / 7 Grog | ★★ | 💰4 | İçince şarkı söyler: herkesin içme hızı artar. Papağan siparişi bağırır | 21 |
| 26 | **Trol Köprü Bekçisi** | L (en iri) | Taş gibi gri ten, minicik kafa / dev gövde, elinde köprü direği | Trol teni, Taş, Koyu Meşe | 7 Goblin Grogu ×3 | ★★★★ | 💰4 | **2 sandalye** kaplar, çok yavaş içer, çok öder | 22 |

### Kademe 4 — Gece ve Efsanevi Müşteriler (Sv 24+)
Nadir gelen, büyük ödüllü, tavernanın "hikâyesini" anlatan müşteriler.

| # | Müşteri | Taban | Siluet anahtarı | Ana renkler | İçecek | Sabır | Ödeme | Davranış / Özellik | Açılış |
|---|---|---|---|---|---|---|---|---|---|
| 27 | **Ejderha Avcısı** | L | Ejder pulu zırh, sırtında devasa kılıç, yanık pelerin | Şömine Ateşi, Koyu Taş, Altın | 9 Ejder Ateşi | ★★★ | 💰5 | İçerken ağzından küçük bir alev çıkar (efekt) | 24 |
| 28 | **Vampir Kont** | M (uzun) | Yüksek yakalı pelerin, sivri saç çizgisi, solgun ten | Gece Lacivert, Bordo `#8A1E2E`, Krem | 4 Şarap | ★★★ | 💰5 | **Sadece gece** gelir, ışıktan uzak masa ister | 25 |
| 29 | **Hayalet Şövalye** | M | Yarı saydam, havada süzülür (bacak yok), zincirli | Kraliyet Mavisi (saydam), Krem | 4 Şarap | ★★★★★ | 💰4 | Gece gelir, "antik altınla" öder (koleksiyon parası). *Stil istisnası: saydam shader* | 26 |
| 30 | **Kılık Değiştirmiş Kral** | M | Pelerinli kapüşon altında belli olan **takma bıyık**, kapüşondan taşan minik taç ucu | Bez (pelerin), Altın, Mor Büyü | Rastgele (değişir) | ★★ | 💰5+ | **Efsanevi:** Ayda bir gelir. Mükemmel servis edilirse taverna "Kraliyet Mührü" alır (kalıcı bonus) | 28 |

---

## C. Üretim Notları

- **Üretim sırası (öneri):** Barlo → Pip → `BODY_S/M/L` tabanları → Kademe 1 (7 müşteri) → prototip testi → kalan kademeler.
- **Paylaşılan aksesuarlar:** Şapkalar, miğferler ve sakallar ayrı mesh olarak modellenir. Varyasyon için kombinlenebilir (örn. Muhafız miğferi + Şövalye tüyü).
- **Renk varyasyonu:** Kalabalık hissi için her müşteri tipinin 2–3 palet varyantı olabilir (UV kaydırarak, ek maliyet yok).
- **Siluet testi:** 30 müşterinin siyah siluetleri tek bir sayfaya dizilir. Birbirine karışan iki müşteri varsa biri yeniden tasarlanır.
