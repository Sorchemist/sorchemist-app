// İzometrik "hobbit köyü" haritası. Köy 13x13 karo: ortada Aqualf'in Kulesi, evler aralarında boşluk
// kalacak şekilde dağınık; oyuncu haritada gezinerek evleri bulur. Her evin 1-12 arası seviyesi vardır.
using System;
using System.Collections.Generic;
using System.Linq;
using Sorchemist.Core;
using Sorchemist.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Sorchemist.UI
{
    public class VillageScreen : GameScreen
    {
        // 13x13 izometrik blok harita. Her karo: üst yüzü 560x308 piksel olan bir toprak bloğu.
        const int GridSize = 13;
        const float TileW = 560, TileH = 308;
        const int RiverX = GridSize;   // oynanabilir alanın hemen sağında nehir
        const float Zoom = 0.55f;

        RectTransform _map, _objects, _questBanner;
        Text _questText;
        readonly Dictionary<string, RectTransform> _houseViews = new Dictionary<string, RectTransform>();
        RectTransform _towerView;
        RectTransform _mystery;
        Image _mysteryImg;
        Text _mysteryLabel;
        int _mysterySpot = -1;
        readonly HashSet<string> _artHouses = new HashSet<string>();

        // Açık bina paneli (geri sayımı canlı güncellemek için)
        RectTransform _panel;
        string _panelId;
        Text _panelStatus;
        Button _panelUpgrade;

        static readonly string[] FillerDecor = { "decor_forest", "decor_forest2", "decor_bushes", "decor_mushrooms" };
        static readonly string[] SmallDecor = { "decor_bushes", "decor_mushrooms", "decor_logs", "decor_fence", "decor_bushes", "decor_signpost" };
        static readonly Vector2Int LabPos = new Vector2Int(GridSize / 2, GridSize / 2);
        const float Margin = 150;          // üst kenar ile (0,0) karosunun merkezi arası

        // ---- Ev yerleşimi: çift koordinatlı karolar (aralarında bir boş karo), kuleye yakından uzağa ----
        static Dictionary<string, Vector2Int> _housePos;
        static HashSet<Vector2Int> _houseSlots;
        static Dictionary<string, Vector2Int> HousePos
        {
            get
            {
                if (_housePos != null) return _housePos;
                var slots = new List<Vector2Int>();
                for (int y = 0; y < GridSize; y += 2)
                    for (int x = 0; x < GridSize; x += 2)
                        if (x != LabPos.x && y != LabPos.y) slots.Add(new Vector2Int(x, y));
                // Kuleye uzaklık; eşitlikte sabit karıştırma (her yöne dağılsın)
                slots = slots.OrderBy(v => (v - LabPos).sqrMagnitude * 100 + ((v.x * 31 + v.y * 17) % 97)).ToList();
                // İlk görevleri olan köylüler kuleye yakın otursun
                var order = new List<string>();
                foreach (var q in VillageDatabase.Quests.OrderBy(q => q.MinLevel))
                    if (!order.Contains(q.VillagerId)) order.Add(q.VillagerId);
                foreach (var v in VillageDatabase.Villagers) if (!order.Contains(v.Id)) order.Add(v.Id);
                _housePos = new Dictionary<string, Vector2Int>();
                for (int i = 0; i < order.Count && i < slots.Count; i++) _housePos[order[i]] = slots[i];
                _houseSlots = new HashSet<Vector2Int>(slots);
                return _housePos;
            }
        }

        // Haritanın kaydırılabilir dikdörtgeni (yerel koordinat). Bu dikdörtgenin her pikseli bir karo ile dolu
        // olduğu ve kaydırma "Clamped" olduğu için ekranda hiç boş alan görünmez.
        static float HalfWidth { get { return (GridSize - 1) * TileW * 0.5f; } }
        static float Bottom { get { return -(Margin + (GridSize - 1) * TileH + Margin); } }

        static bool Playable(int gx, int gy) { return gx >= 0 && gy >= 0 && gx < GridSize && gy < GridSize; }
        static bool OnPath(int gx, int gy) { return Playable(gx, gy) && (gx == LabPos.x || gy == LabPos.y); }

        static Vector2 IsoToLocal(int gx, int gy)
        {
            // Dönen değer karonun üst yüz merkezi
            return new Vector2((gx - gy) * TileW * 0.5f, -(gx + gy) * TileH * 0.5f - Margin);
        }

        static int Hash(int x, int y) { return Mathf.Abs(x * 73856093 ^ y * 19349663); }

        static string GroundKey(int gx, int gy)
        {
            if (gx == RiverX) return "tile_water";
            if (!Playable(gx, gy)) return ((gx * 5 + gy * 11) % 9 == 0) ? "tile_grass_b" : "tile_grass";
            if (gx == LabPos.x && gy == LabPos.y) return "tile_cross";
            if (OnPath(gx, gy)) return "tile_path";
            return ((gx * 7 + gy * 3) % 5 == 0) ? "tile_grass_b" : "tile_grass";
        }

        static Color GroundColor(int gx, int gy)
        {
            if (gx == RiverX) return UIKit.Hex("#3E8ED0");
            if (OnPath(gx, gy)) return Theme.Path;
            return (gx + gy) % 2 == 0 ? Theme.Grass : Theme.GrassDark;
        }

        public override void Build()
        {
            var bg = UIKit.Img(Root, "Sky", UIKit.Hex("#1E3A2C"));
            UIKit.Stretch(bg.rectTransform);
            var skyArt = Art.Get("bg_village");
            if (skyArt != null)
            {
                bg.color = Color.white; bg.sprite = skyArt;
                var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = skyArt.rect.width / skyArt.rect.height;
                Root.gameObject.AddComponent<RectMask2D>();
            }

            RectTransform content;
            var sr = UIKit.Scroll(Root, "MapScroll", true, true, out content);
            UIKit.Stretch((RectTransform)sr.transform);
            Destroy(content.GetComponent<ContentSizeFitter>());
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = new Vector2(HalfWidth * 2, -Bottom);
            sr.movementType = ScrollRect.MovementType.Clamped;   // haritanın dışına taşmak yok
            sr.inertia = true; sr.decelerationRate = 0.12f;
            _map = content;

            var tiles = UIKit.Rect("Tiles", _map);
            UIKit.Place(tiles, new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            _objects = UIKit.Rect("Objects", _map);
            UIKit.Place(_objects, new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);

            // Zemin blokları: arkadan öne (gx+gy küçükten büyüğe) çizilir
            // Dikdörtgeni tamamen kaplayan bütün karolar (oynanabilir 7x7 + çevresindeki dolgu karoları)
            var cells = new List<Vector2Int>();
            for (int gy = -GridSize; gy < GridSize * 2; gy++)
                for (int gx = -GridSize; gx < GridSize * 2; gx++)
                {
                    var c = IsoToLocal(gx, gy);
                    if (Mathf.Abs(c.x) <= HalfWidth + TileW * 0.5f + 1 && c.y <= TileH * 0.5f + 1 && c.y >= Bottom - TileH * 0.5f - 1)
                        cells.Add(new Vector2Int(gx, gy));
                }
            cells.Sort((p, q) => (p.x + p.y).CompareTo(q.x + q.y));
            foreach (var c in cells)
            {
                var art = Art.First(GroundKey(c.x, c.y), "tile_grass");
                Image t;
                if (art != null)
                {
                    // Görselin ortası = üst yüzün merkezi (asset hazırlanırken böyle hizalandı)
                    t = Art.Image(tiles, "T", art);
                    float h = (TileW + 6) * art.rect.height / art.rect.width;
                    UIKit.Place(t.rectTransform, new Vector2(0.5f, 0.5f), IsoToLocal(c.x, c.y), new Vector2(TileW + 6, h));
                }
                else
                {
                    t = UIKit.Img(tiles, "T", GroundColor(c.x, c.y), UIKit.Diamond);
                    UIKit.Place(t.rectTransform, new Vector2(0.5f, 0.5f), IsoToLocal(c.x, c.y), new Vector2(TileW + 2, TileH + 2));
                }
                t.raycastTarget = false;
            }

            // Nesneleri derinliğe göre sırala (arkadakiler önce çizilir)
            var items = new List<KeyValuePair<int, Action>>();
            var used = new HashSet<Vector2Int>();
            foreach (var v in VillageDatabase.Villagers)
            {
                var villager = v;
                Vector2Int pos;
                if (!HousePos.TryGetValue(v.Id, out pos)) continue;
                used.Add(pos);
                items.Add(new KeyValuePair<int, Action>(pos.x + pos.y, () => BuildHouse(villager, pos)));
            }
            foreach (var c in cells)
            {
                if (c.x == RiverX || used.Contains(c) || c == LabPos || OnPath(c.x, c.y)) continue;
                int h = Hash(c.x, c.y);
                var p = c;
                if (!Playable(c.x, c.y))
                {
                    // Dolgu karolarına (sabit) orman/çalı
                    if (h % 3 != 0) continue;
                    var key = FillerDecor[h % FillerDecor.Length];
                    items.Add(new KeyValuePair<int, Action>(p.x + p.y, () => BuildDecor(key, p, 1f)));
                }
                else if (_houseSlots.Contains(c))
                {
                    // Boş kalan ev arsaları: koru
                    var key = FillerDecor[h % 2];
                    items.Add(new KeyValuePair<int, Action>(p.x + p.y, () => BuildDecor(key, p, 0.85f)));
                }
                else if (h % 4 == 0)
                {
                    // Evlerin arasındaki boşluklarda küçük süsler (evleri örtmesin)
                    var key = SmallDecor[h % SmallDecor.Length];
                    items.Add(new KeyValuePair<int, Action>(p.x + p.y, () => BuildDecor(key, p, 0.55f)));
                }
            }
            // Nehrin üstünde şelale
            var fall = new Vector2Int(RiverX, 2);
            items.Add(new KeyValuePair<int, Action>(fall.x + fall.y, () => BuildDecor("decor_waterfall", fall, 1f)));
            items.Add(new KeyValuePair<int, Action>(LabPos.x + LabPos.y, BuildLab));
            items.Sort((a, b) => a.Key.CompareTo(b.Key));
            foreach (var it in items) it.Value();
            BuildMystery();

            // Uzaklaştırılmış görünüm; köy ekrandan büyük, gezinerek keşfedilir
            content.localScale = Vector3.one * Zoom;
            // Haritayı kuleye ortala
            content.anchoredPosition = new Vector2(0, -IsoToLocal(LabPos.x, LabPos.y).y * Zoom - 470);

            // Görev afişi (ekranın üstünde sabit)
            var banner = UIKit.Btn(Root, "QuestBanner", "", UIKit.Hex("#8E44AD"), OpenQuest, 34);
            UIKit.Wood(banner);
            _questBanner = (RectTransform)banner.transform;
            UIKit.Place(_questBanner, new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(1000, 110));
            _questText = UIKit.LabelOf(banner);

            var hint = UIKit.Txt(Root, "Hint", "Haritada gezin • Evlere dokun: yükselt, sohbet et • Gizemli Kutu'yu kaçırma", 26, new Color(1, 1, 1, 0.75f));
            hint.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.7f);
            UIKit.Place(hint.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(1400, 44));
        }

        // Nesne kökü: alt orta noktası karonun üst yüz merkezinin biraz önünde
        RectTransform ObjectRoot(string name, int gx, int gy, Vector2 size)
        {
            var rt = UIKit.Rect(name, _objects);
            UIKit.Place(rt, new Vector2(0.5f, 0.5f), IsoToLocal(gx, gy) + new Vector2(0, -TileH * 0.28f), size, new Vector2(0.5f, 0));
            return rt;
        }

        void BuildHouse(VillagerData v, Vector2Int pos)
        {
            var rt = ObjectRoot("House_" + v.Id, pos.x, pos.y, new Vector2(340, 330));
            var houseArt = Art.First("house_" + v.Id, "house_default");
            Image wall, roof;
            if (houseArt != null)
            {
                wall = Art.Image(rt, "Wall", houseArt);
                UIKit.Place(wall.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 0), new Vector2(340, 300), new Vector2(0.5f, 0));
                roof = wall;
                _artHouses.Add(v.Id);
            }
            else
            {
                wall = UIKit.Panel(rt, "Wall", UIKit.Hex(v.ColorHex));
                UIKit.Place(wall.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(170, 130), new Vector2(0.5f, 0));
                roof = UIKit.Img(rt, "Roof", UIKit.Hex("#7B3F00"), UIKit.Triangle);
                UIKit.Place(roof.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(220, 110), new Vector2(0.5f, 0));
                var door = UIKit.Img(rt, "Door", UIKit.Hex("#4A2C12"), UIKit.Circle);
                UIKit.Place(door.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(56, 84), new Vector2(0.5f, 0));
            }

            AddNameplate(rt, v.NameTr, Color.white, -30);

            var state = UIKit.Rect("State", rt);           // Yaşayan harita süslemeleri buraya
            UIKit.Stretch(state);

            var bangArt = Art.Get("icon_bang");
            Graphic bang;
            if (bangArt != null)
            {
                var bi = Art.Image(rt, "Bang", bangArt);
                UIKit.Place(bi.rectTransform, new Vector2(0.5f, 1), new Vector2(0, 50), new Vector2(90, 90));
                bi.raycastTarget = false;
                bang = bi;
            }
            else
            {
                var bt = UIKit.Txt(rt, "Bang", "!", 90, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
                UIKit.Place(bt.rectTransform, new Vector2(0.5f, 1), new Vector2(0, 60), new Vector2(80, 110));
                bt.gameObject.AddComponent<Outline>().effectColor = Color.black;
                bang = bt;
            }
            bang.gameObject.AddComponent<Bouncer>();

            var btn = wall.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(() => TapHouse(v));
            if (roof != wall) roof.gameObject.AddComponent<Button>().onClick.AddListener(() => TapHouse(v));
            _houseViews[v.Id] = rt;
        }

        // İsim + seviye rozeti + yükseltme geri sayımı
        void AddNameplate(RectTransform rt, string name, Color color, float y)
        {
            var plate = UIKit.Img(rt, "Plate", new Color(0.08f, 0.06f, 0.14f, 0.78f), UIKit.Rounded);
            plate.type = Image.Type.Sliced;
            UIKit.Place(plate.rectTransform, new Vector2(0.5f, 0), new Vector2(0, y), new Vector2(360, 84));
            plate.raycastTarget = false;
            var label = UIKit.Txt(plate.transform, "Name", name, 28, color, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(label.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -21), new Vector2(350, 38));
            label.resizeTextForBestFit = true; label.resizeTextMinSize = 14; label.resizeTextMaxSize = 28;
            var lvl = UIKit.Txt(plate.transform, "Level", "", 24, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(lvl.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 21), new Vector2(350, 34));
        }

        void UpdateNameplate(RectTransform rt, string id, long now)
        {
            var lvl = rt.Find("Plate/Level");
            if (lvl == null) return;
            var b = State.Building(id);
            string t = "Sv." + b.Level + (b.Level >= BuildingDatabase.MaxLevel ? " (MAKS)" : "");
            if (b.UpgradeEndsAt > 0)
                t += "  <color=#7CF2C8>" + BuildingDatabase.FormatDuration(Session.UpgradeSecondsLeft(id, now)) + "</color>";
            lvl.GetComponent<Text>().text = t;
        }

        void BuildDecor(string key, Vector2Int p, float scale)
        {
            var art = Art.Get(key);
            if (art == null) return;
            // Küçük dekorlar (kütük, çit, kemer) daha küçük çizilir
            float s = key == "decor_logs" ? 200 : key == "decor_fence" ? 260 : key == "decor_arch" ? 300 : key == "decor_signpost" ? 150 : 460;
            s *= scale;
            var rt = ObjectRoot("Decor_" + key, p.x, p.y, new Vector2(s, s));
            var img = Art.Image(rt, "Art", art);
            UIKit.Place(img.rectTransform, new Vector2(0.5f, 0), new Vector2(0, s > 300 ? -30 : 0), new Vector2(s, s * 0.92f), new Vector2(0.5f, 0));
            img.raycastTarget = false;
        }

        void BuildLab()
        {
            var rt = ObjectRoot("Lab", LabPos.x, LabPos.y, new Vector2(560, 500));
            var towerArt = Art.First("lab_tower", "cauldron");
            Image tower;
            if (towerArt != null)
            {
                tower = Art.Image(rt, "Tower", towerArt);
                UIKit.Place(tower.rectTransform, new Vector2(0.5f, 0), new Vector2(0, -20), new Vector2(560, 500), new Vector2(0.5f, 0));
            }
            else
            {
                tower = UIKit.Panel(rt, "Tower", UIKit.Hex("#5B2C83"));
                UIKit.Place(tower.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(170, 260), new Vector2(0.5f, 0));
                var roof = UIKit.Img(rt, "Roof", UIKit.Hex("#7CF2C8"), UIKit.Triangle);
                UIKit.Place(roof.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 295), new Vector2(230, 190), new Vector2(0.5f, 0));
                var win = UIKit.Img(rt, "Window", Theme.Gold, UIKit.Circle);
                UIKit.Place(win.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 200), new Vector2(64, 64), new Vector2(0.5f, 0));
                win.gameObject.AddComponent<Bouncer>().Glow = true;
            }
            AddNameplate(rt, "Aqualf'in Kulesi", Theme.Accent, -10);
            tower.gameObject.AddComponent<Button>().onClick.AddListener(() => OpenBuilding(BuildingDatabase.Tower));
            _towerView = rt;
        }

        // ---------- Gizemli kutu (her 6 saatte bir) ----------
        // Kutunun belirebileceği yerler: laboratuvarın çevresi
        static readonly Vector2[] MysterySpots =
        {
            new Vector2(300, -40), new Vector2(-300, -40), new Vector2(0, -200), new Vector2(330, 120)
        };

        void BuildMystery()
        {
            _mystery = UIKit.Rect("MysteryBox", _objects);
            UIKit.Place(_mystery, new Vector2(0.5f, 0.5f), IsoToLocal(LabPos.x, LabPos.y), new Vector2(200, 230), new Vector2(0.5f, 0));
            var glow = UIKit.Img(_mystery, "Glow", new Color(1f, 0.9f, 0.4f, 0.5f), UIKit.Circle);
            UIKit.Place(glow.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 100), new Vector2(260, 260));
            glow.raycastTarget = false;
            glow.gameObject.AddComponent<Bouncer>().Glow = true;
            var art = Art.First("mystery_box_2", "mystery_box_1");
            _mysteryImg = art != null ? Art.Image(_mystery, "Box", art) : UIKit.Img(_mystery, "Box", Theme.Gold, UIKit.Diamond);
            UIKit.Place(_mysteryImg.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(170, 170), new Vector2(0.5f, 0));
            _mysteryImg.gameObject.AddComponent<Bouncer>().Amplitude = 14;
            _mysteryImg.gameObject.AddComponent<Button>().onClick.AddListener(TapMystery);
            _mysteryLabel = UIKit.Txt(_mystery, "Label", "", 28, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(_mysteryLabel.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(320, 40));
            _mysteryLabel.gameObject.AddComponent<Outline>().effectColor = Color.black;
        }

        void RefreshMystery()
        {
            if (_mystery == null) return;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            bool ready = Session.MysteryReady(now);
            int spot = State.MysteryOpened % MysterySpots.Length;
            if (spot != _mysterySpot)
            {
                _mysterySpot = spot;
                _mystery.anchoredPosition = IsoToLocal(LabPos.x, LabPos.y) + MysterySpots[spot];
                _mystery.SetAsLastSibling();
            }
            _mystery.Find("Glow").gameObject.SetActive(ready);
            _mysteryImg.color = ready ? Color.white : new Color(0.55f, 0.55f, 0.6f, 0.55f);
            if (ready) _mysteryLabel.text = "Gizemli Kutu!";
            else
            {
                long left = Session.MysterySecondsLeft(now);
                _mysteryLabel.text = (left / 3600).ToString("0") + ":" + (left % 3600 / 60).ToString("00") + ":" + (left % 60).ToString("00");
            }
        }

        void TapMystery()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (!Session.MysteryReady(now))
            {
                G.Toast("Gizemli kutu her " + Session.MysteryInterval / 3600 + " saatte bir gelir. Sıradaki: " + _mysteryLabel.text);
                return;
            }
            var r = Session.OpenMystery(now);
            if (r == null) return;
            AudioFX.Play(Sfx.Chime);
            Haptics.Bump();
            FX.I.Burst(_objects, Theme.Gold, _mystery.anchoredPosition + Vector2.up * 120, 60, 900);
            FX.I.Flash(G.Overlay, new Color(1f, 0.9f, 0.5f, 0.6f), 0.6f);
            string els = string.Join(", ", r.Elements.Select(kv => kv.Value + " x " + AlchemyNames.Name(ElementDatabase.Get(kv.Key).Number) +
                " (" + kv.Key + ")").ToArray());
            var raw = RawMaterialDatabase.Get(r.RawId);
            ResultPopup.Show(r.Rare ? "Nadir Hazine!" : "Gizemli Kutu", Theme.Gold,
                "Kutunun içinden şunlar çıktı:\n\n<b>" + els + "</b>\n+1 " + raw.NameTr + "\n+" + r.Gold + " altın",
                "Sıradaki kutu " + Session.MysteryInterval / 3600 + " saat sonra köye gelecek. Nadir elementler için 'Şans Tılsımı' pasifi işe yarar.",
                "+" + r.Xp + " XP" + (r.Knowledge > 0 ? "  •  +" + r.Knowledge + " Bilgi" : ""), "Sorchemist'te gizemli kutudan " + els + " çıktı!",
                () => { G.HandleLevelUps(r.LevelUps); Refresh(); }, Art.Has("mystery_box_2") ? "mystery_box_2" : null);
            RefreshMystery();
        }

        // ---------- Etkileşimler ----------
        void TapHouse(VillagerData v)
        {
            AudioFX.Play(Sfx.Click, 0.6f);
            OpenBuilding(v.Id);
        }

        void Talk(VillagerData v)
        {
            var st = State.GetHouse(v.Id);
            string line;
            switch (st)
            {
                case HouseState.Happy: line = "Selam Aqualf! Senin sayende her şey yolunda."; break;
                case HouseState.GiantPumpkins: line = "Kapımı açamıyorum. Kabaklar. Her yerde kabaklar."; break;
                case HouseState.Frog: line = "Vırak. (Sanırım sana kızgın değil.)"; break;
                case HouseState.Floating: line = "Yukarıdan manzara güzel ama inmek istiyorum!"; break;
                case HouseState.Frozen: line = "B-b-burası b-biraz s-soğuk..."; break;
                case HouseState.Foam: line = "Köpükten kale yaptım! Ama evim nerede?"; break;
                case HouseState.Glowing: line = "Evim gece gündüz parlıyor. Gözlük lazım."; break;
                case HouseState.Pink: line = "Pembe yakıştı mı sence? Tavuklarım bayıldı."; break;
                case HouseState.Fireworks: line = "Ambarım... ama gösteri muhteşemdi!"; break;
                default:
                    line = v.AmbientTr != null && v.AmbientTr.Length > 0
                        ? v.AmbientTr[UnityEngine.Random.Range(0, v.AmbientTr.Length)]
                        : "Merhaba simyacı! Bugün sana bir işim yok.";
                    break;
            }
            DialoguePanel.Show(G.Overlay, v.NameTr, v.RoleTr, v.ColorHex, "portrait_" + v.Id, new[] { line }, null);
        }

        // ---------- Bina paneli: seviye, gelir, açtığı deneyler, yükseltme ----------
        void ClosePanel()
        {
            if (_panel != null) Destroy(_panel.gameObject);
            _panel = null; _panelId = null; _panelStatus = null; _panelUpgrade = null;
        }

        void OpenBuilding(string id)
        {
            ClosePanel();
            bool tower = id == BuildingDatabase.Tower;
            var v = tower ? null : VillageDatabase.GetVillager(id);
            var b = State.Building(id);
            RectTransform body;
            _panel = UIKit.Modal(G.Overlay, BuildingDatabase.NameTr(id), out body, 900, 1500);
            _panelId = id;

            string portrait = tower ? "portrait_aqualf" : "portrait_" + id;
            bool hasPortrait = Art.Has(portrait);
            if (hasPortrait)
            {
                var pi = Art.Image(body, "Portrait", Art.Get(portrait));
                UIKit.Place(pi.rectTransform, new Vector2(0, 1), new Vector2(170, -180), new Vector2(320, 340));
                pi.raycastTarget = false;
            }

            string info;
            int lv = b.Level;
            if (tower)
            {
                info = "<b>Seviye " + lv + " / " + BuildingDatabase.MaxLevel + "</b>\n" +
                    "Kazan kapasitesi: <b>" + BuildingDatabase.CauldronCapacity(lv) + " atom</b>" + NextVal(lv, BuildingDatabase.CauldronCapacity) + "\n" +
                    "Pasif yuvası: <b>" + BuildingDatabase.PassiveSlots(lv) + "</b>" + NextVal(lv, BuildingDatabase.PassiveSlots) + "\n" +
                    "Usta sayısı (aynı anda yükseltme): <b>" + BuildingDatabase.Builders(lv) + "</b>" + NextVal(lv, BuildingDatabase.Builders) + "\n" +
                    "Bilgi geliri: <b>" + Session.KnowledgePerMinute.ToString("0.#") + "/dk</b>\n" +
                    "<size=26><color=#A99FC9>Köydeki evler kulenin en fazla 2 seviye üstüne çıkabilir.</color></size>";
            }
            else
            {
                info = "<b>" + v.NameTr + "</b> <color=#A99FC9>(" + v.RoleTr + ")</color>\n" +
                    "<b>Seviye " + lv + " / " + BuildingDatabase.MaxLevel + "</b>   Altın: <b>" + BuildingDatabase.GoldPerMinute(id, lv) + "/dk</b>" +
                    (lv < BuildingDatabase.MaxLevel ? " <color=#7CF2C8>(→ " + BuildingDatabase.GoldPerMinute(id, lv + 1) + ")</color>" : "") + "\n";
                var unlocks = BuildingDatabase.UnlocksOf(id).ToList();
                if (unlocks.Count > 0)
                {
                    info += "\n<b>Öğrettiği deneyler:</b>\n";
                    foreach (var u in unlocks)
                    {
                        var c = CompoundDatabase.Get(u.CompoundId);
                        bool open = lv >= u.Level;
                        bool known = State.Discovered.Contains(c.Id);
                        string nm = known ? c.NameTr + " (" + c.Formula + ")" : "??? (" + CompoundDatabase.EffectNameTr(c.Effect) + ")";
                        info += open ? "<color=#7CF2C8>• " + nm + " — açık</color>\n" : "<color=#FFB36B>• " + nm + " — Sv." + u.Level + "'de açılır</color>\n";
                    }
                }
                else info += "\n<size=26><color=#A99FC9>Bu ev yeni deney öğretmez ama seviyesi arttıkça köyün altın geliri artar.</color></size>";
            }
            var t = UIKit.Txt(body, "Info", info, 32, Theme.Text, TextAnchor.UpperLeft);
            t.rectTransform.anchorMin = Vector2.zero; t.rectTransform.anchorMax = Vector2.one;
            t.rectTransform.offsetMin = new Vector2(hasPortrait ? 360 : 10, 250); t.rectTransform.offsetMax = new Vector2(-10, -10);

            _panelStatus = UIKit.Txt(body, "Status", "", 30, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            _panelStatus.rectTransform.anchorMin = new Vector2(0, 0); _panelStatus.rectTransform.anchorMax = new Vector2(1, 0);
            _panelStatus.rectTransform.pivot = new Vector2(0.5f, 0);
            _panelStatus.rectTransform.offsetMin = new Vector2(0, 140); _panelStatus.rectTransform.offsetMax = new Vector2(0, 220);

            var row = UIKit.Rect("Buttons", body);
            row.anchorMin = new Vector2(0, 0); row.anchorMax = new Vector2(1, 0); row.pivot = new Vector2(0.5f, 0);
            row.offsetMin = new Vector2(0, 10); row.offsetMax = new Vector2(0, 120);
            UIKit.HList(row, 16);

            _panelUpgrade = UIKit.Wood(UIKit.Btn(row, "Upgrade", "Yükselt", Theme.Accent, () => DoUpgrade(id), 32), UIKit.Hex("#0E3B2C"), UIKit.Hex("#B8FFE4"));
            UIKit.LE(_panelUpgrade, -1, -1, 2);
            if (tower)
            {
                var lab = UIKit.Wood(UIKit.Btn(row, "Lab", "Laboratuvara Gir", Theme.Magic, () => { ClosePanel(); G.Show(ScreenId.Lab); }, 32));
                UIKit.LE(lab, -1, -1, 2);
            }
            else
            {
                var q = Session.ActiveQuest;
                if (q != null && q.VillagerId == id)
                {
                    var qb = UIKit.Wood(UIKit.Btn(row, "Quest", "Görevi Dinle", UIKit.Hex("#8E44AD"), () => { ClosePanel(); OpenQuest(); }, 32));
                    UIKit.LE(qb, -1, -1, 2);
                }
                else
                {
                    var tb = UIKit.Wood(UIKit.Btn(row, "Talk", "Sohbet Et", Theme.PanelLight, () => { ClosePanel(); Talk(v); }, 32));
                    UIKit.LE(tb, -1, -1, 2);
                }
            }
            var close = UIKit.Wood(UIKit.Btn(row, "Close", "Kapat", Theme.PanelLight, ClosePanel, 32));
            UIKit.LE(close, -1, -1, 1);
            UpdatePanel();
        }

        static string NextVal(int lv, Func<int, int> f)
        {
            if (lv >= BuildingDatabase.MaxLevel) return "";
            int a = f(lv), b = f(lv + 1);
            return b != a ? " <color=#7CF2C8>(→ " + b + ")</color>" : "";
        }

        void UpdatePanel()
        {
            if (_panel == null || _panelStatus == null) return;
            var id = _panelId;
            var b = State.Building(id);
            long now = GameManager.Now;
            var label = UIKit.LabelOf(_panelUpgrade);
            if (b.Level >= BuildingDatabase.MaxLevel)
            {
                _panelStatus.text = "En yüksek seviyeye ulaştı!";
                label.text = "MAKS";
                _panelUpgrade.interactable = false;
                return;
            }
            if (b.UpgradeEndsAt > 0)
            {
                _panelStatus.text = "Sv." + (b.Level + 1) + "'e yükseltiliyor... Kalan: " + BuildingDatabase.FormatDuration(Session.UpgradeSecondsLeft(id, now));
                label.text = "Yükseltiliyor";
                _panelUpgrade.interactable = false;
                return;
            }
            int cost = BuildingDatabase.UpgradeCost(id, b.Level);
            string why = Session.CanUpgrade(id);
            _panelStatus.text = "Sv." + b.Level + " → Sv." + (b.Level + 1) + ":  " + cost + " altın  •  " +
                BuildingDatabase.FormatDuration(Session.UpgradeDuration(id)) +
                (why != null ? "\n<size=26><color=#FF9F9F>" + why + "</color></size>" : "");
            label.text = "Yükselt (" + cost + " altın)";
            _panelUpgrade.interactable = why == null;
        }

        void DoUpgrade(string id)
        {
            string why = Session.CanUpgrade(id);
            if (why != null) { G.Toast(why); return; }
            Session.StartUpgrade(id, GameManager.Now);
            AudioFX.Play(Sfx.Collect);
            RectTransform view = id == BuildingDatabase.Tower ? _towerView : (_houseViews.ContainsKey(id) ? _houseViews[id] : null);
            if (view != null) FX.I.Smoke(_objects, UIKit.Hex("#D9C9A8"), view.anchoredPosition + Vector2.up * 120, 14);
            G.Toast(BuildingDatabase.NameTr(id) + " yükseltmesi başladı: " + BuildingDatabase.FormatDuration(Session.UpgradeSecondsLeft(id, GameManager.Now)));
            UpdatePanel();
            Refresh();
        }

        void FocusOn(Vector2Int pos)
        {
            var sr = _map.GetComponentInParent<ScrollRect>();
            if (sr != null) sr.StopMovement();
            var p = IsoToLocal(pos.x, pos.y);
            float vh = sr != null && sr.viewport != null ? sr.viewport.rect.height : ((RectTransform)sr.transform).rect.height;
            _map.anchoredPosition = new Vector2(-p.x * Zoom, -p.y * Zoom - vh * 0.55f);
        }

        public override void Tick()
        {
            long now = GameManager.Now;
            foreach (var kv in _houseViews) UpdateNameplate(kv.Value, kv.Key, now);
            if (_towerView != null) UpdateNameplate(_towerView, BuildingDatabase.Tower, now);
            UpdatePanel();
            RefreshMystery();
        }

        void OpenQuest()
        {
            var q = Session.ActiveQuest;
            if (q == null) { G.Toast("Bugün kapında kimse yok. Laboratuvarda deney yap!"); return; }
            var v = VillageDatabase.GetVillager(q.VillagerId);
            Vector2Int hp;
            if (HousePos.TryGetValue(v.Id, out hp)) FocusOn(hp);   // haritayı köylünün evine kaydır
            DialoguePanel.Show(G.Overlay, v.NameTr, v.RoleTr, v.ColorHex, "portrait_" + v.Id, q.DialogueTr, null,
                new DialogueChoice("İksir Ver", Color.Lerp(Theme.Accent, Color.black, 0.35f), () => PotionPicker.Show(Deliver)),
                new DialogueChoice("Laboratuvar", Theme.Magic, () => G.Show(ScreenId.Lab)),
                new DialogueChoice("Sonra", Theme.PanelLight, null));
        }

        void Deliver(string potionId)
        {
            var q = Session.ActiveQuest;
            var v = q != null ? VillageDatabase.GetVillager(q.VillagerId) : null;
            var rep = Session.Deliver(potionId);
            if (rep == null) return;
            var o = rep.Outcome;
            RectTransform house;
            _houseViews.TryGetValue(v.Id, out house);
            Vector2 at = house != null ? house.anchoredPosition + Vector2.up * 100 : Vector2.zero;

            if (o.Success)
            {
                AudioFX.Play(Sfx.Chime);
                FX.I.Burst(_objects, Theme.Gold, at, 50, 800);
                FX.I.FloatText(_objects, "+" + o.Gold + " altın", Theme.Gold, at + Vector2.up * 120);
                string rewards = o.RawRewards.Length > 0 ? "\n\nÖdül: " + string.Join(", ", Array.ConvertAll(o.RawRewards, r => RawMaterialDatabase.Get(r).NameTr)) : "";
                ResultPopup.Show("Görev Tamam!", Theme.Accent, o.MessageTr + rewards, null, "+" + o.Gold + " altın  •  +" + o.Xp + " XP",
                    "Aqualf'in iksiri " + v.NameTr + "'nin derdine deva oldu!", () => { G.HandleLevelUps(rep.LevelUps); Refresh(); }, "portrait_" + v.Id);
            }
            else if (o.Fail != null)
            {
                AudioFX.Play(Sfx.Boom, 0.8f);
                Haptics.Bump();
                FX.I.Shake(_map, 25, 0.6f);
                FX.I.Smoke(_objects, Color.white, at, 16);
                ResultPopup.Show(o.Fail.TitleTr, ResultPopup.RarityColor(o.Fail.Rarity), o.Fail.DescriptionTr, o.Fail.ScienceTr,
                    rep.NewFail ? "YENİ FAIL-DEX GİRDİSİ! (" + ResultPopup.RarityName(o.Fail.Rarity) + ")" : null,
                    "Fındıkdere'de " + o.Fail.TitleTr + " vakası! Aqualf yine yaptı...", () => { G.HandleLevelUps(rep.LevelUps); Refresh(); }, "fx_fail");
            }
            else
            {
                AudioFX.Play(Sfx.Fail, 0.7f);
                ResultPopup.Show("Pek İşe Yaramadı", Theme.TextDim, o.MessageTr, null, null, "Aqualf'in iksiri tutmadı!",
                    () => { G.HandleLevelUps(rep.LevelUps); Refresh(); }, "portrait_" + v.Id);
            }
            Refresh();
        }

        // ---------- Görünüm güncelleme ----------
        public override void Refresh()
        {
            var q = Session.ActiveQuest;
            if (q != null)
            {
                var v = VillageDatabase.GetVillager(q.VillagerId);
                _questText.text = "<b>" + v.NameTr + "</b> yardım istiyor!  <size=30>(dinlemek için dokun)</size>";
            }
            else _questText.text = "Bugün sakin bir gün.";

            long now = GameManager.Now;
            foreach (var kv in _houseViews)
            {
                kv.Value.Find("Bang").gameObject.SetActive(q != null && q.VillagerId == kv.Key);
                ApplyHouseState(kv.Value, State.GetHouse(kv.Key));
                UpdateNameplate(kv.Value, kv.Key, now);
            }
            if (_towerView != null) UpdateNameplate(_towerView, BuildingDatabase.Tower, now);
            RefreshMystery();
        }

        void ApplyHouseState(RectTransform house, HouseState st)
        {
            var deco = (RectTransform)house.Find("State");
            UIKit.Clear(deco);
            var wall = house.Find("Wall").GetComponent<Image>();
            var bob = house.GetComponent<Bouncer>();
            if (bob != null) Destroy(bob);

            Color tint = Color.white;
            switch (st)
            {
                case HouseState.GiantPumpkins:
                    for (int i = 0; i < 3; i++)
                    {
                        var p = UIKit.Img(deco, "Pumpkin", UIKit.Hex("#FF8C1A"), UIKit.Circle);
                        UIKit.Place(p.rectTransform, new Vector2(0.5f, 0), new Vector2(-110 + i * 110, 40 + (i % 2) * 50), Vector2.one * (120 + i * 25));
                    }
                    break;
                case HouseState.Frog:
                    var frog = UIKit.Img(deco, "Frog", UIKit.Hex("#3DBE4A"), UIKit.Circle);
                    UIKit.Place(frog.rectTransform, new Vector2(0.5f, 0), new Vector2(140, 20), new Vector2(90, 70));
                    var fz = UIKit.Txt(frog.transform, "F", "o o", 22, Color.black); UIKit.Stretch(fz.rectTransform);
                    break;
                case HouseState.Floating:
                    house.gameObject.AddComponent<Bouncer>().Amplitude = 30;
                    break;
                case HouseState.Frozen: tint = UIKit.Hex("#BDEBFF"); break;
                case HouseState.Foam:
                    for (int i = 0; i < 6; i++)
                    {
                        var f = UIKit.Img(deco, "Foam", new Color(1, 1, 1, 0.9f), UIKit.Circle);
                        UIKit.Place(f.rectTransform, new Vector2(0.5f, 0), new Vector2(-130 + i * 52, 30 + (i % 3) * 25), Vector2.one * 90);
                    }
                    break;
                case HouseState.Glowing:
                    var g = UIKit.Img(deco, "Glow", new Color(1f, 0.95f, 0.5f, 0.45f), UIKit.Circle);
                    UIKit.Place(g.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 150), Vector2.one * 400);
                    g.transform.SetAsFirstSibling();
                    g.gameObject.AddComponent<Bouncer>().Glow = true;
                    break;
                case HouseState.Pink: tint = UIKit.Hex("#FF9EC7"); break;
                case HouseState.Fireworks:
                    tint = UIKit.Hex("#555555");
                    break;
                case HouseState.Happy:
                    var heart = UIKit.Txt(deco, "Happy", "<3", 40, Theme.Danger, TextAnchor.MiddleCenter, FontStyle.Bold);
                    UIKit.Place(heart.rectTransform, new Vector2(0.5f, 0), new Vector2(130, 280), new Vector2(80, 60));
                    break;
            }
            var v = VillageDatabase.GetVillager(house.name.Substring("House_".Length));
            wall.color = (_artHouses.Contains(v.Id) ? Color.white : UIKit.Hex(v.ColorHex)) * tint;
        }
    }
}
