// Pasif özellikler: Bilgi Puanı ile açılır, kulenin seviyesine göre sayısı artan yuvalara takılır.
// Yalnızca takılı (yuvadaki) pasifler etki eder.
using System;
using System.Collections.Generic;

namespace Sorchemist.Core
{
    [Serializable]
    public class PassiveData
    {
        public string Id;
        public string NameTr { get { return Loc.Pick(_nameTr, NameEn); } set { _nameTr = value; } }
        string _nameTr; public string NameEn;
        public string DescriptionTr { get { return Loc.Pick(_descriptionTr, DescriptionEn); } set { _descriptionTr = value; } }
        string _descriptionTr; public string DescriptionEn;
        public int Cost;          // Bilgi Puanı
        public string Icon;       // Sanat anahtarı (ör. el_79)
    }

    public static class PassiveDatabase
    {
        public const string GoldPurse = "p_gold";        // Altın geliri +%25
        public const string Owl = "p_owl";               // Bilgi geliri +%25
        public const string Builder = "p_builder";       // Yükseltme süreleri -%15
        public const string PatientPot = "p_brew";       // Demleme süresi -%30
        public const string Column = "p_column";         // Ayrıştırmada her elementten +1
        public const string Luck = "p_luck";             // Gizemli kutuda ikinci element şansı
        public const string TimeSand = "p_sand";         // Gizemli kutu 5 saatte bir
        public const string Mendeleev = "p_mendeleev";   // Element kartlarında bilimsel bilgi
        public const string CopperPot = "p_copper";      // Kazana +2 atom
        public const string PurpleFlame = "p_purple";    // Mor alev + kristal yağmuru (görsel)

        private static List<PassiveData> _all;
        public static IReadOnlyList<PassiveData> All { get { Ensure(); return _all; } }
        public static PassiveData Get(string id) { Ensure(); return _all.Find(p => p.Id == id); }

        static void Add(string id, string name, int cost, string icon, string desc)
        {
            _all.Add(new PassiveData { Id = id, NameTr = name, Cost = cost, Icon = icon, DescriptionTr = desc });
        }

        static void Ensure()
        {
            if (_all != null) return;
            EnsureData();
            English.ApplyPassives(_all);
        }

        static void EnsureData()
        {
            if (_all != null) return;
            _all = new List<PassiveData>();
            Add(Mendeleev, "Mendeleyev'in Defteri", 10, "el_101", "Element kartlarında atom kütlesi, periyot ve grup bilgisi görünür.");
            Add(PurpleFlame, "Mor Alev", 15, "el_19", "Kazanın alevi mor yanar, başarılı iksirlerde kristaller yağar.");
            Add(GoldPurse, "Altın Kesesi", 20, "el_79", "Köyün altın geliri %25 artar.");
            Add(PatientPot, "Sabırlı Kazan", 25, "el_8", "İksirlerin demlenme süresi %30 kısalır.");
            Add(Owl, "Bilge Baykuş", 30, "el_53", "Bilgi Puanı geliri %25 artar.");
            Add(Column, "Damıtma Kolonu", 40, "el_14", "Ayrıştırmada her elementten 1 fazla çıkar.");
            Add(CopperPot, "Bakır Kazan", 45, "el_29", "Kazana 2 atom daha sığar.");
            Add(Luck, "Şans Tılsımı", 50, "el_47", "Gizemli Kutu'dan ikinci element çıkma şansı artar.");
            Add(Builder, "Hızlı Usta", 60, "el_26", "Bina yükseltme süreleri %15 kısalır.");
            Add(TimeSand, "Zaman Kumu", 80, "el_14", "Gizemli Kutu 6 yerine 5 saatte bir gelir.");
        }
    }
}
