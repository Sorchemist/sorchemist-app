// Gerçek kimyaya dayalı bileşikler ve oyun içi "büyü" etkileri.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sorchemist.Core
{
    // İksirin köylü üzerindeki oyun etkisi
    public enum PotionEffect
    {
        None,
        Hydrate,      // Su: susuzluk, kuraklık
        Preserve,     // Tuz: yiyecek saklama
        Fizz,         // CO2: gazoz, kabarma
        Growth,       // Amonyak: gübre, büyüme
        Fireworks,    // Güherçile: havai fişek
        Soothe,       // Karbonat: mide yatıştırıcı
        Disinfect,    // Hidrojen peroksit: yara temizleme
        PestControl,  // Göztaşı: bordo bulamacı, böcek/mantar
        Light,        // Magnezyum oksit: göz kamaştıran ışık
        Warmth,       // Sönmemiş kireç: ısı veren
        Cool,         // Amonyum nitrat: soğuk kompres
        Cleanse,      // Sudkostik: sabun yapımı
        Build,        // Kireçtaşı: harç, yapı
        Pigment,      // Pas: kırmızı boya
        Fuel,         // Etanol: lamba yakıtı
        Dissolve,     // Tuz ruhu: pas çözücü
        Glass         // Silika: cam
    }

    public enum HeatLevel { Cold = 0, Warm = 1, Hot = 2, Inferno = 3 }

    [Serializable]
    public class CompoundData
    {
        public string Id;
        public string Formula;      // Görsel formül, ör. "H2O"
        public string NameTr { get { return Loc.Pick(_nameTr, NameEn); } set { _nameTr = value; } }
        string _nameTr; public string NameEn;
        public string FlavorTr { get { return Loc.Pick(_flavorTr, FlavorEn); } set { _flavorTr = value; } }     // Kısa, eğitici açıklama
        string _flavorTr; public string FlavorEn;
        public Dictionary<string, int> Atoms = new Dictionary<string, int>();
        public PotionEffect Effect;
        public HeatLevel MinHeat;
        public HeatLevel MaxHeat;
        public bool NeedsStir;      // Karıştırma minioyunu gerekli mi
        public string ColorHex;     // Sıvı rengi (Unity tarafı kullanır)
        public int Xp;

        public int AtomCount { get { return Atoms.Values.Sum(); } }
    }

    public static class CompoundDatabase
    {
        private static List<CompoundData> _all;
        public static IReadOnlyList<CompoundData> All { get { Ensure(); return _all; } }

        public static CompoundData Get(string id)
        {
            Ensure();
            return _all.FirstOrDefault(c => c.Id == id);
        }

        public static CompoundData FindByAtoms(IDictionary<string, int> atoms)
        {
            Ensure();
            foreach (var c in _all)
                if (SameComposition(c.Atoms, atoms)) return c;
            return null;
        }

        public static bool SameComposition(IDictionary<string, int> a, IDictionary<string, int> b)
        {
            var ac = a.Where(kv => kv.Value > 0).ToList();
            var bc = b.Where(kv => kv.Value > 0).ToList();
            if (ac.Count != bc.Count) return false;
            foreach (var kv in ac)
            {
                int v;
                if (!b.TryGetValue(kv.Key, out v) || v != kv.Value) return false;
            }
            return true;
        }

        // "H2O" gibi bir formülü atom sayılarına çevirir (parantez desteklemez; gerekmez).
        public static Dictionary<string, int> ParseFormula(string f)
        {
            var d = new Dictionary<string, int>();
            int i = 0;
            while (i < f.Length)
            {
                if (!char.IsUpper(f[i])) throw new FormatException("Formül hatası: " + f);
                var sym = new StringBuilder().Append(f[i++]);
                while (i < f.Length && char.IsLower(f[i])) sym.Append(f[i++]);
                int n = 0;
                while (i < f.Length && char.IsDigit(f[i])) n = n * 10 + (f[i++] - '0');
                if (n == 0) n = 1;
                string s = sym.ToString();
                if (ElementDatabase.Get(s) == null) throw new FormatException("Bilinmeyen element: " + s);
                int cur;
                d.TryGetValue(s, out cur);
                d[s] = cur + n;
            }
            return d;
        }

        public static string Pretty(string ascii)
        {
            const string sub = "0123456789"; // Unicode alt simgeler bazı fontlarda yok; güvenli ASCII
            var sb = new StringBuilder();
            foreach (char ch in ascii) sb.Append(char.IsDigit(ch) ? sub[ch - '0'] : ch);
            return sb.ToString();
        }

        private static void Add(string id, string formula, string name, PotionEffect eff,
            HeatLevel min, HeatLevel max, bool stir, string color, int xp, string flavor)
        {
            _all.Add(new CompoundData
            {
                Id = id,
                Formula = Pretty(formula),
                NameTr = name,
                Atoms = ParseFormula(formula),
                Effect = eff,
                MinHeat = min,
                MaxHeat = max,
                NeedsStir = stir,
                ColorHex = color,
                Xp = xp,
                FlavorTr = flavor
            });
        }

        private static void Ensure()
        {
            if (_all != null) return;
            EnsureData();
            English.ApplyCompounds(_all);
        }

        private static void EnsureData()
        {
            if (_all != null) return;
            _all = new List<CompoundData>();
            Add("water", "H2O", "Su", PotionEffect.Hydrate, HeatLevel.Cold, HeatLevel.Warm, false, "#5EC8FF", 10,
                "Hidrojen oksijenle yanınca su oluşur. Hayatın en sade iksiri.");
            Add("salt", "NaCl", "Sofra Tuzu", PotionEffect.Preserve, HeatLevel.Cold, HeatLevel.Hot, true, "#F4F4F4", 15,
                "Sodyum ve klor iyonik bağ kurar. Tuz, mikropları kurutarak yiyecekleri korur.");
            Add("co2", "CO2", "Karbondioksit", PotionEffect.Fizz, HeatLevel.Warm, HeatLevel.Hot, false, "#CFE8E4", 15,
                "Karbon yanınca CO2 çıkar. Gazozdaki kabarcıklar ondan.");
            Add("ammonia", "NH3", "Amonyak", PotionEffect.Growth, HeatLevel.Hot, HeatLevel.Hot, true, "#B9F27C", 25,
                "Haber-Bosch yöntemi: azot + hidrojen, ısı ve basınçla. Dünyanın gübresi.");
            Add("saltpeter", "KNO3", "Güherçile", PotionEffect.Fireworks, HeatLevel.Warm, HeatLevel.Hot, true, "#FFD166", 30,
                "Potasyum nitrat güçlü bir oksitleyicidir; barut ve havai fişeklerin kalbidir.");
            Add("bakingsoda", "NaHCO3", "Yemek Sodası", PotionEffect.Soothe, HeatLevel.Cold, HeatLevel.Warm, true, "#FFFFFF", 30,
                "Sodyum bikarbonat bir bazdır; mide asidini nötrleştirir.");
            Add("peroxide", "H2O2", "Hidrojen Peroksit", PotionEffect.Disinfect, HeatLevel.Cold, HeatLevel.Cold, false, "#E0F7FF", 25,
                "Fazladan bir oksijen! Mikropları oksitleyerek yaraları temizler. Isıtılırsa bozunur.");
            Add("bluevitriol", "CuSO4", "Göztaşı", PotionEffect.PestControl, HeatLevel.Warm, HeatLevel.Hot, true, "#1E6FFF", 35,
                "Bakır sülfatın mavi kristalleri. Kireçle karışınca bağlara 'bordo bulamacı' olur.");
            Add("magnesia", "MgO", "Magnezyum Oksit", PotionEffect.Light, HeatLevel.Inferno, HeatLevel.Inferno, false, "#FFFFFF", 35,
                "Magnezyum göz kamaştıran beyaz bir alevle yanar. Eski fotoğrafçıların flaşı!");
            Add("quicklime", "CaO", "Sönmemiş Kireç", PotionEffect.Warmth, HeatLevel.Inferno, HeatLevel.Inferno, false, "#FFF3D6", 30,
                "Kireçtaşı çok ısıtılınca CaO olur. Suyla karşılaşınca ortalığı ısıtır.");
            Add("coldpack", "N2H4O3", "Amonyum Nitrat", PotionEffect.Cool, HeatLevel.Cold, HeatLevel.Cold, true, "#DFF6FF", 40,
                "NH4NO3 suda çözünürken ısı emer (endotermik). Soğuk kompreslerin sırrı.");
            Add("lye", "NaOH", "Sudkostik", PotionEffect.Cleanse, HeatLevel.Cold, HeatLevel.Warm, true, "#F0E6FF", 25,
                "Güçlü bir baz. Yağla tepkimeye girince sabun olur (sabunlaşma).");
            Add("limestone", "CaCO3", "Kireçtaşı", PotionEffect.Build, HeatLevel.Cold, HeatLevel.Warm, true, "#E8E2D0", 30,
                "Tebeşir, mermer, deniz kabuğu... Harcın ve yapıların temeli.");
            Add("rust", "Fe2O3", "Demir(III) Oksit", PotionEffect.Pigment, HeatLevel.Warm, HeatLevel.Hot, false, "#B5452B", 30,
                "Pas! Mağara resimlerindeki kırmızı aşı boyası da budur.");
            Add("ethanol", "C2H6O", "Etanol", PotionEffect.Fuel, HeatLevel.Warm, HeatLevel.Warm, true, "#FDF6E3", 40,
                "Mavi alevle temiz yanan bir alkol. Lambalar için mükemmel yakıt.");
            Add("hcl", "HCl", "Tuz Ruhu", PotionEffect.Dissolve, HeatLevel.Cold, HeatLevel.Warm, false, "#F7FFB0", 20,
                "Hidroklorik asit. Midende de var! Pası ve kireci çözer.");
            Add("silica", "SiO2", "Silika (Cam)", PotionEffect.Glass, HeatLevel.Inferno, HeatLevel.Inferno, false, "#D6F5FF", 35,
                "Kum çok yüksek sıcaklıkta erir ve soğuyunca cam olur.");
        }

        public static string EffectNameTr(PotionEffect e)
        {
            if (Loc.En) return English.Effect(e);
            switch (e)
            {
                case PotionEffect.Hydrate: return "Sulama";
                case PotionEffect.Preserve: return "Koruma";
                case PotionEffect.Fizz: return "Köpürtme";
                case PotionEffect.Growth: return "Büyütme";
                case PotionEffect.Fireworks: return "Havai Fişek";
                case PotionEffect.Soothe: return "Yatıştırma";
                case PotionEffect.Disinfect: return "Arındırma";
                case PotionEffect.PestControl: return "Haşere Kovma";
                case PotionEffect.Light: return "Işık";
                case PotionEffect.Warmth: return "Isıtma";
                case PotionEffect.Cool: return "Serinletme";
                case PotionEffect.Cleanse: return "Temizlik";
                case PotionEffect.Build: return "İnşa";
                case PotionEffect.Pigment: return "Boya";
                case PotionEffect.Fuel: return "Yakıt";
                case PotionEffect.Dissolve: return "Çözme";
                case PotionEffect.Glass: return "Cam";
                default: return "Yok";
            }
        }

        public static string HeatNameTr(HeatLevel h)
        {
            if (Loc.En) return English.Heat(h);
            switch (h)
            {
                case HeatLevel.Cold: return "Soğuk";
                case HeatLevel.Warm: return "Ilık";
                case HeatLevel.Hot: return "Sıcak";
                default: return "Cehennem";
            }
        }
    }
}
