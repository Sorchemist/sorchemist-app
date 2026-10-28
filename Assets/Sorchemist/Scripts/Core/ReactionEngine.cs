// Kazandaki atomları + ısı + karıştırma kalitesini değerlendirir.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sorchemist.Core
{
    public class BrewInput
    {
        public Dictionary<string, int> Atoms = new Dictionary<string, int>();
        public HeatLevel Heat = HeatLevel.Cold;
        public float StirQuality = 0f;   // 0..1 (karıştırma minioyunu)
        public int CauldronCapacity = 6;
        public bool PatienceSpell;       // Büyü yeteneği: ısı toleransı +1 kademe
    }

    public class BrewResult
    {
        public bool Success;
        public CompoundData Compound;    // Başarılıysa üretilen bileşik
        public FailEntry Fail;           // Başarısızsa Fail-Dex girdisi
        public string MessageTr;
        public string HintTr;            // Öğretici ipucu
        public int Xp;
    }

    public static class ReactionEngine
    {
        static readonly string[] Alkali = { "Li", "Na", "K", "Rb", "Cs", "Fr" };
        static readonly string[] Noble = { "He", "Ne", "Ar", "Kr", "Xe", "Rn", "Og" };
        static readonly string[] Hot = { "U", "Pu", "Ra", "Po", "Th", "Am", "Rn" };

        public static BrewResult Brew(BrewInput input)
        {
            var atoms = input.Atoms.Where(kv => kv.Value > 0).ToDictionary(kv => kv.Key, kv => kv.Value);
            int total = atoms.Values.Sum();

            if (total == 0)
                return new BrewResult { MessageTr = "Kazan boş. Aqualf boş kazanı karıştırarak meditasyon yaptı." };

            if (total > input.CauldronCapacity)
                return FailWith("overflow", null);

            var match = CompoundDatabase.FindByAtoms(atoms);
            if (match != null) return EvaluateKnown(match, input);

            return EvaluateUnknown(atoms, input);
        }

        private static BrewResult EvaluateKnown(CompoundData c, BrewInput input)
        {
            int tol = input.PatienceSpell ? 1 : 0;
            int heat = (int)input.Heat;
            int min = (int)c.MinHeat - tol;
            int max = (int)c.MaxHeat + tol;

            if (heat > max)
            {
                if (c.Id == "peroxide") return FailWith("peroxide_foam", "Hidrojen peroksit ısıya dayanamaz: soğuk tut!");
                if (input.Heat == HeatLevel.Inferno) return FailWith("roof_blown", c.NameTr + " için fazla sıcak. İdeal: " + Range(c));
                return FailWith("burnt", c.NameTr + " için fazla sıcak. İdeal: " + Range(c));
            }
            if (heat < min)
                return FailWith("lukewarm_sludge", c.NameTr + " için daha fazla ısı gerek. İdeal: " + Range(c));
            if (c.NeedsStir && input.StirQuality < 0.5f)
                return FailWith("lumpy", c.NameTr + " iyice karıştırılmalı!");

            return new BrewResult
            {
                Success = true,
                Compound = c,
                Xp = c.Xp,
                MessageTr = c.NameTr + " (" + c.Formula + ") oluştu! " + c.FlavorTr
            };
        }

        private static BrewResult EvaluateUnknown(Dictionary<string, int> atoms, BrewInput input)
        {
            Func<string, int> n = s => { int v; return atoms.TryGetValue(s, out v) ? v : 0; };
            bool hasAlkali = Alkali.Any(a => n(a) > 0);
            bool onlyNoble = atoms.Keys.All(k => Noble.Contains(k));
            int heat = (int)input.Heat;

            if (n("Au") > 0) return FailWith("midas", "Altın çok soy bir metaldir; neredeyse hiçbir şeyle tepkimeye girmez.");
            if (Hot.Any(r => n(r) > 0)) return FailWith("glow_beard", "Radyoaktif elementlerle oynama!");
            if (n("Hg") > 0) return FailWith("mercury_hat", "Cıva güzel ama tehlikeli.");
            if (onlyNoble) return FailWith("noble_snub", "Soy gazlar tepkimeye girmez.");
            if (hasAlkali && n("H") >= 2 && n("O") >= 1) return FailWith("alkali_boom", "Alkali metal + su = tehlike!");
            if (n("Mg") > 0 && input.Heat == HeatLevel.Inferno) return FailWith("blinding_flash", "Magnezyum oksit için 1 Mg + 1 O dene.");
            if (n("Cl") >= 1 && n("Na") == 0 && n("H") == 0) return FailWith("green_cloud", "Klor bir eş arıyor: Na veya H.");
            if (n("S") > 0 && heat >= (int)HeatLevel.Hot) return FailWith("rotten_egg", null);
            if (n("H") > 0 && n("O") > 0 && heat >= (int)HeatLevel.Hot && atoms.Count == 2)
                return FailWith("hydrogen_pop", "Su için tam olarak 2 H ve 1 O gerekir.");

            var near = FindNearMiss(atoms);
            if (near != null)
                return FailWith("near_miss", "Bu karışım " + near.NameTr + " (" + near.Formula + ") bileşiğine çok benziyor...");

            return FailWith("mystery_goo", null);
        }

        // Toplam farkı 1 atom olan bilinen bir bileşik var mı?
        public static CompoundData FindNearMiss(Dictionary<string, int> atoms)
        {
            foreach (var c in CompoundDatabase.All)
            {
                var keys = new HashSet<string>(c.Atoms.Keys);
                keys.UnionWith(atoms.Keys);
                int diff = 0;
                foreach (var k in keys)
                {
                    int a, b;
                    c.Atoms.TryGetValue(k, out a);
                    atoms.TryGetValue(k, out b);
                    diff += Math.Abs(a - b);
                }
                if (diff == 1) return c;
            }
            return null;
        }

        private static string Range(CompoundData c)
        {
            return c.MinHeat == c.MaxHeat
                ? CompoundDatabase.HeatNameTr(c.MinHeat)
                : CompoundDatabase.HeatNameTr(c.MinHeat) + "–" + CompoundDatabase.HeatNameTr(c.MaxHeat);
        }

        private static BrewResult FailWith(string failId, string hint)
        {
            var f = FailDexDatabase.Get(failId);
            return new BrewResult
            {
                Success = false,
                Fail = f,
                Xp = f != null ? f.Xp : 0,
                MessageTr = f != null ? f.TitleTr + "! " + f.DescriptionTr : "Bir şeyler ters gitti.",
                HintTr = hint
            };
        }
    }
}
