// Köy binaları: her binanın 1-12 arası seviyesi var. Yükseltmeler gerçek zamanlı sürer
// (oyun kapalıyken de ilerler); seviye atladıkça yeni deneyler açılır ve gelir artar.
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
        public const string Tower = "tower";      // Aqualf'in Kulesi
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

        /// <summary>Bina başına dakikalık altın üretimi (yalnızca oyun açıkken).</summary>
        public static float GoldPerMinute(string id, int level)
        {
            return id == Tower ? 0 : level;
        }

        // Hangi deneyi hangi bina, kaçıncı seviyede öğretir
        public static readonly List<CompoundUnlock> Unlocks = new List<CompoundUnlock>
        {
            U("water", "sucu", 1), U("salt", "derici", 1),
            U("co2", "asci", 2), U("bakingsoda", "bal", 2), U("lye", "kopuk", 2),
            U("peroxide", "ebe", 3), U("bluevitriol", "terzi", 3), U("limestone", "ruzgar", 3), U("rust", "tokmak", 3),
            U("ammonia", "arici", 3),
            U("saltpeter", "zeytin", 4), U("magnesia", "fener", 4), U("quicklime", "tasusta", 4), U("hcl", "kunduraci", 4),
            U("coldpack", "komurcu", 5), U("ethanol", "oduncu", 5), U("silica", "comlekci", 5),
        };

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
            if (id == Tower) return Loc.T("Aqualf'in Kulesi", "Aqualf's Tower");
            var v = VillageDatabase.GetVillager(id);
            if (v == null) return id;
            return Loc.En ? v.RoleTr + "'s House" : v.RoleTr + " Evi";
        }

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
