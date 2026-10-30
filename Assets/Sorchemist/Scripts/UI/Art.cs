// Sanat yuvaları (sprite slots). Bir PNG'yi
//   Assets/Sorchemist/Resources/SorchemistArt/<anahtar>.png
// adıyla koyman yeterli: oyun onu otomatik kullanır. Dosya yoksa kodla çizilen
// yer tutucuya döner. Tüm anahtarların listesi: ASSET_LISTESI.md
using System.Collections.Generic;
using Sorchemist.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Sorchemist.UI
{
    public static class Art
    {
        const string Folder = "SorchemistArt/";
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        /// <summary>Anahtara karşılık gelen sprite'ı döndürür, yoksa null.</summary>
        public static Sprite Get(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            // İngilizcede yazı içeren görsellerin İngilizce kopyası (el_8_en, house_bal_en) varsa onu kullan
            if (Loc.En && (key.StartsWith("el_") || key.StartsWith("house_")) && !key.EndsWith("_en"))
            {
                var en = Get(key + "_en");
                if (en != null) return en;
            }
            Sprite s;
            if (Cache.TryGetValue(key, out s)) return s;

            s = Resources.Load<Sprite>(Folder + key);
            if (s == null)
            {
                // Doku "Sprite" olarak içe aktarılmadıysa da çalışsın
                var tex = Resources.Load<Texture2D>(Folder + key);
                if (tex != null)
                    s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
            }
            Cache[key] = s;
            return s;
        }

        /// <summary>Sırayla dener: ilk bulunan sprite'ı döndürür.</summary>
        public static Sprite First(params string[] keys)
        {
            foreach (var k in keys)
            {
                var s = Get(k);
                if (s != null) return s;
            }
            return null;
        }

        public static bool Has(string key) { return Get(key) != null; }

        /// <summary>Bileşiğin iksir şişesi görseli (etkisine göre 6 şişe türünden biri).</summary>
        public static string PotionKey(CompoundData c)
        {
            switch (c.Effect)
            {
                case PotionEffect.Hydrate: return "potion_drop";
                case PotionEffect.Fizz: case PotionEffect.Cool: case PotionEffect.Fuel: return "potion_storm";
                case PotionEffect.Light: case PotionEffect.Glass: case PotionEffect.Growth: return "potion_star";
                case PotionEffect.Warmth: case PotionEffect.Build: case PotionEffect.Pigment: case PotionEffect.PestControl: return "potion_urn";
                case PotionEffect.Fireworks: return "potion_orb";
                default: return "potion_bottle";
            }
        }

        static readonly Dictionary<string, Sprite> SlicedCache = new Dictionary<string, Sprite>();

        /// <summary>9 dilimli (kenarları bozulmadan uzayan) sürüm. border: sol, alt, sağ, üst (piksel).</summary>
        public static Sprite Sliced(string key, Vector4 border)
        {
            Sprite s;
            if (SlicedCache.TryGetValue(key, out s)) return s;
            var src = Get(key);
            if (src != null)
                s = Sprite.Create(src.texture, src.textureRect, new Vector2(0.5f, 0.5f), src.pixelsPerUnit, 0,
                    SpriteMeshType.FullRect, border);
            SlicedCache[key] = s;
            return s;
        }

        /// <summary>Sprite'lı, en-boy oranını koruyan bir Image oluşturur.</summary>
        public static Image Image(Transform parent, string name, Sprite sprite, Color? tint = null)
        {
            var img = UIKit.Img(parent, name, tint ?? Color.white, sprite);
            img.type = UnityEngine.UI.Image.Type.Simple;
            img.preserveAspect = true;
            return img;
        }
    }
}
