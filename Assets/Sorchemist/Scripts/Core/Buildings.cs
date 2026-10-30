// Kasaba binaları: Simya Kulesi + 23 bina, 4 mahalle (grup). Her binanın 1-12 arası seviyesi var.
// Yükseltmeler gerçek zamanlı sürer (oyun kapalıyken de ilerler); seviye atladıkça yeni deneyler açılır.
// Mahalleler oyuncu seviyesiyle büyülü sisin altından açılır: 1. grup baştan, 2. grup Sv.5, 3. grup Sv.10, 4. grup Sv.15.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sorchemist.Core
{
    [Serializable]
    public class BuildingEntry
    {
        public string Id;
        public int Level = 1;
        public long UpgradeEndsAt;   // 0 = yükseltme yok, aksi halde bitiş zamanı (unix sn)
    }

    public class CompoundUnlock
    {
        public string CompoundId;
        public string BuildingId;   // Bu deneyi öğreten bina
        public int Level;           // Gereken bina seviyesi
    }

    public static class BuildingDatabase
    {
        public const string Tower = "tower";      // Simya Kulesi (Başsimyacı'nın kulesi, Aqualf'in laboratuvarı)

        // ----- Mahalleler (büyülü sis) -----
        public static readonly int[] GroupUnlockLevels = { 0, 1, 5, 10, 15 };
        public static int Group(string id)
        {
            if (id == Tower) return 1;
            var v = VillageDatabase.GetVillager(id);
            return v != null ? Math.Max(1, v.Group) : 1;
        }
        public static int GroupUnlockLevel(int group) { return GroupUnlockLevels[Math.Max(1, Math.Min(4, group))]; }
        public static int UnlockLevelOf(string id) { return GroupUnlockLevel(Group(id)); }

        // Eski köy kimlikleri -> yeni kasaba binaları (kayıt ve görev uyumluluğu için)
        public static readonly Dictionary<string, string> LegacyIds = new Dictionary<string, string>
        {
            { "tokmak", "forge" }, { "fistik", "glasshouse" }, { "filiz", "glasshouse" }, { "arici", "glasshouse" },
            { "sucu", "watermill" }, { "tasusta", "mine" }, { "kopuk", "well" }, { "berber", "workshop" }, { "ebe", "healer" },
            { "comlekci", "crystal" }, { "dokumaci", "market" }, { "derici", "market" }, { "kunduraci", "market" }, { "terzi", "market" },
            { "ruzgar", "windmill" }, { "balikci", "dock" }, { "marangoz", "dock" }, { "zeytin", "hollow" }, { "bal", "hollow" },
            { "asci", "hollow" }, { "pirtik", "stable" }, { "fener", "stable" }, { "komurcu", "smeltery" }, { "oduncu", "smeltery" },
        };
        public static string MapLegacy(string id) { string n; return id != null && LegacyIds.TryGetValue(id, out n) ? n : id; }
        public const int MaxLevel = 12;

        // Seviye L'den L+1'e geçiş süresi (saniye). İndeks = mevcut seviye.
        static readonly long[] UpgradeTime =
        {
            0,
            60,                 // 1 -> 2 : 1 dakika
            5 * 60,             // 2 -> 3 : 5 dakika
            15 * 60,            // 3 -> 4 : 15 dakika
            3600,               // 4 -> 5 : 1 saat
            3 * 3600,           // 5 -> 6 : 3 saat
            12 * 3600,          // 6 -> 7 : 12 saat
            24 * 3600,          // 7 -> 8 : 24 saat
            3 * 86400,          // 8 -> 9 : 3 gün
            6 * 86400,          // 9 -> 10: 6 gün
            12 * 86400,         // 10 -> 11: 12 gün
            24 * 86400,         // 11 -> 12: 24 gün
        };

        public static long UpgradeSeconds(int fromLevel)
        {
            return fromLevel >= 1 && fromLevel < MaxLevel ? UpgradeTime[fromLevel] : 0;
        }

        /// <summary>Yükseltme ücreti (altın). Kule biraz daha pahalı.</summary>
        public static int UpgradeCost(string id, int fromLevel)
        {
            double baseCost = id == Tower ? 25 : 15;
            return (int)Math.Round(baseCost * Math.Pow(2.1, fromLevel - 1));
        }

        /// <summary>Bina başına dakikalık pasif altın üretimi (yalnızca oyun açıkken). Seviye başına saatte 0,6 altın.</summary>
        public const float GoldPerLevelPerMinute = 0.01f;

        public static float GoldPerMinute(string id, int level)
        {
            return id == Tower ? 0 : level * GoldPerLevelPerMinute;
        }

        /// <summary>Mevsimin bu binanın üretimine etkisi (çarpan).</summary>
        public static float SeasonGoldFactor(string id, Season s)
        {
            switch (s)
            {
                case Season.Spring:
                    if (id == "glasshouse" || id == "healer" || id == "hollow" || id == "stable") return 1.35f;
                    if (id == "smeltery") return 0.85f;
                    break;
                case Season.Autumn:
                    if (id == "forge" || id == "windmill" || id == "dock" || id == "aviary") return 1.35f;
                    if (id == "well") return 0.85f;
                    break;
                case Season.Winter:
                    if (id == "crystal" || id == "observatory" || id == "distillery") return 1.35f;
                    if (id == "watermill" || id == "glasshouse") return 0.8f;
                    break;
                case Season.Storm:
                    if (id == "watermill" || id == "well" || id == "portal" || id == "crater") return 1.5f;
                    if (id == "forge" || id == "dock") return 0.75f;
                    break;
            }
            return 1f;
        }

        /// <summary>Saatlik pasif altın (arayüzde gösterilir).</summary>
        public static float GoldPerHour(string id, int level) { return GoldPerMinute(id, level) * 60f; }

        // Hangi deneyi hangi bina, kaçıncı seviyede öğretir
        public static readonly List<CompoundUnlock> Unlocks = new List<CompoundUnlock>
        {
            // 1. mahalle (baştan açık)
            U("water", "watermill", 1), U("salt", "mine", 1),
            U("co2", "glasshouse", 2), U("rust", "forge", 2), U("peroxide", "well", 2),
            U("limestone", "mine", 3), U("ammonia", "glasshouse", 3), U("quicklime", "forge", 4),
            // 2. mahalle (zanaat ve araştırma)
            U("hcl", "workshop", 1), U("bakingsoda", "healer", 1), U("silica", "crystal", 1),
            U("lye", "workshop", 2), U("bluevitriol", "market", 2), U("saltpeter", "library", 2), U("coldpack", "windmill", 2),
            U("magnesia", "crystal", 3),
            // 3. mahalle (keşif ve arıtma)
            U("ethanol", "distillery", 1),
        };

        /// <summary>Bir deneyin zorluğu: önce mahalle, sonra bina seviyesi.</summary>
        public static int UnlockRank(CompoundUnlock u) { return u == null ? 0 : (Group(u.BuildingId) - 1) * 10 + u.Level; }

        static CompoundUnlock U(string c, string b, int l) { return new CompoundUnlock { CompoundId = c, BuildingId = b, Level = l }; }

        public static CompoundUnlock UnlockFor(string compoundId) { return Unlocks.FirstOrDefault(u => u.CompoundId == compoundId); }

        public static IEnumerable<CompoundUnlock> UnlocksOf(string buildingId) { return Unlocks.Where(u => u.BuildingId == buildingId); }

        public static IEnumerable<string> AllIds
        {
            get
            {
                yield return Tower;
                foreach (var v in VillageDatabase.Villagers) yield return v.Id;
            }
        }

        public static string NameTr(string id)
        {
            if (id == Tower) return Loc.T("Simya Kulesi", "Alchemist Tower");
            var v = VillageDatabase.GetVillager(id);
            return v != null ? v.BuildingTr : id;
        }

        // ----- Özel bina etkileri (seviye 1'in üstündeki her seviye için) -----
        /// <summary>Kronos Kum Saati Kulesi: demleme ve yükseltme süresi seviye başına %2 kısalır (en fazla %22).</summary>
        public static float ChronosFactor(int chronosLevel) { return 1f - 0.02f * Math.Max(0, Math.Min(11, chronosLevel - 1)); }
        /// <summary>Haberci Kuş Kulesi: görev altını seviye başına %3 artar.</summary>
        public static float AviaryGoldFactor(int level) { return 1f + 0.03f * Math.Max(0, level - 1); }
        /// <summary>Rünlü Hazine Mahzeni: gizemli sandık altını seviye başına %10 artar.</summary>
        public static float VaultFactor(int level) { return 1f + 0.10f * Math.Max(0, level - 1); }
        /// <summary>Kök Ev: Bilgi geliri seviye başına %3 artar.</summary>
        public static float HollowKnowledgeFactor(int level) { return 1f + 0.03f * Math.Max(0, level - 1); }
        /// <summary>Gizemli sandığın türü kulenin seviyesine göre (1 tahta kasa ... 6 kozmik sandık).</summary>
        public static int ChestTier(int towerLevel) { return Math.Max(1, Math.Min(6, 1 + (towerLevel - 1) / 2)); }

        // ----- Kule seviyesinin etkileri -----
        public static int CauldronCapacity(int towerLevel) { return Math.Min(12, 5 + towerLevel); }          // 6..12 atom
        public static int PassiveSlots(int towerLevel) { return Math.Min(5, 1 + towerLevel / 3); }           // 1..5 yuva
        public static int Builders(int towerLevel) { return 1 + (towerLevel >= 5 ? 1 : 0) + (towerLevel >= 10 ? 1 : 0); }

        public static string FormatDuration(long seconds)
        {
            string D = Loc.T(" gün", "d"), H = Loc.T(" sa", "h"), M = Loc.T(" dk", "m"), S = Loc.T(" sn", "s");
            if (seconds <= 0) return "0" + S;
            long d = seconds / 86400, h = seconds % 86400 / 3600, m = seconds % 3600 / 60, s = seconds % 60;
            if (d > 0) return d + D + (h > 0 ? " " + h + H : "");
            if (h > 0) return h + H + (m > 0 ? " " + m + M : "");
            if (m > 0) return m + M + (s > 0 ? " " + s + S : "");
            return s + S;
        }
    }
}
