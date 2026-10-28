// "Tatmin edici" görsel efektler: kabarcık, duman, patlama, kristal yağmuru, ekran sarsıntısı.
// Hepsi UI üzerinde çalışır; ileride Unity Particle System / VFX Graph ile değiştirilebilir.
using System.Collections;
using Sorchemist.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Sorchemist.UI
{
    public class FX : MonoBehaviour
    {
        public static FX I;
        void Awake() { I = this; }

        class P
        {
            public RectTransform Rt; public Image Img; public Vector2 Vel; public float Life, MaxLife;
            public float Gravity, Spin, StartScale, EndScale; public Color Col; public bool Wobble;
        }

        IEnumerator RunParticles(P[] ps)
        {
            bool alive = true;
            while (alive)
            {
                alive = false;
                float dt = Time.deltaTime;
                foreach (var p in ps)
                {
                    if (p.Rt == null || p.Life >= p.MaxLife) continue;
                    alive = true;
                    p.Life += dt;
                    float k = p.Life / p.MaxLife;
                    p.Vel.y -= p.Gravity * dt;
                    var pos = p.Rt.anchoredPosition + p.Vel * dt;
                    if (p.Wobble) pos.x += Mathf.Sin(p.Life * 9f + p.MaxLife * 13f) * 60f * dt;
                    p.Rt.anchoredPosition = pos;
                    p.Rt.localRotation = Quaternion.Euler(0, 0, p.Spin * p.Life);
                    p.Rt.localScale = Vector3.one * Mathf.Lerp(p.StartScale, p.EndScale, k);
                    var c = p.Col; c.a *= 1f - k * k; p.Img.color = c;
                    if (k >= 1f) Destroy(p.Rt.gameObject);
                }
                yield return null;
            }
        }

        P[] Spawn(RectTransform area, int count, Sprite sprite, Color col, Vector2 origin, System.Func<int, P> init)
        {
            var arr = new P[count];
            for (int i = 0; i < count; i++)
            {
                var img = UIKit.Img(area, "fx", col, sprite);
                img.raycastTarget = false;
                var p = init(i);
                p.Img = img; p.Rt = img.rectTransform; p.Col = col;
                UIKit.Place(p.Rt, new Vector2(0.5f, 0.5f), origin, Vector2.one * 40);
                arr[i] = p;
            }
            StartCoroutine(RunParticles(arr));
            return arr;
        }

        public void Bubbles(RectTransform area, Color col, Vector2 origin, int count = 18, float spread = 160, bool sound = true)
        {
            Spawn(area, count, UIKit.Circle, new Color(col.r, col.g, col.b, 0.85f), origin, i => new P
            {
                Vel = new Vector2(Random.Range(-spread, spread) * 0.3f, Random.Range(150, 380)),
                MaxLife = Random.Range(0.8f, 1.8f), StartScale = Random.Range(0.3f, 0.9f),
                EndScale = Random.Range(0.9f, 1.6f), Wobble = true
            });
            if (sound) StartCoroutine(BubbleSounds(Mathf.Min(count, 8)));
        }

        IEnumerator BubbleSounds(int n)
        {
            for (int i = 0; i < n; i++)
            {
                AudioFX.Play(Sfx.Bubble, 0.5f, 0.25f);
                yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
            }
        }

        public void Smoke(RectTransform area, Color col, Vector2 origin, int count = 14)
        {
            Spawn(area, count, UIKit.Circle, new Color(col.r, col.g, col.b, 0.6f), origin, i => new P
            {
                Vel = new Vector2(Random.Range(-90, 90), Random.Range(80, 200)),
                MaxLife = Random.Range(1.5f, 2.5f), StartScale = 1.5f, EndScale = Random.Range(5f, 8f), Wobble = true
            });
        }

        public void Burst(RectTransform area, Color col, Vector2 origin, int count = 40, float power = 900)
        {
            Spawn(area, count, UIKit.Circle, col, origin, i =>
            {
                float a = Random.Range(0, Mathf.PI * 2);
                float s = Random.Range(power * 0.3f, power);
                return new P
                {
                    Vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s, Gravity = 900,
                    MaxLife = Random.Range(0.6f, 1.3f), StartScale = Random.Range(0.4f, 1.0f), EndScale = 0.1f
                };
            });
        }

        public void CrystalRain(RectTransform area, Color col, int count = 30)
        {
            float w = area.rect.width;
            float h = area.rect.height;
            Spawn(area, count, UIKit.Diamond, col, new Vector2(0, h * 0.5f), i => new P
            {
                Vel = new Vector2(Random.Range(-60, 60), Random.Range(-500, -200)), Gravity = 300,
                MaxLife = Random.Range(1.5f, 2.6f), StartScale = Random.Range(0.6f, 1.3f), EndScale = 0.8f,
                Spin = Random.Range(-360, 360)
            });
            // Kristalleri yatayda dağıt
            foreach (Transform t in area)
            {
                var rt = t as RectTransform;
                if (rt != null && t.name == "fx" && rt.anchoredPosition.y > h * 0.49f)
                    rt.anchoredPosition = new Vector2(Random.Range(-w * 0.5f, w * 0.5f), rt.anchoredPosition.y);
            }
        }

        public void Flash(RectTransform overlay, Color col, float dur = 0.5f)
        {
            var img = UIKit.Img(overlay, "flash", col);
            img.raycastTarget = false;
            UIKit.Stretch(img.rectTransform);
            StartCoroutine(FadeOut(img, dur));
        }

        IEnumerator FadeOut(Image img, float dur)
        {
            float t = 0; Color c = img.color;
            while (t < dur && img != null)
            {
                t += Time.deltaTime;
                img.color = new Color(c.r, c.g, c.b, c.a * (1 - t / dur));
                yield return null;
            }
            if (img != null) Destroy(img.gameObject);
        }

        public void Shake(RectTransform rt, float intensity = 30, float dur = 0.5f)
        {
            StartCoroutine(DoShake(rt, intensity, dur));
        }

        IEnumerator DoShake(RectTransform rt, float intensity, float dur)
        {
            Vector2 start = rt.anchoredPosition;
            float t = 0;
            while (t < dur && rt != null)
            {
                t += Time.deltaTime;
                rt.anchoredPosition = start + Random.insideUnitCircle * intensity * (1 - t / dur);
                yield return null;
            }
            if (rt != null) rt.anchoredPosition = start;
        }

        public void FloatText(RectTransform area, string text, Color col, Vector2 origin)
        {
            var t = UIKit.Txt(area, "float", text, 48, col, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Place(t.rectTransform, new Vector2(0.5f, 0.5f), origin, new Vector2(700, 100));
            t.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.6f);
            StartCoroutine(Rise(t));
        }

        IEnumerator Rise(Text t)
        {
            float time = 0; var c = t.color;
            while (time < 1.4f && t != null)
            {
                time += Time.deltaTime;
                t.rectTransform.anchoredPosition += Vector2.up * 120 * Time.deltaTime;
                t.color = new Color(c.r, c.g, c.b, 1 - time / 1.4f);
                yield return null;
            }
            if (t != null) Destroy(t.gameObject);
        }

        /// <summary>Basit "zıplama" animasyonu (buton/ikon geri bildirimi).</summary>
        public void Pop(Transform tr, float amount = 0.2f)
        {
            StartCoroutine(DoPop(tr, amount));
        }

        IEnumerator DoPop(Transform tr, float amount)
        {
            float t = 0;
            while (t < 0.25f && tr != null)
            {
                t += Time.deltaTime;
                tr.localScale = Vector3.one * (1 + Mathf.Sin(t / 0.25f * Mathf.PI) * amount);
                yield return null;
            }
            if (tr != null) tr.localScale = Vector3.one;
        }
    }
}
