// Fail-Dex: komik ve epik başarısızlıklar ansiklopedisi. Viralitenin ana kaynağı.
using System;
using System.Collections.Generic;

namespace Sorchemist.Core
{
    public enum FailRarity { Common, Rare, Epic, Legendary }

    // Unity tarafı bu tipe göre animasyon/efekt seçer
    public enum FailVisual
    {
        Smoke, Explosion, HairOnFire, RoofBlown, Foam, GreenCloud, Glow, GoldFlash,
        Flash, Sludge, Frog, GiantVeg, Frozen, Floating, PinkPaint, Bubbles, Nothing
    }

    [Serializable]
    public class FailEntry
    {
        public string Id;
        public string TitleTr;
        public string DescriptionTr;  // Ne oldu (komik)
        public string ScienceTr;      // Neden oldu (eğitici)
        public FailRarity Rarity;
        public FailVisual Visual;
        public int Xp;                // Başarısızlık da öğretir!
    }

    public static class FailDexDatabase
    {
        private static Dictionary<string, FailEntry> _all;
        private static List<FailEntry> _ordered;

        public static IReadOnlyList<FailEntry> All { get { Ensure(); return _ordered; } }
        public static FailEntry Get(string id) { Ensure(); FailEntry e; return _all.TryGetValue(id, out e) ? e : null; }

        private static void Add(string id, string title, FailRarity r, FailVisual v, int xp, string desc, string sci)
        {
            var e = new FailEntry { Id = id, TitleTr = title, Rarity = r, Visual = v, Xp = xp, DescriptionTr = desc, ScienceTr = sci };
            _all[id] = e;
            _ordered.Add(e);
        }

        private static void Ensure()
        {
            if (_all != null) return;
            _all = new Dictionary<string, FailEntry>();
            _ordered = new List<FailEntry>();

            // --- Laboratuvar başarısızlıkları ---
            Add("mystery_goo", "Gizemli Bulamaç", FailRarity.Common, FailVisual.Sludge, 2,
                "Kazanda gri, şüpheli bir şey fokurduyor. Aqualf onu 'sanat' olarak sınıflandırdı.",
                "Atom oranları bilinen bir bileşiğe uymuyor. Kimyada oranlar (stokiyometri) her şeydir.");
            Add("near_miss", "Neredeyse!", FailRarity.Common, FailVisual.Bubbles, 4,
                "Kazan umutla köpürdü... sonra vazgeçti.",
                "Bir atom fazla ya da eksik! Formülü tekrar say.");
            Add("lukewarm_sludge", "Uyuşuk Çamur", FailRarity.Common, FailVisual.Sludge, 3,
                "Malzemeler kazanda birbirine bakıp esnedi. Hiçbir şey olmadı.",
                "Tepkimenin başlaması için 'aktivasyon enerjisi' gerekir. Biraz daha ısı!");
            Add("burnt", "Yanık Kokusu", FailRarity.Common, FailVisual.Smoke, 3,
                "Laboratuvar kara dumanla doldu. Aqualf'in kaşları artık biraz daha ince.",
                "Fazla ısı ürünü bozundurabilir. Her tepkimenin ideal bir sıcaklık aralığı vardır.");
            Add("lumpy", "Topak Topak", FailRarity.Common, FailVisual.Sludge, 3,
                "İksir, büyükannenin kötü günündeki irmik helvasına benziyor.",
                "Karıştırmak tanecikleri buluşturur; çarpışma olmadan tepkime olmaz (çarpışma teorisi).");
            Add("roof_blown", "Çatı Uçtu!", FailRarity.Rare, FailVisual.RoofBlown, 8,
                "BUM! Laboratuvarın çatısı köy meydanına kondu. Köylüler alkışladı.",
                "Aşırı ısı gazları hızla genleştirir. Kapalı kapta basınç = patlama.");
            Add("alkali_boom", "Saçlar Alev Aldı", FailRarity.Rare, FailVisual.HairOnFire, 10,
                "Alkali metal suyla buluştu. Aqualf'in saçı artık bir meşale.",
                "Sodyum ve potasyum suyla şiddetle tepkimeye girer: hidrojen gazı ve ısı açığa çıkar.");
            Add("peroxide_foam", "Fil Diş Macunu", FailRarity.Epic, FailVisual.Foam, 15,
                "Kazandan dev bir köpük sütunu fışkırdı ve laboratuvarı doldurdu!",
                "Hidrojen peroksit ısıyla hızla su ve oksijene ayrışır. Ünlü 'fil diş macunu' deneyi!");
            Add("noble_snub", "Soylu Sessizlik", FailRarity.Rare, FailVisual.Nothing, 6,
                "Soy gazlar kimseyle konuşmadı. Kazan sadece kibirli bir parıltı yaydı.",
                "Soy gazların dış elektron kabuğu doludur; neredeyse hiç tepkimeye girmezler.");
            Add("hydrogen_pop", "Pıt!", FailRarity.Common, FailVisual.Flash, 4,
                "Minicik bir 'pıt' sesi. Aqualf bunu bir başarı saydı.",
                "Hidrojen oksijenle tutuşunca karakteristik bir 'pıt' sesi çıkarır. Okullarda H2 testidir.");
            Add("green_cloud", "Yeşil Bulut", FailRarity.Rare, FailVisual.GreenCloud, 8,
                "Sarı-yeşil bir bulut yükseldi. Laboratuvarın kedisi taşınmayı düşünüyor.",
                "Klor gazı zehirlidir ve sarı-yeşil renktedir. Tek başına değil, tuz gibi bileşiklerde güvenlidir.");
            Add("mercury_hat", "Gümüş Buhar", FailRarity.Epic, FailVisual.Smoke, 12,
                "Aqualf'in sivri şapkası gümüş bir buharla eridi. Biraz da tuhaf hissediyor.",
                "Cıva buharı çok zehirlidir. 'Deli şapkacı' deyimi şapka yapımında kullanılan cıvadan gelir.");
            Add("glow_beard", "Parlayan Sakal", FailRarity.Epic, FailVisual.Glow, 15,
                "Aqualf'in sakalı karanlıkta yeşil parlıyor. Gece okumak için kullanışlı.",
                "Radyoaktif elementler kararsız çekirdekleri nedeniyle ışınım yayar. (Gerçekte asla dokunma!)");
            Add("midas", "Midas Kazanı", FailRarity.Legendary, FailVisual.GoldFlash, 25,
                "Üç saniyeliğine her şey altına döndü. Sonra geri döndü. Kimse inanmadı.",
                "Simyacıların altın yapma hayali: elementler kimyasal yolla birbirine dönüşemez, ancak nükleer tepkimeyle.");
            Add("blinding_flash", "Kör Edici Işık", FailRarity.Rare, FailVisual.Flash, 8,
                "Göz kamaştıran bir parıltı! Aqualf on dakika boyunca mor benekler gördü.",
                "Magnezyum yanarken çok parlak beyaz ışık yayar; ama yanlış oranlarla dağınık bir parlama olur.");
            Add("rotten_egg", "Çürük Yumurta", FailRarity.Common, FailVisual.GreenCloud, 4,
                "Bütün köy burnunu tuttu. Fırıncı ekmeklerin bozulduğunu sandı.",
                "Isıtılan kükürt bileşikleri hidrojen sülfür (H2S) üretir: çürük yumurta kokusu.");
            Add("overflow", "Kazan Taştı", FailRarity.Common, FailVisual.Foam, 2,
                "Çok fazla malzeme! Kazan ayaklandı ve fokurdayarak taştı.",
                "Gereğinden fazla madde, istenmeyen yan ürünler demektir.");

            // --- Köylü (görev) başarısızlıkları: yanlış iksir yanlış kişiye ---
            Add("giant_pumpkins", "Kabak İstilası", FailRarity.Epic, FailVisual.GiantVeg, 15,
                "Böcek kovacak iksir yerine gübre verdin. Ertesi sabah ev, dev kabakların altında kaldı.",
                "Azotlu gübreler bitki büyümesini hızlandırır; haşereleri değil!");
            Add("frog_villager", "Vırak!", FailRarity.Epic, FailVisual.Frog, 15,
                "Köylü iksiri içti ve... kurbağaya dönüştü. Durumdan pek şikayetçi değil.",
                "Bu tamamen büyü. Kimyada kimse kurbağaya dönüşmez. Muhtemelen.");
            Add("floating_villager", "Balon Adam", FailRarity.Rare, FailVisual.Floating, 10,
                "Gazlı iksirden sonra köylü şişti ve baca hizasında süzülmeye başladı.",
                "CO2 gazı sıvıdan kaçarken hacim kaplar. Ama insanı uçurmaz... burası Sorchemist köyü.");
            Add("frozen_house", "Buz Sarayı", FailRarity.Rare, FailVisual.Frozen, 10,
                "Serinletici iksir fazla iyi çalıştı. Evin her yeri buz tuttu.",
                "Endotermik tepkimeler çevreden ısı çeker. Fazlası buzlanma demek.");
            Add("foam_flood", "Köpük Seli", FailRarity.Rare, FailVisual.Foam, 10,
                "Sabun yerine yanlış şey... sokaklar köpükle doldu. Çocuklar çok mutlu.",
                "Bazlar yağlarla sabun ve köpük oluşturur.");
            Add("glowing_house", "Fener Ev", FailRarity.Rare, FailVisual.Glow, 10,
                "Ev artık gece gündüz parlıyor. Komşular uyuyamıyor.",
                "Işık yayan tepkimeler (kemilüminesans) gerçektir; ateş böcekleri de bunu yapar!");
            Add("pink_village", "Pembe Köy", FailRarity.Rare, FailVisual.PinkPaint, 10,
                "Boya iksiri rüzgârla dağıldı. Bütün tavuklar artık kırmızımsı pembe.",
                "Demir oksit binlerce yıldır kullanılan doğal bir pigmenttir.");
            Add("fireworks_mishap", "Erken Festival", FailRarity.Epic, FailVisual.Explosion, 15,
                "Havai fişek iksiri yanlış yerde patladı. Köyün en güzel gösterisi... saman ambarında.",
                "Nitratlar oksijen sağlayarak yanmayı hızlandırır. Bu yüzden barutun temelidir.");
        }
    }
}
