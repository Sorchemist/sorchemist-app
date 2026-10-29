// Dil desteği (Türkçe / İngilizce). Varsayılan dil Türkçe.
// Kullanım: Loc.T("Kazan", "Cauldron"). Oyun verilerindeki *Tr alanları seçili dilde metin döndürür
// (İngilizce karşılıkları English.cs içindedir).
namespace Sorchemist.Core
{
    public enum Lang { Tr = 0, En = 1 }

    public static class Loc
    {
        public static Lang Current = Lang.Tr;
        public static bool En { get { return Current == Lang.En; } }

        /// <summary>Seçili dile göre Türkçe ya da İngilizce metni döndürür.</summary>
        public static string T(string tr, string en) { return En && en != null ? en : tr; }

        /// <summary>İngilizce karşılığı varsa ve dil İngilizceyse onu, değilse Türkçeyi döndürür.</summary>
        public static string Pick(string tr, string en) { return En && !string.IsNullOrEmpty(en) ? en : tr; }

        public static string[] Pick(string[] tr, string[] en) { return En && en != null && en.Length > 0 ? en : tr; }
    }
}
