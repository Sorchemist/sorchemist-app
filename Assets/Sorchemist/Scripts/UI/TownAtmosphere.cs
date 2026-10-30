// Kasabanın atmosferi (Assets2'deki ArcaneDayNightCycleManager, mevsim/hava rehberi ve ses mimarisinin
// uGUI tabanlı oyunumuza uyarlanmış hali):
//  - Gece/gündüz: 24 dakikalık oyun günü. Gece haritanın üstüne lacivert bir örtü iner; binaların pencereleri,
//    ocaklar ve rünler parlar (demirci ve magma ocağı titrer).
//  - Mevsimler (İlkbahar, Sonbahar, Kış, Fırtına): renk tonu, haritaya serpilen mevsim objeleri, yağan
//    yaprak/kar/yağmur parçacıkları, fırtınada şimşek ve gök gürültüsü, kışın donan nehir.
//  - Ses: gündüz/gece ambiyansı arasında yumuşak geçiş, mevsim sesi (yağmur, rüzgâr, kış), ve bazı binaların
//    2:1 izometrik elips mesafesine göre kısılan, sağ/sol kulaklığa dağılan döngü sesleri (su değirmeni, ocaklar, portal...).
//  - Sağ üstte mevsim madalyonu ve kasaba saati: dokununca mevsimin etkileri (Gözlemevi ve Rüzgâr Değirmeni Sv.2
//    olunca sıradaki mevsimin tahmini de görünür).
using System;
using System.Collections.Generic;
using Sorchemist.Core;
using Sorchemist.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Sorchemist.UI
{
    public class TownAtmosphere : MonoBehaviour
    {
        // ---- VillageScreen'in verdikleri ----
        public RectTransform Root, Map, Objects;
        public Func<int, int, Vector2> CellPos;            // karonun harita içi konumu
        public List<Vector2Int> PropCells = new List<Vector2Int>();
        public List<Image> WaterTiles = new List<Image>();
        public readonly Dictionary<string, RectTransform> Buildings = new Dictionary<string, RectTransform>();

        GameManager G { get { return GameManager.I; } }

        Image _tint, _night, _flash;
        RectTransform _glowLayer, _fx, _propLayer;
        readonly List<KeyValuePair<string, Image>> _glows = new List<KeyValuePair<string, Image>>();
        Season _season = (Season)(-1);
        Image _medal;
        Text _clock;
        float _thunderTimer = 8f, _flashA;

        static Sprite _soft;
        /// <summary>Yumuşak kenarlı ışık lekesi (çalışma anında üretilir).</summary>
        public static Sprite Soft
        {
            get
            {
                if (_soft != null) return _soft;
                const int n = 64;
                var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float dx = (x + 0.5f) / n * 2 - 1, dy = (y + 0.5f) / n * 2 - 1;
                        float a = Mathf.Clamp01(1 - Mathf.Sqrt(dx * dx + dy * dy));
                        tex.SetPixel(x, y, new Color(1, 1, 1, a * a));
                    }
                tex.Apply();
                tex.wrapMode = TextureWrapMode.Clamp;
                _soft = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100);
                return _soft;
            }
        }

        // ------------------------------------------------------------ kurulum
        public void Build()
        {
            // Mevsim objeleri: binaların arkasında kalsın diye nesne katmanının en başına
            _propLayer = UIKit.Rect("SeasonProps", Objects);
            UIKit.Place(_propLayer, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            _propLayer.SetAsFirstSibling();

            // Mevsim tonu ve gece örtüsü haritanın içinde (UI'ın değil, yalnızca dünyanın üstünde)
            _tint = UIKit.Img(Map, "SeasonTint", new Color(0, 0, 0, 0));
            UIKit.Stretch(_tint.rectTransform); _tint.raycastTarget = false;
            _night = UIKit.Img(Map, "NightShade", new Color(0.04f, 0.06f, 0.22f, 0));
            UIKit.Stretch(_night.rectTransform); _night.raycastTarget = false;

            // Gece ışıkları örtünün üstünde, haritayla birlikte kayar
            _glowLayer = UIKit.Rect("NightLights", Map);
            _glowLayer.anchorMin = Objects.anchorMin; _glowLayer.anchorMax = Objects.anchorMax;
            _glowLayer.pivot = Objects.pivot; _glowLayer.anchoredPosition = Objects.anchoredPosition; _glowLayer.sizeDelta = Objects.sizeDelta;
            foreach (var kv in Buildings) AddGlow(kv.Key, kv.Value);

            // Hava parçacıkları ekran üzerinde
            _fx = UIKit.Rect("Weather", Root);
            UIKit.Stretch(_fx);
            _flash = UIKit.Img(Root, "Lightning", new Color(0.85f, 0.9f, 1f, 0));
            UIKit.Stretch(_flash.rectTransform); _flash.raycastTarget = false;

            // Mevsim madalyonu + saat (sağ üst, üst çubuğun altında)
            var medal = UIKit.Rect("SeasonMedal", Root);
            UIKit.Place(medal, new Vector2(1, 1), new Vector2(-24, -G.TopInset - 14), new Vector2(150, 190), new Vector2(1, 1));
            _medal = UIKit.Img(medal, "Icon", Color.white);
            UIKit.Place(_medal.rectTransform, new Vector2(0.5f, 1), Vector2.zero, new Vector2(140, 140), new Vector2(0.5f, 1));
            _medal.preserveAspect = true;
            _medal.gameObject.AddComponent<Button>().onClick.AddListener(ShowSeasonInfo);
            _clock = UIKit.Txt(medal, "Clock", "", 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(_clock.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(150, 40), new Vector2(0.5f, 0));
            _clock.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);

            ApplySeason(true);
        }

        void AddGlow(string id, RectTransform view)
        {
            var g = UIKit.Img(_glowLayer, "Glow_" + id, GlowColor(id), Soft);
            g.raycastTarget = false;
            var rt = g.rectTransform;
            rt.anchorMin = rt.anchorMax = view.anchorMin; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = view.anchoredPosition + new Vector2(0, id == BuildingDatabase.Tower ? 230 : 150);
            rt.sizeDelta = id == BuildingDatabase.Tower ? new Vector2(520, 420) : new Vector2(380, 290);
            _glows.Add(new KeyValuePair<string, Image>(id, g));
        }

        static Color GlowColor(string id)
        {
            switch (id)
            {
                case "forge": case "smeltery": case "distillery": return new Color(1f, 0.5f, 0.15f, 0);
                case "portal": case "sporedome": case "crater": case "well": return new Color(0.75f, 0.35f, 1f, 0);
                case "crystal": case "observatory": case "watermill": case "library": return new Color(0.3f, 0.85f, 1f, 0);
                case "tower": return new Color(0.55f, 1f, 0.75f, 0);
                default: return new Color(1f, 0.82f, 0.45f, 0);
            }
        }

        // ------------------------------------------------------------ mevsim
        void ApplySeason(bool force)
        {
            var s = SeasonClock.At(GameManager.Now);
            if (!force && s == _season) return;
            _season = s;
            string medal = s == Season.Spring ? "season_med_spring" : s == Season.Autumn ? "season_med_autumn" : s == Season.Winter ? "season_med_winter" : "season_med_storm";
            var art = Art.Get(medal);
            _medal.sprite = art; _medal.color = art != null ? Color.white : new Color(1, 1, 1, 0);
            // Nehir kışın donar
            foreach (var w in WaterTiles) if (w != null) w.color = s == Season.Winter ? new Color(0.82f, 0.93f, 1f) : Color.white;
            BuildProps(s);
            ClearParticles();
        }

        static readonly string[][] Props =
        {
            new[] { "sp_flowers", "sp_flowers2", "sp_mushrooms", "sp_mossrock", "sp_sprout", "sp_rocks" },
            new[] { "au_leaves", "au_leaves2", "au_log", "au_log2", "au_pumpkin", "au_pumpkin2", "au_redcaps", "au_mushrooms", "au_brownshrooms" },
            new[] { "wi_icerock", "wi_ice", "wi_runestone", "wi_bush", "wi_rune", "fx_snowdrift" },
            new[] { "st_puddle", "st_rocks", "st_rocks2", "st_reeds", "st_crystal" },
        };

        void BuildProps(Season s)
        {
            UIKit.Clear(_propLayer);
            var keys = Props[(int)s];
            int i = 0;
            foreach (var c in PropCells)
            {
                string key = keys[(c.x * 7 + c.y * 13 + i) % keys.Length];
                i++;
                var art = Art.Get(key);
                if (art == null) continue;
                float h = key.StartsWith("au_log") || key == "fx_snowdrift" || key == "st_puddle" ? 120 : 170;
                float w = h * art.rect.width / art.rect.height;
                var img = Art.Image(_propLayer, key, art);
                var p = CellPos(c.x, c.y) + new Vector2(((c.x * 37 + c.y * 11) % 120) - 60, ((c.x * 13 + c.y * 29) % 50) - 40);
                UIKit.Place(img.rectTransform, new Vector2(0.5f, 0.5f), p, new Vector2(w, h), new Vector2(0.5f, 0));
                img.raycastTarget = false;
            }
        }

        public void ShowSeasonInfo()
        {
            AudioFX.Play(Sfx.Chime, 0.5f);
            long now = GameManager.Now;
            var s = SeasonClock.At(now);
            var gs = G.Session;
            bool forecast = gs.OpenLevel("observatory") >= 2 && gs.OpenLevel("windmill") >= 2;
            string body = "<b>" + SeasonClock.NameTr(s) + "</b>  <color=#A99FC9>(" + Loc.T("kalan ", "") +
                          BuildingDatabase.FormatDuration(SeasonClock.SecondsLeft(now)) + Loc.T(")", " left)") + "</color>\n\n" + SeasonClock.EffectsTr(s) + "\n\n";
            if (forecast)
            {
                var n = SeasonClock.Next(s);
                body += Loc.T("<color=#7CF2C8><b>Simyacı Barometresi:</b> sıradaki mevsim ", "<color=#7CF2C8><b>Alchemist's Barometer:</b> next season is ") +
                        "<b>" + SeasonClock.NameTr(n) + "</b>.</color>\n" + "<size=26>" + SeasonClock.EffectsTr(n) + "</size>";
            }
            else
                body += Loc.T("<size=26><color=#A99FC9>Göksel Gözlemevi ve Rüzgâr Değirmeni Sv.2 olunca sıradaki mevsimi önceden görebilirsin.</color></size>",
                              "<size=26><color=#A99FC9>Raise the Celestial Observatory and the Wind Mill to Lv.2 to see the next season in advance.</color></size>");
            body += Loc.T("\n\n<size=26>Kasaba saati: ", "\n\n<size=26>Town clock: ") + SeasonClock.Clock(now) + "</size>";
            RectTransform b;
            var modal = UIKit.Modal(G.Overlay, Loc.T("Mevsim ve Hava", "Season & Weather"), out b, 900, 1250);
            var t = UIKit.Txt(b, "Body", body, 30, Theme.Text, TextAnchor.UpperLeft);
            UIKit.Stretch(t.rectTransform, 10, 10, 10, 110);
            var close = UIKit.Wood(UIKit.Btn(b, "Close", Loc.T("Kapat", "Close"), Theme.PanelLight, () => Destroy(modal.gameObject), 30));
            UIKit.Place((RectTransform)close.transform, new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(320, 90), new Vector2(0.5f, 0));
        }

        // ------------------------------------------------------------ her kare
        void Update()
        {
            if (Map == null) return;
            long now = GameManager.Now;
            ApplySeason(false);
            float tod = SeasonClock.TimeOfDay(now);
            float night = SeasonClock.NightGlow(tod);
            _clock.text = SeasonClock.Clock(now);

            // Mevsim tonu
            Color tint;
            switch (_season)
            {
                case Season.Spring: tint = new Color(1f, 0.92f, 0.75f, 0.06f); break;
                case Season.Autumn: tint = new Color(1f, 0.55f, 0.15f, 0.12f); break;
                case Season.Winter: tint = new Color(0.86f, 0.94f, 1f, 0.24f); break;
                default: tint = new Color(0.25f, 0.3f, 0.45f, 0.32f); break;
            }
            _tint.color = tint;
            // Gece: lacivert-mor; şafak ve alacakaranlıkta pembe-turuncu
            float dusk = Mathf.Clamp01(1f - Mathf.Abs(night - 0.5f) * 2f);
            var nc = Color.Lerp(new Color(0.04f, 0.06f, 0.22f), new Color(0.45f, 0.15f, 0.35f), dusk);
            nc.a = night * 0.55f + dusk * 0.08f;
            _night.color = nc;

            // Pencere, ocak ve rün ışıkları
            for (int i = 0; i < _glows.Count; i++)
            {
                var kv = _glows[i];
                bool open = kv.Key == BuildingDatabase.Tower || G.State.IsDistrictOpen(kv.Key);
                float a = open ? night * 0.75f : 0f;
                if (kv.Key == "forge" || kv.Key == "smeltery")
                    a = open ? Mathf.Max(a, 0.18f) * (0.8f + 0.4f * Mathf.PerlinNoise(Time.time * 6f, i * 3.1f)) : 0f;   // ocak ateşi titrer
                var c = kv.Value.color; c.a = a; kv.Value.color = c;
            }

            UpdateParticles();
            UpdateAudio(night);
        }

        // ------------------------------------------------------------ parçacıklar
        class P { public RectTransform Rt; public Image Img; public Vector2 V; public float Spin, Phase; }
        readonly List<P> _parts = new List<P>();

        void ClearParticles()
        {
            foreach (var p in _parts) if (p.Rt != null) Destroy(p.Rt.gameObject);
            _parts.Clear();
        }

        int TargetCount { get { return _season == Season.Spring ? 22 : _season == Season.Autumn ? 26 : _season == Season.Winter ? 70 : 90; } }

        void Spawn(bool anywhere)
        {
            var size = _fx.rect.size;
            var p = new P();
            Color c; Vector2 sz; Sprite sp = UIKit.Circle;
            switch (_season)
            {
                case Season.Spring:
                    bool pollen = UnityEngine.Random.value < 0.4f;
                    c = pollen ? new Color(1f, 0.92f, 0.4f, 0.8f) : new Color(1f, 0.7f, 0.85f, 0.9f);
                    sz = pollen ? Vector2.one * 9 : new Vector2(22, 14); sp = pollen ? Soft : UIKit.Circle;
                    p.V = new Vector2(UnityEngine.Random.Range(20, 70), -UnityEngine.Random.Range(30, 70)); p.Spin = UnityEngine.Random.Range(-90, 90);
                    break;
                case Season.Autumn:
                    c = Color.Lerp(new Color(1f, 0.45f, 0.1f, 0.95f), new Color(0.85f, 0.2f, 0.1f, 0.95f), UnityEngine.Random.value);
                    sz = new Vector2(24, 15);
                    p.V = new Vector2(UnityEngine.Random.Range(90, 200), -UnityEngine.Random.Range(40, 90)); p.Spin = UnityEngine.Random.Range(-200, 200);
                    break;
                case Season.Winter:
                    c = new Color(1, 1, 1, UnityEngine.Random.Range(0.6f, 0.95f));
                    float r = UnityEngine.Random.Range(6, 14); sz = Vector2.one * r; sp = Soft;
                    p.V = new Vector2(UnityEngine.Random.Range(-30, 50), -UnityEngine.Random.Range(60, 130));
                    break;
                default:
                    c = new Color(0.75f, 0.85f, 1f, 0.45f);
                    sz = new Vector2(3, 46); sp = null;
                    p.V = new Vector2(-260, -1300);
                    break;
            }
            p.Img = UIKit.Img(_fx, "P", c, sp);
            p.Img.raycastTarget = false;
            p.Rt = p.Img.rectTransform;
            p.Rt.anchorMin = p.Rt.anchorMax = Vector2.zero;
            p.Rt.sizeDelta = sz;
            float x = UnityEngine.Random.Range(-100f, size.x + 100f);
            float y = anywhere ? UnityEngine.Random.Range(0, size.y) : size.y + 40;
            p.Rt.anchoredPosition = new Vector2(x, y);
            if (_season == Season.Storm) p.Rt.localRotation = Quaternion.Euler(0, 0, -11);
            p.Phase = UnityEngine.Random.value * 6f;
            _parts.Add(p);
        }

        void UpdateParticles()
        {
            if (_fx == null) return;
            if (_parts.Count < TargetCount) Spawn(_parts.Count < TargetCount / 2);
            float dt = Time.deltaTime;
            var size = _fx.rect.size;
            for (int i = _parts.Count - 1; i >= 0; i--)
            {
                var p = _parts[i];
                var pos = p.Rt.anchoredPosition + p.V * dt;
                if (_season != Season.Storm) pos.x += Mathf.Sin(Time.time * 1.7f + p.Phase) * 25f * dt;   // yaprak/kar salınımı
                p.Rt.anchoredPosition = pos;
                if (p.Spin != 0) p.Rt.Rotate(0, 0, p.Spin * dt);
                if (pos.y < -60 || pos.x > size.x + 160 || pos.x < -200)
                {
                    Destroy(p.Rt.gameObject);
                    _parts.RemoveAt(i);
                }
            }
            // Fırtınada şimşek ve gök gürültüsü
            if (_season == Season.Storm)
            {
                _thunderTimer -= dt;
                if (_thunderTimer <= 0)
                {
                    _thunderTimer = UnityEngine.Random.Range(7f, 16f);
                    _flashA = 0.7f;
                    AudioFX.PlayClip("thunder", 0.8f);
                }
            }
            if (_flashA > 0)
            {
                _flashA = Mathf.Max(0, _flashA - dt * 2.5f);
                float flicker = _flashA > 0.45f && _flashA < 0.55f ? 0.2f : _flashA;
                _flash.color = new Color(0.85f, 0.9f, 1f, flicker);
            }
        }

        // ------------------------------------------------------------ ses
        static readonly Dictionary<string, string> BuildingLoops = new Dictionary<string, string>
        {
            { "watermill", "loop_water" }, { "forge", "loop_fire" }, { "smeltery", "loop_fire" },
            { "portal", "loop_portal" }, { "distillery", "loop_bubble" }, { "workshop", "loop_bubble" },
        };

        void UpdateAudio(float night)
        {
            float day = 1f - night;
            string weather = _season == Season.Storm ? "amb_rain" : _season == Season.Winter ? "amb_winter" : _season == Season.Autumn ? "amb_wind" : null;
            AudioFX.SetLoop("amb_day", "amb_day", day * 0.35f * (_season == Season.Storm ? 0.3f : 1f), 0);
            AudioFX.SetLoop("amb_night", "amb_night", night * 0.35f, 0);
            AudioFX.SetLoop("amb_weather", weather, weather != null ? 0.32f : 0f, 0);

            // Binaların izometrik 2:1 elips mesafesine göre ses yüksekliği ve sağ/sol dengesi
            const float minD = 220, maxD = 1300;
            foreach (var kv in BuildingLoops)
            {
                RectTransform view;
                if (!Buildings.TryGetValue(kv.Key, out view) || view == null || !G.State.IsDistrictOpen(kv.Key))
                { AudioFX.SetLoop("b_" + kv.Key, kv.Value, 0, 0); continue; }
                Vector3 local = Root.InverseTransformPoint(view.position);
                float dx = local.x, dy = (local.y + 100f) * 2f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float t = Mathf.InverseLerp(minD, maxD, d);
                float vol = (1f - t * t * (3 - 2 * t)) * 0.55f;
                float pan = Mathf.Clamp(dx / maxD, -1f, 1f) * 0.65f;
                AudioFX.SetLoop("b_" + kv.Key, kv.Value, vol, pan);
            }
        }

        void OnDisable()
        {
            AudioFX.StopLoops();
            if (_flash != null) _flash.color = new Color(1, 1, 1, 0);
        }
    }
}
