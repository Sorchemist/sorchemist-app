// Oyuncunun kaydedilebilir durumu. Unity'nin JsonUtility'si ile uyumlu olsun diye
// sözlük yerine List kullanır (JsonUtility Dictionary serileştiremez).
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sorchemist.Core
{
    [Serializable]
    public class ItemStack
    {
        public string Id;
        public int Count;
    }

    [Serializable]
    public class HouseEntry
    {
        public string VillagerId;
        public int State; // HouseState
    }

    [Serializable]
    public class NodeEntry
    {
        public string RawId;
        public long ReadyAtUnix; // Bu zamandan sonra toplanabilir
    }

    [Serializable]
    public class PlayerState
    {
        public int Version = 1;
        public int Gold = 10;
        public int Xp;
        public int Level = 1;
        public int SkillPoints;
        public int Day = 1;
        public string ActiveQuestId;
        public bool SoundOn = true;
        public int Language;            // 0 = Türkçe, 1 = English
        public bool HapticsOn = true;
        public long MysteryReadyAt;     // Gizemli kutunun açılabileceği zaman (unix sn). 0 = hemen
        public int MysteryOpened;       // Kaç kutu açıldı

        // ---- Bina / zaman tabanlı ilerleme ----
        public int Knowledge;           // Bilgi Puanı (pasif özellik açmak için)
        public float GoldAcc, KnowledgeAcc, XpAcc;   // Çevrimiçi gelir birikimi (küsuratlar)
        public string BrewingId;        // Kazanda demlenen bileşik (yoksa boş)
        public long BrewReadyAt;        // Demlemenin biteceği zaman
        public List<BuildingEntry> Buildings = new List<BuildingEntry>();
        public List<string> OwnedPassives = new List<string>();
        public List<string> EquippedPassives = new List<string>();
        public List<string> KnownElements = new List<string>();   // En az bir kez elde edilmiş elementler

        public List<ItemStack> Elements = new List<ItemStack>();
        public List<ItemStack> Raw = new List<ItemStack>();
        public List<ItemStack> Potions = new List<ItemStack>();
        public List<string> Discovered = new List<string>();
        public List<string> FailDex = new List<string>();
        public List<string> Skills = new List<string>();          // (eski sürüm, kullanılmıyor)
        public List<string> CompletedQuests = new List<string>();
        public List<HouseEntry> Houses = new List<HouseEntry>();
        public List<NodeEntry> Nodes = new List<NodeEntry>();

        public static PlayerState NewGame()
        {
            var s = new PlayerState();
            // Haritada toplama noktası yok; başlangıç malzemeleri biraz daha cömert
            s.Gold = 30;
            s.AddRaw("seawater", 3);
            s.AddRaw("dewcap", 3);
            s.AddRaw("air", 2);
            s.AddRaw("ash", 2);
            s.AddRaw("limestone", 1);
            return s;
        }

        // ---------- Envanter yardımcıları ----------
        public static int Get(List<ItemStack> list, string id)
        {
            var s = list.Find(x => x.Id == id);
            return s != null ? s.Count : 0;
        }

        public static void Add(List<ItemStack> list, string id, int n)
        {
            var s = list.Find(x => x.Id == id);
            if (s == null) { s = new ItemStack { Id = id, Count = 0 }; list.Add(s); }
            s.Count += n;
            if (s.Count <= 0) list.Remove(s);
        }

        public void AddElement(string sym, int n) { Add(Elements, sym, n); }
        public void AddRaw(string id, int n) { Add(Raw, id, n); }
        public void AddPotion(string id, int n) { Add(Potions, id, n); }
        public int ElementCount(string sym) { return Get(Elements, sym); }
        public int RawCount(string id) { return Get(Raw, id); }
        public int PotionCount(string id) { return Get(Potions, id); }

        public bool HasElements(IDictionary<string, int> need)
        {
            return need.All(kv => ElementCount(kv.Key) >= kv.Value);
        }

        public bool ConsumeElements(IDictionary<string, int> need)
        {
            if (!HasElements(need)) return false;
            foreach (var kv in need) AddElement(kv.Key, -kv.Value);
            return true;
        }

        // ---------- İlerleme ----------
        public bool HasPassive(string id) { return EquippedPassives.Contains(id); }

        public BuildingEntry Building(string id)
        {
            var b = Buildings.Find(x => x.Id == id);
            if (b == null) { b = new BuildingEntry { Id = id, Level = 1 }; Buildings.Add(b); }
            return b;
        }

        public int BuildingLevel(string id) { return Building(id).Level; }
        public int TowerLevel { get { return BuildingLevel(BuildingDatabase.Tower); } }

        public int CauldronCapacity
        {
            get { return BuildingDatabase.CauldronCapacity(TowerLevel) + (HasPassive(PassiveDatabase.CopperPot) ? 2 : 0); }
        }

        public int PassiveSlots { get { return BuildingDatabase.PassiveSlots(TowerLevel); } }
        public int Builders { get { return BuildingDatabase.Builders(TowerLevel); } }

        public bool IsUnlocked(string compoundId)
        {
            var u = BuildingDatabase.UnlockFor(compoundId);
            return u == null || BuildingLevel(u.BuildingId) >= u.Level;
        }

        public static int XpToNext(int level) { return 40 + level * 30; }

        /// <summary>XP ekler, seviye atladıysa kaç seviye atladığını döndürür.</summary>
        public int AddXp(int amount)
        {
            int ups = 0;
            Xp += Math.Max(0, amount);
            while (Xp >= XpToNext(Level))
            {
                Xp -= XpToNext(Level);
                Level++;
                ups++;
            }
            return ups;
        }

        public HouseState GetHouse(string villagerId)
        {
            var h = Houses.Find(x => x.VillagerId == villagerId);
            return h != null ? (HouseState)h.State : HouseState.Normal;
        }

        public void SetHouse(string villagerId, HouseState st)
        {
            var h = Houses.Find(x => x.VillagerId == villagerId);
            if (h == null) { h = new HouseEntry { VillagerId = villagerId }; Houses.Add(h); }
            h.State = (int)st;
        }

        public long NodeReadyAt(string rawId)
        {
            var n = Nodes.Find(x => x.RawId == rawId);
            return n != null ? n.ReadyAtUnix : 0;
        }

        public void SetNodeReadyAt(string rawId, long t)
        {
            var n = Nodes.Find(x => x.RawId == rawId);
            if (n == null) { n = new NodeEntry { RawId = rawId }; Nodes.Add(n); }
            n.ReadyAtUnix = t;
        }
    }
}
