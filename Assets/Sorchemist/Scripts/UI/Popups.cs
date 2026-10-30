// Kısa bildirim (toast), sonuç penceresi ve iksir seçici.
using System;
using System.Collections;
using Sorchemist.Core;
using Sorchemist.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Sorchemist.UI
{
    public static class Toaster
    {
        public static void Show(RectTransform overlay, string msg)
        {
            var p = UIKit.Panel(overlay, "Toast", new Color(0.1f, 0.08f, 0.18f, 0.95f));
            p.raycastTarget = false;
            UIKit.Place(p.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(1200, 140));
            var t = UIKit.Txt(p.transform, "T", msg, 34, Theme.Text);
            UIKit.Stretch(t.rectTransform, 24, 10, 24, 10);
            FX.I.StartCoroutine(Life(p));
        }

        static IEnumerator Life(Image p)
        {
            yield return new WaitForSeconds(2.8f);
            float t = 0;
            var cg = p.gameObject.AddComponent<CanvasGroup>();
            while (t < 0.4f && p != null) { t += Time.deltaTime; cg.alpha = 1 - t / 0.4f; yield return null; }
            if (p != null) UnityEngine.Object.Destroy(p.gameObject);
        }
    }

    public static class ResultPopup
    {
        public static Color RarityColor(FailRarity r)
        {
            switch (r)
            {
                case FailRarity.Rare: return UIKit.Hex("#4FA3FF");
                case FailRarity.Epic: return Theme.Magic;
                case FailRarity.Legendary: return Theme.Gold;
                default: return Theme.TextDim;
            }
        }

        public static string RarityName(FailRarity r)
        {
            switch (r)
            {
                case FailRarity.Rare: return Loc.T("Nadir", "Rare");
                case FailRarity.Epic: return Loc.T("Epik", "Epic");
                case FailRarity.Legendary: return Loc.T("Efsanevi", "Legendary");
                default: return Loc.T("Sıradan", "Common");
            }
        }

        /// <summary>Başarılı iksir veya Fail-Dex girdisi için paylaşılabilir sonuç kartı.
        /// iconKey: çerçevedeki yuvarlak pencerede gösterilecek görsel (fx_success, fx_fail, el_8...).</summary>
        public static void Show(string title, Color titleColor, string body, string science, string badge,
            string shareText, Action onClose, string iconKey = null)
        {
            var gm = GameManager.I;
            var frameArt = Art.Get("frame_dialog");
            if (frameArt == null) { ShowPlain(title, titleColor, body, science, badge, shareText, onClose); return; }

            var dim = UIKit.Img(gm.Overlay, "Result", Theme.Dim);
            UIKit.Stretch(dim.rectTransform);
            var fs = new Vector2(frameArt.rect.width, frameArt.rect.height);
            var card = Art.Image(dim.transform, "Card", frameArt);
            card.preserveAspect = false;
            float w = 1300, h = w * fs.y / fs.x;
            UIKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(110, 0), new Vector2(w, h));
            FX.I.Pop(card.transform, 0.08f);

            // Yuvarlak pencerede görsel
            var hole = UIKit.InFrame(UIKit.Rect("Hole", card.transform), fs, 158, 145, 372, 359);
            var icon = Art.Get(iconKey);
            if (icon != null && (iconKey.StartsWith("fx_") || iconKey.StartsWith("mystery")))
            {
                var ic = Art.Image(hole, "Icon", icon);
                UIKit.Stretch(ic.rectTransform, -30, -30, -30, -30);
                if (iconKey.StartsWith("fx_")) ic.gameObject.AddComponent<Spinner>();
                else ic.gameObject.AddComponent<Bouncer>().Amplitude = 10;
            }
            else if (icon != null)
            {
                // Portre: daire maskeli
                var m = UIKit.Img(hole, "Mask", UIKit.Hex("#C98A4A"), UIKit.Circle);
                UIKit.Stretch(m.rectTransform, 8, 8, 8, 8);
                m.gameObject.AddComponent<Mask>().showMaskGraphic = true;
                var ic = Art.Image(m.transform, "Icon", icon);
                UIKit.Stretch(ic.rectTransform);
                ic.preserveAspect = false;
            }

            var ttl = UIKit.Txt(UIKit.InFrame(UIKit.Rect("TitleArea", card.transform), fs, 440, 38, 795, 112), "Title", title, 34,
                UIKit.WoodText, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Stretch(ttl.rectTransform);
            ttl.resizeTextForBestFit = true; ttl.resizeTextMinSize = 20; ttl.resizeTextMaxSize = 34;
            ttl.horizontalOverflow = HorizontalWrapMode.Wrap; ttl.verticalOverflow = VerticalWrapMode.Truncate;

            var content = UIKit.InFrame(UIKit.Rect("TextArea", card.transform), fs, 440, 150, 1010, 612);
            var v = UIKit.VList(content, 12, 0); v.childForceExpandHeight = false;
            if (!string.IsNullOrEmpty(badge))
                UIKit.LE(UIKit.Txt(content, "Badge", badge, 30, Color.Lerp(titleColor, Color.black, 0.6f), TextAnchor.MiddleLeft, FontStyle.Bold), 44);
            UIKit.LE(UIKit.Txt(content, "Body", body, 30, UIKit.WoodText, TextAnchor.UpperLeft), 190);
            if (!string.IsNullOrEmpty(science))
            {
                var sp = UIKit.Panel(content, "Science", new Color(0.35f, 0.22f, 0.1f, 0.18f));
                UIKit.LE(sp, 160);
                var st = UIKit.Txt(sp.transform, "T", Loc.T("<b>Bilim köşesi:</b> ", "<b>Science corner:</b> ") + science, 26, UIKit.Hex("#2E5E4E"), TextAnchor.MiddleLeft);
                UIKit.Stretch(st.rectTransform, 18, 10, 18, 10);
            }
            UIKit.LE(UIKit.Txt(content, "Brand", "SORCHEMIST  •  @sorchemist", 22, UIKit.Hex("#8B6B4A"), TextAnchor.MiddleLeft, FontStyle.Bold), 30);

            var buttons = UIKit.InFrame(UIKit.Rect("Buttons", card.transform), fs, 95, 400, 380, 620);
            var bv = UIKit.VList(buttons, 14, 0); bv.childForceExpandHeight = false;
            UIKit.LE(UIKit.Wood(UIKit.Btn(buttons, "Share", Loc.T("Paylaş", "Share"), Color.white, () => gm.Share(shareText), 34), null, UIKit.Hex("#FFC2D6")), 90);
            UIKit.LE(UIKit.Wood(UIKit.Btn(buttons, "Ok", Loc.T("Tamam", "OK"), Color.white, () =>
            {
                UnityEngine.Object.Destroy(dim.gameObject);
                if (onClose != null) onClose();
            }, 34)), 90);
        }

        static void ShowPlain(string title, Color titleColor, string body, string science, string badge,
            string shareText, Action onClose)
        {
            var gm = GameManager.I;
            RectTransform content;
            var modal = UIKit.Modal(gm.Overlay, title, out content, 920, 1400);
            modal.Find("Card/Title").GetComponent<Text>().color = titleColor;
            FX.I.Pop(modal.Find("Card"), 0.08f);
            UIKit.VList(content, 20, 10);
            if (!string.IsNullOrEmpty(badge)) UIKit.LE(UIKit.Txt(content, "Badge", badge, 40, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold), 60);
            UIKit.LE(UIKit.Txt(content, "Body", body, 38, Theme.Text), 200);
            if (!string.IsNullOrEmpty(science))
            {
                var sp = UIKit.Panel(content, "Science", Theme.PanelLight);
                UIKit.LE(sp, 190);
                var st = UIKit.Txt(sp.transform, "T", Loc.T("<b>Bilim köşesi:</b>\n", "<b>Science corner:</b>\n") + science, 32, Theme.Accent);
                UIKit.Stretch(st.rectTransform, 24, 16, 24, 16);
            }
            var row = UIKit.Rect("Row", content);
            UIKit.LE(row, 120);
            UIKit.HList(row, 20);
            UIKit.Btn(row, "Share", Loc.T("Paylaş", "Share"), UIKit.Hex("#E1306C"), () => gm.Share(shareText));
            UIKit.Btn(row, "Ok", Loc.T("Tamam", "OK"), Theme.Magic, () =>
            {
                UnityEngine.Object.Destroy(modal.gameObject);
                if (onClose != null) onClose();
            });
        }
    }

    public static class PotionPicker
    {
        public static void Show(Action<string> onPick)
        {
            Show(Loc.T("Hangi iksiri vereceksin?", "Which potion will you give?"), null, true, onPick);
        }

        /// <summary>İksir seçici. onlyOwned=false ise bütün bileşikler listelenir (ör. takasta istenen iksir).</summary>
        public static void Show(string title, Func<CompoundData, bool> filter, bool onlyOwned, Action<string> onPick)
        {
            var gm = GameManager.I;
            RectTransform body;
            var modal = UIKit.Modal(gm.Overlay, title, out body, 900, 1200);
            RectTransform list;
            var sr = UIKit.Scroll(body, "Scroll", true, false, out list);
            UIKit.Stretch((RectTransform)sr.transform, 0, 0, 0, 130);
            UIKit.VList(list, 14, 6);

            bool any = false;
            foreach (var c in CompoundDatabase.All)
            {
                int have = gm.State.PotionCount(c.Id);
                if ((onlyOwned && have <= 0) || (filter != null && !filter(c))) continue;
                any = true;
                string id = c.Id;
                var b = UIKit.Btn(list, "P_" + id,
                    c.NameTr + " (" + c.Formula + ")  x" + have + Loc.T("\n<size=28><color=#A99FC9>Etki: ", "\n<size=28><color=#A99FC9>Effect: ") +
                    CompoundDatabase.EffectNameTr(c.Effect) + "</color></size>",
                    Theme.PanelLight, () => { UnityEngine.Object.Destroy(modal.gameObject); onPick(id); }, 36);
                UIKit.LE(b, 120);
                var dot = UIKit.Img(b.transform, "Dot", UIKit.Hex(c.ColorHex), UIKit.Circle);
                UIKit.Place(dot.rectTransform, new Vector2(0, 0.5f), new Vector2(50, 0), new Vector2(60, 60));
            }
            if (!any)
            {
                var t = UIKit.Txt(list, "Empty", Loc.T("Hiç iksirin yok!\nLaboratuvara gidip bir şeyler pişir.", "You have no potions!\nGo to the lab and brew something."), 38, Theme.TextDim);
                UIKit.LE(t, 300);
            }
            var close = UIKit.Btn(body, "Close", Loc.T("Vazgeç", "Cancel"), Theme.Danger, () => UnityEngine.Object.Destroy(modal.gameObject));
            UIKit.Place((RectTransform)close.transform, new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(460, 100));
        }
    }

    /// <summary>İki seçenekli onay penceresi (ör. "Buluttaki kaydı yükle / Bu cihazdakini kullan").</summary>
    public static class ChoiceDialog
    {
        public static void Show(string title, string body, string aLabel, Action a, string bLabel, Action b, Color? aColor = null)
        {
            var gm = GameManager.I;
            RectTransform card;
            var modal = UIKit.Modal(gm.Overlay, title, out card, 640, 1200);
            var t = UIKit.Txt(card, "Body", body, 34, Theme.Text, TextAnchor.UpperCenter);
            UIKit.Stretch(t.rectTransform, 20, 10, 20, 150);
            var row = UIKit.Rect("Buttons", card);
            row.anchorMin = Vector2.zero; row.anchorMax = new Vector2(1, 0); row.pivot = new Vector2(0.5f, 0);
            row.offsetMin = new Vector2(0, 10); row.offsetMax = new Vector2(0, 120);
            UIKit.HList(row, 20);
            UIKit.Wood(UIKit.Btn(row, "A", aLabel, aColor ?? Theme.Accent, () => { UnityEngine.Object.Destroy(modal.gameObject); if (a != null) a(); }, 32));
            UIKit.Wood(UIKit.Btn(row, "B", bLabel, Theme.PanelLight, () => { UnityEngine.Object.Destroy(modal.gameObject); if (b != null) b(); }, 32));
        }
    }
}
