# Sorchemist Sunucusu – Hostinger Kurulumu

Oyunun çevrimiçi özellikleri (hesap, bulut kaydı, arkadaşlar, hediye/takas, haftalık sıralama, loncalar)
`https://sorchemist.com/api/api.php` adresindeki küçük bir PHP + MySQL servisiyle çalışır.
Dosyalar bu klasörde: `Server/api/`.

## 1. Veritabanını oluştur (hPanel)
1. hPanel → **Veritabanları → MySQL Veritabanları**.
2. Yeni veritabanı: ad `sorchemist`, kullanıcı `sorchemist`, güçlü bir şifre (şifreyi bir yere not et).
3. Hostinger adları öne ek koyar (ör. `u123456789_sorchemist`). Ekranda yazan **tam** adları not et.

## 2. Dosyaları yükle (hPanel → Dosya Yöneticisi)
1. `public_html` içinde `api` adında bir klasör aç.
2. `Server/api` klasöründeki dosyaları (`api.php`, `lib.php`, `anticheat.php`, `install.php`, `config.sample.php`, `.htaccess`) bu klasöre yükle.
   (Proje klasöründeki `sorchemist-api.zip` dosyasını yükleyip "Extract/Çıkart" demek de olur.)
3. `config.sample.php` dosyasının bir kopyasını `config.php` adıyla oluştur ve düzenle:
   - `db_name`, `db_user`: 1. adımdaki tam adlar
   - `db_pass`: veritabanı şifren
   - `install_key`: uzun, rastgele bir metin (ör. 40 karakter)

## 3. Tabloları kur
Tarayıcıda bir kez aç: `https://sorchemist.com/api/install.php?key=<install_key>`
`{"ok":true,"installed":true,"driver":"mysql"}` görmelisin. Sonra `config.php` içinde `install_key` değerini `''` yap
(ya da `install.php` dosyasını sil).

## 4. Kontrol
`https://sorchemist.com/api/api.php` adresini açınca `{"ok":true,"service":"sorchemist","version":1}` görünmeli.
`https://sorchemist.com/api/config.php` adresi **403 (yasak)** vermeli.

## 5. SSL
hPanel → **Güvenlik → SSL** bölümünde sorchemist.com için SSL'in açık olduğundan emin ol (oyun yalnızca https ile bağlanır).

## Google ile giriş (isteğe bağlı, sonra yapılabilir)
1. Google Cloud Console → yeni proje → **APIs & Services → Credentials**.
2. **OAuth consent screen**'i doldur (uygulama adı Sorchemist, destek e-postası).
3. **Create credentials → OAuth client ID → Web application** → oluşan istemci kimliğini (`...apps.googleusercontent.com`)
   `config.php` içindeki `google_client_ids` listesine ekle.
4. Aynı ekranda bir **Android** istemci kimliği de oluştur: paket adı `com.sorchemist.aqualf`, imza anahtarının SHA-1 parmak izi
   (Play Console → Uygulama bütünlüğü → Uygulama imzalama anahtarı sertifikası).
5. Unity'ye bir Google Sign-In eklentisi (ör. Credential Manager / Google Sign-In Unity eklentisi) eklenince,
   eklentinin verdiği **ID token** `CloudService.GoogleIdTokenProvider` üzerinden sunucuya gönderilir. Sunucu jetonu Google'a doğrulatır.

## Güvenlik notları
- Şifreler yalnızca `password_hash` (bcrypt) özeti olarak saklanır; oturum jetonları sunucuda SHA-256 özeti olarak tutulur.
- `config.php`, `lib.php` tarayıcıdan okunamaz (`.htaccess`). `config.php` GitHub'a yüklenmez (`.gitignore`).
- Giriş/kayıt/istek sayıları IP ve kullanıcı başına sınırlandırılır.
- Oyuncular **Hesabım → Hesabı Sil** ile hesaplarını ve bütün verilerini silebilir (Google Play gereği).

## Hile koruması
Oyun cihazda oynanır, ama sunucu her bulut kaydını **bir önceki geçerli kayıtla** karşılaştırır (`anticheat.php`):
- **Zaman sınırları:** aradan geçen gerçek süre (sunucu saati) içinde mümkün olandan fazla altın, XP, Bilgi Puanı, görev,
  iksir, gizemli kutu ya da bina seviyesi kazanılmışsa kayıt `save_rejected` ile reddedilir. Sınırlar cömerttir; dürüst oyuncu takılmaz.
  Telefonun saatini ileri almak işe yaramaz, çünkü sunucu kendi saatini kullanır.
- **Tutarlılık:** bina seviyeleri 1–12 ve kulenin en fazla 2 üstü, görev/Fail-Dex/bileşik/pasif kimlikleri gerçek olmalı, tekrar yok.
- **Sıralama ve köy vitrini** artık cihazın gönderdiği sayılardan değil, doğrulanmış kayıttan sunucuda hesaplanır.
- **Defter (ledger):** hediye, takas ve lonca bağışı/ödülü sunucudaki `ledger` tablosuna yazılır. Oyuncu ancak son geçerli kaydında
  gerçekten olan iksiri verebilir (`not_enough_potions`); verdiği iksiri cihazında düşmezse sonraki kaydı reddedilir.
- **Yerel kayıt imzası:** cihazdaki kayıt dosyası elle düzenlenirse imza tutmaz, sunucu o kaydı kabul etmez.
- **Yaptırım:** her ret `cheat_log` tablosuna yazılır (saatte en çok bir ihlal sayılır). 3 ihlalde hesap işaretlenir (`users.flagged = 1`):
  sıralamada görünmez, hediye/takas/lonca bağışı yapamaz. Yanlışlıkla işaretlenen birini phpMyAdmin'den
  `UPDATE users SET flagged = 0, cheat_strikes = 0 WHERE id = ...` ile affedebilirsin.
- Son 10 kayıt sürümü `save_history` tablosunda tutulur.

> **Güncelleme yaparken:** yeni dosyaları yükledikten sonra `install.php`'yi bir kez daha çalıştır (yeni tabloları ve sütunları ekler,
> mevcut verilere dokunmaz). `config.php` içinde `allow_test_clock` **yoksa ya da false ise** sorun yok; gerçek sunucuda asla true yapma.

## Kapasite
Kayıtlar gzip ile sıkıştırılır: oyunu bitirmiş bir oyuncunun kaydı ~6 KB, yeni bir oyuncunun ~1–2 KB. 50.000 oyuncu için toplam
veritabanı en kötü durumda ~0,3–0,6 GB (Hostinger web planlarındaki 3 GB veritabanı sınırının çok altında).

## Yerel test
```
cd Server/api
cp config.sample.php config.php   # içinde 'db_dsn' => 'sqlite:/tmp/sorchemist.sqlite', 'install_key' => 'test', 'allow_test_clock' => true
php -S 127.0.0.1:8765 &
curl "http://127.0.0.1:8765/install.php?key=test"
python3 ../tests/api_test.py
```
