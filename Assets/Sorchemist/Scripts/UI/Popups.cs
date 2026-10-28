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
            UIKit.Place(p.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 260), new Vector2(980, 170));
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
                case FailRarity.Rare: return "Nadir";
                case FailRarity.Epic: return "Epik";
                case FailRarity.Legendary: return "Efsanevi";
                default: return "Sıradan";
            }
        }

        /// <summary>Başarılı iksir veya Fail-Dex girdisi için paylaşılabilir sonuç kartı.</summary>
        public static void Show(string title, Color titleColor, string body, string science, string badge,
            string shareText, Action onClose)
        {
            var gm = GameManager.I;
            RectTransform content;
            var modal = UIKit.Modal(gm.Overlay, title, out content, 1350);
            modal.Find("Card/Title").GetComponent<Text>().color = titleColor;
            FX.I.Pop(modal.Find("Card"), 0.08f);

            UIKit.VList(content, 20, 10);
            if (!string.IsNullOrEmpty(badge))
            {
                var b = UIKit.Txt(content, "Badge", badge, 40, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.LE(b, 60);
            }
            var bt = UIKit.Txt(content, "Body", body, 38, Theme.Text);
            UIKit.LE(bt, 360);
            if (!string.IsNullOrEmpty(science))
            {
                var sp = UIKit.Panel(content, "Science", Theme.PanelLight);
                UIKit.LE(sp, 300);
                var st = UIKit.Txt(sp.transform, "T", "<b>Bilim köşesi:</b>\n" + science, 32, Theme.Accent);
                UIKit.Stretch(st.rectTransform, 24, 16, 24, 16);
            }
            var brand = UIKit.Txt(content, "Brand", "SORCHEMIST  •  @sorchemist", 28, Theme.TextDim, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.LE(brand, 40);

            var row = UIKit.Rect("Row", content);
            UIKit.LE(row, 140);
            UIKit.HList(row, 20);
            UIKit.Btn(row, "Share", "Paylaş", UIKit.Hex("#E1306C"), () => gm.Share(shareText));
            UIKit.Btn(row, "Ok", "Tamam", Theme.Magic, () =>
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
            var gm = GameManager.I;
            RectTransform body;
            var modal = UIKit.Modal(gm.Overlay, "Hangi iksiri vereceksin?", out body, 1400);
            RectTransform list;
            var sr = UIKit.Scroll(body, "Scroll", true, false, out list);
            UIKit.Stretch((RectTransform)sr.transform, 0, 0, 0, 150);
            UIKit.VList(list, 14, 6);

            bool any = false;
            foreach (var st in gm.State.Potions)
            {
                var c = CompoundDatabase.Get(st.Id);
                if (c == null || st.Count <= 0) continue;
                any = true;
                string id = st.Id;
                var b = UIKit.Btn(list, "P_" + id,
                    c.NameTr + " (" + c.Formula + ")  x" + st.Count + "\n<size=28><color=#A99FC9>Etki: " +
                    CompoundDatabase.EffectNameTr(c.Effect) + "</color></size>",
                    Theme.PanelLight, () => { UnityEngine.Object.Destroy(modal.gameObject); onPick(id); }, 36);
                UIKit.LE(b, 140);
                var dot = UIKit.Img(b.transform, "Dot", UIKit.Hex(c.ColorHex), UIKit.Circle);
                UIKit.Place(dot.rectTransform, new Vector2(0, 0.5f), new Vector2(50, 0), new Vector2(60, 60));
            }
            if (!any)
            {
                var t = UIKit.Txt(list, "Empty", "Hiç iksirin yok!\nLaboratuvara gidip bir şeyler pişir.", 38, Theme.TextDim);
                UIKit.LE(t, 300);
            }
            var close = UIKit.Btn(body, "Close", "Vazgeç", Theme.Danger, () => UnityEngine.Object.Destroy(modal.gameObject));
            UIKit.Place((RectTransform)close.transform, new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(500, 120));
        }
    }
}
