// Sanat yuvaları (sprite slots). Bir PNG'yi
//   Assets/Sorchemist/Resources/SorchemistArt/<anahtar>.png
// adıyla koyman yeterli: oyun onu otomatik kullanır. Dosya yoksa kodla çizilen
// yer tutucuya döner. Tüm anahtarların listesi: ASSET_LISTESI.md
using System.Collections.Generic;
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
