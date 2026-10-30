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
        public string NameTr { get { return Loc.Pick(_nameTr, NameEn); } set { _nameTr = value; } }
        string _nameTr; public string NameEn;
        public string RoleTr { get { return Loc.Pick(_roleTr, RoleEn); } set { _roleTr = value; } }
        string _roleTr; public string RoleEn;
        public int GridX, GridY;       // İzometrik haritadaki ev konumu
        public string ColorHex;        // Yer tutucu görsel rengi
        public string[] AmbientTr { get { return Loc.Pick(_ambientTr, AmbientEn); } set { _ambientTr = value; } }     // Görevi olmayan köylülerin sohbet cümleleri
        string[] _ambientTr; public string[] AmbientEn;
    }

    [Serializable]
    public class QuestData
    {
        public string Id;
        public string VillagerId;
        public int MinLevel;
        public string[] DialogueTr { get { return Loc.Pick(_dialogueTr, DialogueEn); } set { _dialogueTr = value; } }            // Köylünün derdini anlattığı satırlar
        string[] _dialogueTr; public string[] DialogueEn;
        public PotionEffect Needed;
        public string SuccessTr { get { return Loc.Pick(_successTr, SuccessEn); } set { _successTr = value; } }
        string _successTr; public string SuccessEn;
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
            EnsureData();
            English.ApplyVillagers(_villagers); English.ApplyQuests(_quests);
        }

        private static void EnsureData()
        {
            if (_villagers != null) return;
            _villagers = new List<VillagerData>();
            _quests = new List<QuestData>();

            // Köylüler: adları evlerinin tabelasındaki meslekle aynıdır (Fırıncı, Demirci, Aşçı...)
            V("fistik", "Çiftçi", "Çiftçi", 1, 4, "#A9744F", "Toprak cömerttir ama ekşiyse hiçbir şey yetişmez.", "Bu yıl kabaklar kocaman olacak, hissediyorum!");
            V("bal", "Fırıncı", "Fırıncı", 2, 2, "#F5B041", "Karbonat hamuru kabartır, maya ise sabrı.", "Fırının ısısı iyi ekmeğin yarısıdır.");
            V("pirtik", "Çoban", "Çoban", 1, 2, "#F1948A", "Koyunlarım beni dinler, keçiler dinlemez.", "Yün kırkarken hep hapşırırım!");
            V("tokmak", "Demirci", "Demirci", 1, 1, "#5D6D7E", "Demir paslanır, çelik dayanır. Fark karbondadır!", "Örs başında ritim her şeydir: tak, tak, tak!");
            V("ebe", "Şifacı", "Şifacı", 2, 4, "#AF7AC5", "Her yaraya bir iksir, her derde bir çay.", "Yaraları temiz tutarsan yarısı kendi kendine iyileşir.");
            V("fener", "Av Koruyucusu", "Av Koruyucusu", 4, 4, "#F4D03F", "Gece ormanda gözler parlar. Çoğu baykuş. Çoğu...", "İz sürmek sabır ister, tıpkı simya gibi.");
            V("zeytin", "Hancı", "Hancı", 4, 2, "#7DCEA0", "Handa bugün meyve gazozu var. Gazı da senden!", "Yolcular uzak diyarlardan haberler getirir.");
            V("kopuk", "Çamaşırcı", "Çamaşırcı", 4, 3, "#85C1E9", "Sabun yağı söker, ben de lekeleri!", "Rüzgârlı gün, çamaşırın bayramıdır.");
            V("filiz", "Ormancı", "Ormancı", 3, 1, "#58D68D", "Her ağaç bir zamanlar bir tohumdu.", "Ormanda yangın çıkmasın diye gözüm hep dumanda.");
            V("ruzgar", "Değirmenci", "Değirmenci", 2, 1, "#D7BDE2", "Rüzgâr eserse un, esmezse dert.", "Değirmen taşı döndükçe köy doyar.");
            // Köyün diğer sakinleri
            V("oduncu", "Oduncu", "Oduncu", 1, 0, "#8D6E63", "Bu meşeler yüz yaşında. Külleri bile bereketlidir, potasyum doludur!", "Baltam keskin, odunum kuru. Kış hazır!");
            V("marangoz", "Marangoz", "Marangoz", 2, 0, "#A1887F", "Kazanın için yeni bir raf mı lazım? Ölç, biç, sonra kes!", "Talaş kokusu gibisi yok.");
            V("tasusta", "Taş Ustası", "Taş Ustası", 4, 0, "#90A4AE", "Kireç taşını kızdırırsan sönmemiş kireç olur, bilir misin?", "Her taşın içinde bir heykel saklıdır.");
            V("derici", "Derici", "Derici", 4, 1, "#795548", "Deriyi yumuşatmak için tuz ve sabır gerekir.", "Kokuya alışırsın. Galiba.");
            V("komurcu", "Kömürcü", "Kömürcü", 0, 2, "#455A64", "Kömür saf karbondur! Ben de biraz karbon gibi görünüyorum.", "Ocakların hepsi benim kömürümle yanar.");
            V("kunduraci", "Kunduracı", "Kunduracı", 5, 2, "#6D4C41", "Ayakkabın delinmiş Aqualf. Damla gibi akıyorsun!", "Her adım bir hikâyedir.");
            V("asci", "Aşçı", "Aşçı", 1, 3, "#EF6C00", "Mutfak da bir laboratuvar! Tuz, karbonat, sirke... hepsi kimya.", "Çorbanın sırrı: azıcık tuz.");
            V("sucu", "Sucu", "Sucu", 2, 3, "#29B6F6", "Kuyu suyu tertemiz. H2O, başka bir şey yok!", "Kovalarım hep dolu, gönlüm de.");
            V("terzi", "Terzi", "Terzi", 5, 3, "#AB47BC", "Kumaşları göztaşıyla mavi, pasla kırmızı boyarım.", "Ölçü al, ölçü al, sonra bir daha ölçü al.");
            V("dokumaci", "Dokumacı", "Dokumacı", 0, 4, "#EC407A", "Tezgâhımda renk renk ipler... boyaları senden alıyorum!", "Bir ilmek daha, bir ilmek daha.");
            V("balikci", "Balıkçı", "Balıkçı", 5, 4, "#0277BD", "Nehir bugün cömert. Deniz suyu da tuz doludur, haberin olsun.", "Sabır balıkçının en iyi yemidir.");
            V("comlekci", "Çömlekçi", "Çömlekçi", 1, 5, "#BF7B4C", "Kil ve kum, ateşte birleşince sağlam bir çömlek olur.", "Elime çamur bulaşmadan gün geçmez.");
            V("berber", "Berber", "Berber", 2, 5, "#7E57C2", "Saçların hâlâ isli mi? Gel bir düzelteyim!", "Köyün bütün dedikoduları bu koltukta konuşulur.");
            V("arici", "Arıcı", "Arıcı", 4, 5, "#FBC02D", "Arılarım çiçekleri sever; sen de iksirleri.", "Bal, doğanın kendi simyasıdır.");

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
                "Çiftçi'nin bütün ekşi eriklerini yedim... karnım yanıyor.",
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

            // ---- Köyün diğer 14 sakininin görevleri (24 köylünün hepsi görev verir) ----
            Q("q_well", "sucu", 1, PotionEffect.Hydrate, 20, 15, "Kovalar yine dolu! Köyün suyu sana emanet.",
                new[] { "dewcap" },
                "Aqualf, kuyunun dibi göründü! Kovalarım boş kaldı.",
                "Bir kova saf su yapabilir misin? İki hidrojen, bir oksijen... öyle demiştin.");
            Q("q_hide", "derici", 2, PotionEffect.Preserve, 25, 20, "Deriler kokmuyor, çürümüyor. Harika!",
                new[] { "seawater" },
                "Yeni tabakladığım deriler bozulmaya başladı.",
                "Tuz deriyi korur derler. Bana biraz tuz lazım.");
            Q("q_bread", "asci", 2, PotionEffect.Fizz, 30, 25, "Hamur kabardı, çorba köpürdü! Mutfak bayram yeri.",
                new[] { "limestone" },
                "Ekmek hamurum kabarmıyor, gazozum da gazsız!",
                "Kabarcık yapan o gazı getirebilir misin? Karbon ve oksijen, bir de biraz ısı.");
            Q("q_flowers", "arici", 2, PotionEffect.Growth, 30, 25, "Çiçekler açtı, arılar vızıldıyor! Sana bir kavanoz bal.",
                new[] { "guano" },
                "Arılarımın çiçekleri soldu, bal azaldı.",
                "Toprağı besleyecek bir gübre lazım. Azotlu bir şey...");
            Q("q_wool", "dokumaci", 3, PotionEffect.Cleanse, 35, 30, "Yünler bembeyaz! Tezgâhım yeniden çalışıyor.",
                new[] { "ash" },
                "Yünler yağlı ve kirli, ipliğe dönüşmüyor.",
                "Yağı söken o güçlü bazdan yapabilir misin?");
            Q("q_razor", "berber", 3, PotionEffect.Disinfect, 35, 30, "Usturalar tertemiz! Müşterilerim artık rahat.",
                new[] { "air" },
                "Usturalarımı mikroplardan arındırmam gerek.",
                "Köpüren, mikrop öldüren o sıvı... ama sakın ısıtma!");
            Q("q_marble", "tasusta", 4, PotionEffect.Dissolve, 40, 35, "Taşlar parlıyor, pas izleri gitti!",
                new[] { "limestone" },
                "Heykelimin üstü pas ve kireç lekesiyle doldu.",
                "Lekeleri çözecek bir asit lazım. Hidrojen ve klor, öyle diyorlar.");
            Q("q_ember", "komurcu", 4, PotionEffect.Cool, 45, 40, "Ohh... elim serinledi. Teşekkürler simyacı!",
                new[] { "ash" },
                "Kömür ocağından bir kor sıçradı, elim yandı!",
                "Isıyı emen serin bir kompres hazırlayabilir misin?");
            Q("q_moths", "terzi", 4, PotionEffect.PestControl, 45, 40, "Güveler kaçtı! Kumaşlarım kurtuldu.",
                new[] { "malachite" },
                "Güveler kumaşlarımı delik deşik ediyor!",
                "Böcekleri kaçıran o mavi kristalden yapabilir misin?");
            Q("q_night", "balikci", 4, PotionEffect.Light, 50, 45, "Nehir gece gündüz gibi! Balıklar ışığa geldi.",
                new[] { "magnesite" },
                "Gece balığına çıkacağım ama fenerim söndü.",
                "Göz kamaştıran beyaz bir ışık lazım bana.");
            Q("q_beams", "marangoz", 5, PotionEffect.Build, 55, 50, "Çatı kirişleri taş gibi oturdu!",
                new[] { "limestone", "sand" },
                "Atölyemin temeli çatladı, kirişler oynuyor.",
                "Harca katılan o beyaz taşı yapabilir misin?");
            Q("q_campfire", "oduncu", 5, PotionEffect.Fuel, 55, 50, "Kamp ateşi isiz, mavi alevle yanıyor!",
                new[] { "dewcap" },
                "Ormanda gece kampı kuracağım, temiz yanan bir yakıt lazım.",
                "Karbon, hidrojen ve biraz oksijen... mavi alevli o sıvı!");
            Q("q_leather", "kunduraci", 5, PotionEffect.Pigment, 50, 45, "Çizmeler kıpkırmızı! Festivalde herkes bunları giyecek.",
                new[] { "hematite" },
                "Festival çizmelerini boyamam lazım ama boyam bitti.",
                "Pasın kırmızısı tam istediğim renk!");
            Q("q_glaze", "comlekci", 6, PotionEffect.Glass, 65, 55, "Çömleklerim cam gibi sırlandı!",
                new[] { "sand" },
                "Çömleklerimi sırlamak için cam tozu lazım.",
                "Kumdan cam olur, ama çok sıcak bir ateşte!");

            // ---- Zekâ görevleri: her köylünün kimyayla çözülen ikinci bir derdi ----
            Q("q_ice_pier", "balikci", 3, PotionEffect.Preserve, 40, 35, "İskele artık kaymıyor! Tuz buzun donma noktasını düşürdü, buz eridi.",
                new[] { "seawater" },
                "Gece ayaz vurdu, iskele cam gibi buz tuttu. Az kalsın nehre uçuyordum!",
                "Deniz suyu kolay kolay donmaz, değil mi? Neden acaba... İçindeki o beyaz tanecikler yüzünden mi?");
            Q("q_bee_sting", "arici", 2, PotionEffect.Soothe, 30, 25, "Şişlik indi, kaşıntı geçti! Baz, iğnedeki asidi nötrledi.",
                new[] { "dewcap" },
                "Bir arı tam burnumun ucunu soktu. Şimdi burnum bir şalgam!",
                "Arı iğnesi biraz asidikmiş. Asidi söndüren, mideye de iyi gelen o beyaz tozdan sürsem?");
            Q("q_sour_bread", "bal", 3, PotionEffect.Soothe, 40, 35, "Ekmek hem kabardı hem ekşiliği gitti. Karbonat + asit = kabarcık!",
                new[] { "ash" },
                "Ekşi mayam fazla coştu, ekmekler limon gibi ekşi oldu.",
                "Ekşiliği alacak, üstüne bir de hamuru kabartacak bir şey lazım. Bir taşla iki kuş!");
            Q("q_axe_rust", "oduncu", 3, PotionEffect.Dissolve, 40, 35, "Balta pırıl pırıl! Asit pası çözdü, demir göründü.",
                new[] { "hematite" },
                "Baltamı yağmurda unuttum, üstü kıpkırmızı pas tuttu.",
                "Pası kazımak yerine eritebilsem... Güçlü bir asit pası çözer, öyle değil mi?");
            Q("q_etching", "tokmak", 4, PotionEffect.Dissolve, 55, 50, "Kılıcın üstündeki desen harika çıktı! Asit sadece mumsuz yerleri yedi.",
                new[] { "hematite" },
                "Kralın muhafızı kılıcına ejderha deseni istiyor. Çekiçle olmaz bu iş.",
                "Çeliği mumla kaplayıp deseni kazıyacağım. Açıkta kalan yerleri yiyecek bir asit lazım.");
            Q("q_sour_soil", "fistik", 4, PotionEffect.Build, 50, 45, "Lahanalar fışkırdı! Kireç taşı toprağın asidini nötrledi.",
                new[] { "guano" },
                "Tarlamın toprağı ekşi, lahanalarım sapsarı.",
                "Babam toprağa beyaz taş tozu serperdi. Taş bina yapmaya yarar ama toprağı da tatlandırırmış!");
            Q("q_damp_flour", "ruzgar", 5, PotionEffect.Warmth, 60, 55, "Ambar kupkuru! Sönmemiş kireç nemi kapıp hapsetti.",
                new[] { "limestone" },
                "Ambarda nem var, un çuvalları küflenmeye başladı.",
                "Havadaki suyu kendine çeken, suyla buluşunca ısınan o kireç... Ambarın köşesine bir kase koysak?");
            Q("q_woodworm", "marangoz", 4, PotionEffect.PestControl, 50, 45, "Kurtlar tahtayı bıraktı! Bakır tuzu ahşabı korur.",
                new[] { "malachite" },
                "Dolaplarımı tahta kurtları yiyor. Gece çıtır çıtır sesler geliyor!",
                "Bağcılar böceğe karşı mavi bir taş kullanıyor. Aynı şey ahşabı da korur mu?");
            Q("q_raddle", "pirtik", 3, PotionEffect.Pigment, 40, 35, "Koçlarım kırmızı göğüslü! Hangi koyun kimin, artık belli.",
                new[] { "hematite" },
                "Komşunun koyunlarıyla benimkiler karıştı. Hepsi aynı beyaz!",
                "Çobanlar eskiden koyunları kızıl toprakla işaretlermiş. Demirden gelen o kızıl boyadan var mı?");
            Q("q_fever", "ebe", 4, PotionEffect.Cool, 55, 50, "Ateş düştü. Soğutan kompres ısıyı çekti aldı.",
                new[] { "air" },
                "Değirmencinin çocuğu ateşler içinde yanıyor.",
                "Suya karışınca kendiliğinden buz gibi olan o tuzdan bir kompres yapabilir misin?");
            Q("q_greenhouse", "filiz", 5, PotionEffect.Glass, 65, 55, "Sera hazır! Cam ışığı alır, ısıyı tutar; fideler bayılacak.",
                new[] { "sand" },
                "Kış gelmeden nadir fidelerimi korumam gerek. Bir sera yapmak istiyorum.",
                "Işığı geçiren ama soğuğu geçirmeyen bir şey... Kumdan yapılan o şeffaf şey!");
            Q("q_flare", "fener", 6, PotionEffect.Fireworks, 75, 65, "Gökyüzü kıpkırmızı parladı, kayıp avcı yolunu buldu!",
                new[] { "crystal" },
                "Bir avcı ormanda kayboldu, akşam çöküyor.",
                "Gökyüzüne bir işaret fişeği atsam görür. Potasyum, azot, oksijen... hızlı yanan o tuz!");
            Q("q_lemonade", "zeytin", 3, PotionEffect.Fizz, 40, 35, "Limonata fokur fokur! Müşteriler bardaklarını tokuşturuyor.",
                new[] { "dewcap" },
                "Handaki limonata düz su gibi, müşteriler esniyor.",
                "İçine o gıdıklayan kabarcıkları koyabilir miyiz? Kömür yanınca çıkan gaz değil mi o?");
            Q("q_bleach", "kopuk", 5, PotionEffect.Disinfect, 60, 55, "Gelinlik kar gibi bembeyaz! Peroksit sarı lekeleri oksitledi.",
                new[] { "air" },
                "Hancının kızı evleniyor ama gelinlik sandıkta sararmış.",
                "Mikrop öldüren, köpüren o sıvı aynı zamanda ağartır diyorlar. Soğuk tut ama!");
            Q("q_boulder", "tasusta", 6, PotionEffect.Fireworks, 80, 70, "BUM! Kaya paramparça, yol açıldı. Güherçile barutun kalbidir.",
                new[] { "sulfur" },
                "Heyelan oldu, dev bir kaya taş ocağının yolunu kapattı.",
                "Kırk adam itse kımıldamaz. Barutun içindeki o tuz lazım: güherçile!");
            Q("q_dehair", "derici", 4, PotionEffect.Cleanse, 50, 45, "Kıllar döküldü, deri tertemiz! Güçlü baz işini yaptı.",
                new[] { "ash" },
                "Deriler kıllı kıllı, tabaklayamıyorum.",
                "Kılları gevşetecek, yağı da sökecek güçlü bir baz lazım. Sabun yapımında kullanılan şey!");
            Q("q_smelly_boots", "kunduraci", 3, PotionEffect.Soothe, 40, 35, "Kokular gitti! Karbonat terdeki asitleri nötrledi.",
                new[] { "seawater" },
                "Oduncunun çizmeleri tamire geldi ama... kokusundan dükkâna giremiyorum!",
                "Kokuyu yapan şey ekşi bir asitmiş. Asidi nötrleyen beyaz toz neydi?");
            Q("q_mirror", "terzi", 6, PotionEffect.Glass, 70, 60, "Boy aynası geldi! Müşteriler kendilerine hayran.",
                new[] { "sand" },
                "Müşteriler kıyafetleri denerken kendilerini göremiyor.",
                "Gümüşle kaplanınca ayna olan o şeffaf malzeme lazım. Kum ve büyük ateş!");
            Q("q_mordant", "dokumaci", 5, PotionEffect.PestControl, 60, 55, "Renkler yıkansa da akmıyor! Bakır sülfat boyayı ipliğe kilitledi.",
                new[] { "malachite" },
                "Halılarımın boyası ilk yıkamada akıp gidiyor.",
                "Boyacılar 'mordan' denen bir tuzla boyayı ipliğe sabitlermiş. O mavi kristal olabilir mi?");
            Q("q_red_glaze", "comlekci", 4, PotionEffect.Pigment, 50, 45, "Fırından kızıl sırlı çömlekler çıktı! Demir oksit ateşte renk verir.",
                new[] { "hematite" },
                "Pazarda herkes kızıl çömlek istiyor, benimkiler hep kahverengi.",
                "Sıra demir pasının kızılını kullanmakta. Ateşte solmayan bir renk o!");
            Q("q_smolder", "komurcu", 6, PotionEffect.Fizz, 75, 65, "Ateş söndü! CO2 havadan ağırdır, oksijeni kesip alevi boğdu.",
                new[] { "limestone" },
                "Kömür yığınım içten için için yanıyor, su dökersem buhar patlaması olur!",
                "Ateşin nefesini kesecek, oksijenin yerini alacak ağır bir gaz lazım.");
            Q("q_green_well", "sucu", 3, PotionEffect.Disinfect, 40, 35, "Kuyu yine berrak! Peroksit yosunu yaktı, geriye su ve oksijen kaldı.",
                new[] { "air" },
                "Kuyunun suyu yeşillendi, yosun bastı. Kimse içmek istemiyor.",
                "Suyu temizleyen ama bozunduğunda sadece su ve oksijen bırakan bir şey olsa...");
            Q("q_copper_pots", "asci", 4, PotionEffect.Dissolve, 50, 45, "Bakır tencereler gün gibi parlıyor! Asit kararmayı çözdü.",
                new[] { "malachite" },
                "Bakır tencerelerim kapkara ve yeşil lekeli oldu.",
                "Biraz asit bakırın üstündeki kirli tabakayı çözer. Mideye de benzeyen o asit!");
            Q("q_shave_soap", "berber", 5, PotionEffect.Cleanse, 55, 50, "Köpük bol, ustura kaygan! Sabunlaşma harikası.",
                new[] { "ash" },
                "Tıraş sabunum bitti, müşteriler kuru kuru tıraş oluyor. Ah ah!",
                "Yağı köpüklü sabuna dönüştüren güçlü bazı getirirsen hemen sabun pişiririm.");

            // ---- Görev bankası: toplam 2000 göreve tamamla (Türkçe + İngilizce) ----
            QuestBank.GenerateInto(_quests);
        }
    }

    public static class QuestResolver
    {
        public static QuestOutcome Deliver(QuestData q, CompoundData potion, Random rng)
        {
            var o = new QuestOutcome();
            var v = VillageDatabase.GetVillager(q.VillagerId);
            string name = v != null ? v.NameTr : Loc.T("Köylü", "Villager");

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
                o.MessageTr = Loc.En
                    ? name + ": \"Hmm... this tastes like a " + CompoundDatabase.EffectNameTr(eff).ToLowerInvariant() +
                      " potion. It didn't really help with my problem.\""
                    : name + ": \"Hmm... bu " + CompoundDatabase.EffectNameTr(eff).ToLowerInvariant() +
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
        /// <param name="canMake">Verilirse, oyuncunun şu an yapabildiği iksirleri isteyen görevler önce gelir.</param>
        public static QuestData NextQuest(int level, ICollection<string> completed, Random rng, Func<QuestData, bool> canMake = null)
        {
            var open = VillageDatabase.Quests.Where(q => q.MinLevel <= level && !completed.Contains(q.Id)).ToList();
            var story = (canMake != null ? open.Where(canMake).OrderBy(q => q.MinLevel).FirstOrDefault() : null)
                        ?? open.OrderBy(q => q.MinLevel).FirstOrDefault();
            if (story != null) return story;
            var pool = VillageDatabase.Quests.Where(q => q.MinLevel <= level).ToList();
            if (canMake != null && pool.Any(canMake)) pool = pool.Where(canMake).ToList();
            return pool.Count == 0 ? null : pool[rng.Next(pool.Count)];
        }
    }
}
