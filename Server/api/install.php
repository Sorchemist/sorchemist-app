<?php
// Sorchemist veritabanı kurulumu. Bir kez çalıştır:
//   https://sorchemist.com/api/install.php?key=<config.php içindeki install_key>
// Kurulumdan sonra bu dosyayı sil (ya da install_key'i boş bırak).

declare(strict_types=1);
define('SORCHEMIST', 1);
require __DIR__ . '/lib.php';

$key = (string)cfg('install_key', '');
if ($key === '' || !hash_equals($key, (string)($_GET['key'] ?? ''))) fail('forbidden', 403);

$sqlite = is_sqlite();
$ai = $sqlite ? 'INTEGER PRIMARY KEY AUTOINCREMENT' : 'BIGINT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY';
$big = $sqlite ? 'INTEGER' : 'BIGINT';
$text = $sqlite ? 'TEXT' : 'MEDIUMTEXT';
$eng = $sqlite ? '' : ' ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci';
$vc = fn(int $n) => $sqlite ? 'TEXT' : "VARCHAR($n)";

$tables = [
"CREATE TABLE IF NOT EXISTS users (
  id $ai,
  email {$vc(190)} NULL,
  pass_hash {$vc(255)} NULL,
  google_sub {$vc(64)} NULL,
  display_name {$vc(40)} NOT NULL,
  friend_code {$vc(8)} NOT NULL,
  level INT NOT NULL DEFAULT 1,
  guild_id $big NULL,
  cheat_strikes INT NOT NULL DEFAULT 0,
  flagged INT NOT NULL DEFAULT 0,
  created_at $big NOT NULL,
  last_seen $big NOT NULL,
  UNIQUE (email), UNIQUE (google_sub), UNIQUE (friend_code)
)$eng",
"CREATE TABLE IF NOT EXISTS sessions (
  token_hash CHAR(64) NOT NULL PRIMARY KEY,
  user_id $big NOT NULL,
  created_at $big NOT NULL,
  expires_at $big NOT NULL
)$eng",
"CREATE TABLE IF NOT EXISTS saves (
  user_id $big NOT NULL PRIMARY KEY,
  rev INT NOT NULL,
  data $text NOT NULL,
  size INT NOT NULL,
  device {$vc(64)} NULL,
  updated_at $big NOT NULL
)$eng",
"CREATE TABLE IF NOT EXISTS villages (
  user_id $big NOT NULL PRIMARY KEY,
  json TEXT NOT NULL,
  updated_at $big NOT NULL
)$eng",
"CREATE TABLE IF NOT EXISTS stats (
  user_id $big NOT NULL PRIMARY KEY,
  level INT NOT NULL DEFAULT 1,
  discoveries INT NOT NULL DEFAULT 0,
  faildex INT NOT NULL DEFAULT 0,
  quests INT NOT NULL DEFAULT 0,
  updated_at $big NOT NULL
)$eng",
"CREATE TABLE IF NOT EXISTS weekly (
  user_id $big NOT NULL,
  week INT NOT NULL,
  base_disc INT NOT NULL, base_fail INT NOT NULL, base_quests INT NOT NULL,
  cur_disc INT NOT NULL, cur_fail INT NOT NULL, cur_quests INT NOT NULL,
  PRIMARY KEY (user_id, week)
)$eng",
"CREATE TABLE IF NOT EXISTS friends (
  user_id $big NOT NULL,
  friend_id $big NOT NULL,
  status {$vc(12)} NOT NULL,
  created_at $big NOT NULL,
  PRIMARY KEY (user_id, friend_id)
)$eng",
"CREATE TABLE IF NOT EXISTS gifts (
  id $ai,
  from_id $big NOT NULL,
  to_id $big NOT NULL,
  potion {$vc(20)} NOT NULL,
  created_at $big NOT NULL,
  claimed_at $big NULL
)$eng",
"CREATE TABLE IF NOT EXISTS gift_days (
  user_id $big NOT NULL,
  day INT NOT NULL,
  n INT NOT NULL,
  PRIMARY KEY (user_id, day)
)$eng",
"CREATE TABLE IF NOT EXISTS trades (
  id $ai,
  from_id $big NOT NULL,
  to_id $big NOT NULL,
  give_potion {$vc(20)} NOT NULL,
  want_potion {$vc(20)} NOT NULL,
  status {$vc(12)} NOT NULL,
  from_claimed INT NOT NULL DEFAULT 0,
  created_at $big NOT NULL,
  resolved_at $big NULL
)$eng",
"CREATE TABLE IF NOT EXISTS guilds (
  id $ai,
  name {$vc(40)} NOT NULL,
  code {$vc(8)} NOT NULL,
  owner_id $big NOT NULL,
  created_at $big NOT NULL,
  UNIQUE (name), UNIQUE (code)
)$eng",
"CREATE TABLE IF NOT EXISTS guild_progress (
  guild_id $big NOT NULL,
  week INT NOT NULL,
  amount INT NOT NULL,
  PRIMARY KEY (guild_id, week)
)$eng",
"CREATE TABLE IF NOT EXISTS guild_contrib (
  guild_id $big NOT NULL,
  week INT NOT NULL,
  user_id $big NOT NULL,
  amount INT NOT NULL,
  claimed INT NOT NULL DEFAULT 0,
  PRIMARY KEY (guild_id, week, user_id)
)$eng",
"CREATE TABLE IF NOT EXISTS save_history (
  user_id $big NOT NULL,
  rev INT NOT NULL,
  data $text NOT NULL,
  created_at $big NOT NULL,
  PRIMARY KEY (user_id, rev)
)$eng",
"CREATE TABLE IF NOT EXISTS ledger (
  id $ai,
  user_id $big NOT NULL,
  resource {$vc(24)} NOT NULL,
  delta INT NOT NULL,
  reason {$vc(24)} NOT NULL,
  created_at $big NOT NULL
)$eng",
"CREATE TABLE IF NOT EXISTS cheat_log (
  id $ai,
  user_id $big NOT NULL,
  reasons {$vc(255)} NOT NULL,
  ip {$vc(45)} NULL,
  created_at $big NOT NULL
)$eng",
"CREATE TABLE IF NOT EXISTS rate_limits (
  k CHAR(40) NOT NULL PRIMARY KEY,
  hits INT NOT NULL,
  expires_at $big NOT NULL
)$eng",
];
$indexes = [
  'CREATE INDEX idx_sessions_user ON sessions (user_id)',
  'CREATE INDEX idx_weekly_week ON weekly (week)',
  'CREATE INDEX idx_gifts_to ON gifts (to_id, claimed_at)',
  'CREATE INDEX idx_trades_to ON trades (to_id, status)',
  'CREATE INDEX idx_trades_from ON trades (from_id, status)',
  'CREATE INDEX idx_users_guild ON users (guild_id)',
  'CREATE INDEX idx_friends_friend ON friends (friend_id)',
  'CREATE INDEX idx_ledger_user ON ledger (user_id, id)',
  'CREATE INDEX idx_cheat_user ON cheat_log (user_id)',
];
// Önceki sürümden yükseltme: users tablosuna yeni sütunlar (varsa hata verir, yok sayılır)
$alters = [
  'ALTER TABLE users ADD COLUMN cheat_strikes INT NOT NULL DEFAULT 0',
  'ALTER TABLE users ADD COLUMN flagged INT NOT NULL DEFAULT 0',
];

foreach ($tables as $sql) db()->exec($sql);
foreach ($alters as $sql) { try { db()->exec($sql); } catch (PDOException $e) { /* zaten var */ } }
foreach ($indexes as $sql) { try { db()->exec($sql); } catch (PDOException $e) { /* zaten var */ } }

out(['installed' => true, 'driver' => db_driver()]);
