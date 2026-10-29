// Bunsen beki minioyunu: körüğe basılı tutarak/ritmik dokunarak ısıyı istediğin bölgede 2 sn tut.
using System;
using Sorchemist.Core;
using Sorchemist.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Sorchemist.UI
{
    public class HeatMinigame : MonoBehaviour
    {
        static readonly Color[] ZoneColors =
        {
            UIKit.Hex("#4FA3FF"), UIKit.Hex("#5BD67A"), UIKit.Hex("#FFA94D"), UIKit.Hex("#FF4D4D")
        };

        Action<HeatLevel> _done;
        HoldButton _bellows;
        RectTransform _needle, _flame, _gauge;
        Image _flameImg, _lockFill;
        Text _status, _timer;
        float _temp = 0.1f, _time, _limit, _lockTime, _noiseSeed;
        int _zone = -1;
        bool _finished;
        bool _purple;
        const float LockNeeded = 2f;

        /// <summary>highlight: Termometre yeteneği varsa ve tarif biliniyorsa ideal bölge (-1 = yok)</summary>
        public static void Run(RectTransform overlay, float limit, int highlight, bool purple, Action<HeatLevel> done)
        {
            RectTransform body;
            var modal = UIKit.Modal(overlay, "Bunsen Beki", out body, 900, 1300);
            var g = modal.gameObject.AddComponent<HeatMinigame>();
            g._done = done; g._limit = limit; g._purple = purple;
            g._noiseSeed = UnityEngine.Random.value * 100f;
            g.Build(body, highlight);
        }

        void Build(RectTransform body, int highlight)
        {
            // Yatay düzen: solda termometre, ortada ocak, sağda kontroller
            var info = UIKit.Txt(body, "Info", "Körüğe basılı tut ya da ritmik dokun. İstediğin bölgede 2 saniye kal!", 34, Theme.TextDim);
            UIKit.Place(info.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(1180, 60));

            _gauge = UIKit.Rect("Gauge", body);
            UIKit.Place(_gauge, new Vector2(0.5f, 0.5f), new Vector2(-470, -30), new Vector2(110, 600));
            string[] names = { "Soğuk", "Ilık", "Sıcak", "Cehennem" };
            for (int i = 0; i < 4; i++)
            {
                var z = UIKit.Img(_gauge, "Zone" + i, ZoneColors[i]);
                z.rectTransform.anchorMin = new Vector2(0, i * 0.25f);
                z.rectTransform.anchorMax = new Vector2(1, (i + 1) * 0.25f);
                z.rectTransform.offsetMin = new Vector2(0, 2); z.rectTransform.offsetMax = new Vector2(0, -2);
                if (highlight >= 0 && highlight != i) z.color = Color.Lerp(ZoneColors[i], Color.black, 0.55f);
                var l = UIKit.Txt(z.transform, "L", names[i] + (highlight == i ? "  <b>(hedef)</b>" : ""), 30, Theme.Text, TextAnchor.MiddleLeft);
                l.rectTransform.anchorMin = new Vector2(1, 0); l.rectTransform.anchorMax = new Vector2(1, 1);
                l.rectTransform.pivot = new Vector2(0, 0.5f);
                l.rectTransform.anchoredPosition = new Vector2(20, 0); l.rectTransform.sizeDelta = new Vector2(300, 0);
                var hi = Art.Get("heat_" + i);   // Crafting.jpg alev ikonları (1 küçük, 2 orta, 3 büyük)
                if (hi != null)
                {
                    var ic = Art.Image(z.transform, "Icon", hi);
                    ic.rectTransform.anchorMin = ic.rectTransform.anchorMax = new Vector2(1, 0.5f);
                    ic.rectTransform.pivot = new Vector2(0, 0.5f);
                    ic.rectTransform.anchoredPosition = new Vector2(14, 0); ic.rectTransform.sizeDelta = new Vector2(62, 62);
                    l.rectTransform.anchoredPosition = new Vector2(86, 0);
                }
            }
            var needle = UIKit.Img(_gauge, "Needle", Color.white);
            _needle = needle.rectTransform;
            _needle.anchorMin = new Vector2(-0.15f, 0); _needle.anchorMax = new Vector2(1.15f, 0);
            _needle.sizeDelta = new Vector2(0, 14); _needle.pivot = new Vector2(0.5f, 0.5f);

            // Ocak ve alev (Art: flame, burner)
            var flameSprite = Art.Get("flame");
            _flameImg = UIKit.Img(body, "Flame", _purple ? Theme.Magic : (flameSprite != null ? Color.white : UIKit.Hex("#FF9F1C")),
                flameSprite ?? UIKit.Triangle);
            _flame = _flameImg.rectTransform;
            UIKit.Place(_flame, new Vector2(0.5f, 0.5f), new Vector2(40, -160), new Vector2(160, 200), new Vector2(0.5f, 0));
            var burnerSprite = Art.Get("burner");
            var burner = burnerSprite != null ? Art.Image(body, "Burner", burnerSprite) : UIKit.Panel(body, "Burner", UIKit.Hex("#6C6C7A"));
            UIKit.Place(burner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(40, -230), new Vector2(160, 140));

            // Sağ panel: durum, süre, kilit çubuğu, körük
            _status = UIKit.Txt(body, "Status", "", 48, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(_status.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(390, 210), new Vector2(440, 80));
            _timer = UIKit.Txt(body, "Timer", "", 30, Theme.TextDim);
            UIKit.Place(_timer.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(390, 150), new Vector2(440, 50));
            var lockBg = UIKit.Img(body, "LockBg", Theme.PanelLight);
            UIKit.Place(lockBg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(390, 95), new Vector2(420, 30));
            _lockFill = UIKit.Img(lockBg.transform, "Fill", Theme.Accent);
            _lockFill.rectTransform.anchorMin = Vector2.zero; _lockFill.rectTransform.anchorMax = new Vector2(0, 1);
            _lockFill.rectTransform.offsetMin = _lockFill.rectTransform.offsetMax = Vector2.zero;

            var b = UIKit.Btn(body, "Bellows", "KÖRÜK\n<size=30>(basılı tut)</size>", UIKit.Hex("#C0392B"), null, 52);
            UIKit.Wood(b, UIKit.Hex("#5A1E00"), UIKit.Hex("#FF9A7A"));
            UIKit.Place((RectTransform)b.transform, new Vector2(0.5f, 0.5f), new Vector2(390, -140), new Vector2(440, 300));
            _bellows = b.gameObject.AddComponent<HoldButton>();
        }

        void Update()
        {
            if (_finished) return;
            float dt = Time.deltaTime;
            _time += dt;

            // Fizik: körük ısıtır, bırakınca soğur, hafif titreşim (alev dalgalanması)
            _temp += (_bellows.Held ? 0.42f : -0.28f) * dt;
            _temp += (Mathf.PerlinNoise(_noiseSeed, _time * 1.3f) - 0.5f) * 0.25f * dt;
            _temp = Mathf.Clamp01(_temp);

            _needle.anchoredPosition = new Vector2(0, _temp * _gauge.rect.height);
            float flicker = 1f + Mathf.Sin(_time * 25f) * 0.06f;
            _flame.sizeDelta = new Vector2(100 + _temp * 120, (60 + _temp * 300) * flicker);

            int zone = Mathf.Min(3, (int)(_temp * 4f));
            if (zone != _zone) { _zone = zone; _lockTime = 0; }
            else _lockTime += dt;

            if (UnityEngine.Random.value < _temp * 0.08f) AudioFX.Play(Sfx.Bubble, 0.3f, 0.3f);

            _lockFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(_lockTime / LockNeeded), 1);
            _status.text = CompoundDatabase.HeatNameTr((HeatLevel)zone);
            _status.color = ZoneColors[zone];
            _timer.text = "Kalan süre: " + Mathf.Max(0, _limit - _time).ToString("0.0") + " sn";

            if (_lockTime >= LockNeeded || _time >= _limit) Finish((HeatLevel)zone);
        }

        void Finish(HeatLevel h)
        {
            _finished = true;
            AudioFX.Play(Sfx.Sizzle, 0.6f);
            Destroy(gameObject, 0.05f);
            if (_done != null) _done(h);
        }
    }
}
