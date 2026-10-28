// Üst bilgi çubuğu (altın, seviye, XP, gün) ve alt gezinme menüsü.
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

        public void Build(RectTransform root, float top, float bottom)
        {
            // ----- Üst çubuk -----
            var bar = UIKit.Img(root, "TopBar", Theme.Panel);
            var brt = bar.rectTransform;
            brt.anchorMin = new Vector2(0, 1); brt.anchorMax = new Vector2(1, 1);
            brt.pivot = new Vector2(0.5f, 1); brt.sizeDelta = new Vector2(0, top); brt.anchoredPosition = Vector2.zero;

            _level = UIKit.Txt(bar.transform, "Level", "", 40, Theme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Place(_level.rectTransform, new Vector2(0, 0.5f), new Vector2(30, 18), new Vector2(260, 60), new Vector2(0, 0.5f));
            var xpBg = UIKit.Img(bar.transform, "XpBg", Theme.PanelLight);
            UIKit.Place(xpBg.rectTransform, new Vector2(0, 0.5f), new Vector2(30, -32), new Vector2(300, 22), new Vector2(0, 0.5f));
            _xpFill = UIKit.Img(xpBg.transform, "XpFill", Theme.Accent);
            _xpFill.rectTransform.anchorMin = Vector2.zero; _xpFill.rectTransform.anchorMax = new Vector2(0, 1);
            _xpFill.rectTransform.offsetMin = _xpFill.rectTransform.offsetMax = Vector2.zero;

            _gold = UIKit.Txt(bar.transform, "Gold", "", 42, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(_gold.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(40, 0), new Vector2(300, 80));
            _day = UIKit.Txt(bar.transform, "Day", "", 34, Theme.TextDim, TextAnchor.MiddleRight);
            UIKit.Place(_day.rectTransform, new Vector2(1, 0.5f), new Vector2(-150, 0), new Vector2(200, 60), new Vector2(1, 0.5f));

            var gear = UIKit.Btn(bar.transform, "Settings", "Ayar", Theme.PanelLight, OpenSettings, 30);
            UIKit.Place((RectTransform)gear.transform, new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(110, 90), new Vector2(1, 0.5f));

            // ----- Alt menü -----
            var nav = UIKit.Img(root, "BottomNav", Theme.Panel);
            var nrt = nav.rectTransform;
            nrt.anchorMin = Vector2.zero; nrt.anchorMax = new Vector2(1, 0);
            nrt.pivot = new Vector2(0.5f, 0); nrt.sizeDelta = new Vector2(0, bottom); nrt.anchoredPosition = Vector2.zero;
            var row = UIKit.Rect("Row", nav.transform);
            UIKit.Stretch(row, 16, 16, 16, 16);
            UIKit.HList(row, 14);
            AddTab(row, ScreenId.Village, "Köy");
            AddTab(row, ScreenId.Lab, "Laboratuvar");
            AddTab(row, ScreenId.FailDex, "Fail-Dex");
            AddTab(row, ScreenId.Skills, "Yetenekler");
            Refresh();
        }

        void AddTab(RectTransform row, ScreenId id, string label)
        {
            var b = UIKit.Btn(row, "Tab_" + id, label, Theme.PanelLight, () => GameManager.I.Show(id), 34);
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
            var modal = UIKit.Modal(gm.Overlay, "Ayarlar", out body, 1100);
            UIKit.VList(body, 24, 10);
            Button sound = null, haptic = null;
            sound = UIKit.Btn(body, "Sound", "", Theme.PanelLight, () =>
            {
                gm.State.SoundOn = !gm.State.SoundOn; AudioFX.I.Enabled = gm.State.SoundOn;
                UIKit.LabelOf(sound).text = "Ses: " + (gm.State.SoundOn ? "Açık" : "Kapalı"); gm.SaveNow();
            });
            UIKit.LabelOf(sound).text = "Ses: " + (gm.State.SoundOn ? "Açık" : "Kapalı");
            UIKit.LE(sound, 130);
            haptic = UIKit.Btn(body, "Haptic", "", Theme.PanelLight, () =>
            {
                gm.State.HapticsOn = !gm.State.HapticsOn; Haptics.Enabled = gm.State.HapticsOn;
                UIKit.LabelOf(haptic).text = "Titreşim: " + (gm.State.HapticsOn ? "Açık" : "Kapalı"); gm.SaveNow();
            });
            UIKit.LabelOf(haptic).text = "Titreşim: " + (gm.State.HapticsOn ? "Açık" : "Kapalı");
            UIKit.LE(haptic, 130);

            var about = UIKit.Txt(body, "About",
                "Sorchemist v0.1 (prototip)\nAqualf ve Fındıkdere köyü\n@sorchemist\n\nKimya gerçek, büyü biraz abartılı.", 32, Theme.TextDim);
            UIKit.LE(about, 260);

            Button reset = null;
            bool armed = false;
            reset = UIKit.Btn(body, "Reset", "Kaydı Sıfırla", Theme.Danger, () =>
            {
                if (!armed) { armed = true; UIKit.LabelOf(reset).text = "Emin misin? Tekrar dokun"; return; }
                gm.ResetGame();
            });
            UIKit.LE(reset, 120);
            var close = UIKit.Btn(body, "Close", "Kapat", Theme.Magic, () => Destroy(modal.gameObject));
            UIKit.LE(close, 130);
        }
    }
}
