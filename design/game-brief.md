# Game Brief: Taverna Tycoon (çalışma adı)

<!-- Detaylı tasarım: GAME_DESIGN.md · Görsel kurallar: DESIGN_RULES.md · Karakterler: CHARACTERS.md.
     Bu brief onların bir sayfalık özetidir; çelişki olursa sayılar için GAME_DESIGN.md ve Scripts/Core/Balance.cs esas alınır. -->

## In your words
"Şimdi mobil oyun için tavern master tycoon tarzı bi oyun düşündüm"
"tavern master tycon iyi gibi bi taverna işletip içecek satma bu oyun için blendera bağlanıp bir sürü mini karakter yönetelim oyuna 3. şahıs bakış açısı ile oynayalım mobil için müşteri isteklerine yetişelim steamde ki tavedern amster oyununa benzeyelim bir tane tasarım kuralımız olsun bütün yapcağımız tasarımlar o çizgide olcak"
"böyle iyi ama şöyle olcak oyun planımız seviye seviye aşama aşama gitcek oyunu ne zor ne kolay yapacağız oyuncu sürekli oyunda gelişmek istiyecek işte otomatik içecek üretimi felan veya taverna genişletme tavernayı istediği gibi düzenleme felan işte garsın sayısı artırımı bunların tasarımınıda yapalım ... ayrıca oyunun başlangıç menüsünüde yapalım oyuncu oyna ayarlar çıkış felan"
"oyun haritası daha büyük olmalı ve bakış açısı daha geniş olmalı oyun oyuncuyu içine çekmeli aralıksız oynayıpo gelişmek istemeli daha çok özellik koymalıyız"
"oyun yatay olmalı dikey olmasın daha rahat görüş olur daha rahat oynanır"
"bakış açısı gene uzak olsun"
"level atladıkça yeni makineler açılsın işte bira satılsın felan mağaza sistemini genişlet görevli alma getir mağaza kiişileştirme getir steamde ki tavern master oyununu incele biraz ona benzesin oynayış"

**Pitch:** Küçük bir yol kenarı meyhanesini, içecek taşıyarak, makine ve personel alarak, seviye seviye efsanevi bir hana büyüttüğün yatay ekranlı 3D oyuncak-taverna tycoon'u.

**Genre:** arcade-idle tycoon (taverna yönetimi)

**Controls:** Tek parmak, ekranda beliren sanal joystick ile Barlo'yu yürüt; almak, servis ve para toplama yaklaşınca otomatik. Butonlar: Mağaza, Görevler, Yükselt. İki parmak ile yakınlaştır.

**Question:** Servis temposu ve "hep bir sonraki hedef görünür" zinciri, oyuncuyu ilk 15 dakikada (Aşama 2'ye kadar) bırakmadan oynatıyor mu? *(inferred (strong))*

**Hooks:** Barlo'dan ödeme karesine akan paralarla duvarları yıkıp tavernayı büyütmek; koşturan tavernacıdan işi yöneten patrona dönüşmek; 30 farklı fantastik müşteri.

**Intent:** soft or no failure (GAME_DESIGN.md §1: "Kaybetme durumu yok") · payoff builds (GAME_DESIGN.md §1–3)

## Look and references
- Look: "Oyuncak Taverna": sıcak, yumuşak köşeli low-poly; tek palet atlası; iri kafalı küçük karakterler; ahşap/bal/kızıl tonlar, gece mavi-mor; imza detay: istasyonların Ahşap → Pirinç → Altın kademe değişimi (DESIGN_RULES.md).
- References: Tavern Master (Steam) — taverna yönetimi, makine/personel/dekor/yıldız katmanı; arcade-idle mobil oyunlar — ödeme kareleri ve otomatik toplama.
- View: 3D · Camera: üçüncü şahıs, uzak ve geniş açılı takip kamerası, yatay (landscape) ekran, iki parmakla yakınlaştırma.

## First minute
Oyuncu boş meyhanede Barlo ile başlar; ilk müşteri oturur ve bira baloncuğu çıkar. Joystick ile fıçıya yürür, bardak tepsiye gelir, masaya götürür; müşteri içip parayı masaya bırakır, Barlo toplar ve paralar sayaca uçar. "Sıradaki Hedef" oku Elma Presi ödeme karesini gösterir; ~30 saniyede 25 parayı döküp ilk makineyi açar, konfeti patlar.

## Challenge
İlk zorluk 3–5. dakikada: masalar dolar, iki içecek türü aynı anda istenir, sabır halkaları sararır. Kızgın müşteri parasını vermeden gider (ceza yok, kaçan kazanç var). 5–7 dakikada bir Yoğun Saat (45 sn, müşteri ×2, bahşiş ×2) tepe zorluktur. Sonra zorluk kalabalık, kirli masalar, maaşlar ve doğru personel/makine yatırımından gelir.

## Core loop
- Müşteri gelir → istek baloncuğu → istasyondan içecek al → masaya taşı → para topla.
- Para → makine, masa, alan, personel, dekor; Ün → taverna seviyesi → yeni içecek ve müşteri.
- Personel ve otomasyon arttıkça rol servis etmekten yönetmeye kayar; bir sonraki hedef hep görünür.

## Goal & fail state
- Hedef: tavernayı 5 aşamada (Yol Kenarı → Efsanevi Han) Sv30'a kadar büyütmek; aktif oyunda en geç 2–5 dakikada bir anlamlı satın alma.
- Kaybetme yok; bir oturum, bir hedef (ödeme karesi, seviye ya da görev) tamamlanınca tamam hissettirir.

## Features
- [x] Joystick hareketi, otomatik al/servis/topla, taşıma kapasitesi
- [x] Müşteri, sabır halkası, masa ve oturma sistemi; kirli masalar
- [x] Ödeme kareleri ve Sıradaki Hedef zinciri; Meyhane → Ana Salon → Bahçe alan açma
- [x] Seviye (Ün) sistemi ve seviyeyle açılan makineler/içecekler/müşteriler
- [x] Mağaza: Makineler, Personel (garson, barmen, temizlikçi, ozan; maaşlı), Dekor
- [x] Taverna yıldızı (★1–5), gün döngüsü ve gün sonu özeti, Yoğun Saat
- [x] Görevler, çevrimdışı kazanç, elmas boost'ları, istasyon yükseltme paneli
- [x] Ana menü, ayarlar, HUD, koleksiyon defteri (yatay UI v2)
- [ ] Düzenleme modu (sürükle-bırak, döndür)
- [ ] Aşama 3–5 alanları ve Sv13–30 içeriği

## Your systems
- otomatik içecek üretimi · in the slice
- taverna genişletme · in the slice
- garson sayısı artırımı / görevli alma · in the slice
- mağaza sistemi (makineler, personel, dekor) · in the slice
- level atladıkça yeni makineler · in the slice
- başlangıç menüsü (oyna, ayarlar, çıkış) · in the slice
- tavernayı istediği gibi düzenleme · later
- mağaza kişiselleştirme / dekor · thin stand-in

## Keep
-

## Avoid
- Dikey ekran ("oyun yatay olmalı dikey olmasın")
- Çok yakın kamera ("bakış açısı gene uzak olsun")
- Kutu dışına taşan yazı ve simgeler
- Kaybetme cezası, zorunlu reklam, ilerlemek için zorunlu satın alma *(GAME_DESIGN.md §1, §8.2)*

## Out of scope
- Prestij / Yeni Şube (Sv30) — ilk sürümde sadece "Yakında" ekranı
- İskelet animasyon seti (şimdilik iskeletsiz squash/hop)
- Gerçek müzik ve ses kayıtları (şimdilik kodla üretilen sesler)
- Çok oyunculu, sohbet, liderlik tablosu *(guessed)*

## Build order
1. Çekirdek servis döngüsü + Sıradaki Hedef zinciri (Meyhane, Sv1–5) — yapıldı.
2. Yönetim katmanı: mağaza, personel, dekor, yıldız, gün döngüsü — yapıldı.
3. Oyuncu testi geri bildirimi, UI düzeltmeleri, denge (/balance-check).
4. Düzenleme modu.
5. Aşama 3 (Kasaba Hanı, Sv13–20) içerik ve alanı.
6. Android derleme ve cihaz performans testi.
7. Aşama 4–5 ve prestij altyapısı.

---
**Kill condition:** İlk 15 dakikalık testte oyuncu Aşama 2'ye gelmeden sıkılıyorsa, tempo ve hedef zinciri yeniden tasarlanır. *(guessed)*

**Who it's for / what they feel:** Kısa mobil oturumlarda sürekli bir şey büyüttüğünü hissetmek isteyen tycoon/idle oyuncuları; rahat ama koşturmacalı. *(inferred (strong))*

**Games you love / don't:** Tavern Master (Steam) — oynayış ve yönetim katmanı.
