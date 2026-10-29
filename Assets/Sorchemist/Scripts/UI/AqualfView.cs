// Aqualf'in yer tutucu (placeholder) görünümü ve tepkileri.
// 3D/2D modelin hazır olduğunda bu sınıfın yerine Animator tetikleyen bir sürüm yazabilirsin;
// React() çağrıları aynı kalır.
using System.Collections;
using Sorchemist.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Sorchemist.UI
{
    public enum AqualfMood { Idle, Focus, Happy, Shocked, Singed, Glowing }

    public class AqualfView : MonoBehaviour
    {
        Image _robe, _head, _hat, _beard, _glow, _sprite;
        bool _useArt;
        Text _face, _bubble;
        RectTransform _root;
        Coroutine _anim;

        public static AqualfView Create(Transform parent, Vector2 anchor, Vector2 pos, float scale = 1f)
        {
            var rt = UIKit.Rect("Aqualf", parent);
            UIKit.Place(rt, anchor, pos, new Vector2(260, 420));
            rt.localScale = Vector3.one * scale;
            var v = rt.gameObject.AddComponent<AqualfView>();
            v.Build(rt);
            return v;
        }

        void Build(RectTransform rt)
        {
            _root = rt;
            _glow = UIKit.Img(rt, "Glow", new Color(0.5f, 1f, 0.6f, 0f), UIKit.Circle);
            UIKit.Place(_glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(420, 420));
            _robe = UIKit.Img(rt, "Robe", UIKit.Hex("#3E7CB1"), UIKit.Triangle);
            UIKit.Place(_robe.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 110), new Vector2(240, 230));
            _beard = UIKit.Img(rt, "Beard", UIKit.Hex("#EDEDED"), UIKit.Triangle);
            UIKit.Place(_beard.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 195), new Vector2(110, 130));
            _beard.rectTransform.localRotation = Quaternion.Euler(0, 0, 180); // ters üçgen sakal
            _head = UIKit.Img(rt, "Head", UIKit.Hex("#F6D7B0"), UIKit.Circle);
            UIKit.Place(_head.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 270), new Vector2(130, 130));
            _hat = UIKit.Img(rt, "Hat", UIKit.Hex("#5B2C83"), UIKit.Triangle);
            UIKit.Place(_hat.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 375), new Vector2(170, 170));
            _face = UIKit.Txt(rt, "Face", "^_^", 34, UIKit.Hex("#3A2A1A"), TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(_face.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 275), new Vector2(160, 60));
            // Gerçek sanat varsa (SorchemistArt/aqualf_idle.png) yer tutucu şekilleri gizle
            var idle = Art.Get("aqualf_idle");
            _useArt = idle != null;
            if (_useArt)
            {
                foreach (var g in new Graphic[] { _robe, _beard, _head, _hat, _face }) g.gameObject.SetActive(false);
                _sprite = Art.Image(rt, "Sprite", idle);
                UIKit.Stretch(_sprite.rectTransform, -40, -30, -40, 0);
            }
            _bubble = UIKit.Txt(rt, "Bubble", "", 30, Theme.Text, TextAnchor.MiddleCenter, FontStyle.Italic);
            UIKit.Place(_bubble.rectTransform, new Vector2(0.5f, 1), new Vector2(0, 40), new Vector2(420, 90));
            SetMood(AqualfMood.Idle);
            StartCoroutine(Breathe());
        }

        IEnumerator Breathe()
        {
            float t = 0;
            while (true)
            {
                t += Time.deltaTime;
                if (_anim == null) _root.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.5f) * 2f);
                _hat.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 2.1f) * 4f);
                yield return null;
            }
        }

        public void SetMood(AqualfMood m)
        {
            switch (m)
            {
                case AqualfMood.Idle: _face.text = "^_^"; break;
                case AqualfMood.Focus: _face.text = "o_o"; break;
                case AqualfMood.Happy: _face.text = "^o^"; break;
                case AqualfMood.Shocked: _face.text = "O_O"; break;
                case AqualfMood.Singed: _face.text = "x_x"; break;
                case AqualfMood.Glowing: _face.text = "*_*"; break;
            }
            if (_useArt)
                _sprite.sprite = Art.First("aqualf_" + m.ToString().ToLowerInvariant(), "aqualf_idle");
            _glow.color = m == AqualfMood.Glowing ? new Color(0.5f, 1f, 0.6f, 0.45f) : new Color(0, 0, 0, 0);
        }

        public void Say(string line)
        {
            _bubble.text = line;
            CancelInvoke("ClearBubble");
            Invoke("ClearBubble", 3.5f);
        }

        void ClearBubble() { _bubble.text = ""; }

        /// <summary>Tepkime sonucuna göre Aqualf'in tepkisi (viral "an"ların kalbi).</summary>
        public void React(BrewResult r)
        {
            if (_anim != null) StopCoroutine(_anim);
            _hat.color = UIKit.Hex("#5B2C83");
            _beard.color = UIKit.Hex("#EDEDED");
            if (_useArt) _sprite.color = Color.white;
            if (r.Success) { SetMood(AqualfMood.Happy); Say("Eureka!"); _anim = StartCoroutine(Jump()); return; }

            FailVisual v = r.Fail != null ? r.Fail.Visual : FailVisual.Sludge;
            switch (v)
            {
                case FailVisual.HairOnFire:
                    SetMood(AqualfMood.Singed); Say(Loc.T("Saçım! SAÇIM!", "My hair! MY HAIR!"));
                    _anim = StartCoroutine(HairFire());
                    break;
                case FailVisual.RoofBlown:
                case FailVisual.Explosion:
                    SetMood(AqualfMood.Singed); Say(Loc.T("...Bu planlanmıştı.", "...That was planned."));
                    _beard.color = UIKit.Hex("#555555");
                    if (_useArt) _sprite.color = UIKit.Hex("#9A8F8A");   // isli
                    _anim = StartCoroutine(Jump());
                    break;
                case FailVisual.Glow:
                    SetMood(AqualfMood.Glowing); Say(Loc.T("Kendimi... parlak hissediyorum.", "I feel... bright."));
                    _beard.color = UIKit.Hex("#9CFF8A");
                    if (_useArt) _sprite.color = UIKit.Hex("#B8FFB0");
                    break;
                case FailVisual.GoldFlash:
                    SetMood(AqualfMood.Shocked); Say(Loc.T("ALTIN! ...değilmiş.", "GOLD! ...or not."));
                    _hat.color = Theme.Gold; _beard.color = Theme.Gold;
                    if (_useArt) _sprite.color = UIKit.Hex("#FFE07A");
                    break;
                case FailVisual.Nothing:
                    SetMood(AqualfMood.Idle); Say(Loc.T("Soylu ama sıkıcı.", "Noble but boring."));
                    break;
                case FailVisual.Smoke:
                    SetMood(AqualfMood.Singed); Say(Loc.T("*öksürük öksürük*", "*cough cough*"));
                    _hat.color = UIKit.Hex("#9A9A9A");
                    if (_useArt) _sprite.color = UIKit.Hex("#B0B0B0");
                    break;
                default:
                    SetMood(AqualfMood.Shocked); Say(Loc.T("Hmm. Not alındı.", "Hmm. Noted."));
                    break;
            }
        }

        IEnumerator Jump()
        {
            Vector2 start = _root.anchoredPosition;
            for (int i = 0; i < 2; i++)
            {
                float t = 0;
                while (t < 0.35f)
                {
                    t += Time.deltaTime;
                    _root.anchoredPosition = start + Vector2.up * Mathf.Sin(t / 0.35f * Mathf.PI) * 60f;
                    yield return null;
                }
            }
            _root.anchoredPosition = start;
            _anim = null;
        }

        IEnumerator HairFire()
        {
            float t = 0;
            while (t < 3f)
            {
                t += Time.deltaTime;
                if (FX.I != null && Random.value < 0.4f)
                {
                    var c = Random.value < 0.5f ? UIKit.Hex("#FF9F1C") : UIKit.Hex("#FFD166");
                    FX.I.Bubbles(_root, c, new Vector2(Random.Range(-40, 40), 170), 1, 40, false);
                }
                _root.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 30f) * 6f);
                yield return null;
            }
            _root.localRotation = Quaternion.identity;
            _anim = null;
        }
    }
}
