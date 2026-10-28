// Küçük "canlılık" animasyonu: zıplama (ünlem, uçan ev) veya parlama (pencere, ışıltı).
using UnityEngine;
using UnityEngine.UI;

namespace Sorchemist.UI
{
    public class Bouncer : MonoBehaviour
    {
        public float Amplitude = 12f;
        public float Speed = 3f;
        public bool Glow;          // true: zıplamak yerine nabız gibi parla

        RectTransform _rt;
        Vector2 _base;
        Graphic _g;
        float _phase;

        void Start()
        {
            _rt = (RectTransform)transform;
            _base = _rt.anchoredPosition;
            _g = GetComponent<Graphic>();
            _phase = Random.value * 6f;
        }

        void Update()
        {
            float s = Mathf.Sin(Time.time * Speed + _phase);
            if (Glow)
            {
                transform.localScale = Vector3.one * (1f + s * 0.08f);
                if (_g != null) { var c = _g.color; c.a = Mathf.Clamp01(0.55f + s * 0.35f); _g.color = c; }
            }
            else _rt.anchoredPosition = _base + Vector2.up * (Mathf.Abs(s) * Amplitude);
        }

        void OnDestroy()
        {
            if (_rt != null && !Glow) _rt.anchoredPosition = _base;
        }
    }
}
