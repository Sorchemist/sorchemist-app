// Mevsim, hava durumu ve gün içi saat (Aqualf kasabasının atmosferi).
// - Mevsimler gerçek saate bağlı 20 saatlik bir döngüdür: Büyülü İlkbahar 6 sa, Kızıl Sonbahar 6 sa,
//   Kristal Kış 6 sa, Fırtınalı Muson 2 sa (özel olay). Her cihazda aynı anda aynı mevsim yaşanır.
// - Bir oyun günü 24 gerçek dakikadır (oyun saatinin 1 saati = 1 gerçek dakika): gece, şafak, öğle, alacakaranlık.
// Mevsimler binaların üretimini (BuildingDatabase.SeasonGoldFactor) ve kazandaki tepkimelerin süresini etkiler.
using System;

namespace Sorchemist.Core
{
    public enum Season { Spring, Autumn, Winter, Storm }

    public static class SeasonClock
    {
        public const long CycleSeconds = 20 * 3600;
        static readonly long[] Starts = { 0, 6 * 3600, 12 * 3600, 18 * 3600 };
        static readonly long[] Lengths = { 6 * 3600, 6 * 3600, 6 * 3600, 2 * 3600 };

        public static Season At(long unix)
        {
            long t = Mod(unix, CycleSeconds);
            for (int i = 3; i >= 0; i--) if (t >= Starts[i]) return (Season)i;
            return Season.Spring;
        }

        public static long SecondsLeft(long unix)
        {
            long t = Mod(unix, CycleSeconds);
            int i = (int)At(unix);
            return Starts[i] + Lengths[i] - t;
        }

        public static Season Next(Season s) { return (Season)(((int)s + 1) % 4); }

        static long Mod(long a, long m) { long r = a % m; return r < 0 ? r + m : r; }

        // ----- Gün içi saat -----
        public const long DaySeconds = 24 * 60;

        /// <summary>0 = gece yarısı, 0.25 = şafak, 0.5 = öğle, 0.75 = gün batımı.</summary>
        public static float TimeOfDay(long unix) { return Mod(unix, DaySeconds) / (float)DaySeconds; }

        public static string Clock(long unix)
        {
            long t = Mod(unix, DaySeconds);
            return (t / 60).ToString("00") + ":" + (t % 60).ToString("00");
        }

        /// <summary>Gece ışıklarının (pencereler, lambalar, rünler) parlaklığı: gece 1, gündüz 0.</summary>
        public static float NightGlow(float tod)
        {
            // 0.00..0.22 gece, 0.22..0.30 şafak, 0.30..0.70 gündüz, 0.70..0.78 akşam, 0.78..1 gece
            if (tod < 0.22f) return 1f;
            if (tod < 0.30f) return 1f - (tod - 0.22f) / 0.08f;
            if (tod < 0.70f) return 0f;
            if (tod < 0.78f) return (tod - 0.70f) / 0.08f;
            return 1f;
        }

        // ----- Mevsimin kazana etkisi -----
        /// <summary>Bu etkideki bir iksirin demlenme süresi çarpanı.</summary>
        public static float BrewFactor(Season s, PotionEffect e)
        {
            switch (s)
            {
                case Season.Spring:   // ılık ve nemli: organik ve şifalı bileşikler hızlanır
                    if (e == PotionEffect.Growth || e == PotionEffect.Soothe || e == PotionEffect.Disinfect) return 0.75f;
                    break;
                case Season.Autumn:   // kuru ve rüzgârlı: yanma, eritme ve oksitlenme yarı sürede
                    if (e == PotionEffect.Fuel || e == PotionEffect.Fireworks || e == PotionEffect.Pigment || e == PotionEffect.Warmth) return 0.5f;
                    break;
                case Season.Winter:   // soğuk: ısı veren asit tepkimeleri ve soğutma rahat
                    if (e == PotionEffect.Dissolve || e == PotionEffect.Cool) return 0.75f;
                    break;
                case Season.Storm:    // yıldırım enerjisi: iyonlaşma ve elektroliz neredeyse anında
                    if (e == PotionEffect.Dissolve || e == PotionEffect.Cleanse || e == PotionEffect.Preserve) return 0.2f;
                    break;
            }
            return 1f;
        }

        /// <summary>İlkbaharda organik ve şifa iksirleri %25 olasılıkla çift çıkar.</summary>
        public static bool DoubleDropEffect(Season s, PotionEffect e)
        {
            return s == Season.Spring && (e == PotionEffect.Growth || e == PotionEffect.Soothe);
        }

        public static string NameTr(Season s)
        {
            switch (s)
            {
                case Season.Spring: return Loc.T("Büyülü İlkbahar", "Magical Spring");
                case Season.Autumn: return Loc.T("Kızıl Sonbahar", "Ember Autumn");
                case Season.Winter: return Loc.T("Kristal Kış", "Crystal Winter");
                default: return Loc.T("Fırtınalı Muson", "Arcane Storm");
            }
        }

        public static string EffectsTr(Season s)
        {
            switch (s)
            {
                case Season.Spring: return Loc.T(
                    "Cam Sera, Şifacı Kulübesi, Kök Ev ve Yaratık Ahırı %35 hızlı; Magma Dökümhanesi %15 yavaş.\nBüyüme, yatıştırma ve dezenfeksiyon iksirleri daha çabuk demlenir; büyüme ve yatıştırma iksirleri %25 ihtimalle çift çıkar.",
                    "Glasshouse, Healer Hut, Hollow Tree and Creature Stable +35%; Magma Smeltery -15%.\nGrowth, soothing and disinfecting potions brew faster; growth and soothing potions have a 25% chance to drop double.");
                case Season.Autumn: return Loc.T(
                    "Demirci Ocağı, Rüzgâr Değirmeni, Zeplin İskelesi ve Posta Kulesi %35 hızlı; Öz Kuyusu %15 yavaş.\nYakıt, havai fişek, boya ve ısıtma iksirleri yarı sürede demlenir.",
                    "Blacksmith Forge, Windmill, Airship Dock and Aviary +35%; Essence Well -15%.\nFuel, firework, pigment and warming potions brew in half the time.");
                case Season.Winter: return Loc.T(
                    "Kristal Kesim Evi, Gözlemevi ve Bakır Damıtma Tesisi %35 hızlı; Su Değirmeni ve Cam Sera %20 yavaş.\nNehir donar. Çözme ve soğutma iksirleri daha çabuk demlenir.",
                    "Crystal Cutter, Observatory and Copper Distillery +35%; Watermill and Glasshouse -20%.\nThe river freezes. Dissolving and cooling potions brew faster.");
                default: return Loc.T(
                    "Su Değirmeni, Öz Kuyusu, Boyut Geçidi ve Yıldıztaşı Krateri %50 hızlı; Demirci Ocağı ve Zeplin İskelesi %25 yavaş.\nYıldırım enerjisiyle çözme, temizleme ve koruma iksirleri neredeyse anında demlenir.",
                    "Watermill, Essence Well, Portal Gate and Meteorite Crater +50%; Forge and Airship Dock -25%.\nLightning energy makes dissolving, cleansing and preserving potions brew almost instantly.");
            }
        }
    }
}
