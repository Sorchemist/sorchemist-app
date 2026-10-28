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
            var modal = UIKit.Modal(overlay, "Bunsen Beki", out body, 1500);
            var g = modal.gameObject.AddComponent<HeatMinigame>();
            g._done = done; g._limit = limit; g._purple = purple;
            g._noiseSeed = UnityEngine.Random.value * 100f;
            g.Build(body, highlight);
        }

        void Build(RectTransform body, int highlight)
        {
            var info = UIKit.Txt(body, "Info", "Körüğe basılı tut ya da ritmik dokun.\nİstediğin bölgede 2 saniye kal!", 36, Theme.TextDim);
            UIKit.Place(info.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(860, 110));

            // Termometre
            _gauge = UIKit.Rect("Gauge", body);
            UIKit.Place(_gauge, new Vector2(0.5f, 0.5f), new Vector2(-220, 30), new Vector2(140, 800));
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
                l.rectTransform.anchoredPosition = new Vector2(20, 0); l.rectTransform.sizeDelta = new Vector2(320, 0);
            }
            var needle = UIKit.Img(_gauge, "Needle", Color.white);
            _needle = needle.rectTransform;
            _needle.anchorMin = new Vector2(-0.15f, 0); _needle.anchorMax = new Vector2(1.15f, 0);
            _needle.sizeDelta = new Vector2(0, 14); _needle.pivot = new Vector2(0.5f, 0.5f);

            // Alev
            _flameImg = UIKit.Img(body, "Flame", _purple ? Theme.Magic : UIKit.Hex("#FF9F1C"), UIKit.Triangle);
            _flame = _flameImg.rectTransform;
            UIKit.Place(_flame, new Vector2(0.5f, 0.5f), new Vector2(200, -150), new Vector2(160, 200), new Vector2(0.5f, 0));
            var burner = UIKit.Panel(body, "Burner", UIKit.Hex("#6C6C7A"));
            UIKit.Place(burner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(200, -200), new Vector2(120, 100));

            // Kilit ilerleme çubuğu
            var lockBg = UIKit.Img(body, "LockBg", Theme.PanelLight);
            UIKit.Place(lockBg.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 330), new Vector2(800, 30));
            _lockFill = UIKit.Img(lockBg.transform, "Fill", Theme.Accent);
            _lockFill.rectTransform.anchorMin = Vector2.zero; _lockFill.rectTransform.anchorMax = new Vector2(0, 1);
            _lockFill.rectTransform.offsetMin = _lockFill.rectTransform.offsetMax = Vector2.zero;

            _status = UIKit.Txt(body, "Status", "", 40, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(_status.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 390), new Vector2(860, 70));
            _timer = UIKit.Txt(body, "Timer", "", 32, Theme.TextDim);
            UIKit.Place(_timer.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 280), new Vector2(860, 50));

            var b = UIKit.Btn(body, "Bellows", "KÖRÜK (basılı tut)", UIKit.Hex("#C0392B"), null, 44);
            UIKit.Place((RectTransform)b.transform, new Vector2(0.5f, 0), new Vector2(0, 120), new Vector2(800, 200));
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
            _flame.sizeDelta = new Vector2(100 + _temp * 120, (60 + _temp * 380) * flicker);

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
