// Oyunun tüm kurallarını tek yerde toplayan sınıf. Unity arayüzü sadece bunu çağırır.
//
// Oyun kuramı:
//  - Puan (altın, Bilgi Puanı, XP) yalnızca zamanla ve OYUN AÇIKKEN kazanılır (TickOnline) +
//    elementler ve bileşikler hakkında bilgi biriktikçe (ilk kez elde edilen element / keşfedilen bileşik).
//  - Bina yükseltmeleri ve iksir demleme gerçek zamanlıdır (oyun kapalıyken de sürer).
//  - Binaların seviyesi yükseldikçe yeni deneyler (bileşikler) açılır, köy gelişir.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sorchemist.Core
{
    public class BrewReport
    {
        public BrewResult Result;
        public bool Started;        // Demleme başladı (doğru ve açık tarif)
        public bool Locked;         // Tarif doğru ama deney henüz açılmamış
        public bool NewDiscovery;   // İlk kez keşfedilen bileşik
        public bool NewFail;        // Fail-Dex'e yeni giriş
        public int Knowledge;       // Kazanılan Bilgi Puanı
        public int LevelUps;
    }

    public class DeliveryReport
    {
        public QuestOutcome Outcome;
        public bool NewFail;
        public int LevelUps;
    }

    public class MysteryReward
    {
        public int Gold;
        public int Xp;
        public int Knowledge;
        public Dictionary<string, int> Elements = new Dictionary<string, int>();
        public string RawId;
        public bool Rare;
        public int LevelUps;
    }

    public class GameSession
    {
        public PlayerState State { get; private set; }
        private readonly Random _rng;

        public event Action Changed;

        // Bilgi Puanı ödülleri
        public const int KnowledgeNewElement = 5;
        public const int KnowledgeNewCompound = 20;
        public const int KnowledgeNewFail = 5;

        public GameSession(PlayerState state, int seed = 0)
        {
            State = state ?? PlayerState.NewGame();
            _rng = seed == 0 ? new Random() : new Random(seed);
            if (string.IsNullOrEmpty(State.ActiveQuestId)) PickNextQuest();
            foreach (var id in BuildingDatabase.AllIds) State.Building(id);   // eksik binaları oluştur
            // Eski kayıtlardaki elementleri "bilinen" say
            foreach (var e in State.Elements) if (!State.KnownElements.Contains(e.Id)) State.KnownElements.Add(e.Id);
        }

        private void Notify() { if (Changed != null) Changed(); }

        public QuestData ActiveQuest { get { return VillageDatabase.GetQuest(State.ActiveQuestId); } }

        public void PickNextQuest()
        {
            var q = QuestResolver.NextQuest(State.Level, State.CompletedQuests, _rng);
            State.ActiveQuestId = q != null ? q.Id : null;
        }

        // =====================================================================
        // Çevrimiçi gelir
        // =====================================================================
        public float GoldPerMinute
        {
            get
            {
                float g = 0;
                foreach (var b in State.Buildings) g += BuildingDatabase.GoldPerMinute(b.Id, b.Level);
                return g * (State.HasPassive(PassiveDatabase.GoldPurse) ? 1.25f : 1f);
            }
        }

        /// <summary>Bilgi birikimi: bilinen element ve keşfedilen bileşik sayısıyla büyür.</summary>
        public float KnowledgePerMinute
        {
            get
            {
                float k = 1f + State.KnownElements.Count * 0.1f + State.Discovered.Count * 0.5f + State.TowerLevel * 0.25f;
                return k * (State.HasPassive(PassiveDatabase.Owl) ? 1.25f : 1f);
            }
        }

        public float XpPerMinute { get { return 1f + State.Discovered.Count * 0.2f; } }

        /// <summary>Oyun açıkken her karede/saniyede çağrılır. Seviye atlama sayısını döndürür.</summary>
        public int TickOnline(float seconds)
        {
            if (seconds <= 0) return 0;
            seconds = Math.Min(seconds, 5f);   // takılma/arka plan dönüşünde büyük sıçramaları engelle
            float m = seconds / 60f;
            State.GoldAcc += GoldPerMinute * m;
            State.KnowledgeAcc += KnowledgePerMinute * m;
            State.XpAcc += XpPerMinute * m;
            int g = (int)State.GoldAcc, k = (int)State.KnowledgeAcc, x = (int)State.XpAcc;
            State.GoldAcc -= g; State.KnowledgeAcc -= k; State.XpAcc -= x;
            State.Gold += g; State.Knowledge += k;
            int ups = x > 0 ? State.AddXp(x) : 0;
            if (g > 0 || k > 0 || x > 0) Notify();
            return ups;
        }

        // =====================================================================
        // Binalar
        // =====================================================================
        public int ActiveUpgrades { get { return State.Buildings.Count(b => b.UpgradeEndsAt > 0); } }

        public long UpgradeDuration(string id)
        {
            long s = BuildingDatabase.UpgradeSeconds(State.BuildingLevel(id));
            return State.HasPassive(PassiveDatabase.Builder) ? (long)(s * 0.85) : s;
        }

        /// <summary>Yükseltme başlatılamıyorsa nedenini, başlatılabiliyorsa null döndürür.</summary>
        public string CanUpgrade(string id)
        {
            var b = State.Building(id);
            if (b.Level >= BuildingDatabase.MaxLevel) return Loc.T("En yüksek seviyede", "Already at max level");
            if (b.UpgradeEndsAt > 0) return Loc.T("Zaten yükseltiliyor", "Already upgrading");
            if (ActiveUpgrades >= State.Builders) return Loc.T("Bütün ustalar meşgul (" + State.Builders + " usta)", "All builders are busy (" + State.Builders + " builders)");
            if (id != BuildingDatabase.Tower && b.Level >= State.TowerLevel + 2)
                return Loc.T("Önce Aqualf'in Kulesi'ni Sv." + (b.Level - 1) + " yap", "First raise Aqualf's Tower to Lv." + (b.Level - 1));
            int cost = BuildingDatabase.UpgradeCost(id, b.Level);
            if (State.Gold < cost) return Loc.T("Yetersiz altın (" + cost + " gerekli)", "Not enough gold (" + cost + " needed)");
            return null;
        }

        public bool StartUpgrade(string id, long nowUnix)
        {
            if (CanUpgrade(id) != null) return false;
            var b = State.Building(id);
            State.Gold -= BuildingDatabase.UpgradeCost(id, b.Level);
            b.UpgradeEndsAt = nowUnix + UpgradeDuration(id);
            Notify();
            return true;
        }

        /// <summary>Süresi dolan yükseltmeleri tamamlar ve tamamlanan binaların listesini döndürür.</summary>
        public List<string> CompleteUpgrades(long nowUnix)
        {
            var done = new List<string>();
            foreach (var b in State.Buildings)
            {
                if (b.UpgradeEndsAt > 0 && nowUnix >= b.UpgradeEndsAt)
                {
                    b.Level = Math.Min(BuildingDatabase.MaxLevel, b.Level + 1);
                    b.UpgradeEndsAt = 0;
                    State.AddXp(10 * b.Level);
                    State.Knowledge += 2 * b.Level;
                    done.Add(b.Id);
                }
            }
            if (done.Count > 0) Notify();
            return done;
        }

        public long UpgradeSecondsLeft(string id, long nowUnix)
        {
            var b = State.Building(id);
            return b.UpgradeEndsAt > 0 ? Math.Max(0, b.UpgradeEndsAt - nowUnix) : 0;
        }

        // =====================================================================
        // Laboratuvar: ayrıştırma (anında, tıklama yok)
        // =====================================================================
        public const float ExtractQuality = 0.75f;

        public Dictionary<string, int> Extract(string rawId)
        {
            var raw = RawMaterialDatabase.Get(rawId);
            if (raw == null || State.RawCount(rawId) <= 0) return null;
            State.AddRaw(rawId, -1);
            int bonus = State.HasPassive(PassiveDatabase.Column) ? 1 : 0;
            var got = Extractor.Extract(raw, ExtractQuality, bonus, State.HasPassive(PassiveDatabase.Luck), _rng);
            foreach (var kv in got) GainElement(kv.Key, kv.Value);
            State.AddXp(2);
            Notify();
            return got;
        }

        /// <summary>Element ekler; ilk kez elde ediliyorsa Bilgi Puanı verir.</summary>
        public int GainElement(string sym, int n)
        {
            State.AddElement(sym, n);
            if (State.KnownElements.Contains(sym)) return 0;
            State.KnownElements.Add(sym);
            State.Knowledge += KnowledgeNewElement;
            return KnowledgeNewElement;
        }

        // =====================================================================
        // Laboratuvar: iksir yapımı (zamanla demlenir, tıklama yok)
        // =====================================================================
        public bool IsBrewing { get { return !string.IsNullOrEmpty(State.BrewingId); } }
        public bool BrewDone(long nowUnix) { return IsBrewing && nowUnix >= State.BrewReadyAt; }
        public long BrewSecondsLeft(long nowUnix) { return IsBrewing ? Math.Max(0, State.BrewReadyAt - nowUnix) : 0; }

        public long BrewDuration(CompoundData c)
        {
            long s = 8L * c.AtomCount;   // atom başına 8 saniye
            return State.HasPassive(PassiveDatabase.PatientPot) ? (long)(s * 0.7) : s;
        }

        /// <summary>Kazana konan atomları dener. Doğru ve açık tarif: demleme başlar.
        /// Yanlış karışım: anında Fail-Dex sonucu (malzemeler harcanır). Kilitli tarif: hiçbir şey harcanmaz.</summary>
        public BrewReport StartBrew(Dictionary<string, int> atoms, long nowUnix)
        {
            var report = new BrewReport();
            if (IsBrewing) { report.Result = new BrewResult { MessageTr = Loc.T("Kazan zaten dolu! Önce demlenen iksiri topla.", "The cauldron is busy! Collect the brewing potion first.") }; return report; }
            if (!State.HasElements(atoms)) { report.Result = new BrewResult { MessageTr = Loc.T("Bu kadar elementin yok!", "You don't have that many elements!") }; return report; }
            if (atoms.Values.Sum() == 0) { report.Result = new BrewResult { MessageTr = Loc.T("Kazan boş.", "The cauldron is empty.") }; return report; }

            var known = CompoundDatabase.FindByAtoms(atoms);
            if (known != null && atoms.Values.Sum() <= State.CauldronCapacity)
            {
                if (!State.IsUnlocked(known.Id))
                {
                    var u = BuildingDatabase.UnlockFor(known.Id);
                    report.Locked = true;
                    report.Result = new BrewResult
                    {
                        MessageTr = Loc.T("Bu deneyin kilidi kapalı: " + BuildingDatabase.NameTr(u.BuildingId) + " Sv." + u.Level + " olmalı.", "This experiment is locked: " + BuildingDatabase.NameTr(u.BuildingId) + " must be Lv." + u.Level + ".")
                    };
                    return report;
                }
                State.ConsumeElements(atoms);
                State.BrewingId = known.Id;
                State.BrewReadyAt = nowUnix + BrewDuration(known);
                report.Started = true;
                report.Result = new BrewResult { Success = true, Compound = known, MessageTr = known.NameTr + Loc.T(" demleniyor...", " is brewing...") };
                Notify();
                return report;
            }

            // Bilinmeyen / yanlış oranlı karışım: komik başarısızlık
            var r = ReactionEngine.Brew(new BrewInput
            {
                Atoms = new Dictionary<string, int>(atoms), Heat = HeatLevel.Warm, StirQuality = 1f,
                CauldronCapacity = State.CauldronCapacity
            });
            report.Result = r;
            State.ConsumeElements(atoms);
            if (r.Fail != null && !State.FailDex.Contains(r.Fail.Id))
            {
                State.FailDex.Add(r.Fail.Id);
                report.NewFail = true;
                State.Knowledge += KnowledgeNewFail;
                report.Knowledge = KnowledgeNewFail;
                r.Xp *= 2;
            }
            report.LevelUps = State.AddXp(r.Xp);
            Notify();
            return report;
        }

        /// <summary>Demlemesi biten iksiri alır.</summary>
        public BrewReport CollectBrew(long nowUnix)
        {
            if (!BrewDone(nowUnix)) return null;
            var c = CompoundDatabase.Get(State.BrewingId);
            State.BrewingId = null; State.BrewReadyAt = 0;
            var report = new BrewReport
            {
                Result = new BrewResult { Success = true, Compound = c, Xp = c.Xp, MessageTr = c.NameTr + " (" + c.Formula + ")" + Loc.T(" hazır! ", " is ready! ") + c.FlavorTr }
            };
            State.AddPotion(c.Id, 1);
            if (!State.Discovered.Contains(c.Id))
            {
                State.Discovered.Add(c.Id);
                report.NewDiscovery = true;
                report.Result.Xp *= 2;
                State.Knowledge += KnowledgeNewCompound;
                report.Knowledge = KnowledgeNewCompound;
            }
            report.LevelUps = State.AddXp(report.Result.Xp);
            Notify();
            return report;
        }

        // =====================================================================
        // Köy: iksiri teslim et
        // =====================================================================
        public DeliveryReport Deliver(string potionId)
        {
            var q = ActiveQuest;
            if (q == null || State.PotionCount(potionId) <= 0) return null;
            var potion = CompoundDatabase.Get(potionId);
            State.AddPotion(potionId, -1);

            var o = QuestResolver.Deliver(q, potion, _rng);
            var rep = new DeliveryReport { Outcome = o };
            State.SetHouse(q.VillagerId, o.NewHouseState);
            State.Gold += o.Gold;
            foreach (var raw in o.RawRewards) State.AddRaw(raw, 1);

            if (o.Fail != null && !State.FailDex.Contains(o.Fail.Id))
            {
                State.FailDex.Add(o.Fail.Id);
                State.Knowledge += KnowledgeNewFail;
                rep.NewFail = true;
            }
            if (o.Success)
            {
                if (!State.CompletedQuests.Contains(q.Id)) State.CompletedQuests.Add(q.Id);
                State.Day++;
            }
            rep.LevelUps = State.AddXp(o.Xp);
            if (o.Success) PickNextQuest();
            Notify();
            return rep;
        }

        // =====================================================================
        // Gizemli kutu
        // =====================================================================
        public long MysteryInterval { get { return (State.HasPassive(PassiveDatabase.TimeSand) ? 5 : 6) * 3600L; } }

        public bool MysteryReady(long nowUnix) { return nowUnix >= State.MysteryReadyAt; }
        public long MysterySecondsLeft(long nowUnix) { return Math.Max(0, State.MysteryReadyAt - nowUnix); }

        static readonly string[] MysteryPool = { "Li", "B", "Ne", "Al", "Ar", "Ti", "Cr", "Co", "Ni", "Zn", "Ag", "Sn", "I", "W", "Pt", "Au", "Hg", "Xe" };

        public MysteryReward OpenMystery(long nowUnix)
        {
            if (!MysteryReady(nowUnix)) return null;
            var r = new MysteryReward();
            bool luck = State.HasPassive(PassiveDatabase.Luck);
            r.Gold = 20 + _rng.Next(41);
            r.Xp = 15;
            int picks = 1 + (_rng.NextDouble() < (luck ? 0.6 : 0.35) ? 1 : 0);
            for (int i = 0; i < picks; i++)
            {
                string sym = MysteryPool[_rng.Next(MysteryPool.Length)];
                int cur; r.Elements.TryGetValue(sym, out cur);
                r.Elements[sym] = cur + 1;
                if (Array.IndexOf(RawMaterialDatabase.MysteryRare, sym) >= 0) r.Rare = true;
            }
            var raws = RawMaterialDatabase.All;
            r.RawId = raws[_rng.Next(raws.Count)].Id;

            State.Gold += r.Gold;
            foreach (var kv in r.Elements) r.Knowledge += GainElement(kv.Key, kv.Value);
            State.AddRaw(r.RawId, 1);
            State.MysteryReadyAt = nowUnix + MysteryInterval;
            State.MysteryOpened++;
            r.LevelUps = State.AddXp(r.Xp);
            Notify();
            return r;
        }

        // =====================================================================
        // Pasif özellikler
        // =====================================================================
        public string CanBuyPassive(string id)
        {
            var p = PassiveDatabase.Get(id);
            if (p == null) return Loc.T("Bilinmeyen özellik", "Unknown passive");
            if (State.OwnedPassives.Contains(id)) return Loc.T("Zaten sende", "You already have it");
            if (State.Knowledge < p.Cost) return Loc.T("Yetersiz Bilgi Puanı (" + p.Cost + " gerekli)", "Not enough Knowledge (" + p.Cost + " needed)");
            return null;
        }

        public bool BuyPassive(string id)
        {
            if (CanBuyPassive(id) != null) return false;
            State.Knowledge -= PassiveDatabase.Get(id).Cost;
            State.OwnedPassives.Add(id);
            if (State.EquippedPassives.Count < State.PassiveSlots) State.EquippedPassives.Add(id);
            Notify();
            return true;
        }

        public string Equip(string id)
        {
            if (!State.OwnedPassives.Contains(id)) return Loc.T("Önce bu özelliği aç", "Unlock this passive first");
            if (State.EquippedPassives.Contains(id)) return null;
            if (State.EquippedPassives.Count >= State.PassiveSlots)
                return Loc.T("Bütün yuvalar dolu. Kuleyi yükselterek yeni yuva aç.", "All slots are full. Upgrade the Tower to open a new slot.");
            State.EquippedPassives.Add(id);
            Notify();
            return null;
        }

        public void Unequip(string id)
        {
            if (State.EquippedPassives.Remove(id)) Notify();
        }

        // Laboratuvarın dükkânı: altınla ham madde al
        public const int RawPrice = 8;
        public bool BuyRaw(string rawId)
        {
            if (State.Gold < RawPrice || RawMaterialDatabase.Get(rawId) == null) return false;
            State.Gold -= RawPrice;
            State.AddRaw(rawId, 1);
            Notify();
            return true;
        }
    }
}
