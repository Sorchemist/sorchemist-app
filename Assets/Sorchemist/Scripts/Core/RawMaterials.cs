// Haritadan toplanan ham maddeler ve laboratuvarda saf elementlere ayrıştırılmaları.
using System;
using System.Collections.Generic;

namespace Sorchemist.Core
{
    public enum ExtractionMethod
    {
        Evaporation,     // Buharlaştırma
        Electrolysis,    // Elektroliz
        Distillation,    // Damıtma
        Calcination,     // Kalsinasyon (kızdırma)
        Smelting,        // Ergitme / indirgeme
        Filtration,      // Süzme
        Arcane           // Büyü: gizemli kristal
    }

    [Serializable]
    public class ElementYield
    {
        public string Symbol;
        public int Min;
        public int Max;
        public ElementYield(string s, int min, int max) { Symbol = s; Min = min; Max = max; }
    }

    [Serializable]
    public class RawMaterialData
    {
        public string Id;
        public string NameTr;
        public string WhereTr;        // Haritada nerede bulunur
        public string FlavorTr;
        public ExtractionMethod Method;
        public List<ElementYield> Yields = new List<ElementYield>();
        public float RegrowSeconds;   // Haritada yeniden belirme süresi
        public string ColorHex;
        public bool RequiresHeavyElements; // "Ağır Elementler" yeteneği gerekir
    }

    public static class RawMaterialDatabase
    {
        private static List<RawMaterialData> _all;
        public static IReadOnlyList<RawMaterialData> All { get { Ensure(); return _all; } }

        // Gizemli kristalden çıkabilecek nadir elementler (büyü!)
        public static readonly string[] MysteryCommon = { "Ne", "Ar", "He", "Li", "B", "Zn", "Sn", "I" };
        public static readonly string[] MysteryRare = { "Ag", "Au", "Hg", "Pt", "W", "Ti", "U", "Xe" };

        public static RawMaterialData Get(string id)
        {
            Ensure();
            return _all.Find(r => r.Id == id);
        }

        private static RawMaterialData Add(string id, string name, string where, ExtractionMethod m,
            float regrow, string color, string flavor, params ElementYield[] yields)
        {
            var r = new RawMaterialData
            {
                Id = id, NameTr = name, WhereTr = where, Method = m, RegrowSeconds = regrow,
                ColorHex = color, FlavorTr = flavor
            };
            r.Yields.AddRange(yields);
            _all.Add(r);
            return r;
        }

        private static void Ensure()
        {
            if (_all != null) return;
            _all = new List<RawMaterialData>();
            Add("seawater", "Deniz Suyu", "Köy iskelesi", ExtractionMethod.Electrolysis, 60, "#2E86C1",
                "Elektrik akımı suyu H2 ve O2'ye, tuzu Na ve Cl'ye ayırır.",
                new ElementYield("H", 2, 4), new ElementYield("O", 1, 2),
                new ElementYield("Na", 1, 2), new ElementYield("Cl", 1, 2));
            Add("air", "Sabah Havası (kavanozda)", "Tepe başı", ExtractionMethod.Distillation, 45, "#D6EAF8",
                "Hava sıvılaştırılıp damıtılınca azot (%78) ve oksijen (%21) ayrılır.",
                new ElementYield("N", 2, 3), new ElementYield("O", 1, 2), new ElementYield("Ar", 0, 1));
            Add("limestone", "Kireç Taşı", "Taş ocağı", ExtractionMethod.Calcination, 90, "#E5E1D3",
                "CaCO3 kızdırılınca CaO ve CO2'ye ayrışır.",
                new ElementYield("Ca", 1, 2), new ElementYield("C", 1, 2), new ElementYield("O", 1, 3));
            Add("ash", "Odun Külü", "Fırıncının ocağı", ExtractionMethod.Filtration, 60, "#7B7D7D",
                "Külden süzülen 'potas' eskiden sabun yapımında kullanılırdı.",
                new ElementYield("K", 1, 2), new ElementYield("C", 1, 2), new ElementYield("O", 0, 1));
            Add("hematite", "Kızıl Demir Cevheri", "Maden girişi", ExtractionMethod.Smelting, 120, "#922B21",
                "Hematit (Fe2O3) karbonla ısıtılınca demir açığa çıkar.",
                new ElementYield("Fe", 1, 2), new ElementYield("O", 1, 2));
            Add("sulfur", "Sarı Kükürt Kayası", "Uyuyan volkan", ExtractionMethod.Smelting, 120, "#F4D03F",
                "Volkan ağızlarında saf kükürt kristalleri birikir. Çürük yumurta kokar!",
                new ElementYield("S", 1, 2));
            Add("malachite", "Malahit", "Yeşil mağara", ExtractionMethod.Smelting, 150, "#1E8449",
                "Yeşil bakır karbonat minerali. Ergitilince parlak bakır verir.",
                new ElementYield("Cu", 1, 2), new ElementYield("C", 0, 1), new ElementYield("O", 1, 2),
                new ElementYield("H", 0, 1));
            Add("sand", "Altın Kum", "Nehir kıyısı", ExtractionMethod.Smelting, 60, "#F5CBA7",
                "Kumun büyük kısmı silisyum dioksittir (SiO2).",
                new ElementYield("Si", 1, 1), new ElementYield("O", 1, 2));
            Add("magnesite", "Ak Magnezit", "Beyaz yamaçlar", ExtractionMethod.Electrolysis, 120, "#FDFEFE",
                "Magnezyum karbonat. Elektrolizle hafif ve parlak magnezyum metali elde edilir.",
                new ElementYield("Mg", 1, 2), new ElementYield("C", 0, 1), new ElementYield("O", 1, 2));
            Add("guano", "Yarasa Gübresi", "Yarasa mağarası", ExtractionMethod.Filtration, 90, "#6E2C00",
                "Tarihte nitratın ve fosforun en değerli kaynaklarından biri. Kokusuna katlan!",
                new ElementYield("N", 1, 2), new ElementYield("H", 1, 3), new ElementYield("P", 0, 1));
            Add("dewcap", "Çiy Mantarı", "Orman açıklığı", ExtractionMethod.Distillation, 30, "#A569BD",
                "Sabah çiyini toplayan büyülü mantarlar. Damıtınca saf su elementleri verir.",
                new ElementYield("H", 2, 3), new ElementYield("O", 1, 1), new ElementYield("C", 0, 1));
            var crystal = Add("crystal", "Gizemli Kristal", "Eski taş çemberi", ExtractionMethod.Arcane, 300, "#BB8FCE",
                "Kimse nereden geldiğini bilmiyor. Her kırılışta farklı bir element fısıldıyor.");
            crystal.RequiresHeavyElements = false;
        }

        public static string MethodNameTr(ExtractionMethod m)
        {
            switch (m)
            {
                case ExtractionMethod.Evaporation: return "Buharlaştırma";
                case ExtractionMethod.Electrolysis: return "Elektroliz";
                case ExtractionMethod.Distillation: return "Damıtma";
                case ExtractionMethod.Calcination: return "Kalsinasyon";
                case ExtractionMethod.Smelting: return "Ergitme";
                case ExtractionMethod.Filtration: return "Süzme";
                default: return "Büyülü Kırılma";
            }
        }
    }

    public static class Extractor
    {
        /// <summary>
        /// Ham maddeyi elementlere ayırır. quality 0..1 arası minioyun başarısıdır
        /// (damıtma sıcaklığını ne kadar iyi tuttuğun). bonusYield yetenek ağacından gelir.
        /// </summary>
        public static Dictionary<string, int> Extract(RawMaterialData raw, float quality, int bonusYield,
            bool luckyCharm, Random rng)
        {
            var result = new Dictionary<string, int>();
            quality = Math.Max(0f, Math.Min(1f, quality));

            if (raw.Method == ExtractionMethod.Arcane)
            {
                double rareChance = 0.12 + quality * 0.18 + (luckyCharm ? 0.2 : 0);
                string[] pool = rng.NextDouble() < rareChance
                    ? RawMaterialDatabase.MysteryRare : RawMaterialDatabase.MysteryCommon;
                result[pool[rng.Next(pool.Length)]] = 1 + bonusYield;
                return result;
            }

            foreach (var y in raw.Yields)
            {
                // Kalite, minimum ile maksimum arasında nereye düşeceğini belirler (+ biraz şans)
                double t = quality * 0.8 + rng.NextDouble() * 0.2;
                int amount = y.Min + (int)Math.Round((y.Max - y.Min) * t);
                if (amount > 0 && bonusYield > 0) amount += bonusYield;
                if (quality < 0.25 && amount > 0) amount -= 1; // Kötü damıtma: kayıp
                if (amount > 0) result[y.Symbol] = amount;
            }
            return result;
        }
    }
}
