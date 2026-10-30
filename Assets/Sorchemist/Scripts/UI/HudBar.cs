// Üst bilgi çubuğu (altın, seviye, XP, Bilgi, gün) ve sol gezinme menüsü (yatay ekran).
// Sol menü, sol üstteki üç çizgili (hamburger) butonla açılıp kapanır.
using System.Collections;
using System.Collections.Generic;
using Sorchemist.Core;
using Sorchemist.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Sorchemist.UI
{
    public class HudBar : MonoBehaviour
    {
        Text _gold, _level, _day, _points;
        bool _dayNumberOnly;
        Image _xpFill;
        readonly Dictionary<ScreenId, Image> _tabs = new Dictionary<ScreenId, Image>();
        RectTransform _rail, _bar, _burger;
        float _top, _railW;
        bool _open = true;
        Coroutine _anim;
        const float BurgerW = 110;

        public void Build(RectTransform root, float top, float rail)
        {
            _top = top; _railW = rail;
            // ----- Sol menü (yatay ekran) -----
            var nav = UIKit.Img(root, "SideRail", Theme.Panel);
            var nrt = nav.rectTransform;
            nrt.anchorMin = Vector2.zero; nrt.anchorMax = new Vector2(0, 1);
            nrt.pivot = new Vector2(0, 0.5f); nrt.sizeDelta = new Vector2(rail, 0); nrt.anchoredPosition = Vector2.zero;

            _rail = nrt;
            var logo = UIKit.Txt(nav.transform, "Logo", "SORCHEMIST", 24, Theme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(logo.rectTransform, new Vector2(0, 1), new Vector2(BurgerW - 8, -top / 2), new Vector2(rail - BurgerW + 4, top), new Vector2(0, 0.5f));
            logo.resizeTextForBestFit = true; logo.resizeTextMinSize = 12; logo.resizeTextMaxSize = 24;

            var col = UIKit.Rect("Column", nav.transform);
            UIKit.Stretch(col, 14, top + 10, 14, 14);
            var v = UIKit.VList(col, 14, 0);
            v.childForceExpandHeight = true;
            AddTab(col, ScreenId.Village, Loc.T("Köy", "Village"));
            AddTab(col, ScreenId.Lab, Loc.T("Laboratuvar", "Laboratory"));
            AddTab(col, ScreenId.FailDex, "Fail-Dex");
            AddTab(col, ScreenId.Skills, Loc.T("Pasifler", "Passives"));
            AddTab(col, ScreenId.Social, Loc.T("Topluluk", "Community"));

            // ----- Üst çubuk -----
            var bar = UIKit.Img(root, "TopBar", new Color(0.08f, 0.06f, 0.14f, 0.92f));
            var brt = bar.rectTransform;
            brt.anchorMin = new Vector2(0, 1); brt.anchorMax = new Vector2(1, 1);
            brt.pivot = new Vector2(0.5f, 1); brt.offsetMin = new Vector2(rail, -top); brt.offsetMax = Vector2.zero;
            _bar = brt;

            var lvlArt = Art.Get("hud_level");
            var goldArt = Art.Get("hud_gold");
            var dayArt = Art.Get("hud_day");
            if (lvlArt != null && goldArt != null && dayArt != null)
            {
                // Taş/tahta çerçeveler (UI.jpg)
                float fh = top - 6;
                var lv = Art.Image(bar.transform, "LevelFrame", lvlArt);
                var lvSize = new Vector2(lvlArt.rect.width, lvlArt.rect.height);
                UIKit.Place(lv.rectTransform, new Vector2(0, 0.5f), new Vector2(16, 0), new Vector2(fh * lvSize.x / lvSize.y, fh), new Vector2(0, 0.5f));
                _level = UIKit.Txt(lv.transform, "Level", "", 30, Theme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.InFrame(_level.rectTransform, lvSize, 45, 35, 300, 145);
                _points = UIKit.Txt(lv.transform, "Points", "", 24, Theme.Gold, TextAnchor.MiddleLeft, FontStyle.Bold);
                UIKit.InFrame(_points.rectTransform, lvSize, 365, 35, 800, 112);
                var xpBg = UIKit.Img(lv.transform, "XpBg", UIKit.Hex("#1E1430"));
                UIKit.InFrame(xpBg.rectTransform, lvSize, 356, 114, 806, 146);
                _xpFill = UIKit.Img(xpBg.transform, "XpFill", Theme.Accent);

                var gd = Art.Image(bar.transform, "GoldFrame", goldArt);
                var gdSize = new Vector2(goldArt.rect.width, goldArt.rect.height);
                UIKit.Place(gd.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(fh * gdSize.x / gdSize.y, fh));
                _gold = UIKit.Txt(gd.transform, "Gold", "", 36, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.InFrame(_gold.rectTransform, gdSize, 170, 45, 530, 150);

                var dy = Art.Image(bar.transform, "DayFrame", dayArt);
                var dySize = new Vector2(dayArt.rect.width, dayArt.rect.height);
                UIKit.Place(dy.rectTransform, new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(fh * dySize.x / dySize.y, fh), new Vector2(1, 0.5f));
                _day = UIKit.Txt(dy.transform, "Day", "", 34, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.InFrame(_day.rectTransform, dySize, 285, 40, 435, 135);
                _dayNumberOnly = true;
                if (Loc.En)
                {
                    // Görseldeki "Gün" yazısının üstüne İngilizce etiket
                    var patch = UIKit.Img(dy.transform, "DayLabelPatch", UIKit.Hex("#40372E"));
                    UIKit.InFrame(patch.rectTransform, dySize, 140, 56, 272, 120);
                    patch.raycastTarget = false;
                    var dl = UIKit.Txt(patch.transform, "DayLabel", "Day", 30, UIKit.Hex("#C9B49F"), TextAnchor.MiddleCenter, FontStyle.Bold);
                    UIKit.Stretch(dl.rectTransform);
                }
            }
            else
            {
                _level = UIKit.Txt(bar.transform, "Level", "", 38, Theme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                UIKit.Place(_level.rectTransform, new Vector2(0, 0.5f), new Vector2(30, 16), new Vector2(360, 56), new Vector2(0, 0.5f));
                var xpBg = UIKit.Img(bar.transform, "XpBg", Theme.PanelLight);
                UIKit.Place(xpBg.rectTransform, new Vector2(0, 0.5f), new Vector2(30, -28), new Vector2(340, 20), new Vector2(0, 0.5f));
                _xpFill = UIKit.Img(xpBg.transform, "XpFill", Theme.Accent);
                _gold = UIKit.Txt(bar.transform, "Gold", "", 42, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Place(_gold.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360, 80));
                _day = UIKit.Txt(bar.transform, "Day", "", 34, Theme.TextDim, TextAnchor.MiddleRight);
                UIKit.Place(_day.rectTransform, new Vector2(1, 0.5f), new Vector2(-24, 0), new Vector2(220, 60), new Vector2(1, 0.5f));
            }
            _xpFill.rectTransform.anchorMin = Vector2.zero; _xpFill.rectTransform.anchorMax = new Vector2(0, 1);
            _xpFill.rectTransform.offsetMin = _xpFill.rectTransform.offsetMax = Vector2.zero;


            BuildBurger(root);
            Refresh();
        }

        // ----- Üç çizgili menü butonu -----
        void BuildBurger(RectTransform root)
        {
            var bg = UIKit.Img(root, "MenuButton", new Color(0.08f, 0.06f, 0.14f, 0.96f));
            _burger = bg.rectTransform;
            UIKit.Place(_burger, new Vector2(0, 1), new Vector2(0, 0), new Vector2(BurgerW, _top), new Vector2(0, 1));
            var btn = bg.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => { AudioFX.Play(Sfx.Click, 0.6f); SetMenuOpen(!_open); });
            for (int i = 0; i < 3; i++)
            {
                var line = UIKit.Img(bg.transform, "Line" + i, Theme.Accent);
                line.raycastTarget = false;
                UIKit.Place(line.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, (1 - i) * 22), new Vector2(58, 9));
            }
        }

        public bool MenuOpen { get { return _open; } }

        public void SetMenuOpen(bool open)
        {
            _open = open;
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(Slide(open ? 1f : 0f));
        }

        IEnumerator Slide(float target)
        {
            // 0 = kapalı, 1 = açık
            float cur = Mathf.InverseLerp(-_railW, 0, _rail.anchoredPosition.x);
            if (target > 0.5f) _rail.gameObject.SetActive(true);
            while (!Mathf.Approximately(cur, target))
            {
                cur = Mathf.MoveTowards(cur, target, Time.unscaledDeltaTime * 5f);
                ApplyMenu(Mathf.SmoothStep(0, 1, cur));
                yield return null;
            }
            if (target < 0.5f) _rail.gameObject.SetActive(false);
            _anim = null;
        }

        void ApplyMenu(float t)
        {
            _rail.anchoredPosition = new Vector2(Mathf.Lerp(-_railW, 0, t), 0);
            float left = Mathf.Lerp(BurgerW, _railW, t);
            _bar.offsetMin = new Vector2(left, -_top);
            var content = GameManager.I != null ? GameManager.I.Content : null;
            if (content != null) content.offsetMin = new Vector2(Mathf.Lerp(0, _railW, t), content.offsetMin.y);
        }

        void AddTab(RectTransform row, ScreenId id, string label)
        {
            var b = UIKit.Btn(row, "Tab_" + id, label, Theme.PanelLight, () => GameManager.I.Show(id), 23);
            UIKit.Wood(b);
            _tabs[id] = b.GetComponent<Image>();
        }

        public void SetActiveTab(ScreenId id)
        {
            foreach (var kv in _tabs) kv.Value.color = UIKit.TabColor(kv.Key == id, Theme.Magic, Theme.PanelLight);
        }

        public void Refresh()
        {
            var gm = GameManager.I;
            if (gm == null || gm.Session == null || _gold == null) return;
            var s = gm.State;
            _gold.text = _dayNumberOnly ? s.Gold.ToString() : s.Gold + Loc.T(" altın", " gold");
            if (_points != null)
            {
                _level.text = Loc.T("Sv. ", "Lv. ") + s.Level;
                _points.text = "<color=#9EE6FF>" + s.Knowledge + Loc.T(" Bilgi</color>  ", " Knowledge</color>  ") + s.Xp + "/" + PlayerState.XpToNext(s.Level) + " XP";
                _points.resizeTextForBestFit = true; _points.resizeTextMinSize = 12; _points.resizeTextMaxSize = 24;
            }
            else
                _level.text = Loc.T("Seviye ", "Level ") + s.Level + Loc.T("  <color=#9EE6FF>", "  <color=#9EE6FF>") + s.Knowledge + Loc.T(" Bilgi</color>", " Knowledge</color>");
            _day.text = _dayNumberOnly ? s.Day.ToString() : Loc.T("Gün ", "Day ") + s.Day;
            _xpFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01((float)s.Xp / PlayerState.XpToNext(s.Level)), 1);
        }

        public static void OpenSettings()
        {
            var gm = GameManager.I;
            RectTransform body;
            var modal = UIKit.Modal(gm.Overlay, Loc.T("Ayarlar", "Settings"), out body, 940, 1100);
            UIKit.VList(body, 14, 4);

            // ----- Dil seçimi -----
            var langTitle = UIKit.Txt(body, "LangTitle", "<b>Dil / Language</b>", 32, Theme.Accent);
            UIKit.LE(langTitle, 44);
            var langRow = UIKit.Rect("LangRow", body);
            UIKit.LE(langRow, 96);
            UIKit.HList(langRow, 18);
            LangButton(langRow, "Türkçe", Lang.Tr, modal);
            LangButton(langRow, "English", Lang.En, modal);

            // ----- Hesap (bulut kaydı, arkadaşlar) -----
            var cloud = CloudService.I;
            string accLabel = cloud != null && cloud.LoggedIn
                ? Loc.T("Hesap: ", "Account: ") + cloud.UserName
                : Loc.T("Giriş Yap / Hesap Aç", "Log In / Sign Up");
            var acc = UIKit.Btn(body, "Account", accLabel, Theme.Magic, () => { Destroy(modal.gameObject); AccountPanel.Show(); }, 32, UIKit.Hex("#2A1640"));
            UIKit.LE(acc, 86);

            Button sound = null, haptic = null;
            sound = UIKit.Btn(body, "Sound", "", Theme.PanelLight, () =>
            {
                gm.State.SoundOn = !gm.State.SoundOn; AudioFX.I.Enabled = gm.State.SoundOn;
                UIKit.LabelOf(sound).text = SoundLabel(gm.State.SoundOn); gm.SaveNow();
            });
            UIKit.LabelOf(sound).text = SoundLabel(gm.State.SoundOn);
            UIKit.LE(sound, 78);
            haptic = UIKit.Btn(body, "Haptic", "", Theme.PanelLight, () =>
            {
                gm.State.HapticsOn = !gm.State.HapticsOn; Haptics.Enabled = gm.State.HapticsOn;
                UIKit.LabelOf(haptic).text = HapticLabel(gm.State.HapticsOn); gm.SaveNow();
            });
            UIKit.LabelOf(haptic).text = HapticLabel(gm.State.HapticsOn);
            UIKit.LE(haptic, 78);

            var about = UIKit.Txt(body, "About", Loc.T(
                "Sorchemist v0.2 (prototip) · Aqualf ve Fındıkdere köyü · @sorchemist\nKimya gerçek, büyü biraz abartılı.",
                "Sorchemist v0.2 (prototype) · Aqualf and Hazelbrook village · @sorchemist\nThe chemistry is real, the magic a little exaggerated."), 26, Theme.TextDim);
            UIKit.LE(about, 70);

            Button reset = null;
            bool armed = false;
            reset = UIKit.Btn(body, "Reset", Loc.T("Kaydı Sıfırla", "Reset Save"), Theme.Danger, () =>
            {
                if (!armed) { armed = true; UIKit.LabelOf(reset).text = Loc.T("Emin misin? Tekrar dokun", "Are you sure? Tap again"); return; }
                gm.ResetGame();
            });
            UIKit.LE(reset, 78);
            var close = UIKit.Btn(body, "Close", Loc.T("Kapat", "Close"), Theme.Magic, () => Destroy(modal.gameObject), 40, UIKit.Hex("#2A1640"));
            UIKit.LE(close, 78);
        }

        static string OnOff(bool on) { return on ? Loc.T("Açık", "On") : Loc.T("Kapalı", "Off"); }
        static string SoundLabel(bool on) { return Loc.T("Ses: ", "Sound: ") + OnOff(on); }
        static string HapticLabel(bool on) { return Loc.T("Titreşim: ", "Vibration: ") + OnOff(on); }

        static void LangButton(RectTransform row, string label, Lang lang, RectTransform modal)
        {
            bool active = Loc.Current == lang;
            var b = UIKit.Btn(row, "Lang_" + lang, (active ? "● " : "") + label, active ? Theme.Accent * 0.7f + Color.black * 0.3f : Theme.PanelLight, () =>
            {
                if (Loc.Current == lang) return;
                AudioFX.Play(Sfx.Chime, 0.6f);
                Destroy(modal.gameObject);
                GameManager.I.SetLanguage(lang);
            }, 34);
            UIKit.LE(b, -1, -1, 1);
        }
    }
}
