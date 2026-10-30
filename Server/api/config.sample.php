<?php
// Bu dosyayı "config.php" adıyla kopyala ve kendi bilgilerinle doldur.
// config.php ASLA GitHub'a yüklenmez (.gitignore) ve tarayıcıdan okunamaz (.htaccess).
return [
    // Hostinger hPanel > Veritabanları > MySQL Veritabanları ekranındaki bilgiler
    'db_host' => 'localhost',
    'db_name' => 'u000000000_sorchemist',
    'db_user' => 'u000000000_sorchemist',
    'db_pass' => 'BURAYA_VERITABANI_SIFRESI',

    // Google ile giriş: Google Cloud Console'daki OAuth "Web application" istemci kimliği
    // (oyunun Android uygulaması da aynı kimlikle jeton alır). Şimdilik boş kalabilir.
    'google_client_ids' => [
        // '1234567890-abcdefg.apps.googleusercontent.com',
    ],

    // install.php'yi bir kez çalıştırmak için uzun, rastgele bir anahtar. Kurulumdan sonra '' yap.
    'install_key' => 'BURAYA_UZUN_RASTGELE_BIR_ANAHTAR',
];
