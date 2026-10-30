<?php
// Sorchemist sunucusu - ortak yardımcılar (veritabanı, JSON, oturum, sınırlar)
// Bu dosyaya tarayıcıdan doğrudan erişilemez (.htaccess).

declare(strict_types=1);

if (!defined('SORCHEMIST')) { http_response_code(403); exit; }

const API_VERSION = 1;
const MAX_SAVE_BYTES = 262144;        // 256 KB (sıkıştırılmış kayıt)
const SESSION_DAYS = 90;
const GIFTS_PER_DAY = 5;
const MAX_FRIENDS = 100;
const MAX_GUILD_MEMBERS = 30;

function cfg(string $key, $default = null)
{
    static $c = null;
    if ($c === null) {
        $file = __DIR__ . '/config.php';
        if (!file_exists($file)) fail('server_not_configured', 500);
        $c = require $file;
    }
    return $c[$key] ?? $default;
}

function db(): PDO
{
    static $pdo = null;
    if ($pdo) return $pdo;
    $dsn = cfg('db_dsn');
    if (!$dsn) $dsn = 'mysql:host=' . cfg('db_host', 'localhost') . ';dbname=' . cfg('db_name') . ';charset=utf8mb4';
    $pdo = new PDO($dsn, cfg('db_user'), cfg('db_pass'), [
        PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION,
        PDO::ATTR_DEFAULT_FETCH_MODE => PDO::FETCH_ASSOC,
        PDO::ATTR_EMULATE_PREPARES => false,
    ]);
    if (is_sqlite()) $pdo->exec('PRAGMA foreign_keys = ON');
    return $pdo;
}

function is_sqlite(): bool { return db_driver() === 'sqlite'; }
function db_driver(): string { static $d = null; if ($d === null) { $dsn = cfg('db_dsn') ?: 'mysql:'; $d = strtolower(strstr($dsn, ':', true)); } return $d; }

function q(string $sql, array $args = []): PDOStatement
{
    $st = db()->prepare($sql);
    $st->execute($args);
    return $st;
}
function row(string $sql, array $args = []): ?array { $r = q($sql, $args)->fetch(); return $r === false ? null : $r; }
function rows(string $sql, array $args = []): array { return q($sql, $args)->fetchAll(); }
function val(string $sql, array $args = []) { $r = q($sql, $args)->fetchColumn(); return $r === false ? null : $r; }

/** Sunucu saati. Testlerde (yalnızca config'te allow_test_clock açıksa) X-Test-Now başlığıyla ileri alınabilir. */
function now(): int
{
    static $t = null;
    if ($t === null) {
        $t = time();
        $h = $_SERVER['HTTP_X_TEST_NOW'] ?? '';
        if ($h !== '' && cfg('allow_test_clock', false) === true && ctype_digit($h)) $t = (int)$h;
    }
    return $t;
}
function week_key(?int $t = null): int { $t = $t ?? now(); return (int)gmdate('o', $t) * 100 + (int)gmdate('W', $t); }   // 202644 gibi
function day_key(?int $t = null): int { return (int)gmdate('Ymd', $t ?? now()); }

// ---------- JSON yanıtları ----------
function out(array $data = []): void
{
    header('Content-Type: application/json; charset=utf-8');
    header('Cache-Control: no-store');
    echo json_encode(['ok' => true] + $data, JSON_UNESCAPED_UNICODE);
    exit;
}

function fail(string $code, int $http = 400, array $extra = []): void
{
    http_response_code($http);
    header('Content-Type: application/json; charset=utf-8');
    echo json_encode(['ok' => false, 'error' => $code] + $extra, JSON_UNESCAPED_UNICODE);
    exit;
}

function input(): array
{
    static $in = null;
    if ($in !== null) return $in;
    $raw = file_get_contents('php://input', false, null, 0, MAX_SAVE_BYTES * 2 + 8192);
    $in = json_decode($raw ?: '{}', true);
    if (!is_array($in)) fail('bad_json');
    return $in;
}

function str_in(string $key, int $max = 200, bool $required = true): string
{
    $v = input()[$key] ?? null;
    if ($v === null || $v === '') { if ($required) fail('missing_' . $key); return ''; }
    if (!is_string($v)) fail('bad_' . $key);
    $v = trim($v);
    if (mb_strlen($v) > $max) fail('too_long_' . $key);
    return $v;
}

function int_in(string $key, int $min = 0, int $max = PHP_INT_MAX, ?int $default = null): int
{
    $v = input()[$key] ?? $default;
    if ($v === null || !is_numeric($v)) fail('missing_' . $key);
    $v = (int)$v;
    if ($v < $min || $v > $max) fail('bad_' . $key);
    return $v;
}

function id_in(string $key): string
{
    $v = str_in($key, 40);
    if (!preg_match('/^[a-z0-9_]{1,40}$/', $v)) fail('bad_' . $key);
    return $v;
}

// ---------- Güvenlik ----------
function client_ip(): string { return substr($_SERVER['REMOTE_ADDR'] ?? '0.0.0.0', 0, 45); }

/** Basit hız sınırı: aynı anahtar (ör. ip+işlem) için pencere içinde en fazla $limit istek. */
function rate_limit(string $key, int $limit, int $windowSec): void
{
    $bucket = intdiv(now(), $windowSec);
    $k = substr(hash('sha256', $key . '|' . $bucket), 0, 40);
    $n = (int)val('SELECT hits FROM rate_limits WHERE k = ?', [$k]);
    if ($n >= $limit) fail('rate_limited', 429);
    if ($n === 0) {
        q('INSERT INTO rate_limits (k, hits, expires_at) VALUES (?, 1, ?)', [$k, now() + $windowSec]);
        if (mt_rand(1, 50) === 1) q('DELETE FROM rate_limits WHERE expires_at < ?', [now()]);
    } else {
        q('UPDATE rate_limits SET hits = hits + 1 WHERE k = ?', [$k]);
    }
}

function new_token(): string { return rtrim(strtr(base64_encode(random_bytes(32)), '+/', '-_'), '='); }
function token_hash(string $t): string { return hash('sha256', $t); }

function issue_session(int $userId): string
{
    $t = new_token();
    q('INSERT INTO sessions (token_hash, user_id, created_at, expires_at) VALUES (?, ?, ?, ?)',
        [token_hash($t), $userId, now(), now() + SESSION_DAYS * 86400]);
    q('UPDATE users SET last_seen = ? WHERE id = ?', [now(), $userId]);
    return $t;
}

/** Geçerli oturumun kullanıcısını döndürür, yoksa 401. */
function auth(): array
{
    $h = $_SERVER['HTTP_AUTHORIZATION'] ?? $_SERVER['REDIRECT_HTTP_AUTHORIZATION'] ?? '';
    $t = '';
    if (stripos($h, 'Bearer ') === 0) $t = trim(substr($h, 7));
    if ($t === '') $t = (string)(input()['token'] ?? '');
    if ($t === '') fail('not_logged_in', 401);
    $u = row('SELECT u.* FROM sessions s JOIN users u ON u.id = s.user_id WHERE s.token_hash = ? AND s.expires_at > ?',
        [token_hash($t), now()]);
    if (!$u) fail('session_expired', 401);
    if ((int)$u['last_seen'] < now() - 300) q('UPDATE users SET last_seen = ? WHERE id = ?', [now(), $u['id']]);
    return $u;
}

// ---------- Kullanıcılar ----------
function new_friend_code(): string
{
    $alphabet = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';   // karışan harfler (I, O, 0, 1) yok
    for ($try = 0; $try < 20; $try++) {
        $c = '';
        for ($i = 0; $i < 8; $i++) $c .= $alphabet[random_int(0, strlen($alphabet) - 1)];
        if (!val('SELECT 1 FROM users WHERE friend_code = ?', [$c])) return $c;
    }
    fail('code_generation_failed', 500);
}

function clean_name(string $n): string
{
    $n = preg_replace('/[\x00-\x1F\x7F<>]/u', '', $n);
    $n = trim(preg_replace('/\s+/u', ' ', $n));
    if (mb_strlen($n) < 2 || mb_strlen($n) > 20) fail('bad_name');
    return $n;
}

function public_user(array $u): array
{
    return [
        'id' => (int)$u['id'],
        'name' => $u['display_name'],
        'level' => (int)$u['level'],
        'friendCode' => $u['friend_code'],
        'lastSeen' => (int)$u['last_seen'],
        'guildId' => $u['guild_id'] !== null ? (int)$u['guild_id'] : null,
        'flagged' => (int)($u['flagged'] ?? 0) === 1,
    ];
}

function are_friends(int $a, int $b): bool
{
    return (bool)val('SELECT 1 FROM friends WHERE user_id = ? AND friend_id = ? AND status = ?', [$a, $b, 'accepted']);
}

// Oyundaki geçerli kimlikler (iksir hediyeleri/takasları için)
const POTION_IDS = ['water', 'salt', 'co2', 'ammonia', 'saltpeter', 'bakingsoda', 'peroxide', 'bluevitriol', 'magnesia',
    'quicklime', 'coldpack', 'lye', 'limestone', 'rust', 'ethanol', 'hcl', 'silica'];
const POTION_EFFECTS = ['water' => 'Hydrate', 'salt' => 'Preserve', 'co2' => 'Fizz', 'ammonia' => 'Growth', 'saltpeter' => 'Fireworks',
    'bakingsoda' => 'Soothe', 'peroxide' => 'Disinfect', 'bluevitriol' => 'PestControl', 'magnesia' => 'Light', 'quicklime' => 'Warmth',
    'coldpack' => 'Cool', 'lye' => 'Cleanse', 'limestone' => 'Build', 'rust' => 'Pigment', 'ethanol' => 'Fuel', 'hcl' => 'Dissolve', 'silica' => 'Glass'];

// Lonca haftalık görevinde sırayla istenen etkiler
const GUILD_EFFECTS = ['Hydrate', 'Preserve', 'Fizz', 'Soothe', 'Cleanse', 'Disinfect', 'PestControl', 'Build', 'Pigment', 'Growth',
    'Fireworks', 'Light', 'Warmth', 'Dissolve', 'Cool', 'Fuel', 'Glass'];

function potion_in(string $key): string
{
    $p = id_in($key);
    if (!in_array($p, POTION_IDS, true)) fail('bad_' . $key);
    return $p;
}

/** Ekleme ya da güncelleme (MySQL ve SQLite için). $keys: benzersiz anahtar sütunları. */
function upsert(string $table, array $data, array $keys): void
{
    $cols = array_keys($data);
    $ph = implode(',', array_fill(0, count($cols), '?'));
    $upd = array_values(array_diff($cols, $keys));
    if (is_sqlite()) {
        $set = implode(',', array_map(fn($c) => "$c = excluded.$c", $upd));
        $sql = "INSERT INTO $table (" . implode(',', $cols) . ") VALUES ($ph) ON CONFLICT(" . implode(',', $keys) . ") DO UPDATE SET $set";
    } else {
        $set = implode(',', array_map(fn($c) => "$c = VALUES($c)", $upd));
        $sql = "INSERT INTO $table (" . implode(',', $cols) . ") VALUES ($ph) ON DUPLICATE KEY UPDATE $set";
    }
    q($sql, array_values($data));
}
