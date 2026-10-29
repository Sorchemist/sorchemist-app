// Bilim ve Büyü yetenek ağacı.
using Sorchemist.Core;
using Sorchemist.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Sorchemist.UI
{
    public class SkillScreen : GameScreen
    {
        RectTransform _sci, _mag;
        Text _header;

        public override void Build()
        {
            _header = UIKit.Txt(Root, "Header", "", 44, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(_header.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(1000, 120));

            var cols = UIKit.Rect("Columns", Root);
            UIKit.Stretch(cols, 16, 150, 16, 10);
            UIKit.HList(cols, 16);
            _sci = Column(cols, "BİLİM", Theme.Accent);
            _mag = Column(cols, "BÜYÜ", Theme.Magic);
        }

        RectTransform Column(RectTransform parent, string title, Color color)
        {
            var col = UIKit.Panel(parent, title, Theme.Panel);
            var t = UIKit.Txt(col.transform, "Title", title, 44, color, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(t.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(480, 80));
            RectTransform content;
            var sr = UIKit.Scroll(col.transform, "Scroll", true, false, out content);
            UIKit.Stretch((RectTransform)sr.transform, 0, 100, 0, 0);
            UIKit.VList(content, 14, 14);
            return content;
        }

        public override void Refresh()
        {
            _header.text = "Yetenek puanı: " + State.SkillPoints +
                           "\n<size=28><color=#A99FC9>Her seviye atlayışında 1 puan kazanırsın.</color></size>";
            UIKit.Clear(_sci); UIKit.Clear(_mag);
            foreach (var s in SkillDatabase.All)
            {
                var skill = s;
                bool has = State.HasSkill(s.Id);
                string why = State.CanUnlock(s.Id);
                bool can = why == null;
                Color bg = has ? (s.Branch == SkillBranch.Science ? Theme.Accent * 0.55f + Color.black * 0.45f : Theme.Magic * 0.55f + Color.black * 0.45f)
                                : can ? Theme.PanelLight : UIKit.Hex("#1F1936");
                bool parch = Art.Has("card_parchment");
                string status = has ? "<color=" + (parch ? "#1E6B3A" : "#7CF2C8") + ">AÇIK</color>"
                    : can ? "<color=" + (parch ? "#9A5B00" : "#FFD166") + ">Aç: " + s.Cost + " puan</color>"
                    : "<color=" + (parch ? "#6B5A4A" : "#A99FC9") + ">" + why + "</color>";
                var b = UIKit.Btn(s.Branch == SkillBranch.Science ? _sci : _mag, s.Id,
                    "<b>" + s.NameTr + "</b>\n<size=24>" + s.DescriptionTr + "</size>\n<size=26>" + status + "</size>", bg, () =>
                    {
                        if (Session.UnlockSkill(skill.Id))
                        {
                            AudioFX.Play(Sfx.LevelUp, 0.8f);
                            FX.I.Burst(G.Overlay, skill.Branch == SkillBranch.Science ? Theme.Accent : Theme.Magic, Vector2.zero, 40, 900);
                            Refresh();
                        }
                        else if (!State.HasSkill(skill.Id)) G.Toast(State.CanUnlock(skill.Id));
                    }, 32);
                if (parch)
                {
                    var bi = b.GetComponent<Image>();
                    bi.sprite = Art.Sliced("card_parchment", new Vector4(50, 50, 50, 50)); bi.type = Image.Type.Sliced;
                    bi.pixelsPerUnitMultiplier = 1.2f;
                    bi.color = has ? Color.white : can ? new Color(0.92f, 0.92f, 0.85f) : new Color(0.6f, 0.58f, 0.55f);
                    UIKit.LabelOf(b).color = UIKit.WoodText;
                    if (!has && !can && Art.Has("icon_lock"))
                    {
                        var lk = Art.Image(b.transform, "Lock", Art.Get("icon_lock"));
                        UIKit.Place(lk.rectTransform, new Vector2(1, 1), new Vector2(-30, -30), new Vector2(46, 58));
                        lk.raycastTarget = false;
                    }
                }
                UIKit.LE(b, 250);
            }
        }
    }
}
