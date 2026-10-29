// Açılış / yükleme ekranı (Art: bg_title = Loading4.jpg).
// Görseldeki "OYNA" ve "AYARLAR" tabelalarının üstüne görünmez butonlar koyar,
// yükleme çubuğunu canlı olarak doldurur.
using System;
using System.Collections;
using Sorchemist.Game;
using Sorchemist.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Sorchemist.UI
{
    public class TitleScreen : MonoBehaviour
    {
        // Görsel üzerindeki konumlar (0..1, sol-alt köken), Loading4.jpg 1400x781 ölçeğinden
        static readonly Rect PlayBtn = R(812, 537, 1030, 610);
        static readonly Rect SettingsBtn = R(1085, 537, 1300, 610);
        static readonly Rect Bar = R(457, 694, 940, 736);
        static readonly Rect BarLabel = R(560, 627, 838, 680);

        static Rect R(float x0, float y0, float x1, float y1)
        {
            const float W = 1400, H = 781;
            return Rect.MinMaxRect(x0 / W, 1 - y1 / H, x1 / W, 1 - y0 / H);
        }

        Image _fill;
        Text _label;
        Button _play;
        Action _onPlay;
        bool _ready;

        public static bool Show(RectTransform overlay, Action onPlay)
        {
            var art = Loc.En ? Art.First("bg_title_en", "bg_title") : Art.Get("bg_title");   // İngilizce yazılı kopya
            if (art == null) return false;
            var root = UIKit.Img(overlay, "Title", Color.black);
            UIKit.Stretch(root.rectTransform);
            root.gameObject.AddComponent<RectMask2D>();
            var t = root.gameObject.AddComponent<TitleScreen>();
            t._onPlay = onPlay;
            t.Build(root.rectTransform, art);
            return true;
        }

        static RectTransform Area(RectTransform parent, string name, Rect r)
        {
            var rt = UIKit.Rect(name, parent);
            rt.anchorMin = r.min; rt.anchorMax = r.max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        void Build(RectTransform root, Sprite art)
        {
            // Arkada: ekranı dolduran karartılmış kopya (geniş ekranlarda kenarlar boş kalmasın)
            var back = Art.Image(root, "ArtBack", art, new Color(0.45f, 0.45f, 0.5f, 1f));
            back.preserveAspect = false;
            var bfit = back.gameObject.AddComponent<AspectRatioFitter>();
            bfit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            bfit.aspectRatio = art.rect.width / art.rect.height;
            back.rectTransform.localScale = Vector3.one * 1.08f;
            // Önde: görselin tamamı (yazı, butonlar ve çubuk kesilmesin)
            var bg = Art.Image(root, "Art", art);
            bg.preserveAspect = false;
            var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fit.aspectRatio = art.rect.width / art.rect.height;
            var img = bg.rectTransform;

            // Basılı yükleme çubuğunu kendi canlı çubuğumuzla örtüyoruz
            var barBg = UIKit.Img(Area(img, "BarBg", Bar), "Bg", UIKit.Hex("#1C1230"));
            UIKit.Stretch(barBg.rectTransform, 6, 4, 6, 4);
            _fill = UIKit.Img(barBg.transform, "Fill", UIKit.Hex("#6FE3F0"));
            _fill.rectTransform.anchorMin = Vector2.zero; _fill.rectTransform.anchorMax = new Vector2(0, 1);
            _fill.rectTransform.offsetMin = _fill.rectTransform.offsetMax = Vector2.zero;
            for (int i = 0; i < 6; i++)
            {
                var bub = UIKit.Img(_fill.transform, "Bubble", new Color(1, 1, 1, 0.5f), UIKit.Circle);
                UIKit.Place(bub.rectTransform, new Vector2(0.15f + i * 0.15f, 0.5f), Vector2.zero, new Vector2(14, 14));
            }

            // Loc.T("YÜKLENİYOR...", "LOADING...") yazısının üstüne durum metni
            var labelBg = UIKit.Img(Area(img, "LabelBg", BarLabel), "Bg", UIKit.Hex("#E9D2A6"));
            UIKit.Stretch(labelBg.rectTransform, 10, 6, 10, 6);
            _label = UIKit.Txt(labelBg.transform, "Label", Loc.T("YÜKLENİYOR...", "LOADING..."), 30, UIKit.WoodText, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Stretch(_label.rectTransform);

            _play = Area(img, "Play", PlayBtn).gameObject.AddComponent<Button>();
            var pImg = _play.gameObject.AddComponent<Image>(); pImg.color = new Color(1, 1, 1, 0.001f);
            _play.targetGraphic = pImg;
            _play.onClick.AddListener(Play);

            var set = Area(img, "Settings", SettingsBtn).gameObject.AddComponent<Button>();
            var sImg = set.gameObject.AddComponent<Image>(); sImg.color = new Color(1, 1, 1, 0.001f);
            set.targetGraphic = sImg;
            set.onClick.AddListener(() => { AudioFX.Play(Sfx.Click); HudBar.OpenSettings(); });

            StartCoroutine(Load());
        }

        IEnumerator Load()
        {
            float t = 0;
            while (t < 1.6f)
            {
                t += Time.deltaTime;
                _fill.rectTransform.anchorMax = new Vector2(Mathf.SmoothStep(0, 1, t / 1.6f), 1);
                yield return null;
            }
            _ready = true;
            _label.text = Loc.T("HAZIR!", "READY!");
            AudioFX.Play(Sfx.Chime, 0.6f);
            if (FX.I != null) FX.I.Pop(_play.transform, 0.15f);
            while (this != null)
            {
                _label.color = Color.Lerp(UIKit.WoodText, Theme.Magic, (Mathf.Sin(Time.time * 4) + 1) / 2 * 0.6f);
                yield return null;
            }
        }

        void Play()
        {
            if (!_ready) return;
            AudioFX.Play(Sfx.Bubble);
            StartCoroutine(FadeOut());
        }

        IEnumerator FadeOut()
        {
            var cg = gameObject.AddComponent<CanvasGroup>();
            float t = 0;
            while (t < 0.4f) { t += Time.deltaTime; cg.alpha = 1 - t / 0.4f; yield return null; }
            var cb = _onPlay;
            Destroy(gameObject);
            if (cb != null) cb();
        }
    }
}
