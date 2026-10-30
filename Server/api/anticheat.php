<?php
// Sorchemist hile koruması.
//
// İlke: oyun cihazda oynanır ama sunucu her bulut kaydını bir önceki geçerli kayıtla karşılaştırır.
// Aradan geçen gerçek süre içinde oyun kurallarına göre MÜMKÜN olmayan bir artış varsa kayıt reddedilir.
// Ayrıca arkadaşlara giden iksirler (hediye, takas, lonca bağışı) sunucudaki "defter"e (ledger) yazılır
// ve oyuncunun gerçekten sahip olduğu iksirlerle sınırlanır.
//
// Sınırlar bilerek cömerttir: dürüst bir oyuncu asla takılmamalı; yalnızca kayıt dosyasıyla oynayıp
// altını, seviyeyi ya da iksirleri şişiren hileler yakalanır.

declare(strict_types=1);

if (!defined('SORCHEMIST')) { http_response_code(403); exit; }

const FAIL_IDS = ['mystery_goo', 'near_miss', 'lukewarm_sludge', 'burnt', 'lumpy', 'roof_blown', 'alkali_boom', 'peroxide_foam',
    'noble_snub', 'hydrogen_pop', 'green_cloud', 'mercury_hat', 'glow_beard', 'midas', 'blinding_flash', 'rotten_egg', 'overflow',
    'giant_pumpkins', 'frog_villager', 'floating_villager', 'frozen_house', 'foam_flood', 'glowing_house', 'pink_village', 'fireworks_mishap'];
const PASSIVE_IDS = ['p_gold', 'p_owl', 'p_builder', 'p_brew', 'p_column', 'p_luck', 'p_sand', 'p_mendeleev', 'p_copper', 'p_purple'];
const BUILDING_IDS = ['tower', 'forge', 'glasshouse', 'watermill', 'mine', 'well', 'workshop', 'healer', 'crystal', 'library', 'market',
    'windmill', 'observatory', 'portal', 'dock', 'hollow', 'stable', 'distillery', 'smeltery', 'chronos', 'sporedome', 'vault', 'crater', 'aviary'];
// Kayıt sürüm 1 (24 evli köy) -> sürüm 2 (24 binalı kasaba). Oyundaki BuildingDatabase.LegacyIds ile aynı.
const LEGACY_IDS = ['tokmak' => 'forge', 'fistik' => 'glasshouse', 'filiz' => 'glasshouse', 'arici' => 'glasshouse', 'sucu' => 'watermill',
    'tasusta' => 'mine', 'kopuk' => 'well', 'berber' => 'workshop', 'ebe' => 'healer', 'comlekci' => 'crystal', 'dokumaci' => 'market',
    'derici' => 'market', 'kunduraci' => 'market', 'terzi' => 'market', 'ruzgar' => 'windmill', 'balikci' => 'dock', 'marangoz' => 'dock',
    'zeytin' => 'hollow', 'bal' => 'hollow', 'asci' => 'hollow', 'pirtik' => 'stable', 'fener' => 'stable', 'komurcu' => 'smeltery', 'oduncu' => 'smeltery'];
// Seviye L'den L+1'e yükseltme süresi (sn), oyundaki BuildingDatabase ile aynı
const UPGRADE_SECONDS = [0, 60, 300, 900, 3600, 10800, 43200, 86400, 259200, 518400, 1036800, 2073600];

// ----- Mutlak üst sınırlar -----
const MAX_GOLD = 50000000;
const MAX_LEVEL = 300;
const MAX_POTION_STACK = 9999;
const MAX_ELEMENT_STACK = 99999;
const MAX_QUESTS = 2100;

// ----- Zamana bağlı cömert üst sınırlar -----
const GOLD_PER_MIN = 10;         // pasif gelir en fazla 24 ev × 12 × 0,01 × 1,25 = 3,6/dk
const XP_PER_MIN = 300;
const KNOWLEDGE_PER_MIN = 40;    // en yüksek ~30/dk
const QUESTS_PER_MIN = 6;
const POTION_SECONDS = 12;       // en kısa demleme ~17 sn; tek kazan
const SLACK_SECONDS = 600;       // saat farkları ve gecikmeler için

/** Kayıt verisini (gzip + base64) çözüp PlayerState dizisine çevirir. */
function decode_save(string $data): ?array
{
    $bin = base64_decode($data, true);
    if ($bin === false) return null;
    $json = @gzdecode($bin);
    if ($json === false || strlen($json) > 2 * 1024 * 1024) return null;
    $s = json_decode($json, true);
    return is_array($s) ? $s : null;
}

function stack_map($list): array
{
    $m = [];
    if (!is_array($list)) return $m;
    foreach ($list as $it) {
        if (!is_array($it) || !isset($it['Id'])) continue;
        $id = (string)$it['Id'];
        $m[$id] = ($m[$id] ?? 0) + (int)($it['Count'] ?? 0);
    }
    return $m;
}

function xp_to_next(int $level): int { return 40 + $level * 30; }

/** Toplam XP (seviye atlamalarında harcanan dahil). */
function total_xp(int $level, int $xp): int
{
    $t = $xp;
    for ($l = 1; $l < min($level, MAX_LEVEL); $l++) $t += xp_to_next($l);
    return $t;
}

/** Kayıttan ölçümler (karşılaştırma için). */
function save_metrics(array $s): array
{
    $b = [];
    $up = [];
    foreach (($s['Buildings'] ?? []) as $x) {
        if (!is_array($x) || !isset($x['Id'])) continue;
        $bid = (string)$x['Id'];
        if ((int)($s['Version'] ?? 1) < 2) $bid = LEGACY_IDS[$bid] ?? $bid;   // eski kayıt: yeni binaya taşı (en yüksek seviye)
        $lv = (int)($x['Level'] ?? 1);
        if (!isset($b[$bid]) || $lv > $b[$bid]) $b[$bid] = $lv;
        if ((int)($x['UpgradeEndsAt'] ?? 0) > 0) $up[$bid] = true;
    }
    $level = (int)($s['Level'] ?? 1);
    return [
        'gold' => (int)($s['Gold'] ?? 0),
        'level' => $level,
        'xp' => (int)($s['Xp'] ?? 0),
        'xpTotal' => total_xp($level, (int)($s['Xp'] ?? 0)),
        'knowledge' => (int)($s['Knowledge'] ?? 0),
        'day' => (int)($s['Day'] ?? 1),
        'mystery' => (int)($s['MysteryOpened'] ?? 0),
        'potions' => stack_map($s['Potions'] ?? []),
        'elements' => stack_map($s['Elements'] ?? []),
        'quests' => array_values(array_filter((array)($s['CompletedQuests'] ?? []), 'is_string')),
        'known' => array_values(array_filter((array)($s['KnownElements'] ?? []), 'is_string')),
        'discovered' => array_values(array_filter((array)($s['Discovered'] ?? []), 'is_string')),
        'faildex' => array_values(array_filter((array)($s['FailDex'] ?? []), 'is_string')),
        'passives' => array_values(array_filter((array)($s['OwnedPassives'] ?? []), 'is_string')),
        'buildings' => $b,
        'upgrading' => $up,
        'ledgerId' => (int)($s['LedgerId'] ?? 0),
        'tamper' => (int)($s['TamperCount'] ?? 0),
    ];
}

/** Tek bir kaydın kendi içinde tutarlı olup olmadığı (geçmişe bakmadan). */
function check_absolute(array $m): array
{
    $bad = [];
    if ($m['gold'] < 0 || $m['gold'] > MAX_GOLD) $bad[] = 'gold_range';
    if ($m['level'] < 1 || $m['level'] > MAX_LEVEL) $bad[] = 'level_range';
    if ($m['xp'] < 0 || $m['xp'] >= xp_to_next($m['level'])) $bad[] = 'xp_range';
    if ($m['knowledge'] < 0 || $m['knowledge'] > 10000000) $bad[] = 'knowledge_range';
    foreach ($m['potions'] as $p => $n) {
        if (!in_array($p, POTION_IDS, true)) $bad[] = 'unknown_potion';
        if ($n < 0 || $n > MAX_POTION_STACK) $bad[] = 'potion_range';
    }
    foreach ($m['elements'] as $e => $n) if ($n < 0 || $n > MAX_ELEMENT_STACK || strlen($e) > 3) $bad[] = 'element_range';
    $q = $m['quests'];
    if (count($q) > MAX_QUESTS || count(array_unique($q)) !== count($q)) $bad[] = 'quest_list';
    foreach ($q as $id) if (!preg_match('/^(q_[a-z_]{2,20}|g\d{4}|k\d{3})$/', $id)) { $bad[] = 'quest_id'; break; }
    if (count(array_unique($m['known'])) > 118) $bad[] = 'known_elements';
    foreach ($m['discovered'] as $c) if (!in_array($c, POTION_IDS, true)) { $bad[] = 'discovered_id'; break; }
    foreach ($m['faildex'] as $f) if (!in_array($f, FAIL_IDS, true)) { $bad[] = 'faildex_id'; break; }
    foreach ($m['passives'] as $p) if (!in_array($p, PASSIVE_IDS, true)) { $bad[] = 'passive_id'; break; }
    $tower = $m['buildings']['tower'] ?? 1;
    foreach ($m['buildings'] as $id => $lv) {
        if (!in_array($id, BUILDING_IDS, true) || $lv < 1 || $lv > 12) { $bad[] = 'building_level'; break; }
        if ($id !== 'tower' && $lv > $tower + 2) { $bad[] = 'building_above_tower'; break; }
    }
    return array_values(array_unique($bad));
}

/**
 * Yeni kaydı önceki geçerli kayıtla karşılaştırır.
 * $ledger: iki kayıt arasında sunucunun bildiği (hediye, takas, lonca) değişiklikler ['p:water' => -1, 'gold' => 150, ...]
 */
function check_delta(array $prev, array $new, int $elapsed, array $ledger): array
{
    $bad = [];
    $elapsed = max(0, $elapsed) + SLACK_SECONDS;
    $min = $elapsed / 60.0;

    $prevQuests = array_flip($prev['quests']);
    $questsGained = 0;
    foreach ($new['quests'] as $q) if (!isset($prevQuests[$q])) $questsGained++;
    if ($questsGained > $min * QUESTS_PER_MIN + 5) $bad[] = 'too_many_quests';

    $mysteryGained = max(0, $new['mystery'] - $prev['mystery']);
    if ($mysteryGained > $elapsed / (5 * 3600) + 1) $bad[] = 'too_many_mystery_boxes';

    // Altın
    $goldAllow = $min * GOLD_PER_MIN + $questsGained * 250 + $mysteryGained * 300 + max(0, $ledger['gold'] ?? 0) + 500;
    if ($new['gold'] - $prev['gold'] > $goldAllow) $bad[] = 'gold_jump';

    // XP
    $xpAllow = $min * XP_PER_MIN + $questsGained * 300 + $mysteryGained * 30 + max(0, $ledger['xp'] ?? 0) + 1500;
    if ($new['xpTotal'] - $prev['xpTotal'] > $xpAllow) $bad[] = 'xp_jump';

    // Bina yükseltmeleri: her seviye gerçek süre ister (en çok 3 usta paralel çalışır, Hızlı Usta −%15)
    $need = 0;
    $levelUps = 0;
    foreach ($new['buildings'] as $id => $lv) {
        $from = $prev['buildings'][$id] ?? 1;
        for ($l = $from; $l < $lv; $l++) {
            // önceki kayıtta zaten süren yükseltmenin süresinin bir kısmı geçmiş olabilir: onu saymayız
            if (!($l === $from && isset($prev['upgrading'][$id]))) $need += UPGRADE_SECONDS[$l] * 0.85;
            $levelUps++;
        }
    }
    if ($need / 3 > $elapsed) $bad[] = 'building_too_fast';

    // Bilgi Puanı
    $newKnown = count(array_diff(array_unique($new['known']), $prev['known']));
    $newDisc = count(array_diff($new['discovered'], $prev['discovered']));
    $newFails = count(array_diff($new['faildex'], $prev['faildex']));
    $knowAllow = $min * KNOWLEDGE_PER_MIN + $newKnown * 5 + $newDisc * 20 + $newFails * 5 + $levelUps * 24
        + max(0, $ledger['knowledge'] ?? 0) + 300;
    if ($new['knowledge'] - $prev['knowledge'] > $knowAllow) $bad[] = 'knowledge_jump';

    // Gün sayacı yalnızca başarılı görevle ilerler
    if ($new['day'] - $prev['day'] > $questsGained + 1) $bad[] = 'day_jump';

    // İksirler: demlenebilecek miktardan fazlası + sunucunun bildiği hediye/takaslar
    $potionGain = 0;
    foreach ($new['potions'] as $p => $n) {
        $d = $n - ($prev['potions'][$p] ?? 0) - ($ledger['p:' . $p] ?? 0);
        if ($d > 0) $potionGain += $d;
    }
    if ($potionGain > $elapsed / POTION_SECONDS + 3) $bad[] = 'potion_jump';

    // Sunucu defteri geri alınamaz: kayıttaki defter numarası geriye gidemez
    if ($new['ledgerId'] < $prev['ledgerId']) $bad[] = 'ledger_rewind';
    // Yerel kayıt dosyası dışarıdan değiştirilmişse
    if ($new['tamper'] > $prev['tamper']) $bad[] = 'local_file_tampered';

    return array_values(array_unique($bad));
}

// =====================================================================
// Defter (ledger): sunucu üzerinden geçen iksir/altın/XP/Bilgi hareketleri
// =====================================================================
function ledger_add(int $userId, string $resource, int $delta, string $reason): int
{
    q('INSERT INTO ledger (user_id, resource, delta, reason, created_at) VALUES (?, ?, ?, ?, ?)', [$userId, $resource, $delta, $reason, now()]);
    return (int)db()->lastInsertId();
}

/** (fromId, toId] aralığındaki defter kayıtlarının kaynak başına toplamı. */
function ledger_sum(int $userId, int $fromId, int $toId): array
{
    $out = [];
    foreach (rows('SELECT resource, SUM(delta) AS d FROM ledger WHERE user_id = ? AND id > ? AND id <= ? GROUP BY resource', [$userId, $fromId, $toId]) as $r)
        $out[$r['resource']] = (int)$r['d'];
    return $out;
}

function ledger_max(int $userId): int { return (int)val('SELECT COALESCE(MAX(id), 0) FROM ledger WHERE user_id = ?', [$userId]); }

/** Son geçerli kayda göre oyuncunun elindeki iksir (henüz kayda yansımamış defter hareketleri dahil). */
function available_potion(int $userId, string $potion): int
{
    $s = row('SELECT data FROM saves WHERE user_id = ?', [$userId]);
    if (!$s) return 0;
    $st = decode_save($s['data']);
    if (!$st) return 0;
    $m = save_metrics($st);
    $pending = ledger_sum($userId, $m['ledgerId'], PHP_INT_MAX);
    return ($m['potions'][$potion] ?? 0) + ($pending['p:' . $potion] ?? 0);
}

/** Harcama öncesi kontrol: yoksa hata verir. */
function require_potion(int $userId, string $potion, int $count = 1): void
{
    if (available_potion($userId, $potion) < $count) fail('not_enough_potions', 409, ['potion' => $potion]);
}

/** İşaretli (hileli) hesaplar sosyal ekonomiye katılamaz. */
function require_clean(array $u): void
{
    if ((int)$u['flagged'] === 1) fail('account_flagged', 403);
}

function cheat_strike(int $userId, array $reasons): void
{
    // Saatte en çok bir ihlal sayılır: aynı kaydın tekrar tekrar gönderilmesi (ör. otomatik kayıt) tek başına hesabı işaretlemez
    $recent = (int)val('SELECT COUNT(*) FROM cheat_log WHERE user_id = ? AND created_at > ?', [$userId, now() - 3600]);
    q('INSERT INTO cheat_log (user_id, reasons, created_at, ip) VALUES (?, ?, ?, ?)', [$userId, implode(',', $reasons), now(), client_ip()]);
    if ($recent > 0) return;
    q('UPDATE users SET cheat_strikes = cheat_strikes + 1 WHERE id = ?', [$userId]);
    // 3 ihlalden sonra hesap işaretlenir: sıralamalarda görünmez, hediye/takas/lonca bağışı yapamaz
    q('UPDATE users SET flagged = 1 WHERE id = ? AND cheat_strikes >= 3', [$userId]);
}
