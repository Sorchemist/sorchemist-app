<?php
// Sorchemist çevrimiçi API'si. Tek giriş noktası:
//   POST https://sorchemist.com/api/api.php   gövde: {"action": "...", ...}   başlık: Authorization: Bearer <token>
// Bütün yanıtlar JSON: {"ok": true, ...} ya da {"ok": false, "error": "kod"}

declare(strict_types=1);
define('SORCHEMIST', 1);
require __DIR__ . '/lib.php';

if (($_SERVER['REQUEST_METHOD'] ?? 'GET') !== 'POST') out(['service' => 'sorchemist', 'version' => API_VERSION]);

try {
    $action = (string)(input()['action'] ?? '');
    $handlers = [
        'register' => 'a_register', 'login' => 'a_login', 'google' => 'a_google', 'logout' => 'a_logout',
        'me' => 'a_me', 'set_name' => 'a_set_name', 'delete_account' => 'a_delete_account',
        'save_get' => 'a_save_get', 'save_put' => 'a_save_put',
        'friends' => 'a_friends', 'friend_add' => 'a_friend_add', 'friend_accept' => 'a_friend_accept', 'friend_remove' => 'a_friend_remove',
        'visit' => 'a_visit',
        'inbox' => 'a_inbox', 'gift_send' => 'a_gift_send', 'gift_claim' => 'a_gift_claim',
        'trade_offer' => 'a_trade_offer', 'trade_accept' => 'a_trade_accept', 'trade_decline' => 'a_trade_decline',
        'trade_cancel' => 'a_trade_cancel', 'trade_claim' => 'a_trade_claim',
        'leaderboard' => 'a_leaderboard',
        'guild_create' => 'a_guild_create', 'guild_join' => 'a_guild_join', 'guild_leave' => 'a_guild_leave',
        'guild_info' => 'a_guild_info', 'guild_contribute' => 'a_guild_contribute', 'guild_claim' => 'a_guild_claim',
    ];
    if (!isset($handlers[$action])) fail('unknown_action', 404);
    rate_limit('all|' . client_ip(), 600, 60);          // dakikada en fazla 600 istek / IP
    ($handlers[$action])();
} catch (Throwable $e) {
    error_log('[sorchemist] ' . $e->getMessage());
    fail('server_error', 500);
}

// =====================================================================
// Hesap
// =====================================================================
function a_register(): void
{
    rate_limit('reg|' . client_ip(), 10, 3600);
    $email = strtolower(str_in('email', 190));
    if (!filter_var($email, FILTER_VALIDATE_EMAIL)) fail('bad_email');
    $pass = str_in('password', 200);
    if (strlen($pass) < 8) fail('password_too_short');
    $name = clean_name(str_in('name', 40));
    if (val('SELECT 1 FROM users WHERE email = ?', [$email])) fail('email_taken', 409);
    q('INSERT INTO users (email, pass_hash, display_name, friend_code, created_at, last_seen) VALUES (?, ?, ?, ?, ?, ?)',
        [$email, password_hash($pass, PASSWORD_DEFAULT), $name, new_friend_code(), now(), now()]);
    $id = (int)db()->lastInsertId();
    out(['token' => issue_session($id), 'user' => public_user(row('SELECT * FROM users WHERE id = ?', [$id]))]);
}

function a_login(): void
{
    rate_limit('login|' . client_ip(), 20, 900);
    $email = strtolower(str_in('email', 190));
    $pass = str_in('password', 200);
    $u = row('SELECT * FROM users WHERE email = ?', [$email]);
    if (!$u || !$u['pass_hash'] || !password_verify($pass, $u['pass_hash'])) fail('wrong_credentials', 401);
    if (password_needs_rehash($u['pass_hash'], PASSWORD_DEFAULT))
        q('UPDATE users SET pass_hash = ? WHERE id = ?', [password_hash($pass, PASSWORD_DEFAULT), $u['id']]);
    out(['token' => issue_session((int)$u['id']), 'user' => public_user($u)]);
}

/** Google ile giriş: oyun Google'dan aldığı kimlik jetonunu (ID token) gönderir, sunucu Google'a doğrulatır. */
function a_google(): void
{
    rate_limit('google|' . client_ip(), 30, 900);
    $idToken = str_in('idToken', 4096);
    $info = google_verify($idToken);
    $sub = $info['sub'];
    $email = isset($info['email']) && ($info['email_verified'] ?? '') === 'true' ? strtolower($info['email']) : null;

    $u = row('SELECT * FROM users WHERE google_sub = ?', [$sub]);
    if (!$u && $email) {
        // Aynı e-postayla açılmış hesap varsa Google'ı ona bağla
        $u = row('SELECT * FROM users WHERE email = ?', [$email]);
        if ($u) q('UPDATE users SET google_sub = ? WHERE id = ?', [$sub, $u['id']]);
    }
    if (!$u) {
        $name = trim((string)($info['given_name'] ?? $info['name'] ?? ''));
        if (mb_strlen($name) < 2) $name = 'Simyacı';
        $name = mb_substr(preg_replace('/[\x00-\x1F\x7F<>]/u', '', $name), 0, 20);
        q('INSERT INTO users (email, google_sub, display_name, friend_code, created_at, last_seen) VALUES (?, ?, ?, ?, ?, ?)',
            [$email, $sub, $name, new_friend_code(), now(), now()]);
        $u = row('SELECT * FROM users WHERE id = ?', [(int)db()->lastInsertId()]);
    }
    out(['token' => issue_session((int)$u['id']), 'user' => public_user($u)]);
}

function google_verify(string $idToken): array
{
    $allowed = (array)cfg('google_client_ids', []);
    if (!$allowed) fail('google_not_configured', 500);
    $url = 'https://oauth2.googleapis.com/tokeninfo?id_token=' . rawurlencode($idToken);
    $ch = curl_init($url);
    curl_setopt_array($ch, [CURLOPT_RETURNTRANSFER => true, CURLOPT_TIMEOUT => 10]);
    $body = curl_exec($ch);
    $code = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    curl_close($ch);
    $info = json_decode((string)$body, true);
    if ($code !== 200 || !is_array($info) || empty($info['sub'])) fail('google_token_invalid', 401);
    if (!in_array($info['aud'] ?? '', $allowed, true)) fail('google_wrong_audience', 401);
    if (!in_array($info['iss'] ?? '', ['accounts.google.com', 'https://accounts.google.com'], true)) fail('google_wrong_issuer', 401);
    if ((int)($info['exp'] ?? 0) < now()) fail('google_token_expired', 401);
    return $info;
}

function a_logout(): void
{
    auth();
    $h = $_SERVER['HTTP_AUTHORIZATION'] ?? '';
    $t = stripos($h, 'Bearer ') === 0 ? trim(substr($h, 7)) : (string)(input()['token'] ?? '');
    q('DELETE FROM sessions WHERE token_hash = ?', [token_hash($t)]);
    out();
}

function a_me(): void
{
    $u = auth();
    $save = row('SELECT rev, size, updated_at FROM saves WHERE user_id = ?', [$u['id']]);
    out(['user' => public_user($u), 'email' => $u['email'], 'google' => $u['google_sub'] !== null,
        'save' => $save ? ['rev' => (int)$save['rev'], 'size' => (int)$save['size'], 'updatedAt' => (int)$save['updated_at']] : null]);
}

function a_set_name(): void
{
    $u = auth();
    $name = clean_name(str_in('name', 40));
    q('UPDATE users SET display_name = ? WHERE id = ?', [$name, $u['id']]);
    out(['name' => $name]);
}

/** Hesabı ve bütün verileri kalıcı olarak siler (Google Play gereği: kullanıcı hesabını silebilmeli). */
function a_delete_account(): void
{
    $u = auth();
    $id = (int)$u['id'];
    if ($u['pass_hash']) {
        $pass = str_in('password', 200);
        if (!password_verify($pass, $u['pass_hash'])) fail('wrong_credentials', 401);
    }
    if ($u['guild_id'] !== null) leave_guild($u);
    foreach (['sessions', 'saves', 'villages', 'stats', 'weekly', 'gift_days'] as $t) q("DELETE FROM $t WHERE user_id = ?", [$id]);
    q('DELETE FROM friends WHERE user_id = ? OR friend_id = ?', [$id, $id]);
    q('DELETE FROM gifts WHERE from_id = ? OR to_id = ?', [$id, $id]);
    q('DELETE FROM trades WHERE from_id = ? OR to_id = ?', [$id, $id]);
    q('DELETE FROM users WHERE id = ?', [$id]);
    out(['deleted' => true]);
}

// =====================================================================
// Bulut kaydı
// =====================================================================
function a_save_get(): void
{
    $u = auth();
    $s = row('SELECT rev, data, updated_at, device FROM saves WHERE user_id = ?', [$u['id']]);
    if (!$s) out(['save' => null]);
    out(['save' => ['rev' => (int)$s['rev'], 'data' => $s['data'], 'updatedAt' => (int)$s['updated_at'], 'device' => $s['device']]]);
}

/**
 * Kaydı yükler. data = gzip + base64 oyun kaydı. baseRev = cihazın en son bildiği sürüm.
 * Sunucudaki sürüm baseRev'den yeniyse (başka bir cihaz kaydetmiş) "conflict" döner; force=true ile üzerine yazılır.
 */
function a_save_put(): void
{
    $u = auth();
    $id = (int)$u['id'];
    rate_limit('save|' . $id, 30, 60);
    $data = str_in('data', MAX_SAVE_BYTES);
    if (!preg_match('/^[A-Za-z0-9+\/=]+$/', $data)) fail('bad_data');
    $baseRev = int_in('baseRev', 0, PHP_INT_MAX, 0);
    $force = !empty(input()['force']);
    $device = mb_substr((string)(input()['device'] ?? ''), 0, 64);

    $cur = row('SELECT rev, updated_at, device FROM saves WHERE user_id = ?', [$id]);
    if ($cur && (int)$cur['rev'] !== $baseRev && !$force)
        fail('conflict', 409, ['serverRev' => (int)$cur['rev'], 'serverUpdatedAt' => (int)$cur['updated_at'], 'serverDevice' => $cur['device']]);
    $rev = $cur ? (int)$cur['rev'] + 1 : 1;
    upsert('saves', ['user_id' => $id, 'rev' => $rev, 'data' => $data, 'size' => strlen($data), 'device' => $device, 'updated_at' => now()], ['user_id']);

    // Sıralama istatistikleri ve köy vitrini (arkadaşlar ziyaret edince görür)
    $st = input()['stats'] ?? null;
    if (is_array($st)) {
        $lv = max(1, min(999, (int)($st['level'] ?? 1)));
        $disc = max(0, min(100000, (int)($st['discoveries'] ?? 0)));
        $fail = max(0, min(100000, (int)($st['faildex'] ?? 0)));
        $qst = max(0, min(100000, (int)($st['quests'] ?? 0)));
        upsert('stats', ['user_id' => $id, 'level' => $lv, 'discoveries' => $disc, 'faildex' => $fail, 'quests' => $qst, 'updated_at' => now()], ['user_id']);
        q('UPDATE users SET level = ? WHERE id = ?', [$lv, $id]);
        $wk = week_key();
        $w = row('SELECT * FROM weekly WHERE user_id = ? AND week = ?', [$id, $wk]);
        if (!$w) {
            q('INSERT INTO weekly (user_id, week, base_disc, base_fail, base_quests, cur_disc, cur_fail, cur_quests) VALUES (?,?,?,?,?,?,?,?)',
                [$id, $wk, $disc, $fail, $qst, $disc, $fail, $qst]);
        } else {
            q('UPDATE weekly SET cur_disc = ?, cur_fail = ?, cur_quests = ? WHERE user_id = ? AND week = ?', [$disc, $fail, $qst, $id, $wk]);
        }
    }
    $village = input()['village'] ?? null;
    if (is_array($village)) {
        $json = json_encode($village, JSON_UNESCAPED_UNICODE);
        if (strlen($json) <= 16384) upsert('villages', ['user_id' => $id, 'json' => $json, 'updated_at' => now()], ['user_id']);
    }
    out(['rev' => $rev]);
}

// =====================================================================
// Arkadaşlar ve köy ziyareti
// =====================================================================
function a_friends(): void
{
    $u = auth();
    $list = rows('SELECT f.status, u.* FROM friends f JOIN users u ON u.id = f.friend_id WHERE f.user_id = ? ORDER BY u.last_seen DESC', [$u['id']]);
    $out = ['friends' => [], 'incoming' => [], 'outgoing' => []];
    foreach ($list as $r) {
        $key = $r['status'] === 'accepted' ? 'friends' : ($r['status'] === 'pending_in' ? 'incoming' : 'outgoing');
        $out[$key][] = public_user($r);
    }
    out($out);
}

function a_friend_add(): void
{
    $u = auth();
    rate_limit('fadd|' . $u['id'], 30, 3600);
    $code = strtoupper(str_in('code', 8));
    $f = row('SELECT * FROM users WHERE friend_code = ?', [$code]);
    if (!$f) fail('friend_not_found', 404);
    $a = (int)$u['id']; $b = (int)$f['id'];
    if ($a === $b) fail('cannot_add_self');
    if ((int)val('SELECT COUNT(*) FROM friends WHERE user_id = ? AND status = ?', [$a, 'accepted']) >= MAX_FRIENDS) fail('too_many_friends');
    $mine = val('SELECT status FROM friends WHERE user_id = ? AND friend_id = ?', [$a, $b]);
    if ($mine === 'accepted') out(['status' => 'accepted', 'friend' => public_user($f)]);
    if ($mine === 'pending_in') { accept_friend($a, $b); out(['status' => 'accepted', 'friend' => public_user($f)]); }
    if (!$mine) {
        q('INSERT INTO friends (user_id, friend_id, status, created_at) VALUES (?, ?, ?, ?)', [$a, $b, 'pending_out', now()]);
        q('INSERT INTO friends (user_id, friend_id, status, created_at) VALUES (?, ?, ?, ?)', [$b, $a, 'pending_in', now()]);
    }
    out(['status' => 'pending', 'friend' => public_user($f)]);
}

function accept_friend(int $a, int $b): void
{
    q('UPDATE friends SET status = ? WHERE (user_id = ? AND friend_id = ?) OR (user_id = ? AND friend_id = ?)', ['accepted', $a, $b, $b, $a]);
}

function a_friend_accept(): void
{
    $u = auth();
    $b = int_in('userId', 1);
    if (val('SELECT status FROM friends WHERE user_id = ? AND friend_id = ?', [$u['id'], $b]) !== 'pending_in') fail('no_request', 404);
    accept_friend((int)$u['id'], $b);
    out();
}

function a_friend_remove(): void
{
    $u = auth();
    $b = int_in('userId', 1);
    q('DELETE FROM friends WHERE (user_id = ? AND friend_id = ?) OR (user_id = ? AND friend_id = ?)', [$u['id'], $b, $b, $u['id']]);
    out();
}

function a_visit(): void
{
    $u = auth();
    $b = int_in('userId', 1);
    $sameGuild = $u['guild_id'] !== null && (int)val('SELECT guild_id FROM users WHERE id = ?', [$b]) === (int)$u['guild_id'];
    if (!are_friends((int)$u['id'], $b) && !$sameGuild) fail('not_friends', 403);
    $f = row('SELECT * FROM users WHERE id = ?', [$b]);
    if (!$f) fail('not_found', 404);
    $v = row('SELECT json, updated_at FROM villages WHERE user_id = ?', [$b]);
    $s = row('SELECT * FROM stats WHERE user_id = ?', [$b]);
    out(['user' => public_user($f), 'village' => $v ? json_decode($v['json'], true) : null, 'updatedAt' => $v ? (int)$v['updated_at'] : 0,
        'stats' => $s ? ['discoveries' => (int)$s['discoveries'], 'faildex' => (int)$s['faildex'], 'quests' => (int)$s['quests']] : null]);
}

// =====================================================================
// Hediye ve takas (gelen kutusu)
// =====================================================================
function a_inbox(): void
{
    $u = auth();
    $id = (int)$u['id'];
    $gifts = rows('SELECT g.id, g.potion, g.created_at, u.display_name AS from_name, u.id AS from_id FROM gifts g JOIN users u ON u.id = g.from_id
                   WHERE g.to_id = ? AND g.claimed_at IS NULL ORDER BY g.id DESC LIMIT 50', [$id]);
    $incoming = rows('SELECT t.*, u.display_name AS other_name FROM trades t JOIN users u ON u.id = t.from_id
                      WHERE t.to_id = ? AND t.status = ? ORDER BY t.id DESC LIMIT 50', [$id, 'open']);
    $mine = rows('SELECT t.*, u.display_name AS other_name FROM trades t JOIN users u ON u.id = t.to_id
                  WHERE t.from_id = ? AND (t.status = ? OR t.from_claimed = 0) ORDER BY t.id DESC LIMIT 50', [$id, 'open']);
    $sentToday = (int)val('SELECT n FROM gift_days WHERE user_id = ? AND day = ?', [$id, day_key()]);
    out([
        'gifts' => array_map(fn($g) => ['id' => (int)$g['id'], 'potion' => $g['potion'], 'fromId' => (int)$g['from_id'], 'fromName' => $g['from_name'], 'at' => (int)$g['created_at']], $gifts),
        'tradesIn' => array_map('trade_out', $incoming),
        'tradesOut' => array_map('trade_out', $mine),
        'giftsLeftToday' => max(0, GIFTS_PER_DAY - $sentToday),
    ]);
}

function trade_out(array $t): array
{
    return ['id' => (int)$t['id'], 'fromId' => (int)$t['from_id'], 'toId' => (int)$t['to_id'], 'otherName' => $t['other_name'],
        'give' => $t['give_potion'], 'want' => $t['want_potion'], 'status' => $t['status'], 'claimed' => (int)$t['from_claimed'] === 1];
}

/** Hediye: gönderen iksiri cihazında düşer, alıcı "Al" deyince kendi envanterine ekler. Günde en fazla 5 hediye. */
function a_gift_send(): void
{
    $u = auth();
    $id = (int)$u['id'];
    $to = int_in('toId', 1);
    $potion = potion_in('potion');
    if (!are_friends($id, $to)) fail('not_friends', 403);
    $n = (int)val('SELECT n FROM gift_days WHERE user_id = ? AND day = ?', [$id, day_key()]);
    if ($n >= GIFTS_PER_DAY) fail('daily_gift_limit');
    upsert('gift_days', ['user_id' => $id, 'day' => day_key(), 'n' => $n + 1], ['user_id', 'day']);
    q('INSERT INTO gifts (from_id, to_id, potion, created_at) VALUES (?, ?, ?, ?)', [$id, $to, $potion, now()]);
    out(['giftsLeftToday' => GIFTS_PER_DAY - $n - 1]);
}

function a_gift_claim(): void
{
    $u = auth();
    $gid = int_in('giftId', 1);
    $st = q('UPDATE gifts SET claimed_at = ? WHERE id = ? AND to_id = ? AND claimed_at IS NULL', [now(), $gid, $u['id']]);
    if ($st->rowCount() !== 1) fail('already_claimed', 409);
    out(['potion' => val('SELECT potion FROM gifts WHERE id = ?', [$gid])]);
}

/** Takas teklifi: teklif eden "give" iksirini emanete verir (cihazında düşer), karşılığında "want" ister. */
function a_trade_offer(): void
{
    $u = auth();
    $id = (int)$u['id'];
    $to = int_in('toId', 1);
    $give = potion_in('give');
    $want = potion_in('want');
    if ($give === $want) fail('same_potion');
    if (!are_friends($id, $to)) fail('not_friends', 403);
    if ((int)val('SELECT COUNT(*) FROM trades WHERE from_id = ? AND status = ?', [$id, 'open']) >= 10) fail('too_many_trades');
    q('INSERT INTO trades (from_id, to_id, give_potion, want_potion, status, created_at) VALUES (?, ?, ?, ?, ?, ?)',
        [$id, $to, $give, $want, 'open', now()]);
    out(['tradeId' => (int)db()->lastInsertId()]);
}

/** Alıcı kabul eder: alıcının cihazı "want" iksirini düşer, "give" iksirini alır. Teklif eden sonra "want"ı toplar. */
function a_trade_accept(): void
{
    $u = auth();
    $tid = int_in('tradeId', 1);
    $st = q('UPDATE trades SET status = ?, resolved_at = ? WHERE id = ? AND to_id = ? AND status = ?', ['accepted', now(), $tid, $u['id'], 'open']);
    if ($st->rowCount() !== 1) fail('trade_not_open', 409);
    out(['receive' => val('SELECT give_potion FROM trades WHERE id = ?', [$tid])]);
}

function a_trade_decline(): void
{
    $u = auth();
    $tid = int_in('tradeId', 1);
    $st = q('UPDATE trades SET status = ?, resolved_at = ? WHERE id = ? AND to_id = ? AND status = ?', ['declined', now(), $tid, $u['id'], 'open']);
    if ($st->rowCount() !== 1) fail('trade_not_open', 409);
    out();
}

function a_trade_cancel(): void
{
    $u = auth();
    $tid = int_in('tradeId', 1);
    $st = q('UPDATE trades SET status = ?, resolved_at = ? WHERE id = ? AND from_id = ? AND status = ?', ['cancelled', now(), $tid, $u['id'], 'open']);
    if ($st->rowCount() !== 1) fail('trade_not_open', 409);
    out();
}

/** Teklif eden, sonuçlanan takası toplar: kabul edildiyse "want", reddedildi/iptal edildiyse kendi "give" iksiri geri gelir. */
function a_trade_claim(): void
{
    $u = auth();
    $tid = int_in('tradeId', 1);
    $t = row('SELECT * FROM trades WHERE id = ? AND from_id = ?', [$tid, $u['id']]);
    if (!$t || $t['status'] === 'open') fail('trade_not_resolved', 409);
    $st = q('UPDATE trades SET from_claimed = 1 WHERE id = ? AND from_claimed = 0', [$tid]);
    if ($st->rowCount() !== 1) fail('already_claimed', 409);
    out(['potion' => $t['status'] === 'accepted' ? $t['want_potion'] : $t['give_potion'], 'status' => $t['status']]);
}

// =====================================================================
// Haftalık sıralama
// =====================================================================
function a_leaderboard(): void
{
    $u = auth();
    $board = id_in('board');                                   // quests | discoveries | faildex | level
    $scope = (string)(input()['scope'] ?? 'global');           // global | friends | guild
    $wk = week_key();
    $cols = ['quests' => '(w.cur_quests - w.base_quests)', 'discoveries' => '(w.cur_disc - w.base_disc)', 'faildex' => '(w.cur_fail - w.base_fail)'];
    $where = '';
    $args = [];
    if ($scope === 'friends') {
        $where = ' AND (u.id = ? OR u.id IN (SELECT friend_id FROM friends WHERE user_id = ? AND status = \'accepted\'))';
        $args = [$u['id'], $u['id']];
    } elseif ($scope === 'guild') {
        if ($u['guild_id'] === null) out(['entries' => [], 'week' => $wk]);
        $where = ' AND u.guild_id = ?';
        $args = [$u['guild_id']];
    }
    if ($board === 'level') {
        $sql = "SELECT u.id, u.display_name, u.level, u.level AS score FROM users u WHERE 1=1 $where ORDER BY score DESC, u.id LIMIT 50";
        $list = rows($sql, $args);
    } else {
        if (!isset($cols[$board])) fail('bad_board');
        $expr = $cols[$board];
        $sql = "SELECT u.id, u.display_name, u.level, $expr AS score FROM weekly w JOIN users u ON u.id = w.user_id
                WHERE w.week = ? $where AND $expr > 0 ORDER BY score DESC, u.id LIMIT 50";
        $list = rows($sql, array_merge([$wk], $args));
    }
    $entries = [];
    foreach ($list as $i => $r) $entries[] = ['rank' => $i + 1, 'id' => (int)$r['id'], 'name' => $r['display_name'], 'level' => (int)$r['level'], 'score' => (int)$r['score']];
    out(['entries' => $entries, 'week' => $wk, 'resetsAt' => strtotime('next monday 00:00 UTC')]);
}

// =====================================================================
// Loncalar (köy birlikleri) ve haftalık ortak görev
// =====================================================================

/** Bu haftanın lonca görevi: herkes için aynı etki; hedef üye sayısıyla büyür. */
function guild_quest(int $guildId, int $wk): array
{
    $members = (int)val('SELECT COUNT(*) FROM users WHERE guild_id = ?', [$guildId]);
    $effect = GUILD_EFFECTS[($wk * 7 + $guildId) % count(GUILD_EFFECTS)];
    $target = min(200, 10 + 5 * max(1, $members));
    $progress = (int)val('SELECT amount FROM guild_progress WHERE guild_id = ? AND week = ?', [$guildId, $wk]);
    return ['effect' => $effect, 'target' => $target, 'progress' => $progress, 'done' => $progress >= $target,
        'rewardGold' => 150, 'rewardXp' => 120, 'rewardKnowledge' => 30];
}

function a_guild_create(): void
{
    $u = auth();
    if ($u['guild_id'] !== null) fail('already_in_guild');
    rate_limit('gcreate|' . $u['id'], 3, 86400);
    $name = clean_name(str_in('name', 40));
    if (val('SELECT 1 FROM guilds WHERE name = ?', [$name])) fail('guild_name_taken', 409);
    $code = new_friend_code();
    q('INSERT INTO guilds (name, code, owner_id, created_at) VALUES (?, ?, ?, ?)', [$name, $code, $u['id'], now()]);
    $gid = (int)db()->lastInsertId();
    q('UPDATE users SET guild_id = ? WHERE id = ?', [$gid, $u['id']]);
    out(['guildId' => $gid, 'code' => $code]);
}

function a_guild_join(): void
{
    $u = auth();
    if ($u['guild_id'] !== null) fail('already_in_guild');
    $code = strtoupper(str_in('code', 8));
    $g = row('SELECT * FROM guilds WHERE code = ?', [$code]);
    if (!$g) fail('guild_not_found', 404);
    if ((int)val('SELECT COUNT(*) FROM users WHERE guild_id = ?', [$g['id']]) >= MAX_GUILD_MEMBERS) fail('guild_full');
    q('UPDATE users SET guild_id = ? WHERE id = ?', [$g['id'], $u['id']]);
    out(['guildId' => (int)$g['id'], 'name' => $g['name']]);
}

function leave_guild(array $u): void
{
    $gid = (int)$u['guild_id'];
    q('UPDATE users SET guild_id = NULL WHERE id = ?', [$u['id']]);
    $next = val('SELECT id FROM users WHERE guild_id = ? ORDER BY last_seen DESC LIMIT 1', [$gid]);
    if ($next === null) {
        q('DELETE FROM guild_progress WHERE guild_id = ?', [$gid]);
        q('DELETE FROM guild_contrib WHERE guild_id = ?', [$gid]);
        q('DELETE FROM guilds WHERE id = ?', [$gid]);
    } elseif ((int)val('SELECT owner_id FROM guilds WHERE id = ?', [$gid]) === (int)$u['id']) {
        q('UPDATE guilds SET owner_id = ? WHERE id = ?', [$next, $gid]);   // başkanlık en aktif üyeye geçer
    }
}

function a_guild_leave(): void
{
    $u = auth();
    if ($u['guild_id'] === null) fail('not_in_guild');
    leave_guild($u);
    out();
}

function a_guild_info(): void
{
    $u = auth();
    if ($u['guild_id'] === null) out(['guild' => null]);
    $gid = (int)$u['guild_id'];
    $g = row('SELECT * FROM guilds WHERE id = ?', [$gid]);
    $wk = week_key();
    $members = rows('SELECT u.*, COALESCE(c.amount, 0) AS contrib FROM users u
                     LEFT JOIN guild_contrib c ON c.user_id = u.id AND c.guild_id = ? AND c.week = ?
                     WHERE u.guild_id = ? ORDER BY contrib DESC, u.level DESC', [$gid, $wk, $gid]);
    $claimed = (int)val('SELECT claimed FROM guild_contrib WHERE guild_id = ? AND week = ? AND user_id = ?', [$gid, $wk, $u['id']]);
    out(['guild' => ['id' => $gid, 'name' => $g['name'], 'code' => $g['code'], 'ownerId' => (int)$g['owner_id'],
        'members' => array_map(fn($m) => public_user($m) + ['contrib' => (int)$m['contrib']], $members),
        'quest' => guild_quest($gid, $wk) + ['claimed' => $claimed === 1], 'week' => $wk]]);
}

/** Üye, haftanın istediği etkideki iksirlerden bağışlar (cihazında düşer). */
function a_guild_contribute(): void
{
    $u = auth();
    if ($u['guild_id'] === null) fail('not_in_guild');
    $gid = (int)$u['guild_id'];
    $potion = potion_in('potion');
    $count = int_in('count', 1, 50);
    $wk = week_key();
    $quest = guild_quest($gid, $wk);
    if (POTION_EFFECTS[$potion] !== $quest['effect']) fail('wrong_effect');
    if ($quest['done']) fail('quest_done');
    $count = min($count, $quest['target'] - $quest['progress']);
    $cur = (int)val('SELECT amount FROM guild_progress WHERE guild_id = ? AND week = ?', [$gid, $wk]);
    upsert('guild_progress', ['guild_id' => $gid, 'week' => $wk, 'amount' => $cur + $count], ['guild_id', 'week']);
    $mine = row('SELECT amount, claimed FROM guild_contrib WHERE guild_id = ? AND week = ? AND user_id = ?', [$gid, $wk, $u['id']]);
    upsert('guild_contrib', ['guild_id' => $gid, 'week' => $wk, 'user_id' => $u['id'], 'amount' => ($mine ? (int)$mine['amount'] : 0) + $count,
        'claimed' => $mine ? (int)$mine['claimed'] : 0], ['guild_id', 'week', 'user_id']);
    out(['accepted' => $count, 'quest' => guild_quest($gid, $wk)]);
}

/** Görev tamamlanınca katkı veren her üye ödülünü bir kez alır. */
function a_guild_claim(): void
{
    $u = auth();
    if ($u['guild_id'] === null) fail('not_in_guild');
    $gid = (int)$u['guild_id'];
    $wk = week_key();
    $quest = guild_quest($gid, $wk);
    if (!$quest['done']) fail('quest_not_done');
    $st = q('UPDATE guild_contrib SET claimed = 1 WHERE guild_id = ? AND week = ? AND user_id = ? AND claimed = 0 AND amount > 0', [$gid, $wk, $u['id']]);
    if ($st->rowCount() !== 1) fail('nothing_to_claim', 409);
    out(['gold' => $quest['rewardGold'], 'xp' => $quest['rewardXp'], 'knowledge' => $quest['rewardKnowledge']]);
}
