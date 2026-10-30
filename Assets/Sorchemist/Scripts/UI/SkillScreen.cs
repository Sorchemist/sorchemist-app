// Pasif özellikler ekranı (eski "Yetenek" sekmesi).
// Bilgi Puanı ile pasif açılır, Simya Kulesi'nin seviyesine göre açılan yuvalara takılır.
// Yalnızca yuvadaki pasifler etki eder.
using Sorchemist.Core;
using Sorchemist.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Sorchemist.UI
{
    public class SkillScreen : GameScreen
    {
        const int MaxSlots = 5;
        Text _header;
        RectTransform _slots, _list;

        public override void Build()
        {
            _header = UIKit.Txt(Root, "Header", "", 40, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(_header.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -55), new Vector2(1500, 100));

            _slots = UIKit.Rect("Slots", Root);
            _slots.anchorMin = new Vector2(0, 1); _slots.anchorMax = new Vector2(1, 1); _slots.pivot = new Vector2(0.5f, 1);
            _slots.offsetMin = new Vector2(30, -290); _slots.offsetMax = new Vector2(-30, -115);
            var h = UIKit.HList(_slots, 18);
            h.childForceExpandWidth = true;

            var panel = UIKit.Panel(Root, "ListPanel", Theme.Panel);
            UIKit.Stretch(panel.rectTransform, 16, 305, 16, 10);
            RectTransform content;
            var sr = UIKit.Scroll(panel.transform, "Scroll", true, false, out content);
            UIKit.Stretch((RectTransform)sr.transform, 0, 0, 0, 0);
            var grid = content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(780, 230);
            grid.spacing = new Vector2(18, 18);
            grid.padding = new RectOffset(18, 18, 18, 18);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            var fit = content.gameObject.GetComponent<ContentSizeFitter>();
            if (fit != null) fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _list = content;
        }

        public override void Refresh()
        {
            _header.text = Loc.T("Bilgi Puanı: ", "Knowledge: ") + State.Knowledge +
                Loc.T("   <size=26><color=#A99FC9>(dakikada +", "   <size=26><color=#A99FC9>(+") + Session.KnowledgePerMinute.ToString("0.#") +
                Loc.T(" · yeni element, iksir ve Fail-Dex sayfaları da Bilgi verir)</color></size>", "/min · new elements, potions and Fail-Dex pages also give Knowledge)</color></size>");

            // ----- Yuvalar -----
            UIKit.Clear(_slots);
            for (int i = 0; i < MaxSlots; i++)
            {
                bool open = i < State.PassiveSlots;
                string id = i < State.EquippedPassives.Count ? State.EquippedPassives[i] : null;
                var p = id != null ? PassiveDatabase.Get(id) : null;
                string label;
                Color bg;
                if (!open)
                {
                    int need = 1; while (BuildingDatabase.PassiveSlots(need) <= i) need++;
                    label = Loc.T("<size=24><color=#4A3420>Kilitli yuva\nKule Sv.", "<size=24><color=#4A3420>Locked slot\nTower Lv.") + need + "</color></size>";
                    bg = UIKit.Hex("#1F1936");
                }
                else if (p != null) { label = "<b>" + p.NameTr + Loc.T("</b>\n<size=22>Çıkarmak için dokun</size>", "</b>\n<size=22>Tap to remove</size>"); bg = Theme.Magic; }
                else { label = Loc.T("<size=26>Boş yuva</size>\n<size=20>Aşağıdan bir pasif tak</size>", "<size=26>Empty slot</size>\n<size=20>Equip a passive below</size>"); bg = Theme.PanelLight; }
                var b = UIKit.Btn(_slots, "Slot" + i, label, bg, () =>
                {
                    if (p == null) return;
                    Session.Unequip(p.Id);
                    AudioFX.Play(Sfx.Click);
                    Refresh();
                }, 26);
                UIKit.Wood(b);
                if (p != null && Art.Has(p.Icon))
                {
                    var ic = Art.Image(b.transform, "Icon", Art.Get(p.Icon));
                    UIKit.Place(ic.rectTransform, new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(70, 70), new Vector2(0, 0.5f));
                    ic.raycastTarget = false;
                }
            }

            // ----- Pasif listesi -----
            UIKit.Clear(_list);
            bool parch = Art.Has("card_parchment");
            foreach (var pd in PassiveDatabase.All)
            {
                var p = pd;
                bool owned = State.OwnedPassives.Contains(p.Id);
                bool equipped = State.HasPassive(p.Id);
                string why = owned ? null : Session.CanBuyPassive(p.Id);
                string status = equipped ? Loc.T("<color=#1E8A4A>TAKILI</color>", "<color=#1E8A4A>EQUIPPED</color>")
                    : owned ? Loc.T("<color=#2A6FB0>Sende · takmak için dokun</color>", "<color=#2A6FB0>Owned · tap to equip</color>")
                    : why == null ? Loc.T("<color=#9A5B00>Aç: ", "<color=#9A5B00>Unlock: ") + p.Cost + Loc.T(" Bilgi</color>", " Knowledge</color>")
                    : "<color=#6B5A4A>" + p.Cost + Loc.T(" Bilgi gerekli</color>", " Knowledge needed</color>");
                if (!parch) status = status.Replace("#1E8A4A", "#7CF2C8").Replace("#2A6FB0", "#9EE6FF").Replace("#9A5B00", "#FFD166").Replace("#6B5A4A", "#A99FC9");
                var card = UIKit.Btn(_list, p.Id,
                    "<b>" + p.NameTr + "</b>\n<size=24>" + p.DescriptionTr + "</size>\n<size=26>" + status + "</size>",
                    equipped ? Theme.Magic : Theme.PanelLight, () => OnCard(p), 30);
                var label = UIKit.LabelOf(card);
                label.alignment = TextAnchor.MiddleLeft;
                label.rectTransform.offsetMin = new Vector2(130, label.rectTransform.offsetMin.y);
                if (parch)
                {
                    var bi = card.GetComponent<Image>();
                    bi.sprite = Art.Sliced("card_parchment", new Vector4(50, 50, 50, 50)); bi.type = Image.Type.Sliced;
                    bi.pixelsPerUnitMultiplier = 1.2f;
                    bi.color = equipped ? new Color(0.85f, 1f, 0.88f) : owned || why == null ? Color.white : new Color(0.65f, 0.62f, 0.58f);
                    label.color = UIKit.WoodText;
                }
                if (Art.Has(p.Icon))
                {
                    var ic = Art.Image(card.transform, "Icon", Art.Get(p.Icon));
                    UIKit.Place(ic.rectTransform, new Vector2(0, 0.5f), new Vector2(24, 0), new Vector2(96, 96), new Vector2(0, 0.5f));
                    ic.raycastTarget = false;
                }
                if (!owned && why != null && Art.Has("icon_lock"))
                {
                    var lk = Art.Image(card.transform, "Lock", Art.Get("icon_lock"));
                    UIKit.Place(lk.rectTransform, new Vector2(1, 1), new Vector2(-30, -30), new Vector2(46, 58));
                    lk.raycastTarget = false;
                }
            }
        }

        void OnCard(PassiveData p)
        {
            if (State.HasPassive(p.Id)) { Session.Unequip(p.Id); AudioFX.Play(Sfx.Click); Refresh(); return; }
            if (State.OwnedPassives.Contains(p.Id))
            {
                string err = Session.Equip(p.Id);
                if (err != null) G.Toast(err); else { AudioFX.Play(Sfx.Chime, 0.7f); Refresh(); }
                return;
            }
            string why = Session.CanBuyPassive(p.Id);
            if (why != null) { G.Toast(why); return; }
            Session.BuyPassive(p.Id);
            AudioFX.Play(Sfx.LevelUp, 0.8f);
            if (FX.I != null) FX.I.Burst(G.Overlay, Theme.Magic, Vector2.zero, 40, 900);
            G.Toast(p.NameTr + Loc.T(" açıldı!", " unlocked!") + (State.HasPassive(p.Id) ? Loc.T(" Yuvaya takıldı.", " Equipped in a slot.") : Loc.T(" Boş yuva yok, bir pasifi çıkarıp takabilirsin.", " No free slot - remove a passive to equip it.")));
            Refresh();
        }
    }
}
