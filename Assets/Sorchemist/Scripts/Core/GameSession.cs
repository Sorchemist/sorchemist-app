// Oyunun tüm kurallarını tek yerde toplayan sınıf. Unity arayüzü sadece bunu çağırır.
using System;
using System.Collections.Generic;

namespace Sorchemist.Core
{
    public class BrewReport
    {
        public BrewResult Result;
        public bool NewDiscovery;   // İlk kez keşfedilen bileşik
        public bool NewFail;        // Fail-Dex'e yeni giriş
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

        public GameSession(PlayerState state, int seed = 0)
        {
            State = state ?? PlayerState.NewGame();
            _rng = seed == 0 ? new Random() : new Random(seed);
            if (string.IsNullOrEmpty(State.ActiveQuestId)) PickNextQuest();
        }

        private void Notify() { if (Changed != null) Changed(); }

        public QuestData ActiveQuest { get { return VillageDatabase.GetQuest(State.ActiveQuestId); } }

        public void PickNextQuest()
        {
            var q = QuestResolver.NextQuest(State.Level, State.CompletedQuests, _rng);
            State.ActiveQuestId = q != null ? q.Id : null;
        }

        // ---------- Harita: toplama ----------
        public bool CanCollect(string rawId, long nowUnix) { return nowUnix >= State.NodeReadyAt(rawId); }

        public bool Collect(string rawId, long nowUnix)
        {
            var raw = RawMaterialDatabase.Get(rawId);
            if (raw == null || !CanCollect(rawId, nowUnix)) return false;
            State.AddRaw(rawId, 1);
            State.SetNodeReadyAt(rawId, nowUnix + (long)raw.RegrowSeconds);
            Notify();
            return true;
        }

        // ---------- Laboratuvar: ayrıştırma ----------
        public Dictionary<string, int> Extract(string rawId, float quality)
        {
            var raw = RawMaterialDatabase.Get(rawId);
            if (raw == null || State.RawCount(rawId) <= 0) return null;
            State.AddRaw(rawId, -1);
            int bonus = State.HasSkill(SkillDatabase.Column) ? 1 : 0;
            var got = Extractor.Extract(raw, quality, bonus, State.HasSkill(SkillDatabase.Luck), _rng);
            foreach (var kv in got) State.AddElement(kv.Key, kv.Value);
            State.AddXp(2);
            Notify();
            return got;
        }

        // ---------- Laboratuvar: iksir yapımı ----------
        public BrewReport Brew(Dictionary<string, int> atoms, HeatLevel heat, float stirQuality)
        {
            var report = new BrewReport();
            if (!State.HasElements(atoms))
            {
                report.Result = new BrewResult { MessageTr = "Bu kadar elementin yok!" };
                return report;
            }

            var input = new BrewInput
            {
                Atoms = new Dictionary<string, int>(atoms),
                Heat = heat,
                StirQuality = stirQuality,
                CauldronCapacity = State.CauldronCapacity,
                PatienceSpell = State.HasSkill(SkillDatabase.Patience)
            };
            var r = ReactionEngine.Brew(input);
            report.Result = r;

            bool empty = true;
            foreach (var kv in atoms) if (kv.Value > 0) empty = false;
            if (empty) return report;

            State.ConsumeElements(atoms); // Başarısızlıkta da malzemeler harcanır

            if (r.Success)
            {
                State.AddPotion(r.Compound.Id, 1);
                if (!State.Discovered.Contains(r.Compound.Id))
                {
                    State.Discovered.Add(r.Compound.Id);
                    report.NewDiscovery = true;
                    r.Xp *= 2; // İlk keşif bonusu
                }
            }
            else if (r.Fail != null && !State.FailDex.Contains(r.Fail.Id))
            {
                State.FailDex.Add(r.Fail.Id);
                report.NewFail = true;
                r.Xp *= 2;
            }
            report.LevelUps = State.AddXp(r.Xp);
            Notify();
            return report;
        }

        // ---------- Köy: iksiri teslim et ----------
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

        // ---------- Gizemli kutu: her 6 saatte bir köye gelir ----------
        public const long MysteryInterval = 6 * 3600;

        public bool MysteryReady(long nowUnix) { return nowUnix >= State.MysteryReadyAt; }
        public long MysterySecondsLeft(long nowUnix) { return Math.Max(0, State.MysteryReadyAt - nowUnix); }

        static readonly string[] MysteryPool = { "Li", "B", "Ne", "Al", "Ar", "Ti", "Cr", "Co", "Ni", "Zn", "Ag", "Sn", "I", "W", "Pt", "Au", "Hg", "Xe" };

        public MysteryReward OpenMystery(long nowUnix)
        {
            if (!MysteryReady(nowUnix)) return null;
            var r = new MysteryReward();
            bool luck = State.HasSkill(SkillDatabase.Luck);
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
            foreach (var kv in r.Elements) State.AddElement(kv.Key, kv.Value);
            State.AddRaw(r.RawId, 1);
            State.MysteryReadyAt = nowUnix + MysteryInterval;
            State.MysteryOpened++;
            r.LevelUps = State.AddXp(r.Xp);
            Notify();
            return r;
        }

        public bool UnlockSkill(string id)
        {
            bool ok = State.Unlock(id);
            if (ok) Notify();
            return ok;
        }

        // Laboratuvarın dükkânı: altınla ham madde al (döngü kilitlenmesin diye)
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
