# Sorchemist: Asset Listesi ve Üretim Rehberi

## Nasıl çalışır?
Görseli `Assets/Sorchemist/Resources/SorchemistArt/` klasörüne **tablodaki dosya adıyla** koy.
Unity onu otomatik olarak Sprite olarak içe aktarır ve oyun hemen kullanır. Dosya yoksa oyun
kodla çizilen yer tutucuya döner; yani asset'leri **birer birer** ekleyebilirsin.

- Biçim: **PNG** (karakter, bina, nesne için **şeffaf arka plan**). Arka planlar JPG olabilir.
- Uzantı yazmadan dosya adı = anahtar. Örnek: `node_sulfur.png` dosyasının anahtarı `node_sulfur`.
- Şeffaf arka planı kendin çıkaramıyorsan görseli **düz beyaz arka planla** üret ve bana ver.
  Kesme, boyutlandırma ve adlandırma işini ben yaparım (Aqualf'i bu şekilde hazırladım).

## Görsel stil (tüm asset'lerde aynı olmalı)
Mevcut Aqualf görsellerin referans: **stilize 3D render (Pixar/DreamWorks tadında), sıcak hobbit
evi fantastik atmosferi, yumuşak sinematik ışık, cam/su parıltıları, kahve-bakır-turkuaz renk paleti.**

Her prompt'un sonuna şu stil bloğunu ekle:

> *Stylized 3D render, Pixar-like, cozy hobbit fantasy village, warm soft cinematic lighting,
> rich detail, vibrant but warm palette (copper, wood brown, turquoise, teal glow),
> game asset, centered, isolated on pure white background, no text, no watermark.*

Karakterlerde **Aqualf_front.jpg dosyasını referans görsel olarak yükle**, böylece tutarlılık korunur.

---

## Öncelik 1: Oyunun yüzünü değiştirenler (önce bunlar)

### Aqualf ifadeleri (şeffaf PNG, yaklaşık 800x1000, tam boy, öne dönük)
| Dosya | Durum | Ne zaman görünür | Prompt fikri |
|---|---|---|---|
| `aqualf_idle.png` | **HAZIR** | Bekleme | (Aqualf_front.jpg'den kesildi) |
| `aqualf_focus.png` | gerekli | Pişirirken | Aqualf leaning forward, squinting with concentration, goggles down over eyes, holding a stirring rod |
| `aqualf_happy.png` | gerekli | Başarılı iksir ("Eureka!") | Aqualf jumping with joy, arms raised, holding a glowing flask, big open smile, sparkles |
| `aqualf_shocked.png` | gerekli | Garip sonuçlar | Aqualf shocked, wide eyes, mouth open, hands up, small puff of colorful smoke |
| `aqualf_singed.png` | gerekli | Patlama, saç yanması | Aqualf after a small explosion, soot smudges, hat smoking and slightly burnt, dazed expression, comedic |
| `aqualf_glowing.png` | gerekli | Radyoaktif ve parlama sonuçları | Aqualf glowing bright green from inside, surprised happy face, green aura |

### Portreler (kare 512x512, diyalog kutusunda daire içinde görünür)
| Dosya | Durum | Kim |
|---|---|---|
| `portrait_aqualf.png` | **HAZIR** | Aqualf (portre.jpg'den kırpıldı) |
| `portrait_filiz.png` | gerekli | Bahçıvan Filiz: genç, güler yüzlü, saçında yaprak, toprak lekeli önlük |
| `portrait_bal.png` | gerekli | Bal Teyze: tombul, sıcak fırıncı kadın, un lekeli, bal rengi başörtüsü |
| `portrait_pirtik.png` | gerekli | Minik Pırtık: afacan çocuk, dağınık saç, çilli, yamalı tulum |
| `portrait_fistik.png` | gerekli | Fıstık Dede: yaşlı çiftçi, hasır şapka, beyaz bıyık, tırmık |
| `portrait_tokmak.png` | gerekli | Demirci Tokmak: iri, sakallı, deri önlük, isli yüz |
| `portrait_ebe.png` | gerekli | Ebe Nine: bilge yaşlı kadın, mor şal, gözlük, şifalı ot demeti |
| `portrait_fener.png` | gerekli | Bekçi Fener: uzun ince gece bekçisi, fener tutan, mavi pelerin |
| `portrait_zeytin.png` | gerekli | Kâhya Zeytin: köy kâhyası, zeytin yeşili yelek, defter, ciddi ama sevimli |
| `portrait_kopuk.png` | gerekli | Çamaşırcı Köpük: neşeli, kolları sıvalı, etrafında sabun köpükleri |
| `portrait_ruzgar.png` | gerekli | Değirmenci Rüzgâr: rüzgârda savrulan saç, un çuvalı, uçuşan atkı |

Prompt kalıbı: *"Portrait bust of [tarif], hobbit-like village character, friendly expression, same 3D style as reference..."*
(Köylüler insan/hobbit gibi; Aqualf su damlası. Hepsi aynı 3D stilde olmalı.)

### Laboratuvar
| Dosya | Durum | Boyut | Not |
|---|---|---|---|
| `bg_lab.jpg` | **GEÇİCİ** | 1600x1700 civarı (dikeye yakın) | Şu an web sitendeki laboratuvar resmi; 3D hobbit stilinde yenisi lazım: *cozy round hobbit-hole alchemy lab interior, shelves of glowing potions, wooden beams, warm candle light, empty center floor for a character* (arka plan, beyaz zemin değil) |
| `cauldron.png` | gerekli | 800x700 şeffaf | Yandan görünüm bakır/dökme demir kazan, ağzı hafif yukarıdan görünür, içi boş |
| `cauldron_top.png` | gerekli | 1024x1024 şeffaf | Karıştırma oyunu için **tam üstten** yuvarlak kazan ağzı, içi boş |
| `flame.png` | gerekli | 256x384 şeffaf | Stilize turuncu alev (oyun mor alev için renklendirir) |
| `burner.png` | gerekli | 300x260 şeffaf | Pirinç Bunsen beki / küçük ocak |

## Öncelik 2: Köy haritası

### İzometrik zemin karoları (şeffaf PNG, **256x128**, tam elmas şeklinde, 2:1)
| Dosya | Not |
|---|---|
| `tile_grass.png` | Çimen |
| `tile_grass_b.png` | Biraz farklı tonda çimen (dama deseni için; yoksa tile_grass kullanılır) |
| `tile_path.png` | Toprak/taş patika |
| `tile_water.png` | Deniz kıyısı suyu |

Prompt: *"single isometric 2:1 diamond ground tile, [grass/dirt path/shallow sea water], top-down isometric, seamless edges"*

### Binalar (şeffaf PNG, yaklaşık 512x512, izometrik açı, tabanı alt orta)
| Dosya | Not |
|---|---|
| `lab_tower.png` | Aqualf'in kulesi: yuvarlak kapılı, turkuaz kubbeli, bacasından renkli duman çıkan simya kulesi (512x660) |
| `house_default.png` | Tüm köylüler için tek ev (en hızlı başlangıç) |
| `house_filiz.png` ... `house_ruzgar.png` | İstersen her köylüye özel ev: bahçeli, fırın bacalı, demirhaneli, yel değirmenli... |

Prompt: *"isometric cozy hobbit house with round door built into a small hill, [özellik], game building asset"*

### Toplama noktaları (şeffaf PNG, 256x256)
| Dosya | Ne |
|---|---|
| `node_seawater.png` | İskele ve deniz suyu kovası |
| `node_air.png` | Tepedeki mantar kapaklı cam kavanoz |
| `node_limestone.png` | Beyaz kireç taşı kayası |
| `node_ash.png` | Kül yığını, sönmüş ocak |
| `node_hematite.png` | Kırmızımsı demir cevheri kayası |
| `node_sulfur.png` | Sarı kükürt kristalli volkanik kaya |
| `node_malachite.png` | Yeşil bantlı malahit kayası |
| `node_sand.png` | Altın kum öbeği, nehir kıyısı |
| `node_magnesite.png` | Bembeyaz parlak mineral kaya |
| `node_guano.png` | Yarasa mağarası girişi (komik, sevimli) |
| `node_dewcap.png` | Çiy damlalı mor mantarlar |
| `node_crystal.png` | Taş çember içinde süzülen mor gizemli kristal |

| Dosya | Not |
|---|---|
| `bg_village.jpg` | 2560x1440, haritanın arkası: uzakta tepeler, gökyüzü, orman kenarı (opsiyonel) |

## Öncelik 3: Viralite ve mağaza (sonraki adımda koda bağlayacağım)
- **Fail-Dex kart illüstrasyonları** (`faildex_<kimlik>.png`, 600x600): 25 adet. En önemlileri: `alkali_boom` (saçı yanan Aqualf),
  `peroxide_foam` (fil diş macunu köpüğü), `roof_blown` (uçan çatı), `giant_pumpkins`, `frog_villager`, `midas`.
- **Köy durum süsleri**: dev kabaklar, kurbağa köylü, buz kaplı ev, köpük, pembe boya lekesi, havai fişek isi.
- **İksir şişeleri** (`potion_<bileşik>.png`, 256x256): 17 bileşik için şişe. Tek tip şişe + renk de yeter.
- **UI kiti**: ahşap/bakır çerçeveli panel, buton (normal/basılı), sekme, ilerleme çubuğu, para ve XP ikonu.
- **Yazı tipi**: Türkçe karakter destekli, yuvarlak ve sıcak bir font (ör. *Baloo 2*, *Fredoka*, *Nunito*; hepsi ücretsiz, Google Fonts).
- **Ses**: gerçek ASMR kayıtları (fokurdama, cam şıngırtısı, köpük, patlama, sayfa çevirme) ve hafif bir ana müzik döngüsü.
- **Play Store**: uygulama ikonu **512x512** (Aqualf yüzü + kazan), öne çıkan görsel **1024x500**,
  en az 4 yatay ekran görüntüsü (**1920x1080**), 15-30 sn tanıtım videosu (Sorchemist1.mp4 stilinde).

## Toplam (asgari, oyunun "bitmiş" görünmesi için)
| Grup | Adet |
|---|---|
| Aqualf ifadeleri | 5 (+1 hazır) |
| Köylü portreleri | 10 |
| Laboratuvar | 5 (bg_lab dahil) |
| Karolar | 3-4 |
| Binalar | 2 (lab_tower + house_default) |
| Toplama noktaları | 12 |
| **Toplam** | **yaklaşık 38 görsel** |

## Üretim ipuçları
1. **Önce stil tutarlılığı:** Aynı aracı (ör. Gemini / Imagen, Midjourney) ve aynı stil bloğunu kullan; karakterlerde referans görsel yükle.
2. **Beyaz arka plan:** Bina, nesne ve karakterleri düz beyaz arka planla üret; şeffaflığı ben çıkarırım.
3. **İzometrik açı:** Karo ve binalarda "isometric 2:1, 30 degree" ifadesini kullan; hepsi aynı açıda olmalı.
4. **Toplu teslim:** Görselleri bir klasöre at, istediğin adla olabilir. Bana söyle; keser, boyutlandırır,
   doğru adla `SorchemistArt` klasörüne koyar ve oyunda kontrol ederim.
