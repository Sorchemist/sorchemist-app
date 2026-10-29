// Aqualf'in laboratuvarı: Kazan (iksir yapımı), Ayrıştır (ham madde -> element), Tarifler.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sorchemist.Core;
using Sorchemist.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Sorchemist.UI
{
    public class LabScreen : GameScreen
    {
        enum Tab { Brew, Extract, Recipes }

        public static Color CategoryColor(ElementCategory c)
        {
            switch (c)
            {
                case ElementCategory.Nonmetal: return UIKit.Hex("#4CAF7A");
                case ElementCategory.NobleGas: return UIKit.Hex("#8E6CEF");
                case ElementCategory.AlkaliMetal: return UIKit.Hex("#E8574F");
                case ElementCategory.AlkalineEarth: return UIKit.Hex("#F08A3C");
                case ElementCategory.Metalloid: return UIKit.Hex("#3FA7A3");
                case ElementCategory.Halogen: return UIKit.Hex("#C9B52A");
                case ElementCategory.PostTransition: return UIKit.Hex("#6D8FB3");
                case ElementCategory.Transition: return UIKit.Hex("#4F6FD6");
                case ElementCategory.Lanthanide: return UIKit.Hex("#C25DB0");
                default: return UIKit.Hex("#A0527A");
            }
        }

        readonly Dictionary<Tab, RectTransform> _panels = new Dictionary<Tab, RectTransform>();
        readonly Dictionary<Tab, Image> _tabButtons = new Dictionary<Tab, Image>();
        Tab _tab = Tab.Brew;

        // Kazan
        readonly Dictionary<string, int> _cauldron = new Dictionary<string, int>();
        RectTransform _scene, _chips, _elementArea, _invContent, _tableContent;
        ScrollRect _invScroll, _tableScroll;
        Image _liquid, _flame;
        Text _capacity;
        AqualfView _aqualf;
        bool _showTable;
        Image _invToggle, _tableToggle;
        float _idleBubble;
        Text _emptyHint;
        bool _busy;

        // Ayrıştır / Tarifler
        RectTransform _extractList, _recipeList;

        public override void Build()
        {
            var tabs = UIKit.Rect("Tabs", Root);
            tabs.anchorMin = new Vector2(0, 1); tabs.anchorMax = Vector2.one; tabs.pivot = new Vector2(0.5f, 1);
            tabs.sizeDelta = new Vector2(-32, 84); tabs.anchoredPosition = new Vector2(0, -12);
            UIKit.HList(tabs, 12);
            AddTab(tabs, Tab.Brew, "Kazan");
            AddTab(tabs, Tab.Extract, "Ayrıştır");
            AddTab(tabs, Tab.Recipes, "Tarifler");

            foreach (Tab t in Enum.GetValues(typeof(Tab)))
            {
                var p = UIKit.Rect("Panel_" + t, Root);
                UIKit.Stretch(p, 0, 108, 0, 0);
                _panels[t] = p;
            }
            BuildBrew(_panels[Tab.Brew]);
            BuildExtract(_panels[Tab.Extract]);
            BuildRecipes(_panels[Tab.Recipes]);
            SetTab(Tab.Brew);
        }

        void AddTab(RectTransform row, Tab t, string label)
        {
            var b = UIKit.Wood(UIKit.Btn(row, "Tab" + t, label, Theme.PanelLight, () => SetTab(t), 36));
            _tabButtons[t] = b.GetComponent<Image>();
        }

        void SetTab(Tab t)
        {
            _tab = t;
            foreach (var kv in _panels) kv.Value.gameObject.SetActive(kv.Key == t);
            foreach (var kv in _tabButtons) kv.Value.color = UIKit.TabColor(kv.Key == t, Theme.Accent * 0.75f + Color.black * 0.25f, Theme.PanelLight);
            Refresh();
        }

        public override void Refresh()
        {
            if (_tab == Tab.Brew) RefreshBrew();
            else if (_tab == Tab.Extract) RefreshExtract();
            else RefreshRecipes();
        }

        // =====================================================================
        // KAZAN  (yatay düzen: solda laboratuvar sahnesi, sağda elementler)
        // =====================================================================
        const float CauldronX = 0.64f, CauldronY = 250f;

        // Kazanın merkezi, sahne merkezine göre (efektler için)
        Vector2 CauldronCenter
        {
            get { return new Vector2(_scene.rect.width * (CauldronX - 0.5f), CauldronY + 40 - _scene.rect.height * 0.5f); }
        }

        void BuildBrew(RectTransform p)
        {
            var scene = UIKit.Panel(p, "Scene", UIKit.Hex("#231C3D"));
            _scene = scene.rectTransform;
            _scene.anchorMin = Vector2.zero; _scene.anchorMax = new Vector2(0.47f, 1);
            _scene.offsetMin = new Vector2(16, 16); _scene.offsetMax = new Vector2(-8, 0);
            scene.gameObject.AddComponent<RectMask2D>();

            var bgArt = Art.Get("bg_lab");
            if (bgArt != null)
            {
                // Arka plan resmi sahneyi kaplar (Art: bg_lab)
                var bg = Art.Image(_scene, "Background", bgArt);
                bg.preserveAspect = false;
                bg.raycastTarget = false;
                var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = bgArt.rect.width / bgArt.rect.height;
                var shade = UIKit.Img(_scene, "Shade", new Color(0.08f, 0.05f, 0.15f, 0.35f));
                UIKit.Stretch(shade.rectTransform);
                shade.raycastTarget = false;
            }
            else
            {
                // Raflar (yer tutucu dekor)
                for (int i = 0; i < 2; i++)
                {
                    var shelf = UIKit.Img(_scene, "Shelf", UIKit.Hex("#3A2E5C"));
                    UIKit.Place(shelf.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -170 - i * 130), new Vector2(700, 14));
                    for (int j = 0; j < 6; j++)
                    {
                        var jar = UIKit.Panel(_scene, "Jar", Color.HSVToRGB((i * 6 + j) / 12f, 0.5f, 0.9f) * new Color(1, 1, 1, 0.55f));
                        UIKit.Place(jar.rectTransform, new Vector2(0.5f, 1), new Vector2(-300 + j * 120 + i * 40, -130 - i * 130), new Vector2(44, 64));
                    }
                }
            }

            _aqualf = AqualfView.Create(_scene, new Vector2(0.24f, 0), new Vector2(0, 260), 1f);

            // Kazan + ocak (Art: cauldron, flame)
            var flameArt = Art.Get("flame");
            _flame = UIKit.Img(_scene, "Flame", flameArt != null ? Color.white : UIKit.Hex("#FF9F1C"), flameArt ?? UIKit.Triangle);
            UIKit.Place(_flame.rectTransform, new Vector2(CauldronX, 0), new Vector2(0, 30), new Vector2(160, 120), new Vector2(0.5f, 0));
            var potArt = Art.Get("cauldron");
            var rim = potArt != null ? Art.Image(_scene, "Cauldron", potArt) : UIKit.Img(_scene, "Cauldron", UIKit.Hex("#15121F"), UIKit.Circle);
            UIKit.Place(rim.rectTransform, new Vector2(CauldronX, 0), new Vector2(0, CauldronY), new Vector2(400, 350));
            _liquid = UIKit.Img(rim.transform, "Liquid", UIKit.Hex("#3B3163"), UIKit.Circle);
            UIKit.Place(_liquid.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, potArt != null ? 113 : 50), new Vector2(potArt != null ? 236 : 330, potArt != null ? 46 : 150));
            if (potArt != null) _flame.gameObject.SetActive(false);   // kazan görselinin kendi ateşi var

            _capacity = UIKit.Txt(_scene, "Capacity", "", 32, Theme.Text);
            UIKit.Place(_capacity.rectTransform, new Vector2(CauldronX, 0), new Vector2(0, 460), new Vector2(400, 50));
            _capacity.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.7f);

            _chips = UIKit.Rect("Chips", _scene);
            UIKit.Place(_chips, new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(740, 70));
            var h = UIKit.HList(_chips, 10);
            h.childForceExpandWidth = false; h.childAlignment = TextAnchor.MiddleCenter;

            // ---- Sağ sütun ----
            var right = UIKit.Rect("RightColumn", p);
            right.anchorMin = new Vector2(0.47f, 0); right.anchorMax = Vector2.one;
            right.offsetMin = new Vector2(8, 16); right.offsetMax = new Vector2(-16, 0);

            var row = UIKit.Rect("Actions", right);
            row.anchorMin = new Vector2(0, 1); row.anchorMax = Vector2.one; row.pivot = new Vector2(0.5f, 1);
            row.sizeDelta = new Vector2(0, 110); row.anchoredPosition = Vector2.zero;
            UIKit.HList(row, 16);
            var clear = UIKit.Wood(UIKit.Btn(row, "Clear", "Boşalt", Theme.PanelLight, () => { _cauldron.Clear(); RefreshBrew(); }, 38));
            UIKit.LE(clear, -1, -1, 1);
            var brew = UIKit.Wood(UIKit.Btn(row, "Brew", "PİŞİR!", UIKit.Hex("#E67E22"), OnBrew, 50), UIKit.Hex("#5A1E00"), UIKit.Hex("#FFB36B"));
            UIKit.LE(brew, -1, -1, 2);

            var toggles = UIKit.Rect("Toggles", right);
            toggles.anchorMin = new Vector2(0, 1); toggles.anchorMax = Vector2.one; toggles.pivot = new Vector2(0.5f, 1);
            toggles.sizeDelta = new Vector2(0, 84); toggles.anchoredPosition = new Vector2(0, -122);
            UIKit.HList(toggles, 12);
            _invToggle = UIKit.Wood(UIKit.Btn(toggles, "Inv", "Envanterim", Theme.Magic, () => { _showTable = false; RefreshBrew(); }, 30)).GetComponent<Image>();
            _tableToggle = UIKit.Wood(UIKit.Btn(toggles, "Table", "Periyodik Tablo (118)", Theme.PanelLight, () => { _showTable = true; RefreshBrew(); }, 30)).GetComponent<Image>();

            _elementArea = UIKit.Rect("Elements", right);
            UIKit.Stretch(_elementArea, 0, 220, 0, 0);

            _invScroll = UIKit.Scroll(_elementArea, "Inventory", true, false, out _invContent);
            UIKit.Stretch((RectTransform)_invScroll.transform);
            var g = UIKit.Grid(_invContent, new Vector2(150, 150), new Vector2(14, 14), 10);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = 5;

            _tableScroll = UIKit.Scroll(_elementArea, "Table", true, true, out _tableContent);
            UIKit.Stretch((RectTransform)_tableScroll.transform);
            BuildPeriodicTable();
        }

        void BuildPeriodicTable()
        {
            Destroy(_tableContent.GetComponent<ContentSizeFitter>());
            const float cw = 96, ch = 104, gap = 6;
            _tableContent.sizeDelta = new Vector2(18 * (cw + gap) + 20, 9 * (ch + gap) + 60);
            foreach (var e in ElementDatabase.All)
            {
                float y = -(e.TableRow * (ch + gap)) - (e.TableRow >= 7 ? 30 : 0) - 10;
                var b = ElementTile(_tableContent, e, 0, new Vector2(cw, ch), false);
                var rt = (RectTransform)b.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(10 + e.TableCol * (cw + gap), y);
            }
            var note = UIKit.Txt(_tableContent, "Note", "Lantanitler ve aktinitler (alt iki satır) aslında 6. ve 7. periyottadır.", 24, Theme.TextDim, TextAnchor.MiddleLeft);
            var nrt = note.rectTransform;
            nrt.anchorMin = nrt.anchorMax = new Vector2(0, 1); nrt.pivot = new Vector2(0, 1);
            nrt.anchoredPosition = new Vector2(330, -(7 * (ch + gap)) + 2); nrt.sizeDelta = new Vector2(1400, 30);
        }

        // Element ikonu (Art: el_<atom no>) + adet rozeti. İkon yoksa renkli kart.
        Button ElementTile(Transform parent, ElementData e, int avail, Vector2 size, bool big)
        {
            var el = e;
            var art = Art.Get("el_" + e.Number);
            Button b;
            if (art != null)
            {
                var img = Art.Image(parent, "E" + e.Number, art);
                b = img.gameObject.AddComponent<Button>();
                b.onClick.AddListener(() => { AudioFXHook.Click(); TapElement(el); });
                img.color = avail > 0 ? Color.white : new Color(0.38f, 0.36f, 0.42f, 0.85f);
                var badge = UIKit.Panel(img.transform, "Badge", new Color(0.1f, 0.06f, 0.16f, 0.85f));
                UIKit.Place(badge.rectTransform, new Vector2(1, 1), new Vector2(-2, -2), big ? new Vector2(60, 34) : new Vector2(42, 26), new Vector2(1, 1));
                var t = UIKit.Txt(badge.transform, "Count", avail > 0 ? "x" + avail : "", big ? 24 : 18, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Stretch(t.rectTransform);
                badge.gameObject.SetActive(avail > 0);
            }
            else
            {
                b = UIKit.Btn(parent, "E" + e.Number, "", CategoryColor(e.Category), () => TapElement(el), 20);
                UIKit.LabelOf(b).text = big
                    ? "<size=22>" + e.Number + "</size>\n<b><size=52>" + e.Symbol + "</size></b>\n<size=22>" + e.NameTr + "</size>\n<b><size=26>x" + avail + "</size></b>"
                    : "<size=18>" + e.Number + "</size>\n<b><size=36>" + e.Symbol + "</size></b>\n<size=18>" + (avail > 0 ? "x" + avail : " ") + "</size>";
                if (avail <= 0) b.GetComponent<Image>().color = Color.Lerp(CategoryColor(e.Category), Theme.Bg, 0.7f);
            }
            var rt = (RectTransform)b.transform;
            rt.sizeDelta = size;
            return b;
        }

        void UpdateTile(Transform cell, ElementData e, int avail)
        {
            var badge = cell.Find("Badge");
            if (badge != null)
            {
                cell.GetComponent<Image>().color = avail > 0 ? Color.white : new Color(0.38f, 0.36f, 0.42f, 0.85f);
                badge.gameObject.SetActive(avail > 0);
                badge.GetComponentInChildren<Text>().text = "x" + avail;
            }
            else
            {
                cell.GetComponent<Image>().color = avail > 0 ? CategoryColor(e.Category) : Color.Lerp(CategoryColor(e.Category), Theme.Bg, 0.7f);
                UIKit.LabelOf(cell.GetComponent<Button>()).text = "<size=18>" + e.Number + "</size>\n<b><size=36>" + e.Symbol +
                    "</size></b>\n<size=18>" + (avail > 0 ? "x" + avail : " ") + "</size>";
            }
        }

        int InCauldron(string sym) { int n; return _cauldron.TryGetValue(sym, out n) ? n : 0; }
        int CauldronTotal { get { return _cauldron.Values.Sum(); } }

        void TapElement(ElementData e)
        {
            if (_busy) return;
            int available = State.ElementCount(e.Symbol) - InCauldron(e.Symbol);
            if (available > 0)
            {
                if (CauldronTotal >= State.CauldronCapacity)
                {
                    G.Toast("Kazan dolu! (" + State.CauldronCapacity + " atom)\nBilim ağacından 'Hassas Terazi' ile büyüt.");
                    return;
                }
                _cauldron[e.Symbol] = InCauldron(e.Symbol) + 1;
                AudioFX.Play(Sfx.Bubble, 0.7f, 0.2f);
                FX.I.Bubbles(_scene, CategoryColor(e.Category), CauldronCenter, 5);
                RefreshBrew();
                return;
            }
            ShowElementInfo(e);
        }

        void ShowElementInfo(ElementData e)
        {
            var sources = RawMaterialDatabase.All.Where(r => r.Yields.Any(y => y.Symbol == e.Symbol)).Select(r => r.NameTr + " (" + r.WhereTr + ")").ToList();
            bool mystery = RawMaterialDatabase.MysteryCommon.Contains(e.Symbol) || RawMaterialDatabase.MysteryRare.Contains(e.Symbol);
            if (mystery) sources.Add("Gizemli Kristal (şansına bağlı)");
            string where = sources.Count > 0 ? "Nerede bulunur: " + string.Join(", ", sources.ToArray()) : "Bu element henüz köyde bulunmuyor. Belki ileride...";

            bool sci = State.HasSkill(SkillDatabase.Periodic);
            RectTransform body;
            var m = UIKit.Modal(G.Overlay, AlchemyNames.Name(e.Number), out body, 820, 1300);
            var icon = Art.Get("el_" + e.Number);
            if (icon != null)
            {
                var ic = Art.Image(body, "Icon", icon);
                UIKit.Place(ic.rectTransform, new Vector2(0, 1), new Vector2(170, -190), new Vector2(330, 330));
            }
            int have = State.ElementCount(e.Symbol);
            var t = UIKit.Txt(body, "Info",
                "<b>" + e.Number + ". " + e.NameTr + " (" + e.Symbol + ")</b>   <color=#A99FC9>" + ElementDatabase.CategoryNameTr(e.Category) + "</color>" +
                "\n<i>" + AlchemyNames.Lore(e.Number) + "</i>" +
                (sci
                    ? "\n\nAtom kütlesi: " + e.Mass.ToString("0.###") + " u   •   Periyot: " + e.Period + (e.Group > 0 ? "   •   Grup: " + e.Group : "   (f-blok)")
                    : "\n\n<color=#A99FC9>Bilimsel bilgi kartı için 'Mendeleyev'in Defteri' yeteneğini aç.</color>") +
                (e.Radioactive ? "\n<color=#9CFF8A>Radyoaktif!</color>" : "") +
                "\n\n" + where +
                "\n\nElindeki: <b>" + have + "</b>", 32, Theme.Text, TextAnchor.UpperLeft);
            t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = new Vector2(icon != null ? 370 : 10, 120); t.rectTransform.offsetMax = new Vector2(-10, -10);
            var close = UIKit.Wood(UIKit.Btn(body, "Close", "Kapat", Theme.Magic, () => Destroy(m.gameObject), 34));
            UIKit.Place((RectTransform)close.transform, new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(360, 96));
        }

        Color MixColor()
        {
            if (_cauldron.Count == 0) return UIKit.Hex("#3B3163");
            var known = CompoundDatabase.FindByAtoms(_cauldron);
            if (known != null && State.Discovered.Contains(known.Id)) return UIKit.Hex(known.ColorHex);
            Color sum = Color.black; int n = 0;
            foreach (var kv in _cauldron)
            {
                sum += CategoryColor(ElementDatabase.Get(kv.Key).Category) * kv.Value;
                n += kv.Value;
            }
            var c = sum / n; c.a = 1; return c;
        }

        void RefreshBrew()
        {
            if (_chips == null) return;
            // Kazan içeriği çipleri (dokununca bir tane çıkar)
            UIKit.Clear(_chips);
            if (_cauldron.Count == 0)
            {
                var t = UIKit.Txt(_chips, "Empty", "Kazan boş. Sağdan element seç!", 32, Theme.TextDim);
                UIKit.LE(t, 60, 700);
            }
            foreach (var kv in _cauldron.ToList())
            {
                string sym = kv.Key;
                var e = ElementDatabase.Get(sym);
                var chip = UIKit.Btn(_chips, "Chip", sym + " x" + kv.Value, CategoryColor(e.Category), () =>
                {
                    if (_busy) return;
                    int n = InCauldron(sym) - 1;
                    if (n <= 0) _cauldron.Remove(sym); else _cauldron[sym] = n;
                    RefreshBrew();
                }, 34);
                UIKit.LE(chip, 64, 150);
            }
            _capacity.text = CauldronTotal + " / " + State.CauldronCapacity + " atom";
            var mix = MixColor();
            if (Art.Has("cauldron")) mix.a = _cauldron.Count == 0 ? 0f : 0.8f;   // boşken görseldeki sıvı görünsün
            _liquid.color = mix;
            _flame.color = State.HasSkill(SkillDatabase.PurpleFlame) ? Theme.Magic : UIKit.Hex("#FF9F1C");

            _invToggle.color = UIKit.TabColor(!_showTable, Theme.Magic, Theme.PanelLight);
            _tableToggle.color = UIKit.TabColor(_showTable, Theme.Magic, Theme.PanelLight);
            _invScroll.gameObject.SetActive(!_showTable);
            _tableScroll.gameObject.SetActive(_showTable);

            if (!_showTable) RefreshInventory();
            else { RefreshTable(); if (_emptyHint != null) { Destroy(_emptyHint.gameObject); _emptyHint = null; } }
        }

        void RefreshInventory()
        {
            UIKit.Clear(_invContent);
            if (_emptyHint != null) { Destroy(_emptyHint.gameObject); _emptyHint = null; }
            var owned = State.Elements.Where(s => s.Count > 0)
                .Select(s => ElementDatabase.Get(s.Id)).Where(e => e != null).OrderBy(e => e.Number).ToList();
            if (owned.Count == 0)
            {
                var t = UIKit.Txt(_elementArea, "Empty", "Hiç elementin yok.\n'Ayrıştır' sekmesinden ham maddeleri ayrıştır!", 32, Theme.TextDim);
                UIKit.Stretch(t.rectTransform);
                _emptyHint = t;
                return;
            }
            foreach (var e in owned)
            {
                int avail = State.ElementCount(e.Symbol) - InCauldron(e.Symbol);
                ElementTile(_invContent, e, avail, new Vector2(150, 150), true);
            }
        }

        void RefreshTable()
        {
            foreach (var e in ElementDatabase.All)
            {
                var cell = _tableContent.Find("E" + e.Number);
                if (cell == null) continue;
                UpdateTile(cell, e, State.ElementCount(e.Symbol) - InCauldron(e.Symbol));
            }
        }

        void OnBrew()
        {
            if (_busy) return;
            if (_cauldron.Count == 0) { G.Toast("Önce kazana element ekle!"); return; }
            _busy = true;
            _aqualf.SetMood(AqualfMood.Focus);

            int highlight = -1;
            var known = CompoundDatabase.FindByAtoms(_cauldron);
            if (known != null && State.HasSkill(SkillDatabase.Thermometer) && State.Discovered.Contains(known.Id))
                highlight = (int)known.MinHeat;

            var atoms = new Dictionary<string, int>(_cauldron);
            Color mix = MixColor();
            HeatMinigame.Run(G.Overlay, G.MinigameTime(10f), highlight, State.HasSkill(SkillDatabase.PurpleFlame), heat =>
                StirMinigame.Run(G.Overlay, G.MinigameTime(6f), mix, q => Resolve(atoms, heat, q)));
        }

        void Resolve(Dictionary<string, int> atoms, HeatLevel heat, float stir)
        {
            var rep = Session.Brew(atoms, heat, stir);
            _cauldron.Clear();
            var r = rep.Result;
            _aqualf.React(r);
            PlayResultFX(r);
            StartCoroutine(ShowResultLater(rep));
        }

        void PlayResultFX(BrewResult r)
        {
            var center = CauldronCenter;
            var ov = G.Overlay;
            if (r.Success)
            {
                var c = UIKit.Hex(r.Compound.ColorHex);
                _liquid.color = c;
                AudioFX.Play(Sfx.Chime);
                FX.I.Bubbles(_scene, c, center, 30, 300);
                FX.I.Burst(_scene, c, center + Vector2.up * 100, 30, 700);
                FX.I.SpriteBurst(_scene, "fx_success", center + Vector2.up * 120, 520);
                if (State.HasSkill(SkillDatabase.CrystalRain)) FX.I.CrystalRain(ov, Theme.Magic, 35);
                return;
            }
            Haptics.Bump();
            var v = r.Fail != null ? r.Fail.Visual : FailVisual.Sludge;
            switch (v)
            {
                case FailVisual.Explosion:
                case FailVisual.RoofBlown:
                    AudioFX.Play(Sfx.Boom);
                    FX.I.Flash(ov, new Color(1, 0.9f, 0.6f, 0.9f), 0.6f);
                    FX.I.Shake(G.SafeRoot, 40, 0.7f);
                    FX.I.Burst(_scene, UIKit.Hex("#FF7B00"), center, 60, 1200);
                    FX.I.Smoke(_scene, UIKit.Hex("#555555"), center, 18);
                    FX.I.SpriteBurst(_scene, "fx_fail", center + Vector2.up * 120, 560);
                    if (v == FailVisual.RoofBlown) FX.I.FloatText(_scene, "ÇATI UÇTU!", Theme.Danger, new Vector2(0, 150));
                    break;
                case FailVisual.HairOnFire:
                    AudioFX.Play(Sfx.Boom, 0.6f); AudioFX.Play(Sfx.Sizzle);
                    FX.I.Flash(ov, new Color(1, 0.6f, 0.2f, 0.6f), 0.4f);
                    FX.I.Burst(_scene, UIKit.Hex("#FFD166"), center, 30, 800);
                    break;
                case FailVisual.Foam:
                    AudioFX.Play(Sfx.Sizzle);
                    FX.I.Bubbles(_scene, Color.white, center, 70, 600);
                    FX.I.Bubbles(_scene, UIKit.Hex("#FFB3D9"), center, 30, 500);
                    break;
                case FailVisual.GreenCloud:
                    AudioFX.Play(Sfx.Sizzle);
                    FX.I.Smoke(_scene, UIKit.Hex("#B5E655"), center, 22);
                    break;
                case FailVisual.Glow:
                    AudioFX.Play(Sfx.Chime, 0.5f);
                    FX.I.Flash(ov, new Color(0.5f, 1f, 0.5f, 0.5f), 1.2f);
                    break;
                case FailVisual.GoldFlash:
                    AudioFX.Play(Sfx.Chime);
                    FX.I.Flash(ov, new Color(1f, 0.85f, 0.2f, 0.8f), 1.5f);
                    FX.I.CrystalRain(ov, Theme.Gold, 50);
                    break;
                case FailVisual.Flash:
                    AudioFX.Play(Sfx.Pop);
                    FX.I.Flash(ov, new Color(1, 1, 1, 0.95f), 0.5f);
                    break;
                case FailVisual.Smoke:
                    AudioFX.Play(Sfx.Sizzle);
                    FX.I.Smoke(_scene, UIKit.Hex("#3A3A3A"), center, 20);
                    break;
                case FailVisual.Nothing:
                    AudioFX.Play(Sfx.Fail, 0.6f);
                    break;
                default:
                    AudioFX.Play(Sfx.Fail, 0.7f);
                    FX.I.Bubbles(_scene, UIKit.Hex("#7A7A6A"), center, 10, 120);
                    break;
            }
        }

        IEnumerator ShowResultLater(BrewReport rep)
        {
            yield return new WaitForSeconds(1.1f);
            var r = rep.Result;
            if (r.Success)
            {
                var c = r.Compound;
                ResultPopup.Show(c.NameTr + " (" + c.Formula + ")", UIKit.Hex(c.ColorHex),
                    "Etki: <b>" + CompoundDatabase.EffectNameTr(c.Effect) + "</b>\n\n" + c.FlavorTr, null,
                    (rep.NewDiscovery ? "YENİ KEŞİF!  •  " : "") + "+" + r.Xp + " XP",
                    "Aqualf laboratuvarda " + c.NameTr + " (" + c.Formula + ") keşfetti!", () => Done(rep), "fx_success");
            }
            else if (r.Fail != null)
            {
                string body = r.Fail.DescriptionTr + (string.IsNullOrEmpty(r.HintTr) ? "" : "\n\n<color=#FFD166>İpucu: " + r.HintTr + "</color>");
                ResultPopup.Show(r.Fail.TitleTr, ResultPopup.RarityColor(r.Fail.Rarity), body, r.Fail.ScienceTr,
                    (rep.NewFail ? "YENİ FAIL-DEX GİRDİSİ!  •  " : "") + ResultPopup.RarityName(r.Fail.Rarity) + "  •  +" + r.Xp + " XP",
                    "Aqualf'in laboratuvarında '" + r.Fail.TitleTr + "' anı!", () => Done(rep), "fx_fail");
            }
            else
            {
                G.Toast(r.MessageTr);
                Done(rep);
            }
        }

        void Done(BrewReport rep)
        {
            _busy = false;
            _aqualf.SetMood(AqualfMood.Idle);
            G.HandleLevelUps(rep.LevelUps);
            RefreshBrew();
        }

        void Update()
        {
            if (_tab != Tab.Brew || _cauldron.Count == 0 || _busy) return;
            _idleBubble += Time.deltaTime;
            if (_idleBubble > 0.9f)   // Kazan hafifçe fokurdasın (ASMR)
            {
                _idleBubble = 0;
                FX.I.Bubbles(_scene, _liquid.color, CauldronCenter + new Vector2(UnityEngine.Random.Range(-80, 80), 0), 2, 60, false);
                if (State.SoundOn && UnityEngine.Random.value < 0.6f) AudioFX.Play(Sfx.Bubble, 0.2f, 0.3f);
            }
        }

        // =====================================================================
        // AYRIŞTIR
        // =====================================================================
        void BuildExtract(RectTransform p)
        {
            var sr = UIKit.Scroll(p, "ExtractScroll", true, false, out _extractList);
            UIKit.Stretch((RectTransform)sr.transform, 16, 0, 16, 0);
            UIKit.VList(_extractList, 14, 8);
        }

        void RefreshExtract()
        {
            UIKit.Clear(_extractList);
            var head = UIKit.Txt(_extractList, "Head",
                "Haritadan topladığın ham maddeleri gerçek kimya yöntemleriyle saf elementlere ayır.\n<color=#FFD166>Eksik malzemeyi " + GameSession.RawPrice + " altına satın alabilirsin.</color>", 30, Theme.TextDim);
            UIKit.LE(head, 110);

            foreach (var raw in RawMaterialDatabase.All)
            {
                var r = raw;
                int have = State.RawCount(raw.Id);
                var row = UIKit.Panel(_extractList, "Row_" + raw.Id, Theme.Panel);
                UIKit.LE(row, 250);
                var dot = UIKit.Img(row.transform, "Dot", UIKit.Hex(raw.ColorHex), raw.Id == "crystal" ? UIKit.Diamond : UIKit.Circle);
                UIKit.Place(dot.rectTransform, new Vector2(0, 0.5f), new Vector2(70, 0), new Vector2(90, 90));

                string yields = raw.Method == ExtractionMethod.Arcane ? "???" :
                    string.Join(", ", raw.Yields.Select(y => y.Symbol).ToArray());
                var info = UIKit.Txt(row.transform, "Info",
                    "<b>" + raw.NameTr + "</b>  <color=#7CF2C8>x" + have + "</color>\n<size=28><color=#A99FC9>" +
                    RawMaterialDatabase.MethodNameTr(raw.Method) + " • " + raw.WhereTr + "</color>\nVerir: " + yields + "</size>",
                    36, Theme.Text, TextAnchor.MiddleLeft);
                info.rectTransform.anchorMin = new Vector2(0, 0); info.rectTransform.anchorMax = new Vector2(1, 1);
                info.rectTransform.offsetMin = new Vector2(140, 10); info.rectTransform.offsetMax = new Vector2(-300, -10);

                var ex = UIKit.Btn(row.transform, "Extract", "Ayrıştır", have > 0 ? Theme.Accent * 0.7f + Color.black * 0.3f : Theme.PanelLight,
                    () => StartExtract(r), 34);
                UIKit.Place((RectTransform)ex.transform, new Vector2(1, 0.5f), new Vector2(-24, 50), new Vector2(260, 90), new Vector2(1, 0.5f));
                ex.interactable = have > 0;
                var buy = UIKit.Btn(row.transform, "Buy", "Al (" + GameSession.RawPrice + "a)", Theme.PanelLight, () =>
                {
                    if (Session.BuyRaw(r.Id)) { AudioFX.Play(Sfx.Collect); RefreshExtract(); }
                    else G.Toast("Yeterli altının yok. Köylülere yardım et!");
                }, 30);
                UIKit.Place((RectTransform)buy.transform, new Vector2(1, 0.5f), new Vector2(-24, -55), new Vector2(260, 80), new Vector2(1, 0.5f));
            }
        }

        void StartExtract(RawMaterialData raw)
        {
            DistillMinigame.Run(G.Overlay, raw, G.MinigameTime(6f), q =>
            {
                var got = Session.Extract(raw.Id, q);
                if (got == null) return;
                AudioFX.Play(Sfx.Chime, 0.7f);
                FX.I.Bubbles(G.Overlay, UIKit.Hex(raw.ColorHex), Vector2.zero, 24, 300);
                string list = got.Count == 0 ? "hiçbir şey (çok kötü damıtma!)" :
                    string.Join(", ", got.Select(kv => "+" + kv.Value + " " + kv.Key).ToArray());
                G.Toast("Saflık %" + Mathf.RoundToInt(q * 100) + "  |  " + list);
                RefreshExtract();
            });
        }

        // =====================================================================
        // TARİFLER
        // =====================================================================
        void BuildRecipes(RectTransform p)
        {
            var sr = UIKit.Scroll(p, "RecipeScroll", true, false, out _recipeList);
            UIKit.Stretch((RectTransform)sr.transform, 16, 0, 16, 0);
            UIKit.VList(_recipeList, 14, 8);
        }

        void RefreshRecipes()
        {
            UIKit.Clear(_recipeList);
            int found = CompoundDatabase.All.Count(c => State.Discovered.Contains(c.Id));
            var head = UIKit.Txt(_recipeList, "Head", "Keşfedilen bileşikler: " + found + " / " + CompoundDatabase.All.Count +
                "\n<size=26>Bilinmeyenler için ipuçlarını köylülerin sözlerinde ara!</size>", 34, Theme.Accent);
            UIKit.LE(head, 110);
            foreach (var c in CompoundDatabase.All)
            {
                bool known = State.Discovered.Contains(c.Id);
                var row = UIKit.Panel(_recipeList, "R_" + c.Id, known ? Theme.Panel : Theme.PanelLight * 0.8f + Color.black * 0.2f);
                UIKit.LE(row, known ? 230 : 150);
                var dot = UIKit.Img(row.transform, "Dot", known ? UIKit.Hex(c.ColorHex) : Color.gray, UIKit.Circle);
                UIKit.Place(dot.rectTransform, new Vector2(0, 0.5f), new Vector2(60, 0), new Vector2(70, 70));
                string text;
                if (known)
                    text = "<b>" + c.NameTr + "  " + c.Formula + "</b>   <color=#FFD166>" + CompoundDatabase.EffectNameTr(c.Effect) + "</color>\n" +
                           "<size=26><color=#A99FC9>Isı: " + HeatRange(c) + (c.NeedsStir ? " • karıştır" : "") + "</color>\n" + c.FlavorTr + "</size>";
                else
                    text = "<b>???</b>   <color=#FFD166>" + CompoundDatabase.EffectNameTr(c.Effect) + "</color>\n<size=26><color=#A99FC9>" +
                           c.AtomCount + " atom • içinde " + c.Atoms.Keys.First() + " var</color></size>";
                var t = UIKit.Txt(row.transform, "T", text, 34, Theme.Text, TextAnchor.MiddleLeft);
                UIKit.Stretch(t.rectTransform, 120, 10, 20, 10);
            }
        }

        static string HeatRange(CompoundData c)
        {
            return c.MinHeat == c.MaxHeat ? CompoundDatabase.HeatNameTr(c.MinHeat)
                : CompoundDatabase.HeatNameTr(c.MinHeat) + "-" + CompoundDatabase.HeatNameTr(c.MaxHeat);
        }
    }
}
