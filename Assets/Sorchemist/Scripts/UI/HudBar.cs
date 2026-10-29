// Üst bilgi çubuğu (altın, seviye, XP, gün) ve sol gezinme menüsü (yatay ekran).
using System.Collections.Generic;
using Sorchemist.Core;
using Sorchemist.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Sorchemist.UI
{
    public class HudBar : MonoBehaviour
    {
        Text _gold, _level, _day;
        Image _xpFill;
        readonly Dictionary<ScreenId, Image> _tabs = new Dictionary<ScreenId, Image>();

        public void Build(RectTransform root, float top, float rail)
        {
            // ----- Sol menü (yatay ekran) -----
            var nav = UIKit.Img(root, "SideRail", Theme.Panel);
            var nrt = nav.rectTransform;
            nrt.anchorMin = Vector2.zero; nrt.anchorMax = new Vector2(0, 1);
            nrt.pivot = new Vector2(0, 0.5f); nrt.sizeDelta = new Vector2(rail, 0); nrt.anchoredPosition = Vector2.zero;

            var logo = UIKit.Txt(nav.transform, "Logo", "SORCHEMIST", 28, Theme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(logo.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -top / 2), new Vector2(rail, top));

            var col = UIKit.Rect("Column", nav.transform);
            UIKit.Stretch(col, 14, top + 10, 14, 14);
            var v = UIKit.VList(col, 14, 0);
            v.childForceExpandHeight = true;
            AddTab(col, ScreenId.Village, "Köy");
            AddTab(col, ScreenId.Lab, "Laboratuvar");
            AddTab(col, ScreenId.FailDex, "Fail-Dex");
            AddTab(col, ScreenId.Skills, "Yetenek");

            // ----- Üst çubuk -----
            var bar = UIKit.Img(root, "TopBar", Theme.Panel);
            var brt = bar.rectTransform;
            brt.anchorMin = new Vector2(0, 1); brt.anchorMax = new Vector2(1, 1);
            brt.pivot = new Vector2(0.5f, 1); brt.offsetMin = new Vector2(rail, -top); brt.offsetMax = Vector2.zero;

            _level = UIKit.Txt(bar.transform, "Level", "", 38, Theme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Place(_level.rectTransform, new Vector2(0, 0.5f), new Vector2(30, 16), new Vector2(360, 56), new Vector2(0, 0.5f));
            var xpBg = UIKit.Img(bar.transform, "XpBg", Theme.PanelLight);
            UIKit.Place(xpBg.rectTransform, new Vector2(0, 0.5f), new Vector2(30, -28), new Vector2(340, 20), new Vector2(0, 0.5f));
            _xpFill = UIKit.Img(xpBg.transform, "XpFill", Theme.Accent);
            _xpFill.rectTransform.anchorMin = Vector2.zero; _xpFill.rectTransform.anchorMax = new Vector2(0, 1);
            _xpFill.rectTransform.offsetMin = _xpFill.rectTransform.offsetMax = Vector2.zero;

            _gold = UIKit.Txt(bar.transform, "Gold", "", 42, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(_gold.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360, 80));
            _day = UIKit.Txt(bar.transform, "Day", "", 34, Theme.TextDim, TextAnchor.MiddleRight);
            UIKit.Place(_day.rectTransform, new Vector2(1, 0.5f), new Vector2(-170, 0), new Vector2(220, 60), new Vector2(1, 0.5f));

            var gear = UIKit.Btn(bar.transform, "Settings", "Ayar", Theme.PanelLight, OpenSettings, 30);
            UIKit.Place((RectTransform)gear.transform, new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(120, 80), new Vector2(1, 0.5f));
            Refresh();
        }

        void AddTab(RectTransform row, ScreenId id, string label)
        {
            var b = UIKit.Btn(row, "Tab_" + id, label, Theme.PanelLight, () => GameManager.I.Show(id), 30);
            _tabs[id] = b.GetComponent<Image>();
        }

        public void SetActiveTab(ScreenId id)
        {
            foreach (var kv in _tabs) kv.Value.color = kv.Key == id ? Theme.Magic : Theme.PanelLight;
        }

        public void Refresh()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Session == null || _gold == null) return;
            var s = gm.State;
            _gold.text = s.Gold + " altın";
            _level.text = "Seviye " + s.Level + (s.SkillPoints > 0 ? "  <color=#FFD166>(+" + s.SkillPoints + ")</color>" : "");
            _day.text = "Gün " + s.Day;
            _xpFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01((float)s.Xp / PlayerState.XpToNext(s.Level)), 1);
        }

        void OpenSettings()
        {
            var gm = GameManager.I;
            RectTransform body;
            var modal = UIKit.Modal(gm.Overlay, "Ayarlar", out body, 900, 1000);
            UIKit.VList(body, 24, 10);
            Button sound = null, haptic = null;
            sound = UIKit.Btn(body, "Sound", "", Theme.PanelLight, () =>
            {
                gm.State.SoundOn = !gm.State.SoundOn; AudioFX.I.Enabled = gm.State.SoundOn;
                UIKit.LabelOf(sound).text = "Ses: " + (gm.State.SoundOn ? "Açık" : "Kapalı"); gm.SaveNow();
            });
            UIKit.LabelOf(sound).text = "Ses: " + (gm.State.SoundOn ? "Açık" : "Kapalı");
            UIKit.LE(sound, 100);
            haptic = UIKit.Btn(body, "Haptic", "", Theme.PanelLight, () =>
            {
                gm.State.HapticsOn = !gm.State.HapticsOn; Haptics.Enabled = gm.State.HapticsOn;
                UIKit.LabelOf(haptic).text = "Titreşim: " + (gm.State.HapticsOn ? "Açık" : "Kapalı"); gm.SaveNow();
            });
            UIKit.LabelOf(haptic).text = "Titreşim: " + (gm.State.HapticsOn ? "Açık" : "Kapalı");
            UIKit.LE(haptic, 100);

            var about = UIKit.Txt(body, "About",
                "Sorchemist v0.1 (prototip)\nAqualf ve Fındıkdere köyü\n@sorchemist\n\nKimya gerçek, büyü biraz abartılı.", 32, Theme.TextDim);
            UIKit.LE(about, 200);

            Button reset = null;
            bool armed = false;
            reset = UIKit.Btn(body, "Reset", "Kaydı Sıfırla", Theme.Danger, () =>
            {
                if (!armed) { armed = true; UIKit.LabelOf(reset).text = "Emin misin? Tekrar dokun"; return; }
                gm.ResetGame();
            });
            UIKit.LE(reset, 100);
            var close = UIKit.Btn(body, "Close", "Kapat", Theme.Magic, () => Destroy(modal.gameObject));
            UIKit.LE(close, 100);
        }
    }
}
