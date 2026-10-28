# Sorchemist — Play Store'a Giden Yol Haritası

Bu iskelet (v0.1) oyunun **çalışan çekirdek döngüsünü** içerir. Aşağıdaki sırayla ilerlemek,
en riskli soruyu ("Bu oyun eğlenceli mi?") en ucuza cevaplamanı sağlar.

## v0.1 — Çekirdek iskelet (BU PAKET)
- [x] 118 element, Türkçe adlar, periyodik tablo ekranı
- [x] 17 gerçek bileşik (H2O, NaCl, NH3, KNO3, CuSO4, NaHCO3, H2O2, MgO, CaO, NH4NO3...)
- [x] 12 ham madde + 7 gerçek ayrıştırma yöntemi (elektroliz, damıtma, kalsinasyon...)
- [x] Reaksiyon motoru: oran + ısı + karıştırma; "neredeyse!" ipuçları
- [x] 3 dokunmatik minioyun: Bunsen beki (ısı), karıştırma, damıtma
- [x] 10 köylü, 14 diyaloglu görev, yanlış iksirin haritadaki sonuçları (kabak istilası, kurbağa, buz evi...)
- [x] Fail-Dex: 25 komik başarısızlık + her birinde "Bilim köşesi"
- [x] Bilim / Büyü yetenek ağacı (10 yetenek)
- [x] Prosedürel ASMR sesleri, kabarcık/duman/patlama efektleri, titreşim
- [x] Ekran görüntüsüyle paylaşım (NativeShare ile gerçek paylaşım menüsü)
- [x] Otomatik kayıt, çentikli ekran desteği, Android/Play ayar menüsü

## v0.2 — "Eğlenceli mi?" testi (1-2 hafta)
- 5-10 kişiye oynat (lise öğrencileri ideal). Gözlemle, müdahale etme.
- Ölç: İlk 10 dakikada kaç kişi Fail-Dex'e bilerek girdi topladı? Kaç kişi "paylaş"a bastı?
- Isı/karıştırma sürelerini, ödülleri ve XP eğrisini ayarla (`GameSession.cs`, `PlayerState.XpToNext`).

## v0.3 — Görsel kimlik (en büyük viralite kaldıracı)
- Aqualf karakter tasarımı: `AqualfView.cs` yer tutucusunun yerine Animator'lü sprite/3D model.
  Mevcut `React()` çağrıları korunur — sadece animasyon tetikleyicilere bağlanır.
  Öncelikli mimikler: Eureka, şok, saç yanması, parlayan sakal, altın şaşkınlığı.
- İzometrik köy için tile set (Unity **Tilemap → Isometric**) — `VillageScreen` şu an UI tabanlı;
  sanat gelince Tilemap'e taşımak kolay.
- Sıvı/kazan efektleri: Unity Particle System veya VFX Graph; shader ile renk geçişleri.
- Gerçek ASMR sesleri kaydet (köpük, cam şıngırtısı, fokurdama) → `AudioFX.cs` klipleri.
- Yazı tipi: TextMeshPro + Türkçe karakter destekli bir font (ör. Nunito, Baloo 2).

## v0.4 — Viral motor
- **Kısa video paylaşımı**: son 5-8 saniyeyi kaydedip Reels/TikTok'a hazır dikey video
  (seçenekler: NatCorder gibi bir kayıt eklentisi; ya da kısa animasyonlu GIF).
- Fail-Dex kartlarını "koleksiyon kartı" tasarımına çevir (nadirlik çerçeveleri, Aqualf pozu).
- Haftalık "Gizli Formül" etkinliği: herkese aynı ipucu, ilk bulanlar sosyal medyada.
- @sorchemist hesabındaki animasyon dilini oyun içi kesitlerde kullan.

## v0.5 — İçerik derinliği
- 40+ bileşik, çok adımlı tarifler (ör. CaCO3 → kalsinasyon → CaO → + su → Ca(OH)2).
- Titrasyon minioyunu (asit-baz, damla damla, renk dönüşümü — fenolftalein pembesi!).
- Gün/gece döngüsü, mevsimler, köy festivalleri.
- Lise müfredatına eşleme: her bileşik için konu etiketi (öğretmenler için "sınıf modu").

## v0.6 — Gelir modeli (oyunu bozmadan)
- Önerilen: **ücretsiz + kozmetik satın alımlar** (kazan görünümleri, alev renkleri,
  Aqualf kostümleri) ve isteğe bağlı **ödüllü reklam** (ör. "kaynak yenilenmesini hızlandır").
- Unity IAP / LevelPlay. Çocuklara yönelik konumlanırsan Google Play "Aileler" politikası
  reklam ağlarını kısıtlar — hedef kitleyi baştan net seç (13+ önerilir).

## v1.0 — Yayın
- Kapalı test: **en az 12 test kullanıcısı, 14 gün** (yeni bireysel hesaplar için zorunlu).
- Mağaza sayfası: kısa video, 4-8 ekran görüntüsü (en komik Fail-Dex anları!), TR + EN açıklama.
- Gizlilik politikası sayfası, veri güvenliği formu, içerik derecelendirmesi.
- Lokalizasyon: İngilizce (metinler şu an kodda; v0.5'te Unity Localization paketine taşı).
- Firebase Analytics / Crashlytics ile çökme ve döngü takibi.

## Teknik borç notları
- Metinler kod içinde → ileride `Localization` tablolarına taşınmalı.
- UI kodla kuruluyor (hızlı prototip için ideal) → sanat netleşince önemli ekranları prefab'a çevir.
- `Core/` klasörü Unity'den bağımsız; `Tests/` içindeki testler `dotnet run` ile çalışır.
