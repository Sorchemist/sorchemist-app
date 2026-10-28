// İki kollu yetenek ağacı: Bilim ve Büyü.
using System;
using System.Collections.Generic;

namespace Sorchemist.Core
{
    public enum SkillBranch { Science, Magic }

    [Serializable]
    public class SkillData
    {
        public string Id;
        public SkillBranch Branch;
        public string NameTr;
        public string DescriptionTr;
        public int Cost;
        public string Requires;   // Önce açılması gereken yetenek (null = yok)
    }

    public static class SkillDatabase
    {
        // Sabit kimlikler (kodda kullanılır)
        public const string PreciseScale = "sci_scale";     // Kazan kapasitesi +3
        public const string Thermometer = "sci_thermo";     // Hedef ısı bandını gösterir
        public const string Column = "sci_column";          // Ayrıştırmada +1 verim
        public const string BigCauldron = "sci_bigcauldron";// Kazan kapasitesi +3 daha
        public const string Periodic = "sci_periodic";      // Periyodik tabloda bilgi kartları
        public const string Patience = "mag_patience";      // Isı toleransı +1 kademe
        public const string PurpleFlame = "mag_purple";     // Mor alev (görsel)
        public const string CrystalRain = "mag_crystals";   // Başarıda kristal yağmuru (görsel)
        public const string TimeBend = "mag_timebend";      // Minioyunlar %30 daha kısa
        public const string Luck = "mag_luck";              // Gizemli kristalden nadir element şansı

        private static List<SkillData> _all;
        public static IReadOnlyList<SkillData> All { get { Ensure(); return _all; } }
        public static SkillData Get(string id) { Ensure(); return _all.Find(s => s.Id == id); }

        private static void Add(string id, SkillBranch b, string name, int cost, string req, string desc)
        {
            _all.Add(new SkillData { Id = id, Branch = b, NameTr = name, Cost = cost, Requires = req, DescriptionTr = desc });
        }

        private static void Ensure()
        {
            if (_all != null) return;
            _all = new List<SkillData>();
            Add(PreciseScale, SkillBranch.Science, "Hassas Terazi", 1, null, "Kazana 3 atom daha sığar (6 -> 9).");
            Add(Thermometer, SkillBranch.Science, "Termometre", 1, null, "Isı minioyununda hedef bölgeyi gösterir.");
            Add(Column, SkillBranch.Science, "Damıtma Kolonu", 2, Thermometer, "Ayrıştırmada her elementten +1 verim.");
            Add(BigCauldron, SkillBranch.Science, "Bakır Kazan", 2, PreciseScale, "Kazana 3 atom daha sığar (9 -> 12).");
            Add(Periodic, SkillBranch.Science, "Mendeleyev'in Defteri", 1, null, "Periyodik tablodaki elementlerin bilgi kartlarını açar.");
            Add(Patience, SkillBranch.Magic, "Sabır Büyüsü", 1, null, "Tepkimeler bir kademe fazla/eksik ısıyı tolere eder.");
            Add(PurpleFlame, SkillBranch.Magic, "Mor Alev", 1, null, "Bunsen bekinin alevi mor yanar. Sadece havalı.");
            Add(CrystalRain, SkillBranch.Magic, "Kristal Yağmuru", 1, PurpleFlame, "Başarılı iksirlerde laboratuvara kristaller yağar.");
            Add(TimeBend, SkillBranch.Magic, "Zaman Bükme", 2, Patience, "Minioyunlar %30 daha kısa sürer.");
            Add(Luck, SkillBranch.Magic, "Şans Tılsımı", 2, null, "Gizemli kristalden nadir element çıkma şansı artar.");
        }
    }
}
