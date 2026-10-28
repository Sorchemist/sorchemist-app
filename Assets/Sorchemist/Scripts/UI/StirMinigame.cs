// Karıştırma minioyunu: kazanın içinde parmağınla daireler çiz. 3 tam tur = mükemmel.
using System;
using Sorchemist.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Sorchemist.UI
{
    public class StirMinigame : MonoBehaviour
    {
        Action<float> _done;
        float _limit, _time, _angleSum, _lastAngle;
        bool _hasLast, _finished;
        RectTransform _swirl, _spoon, _pot;
        Image _fill;
        Text _status;
        Color _liquid;
        const float TargetTurns = 3f;

        public static void Run(RectTransform overlay, float limit, Color liquid, Action<float> done)
        {
            RectTransform body;
            var modal = UIKit.Modal(overlay, "Karıştır!", out body, 1400);
            var g = modal.gameObject.AddComponent<StirMinigame>();
            g._done = done; g._limit = limit; g._liquid = liquid;
            g.Build(body);
        }

        void Build(RectTransform body)
        {
            var info = UIKit.Txt(body, "Info", "Parmağınla kazanın içinde daireler çiz!", 38, Theme.TextDim);
            UIKit.Place(info.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(860, 80));

            var rim = UIKit.Img(body, "Rim", UIKit.Hex("#2B2B33"), UIKit.Circle);
            _pot = rim.rectTransform;
            UIKit.Place(_pot, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(720, 720));
            var liquid = UIKit.Img(_pot, "Liquid", _liquid, UIKit.Circle);
            UIKit.Place(liquid.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640, 640));
            liquid.raycastTarget = false;

            _swirl = UIKit.Rect("Swirl", _pot);
            UIKit.Place(_swirl, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640, 640));
            for (int i = 0; i < 6; i++)
            {
                var dot = UIKit.Img(_swirl, "d", new Color(1, 1, 1, 0.35f), UIKit.Circle);
                float a = i * Mathf.PI / 3f; float r = 120 + i * 30;
                UIKit.Place(dot.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r, Vector2.one * (60 - i * 6));
                dot.raycastTarget = false;
            }
            var spoon = UIKit.Img(_pot, "Spoon", UIKit.Hex("#C49A6C"));
            _spoon = spoon.rectTransform;
            UIKit.Place(_spoon, new Vector2(0.5f, 0.5f), new Vector2(200, 0), new Vector2(40, 260));
            spoon.raycastTarget = false;

            var drag = rim.gameObject.AddComponent<DragArea>();
            drag.OnDragLocal = OnDrag;
            drag.OnRelease = () => _hasLast = false;

            var barBg = UIKit.Img(body, "BarBg", Theme.PanelLight);
            UIKit.Place(barBg.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(800, 36));
            _fill = UIKit.Img(barBg.transform, "Fill", Theme.Accent);
            _fill.rectTransform.anchorMin = Vector2.zero; _fill.rectTransform.anchorMax = new Vector2(0, 1);
            _fill.rectTransform.offsetMin = _fill.rectTransform.offsetMax = Vector2.zero;

            _status = UIKit.Txt(body, "Status", "", 36, Theme.Text);
            UIKit.Place(_status.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 80), new Vector2(860, 60));

            var skip = UIKit.Btn(body, "Skip", "Karıştırmadan geç", Theme.PanelLight, () => Finish(0f), 32);
            UIKit.Place((RectTransform)skip.transform, new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(460, 80));
        }

        void OnDrag(Vector2 local)
        {
            if (_finished || local.magnitude < 40f) return;
            float ang = Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg;
            if (_hasLast)
            {
                float d = Mathf.DeltaAngle(_lastAngle, ang);
                if (Mathf.Abs(d) < 90f) // ani zıplamaları say(ma)
                {
                    _angleSum += Mathf.Abs(d);
                    _swirl.localRotation = Quaternion.Euler(0, 0, _swirl.localEulerAngles.z + d);
                    if (UnityEngine.Random.value < 0.15f) AudioFX.Play(Sfx.Bubble, 0.35f, 0.3f);
                }
            }
            _spoon.anchoredPosition = local.normalized * Mathf.Min(local.magnitude, 280f);
            _spoon.localRotation = Quaternion.Euler(0, 0, ang - 90f);
            _lastAngle = ang; _hasLast = true;
        }

        float Quality { get { return Mathf.Clamp01(_angleSum / (360f * TargetTurns)); } }

        void Update()
        {
            if (_finished) return;
            _time += Time.deltaTime;
            _fill.rectTransform.anchorMax = new Vector2(Quality, 1);
            _status.text = "Tur: " + (_angleSum / 360f).ToString("0.0") + " / " + TargetTurns +
                           "    Süre: " + Mathf.Max(0, _limit - _time).ToString("0.0");
            if (Quality >= 1f || _time >= _limit) Finish(Quality);
        }

        void Finish(float q)
        {
            if (_finished) return;
            _finished = true;
            if (FX.I != null) FX.I.Bubbles((RectTransform)transform.parent, _liquid, Vector2.zero, 14);
            Destroy(gameObject, 0.05f);
            if (_done != null) _done(q);
        }
    }
}
