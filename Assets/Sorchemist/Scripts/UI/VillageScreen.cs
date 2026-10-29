// İzometrik "hobbit köyü" haritası: evler, toplama noktaları ve yaşayan harita durumları.
// Şu an yer tutucu şekiller kullanır. Sanat hazır olunca Image.sprite'ları değiştirmen yeterli.
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
        // 7x7 izometrik blok harita. Her karo: üst yüzü 560x308 piksel olan bir toprak bloğu.
        const int GridSize = 7;
        const float TileW = 560, TileH = 308;
        const int RiverX = 6;   // sağ kenar: nehir

        RectTransform _map, _objects, _questBanner;
        Text _questText;
        readonly Dictionary<string, RectTransform> _houseViews = new Dictionary<string, RectTransform>();
        readonly Dictionary<string, Text> _nodeTimers = new Dictionary<string, Text>();
        readonly Dictionary<string, Image> _nodeImages = new Dictionary<string, Image>();
        float _tick;
        RectTransform _mystery;
        Image _mysteryImg;
        Text _mysteryLabel;
        int _mysterySpot = -1;
        readonly HashSet<string> _artHouses = new HashSet<string>();
        readonly HashSet<string> _artNodes = new HashSet<string>();

        // Ham madde noktalarının harita konumları (gx, gy)
        static readonly Dictionary<string, Vector2Int> NodePos = new Dictionary<string, Vector2Int>
        {
            { "air", new Vector2Int(3, 0) }, { "hematite", new Vector2Int(0, 1) },
            { "magnesite", new Vector2Int(5, 1) }, { "dewcap", new Vector2Int(3, 2) },
            { "limestone", new Vector2Int(0, 3) }, { "ash", new Vector2Int(3, 4) },
            { "guano", new Vector2Int(3, 5) }, { "malachite", new Vector2Int(5, 5) },
            { "sulfur", new Vector2Int(1, 6) }, { "crystal", new Vector2Int(3, 6) },
            { "seawater", new Vector2Int(6, 3) }, { "sand", new Vector2Int(6, 5) },
        };
        // Dekor (Tiles.jpg parçaları)
        static readonly Dictionary<Vector2Int, string> Decor = new Dictionary<Vector2Int, string>
        {
            { new Vector2Int(0, 0), "decor_forest" }, { new Vector2Int(5, 0), "decor_forest2" },
            { new Vector2Int(0, 5), "decor_mushrooms" }, { new Vector2Int(0, 6), "decor_arch" },
            { new Vector2Int(2, 6), "decor_logs" }, { new Vector2Int(4, 6), "decor_fence" },
            { new Vector2Int(5, 6), "decor_bushes" }, { new Vector2Int(6, 1), "decor_waterfall" },
        };
        static readonly Vector2Int LabPos = new Vector2Int(3, 3);
        const float Margin = 260;

        static Vector2 IsoToLocal(int gx, int gy)
        {
            // Haritanın tepesi (0,0) üstte ortada; dönen değer karonun üst yüz merkezi
            return new Vector2((gx - gy) * TileW * 0.5f, -(gx + gy) * TileH * 0.5f - Margin);
        }

        static string GroundKey(int gx, int gy)
        {
            if (gx == RiverX) return "tile_water";
            if (gx == LabPos.x && gy == LabPos.y) return "tile_cross";
            if (gx == LabPos.x || gy == LabPos.y) return "tile_path";
            return ((gx * 7 + gy * 3) % 5 == 0) ? "tile_grass_b" : "tile_grass";
        }

        static Color GroundColor(int gx, int gy)
        {
            if (gx == RiverX) return UIKit.Hex("#3E8ED0");
            if (gx == LabPos.x || gy == LabPos.y) return Theme.Path;
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
            content.sizeDelta = new Vector2(GridSize * TileW + 500, GridSize * TileH + Margin + 500);
            _map = content;

            var tiles = UIKit.Rect("Tiles", _map);
            UIKit.Place(tiles, new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            _objects = UIKit.Rect("Objects", _map);
            UIKit.Place(_objects, new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);

            // Zemin blokları: arkadan öne (gx+gy küçükten büyüğe) çizilir
            var cells = new List<Vector2Int>();
            for (int gy = 0; gy < GridSize; gy++)
                for (int gx = 0; gx < GridSize; gx++) cells.Add(new Vector2Int(gx, gy));
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
            foreach (var v in VillageDatabase.Villagers)
            {
                var villager = v;
                items.Add(new KeyValuePair<int, Action>(v.GridX + v.GridY, () => BuildHouse(villager)));
            }
            foreach (var kv in NodePos)
            {
                var id = kv.Key; var p = kv.Value;
                items.Add(new KeyValuePair<int, Action>(p.x + p.y, () => BuildNode(id, p)));
            }
            foreach (var kv in Decor)
            {
                var key = kv.Value; var p = kv.Key;
                items.Add(new KeyValuePair<int, Action>(p.x + p.y, () => BuildDecor(key, p)));
            }
            items.Add(new KeyValuePair<int, Action>(LabPos.x + LabPos.y, BuildLab));
            items.Sort((a, b) => a.Key.CompareTo(b.Key));
            foreach (var it in items) it.Value();
            BuildMystery();

            // Uzaklaştırılmış görünüm: köyün büyük kısmı ekrana sığsın
            const float zoom = 0.6f;
            content.localScale = Vector3.one * zoom;
            // Haritayı laboratuvara ortala
            content.anchoredPosition = new Vector2(0, ((LabPos.x + LabPos.y) * TileH * 0.5f + Margin) * zoom - 420);

            // Görev afişi (ekranın üstünde sabit)
            var banner = UIKit.Btn(Root, "QuestBanner", "", UIKit.Hex("#8E44AD"), OpenQuest, 34);
            UIKit.Wood(banner);
            _questBanner = (RectTransform)banner.transform;
            UIKit.Place(_questBanner, new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(1000, 110));
            _questText = UIKit.LabelOf(banner);

            var hint = UIKit.Txt(Root, "Hint", "Haritayı kaydır • Kaynaklara dokunarak topla • Evlere dokunup sohbet et", 26, new Color(1, 1, 1, 0.75f));
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

        void BuildHouse(VillagerData v)
        {
            var rt = ObjectRoot("House_" + v.Id, v.GridX, v.GridY, new Vector2(340, 330));
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

            // Evin önünde duran köylü (Art: villager_<kimlik>)
            var figArt = Art.Get("villager_" + v.Id);
            if (figArt != null)
            {
                var fig = Art.Image(rt, "Villager", figArt);
                UIKit.Place(fig.rectTransform, new Vector2(0, 0), new Vector2(10, -24), new Vector2(120, 140), new Vector2(0.5f, 0));
                fig.raycastTarget = false;
            }

            var label = UIKit.Txt(rt, "Name", v.NameTr, 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(label.rectTransform, new Vector2(0.5f, 0), new Vector2(0, -30), new Vector2(300, 36));
            label.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);

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

        void BuildNode(string rawId, Vector2Int p)
        {
            var raw = RawMaterialDatabase.Get(rawId);
            var rt = ObjectRoot("Node_" + rawId, p.x, p.y, new Vector2(200, 220));
            var nodeArt = Art.Get("node_" + rawId);
            Image img;
            if (nodeArt != null)
            {
                img = Art.Image(rt, "Icon", nodeArt);
                UIKit.Place(img.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(170, 150), new Vector2(0.5f, 0));
                _artNodes.Add(rawId);
                img.gameObject.AddComponent<Bouncer>().Amplitude = 6;
            }
            else
            {
                img = UIKit.Img(rt, "Icon", UIKit.Hex(raw.ColorHex), rawId == "crystal" ? UIKit.Diamond : UIKit.Circle);
                UIKit.Place(img.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 40), rawId == "crystal" ? new Vector2(80, 120) : new Vector2(110, 90), new Vector2(0.5f, 0));
                if (rawId == "crystal") img.rectTransform.localRotation = Quaternion.Euler(0, 0, 90);
            }
            img.gameObject.AddComponent<Button>().onClick.AddListener(() => TapNode(rawId, rt));
            var label = UIKit.Txt(rt, "Name", raw.NameTr, 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(label.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(300, 36));
            label.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
            var timer = UIKit.Txt(rt, "Timer", "", 26, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(timer.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 200), new Vector2(180, 40));
            timer.gameObject.AddComponent<Outline>().effectColor = Color.black;
            _nodeTimers[rawId] = timer;
            _nodeImages[rawId] = img;
        }

        void BuildDecor(string key, Vector2Int p)
        {
            var art = Art.Get(key);
            if (art == null) return;
            // Küçük dekorlar (kütük, çit, kemer) daha küçük çizilir
            float s = key == "decor_logs" ? 200 : key == "decor_fence" ? 260 : key == "decor_arch" ? 300 : key == "decor_signpost" ? 150 : 460;
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
            var label = UIKit.Txt(rt, "Name", "Aqualf'in Kulesi", 30, Theme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(label.rectTransform, new Vector2(0.5f, 0), new Vector2(0, -10), new Vector2(340, 40));
            label.gameObject.AddComponent<Outline>().effectColor = Color.black;
            tower.gameObject.AddComponent<Button>().onClick.AddListener(() => G.Show(ScreenId.Lab));
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
                G.Toast("Gizemli kutu her 6 saatte bir gelir. Sıradaki: " + _mysteryLabel.text);
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
                "Sıradaki kutu 6 saat sonra köye gelecek. Nadir elementler için 'Şans Tılsımı' yeteneği işe yarar.",
                "+" + r.Xp + " XP", "Sorchemist'te gizemli kutudan " + els + " çıktı!",
                () => { G.HandleLevelUps(r.LevelUps); Refresh(); }, Art.Has("mystery_box_2") ? "mystery_box_2" : null);
            RefreshMystery();
        }

        // ---------- Etkileşimler ----------
        void TapNode(string rawId, RectTransform rt)
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (Session.Collect(rawId, now))
            {
                AudioFX.Play(Sfx.Collect);
                Haptics.Bump();
                var raw = RawMaterialDatabase.Get(rawId);
                FX.I.Burst(_objects, UIKit.Hex(raw.ColorHex), rt.anchoredPosition + Vector2.up * 60, 20, 500);
                FX.I.FloatText(_objects, "+1 " + raw.NameTr, Color.white, rt.anchoredPosition + Vector2.up * 150);
                RefreshNodes();
            }
            else
            {
                G.Toast("Bu kaynak henüz yenilenmedi. Biraz bekle!");
            }
        }

        void TapHouse(VillagerData v)
        {
            var q = Session.ActiveQuest;
            if (q != null && q.VillagerId == v.Id) { OpenQuest(); return; }
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

        void OpenQuest()
        {
            var q = Session.ActiveQuest;
            if (q == null) { G.Toast("Bugün kapında kimse yok. Laboratuvarda deney yap!"); return; }
            var v = VillageDatabase.GetVillager(q.VillagerId);
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

            foreach (var kv in _houseViews)
            {
                kv.Value.Find("Bang").gameObject.SetActive(q != null && q.VillagerId == kv.Key);
                ApplyHouseState(kv.Value, State.GetHouse(kv.Key));
            }
            RefreshNodes();
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

        void RefreshNodes()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            foreach (var kv in _nodeTimers)
            {
                long left = State.NodeReadyAt(kv.Key) - now;
                bool ready = left <= 0;
                kv.Value.text = ready ? "Hazır!" : (left / 60) + ":" + (left % 60).ToString("00");
                kv.Value.color = ready ? Theme.Accent : Theme.Gold;
                var img = _nodeImages[kv.Key];
                var c = _artNodes.Contains(kv.Key) ? Color.white : UIKit.Hex(RawMaterialDatabase.Get(kv.Key).ColorHex);
                img.color = ready ? c : Color.Lerp(c, Color.gray, 0.6f);
            }
        }

        void Update()
        {
            _tick += Time.deltaTime;
            if (_tick > 1f) { _tick = 0; RefreshNodes(); RefreshMystery(); }
        }
    }
}
