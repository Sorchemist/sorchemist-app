// Köylüler, RPG tarzı diyaloglu görevler ve yanlış iksirin köydeki sonuçları.
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sorchemist.Core
{
    // Haritada evin görünümü (Yaşayan Harita)
    public enum HouseState { Normal, Happy, GiantPumpkins, Frog, Floating, Frozen, Foam, Glowing, Pink, Fireworks }

    [Serializable]
    public class VillagerData
    {
        public string Id;
        public string NameTr;
        public string RoleTr;
        public int GridX, GridY;       // İzometrik haritadaki ev konumu
        public string ColorHex;        // Yer tutucu görsel rengi
        public string[] AmbientTr;     // Görevi olmayan köylülerin sohbet cümleleri
    }

    [Serializable]
    public class QuestData
    {
        public string Id;
        public string VillagerId;
        public int MinLevel;
        public string[] DialogueTr;            // Köylünün derdini anlattığı satırlar
        public PotionEffect Needed;
        public string SuccessTr;
        public int RewardGold;
        public int RewardXp;
        public string[] RewardRaw;             // Ödül olarak verilen ham madde id'leri
        // Yanlış etki -> Fail-Dex girdisi (özel komik sonuçlar)
        public Dictionary<PotionEffect, string> Mishaps = new Dictionary<PotionEffect, string>();
    }

    public class QuestOutcome
    {
        public bool Success;
        public string MessageTr;
        public FailEntry Fail;          // Yanlış iksir köyde komik bir şeye yol açtıysa
        public HouseState NewHouseState;
        public int Gold;
        public int Xp;
        public string[] RawRewards = new string[0];
    }

    public static class VillageDatabase
    {
        private static List<VillagerData> _villagers;
        private static List<QuestData> _quests;

        public static IReadOnlyList<VillagerData> Villagers { get { Ensure(); return _villagers; } }
        public static IReadOnlyList<QuestData> Quests { get { Ensure(); return _quests; } }
        public static VillagerData GetVillager(string id) { Ensure(); return _villagers.Find(v => v.Id == id); }
        public static QuestData GetQuest(string id) { Ensure(); return _quests.Find(q => q.Id == id); }

        // Genel kural: belirli bir yanlış etki her köylüde aynı komik sonucu doğurur.
        public static readonly Dictionary<PotionEffect, string> GlobalMishaps = new Dictionary<PotionEffect, string>
        {
            { PotionEffect.Growth, "giant_pumpkins" },
            { PotionEffect.Fizz, "floating_villager" },
            { PotionEffect.Cool, "frozen_house" },
            { PotionEffect.Cleanse, "foam_flood" },
            { PotionEffect.Light, "glowing_house" },
            { PotionEffect.Pigment, "pink_village" },
            { PotionEffect.Fireworks, "fireworks_mishap" },
        };

        private static void V(string id, string name, string role, int x, int y, string color, params string[] ambient)
        {
            _villagers.Add(new VillagerData { Id = id, NameTr = name, RoleTr = role, GridX = x, GridY = y, ColorHex = color, AmbientTr = ambient });
        }

        private static QuestData Q(string id, string villager, int lvl, PotionEffect need, int gold, int xp,
            string success, string[] raws, params string[] lines)
        {
            var q = new QuestData
            {
                Id = id, VillagerId = villager, MinLevel = lvl, Needed = need, RewardGold = gold,
                RewardXp = xp, SuccessTr = success, RewardRaw = raws ?? new string[0], DialogueTr = lines
            };
            _quests.Add(q);
            return q;
        }

        private static void Ensure()
        {
            if (_villagers != null) return;
            _villagers = new List<VillagerData>();
            _quests = new List<QuestData>();

            // Görevli köylüler (7x7 izometrik harita konumlarıyla)
            V("fistik", "Fıstık Dede", "Çiftçi", 1, 4, "#A9744F");
            V("bal", "Bal Teyze", "Fırıncı", 2, 2, "#F5B041");
            V("pirtik", "Minik Pırtık", "Çoban", 1, 2, "#F1948A");
            V("tokmak", "Demirci Tokmak", "Demirci", 1, 1, "#5D6D7E");
            V("ebe", "Ebe Nine", "Şifacı", 2, 4, "#AF7AC5");
            V("fener", "Fener", "Av Koruyucusu", 4, 4, "#F4D03F");
            V("zeytin", "Kâhya Zeytin", "Hancı", 4, 2, "#7DCEA0");
            V("kopuk", "Çamaşırcı Köpük", "Çamaşırcı", 4, 3, "#85C1E9");
            V("filiz", "Filiz", "Ormancı", 3, 1, "#58D68D");
            V("ruzgar", "Değirmenci Rüzgâr", "Değirmenci", 2, 1, "#D7BDE2");
            // Diğer köy halkı (şimdilik sohbet; ileride görev verebilirler)
            V("oduncu", "Kütük", "Oduncu", 1, 0, "#8D6E63", "Bu meşeler yüz yaşında. Külleri bile bereketlidir, potasyum doludur!", "Baltam keskin, odunum kuru. Kış hazır!");
            V("marangoz", "Rende Usta", "Marangoz", 2, 0, "#A1887F", "Kazanın için yeni bir raf mı lazım? Ölç, biç, sonra kes!", "Talaş kokusu gibisi yok.");
            V("tasusta", "Çakıl", "Taş Ustası", 4, 0, "#90A4AE", "Kireç taşını kızdırırsan sönmemiş kireç olur, bilir misin?", "Her taşın içinde bir heykel saklıdır.");
            V("derici", "Tabak", "Derici", 4, 1, "#795548", "Deriyi yumuşatmak için tuz ve sabır gerekir.", "Kokuya alışırsın. Galiba.");
            V("komurcu", "İs", "Kömürcü", 0, 2, "#455A64", "Kömür saf karbondur! Ben de biraz karbon gibi görünüyorum.", "Ocakların hepsi benim kömürümle yanar.");
            V("kunduraci", "Çivi", "Kunduracı", 5, 2, "#6D4C41", "Ayakkabın delinmiş Aqualf. Damla gibi akıyorsun!", "Her adım bir hikâyedir.");
            V("asci", "Kepçe", "Aşçı", 1, 3, "#EF6C00", "Mutfak da bir laboratuvar! Tuz, karbonat, sirke... hepsi kimya.", "Çorbanın sırrı: azıcık tuz.");
            V("sucu", "Damla", "Sucu", 2, 3, "#29B6F6", "Kuyu suyu tertemiz. H2O, başka bir şey yok!", "Kovalarım hep dolu, gönlüm de.");
            V("terzi", "Makas", "Terzi", 5, 3, "#AB47BC", "Kumaşları göztaşıyla mavi, pasla kırmızı boyarım.", "Ölçü al, ölçü al, sonra bir daha ölçü al.");
            V("dokumaci", "İlmek", "Dokumacı", 0, 4, "#EC407A", "Tezgâhımda renk renk ipler... boyaları senden alıyorum!", "Bir ilmek daha, bir ilmek daha.");
            V("balikci", "Olta", "Balıkçı", 5, 4, "#0277BD", "Nehir bugün cömert. Deniz suyu da tuz doludur, haberin olsun.", "Sabır balıkçının en iyi yemidir.");
            V("comlekci", "Testi", "Çömlekçi", 1, 5, "#BF7B4C", "Kil ve kum, ateşte birleşince sağlam bir çömlek olur.", "Elime çamur bulaşmadan gün geçmez.");
            V("berber", "Fırça", "Berber", 2, 5, "#7E57C2", "Saçların hâlâ isli mi? Gel bir düzelteyim!", "Köyün bütün dedikoduları bu koltukta konuşulur.");
            V("arici", "Petek", "Arıcı", 4, 5, "#FBC02D", "Arılarım çiçekleri sever; sen de iksirleri.", "Bal, doğanın kendi simyasıdır.");

            Q("q_thirst", "filiz", 1, PotionEffect.Hydrate, 20, 15, "Fidelerim kana kana içti! Sana en güzel çiçeğimi ayırdım.",
                new[] { "dewcap" },
                "Aqualf! Kuyu kurudu, fidelerim boynunu büktü.",
                "Bana biraz saf su yapabilir misin? H'ler ve O'lar, sen bilirsin...");
            Q("q_pickles", "bal", 1, PotionEffect.Preserve, 25, 20, "Turşularım taş gibi! Fırından taze bir somun senin.",
                new[] { "ash" },
                "Kış için kurduğum turşular küflenmeye başladı, ah ah!",
                "Yiyecekleri koruyan o beyaz, tuzlu şeyden lazım. Denizden geldiğini söylerler.");
            Q("q_tummy", "pirtik", 2, PotionEffect.Soothe, 30, 25, "Karnım geçti! Yaşasın! Sana bir kurbağa yakaladım. Şaka şaka.",
                new[] { "seawater" },
                "Fıstık Dede'nin bütün ekşi eriklerini yedim... karnım yanıyor.",
                "Annem 'bir baz lazım, asidi nötrleştirsin' diyor. Ne demekse!");
            Q("q_bugs", "fistik", 2, PotionEffect.PestControl, 40, 35, "Böcekler kaçtı! Bu yıl bağdan bol üzüm çıkacak.",
                new[] { "malachite" },
                "Evlat, üzüm bağımı mantar ve böcek bastı.",
                "Dedemin dedesi mavi bir taşla ilaç yapardı. Ama sakın gübre getirme, böcekler bayram eder!");
            Q("q_burn", "tokmak", 3, PotionEffect.Cool, 45, 40, "Ohh... buz gibi. Sana yeni bir kazan ayağı döveceğim.",
                new[] { "hematite" },
                "Ocağın başında elimi yaktım. Zonkluyor.",
                "Isıyı emip serinleten bir şey yapabilir misin? Ama evimi dondurma sakın.");
            Q("q_wound", "ebe", 3, PotionEffect.Disinfect, 45, 40, "Yaralar tertemiz. Köy senin gibi bir simyacıya şükretsin.",
                new[] { "dewcap", "air" },
                "Odunculardan biri kolunu kesmiş. Yarayı temizlemem gerek.",
                "Fazladan oksijeni olan o köpüren suyu bilir misin? Soğuk tut ama, yoksa taşar!");
            Q("q_dark", "fener", 4, PotionEffect.Light, 50, 45, "Gece gündüz gibi aydınlık! Artık kurtlar yaklaşamaz.",
                new[] { "magnesite" },
                "Fenerimin camı kırıldı. Gece köyü karanlıkta bekliyorum.",
                "Göz kamaştıran beyaz bir ışık lazım. Beyaz yamaçlardaki taşlardan duymuştum.");
            Q("q_laundry", "kopuk", 4, PotionEffect.Cleanse, 40, 40, "Çarşaflar bembeyaz! Köpükler de tam kıvamında.",
                new[] { "seawater" },
                "Festival örtüleri isle kapkara oldu.",
                "Yağı çözen, sabun yapmaya yarayan güçlü bir baz lazım bana.");
            Q("q_mill", "ruzgar", 5, PotionEffect.Build, 60, 50, "Değirmen taş gibi sağlam. Sana bir çuval un!",
                new[] { "limestone", "sand" },
                "Değirmenin duvarı çatladı, her rüzgârda biraz daha sallanıyor.",
                "Taşları birbirine bağlayacak, harca katılan o beyaz taştan yapabilir misin?");
            Q("q_cold", "zeytin", 5, PotionEffect.Warmth, 55, 50, "Meydan sıcacık! Köy meclisi seni onur üyesi yaptı.",
                new[] { "limestone" },
                "Kış erken geldi, meydandaki kazanlar ısınmıyor.",
                "Suyla buluşunca ısı veren o kireçten yapabilir misin? Çok, çok yüksek ısı gerekir derler.");
            Q("q_festival", "zeytin", 6, PotionEffect.Fireworks, 80, 70, "Gökyüzü renk renk! Bu festival yıllarca konuşulacak.",
                new[] { "crystal" },
                "Hasat festivali yaklaşıyor ve havai fişeğimiz yok!",
                "Potasyum, azot, oksijen... Eski usul güherçile! Ama ambarın yakınında deneme, olur mu?");
            Q("q_paint", "pirtik", 6, PotionEffect.Pigment, 50, 45, "Ahırı kırmızıya boyadım! Tavuklara dokunmadım. Belki biraz.",
                new[] { "hematite" },
                "Ahırı boyamak istiyorum ama boya yok.",
                "Demir paslanınca kırmızı olurmuş, doğru mu?");
            Q("q_lamp", "fener", 7, PotionEffect.Fuel, 70, 60, "Lamba mavi alevle yanıyor, hiç is yok! Harika.",
                new[] { "dewcap", "ash" },
                "Lambalarımdaki yağ çok isli yanıyor.",
                "Temiz yanan, mavi alevli bir yakıt lazım. Karbon, hidrojen ve biraz oksijen diyorlar.");
            Q("q_window", "bal", 7, PotionEffect.Glass, 70, 60, "Vitrinim pırıl pırıl! Herkes çöreklerimi görebiliyor.",
                new[] { "sand" },
                "Dükkânımın camı kırıldı, rüzgâr çörekleri soğutuyor.",
                "Kumdan cam yapılır derler. Ama çok sıcak bir ateş gerekir!");
        }
    }

    public static class QuestResolver
    {
        public static QuestOutcome Deliver(QuestData q, CompoundData potion, Random rng)
        {
            var o = new QuestOutcome();
            var v = VillageDatabase.GetVillager(q.VillagerId);
            string name = v != null ? v.NameTr : "Köylü";

            if (potion != null && potion.Effect == q.Needed)
            {
                o.Success = true;
                o.MessageTr = name + ": \"" + q.SuccessTr + "\"";
                o.NewHouseState = HouseState.Happy;
                o.Gold = q.RewardGold;
                o.Xp = q.RewardXp;
                o.RawRewards = q.RewardRaw;
                return o;
            }

            PotionEffect eff = potion != null ? potion.Effect : PotionEffect.None;
            string failId;
            if (!q.Mishaps.TryGetValue(eff, out failId))
                VillageDatabase.GlobalMishaps.TryGetValue(eff, out failId);
            if (failId == null && rng.NextDouble() < 0.3) failId = "frog_villager";

            o.Success = false;
            o.Xp = 3;
            if (failId != null)
            {
                o.Fail = FailDexDatabase.Get(failId);
                o.NewHouseState = StateFor(failId);
                o.MessageTr = o.Fail.TitleTr + "! " + o.Fail.DescriptionTr;
                o.Xp += o.Fail.Xp;
            }
            else
            {
                o.NewHouseState = HouseState.Normal;
                o.MessageTr = name + ": \"Hmm... bu " + CompoundDatabase.EffectNameTr(eff).ToLowerInvariant() +
                              " iksiri gibi. Benim derdime pek iyi gelmedi.\"";
            }
            return o;
        }

        public static HouseState StateFor(string failId)
        {
            switch (failId)
            {
                case "giant_pumpkins": return HouseState.GiantPumpkins;
                case "frog_villager": return HouseState.Frog;
                case "floating_villager": return HouseState.Floating;
                case "frozen_house": return HouseState.Frozen;
                case "foam_flood": return HouseState.Foam;
                case "glowing_house": return HouseState.Glowing;
                case "pink_village": return HouseState.Pink;
                case "fireworks_mishap": return HouseState.Fireworks;
                default: return HouseState.Normal;
            }
        }

        /// <summary>Sıradaki görev: önce açılmamış hikâye görevleri, bitince tekrar eden istekler.</summary>
        public static QuestData NextQuest(int level, ICollection<string> completed, Random rng)
        {
            var story = VillageDatabase.Quests
                .Where(q => q.MinLevel <= level && !completed.Contains(q.Id))
                .OrderBy(q => q.MinLevel).FirstOrDefault();
            if (story != null) return story;
            var pool = VillageDatabase.Quests.Where(q => q.MinLevel <= level).ToList();
            return pool.Count == 0 ? null : pool[rng.Next(pool.Count)];
        }
    }
}
