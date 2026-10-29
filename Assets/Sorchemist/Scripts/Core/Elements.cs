// Sorchemist - Saf C# çekirdek (UnityEngine kullanmaz, test edilebilir)
// 118 elementin tamamı: atom numarası, sembol, Türkçe ad, atom kütlesi, kategori.
using System;
using System.Collections.Generic;

namespace Sorchemist.Core
{
    public enum ElementCategory
    {
        Nonmetal,        // Ametal
        NobleGas,        // Soy gaz
        AlkaliMetal,     // Alkali metal
        AlkalineEarth,   // Toprak alkali metal
        Metalloid,       // Yarı metal
        Halogen,         // Halojen
        PostTransition,  // Zayıf metal
        Transition,      // Geçiş metali
        Lanthanide,      // Lantanit
        Actinide         // Aktinit
    }

    [Serializable]
    public class ElementData
    {
        public int Number;
        public string Symbol;
        public string NameTr { get { return Loc.Pick(_nameTr, NameEn); } set { _nameTr = value; } }
        string _nameTr; public string NameEn;
        public float Mass;
        public ElementCategory Category;
        public int Period;      // 1-7
        public int Group;       // 1-18 (f-blok için 0)
        public int TableRow;    // Periyodik tablo ekranındaki satır (0-8; 7-8 f-blok)
        public int TableCol;    // Periyodik tablo ekranındaki sütun (0-17)
        public bool Radioactive => Number == 43 || Number == 61 || Number >= 84;

        public override string ToString() { return Symbol + " (" + NameTr + ")"; }
    }

    public static class ElementDatabase
    {
        // Format: Sembol|Türkçe ad|Kütle|Kategori kodu
        // Kodlar: N=Ametal G=Soy gaz A=Alkali E=Toprak alkali M=Yarı metal H=Halojen
        //         P=Zayıf metal T=Geçiş L=Lantanit C=Aktinit
        private const string Raw =
            "H|Hidrojen|1.008|N;He|Helyum|4.0026|G;Li|Lityum|6.94|A;Be|Berilyum|9.0122|E;B|Bor|10.81|M;" +
            "C|Karbon|12.011|N;N|Azot|14.007|N;O|Oksijen|15.999|N;F|Flor|18.998|H;Ne|Neon|20.180|G;" +
            "Na|Sodyum|22.990|A;Mg|Magnezyum|24.305|E;Al|Alüminyum|26.982|P;Si|Silisyum|28.085|M;P|Fosfor|30.974|N;" +
            "S|Kükürt|32.06|N;Cl|Klor|35.45|H;Ar|Argon|39.948|G;K|Potasyum|39.098|A;Ca|Kalsiyum|40.078|E;" +
            "Sc|Skandiyum|44.956|T;Ti|Titanyum|47.867|T;V|Vanadyum|50.942|T;Cr|Krom|51.996|T;Mn|Mangan|54.938|T;" +
            "Fe|Demir|55.845|T;Co|Kobalt|58.933|T;Ni|Nikel|58.693|T;Cu|Bakır|63.546|T;Zn|Çinko|65.38|T;" +
            "Ga|Galyum|69.723|P;Ge|Germanyum|72.630|M;As|Arsenik|74.922|M;Se|Selenyum|78.971|N;Br|Brom|79.904|H;" +
            "Kr|Kripton|83.798|G;Rb|Rubidyum|85.468|A;Sr|Stronsiyum|87.62|E;Y|İtriyum|88.906|T;Zr|Zirkonyum|91.224|T;" +
            "Nb|Niyobyum|92.906|T;Mo|Molibden|95.95|T;Tc|Teknesyum|98|T;Ru|Rutenyum|101.07|T;Rh|Rodyum|102.91|T;" +
            "Pd|Paladyum|106.42|T;Ag|Gümüş|107.87|T;Cd|Kadmiyum|112.41|T;In|İndiyum|114.82|P;Sn|Kalay|118.71|P;" +
            "Sb|Antimon|121.76|M;Te|Tellür|127.60|M;I|İyot|126.90|H;Xe|Ksenon|131.29|G;Cs|Sezyum|132.91|A;" +
            "Ba|Baryum|137.33|E;La|Lantan|138.91|L;Ce|Seryum|140.12|L;Pr|Praseodimyum|140.91|L;Nd|Neodimyum|144.24|L;" +
            "Pm|Prometyum|145|L;Sm|Samaryum|150.36|L;Eu|Evropiyum|151.96|L;Gd|Gadolinyum|157.25|L;Tb|Terbiyum|158.93|L;" +
            "Dy|Disprozyum|162.50|L;Ho|Holmiyum|164.93|L;Er|Erbiyum|167.26|L;Tm|Tulyum|168.93|L;Yb|İterbiyum|173.05|L;" +
            "Lu|Lutesyum|174.97|L;Hf|Hafniyum|178.49|T;Ta|Tantal|180.95|T;W|Tungsten|183.84|T;Re|Renyum|186.21|T;" +
            "Os|Osmiyum|190.23|T;Ir|İridyum|192.22|T;Pt|Platin|195.08|T;Au|Altın|196.97|T;Hg|Cıva|200.59|T;" +
            "Tl|Talyum|204.38|P;Pb|Kurşun|207.2|P;Bi|Bizmut|208.98|P;Po|Polonyum|209|P;At|Astatin|210|H;" +
            "Rn|Radon|222|G;Fr|Fransiyum|223|A;Ra|Radyum|226|E;Ac|Aktinyum|227|C;Th|Toryum|232.04|C;" +
            "Pa|Protaktinyum|231.04|C;U|Uranyum|238.03|C;Np|Neptünyum|237|C;Pu|Plütonyum|244|C;Am|Amerikyum|243|C;" +
            "Cm|Küriyum|247|C;Bk|Berkelyum|247|C;Cf|Kaliforniyum|251|C;Es|Aynştaynyum|252|C;Fm|Fermiyum|257|C;" +
            "Md|Mendelevyum|258|C;No|Nobelyum|259|C;Lr|Lavrensiyum|266|C;Rf|Rutherfordiyum|267|T;Db|Dubniyum|268|T;" +
            "Sg|Seaborgiyum|269|T;Bh|Bohriyum|270|T;Hs|Hassiyum|277|T;Mt|Meitneriyum|278|T;Ds|Darmstadtiyum|281|T;" +
            "Rg|Röntgenyum|282|T;Cn|Kopernikyum|285|T;Nh|Nihonyum|286|P;Fl|Flerovyum|289|P;Mc|Moskovyum|290|P;" +
            "Lv|Livermoryum|293|P;Ts|Tennesin|294|H;Og|Oganesson|294|G";

        private static List<ElementData> _all;
        private static Dictionary<string, ElementData> _bySymbol;

        public static IReadOnlyList<ElementData> All { get { Ensure(); return _all; } }
        public const int Count = 118;

        public static ElementData Get(string symbol)
        {
            Ensure();
            ElementData e;
            return _bySymbol.TryGetValue(symbol, out e) ? e : null;
        }

        public static ElementData Get(int number)
        {
            Ensure();
            return number >= 1 && number <= _all.Count ? _all[number - 1] : null;
        }

        private static void Ensure()
        {
            if (_all != null) return;
            EnsureData();
            English.ApplyElements(_all);
        }

        private static void EnsureData()
        {
            if (_all != null) return;
            _all = new List<ElementData>(Count);
            _bySymbol = new Dictionary<string, ElementData>();
            string[] entries = Raw.Split(';');
            for (int i = 0; i < entries.Length; i++)
            {
                string[] p = entries[i].Split('|');
                var e = new ElementData
                {
                    Number = i + 1,
                    Symbol = p[0],
                    NameTr = p[1],
                    Mass = float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture),
                    Category = ParseCategory(p[3][0])
                };
                AssignPosition(e);
                _all.Add(e);
                _bySymbol[e.Symbol] = e;
            }
            if (_all.Count != Count)
                throw new InvalidOperationException("Element tablosu bozuk: " + _all.Count);
        }

        private static ElementCategory ParseCategory(char c)
        {
            switch (c)
            {
                case 'N': return ElementCategory.Nonmetal;
                case 'G': return ElementCategory.NobleGas;
                case 'A': return ElementCategory.AlkaliMetal;
                case 'E': return ElementCategory.AlkalineEarth;
                case 'M': return ElementCategory.Metalloid;
                case 'H': return ElementCategory.Halogen;
                case 'P': return ElementCategory.PostTransition;
                case 'T': return ElementCategory.Transition;
                case 'L': return ElementCategory.Lanthanide;
                default: return ElementCategory.Actinide;
            }
        }

        // Periyot/grup ve ekran konumunu atom numarasından hesaplar.
        private static void AssignPosition(ElementData e)
        {
            int z = e.Number;
            int[] periodEnd = { 2, 10, 18, 36, 54, 86, 118 };
            int period = 1;
            while (z > periodEnd[period - 1]) period++;
            int start = period == 1 ? 1 : periodEnd[period - 2] + 1;
            int idx = z - start; // periyot içindeki sıra (0 tabanlı)
            int group;

            if (period == 1) group = z == 1 ? 1 : 18;
            else if (period <= 3) group = idx < 2 ? idx + 1 : idx + 11;   // 2 s + 6 p
            else if (period <= 5) group = idx + 1;                       // 18 eleman
            else
            {
                // 6. ve 7. periyot: 2 s, 15 f (lantanit/aktinit, La/Ac dahil), sonra 9 d (grup 4-12), 6 p
                if (idx < 2) group = idx + 1;
                else if (idx < 17) group = 0;          // f-blok
                else group = idx - 13;                 // idx 17 -> grup 4
            }

            e.Period = period;
            e.Group = group;
            if (group == 0)
            {
                e.TableRow = period == 6 ? 7 : 8;
                e.TableCol = 2 + (idx - 2);            // 2..16
            }
            else
            {
                e.TableRow = period - 1;
                e.TableCol = group - 1;
            }
        }

        public static string CategoryNameTr(ElementCategory c)
        {
            if (Loc.En) return English.Category(c);
            switch (c)
            {
                case ElementCategory.Nonmetal: return "Ametal";
                case ElementCategory.NobleGas: return "Soy gaz";
                case ElementCategory.AlkaliMetal: return "Alkali metal";
                case ElementCategory.AlkalineEarth: return "Toprak alkali";
                case ElementCategory.Metalloid: return "Yarı metal";
                case ElementCategory.Halogen: return "Halojen";
                case ElementCategory.PostTransition: return "Zayıf metal";
                case ElementCategory.Transition: return "Geçiş metali";
                case ElementCategory.Lanthanide: return "Lantanit";
                default: return "Aktinit";
            }
        }
    }
}
