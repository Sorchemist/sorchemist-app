// Çevrimiçi özelliklerin saf (Unity'siz) kısmı: kaydı sıkıştırma, sıralama istatistikleri ve
// arkadaşların ziyaret ettiğinde göreceği köy vitrini.
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Sorchemist.Core
{
    public static class CloudData
    {
        /// <summary>Kaydın JSON metnini gzip ile sıkıştırıp base64'e çevirir (2000 görevlik kayıt ~24 KB → ~6 KB).</summary>
        public static string Pack(string json)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            using (var ms = new MemoryStream())
            {
                using (var gz = new GZipStream(ms, CompressionMode.Compress, true)) gz.Write(bytes, 0, bytes.Length);
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        public static string Unpack(string packed)
        {
            var data = Convert.FromBase64String(packed);
            using (var ms = new MemoryStream(data))
            using (var gz = new GZipStream(ms, CompressionMode.Decompress))
            using (var r = new StreamReader(gz, Encoding.UTF8)) return r.ReadToEnd();
        }

        /// <summary>Haftalık sıralamalar için sayaçlar.</summary>
        public static Dictionary<string, object> Stats(PlayerState s)
        {
            return new Dictionary<string, object>
            {
                { "level", s.Level },
                { "discoveries", s.KnownElements.Count + s.Discovered.Count },
                { "faildex", s.FailDex.Count },
                { "quests", s.CompletedQuests.Count },
            };
        }

        /// <summary>Arkadaşların görebileceği köy özeti (envanter ve altın gibi özel bilgiler yok).</summary>
        public static Dictionary<string, object> Village(PlayerState s, string name)
        {
            var buildings = new List<object>();
            foreach (var b in s.Buildings) buildings.Add(new Dictionary<string, object> { { "id", b.Id }, { "lv", b.Level } });
            var houses = new List<object>();
            foreach (var h in s.Houses) if (h.State != 0) houses.Add(new Dictionary<string, object> { { "id", h.VillagerId }, { "st", h.State } });
            return new Dictionary<string, object>
            {
                { "name", name ?? "" },
                { "level", s.Level },
                { "day", s.Day },
                { "tower", s.TowerLevel },
                { "buildings", buildings },
                { "houses", houses },
                { "discovered", s.Discovered.Count },
                { "faildex", s.FailDex.Count },
            };
        }
    }
}
