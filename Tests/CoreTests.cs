// Çekirdek oyun kuralları için basit testler. Çalıştırmak için: cd Tests && dotnet run
using System;
using System.Collections.Generic;
using Sorchemist.Core;

static class CoreTests
{
    static int _fail;
    static void Check(bool ok, string name) { Console.WriteLine((ok ? "  OK   " : "  HATA ") + name); if (!ok) _fail++; }
    static Dictionary<string, int> A(string f) { return CompoundDatabase.ParseFormula(f); }
    static BrewResult B(string f, HeatLevel h, float stir = 1f, int cap = 12)
    { return ReactionEngine.Brew(new BrewInput { Atoms = A(f), Heat = h, StirQuality = stir, CauldronCapacity = cap }); }

    static int Main()
    {
        Console.WriteLine("Elementler");
        Check(ElementDatabase.All.Count == 118, "118 element");
        Check(ElementDatabase.Get("Fe").NameTr == "Demir" && ElementDatabase.Get(26).Symbol == "Fe", "Demir = 26");
        Check(ElementDatabase.Get("Og").Period == 7 && ElementDatabase.Get("Og").Group == 18, "Og: 7. periyot, 18. grup");
        Check(ElementDatabase.Get("La").Group == 0 && ElementDatabase.Get("Hf").Group == 4, "f-blok konumları");

        Console.WriteLine("Tepkimeler");
        Check(B("H2O", HeatLevel.Cold).Success, "H2O soğukta oluşur");
        Check(B("NaCl", HeatLevel.Warm, 0.9f).Success, "NaCl karıştırılarak oluşur");
        Check(B("NaCl", HeatLevel.Warm, 0.1f).Fail.Id == "lumpy", "Karıştırılmayan tuz topaklanır");
        Check(B("H2O2", HeatLevel.Hot).Fail.Id == "peroxide_foam", "Isıtılan H2O2 = fil diş macunu");
        Check(B("CaO", HeatLevel.Warm).Fail.Id == "lukewarm_sludge", "CaO için ısı yetersiz");
        Check(B("H2O", HeatLevel.Inferno).Fail.Id == "roof_blown", "Su cehennemde çatıyı uçurur");
        Check(B("NaH2O", HeatLevel.Cold).Fail.Id == "alkali_boom", "Sodyum + su patlar");
        Check(B("NeAr", HeatLevel.Hot).Fail.Id == "noble_snub", "Soy gazlar tepkimeye girmez");
        Check(B("NH2", HeatLevel.Hot).Fail.Id == "near_miss", "NH2 -> neredeyse NH3");
        Check(B("AuO", HeatLevel.Hot).Fail.Id == "midas", "Altın -> Midas");
        Check(B("H2O", HeatLevel.Cold, 1f, 2).Fail.Id == "overflow", "Kapasite aşımı");
        var patient = ReactionEngine.Brew(new BrewInput { Atoms = A("CaO"), Heat = HeatLevel.Hot, CauldronCapacity = 6, PatienceSpell = true });
        Check(patient.Success, "Sabır büyüsü 1 kademe tolerans verir");

        Console.WriteLine("Oyun akışı");
        var s = new GameSession(PlayerState.NewGame(), 42);
        Check(s.ActiveQuest != null && s.ActiveQuest.Id == "q_thirst", "İlk görev: su");
        s.State.AddElement("H", 2); s.State.AddElement("O", 1);
        var rep = s.Brew(A("H2O"), HeatLevel.Cold, 0f);
        Check(rep.Result.Success && rep.NewDiscovery && s.State.PotionCount("water") == 1, "Su pişirildi ve keşfedildi");
        Check(s.State.ElementCount("H") == 0, "Malzemeler harcandı");
        var d = s.Deliver("water");
        Check(d.Outcome.Success && s.State.CompletedQuests.Contains("q_thirst"), "Görev tamamlandı");
        Check(s.ActiveQuest.Id != "q_thirst", "Yeni görev geldi");

        // Yanlış iksir -> kabak istilası
        var bugs = VillageDatabase.GetQuest("q_bugs");
        var o = QuestResolver.Deliver(bugs, CompoundDatabase.Get("ammonia"), new Random(1));
        Check(!o.Success && o.Fail.Id == "giant_pumpkins" && o.NewHouseState == HouseState.GiantPumpkins, "Gübre böcek görevinde kabak istilası yapar");

        Console.WriteLine("Ayrıştırma ve yetenekler");
        var got = Extractor.Extract(RawMaterialDatabase.Get("seawater"), 1f, 0, false, new Random(3));
        Check(got.ContainsKey("Na") && got.ContainsKey("Cl"), "Deniz suyundan Na ve Cl");
        var st = PlayerState.NewGame();
        Check(st.CauldronCapacity == 6, "Başlangıç kapasitesi 6");
        st.AddXp(500);
        Check(st.Level > 1 && st.SkillPoints == st.Level - 1, "XP ile seviye ve puan");
        Check(st.Unlock(SkillDatabase.PreciseScale) && st.CauldronCapacity == 9, "Hassas Terazi -> 9");
        Check(st.CanUnlock(SkillDatabase.Column) != null || st.HasSkill(SkillDatabase.Thermometer), "Damıtma Kolonu önce Termometre ister");

        Console.WriteLine(_fail == 0 ? "\nTÜM TESTLER GEÇTİ" : "\n" + _fail + " TEST BAŞARISIZ");
        return _fail == 0 ? 0 : 1;
    }
}
