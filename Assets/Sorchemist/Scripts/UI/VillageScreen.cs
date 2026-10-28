// İzometrik "hobbit köyü" haritası: evler, toplama noktaları ve yaşayan harita durumları.
// Şu an yer tutucu şekiller kullanır. Sanat hazır olunca Image.sprite'ları değiştirmen yeterli.
using System;
using System.Collections.Generic;
using Sorchemist.Core;
using Sorchemist.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Sorchemist.UI
{
    public class VillageScreen : GameScreen
    {
        const int GridSize = 10;
        const float TileW = 200, TileH = 100;

        RectTransform _map, _objects, _questBanner;
        Text _questText;
        readonly Dictionary<string, RectTransform> _houseViews = new Dictionary<string, RectTransform>();
        readonly Dictionary<string, Text> _nodeTimers = new Dictionary<string, Text>();
        readonly Dictionary<string, Image> _nodeImages = new Dictionary<string, Image>();
        float _tick;

        // Ham madde noktalarının harita konumları (gx, gy)
        static readonly Dictionary<string, Vector2Int> NodePos = new Dictionary<string, Vector2Int>
        {
            { "seawater", new Vector2Int(9, 5) }, { "air", new Vector2Int(0, 0) },
            { "limestone", new Vector2Int(8, 1) }, { "ash", new Vector2Int(5, 1) },
            { "hematite", new Vector2Int(0, 4) }, { "sulfur", new Vector2Int(9, 0) },
            { "malachite", new Vector2Int(0, 8) }, { "sand", new Vector2Int(8, 8) },
            { "magnesite", new Vector2Int(6, 0) }, { "guano", new Vector2Int(2, 9) },
            { "dewcap", new Vector2Int(5, 8) }, { "crystal", new Vector2Int(9, 9) },
        };
        static readonly Vector2Int LabPos = new Vector2Int(3, 3);

        static Vector2 IsoToLocal(int gx, int gy)
        {
            // Haritanın tepesi (0,0) üstte ortada
            return new Vector2((gx - gy) * TileW * 0.5f, -(gx + gy) * TileH * 0.5f - 160);
        }

        public override void Build()
        {
            var bg = UIKit.Img(Root, "Sky", UIKit.Hex("#2D5A3D"));
            UIKit.Stretch(bg.rectTransform);

            RectTransform content;
            var sr = UIKit.Scroll(Root, "MapScroll", true, true, out content);
            UIKit.Stretch((RectTransform)sr.transform);
            // İçerik boyutu sabit; ContentSizeFitter'ı kapat
            Destroy(content.GetComponent<ContentSizeFitter>());
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.sizeDelta = new Vector2(GridSize * TileW + 200, GridSize * TileH + 500);
            content.anchoredPosition = Vector2.zero;
            _map = content;

            var tiles = UIKit.Rect("Tiles", _map);
            UIKit.Place(tiles, new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            _objects = UIKit.Rect("Objects", _map);
            UIKit.Place(_objects, new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);

            // Zemin karoları
            for (int gy = 0; gy < GridSize; gy++)
                for (int gx = 0; gx < GridSize; gx++)
                {
                    Color c = (gx + gy) % 2 == 0 ? Theme.Grass : Theme.GrassDark;
                    if (gx == GridSize - 1) c = UIKit.Hex("#3E8ED0");                  // deniz kıyısı
                    else if (gx == LabPos.x || gy == LabPos.y) c = Theme.Path;           // patika
                    var t = UIKit.Img(tiles, "T", c, UIKit.Diamond);
                    t.raycastTarget = false;
                    UIKit.Place(t.rectTransform, new Vector2(0.5f, 0.5f), IsoToLocal(gx, gy), new Vector2(TileW + 2, TileH + 2));
                }

            // Nesneleri derinliğe göre sırala (arkadakiler önce çizilir)
            var items = new List<KeyValuePair<int, Action>>();
            foreach (var v in VillageDatabase.Villagers)
            {
                var villager = v;
                items.Add(new KeyValuePair<int, Action>(v.GridX + 1 + v.GridY + 1, () => BuildHouse(villager)));
            }
            foreach (var kv in NodePos)
            {
                var id = kv.Key; var p = kv.Value;
                items.Add(new KeyValuePair<int, Action>(p.x + p.y, () => BuildNode(id, p)));
            }
            items.Add(new KeyValuePair<int, Action>(LabPos.x + LabPos.y, BuildLab));
            items.Sort((a, b) => a.Key.CompareTo(b.Key));
            foreach (var it in items) it.Value();

            // Görev afişi (ekranın üstünde sabit)
            var banner = UIKit.Btn(Root, "QuestBanner", "", UIKit.Hex("#8E44AD"), OpenQuest, 36);
            _questBanner = (RectTransform)banner.transform;
            UIKit.Place(_questBanner, new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(1000, 130));
            _questText = UIKit.LabelOf(banner);

            var hint = UIKit.Txt(Root, "Hint", "Haritayı kaydır • Kaynaklara dokunarak topla", 28, new Color(1, 1, 1, 0.6f));
            UIKit.Place(hint.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(1000, 50));
        }

        RectTransform ObjectRoot(string name, Vector2 pos, Vector2 size)
        {
            var rt = UIKit.Rect(name, _objects);
            UIKit.Place(rt, new Vector2(0.5f, 0.5f), pos, size, new Vector2(0.5f, 0.15f));
            return rt;
        }

        void BuildHouse(VillagerData v)
        {
            var rt = ObjectRoot("House_" + v.Id, IsoToLocal(v.GridX + 1, v.GridY + 1), new Vector2(170, 200));
            var wall = UIKit.Panel(rt, "Wall", UIKit.Hex(v.ColorHex));
            UIKit.Place(wall.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(130, 100), new Vector2(0.5f, 0));
            var roof = UIKit.Img(rt, "Roof", UIKit.Hex("#7B3F00"), UIKit.Triangle);
            UIKit.Place(roof.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(170, 90), new Vector2(0.5f, 0));
            var door = UIKit.Img(rt, "Door", UIKit.Hex("#4A2C12"), UIKit.Circle);
            UIKit.Place(door.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(46, 70), new Vector2(0.5f, 0));

            var label = UIKit.Txt(rt, "Name", v.NameTr, 26, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(label.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 25), new Vector2(260, 40));
            label.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.7f);

            var state = UIKit.Rect("State", rt);           // Yaşayan harita süslemeleri buraya
            UIKit.Stretch(state);

            var bang = UIKit.Txt(rt, "Bang", "!", 90, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(bang.rectTransform, new Vector2(0.5f, 1), new Vector2(0, 60), new Vector2(80, 110));
            bang.gameObject.AddComponent<Outline>().effectColor = Color.black;
            bang.gameObject.AddComponent<Bouncer>();

            var btn = wall.gameObject.AddComponent<Button>();
            btn.onClick.AddListener(() => TapHouse(v));
            roof.gameObject.AddComponent<Button>().onClick.AddListener(() => TapHouse(v));
            _houseViews[v.Id] = rt;
        }

        void BuildNode(string rawId, Vector2Int p)
        {
            var raw = RawMaterialDatabase.Get(rawId);
            var rt = ObjectRoot("Node_" + rawId, IsoToLocal(p.x, p.y), new Vector2(160, 160));
            var img = UIKit.Img(rt, "Icon", UIKit.Hex(raw.ColorHex), rawId == "crystal" ? UIKit.Diamond : UIKit.Circle);
            UIKit.Place(img.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 40), rawId == "crystal" ? new Vector2(80, 120) : new Vector2(100, 80), new Vector2(0.5f, 0));
            if (rawId == "crystal") img.rectTransform.localRotation = Quaternion.Euler(0, 0, 90);
            img.gameObject.AddComponent<Button>().onClick.AddListener(() => TapNode(rawId, rt));
            var label = UIKit.Txt(rt, "Name", raw.NameTr, 24, Color.white, TextAnchor.MiddleCenter);
            UIKit.Place(label.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 15), new Vector2(240, 36));
            label.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.7f);
            var timer = UIKit.Txt(rt, "Timer", "", 26, Theme.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(timer.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 140), new Vector2(160, 40));
            timer.gameObject.AddComponent<Outline>().effectColor = Color.black;
            _nodeTimers[rawId] = timer;
            _nodeImages[rawId] = img;
        }

        void BuildLab()
        {
            var rt = ObjectRoot("Lab", IsoToLocal(LabPos.x, LabPos.y), new Vector2(200, 320));
            var tower = UIKit.Panel(rt, "Tower", UIKit.Hex("#5B2C83"));
            UIKit.Place(tower.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(130, 200), new Vector2(0.5f, 0));
            var roof = UIKit.Img(rt, "Roof", UIKit.Hex("#7CF2C8"), UIKit.Triangle);
            UIKit.Place(roof.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 250), new Vector2(170, 150), new Vector2(0.5f, 0));
            var win = UIKit.Img(rt, "Window", Theme.Gold, UIKit.Circle);
            UIKit.Place(win.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 180), new Vector2(50, 50), new Vector2(0.5f, 0));
            win.gameObject.AddComponent<Bouncer>().Glow = true;
            var label = UIKit.Txt(rt, "Name", "Aqualf'in Kulesi", 28, Theme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(label.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 25), new Vector2(300, 40));
            label.gameObject.AddComponent<Outline>().effectColor = Color.black;
            tower.gameObject.AddComponent<Button>().onClick.AddListener(() => G.Show(ScreenId.Lab));
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
                default: line = "Merhaba simyacı! Bugün sana bir işim yok."; break;
            }
            DialoguePanel.Show(G.Overlay, v.NameTr, v.RoleTr, v.ColorHex, new[] { line }, null);
        }

        void OpenQuest()
        {
            var q = Session.ActiveQuest;
            if (q == null) { G.Toast("Bugün kapında kimse yok. Laboratuvarda deney yap!"); return; }
            var v = VillageDatabase.GetVillager(q.VillagerId);
            DialoguePanel.Show(G.Overlay, v.NameTr, v.RoleTr, v.ColorHex, q.DialogueTr, null,
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
                    "Aqualf'in iksiri " + v.NameTr + "'nin derdine deva oldu!", () => { G.HandleLevelUps(rep.LevelUps); Refresh(); });
            }
            else if (o.Fail != null)
            {
                AudioFX.Play(Sfx.Boom, 0.8f);
                Haptics.Bump();
                FX.I.Shake(_map, 25, 0.6f);
                FX.I.Smoke(_objects, Color.white, at, 16);
                ResultPopup.Show(o.Fail.TitleTr, ResultPopup.RarityColor(o.Fail.Rarity), o.Fail.DescriptionTr, o.Fail.ScienceTr,
                    rep.NewFail ? "YENİ FAIL-DEX GİRDİSİ! (" + ResultPopup.RarityName(o.Fail.Rarity) + ")" : null,
                    "Fındıkdere'de " + o.Fail.TitleTr + " vakası! Aqualf yine yaptı...", () => { G.HandleLevelUps(rep.LevelUps); Refresh(); });
            }
            else
            {
                AudioFX.Play(Sfx.Fail, 0.7f);
                ResultPopup.Show("Pek İşe Yaramadı", Theme.TextDim, o.MessageTr, null, null, "Aqualf'in iksiri tutmadı!",
                    () => { G.HandleLevelUps(rep.LevelUps); Refresh(); });
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
                        UIKit.Place(p.rectTransform, new Vector2(0.5f, 0), new Vector2(-60 + i * 60, 50 + (i % 2) * 40), Vector2.one * (90 + i * 20));
                    }
                    break;
                case HouseState.Frog:
                    var frog = UIKit.Img(deco, "Frog", UIKit.Hex("#3DBE4A"), UIKit.Circle);
                    UIKit.Place(frog.rectTransform, new Vector2(0.5f, 0), new Vector2(90, 30), new Vector2(70, 55));
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
                        UIKit.Place(f.rectTransform, new Vector2(0.5f, 0), new Vector2(-75 + i * 30, 40 + (i % 3) * 15), Vector2.one * 60);
                    }
                    break;
                case HouseState.Glowing:
                    var g = UIKit.Img(deco, "Glow", new Color(1f, 0.95f, 0.5f, 0.45f), UIKit.Circle);
                    UIKit.Place(g.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 120), Vector2.one * 260);
                    g.transform.SetAsFirstSibling();
                    g.gameObject.AddComponent<Bouncer>().Glow = true;
                    break;
                case HouseState.Pink: tint = UIKit.Hex("#FF9EC7"); break;
                case HouseState.Fireworks:
                    tint = UIKit.Hex("#555555");
                    break;
                case HouseState.Happy:
                    var heart = UIKit.Txt(deco, "Happy", "<3", 40, Theme.Danger, TextAnchor.MiddleCenter, FontStyle.Bold);
                    UIKit.Place(heart.rectTransform, new Vector2(0.5f, 0), new Vector2(80, 180), new Vector2(80, 60));
                    break;
            }
            var v = VillageDatabase.GetVillager(house.name.Substring("House_".Length));
            wall.color = UIKit.Hex(v.ColorHex) * tint;
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
                var c = UIKit.Hex(RawMaterialDatabase.Get(kv.Key).ColorHex);
                img.color = ready ? c : Color.Lerp(c, Color.gray, 0.6f);
            }
        }

        void Update()
        {
            _tick += Time.deltaTime;
            if (_tick > 1f) { _tick = 0; RefreshNodes(); }
        }
    }
}
