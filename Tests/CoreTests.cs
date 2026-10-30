// Çekirdek oyun kuralları için basit testler. Çalıştırmak için: cd Tests && dotnet run
using System;
using System.Linq;
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
        long now = 1000000;
        var s = new GameSession(PlayerState.NewGame(), 42);
        Check(s.ActiveQuest != null && s.ActiveQuest.Id == "q_thirst", "İlk görev: su");
        s.State.AddElement("H", 2); s.State.AddElement("O", 1);
        var rep = s.StartBrew(A("H2O"), now);
        Check(rep.Started && s.IsBrewing && s.State.ElementCount("H") == 0, "Su demlenmeye başladı, malzemeler harcandı");
        Check(s.CollectBrew(now + 5) == null, "Süre dolmadan toplanamaz");
        var col = s.CollectBrew(now + s.BrewDuration(CompoundDatabase.Get("water")));
        Check(col != null && col.NewDiscovery && s.State.PotionCount("water") == 1, "Su toplandı ve keşfedildi");
        Check(s.State.Knowledge >= GameSession.KnowledgeNewCompound, "Keşif Bilgi Puanı verdi");
        var d = s.Deliver("water");
        Check(d.Outcome.Success && s.State.CompletedQuests.Contains("q_thirst"), "Görev tamamlandı");
        Check(s.ActiveQuest.Id != "q_thirst", "Yeni görev geldi");

        // Kilitli deney: kireç taşı (Cevher Madeni Sv.3)
        s.State.AddElement("Ca", 1); s.State.AddElement("C", 1); s.State.AddElement("O", 3);
        var lk = s.StartBrew(A("CaCO3"), now);
        Check(lk.Locked && !lk.Started && s.State.ElementCount("Ca") == 1, "Kilitli deney malzeme harcamaz");

        // Yanlış iksir -> kabak istilası
        var bugs = VillageDatabase.GetQuest("q_bugs");
        var o = QuestResolver.Deliver(bugs, CompoundDatabase.Get("ammonia"), new Random(1));
        Check(!o.Success && o.Fail.Id == "giant_pumpkins" && o.NewHouseState == HouseState.GiantPumpkins, "Gübre böcek görevinde kabak istilası yapar");

        Console.WriteLine("Binalar ve gelir");
        Check(BuildingDatabase.UpgradeSeconds(1) == 60 && BuildingDatabase.UpgradeSeconds(4) == 3600 && BuildingDatabase.UpgradeSeconds(11) == 24 * 86400, "Yükseltme süreleri 1 dk .. 24 gün");
        Check(BuildingDatabase.UpgradeSeconds(12) == 0, "Sv.12 son seviye");
        var g = new GameSession(PlayerState.NewGame(), 7);
        g.State.Gold = 1000;
        Check(g.StartUpgrade("watermill", now) && g.State.Building("watermill").UpgradeEndsAt == now + 60, "Su Değirmeni yükseltmesi 1 dk");
        Check(g.CanUpgrade("mine") != null, "Tek usta: ikinci yükseltme bekler");
        Check(g.CompleteUpgrades(now + 59).Count == 0 && g.CompleteUpgrades(now + 60).Count == 1 && g.State.BuildingLevel("watermill") == 2, "Süre dolunca Sv.2");
        Check(g.CanUpgrade("market") != null && !g.State.IsDistrictOpen("market") && g.State.IsDistrictOpen("forge"), "2. mahalle sisin altında, yükseltilemez");
        Check(!g.State.IsUnlocked("hcl"), "Sisteki binanın deneyi kapalı");
        int gold0 = g.State.Gold;
        for (int i = 0; i < 60; i++) g.TickOnline(1f);
        Check(g.State.Gold - gold0 <= 1, "Pasif altın yavaş: 1 dakikada en fazla 1 altın");
        for (int i = 0; i < 2400; i++) g.TickOnline(5f);
        Check(g.State.Gold > gold0, "Oyun açıkken altın birikir (3 saatte)");
        Check(g.State.CauldronCapacity == 6, "Başlangıç kapasitesi 6");

        Console.WriteLine("Ayrıştırma ve pasifler");
        var got = Extractor.Extract(RawMaterialDatabase.Get("seawater"), 1f, 0, false, new Random(3));
        Check(got.ContainsKey("Na") && got.ContainsKey("Cl"), "Deniz suyundan Na ve Cl");
        g.State.Knowledge = 100;
        Check(g.BuyPassive(PassiveDatabase.CopperPot) && g.State.CauldronCapacity == 8, "Bakır Kazan -> 8");
        Check(g.State.PassiveSlots == 1 && g.BuyPassive(PassiveDatabase.Owl) && !g.State.HasPassive(PassiveDatabase.Owl), "Tek yuva: ikinci pasif takılmaz");
        Check(g.Equip(PassiveDatabase.Owl) != null, "Yuva dolu uyarısı");
        g.Unequip(PassiveDatabase.CopperPot);
        Check(g.Equip(PassiveDatabase.Owl) == null && g.State.HasPassive(PassiveDatabase.Owl), "Çıkar-tak çalışır");

        Console.WriteLine("Köylüler ve görevler");
        Check(VillageDatabase.Villagers.Count == 23 && BuildingDatabase.AllIds.Count() == 24, "Simya Kulesi + 23 bina");
        bool namesOk = true;
        foreach (var v in VillageDatabase.Villagers) if (string.IsNullOrEmpty(v.NameTr) || string.IsNullOrEmpty(v.RoleTr) || string.IsNullOrEmpty(v.BuildingTr) || v.Group < 1 || v.Group > 4) namesOk = false;
        Check(namesOk && VillageDatabase.GetVillager("mine").NameTr == "Madenci" && BuildingDatabase.NameTr("forge") == "Demirci Ocağı", "Görevli adı, unvanı, binası ve mahallesi");
        var perGroup = new int[5];
        foreach (var v in VillageDatabase.Villagers) perGroup[v.Group]++;
        Check(perGroup[1] == 5 && perGroup[2] == 6 && perGroup[3] == 6 && perGroup[4] == 6, "Mahalleler: 5+kule, 6, 6, 6");
        Check(VillageDatabase.Quests.Count == QuestBank.TotalQuests + QuestBank.KeeperProblemCount, "Toplam 2000 + yeni görevlilerin görevleri");
        bool lvOk = true;
        foreach (var q in VillageDatabase.Quests) if (q.MinLevel < BuildingDatabase.UnlockLevelOf(q.VillagerId)) lvOk = false;
        Check(lvOk, "Hiçbir görev mahallesi açılmadan gelmez");
        var perKeeper = new Dictionary<string, int>();
        foreach (var q in VillageDatabase.Quests) { int c0; perKeeper.TryGetValue(q.VillagerId, out c0); perKeeper[q.VillagerId] = c0 + 1; }
        Check(VillageDatabase.Villagers.All(v => perKeeper.ContainsKey(v.Id) && perKeeper[v.Id] >= 8), "Her görevlinin en az 8 görevi var");
        Check(BuildingDatabase.MapLegacy("bal") == "hollow" && BuildingDatabase.MapLegacy("forge") == "forge", "Eski köy kimlikleri yeni binalara");
        var ids = new HashSet<string>();
        bool genOk = true;
        foreach (var q in VillageDatabase.Quests)
        {
            if (!ids.Add(q.Id) || VillageDatabase.GetVillager(q.VillagerId) == null) genOk = false;
            if (q.DialogueEn == null || q.SuccessEn == null || q.DialogueTr.Length != q.DialogueEn.Length) genOk = false;
        }
        Check(genOk, "Görev kimlikleri benzersiz, köylüler geçerli, her görevin İngilizcesi var");
        var texts = new HashSet<string>();
        foreach (var q in VillageDatabase.Quests) texts.Add(string.Join("|", q.DialogueTr));
        Check(texts.Count == VillageDatabase.Quests.Count, "Hiçbir görevin metni birebir tekrar etmiyor");
        var problems = new HashSet<string>();
        foreach (var q in VillageDatabase.Quests) problems.Add(q.DialogueTr[0]);
        Check(problems.Count == VillageDatabase.Quests.Count, "Her görevin kendine ait ayrı bir derdi var");
        bool solvable = true;
        foreach (var q in VillageDatabase.Quests)
        {
            bool any = false;
            foreach (var c in CompoundDatabase.All) if (c.Effect == q.Needed) any = true;
            if (!any || q.RewardGold <= 0 || q.RewardXp <= 0) solvable = false;
        }
        Check(solvable, "Her görev bir bileşikle çözülür ve altın + XP verir");
        var ice = QuestResolver.Deliver(VillageDatabase.GetQuest("q_ice_pier"), CompoundDatabase.Get("salt"), new Random(1));
        Check(ice.Success && ice.Gold == 40, "Tuz iskeledeki buzu eritir");
        var g2 = new GameSession(PlayerState.NewGame(), 3);
        g2.State.Level = 6;
        g2.PickNextQuest();
        Check(g2.CanMakeFor(g2.ActiveQuest), "Önce yapılabilir iksir isteyen görev gelir");

        Console.WriteLine("Çevrimiçi (JSON ve bulut kaydı)");
        var jn = JNode.From("{\"ok\":true,\"user\":{\"id\":42,\"name\":\"Ay\\u015fe\"},\"list\":[1,2.5,\"x\"],\"n\":null}");
        Check(jn["ok"].Bool && jn["user"]["id"].Int == 42 && jn["user"]["name"].Str == "Ayşe" && jn["list"].Count == 3 && jn["n"].IsNull, "JSON okunur");
        var ser = MiniJson.Serialize(new Dictionary<string, object> { { "a", "t\"ırnak" }, { "b", new List<object> { 1, true } } });
        Check(JNode.From(ser)["a"].Str == "t\"ırnak" && JNode.From(ser)["b"][1].Bool, "JSON yazılır ve geri okunur");
        var big = PlayerState.NewGame();
        for (int i = 0; i < 2000; i++) big.CompletedQuests.Add("g" + (i + 1).ToString("0000"));
        string bigJson = MiniJson.Serialize(big.CompletedQuests);
        string packed = CloudData.Pack(bigJson);
        Check(CloudData.Unpack(packed) == bigJson && packed.Length < bigJson.Length / 2, "Kayıt sıkıştırılır (" + bigJson.Length + " -> " + packed.Length + " bayt)");
        var vil = CloudData.Village(big, "Ali");
        Check((int)CloudData.Stats(big)["quests"] == 2000 && (string)vil["name"] == "Ali", "Sıralama istatistikleri ve köy vitrini");

        Console.WriteLine("Hile koruması");
        string sig = SaveSignature.Compute(bigJson, "cihaz1");
        Check(SaveSignature.Verify(bigJson, "cihaz1", sig), "Kayıt imzası doğrulanır");
        Check(!SaveSignature.Verify(bigJson.Replace("g0001", "g0002"), "cihaz1", sig) && !SaveSignature.Verify(bigJson, "cihaz2", sig), "Değiştirilen kayıt ya da başka cihaz imzası tutmaz");
        var ps = new PlayerState { LedgerId = 7, TamperCount = 1 };
        Check(ps.LedgerId == 7 && ps.TamperCount == 1 && PlayerState.NewGame().TamperCount == 0, "Defter numarası ve kurcalama sayacı kayıtta");

        Console.WriteLine("Kayıt taşıma, mevsimler");
        var old = PlayerState.NewGame();
        old.Version = 1;
        old.Buildings.Clear();
        old.Buildings.Add(new BuildingEntry { Id = "tower", Level = 3 });
        old.Buildings.Add(new BuildingEntry { Id = "bal", Level = 4 });
        old.Buildings.Add(new BuildingEntry { Id = "zeytin", Level = 2 });
        old.Buildings.Add(new BuildingEntry { Id = "sucu", Level = 5, UpgradeEndsAt = 99 });
        old.SetHouse("pirtik", HouseState.Happy);
        var ms = new GameSession(old, 5);
        Check(ms.State.Version == PlayerState.CurrentVersion && ms.State.BuildingLevel("hollow") == 4 && ms.State.Building("watermill").UpgradeEndsAt == 99
              && ms.State.GetHouse("stable") == HouseState.Happy && !ms.State.Buildings.Any(b => b.Id == "bal"), "Eski köy kaydı kasabaya taşındı");
        Check(SeasonClock.At(0) == Season.Spring && SeasonClock.At(6 * 3600) == Season.Autumn && SeasonClock.At(13 * 3600) == Season.Winter
              && SeasonClock.At(19 * 3600) == Season.Storm && SeasonClock.At(20 * 3600) == Season.Spring, "Mevsim döngüsü 6+6+6+2 saat");
        Check(SeasonClock.NightGlow(0f) == 1f && SeasonClock.NightGlow(0.5f) == 0f && SeasonClock.Clock(12 * 60 + 5) == "12:05", "Gün içi saat ve gece ışıkları");
        var ss = new GameSession(PlayerState.NewGame(), 9);
        ss.Clock = () => 6 * 3600 + 10;   // sonbahar
        long fuelNormal = 8L * CompoundDatabase.Get("ethanol").AtomCount;
        Check(ss.BrewDuration(CompoundDatabase.Get("ethanol")) == fuelNormal / 2, "Sonbaharda yakıt iksiri yarı sürede");
        Check(BuildingDatabase.SeasonGoldFactor("forge", Season.Autumn) > 1f && BuildingDatabase.SeasonGoldFactor("forge", Season.Storm) < 1f, "Mevsim bina üretimini etkiler");
        Check(BuildingDatabase.ChestTier(1) == 1 && BuildingDatabase.ChestTier(12) == 6, "Gizemli sandık kule seviyesiyle büyür");

        Console.WriteLine("Dil (İngilizce)");
        Loc.Current = Lang.En;
        Check(ElementDatabase.Get("Fe").NameTr == "Iron" && CompoundDatabase.Get("salt").NameTr == "Table Salt", "Element ve bileşik adları İngilizce");
        Check(VillageDatabase.GetVillager("hollow").NameTr == "Innkeeper" && VillageDatabase.GetVillager("forge").BuildingTr == "Blacksmith Forge" && VillageDatabase.GetQuest("q_thirst").DialogueTr[0].StartsWith("Aqualf!"), "Görevli ve görev metinleri İngilizce");
        Check(FailDexDatabase.Get("midas").TitleTr == "Midas Cauldron" && PassiveDatabase.Get(PassiveDatabase.Owl).NameTr == "Wise Owl", "Fail-Dex ve pasifler İngilizce");
        Check(AlchemyNames.Name(1) == "Pure Dew Essence" && BuildingDatabase.NameTr("tower") == "Alchemist Tower", "Simya adları ve binalar İngilizce");
        bool allEn = true;
        foreach (var v in VillageDatabase.Villagers) if (v.NameEn == null) allEn = false;
        foreach (var q in VillageDatabase.Quests) if (q.DialogueEn == null) allEn = false;
        foreach (var c in CompoundDatabase.All) if (c.NameEn == null) allEn = false;
        foreach (var f in FailDexDatabase.All) if (f.TitleEn == null) allEn = false;
        foreach (var r in RawMaterialDatabase.All) if (r.NameEn == null) allEn = false;
        Check(allEn && English.Alchemy.Length == 118, "Bütün kayıtların İngilizcesi var");
        Loc.Current = Lang.Tr;
        Check(ElementDatabase.Get("Fe").NameTr == "Demir", "Türkçeye geri dönüş");

        Console.WriteLine(_fail == 0 ? "\nTÜM TESTLER GEÇTİ" : "\n" + _fail + " TEST BAŞARISIZ");
        return _fail == 0 ? 0 : 1;
    }
}
