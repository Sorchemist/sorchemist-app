// Kodla arayüz kurmak için yardımcılar. Sahneye elle hiçbir şey sürüklemen gerekmez.
// Yer tutucu sprite'ları (kare, daire, elmas, yuvarlak köşe) çalışma anında üretir.
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Sorchemist.UI
{
    public static class Theme
    {
        public static readonly Color Bg = UIKit.Hex("#1B1530");
        public static readonly Color Panel = UIKit.Hex("#2A2247");
        public static readonly Color PanelLight = UIKit.Hex("#3B3163");
        public static readonly Color Accent = UIKit.Hex("#7CF2C8");   // Aqualf turkuazı
        public static readonly Color Magic = UIKit.Hex("#B98CFF");
        public static readonly Color Gold = UIKit.Hex("#FFD166");
        public static readonly Color Danger = UIKit.Hex("#FF6B6B");
        public static readonly Color Text = UIKit.Hex("#F5F0FF");
        public static readonly Color TextDim = UIKit.Hex("#A99FC9");
        public static readonly Color Grass = UIKit.Hex("#6FBF73");
        public static readonly Color GrassDark = UIKit.Hex("#4E9A57");
        public static readonly Color Path = UIKit.Hex("#D9B98A");
        public static readonly Color Dim = new Color(0, 0, 0, 0.7f);
    }

    public static class UIKit
    {
        private static Font _font;
        private static Sprite _white, _circle, _diamond, _rounded;

        public static Font Font
        {
            get
            {
                if (_font != null) return _font;
                // Unity 2022.2+ "LegacyRuntime.ttf", daha eskiler "Arial.ttf" kullanır.
                try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch (Exception) { }
                if (_font == null) { try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch (Exception) { } }
                if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 32);
                return _font;
            }
        }

        public static Color Hex(string hex)
        {
            Color c;
            return ColorUtility.TryParseHtmlString(hex, out c) ? c : Color.magenta;
        }

        // ---------- Üretilen sprite'lar ----------
        public static Sprite White
        {
            get
            {
                if (_white == null)
                {
                    var t = new Texture2D(4, 4);
                    var px = new Color[16];
                    for (int i = 0; i < 16; i++) px[i] = Color.white;
                    t.SetPixels(px); t.Apply();
                    _white = Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100);
                }
                return _white;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (_circle == null) _circle = MakeShape(128, 128, (x, y) =>
                {
                    float dx = x - 63.5f, dy = y - 63.5f;
                    return Mathf.Clamp01(63.5f - Mathf.Sqrt(dx * dx + dy * dy));
                }, Vector4.zero);
                return _circle;
            }
        }

        // İzometrik zemin karosu (2:1 elmas)
        public static Sprite Diamond
        {
            get
            {
                if (_diamond == null) _diamond = MakeShape(256, 128, (x, y) =>
                {
                    float d = Mathf.Abs(x - 127.5f) / 128f + Mathf.Abs(y - 63.5f) / 64f;
                    return Mathf.Clamp01((1f - d) * 64f);
                }, Vector4.zero);
                return _diamond;
            }
        }

        private static Sprite _triangle;
        // Sivri şapka / çatı için üçgen
        public static Sprite Triangle
        {
            get
            {
                if (_triangle == null) _triangle = MakeShape(128, 128, (x, y) =>
                {
                    float half = (127 - y) / 127f * 64f;  // aşağıda geniş, yukarıda sivri
                    return Mathf.Clamp01(half - Mathf.Abs(x - 63.5f) + 0.5f);
                }, Vector4.zero);
                return _triangle;
            }
        }

        // 9-dilimli yuvarlak köşeli panel/buton
        public static Sprite Rounded
        {
            get
            {
                if (_rounded == null)
                {
                    const int s = 64; const float r = 24f;
                    _rounded = MakeShape(s, s, (x, y) =>
                    {
                        float cx = Mathf.Clamp(x, r, s - 1 - r), cy = Mathf.Clamp(y, r, s - 1 - r);
                        float dx = x - cx, dy = y - cy;
                        return Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    }, new Vector4(r + 1, r + 1, r + 1, r + 1));
                }
                return _rounded;
            }
        }

        private static Sprite MakeShape(int w, int h, Func<int, int, float> alpha, Vector4 border)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = new Color32(255, 255, 255, (byte)(alpha(x, y) * 255));
            t.SetPixels32(px); t.Apply();
            return Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, border);
        }

        // ---------- RectTransform yardımcıları ----------
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>anchor: ebeveyn içinde nokta (0..1), pos: o noktadan piksel kayması.</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Img(Transform parent, string name, Color color, Sprite sprite = null)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite ?? White;
            img.color = color;
            if (sprite == Rounded) img.type = Image.Type.Sliced;
            return img;
        }

        public static Image Panel(Transform parent, string name, Color color)
        {
            var img = Img(parent, name, color, Rounded);
            img.type = Image.Type.Sliced;
            return img;
        }

        public static Text Txt(Transform parent, string name, string text, int size, Color color,
            TextAnchor align = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            return t;
        }

        public static Button Btn(Transform parent, string name, string label, Color bg, Action onClick,
            int fontSize = 40, Color? textColor = null)
        {
            var img = Panel(parent, name, bg);
            var b = img.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            b.colors = colors;
            var t = Txt(img.transform, "Label", label, fontSize, textColor ?? Theme.Text);
            Stretch(t.rectTransform, 12, 6, 12, 6);
            if (onClick != null) b.onClick.AddListener(() => { AudioFXHook.Click(); onClick(); });
            return b;
        }

        public static Text LabelOf(Button b) { return b.GetComponentInChildren<Text>(); }

        /// <summary>Yazı alanı (e-posta, şifre, ad, kod). Mobilde dokununca klavye açılır.</summary>
        public static InputField Input(Transform parent, string name, string placeholder,
            InputField.ContentType type = InputField.ContentType.Standard, int fontSize = 34, int charLimit = 100)
        {
            var bg = Panel(parent, name, new Color(0.96f, 0.93f, 0.86f, 1f));
            var field = bg.gameObject.AddComponent<InputField>();
            var text = Txt(bg.transform, "Text", "", fontSize, WoodText, TextAnchor.MiddleLeft);
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            Stretch(text.rectTransform, 22, 6, 22, 6);
            var ph = Txt(bg.transform, "Placeholder", placeholder, fontSize, new Color(0.45f, 0.38f, 0.3f, 0.75f), TextAnchor.MiddleLeft, FontStyle.Italic);
            Stretch(ph.rectTransform, 22, 6, 22, 6);
            field.textComponent = text;
            field.placeholder = ph;
            field.targetGraphic = bg;
            field.contentType = type;
            field.characterLimit = charLimit;
            field.lineType = InputField.LineType.SingleLine;
            return field;
        }

        public static readonly Color WoodText = Hex("#3B2410");
        public static readonly Color WoodInactive = new Color(0.66f, 0.62f, 0.58f, 1f);
        public static bool HasWood { get { return Art.Has("btn_wood"); } }

        /// <summary>Butonu tahta tabela görünümüne çevirir (Art: btn_wood). tint: tahtayı hafifçe renklendirir.</summary>
        public static Button Wood(Button b, Color? textColor = null, Color? tint = null)
        {
            var sp = Art.Sliced("btn_wood", new Vector4(80, 50, 80, 50));
            if (sp == null) return b;
            var img = b.GetComponent<Image>();
            img.sprite = sp;
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1.7f;
            img.color = tint ?? Color.white;
            var t = LabelOf(b);
            if (t != null)
            {
                t.color = textColor ?? WoodText;
                t.fontStyle = FontStyle.Bold;
                t.rectTransform.offsetMin = new Vector2(22, 8); t.rectTransform.offsetMax = new Vector2(-22, -8);
                t.resizeTextForBestFit = true; t.resizeTextMinSize = 16; t.resizeTextMaxSize = t.fontSize;
            }
            return b;
        }

        /// <summary>Çerçeve görselinin içindeki bir piksel dikdörtgenine yerleştirir (frameSize: görselin piksel boyutu).</summary>
        public static RectTransform InFrame(RectTransform child, Vector2 frameSize, float x0, float y0, float x1, float y1)
        {
            child.anchorMin = new Vector2(x0 / frameSize.x, 1 - y1 / frameSize.y);
            child.anchorMax = new Vector2(x1 / frameSize.x, 1 - y0 / frameSize.y);
            child.offsetMin = child.offsetMax = Vector2.zero;
            return child;
        }

        /// <summary>Sekme rengi: tahta varsa beyaz/gri, yoksa verilen renkler.</summary>
        public static Color TabColor(bool active, Color activeColor, Color inactiveColor)
        {
            if (HasWood) return active ? Color.white : WoodInactive;
            return active ? activeColor : inactiveColor;
        }

        public static LayoutElement LE(Component c, float prefHeight = -1, float prefWidth = -1, float flexW = -1)
        {
            var le = c.gameObject.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            if (prefHeight >= 0) { le.preferredHeight = prefHeight; le.minHeight = prefHeight; le.flexibleHeight = 0; }
            if (prefWidth >= 0) { le.preferredWidth = prefWidth; le.minWidth = prefWidth; }
            if (flexW >= 0) le.flexibleWidth = flexW;
            return le;
        }

        /// <summary>Kaydırılabilir alan. İçerik otomatik büyür.</summary>
        public static ScrollRect Scroll(Transform parent, string name, bool vertical, bool horizontal, out RectTransform content)
        {
            var root = Rect(name, parent);
            var sr = root.gameObject.AddComponent<ScrollRect>();
            var bg = root.gameObject.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.001f); // dokunmayı yakalamak için
            root.gameObject.AddComponent<RectMask2D>();
            content = Rect("Content", root);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(horizontal ? 0 : 1, 1);
            content.pivot = new Vector2(0, 1);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = vertical ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            fit.horizontalFit = horizontal ? ContentSizeFitter.FitMode.PreferredSize : ContentSizeFitter.FitMode.Unconstrained;
            sr.content = content;
            sr.vertical = vertical;
            sr.horizontal = horizontal;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 30;
            return sr;
        }

        public static VerticalLayoutGroup VList(RectTransform rt, float spacing = 16, int pad = 24)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(pad, pad, pad, pad);
            v.childControlHeight = true; v.childControlWidth = true;
            v.childForceExpandHeight = false; v.childForceExpandWidth = true;
            return v;
        }

        public static HorizontalLayoutGroup HList(RectTransform rt, float spacing = 16, int pad = 0)
        {
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = new RectOffset(pad, pad, pad, pad);
            h.childControlHeight = true; h.childControlWidth = true;
            h.childForceExpandHeight = true; h.childForceExpandWidth = true;
            return h;
        }

        public static GridLayoutGroup Grid(RectTransform rt, Vector2 cell, Vector2 spacing, int pad = 24)
        {
            var g = rt.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = cell;
            g.spacing = spacing;
            g.padding = new RectOffset(pad, pad, pad, pad);
            return g;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }

        /// <summary>Karartılmış arka planlı açılır pencere. Kapatmak için döndürülen kökü yok et.</summary>
        // Yatay ekran için (referans 1920x1080) güvenli azami kart boyutu
        public const float MaxModalHeight = 940;

        public static RectTransform Modal(Transform overlay, string title, out RectTransform body, float height = 900, float width = 1300)
        {
            height = Mathf.Min(height, MaxModalHeight);
            var dim = Img(overlay, "Modal", Theme.Dim);
            Stretch(dim.rectTransform);
            var card = Panel(dim.transform, "Card", Theme.Panel);
            Place(card.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, height));
            var t = Txt(card.transform, "Title", title, 50, Theme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(t.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -55), new Vector2(width - 80, 90));
            body = Rect("Body", card.transform);
            Stretch(body, 40, 110, 40, 30);
            return dim.rectTransform;
        }
    }

    // UIKit'in ses sistemine bağımlı olmaması için küçük bir kanca
    public static class AudioFXHook
    {
        public static Action OnClick;
        public static void Click() { if (OnClick != null) OnClick(); }
    }
}
