// Ayrıştırma (damıtma/elektroliz...) minioyunu: hareket eden yeşil bandı imleçle takip et.
// Bantta geçirdiğin süre oranı = verim kalitesi.
using System;
using Sorchemist.Core;
using Sorchemist.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Sorchemist.UI
{
    public class DistillMinigame : MonoBehaviour
    {
        Action<float> _done;
        float _limit, _time, _inBand, _cursor = 0.2f, _seed;
        bool _finished;
        HoldButton _hold;
        RectTransform _track, _band, _cursorRt;
        Image _bandImg, _drop;
        Text _status;
        const float BandWidth = 0.22f;

        public static void Run(RectTransform overlay, RawMaterialData raw, float limit, Action<float> done)
        {
            RectTransform body;
            var modal = UIKit.Modal(overlay, RawMaterialDatabase.MethodNameTr(raw.Method) + ": " + raw.NameTr, out body, 1200);
            var g = modal.gameObject.AddComponent<DistillMinigame>();
            g._done = done; g._limit = limit; g._seed = UnityEngine.Random.value * 50f;
            g.Build(body, raw);
        }

        void Build(RectTransform body, RawMaterialData raw)
        {
            var info = UIKit.Txt(body, "Info", raw.FlavorTr + "\n\n<color=#7CF2C8>Basılı tutarak imleci yeşil bantta tut!</color>", 34, Theme.TextDim);
            UIKit.Place(info.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(860, 200));

            // Damıtma şişesi (dekor)
            var flask = UIKit.Img(body, "Flask", UIKit.Hex(raw.ColorHex), UIKit.Circle);
            UIKit.Place(flask.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-250, 80), new Vector2(220, 220));
            var tube = UIKit.Img(body, "Tube", new Color(1, 1, 1, 0.5f));
            UIKit.Place(tube.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(380, 18));
            tube.rectTransform.localRotation = Quaternion.Euler(0, 0, -12);
            _drop = UIKit.Img(body, "Drop", Theme.Accent, UIKit.Circle);
            UIKit.Place(_drop.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(220, 100), new Vector2(40, 40));
            var beaker = UIKit.Panel(body, "Beaker", new Color(1, 1, 1, 0.15f));
            UIKit.Place(beaker.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(230, 0), new Vector2(160, 160));

            _track = UIKit.Img(body, "Track", Theme.PanelLight).rectTransform;
            UIKit.Place(_track, new Vector2(0.5f, 0.5f), new Vector2(0, -150), new Vector2(820, 80));
            _bandImg = UIKit.Img(_track, "Band", UIKit.Hex("#5BD67A"));
            _band = _bandImg.rectTransform;
            _band.anchorMin = new Vector2(0, 0); _band.anchorMax = new Vector2(BandWidth, 1);
            _band.offsetMin = _band.offsetMax = Vector2.zero;
            _cursorRt = UIKit.Img(_track, "Cursor", Color.white).rectTransform;
            _cursorRt.anchorMin = new Vector2(0, -0.2f); _cursorRt.anchorMax = new Vector2(0, 1.2f);
            _cursorRt.sizeDelta = new Vector2(16, 0);

            _status = UIKit.Txt(body, "Status", "", 36, Theme.Text);
            UIKit.Place(_status.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, -240), new Vector2(860, 60));

            var b = UIKit.Btn(body, "Heat", "ISIT (basılı tut)", UIKit.Hex("#C0392B"), null, 42);
            UIKit.Place((RectTransform)b.transform, new Vector2(0.5f, 0), new Vector2(0, 100), new Vector2(800, 180));
            _hold = b.gameObject.AddComponent<HoldButton>();
        }

        void Update()
        {
            if (_finished) return;
            float dt = Time.deltaTime;
            _time += dt;
            _cursor = Mathf.Clamp01(_cursor + (_hold.Held ? 0.55f : -0.45f) * dt);

            float center = 0.15f + Mathf.PerlinNoise(_seed, _time * 0.45f) * 0.7f;
            float lo = center - BandWidth / 2f, hi = center + BandWidth / 2f;
            _band.anchorMin = new Vector2(lo, 0); _band.anchorMax = new Vector2(hi, 1);
            _cursorRt.anchorMin = new Vector2(_cursor, -0.2f); _cursorRt.anchorMax = new Vector2(_cursor, 1.2f);
            _cursorRt.anchoredPosition = Vector2.zero;

            bool inside = _cursor >= lo && _cursor <= hi;
            if (inside)
            {
                _inBand += dt;
                // Damla düşür (görsel tatmin)
                var p = _drop.rectTransform.anchoredPosition;
                p.y -= 260 * dt; if (p.y < 20) { p.y = 100; AudioFX.Play(Sfx.Bubble, 0.25f, 0.4f); }
                _drop.rectTransform.anchoredPosition = p;
            }
            _bandImg.color = inside ? UIKit.Hex("#7CF2C8") : UIKit.Hex("#5BD67A");
            float q = _inBand / _limit;
            _status.text = "Saflık: %" + Mathf.RoundToInt(Mathf.Clamp01(q) * 100) +
                           "    Süre: " + Mathf.Max(0, _limit - _time).ToString("0.0");
            if (_time >= _limit) Finish(Mathf.Clamp01(q * 1.25f)); // biraz cömert ol
        }

        void Finish(float q)
        {
            _finished = true;
            Destroy(gameObject, 0.05f);
            if (_done != null) _done(q);
        }
    }
}
