# Balance Check: Son denge geçişi + cila (v1.8, 2026-11-12 → 2026-11-16)

Önceki rapor: `balance-check-economy-2026-11-10.md`. Hedefler: görev brifi B1–B9 (seviye süresi, baskı, oturak, oyuncuya iş, yükseltmelerin anlamı, maaş, Kral Şerbeti, etkinlik fiyatı, prestij).

## Yöntem

- Windows geliştirme derlemesi + `PlaytestBot`, yeni **ölçüm modu** `-autotest-measure` (`Scripts/Debug/PlaytestBotV18.cs`): Hazine (VIP / Lirael), zorla etkinlik, kovma, panel turu, dil, reklam ve ekran görüntüsü adımları kapalı; bot yalnızca oyuncu gibi oynar ve saniyede bir örnek alır.
- Yeni ölçüm satırları (`[Playtest] v1.8 ÖLÇÜM …`): TEMPO (Ün/dk, bot ve insan ×1.3 seviye süresi), EKONOMİ (altın/dk, servis/dk, günlük maaş / gelir), BASKI (kızgın / sonuçlanan, mutluluk ort. ve aralık, %95+ ve %50− zamanı), OTURAK (normal ve yoğun / etkinlik doluluğu, kirli boş, masa başına bekleyen sipariş), ÇARPAN (uygulanan müşteri çarpanı, seviye / oturak ölçeği, ortalama aralık), KIZGIN YER (bölge kırılımı), ÖZEL (açılan / yetişen / kızgın), SİPARİŞ (içecek bazında sipariş / servis, Kral Şerbeti payı), MAKİNE (seviye, fiyat, sipariş beklerken boş kalma, dolu, malzemesiz saniye), PENCERE (200–380 oyun-sn altın, etkinlik karşılaştırması).
- Başlangıçlar: yeni oyun, Sv5, Sv9, Sv15 (kademe 1), Sv22 (kademe 2), Sv29 (kademe 2); 500 oyun-sn; `-autotest-unlock` ile o seviyedeki alan / makine / personel. Yeni bayraklar: `-autotest-coins` (eski 3.000 × Sv başlangıcı botun her yükseltmeyi almasına yol açıyordu), `-autotest-staff-level`, `-autotest-upgrades` (iyi oyuncunun o seviyedeki görevli / makine seviyesi: Sv5 1/1, Sv9 2/2, Sv15 3/4, Sv22 4/6, Sv29 5/7), `-autotest-event <anahtar>`.
- **Ölçüm hataları düzeltildi:** (1) bot kızgın müşteriyi ×4 hızda hiç saymıyordu (`emoteTime > 1.45` eşiği ilk karede kaçıyordu) — önceki raporlardaki "kızgın 0" bu yüzdendi; (2) önceki tempo ölçümleri 150. sn'de Lirael + VIP (×2 gelir) veriyordu. (3) Bot "iyi oyuncu" yapıldı: sabrı en az kalan ve yetişilebilen müşteri önce, garsonun üstlendiğini almaz, özel siparişe öncelik, maaşı kasada tutar, açılan görevliyi önce işe alır.
- İnsan oyuncu = bot × 1.3 süre (önceki rapordaki varsayım). Bot insandan hızlı ve hatasız; özellikle özel sipariş ve kızgın oranları insan için daha kötü olacaktır.

## Ölçüm: önce / sonra

**Önce** (`b2`: eski denge, düzeltilmiş "iyi oyuncu" bot, görevli seviyesi 0–2 — o sürümde `-autotest-staff-level` yoktu; Hazine'siz):

| Başlangıç | Ün/dk | Seviye Ün | İnsan dk / sv | Altın/dk | Servis/dk | Kızgın | Mutluluk ort. (aralık) | Oturak normal / yoğun | Maaş / gelir | Özel (yetişen) | Bekleyen / masa | Çarpan | Kral Şerbeti |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Yeni oyun | 10.2 | 46 | 5.9 | 180 | 10.2 | %1.2 | 89 (59–100) | 64 / 87 | %1 | 4/4 | 1.19 | ×1.53 | – |
| Sv5 | 16.7 | 80 | 6.2 | 394 | 15.6 | %3.7 | 85 (43–100) | 40 / 69 | %2 | 26/27 %96 | 1.29 | ×1.49 | – |
| Sv9 | 19.6 | 184 | 12.2 | 680 | 17.4 | %30 | 49 (13–84) | 37 / 55 | %10 | 24/39 %62 | 1.29 | ×1.15 | – |
| Sv15 | 13.4 | 645 | 62 | 735 | 10.6 | %60 | 25 (2–71) | 38 / 54 | %28 | 10/51 %20 | 1.24 | ×0.93 | – |
| Sv22 | 29.6 | 1.177 | 52 | 3.097 | 19.1 | %39 | 26 (0–67) | 31 / 55 | %19 | 5/42 %12 | 1.22 | ×0.89 | – |
| Sv29 | 13.3 | 2.017 | 197 | 3.662 | 7.0 | %74 | 21 (0–69) | 23 / 39 | %40 | 9/43 %21 | 1.14 | ×0.85 | %2.9 |

Hazine'siz oyuncu için eski denge orta ve geç oyunda oynanamaz durumdaydı (Sv15+ kızgın %40–74, seviye 50–200 dk). Önceki raporun "fazla kolay" bulgusu Hazine'li bot ve kızgın sayma hatasından geliyordu.

**Sonra** (`i4`: son sabitler, aynı bot, iyi oyuncunun görevli / makine seviyeleriyle):

| Başlangıç | Ün/dk | Seviye Ün | İnsan dk / sv | Altın/dk | Servis/dk | Kızgın | Mutluluk ort. (aralık) | Oturak normal / yoğun | Maaş / gelir | Özel (yetişen) | Bekleyen / masa | Çarpan | Kral Şerbeti |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Yeni oyun | 10.7 | 12–35 | 2.4 / 3.0 / 3.5 / 4.1 (Sv1–4, ölçülen) | 195 | 10.7 | %0 | 90 (60–100) | 73 / 91 | %2.5 | 15/15 | 1.06 | ×1.54 | – |
| Sv5 | 16.7 | 50 | 4.5 (ölçülen) | 288 | 15.7 | %5.8 | 77 (43–100) | 57 / 79 | %11.5 | 7/9 %78 | 1.38 | ×1.56 | – |
| Sv9 | 29.8 | 155 | 6.8 | 915 | 27.1 | %0.4 | 78 (43–100) | 40 / 55 | %20 | 21/24 %88 | 1.14 | ×1.42 | – |
| Sv15 | 29.3 | 260 | 11.5 | 2.574 | 25.2 | %16.3 | 56 (0–88) | 45 / 65 | %17 | 6/21 %29 | 1.30 | ×1.21 | – |
| Sv22 | 47.2 | 480 | 13.2 | 10.712 | 30.0 | %8.8 | 58 (14–89) | 43 / 54 | %10 (bot maaşın bir kısmını ödeyemedi; formül düzeyinde %25–35) | 9/28 %32 | 1.21 | ×1.22 | – |
| Sv29 | 47.6 | 550 | 15.0 | 13.824 | 28.3 | %9.6 | 55 (5–86) | 35 / 41 | %18 | 9/20 %45 | 1.17 | ×1.19 | %5.9 |

Ara turlar (`i1`–`i3`) ve etkinlik koşuları (`e3_*`) günlükleri `production/qa/evidence/2026-11-12-v18-log.txt` içinde özetlendi.

## Değişen sabitler (eski → yeni, gerekçe)

| Sabit | Eski | Yeni | Gerekçe |
|---|---|---|---|
| `Balance.UnForLevel` | 20 × 1.32^…, Sv12 / 16 kırılımları (Sv15 645, Sv29 2.017) | ölçülmüş `UnTable` (Sv1 12 … Sv15 260 … Sv29 550, Sv30 560) | Ün/dk × hedef insan süresi ÷ 1.3; eski eğri Hazine'li botla ölçülmüştü |
| `PatienceSeconds` (`PatienceMin/Max`) | 15 / 45 sn | 20 / 50 sn | büyük haritada yol uzun; Sv15 %60 kızgın |
| `PatienceLevelScale` alt sınırı (`PatienceLevelFloor`) | 0.85 | 0.90 | geç oyun kızgın |
| `SpawnLevelScale` Sv1–5 (`EarlySpawnScale`) | ×1.25 | ×1.1 | "biraz daha çok müşteri"; Sv1–4 kızgın %0–1 |
| `SpawnLevelScale` kalabalık sınırı (`MaxCrowd`) | ×1.25 | ×1.15 | geç oyunda servis kapasitesi büyümüyor |
| `SeatSpawnScale` üst sınırı (`SeatSpawnMax`) | 2.2 | 1.7 | oturak arttıkça sipariş, garson sayısı sabit |
| `DrinkDuration` | sabit 3 sn | `lerp(7, 22, (Sv−1)/19)` sn | doluluk %23–40 → sipariş hızını artırmadan masalar dolu |
| `ProduceTimeScale` (yeni) | ×1 | ×2 | hiçbir makine sipariş beklerken boşalmıyordu: hız / stok / barmen / aşçı etkisizdi |
| `WaiterSpeedScale` (yeni) | ×1 | ×1.3 | garsonlar Sv15+ haritada yetişemiyordu |
| `SpecialOrderChance` (`SpecialChanceEarly/Late`) | %18 / %22 | %10 / %7 | iyi oynayan bot Sv15'te %20 yetiştirebiliyordu |
| `SpecialPatienceMultiplier` | 1.4 | 2.0 | aynı |
| `SignatureOrderChance` (yeni, `Customer.NewOrder`) | – | 0.12 (ölçülen 0.09 ile) | Kral Şerbeti içeceklerin %1.5–2.9'u idi |
| `Wage` (`WageScale`, `WageLevelGrowth`) | ×1, 1.15^Δsv | ×2, 1.12^Δsv | maaş / gelir Sv9–22 %9–11, Sv29 %59 (eski üsle) |
| `TipPerStar` (`Management.TipFromStars`) | %5 / yıldız | %10 / yıldız | dekor geri ödemesi 20–30 gün |
| Dekor fiyatları (`Catalog.DecorList`) | flama 120, fıçı 150, kalkan 220, boynuz 260, avize 420, mühür 3.000 | 80, 80, 150, 170, 250, 1.500 | aynı |
| `Automation.UstaPrices` | 8.000 / 18.000 / 40.000 | 6.000 / 14.000 / 30.000 | geri ödeme 5–8 gün |
| Etkinlik fiyatı (`TavernEvents.Cost`) | `taban + artış × Sv` | `pay × EventIncomeRef(Sv)`, `EventIncomeRef = 660 × 1.135^(Sv−1)` (3 dk gelir); pay: Tellal 0.07, Konser 0.45, Festival 0.25, Kervan 0.25, Turnuva 0.07, Kraliyet 5 | ölçümde Konser fiyatın 2.8 katı, Kraliyet 13 katı ek gelir getirdi; Tellal ve Turnuva ölçülebilir ek gelir getirmedi |
| `Prestige.UnPerBranch` | 0.15 | 0.35 | ikinci şube %25–35 kısa |
| `Premium.StarterOfferMaxLevel` (yeni) | – | 6 | başlangıç teklifi yalnızca Sv3–6'da kendiliğinden |

## Hedefler: tuttu / tutmadı

| Hedef | Durum | Ölçülen |
|---|---|---|
| B1 seviye süresi Sv1–5 2–4 dk | tuttu (Sv5 biraz uzun) | Sv1–4 insan 2.4 / 3.0 / 3.5 / 4.1 dk; Sv5 4.5 dk |
| B1 Sv6–12 5–9 dk | tuttu | Sv6 5.6, Sv9 6.8 dk |
| B1 Sv13–20 9–14 dk | tuttu | Sv15 11.5 dk |
| B1 Sv21–30 14–20 dk | sınırda | Sv22 13.2 dk (biraz hızlı), Sv29 15.0 dk |
| B1 toplam ilk şube 5–6 sa | tuttu (hesap) | tablo + hedef sürelerle ≈ 5.1 sa (her seviye ayrı ölçülmedi) |
| B2 Sv1–4 kızgın < %3 | tuttu | %0 |
| B2 Sv8+ personelsiz yetişilemez | tuttu | eski bot (görevli seviyesi 0–1) Sv9'da %30 kızgındı; personelle %0.4 |
| B2 Sv12+ kızgın %5–12 | kısmen | Sv22 %8.8, Sv29 %9.6 tuttu; **Sv15 %16.3 tutmadı** (aynı ayarla önceki turda %9.6; koşudan koşuya ±5 puan oynuyor) |
| B2 mutluluk çoğunlukla %60–85 | tutmadı (az farkla) | Sv9 78, Sv15–29 ort. 55–58 |
| B3 normal doluluk %55–80, yoğun %90+ | kısmen | Sv1 73 / 91, Sv5 57 / 79 tuttu; **Sv9–29 normal %35–45, yoğun %41–65 tutmadı** (eski %23–38'den iyi) |
| B4 özel siparişlerin %70–85'i (iyi oyuncu) | kısmen | Sv5 %78, Sv9 %88 tuttu; **Sv15–29 bot %29–45 tutmadı** (oyuncunun para toplama, han, oyun masası ve teras işi de var) |
| B4 kasa otomasyonu Sv22 | tuttu | değişmedi |
| B5 yükseltme geri ödemesi | kısmen, tablo aşağıda | kalite çoğu 0.3–2.7 gün; az satılan içeceklerde 8–10 gün |
| B6 maaş / gelir Sv8–30 %15–35 | tuttu (formül), ölçüm kısmen | formül testi Sv8–30 %15–35 geçiyor; ölçüm Sv9 %20, Sv15 %17, Sv29 %18; Sv22 %10 (botun o gün yüksek geliri) |
| B7 Kral Şerbeti ≥ %8 | sınırda | 0.09 ile ölçüm %5.9–8.2 (3 koşu); 0.12'ye çıkarıldı, **yeniden ölçülmedi** |
| B8 etkinlik fiyatı ek gelirin %40–60'ı | Konser, Festival, Kervan tuttu (hesapla); Kraliyet hesapla tuttu; Tellal / Turnuva ölçülemedi | aşağıda |
| B9 ikinci şube %25–35 kısa | hesapla tuttu, ölçülmedi | Ün ×1.35 → seviye süreleri ×0.74 (%26 kısa) + gelir ×1.25; ikinci prestij yolunda çarpanlar loglandı |

### Etkinlikler (3 dk pencere, aynı başlangıçla etkinliksiz koşuya göre)

| Etkinlik (Sv) | Eski fiyat | Ölçülen ek gelir | Eski oran | Yeni fiyat | Yeni oran |
|---|---|---|---|---|---|
| Tellal (Sv5) | 190 | ≈ 0 (oturak / servis doygun; gürültü ±%15) | – | 0.07 × 3 dk gelir ≈ 75 | hedef payı varsayımla |
| Ozan Konseri (Sv9) | 660 | +1.845 | %36 | 0.45 × ref ≈ 830 | ≈ %45 |
| Bira Festivali (Sv9) | 870 | ölçüm gürültülü (≈ 0); hesap: satış ×1.5 → +%50 = ≈ +1.000 | %87 (hesap) | 0.25 × ref ≈ 460 | ≈ %46 (hesap) |
| Tüccar Kervanı (Sv15) | 1.700 | +2.914 | %58 | 0.25 × ref ≈ 970 | ≈ %33 (ölçülen ek gelir 3 dk referansın %75'i; biraz ucuz) |
| Zar Turnuvası (Sv15) | 2.050 | ölçülebilir değil (oyun masası ücreti küçük) | – | 0.07 × ref ≈ 270 | ölçülmedi |
| Kraliyet Ziyareti (Sv29) | 16.500 | +212.600 | %8 | 5 × ref ≈ 115.000 | ≈ %54 |

## Geri ödeme tablosu (oyun günü = 7 dk; son ölçüm `i4`)

Kalite: `Δfiyat × (ödeme çarpanı − 0.3 malzeme) × günlük satış`. Hız / stok yalnızca makine sipariş beklerken boşaldığında getirir; "aç" süresiyle değerlendirildi. Personel ve dekor modelle tahmin.

| Kalem | Örnek | Bedel | Geri ödeme | Not |
|---|---|---|---|---|
| Kalite (çok satılan içecek / yemek) | Sv9 Bira k2→3, Sv15 KozEt k4→5, Sv22 Grog k6→7, Sv29 Kral Şerbeti k7→8 | 45 / 248 / 908 / 1.126 | 0.5–0.9 gün | – |
| Kalite (orta) | Sv9 Şarap, Sv22 Yıllanmış Şarap, Sv29 Ejder Ateşi | 178 / 908 / 1.126 | 1.5–2.7 gün | – |
| Kalite (az satılan) | Sv15 Çay k4→5, Grog k4→5 | 248 / 620 | **8.5–10.1 gün** | o seviyede az sipariş alıyor; sınırda |
| Kalite | Sv29 Gün Batımı Punçu | 1.126 | **∞** | teras müşterisi neredeyse yalnızca oyuncudan servis alıyor, ölçümde 0 satış |
| Hız (yemek) | Sv9 Güveç h0 (114 sn aç / 500), Sv15 Turta h0 | 20 × 4 × 1.18^lv | 1–2 gün (tahmin) | `ProduceTimeScale` sonrası yemek makineleri boşalıyor |
| Hız / stok (içecek, h ≥ 3) | Sv15+ tüm içecekler | – | **etkisiz** | hiçbir içecek makinesi sipariş beklerken boşalmadı; yalnızca yoğun saatte ve erken seviyede anlamlı |
| Görevli seviyesi | garson Sv1→2 (+1 bardak) | 90–780 | < 1 gün (tahmin) | görevli seviyesi 0–2 → 2–5: Sv15 kızgın %60 → %8–16, Ün/dk 13 → 29–34 |
| Raf / teslimat | tüm seviyeler | 40×1.8^lv / 120×2^lv | **ekonomik getirisi yok** | ölçümde malzemesiz kalma 0 sn (otomatik sipariş + raf yetiyor); kolaylık kalemi |
| Usta Altın Kese | 1 / 2 / 3 | 6.000 / 14.000 / 30.000 | Sv15 3.3 gün, Sv22 1.9, Sv29 3.1 | +%10 gelir |
| Usta müşteri | 1–3 | aynı | **servis doygunsa etkisiz** | müşteri çarpanı kızgın oranını artırıyor |
| Dekor | saksı 40 (+2), halı 90 (+4), avize 250 (+8), mühür 1.500 (+15) | – | 3–7 gün (model: yıldız başına +%10 bahşiş + zengin müşteri) | tahmin; ölçülmedi |

## Bilinen / tutmayanlar

- Orta oyun (Sv15) baskısı koşudan koşuya %9–16 arasında oynuyor; mutluluk geç oyunda %55–58 (hedef %60+). Bir sonraki tur için öneri: `MaxCrowd` 1.15 → 1.1 ya da Sv15 kademesinde garson hızı.
- Geç oyunda normal saat doluluğu %35–45: içme süresi uzatıldı ama oturak sayısı (77–95) çok büyük. `SeatSpawnMax` ile `DrinkDurationLate` birlikte bir tur daha ayarlanmalı.
- Özel siparişler geç oyunda iyi oyuncu için bile %30–45; Sv15+ için `SpecialChanceLate` 0.07 → 0.05 ya da sabır ×2 → ×2.5 denenmeli. İnsan oyuncuda düşük olacak.
- Teras müşterilerini yalnızca oyuncu servis ediyor; geç oyun kızgınlarının üçte biri terasta.
- Kral Şerbeti 0.12 ve maaş (×2) son halleriyle tam set yeniden ölçülmedi.

## C11 — periyodik yavaş kareler

Kaynak **bot**: `-autotest-noshots` (yalnızca ekran görüntüsü kapalı) koşusunda 75 gerçek sn'de 1 yavaş kare (başlangıç), aynı koşu görüntülerle 23–24 yavaş kare (en uzun 246 ms). Kareler botun `ScreenCapture.CaptureScreenshot(…, 2)` anlarıyla çakışıyor. Sessiz koşu (`-autotest-quiet`: görüntü, günlük, örnekleme ve test adımları kapalı) 153 gerçek sn'de 1 yavaş kare. Oyun kodunda yine de her kare bellek ayıran dört yer düzeltildi (`GameState.Station`, `Management.Working`, `Carrier.Has/CountOf`, `Waiter.PickJob` lambda / `FindAll`): sessiz Sv15 koşusunda GC toplama 339 → 99 (−%71).

## Cila ve doğrulama (kanıt: `production/qa/evidence/2026-11-12-v18-*`)

- Birim testleri: 84 / 84 geçti (74 eski + 10 yeni: maaş / gelir bandı Sv8–30, etkinlik fiyat sıralaması ve payı, özel sipariş payı, içme süresi, Ün tablosu, başlangıç teklifi penceresi ×3, geliştirici ölçüm satırı ×3).
- Gerileme (son derleme, `Exception` / `NullReference` 0): Türkçe yeni oyun 330 sn (rehber 37.7 gerçek sn'de bitti), Sv9 300 sn, prestij (Sv30 → şube 1: gelir ×1.25, Ün ×1.35), ikinci prestij (şube 1 → 2: gelir ×1.50, Ün ×1.70), içerik (Sv29: Şerbet Çeşmesi x 5.25, çakışan nesne yok; Kral Şerbeti sipariş 10 / servis 10), yayın taklidi (Ölçüm özeti satırı yok), Japonca ve Almanca kısa koşu.
- Hedef kutusu 10 dilde ölçüldü: hepsinde metin kutuya sığıyor (Fransızca küçük puntoya geçiyor); görüntüler `…-v18-hedef-*.png`.
