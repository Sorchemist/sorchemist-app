# Sorchemist — Adım Adım Kurulum Rehberi (Yeni Başlayanlar İçin)

Bu rehber seni sıfırdan "telefonda çalışan oyun"a ve oradan Google Play'e götürür.
Her adımı sırayla yap; bir adım bitmeden sonrakine geçme.

---

## Adım 1 — Unity'yi kur (bir kerelik, ~30-60 dk)

1. **Unity Hub**'ı indir ve kur: https://unity.com/download
2. Unity Hub'ı aç → sol menüden **Installs** → **Install Editor**.
3. **Unity 6.3 LTS** sürümünü seç (LTS = uzun süre desteklenen, en kararlı sürüm).
4. Modül seçim ekranında şunları **işaretle**:
   - [x] **Android Build Support**
     - [x] OpenJDK
     - [x] Android SDK & NDK Tools
   - (Windows'taysan) [x] Microsoft Visual Studio Community — kod editörü olarak.
5. **Install**'a bas ve bitmesini bekle.

> İpucu: Unity hesabı istenirse ücretsiz "Personal" lisans yeterli.

## Adım 2 — Repoyu VS Code ile bilgisayarına indir (clone)

1. VS Code'da **Ctrl+Shift+P** (Mac: Cmd+Shift+P) → `Git: Clone` yaz → Enter.
2. Adres olarak yapıştır: `https://github.com/Sorchemist/sorchemist-app.git`
3. İndirilecek klasörü seç (ör. `Belgeler/Projeler`) → **Open** ile klasörü aç.
4. Sağ altta "önerilen eklentiler" sorulursa **Install** de (Unity ve C# desteği).

> Git kurulu değilse VS Code seni https://git-scm.com adresine yönlendirir; kur ve VS Code'u yeniden başlat.

## Adım 3 — Projeyi Unity'de aç

1. Unity Hub → **Projects** → **Add** (veya *Open*) → **Add project from disk**.
2. Az önce indirdiğin `sorchemist-app` klasörünü seç.
3. Hub, sürüm farkı uyarısı verirse kurulu **Unity 6.3** sürümünü seç → **Open with ...**.
4. İlk açılış birkaç dakika sürer: Unity `Library` klasörünü ve eksik proje ayarlarını üretir,
   kodları derler. **Console** penceresinde (Window → General → Console) kırmızı hata olmamalı.
5. Unity, her dosya için `.meta` dosyaları oluşturur. Bunları **commit'le ve push'la**
   (VS Code → sol menü *Source Control* → mesaj yaz → **Commit** → **Sync Changes**).
   `Library`, `Temp`, `.apk/.aab` ve keystore dosyaları `.gitignore` sayesinde yüklenmez.

> `Tests` klasörü Unity dışında çalışır; GitHub'a her push'ta otomatik çalışan bir test de var
> (repo sayfasında **Actions** sekmesi).

## Adım 4 — Oyna!

1. Herhangi bir sahne açıkken (varsayılan "SampleScene" olur) üstteki **Play**'e bas.
2. Oyun kendini otomatik kurar: köy haritası, Aqualf'in tanıtımı, laboratuvar...
   Sahneye hiçbir şey sürüklemene gerek yok.
3. Telefon görünümü için: *Game* penceresinin üstündeki çözünürlük menüsünden
   **1080x1920 Portrait** seç (yoksa **+** ile ekle).

**İlk oyun döngüsü (test et):**
1. Haritada **Bahçıvan Filiz**'in evindeki **!** işaretine (veya üstteki mor afişe) dokun → derdini dinle.
2. **Laboratuvar** → **Ayrıştır** sekmesi → *Çiy Mantarı* → **Ayrıştır** → ısıt butonunu basılı tutarak imleci yeşil bantta tut.
3. **Kazan** sekmesi → 2 × **H**, 1 × **O** ekle → **PİŞİR!**
4. Isı oyununda **Soğuk** veya **Ilık** bölgede 2 sn kal → kazanı karıştır.
5. "Su (H2O) oluştu!" → **Köy** → Filiz'e **İksir Ver**.
6. Sonra bilerek hata yap: örneğin sodyumlu karışım + su… ve **Fail-Dex**'i doldurmaya başla.

## Adım 5 — Kodlar nerede, ne yapıyor?

```
Assets/Sorchemist/Scripts/
├── Core/   ← Oyunun KURALLARI (Unity'den bağımsız, saf C#)
│   ├── Elements.cs      118 element (Türkçe adlar, kütleler, periyodik tablo konumu)
│   ├── Compounds.cs     17 gerçek bileşik + iksir etkileri + ideal ısı
│   ├── RawMaterials.cs  Haritadaki ham maddeler ve ayrıştırma yöntemleri
│   ├── ReactionEngine.cs Kazandaki karışımı değerlendirir (başarı / komik başarısızlık)
│   ├── FailDex.cs       25 başarısızlık girdisi (komik + "Bilim köşesi" açıklaması)
│   ├── Villagers.cs     10 köylü, 14 görev, yanlış iksirin köydeki sonuçları
│   ├── SkillTree.cs     Bilim & Büyü yetenek ağacı
│   ├── PlayerState.cs   Kaydedilen her şey (envanter, seviye, harita durumu)
│   └── GameSession.cs   Tüm kuralları birleştiren "beyin"
├── Game/   ← Unity servisleri (başlatma, kayıt, ses, paylaşım)
├── UI/     ← Ekranlar, minioyunlar, efektler (hepsi kodla kuruluyor)
└── Editor/ ← Unity menüsüne "Sorchemist" menüsü ekler
```

**İçerik eklemek çok kolay** (kod bilgisi gerektirmez, kopyala-yapıştır):
- Yeni bileşik: `Compounds.cs` içinde bir `Add("id", "Formül", "Ad", ...)` satırı kopyala.
- Yeni görev: `Villagers.cs` içinde bir `Q(...)` bloğunu kopyala, metni değiştir.
- Yeni başarısızlık: `FailDex.cs` içinde bir `Add(...)` satırı kopyala.

## Adım 6 — Telefonda dene (Android)

1. Telefonunda **Geliştirici Seçenekleri**'ni aç (Ayarlar → Telefon hakkında → "Yapım numarası"na 7 kez dokun).
2. Geliştirici seçeneklerinden **USB hata ayıklama**'yı aç, telefonu USB ile bağla.
3. Unity → üst menü **Sorchemist → 1) Android ve Play Store Ayarlarını Uygula**.
   (Paket adı, 64-bit, IL2CPP, dikey ekran, .aab ayarlarını otomatik yapar.)
4. **File → Build Profiles** (eski sürümlerde *Build Settings*) → **Android** → **Switch Platform**.
5. Test için: *Build App Bundle* kutusunu geçici olarak **kapat** → **Build And Run**.
   APK telefona yüklenip açılır.

## Adım 7 — Play Store için imzalı .aab üret

1. **Edit → Project Settings → Player → Android sekmesi → Publishing Settings**.
2. **Keystore Manager** → **Create New** → bir dosya adı ve **güçlü şifre** gir.
   DİKKAT: Bu keystore dosyasını ve şifresini **kaybetme**, yedekle! (Google Play App Signing
   kullanacağın için kaybedersen "upload key" sıfırlanabilir ama uğraştırır.)
3. *Build App Bundle* kutusunu tekrar **aç** → **Build**. Ortaya `Sorchemist.aab` çıkar.
4. Her yeni yüklemede `Sorchemist/Editor/SorchemistMenu.cs` içindeki
   `bundleVersionCode` sayısını 1 artır (ya da Player ayarlarından elle artır).

## Adım 8 — (İsteğe bağlı) Gerçek paylaşım menüsü: NativeShare

Oyundaki **Paylaş** butonu şu an ekran görüntüsünü kaydeder ve metni panoya kopyalar.
Instagram/TikTok/WhatsApp paylaşım menüsünü açmak için:
1. **Window → Package Manager → + → Install package from git URL…**
2. Şunu yapıştır: `https://github.com/yasirkula/UnityNativeShare.git` → **Install**.
3. Unity menüsü **Sorchemist → 2) NativeShare Paylaşımını Etkinleştir**.

## Adım 9 — Google Play Console

1. https://play.google.com/console → geliştirici hesabı aç (tek seferlik **25 $**).
2. **Uygulama oluştur** → ad: Sorchemist, tür: Oyun, Ücretsiz.
3. Kişisel (bireysel) yeni hesaplarda kural: üretime çıkmadan önce **kapalı testte en az
   12 test kullanıcısı, 14 gün kesintisiz** katılmalı. Arkadaşlarını/takipçilerini şimdiden topla!
4. Doldurman gerekenler: mağaza açıklaması, ikon (512×512), öne çıkan görsel (1024×500),
   en az 2 ekran görüntüsü, **gizlilik politikası** linki, içerik derecelendirme anketi,
   hedef kitle (çocuklara yönelikse "Aileler" politikalarına dikkat), veri güvenliği formu.
5. `.aab` dosyasını **Kapalı test** kanalına yükle → test kullanıcılarını ekle → 14 gün sonra
   **Üretim erişimi başvurusu** yap.

> Not: Google Play 31 Ağustos 2026'dan beri yeni uygulamalarda **hedef API 36 (Android 16)**
> istiyor. Menü ayarımız "en yüksek yüklü API"yi seçer; Unity 6.3 LTS ile güncel SDK'yı
> kurduğunda bu otomatik sağlanır. Yüklemeden önce Play Console'un uyarı vermediğini kontrol et.

## Sorun giderme

| Sorun | Çözüm |
|---|---|
| Console'da `InputSystemUIInputModule` hatası | Package Manager'dan **Input System** paketini kur, ya da Project Settings → Player → *Active Input Handling* = **Both** |
| Yazılar görünmüyor | Unity'yi yeniden başlat; hâlâ yoksa Console'u kontrol et ve hatayı bana gönder |
| Play'e basınca boş mavi ekran | Console'da kırmızı hata var demektir — ilk hatayı kopyalayıp sor |
| Kaydı sıfırlamak | Oyunda **Ayar → Kaydı Sıfırla** veya menü **Sorchemist → Kayıt Dosyasını Sil** |

Takıldığın her yerde Console'daki **ilk kırmızı satırı** kopyalayıp bana gönder; birlikte çözeriz.
