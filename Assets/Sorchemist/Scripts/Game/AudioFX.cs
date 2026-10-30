// Prosedürel (kodla üretilen) ses efektleri: kabarcık, patlama, çan, cızırtı...
// Gerçek ASMR sesleri kaydettiğinde bunları AudioClip alanlarıyla değiştirebilirsin.
using UnityEngine;

namespace Sorchemist.Game
{
    public enum Sfx { Click, Bubble, Pop, Boom, Chime, Sizzle, Fail, Collect, LevelUp, Coins }

    public class AudioFX : MonoBehaviour
    {
        public static AudioFX I;
        public bool Enabled = true;

        const int Rate = 44100;
        AudioSource _src, _buildingSrc;
        AudioClip[] _clips;
        readonly System.Collections.Generic.Dictionary<string, AudioClip> _loaded = new System.Collections.Generic.Dictionary<string, AudioClip>();
        const string Folder = "SorchemistSfx/";

        void Awake()
        {
            I = this;
            _src = gameObject.AddComponent<AudioSource>();
            _src.playOnAwake = false;
            _buildingSrc = gameObject.AddComponent<AudioSource>();
            _buildingSrc.playOnAwake = false;
            int n = System.Enum.GetValues(typeof(Sfx)).Length;
            _clips = new AudioClip[n];
            _clips[(int)Sfx.Click] = Make("click", 0.05f, (t, i) => Mathf.Sin(2 * Mathf.PI * 1400 * t) * Env(t, 0.05f, 60));
            _clips[(int)Sfx.Bubble] = Make("bubble", 0.12f, (t, i) =>
            {
                float f = 250 + 900 * t / 0.12f;               // yukarı kayan "blup"
                return Mathf.Sin(2 * Mathf.PI * f * t) * Env(t, 0.12f, 25) * 0.8f;
            });
            _clips[(int)Sfx.Pop] = Make("pop", 0.08f, (t, i) => Noise(i) * Env(t, 0.08f, 70));
            _clips[(int)Sfx.Boom] = MakeBoom();
            _clips[(int)Sfx.Chime] = Make("chime", 1.2f, (t, i) =>
                (Mathf.Sin(2 * Mathf.PI * 880 * t) + 0.6f * Mathf.Sin(2 * Mathf.PI * 1318.5f * t) +
                 0.4f * Mathf.Sin(2 * Mathf.PI * 1760 * t)) * 0.35f * Env(t, 1.2f, 4));
            _clips[(int)Sfx.Sizzle] = Make("sizzle", 0.7f, (t, i) => Noise(i) * 0.25f * Mathf.Sin(Mathf.PI * t / 0.7f));
            _clips[(int)Sfx.Fail] = Make("fail", 1.0f, (t, i) =>
            {
                // "vaa-vaa-vaaa" inen ton
                float f = t < 0.3f ? 392 : t < 0.6f ? 370 : 330 - 30 * (t - 0.6f);
                return Mathf.Sign(Mathf.Sin(2 * Mathf.PI * f * t)) * 0.15f * Env(t, 1.0f, 2.5f);
            });
            _clips[(int)Sfx.Collect] = Make("collect", 0.25f, (t, i) =>
                Mathf.Sin(2 * Mathf.PI * (t < 0.1f ? 988 : 1319) * t) * 0.4f * Env(t, 0.25f, 10));
            _clips[(int)Sfx.LevelUp] = Make("levelup", 0.9f, (t, i) =>
            {
                float[] notes = { 523, 659, 784, 1047 };
                int k = Mathf.Min(3, (int)(t / 0.15f));
                return Mathf.Sin(2 * Mathf.PI * notes[k] * t) * 0.35f * Env(t, 0.9f, 3);
            });
            // Kayıtlı (önceden üretilmiş) gerçekçi sikke sesi; yoksa basit "şing"
            _clips[(int)Sfx.Coins] = Load("coins") ?? _clips[(int)Sfx.Collect];
        }

        static float Env(float t, float len, float k) { return Mathf.Exp(-k * t) * Mathf.Clamp01((len - t) * 50f); }

        static uint _seed = 12345;
        static float Noise(int i) { _seed = _seed * 1664525u + 1013904223u; return ((_seed >> 9) / 4194304f) - 1f; }

        AudioClip Make(string name, float len, System.Func<float, int, float> f)
        {
            int n = (int)(Rate * len);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = f((float)i / Rate, i);
            var c = AudioClip.Create(name, n, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }

        AudioClip MakeBoom()
        {
            int n = (int)(Rate * 1.4f);
            var d = new float[n];
            float lp = 0;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / Rate;
                lp += (Noise(i) - lp) * 0.06f;   // basit alçak geçiren filtre: gümbürtü
                d[i] = (lp * 3f + Mathf.Sin(2 * Mathf.PI * 55 * t) * 0.5f) * Mathf.Exp(-3.5f * t);
            }
            var c = AudioClip.Create("boom", n, 1, Rate, false);
            c.SetData(d, 0);
            return c;
        }

        AudioClip Load(string key)
        {
            AudioClip c;
            if (_loaded.TryGetValue(key, out c)) return c;
            c = Resources.Load<AudioClip>(Folder + key);
            _loaded[key] = c;
            return c;
        }

        /// <summary>Binaya dokununca o binaya özgü kısa ses (en fazla 2 sn). Yenisi gelince öncekini keser.</summary>
        public static void PlayBuilding(string buildingId, float volume = 0.85f)
        {
            if (I == null || !I.Enabled) return;
            var c = I.Load("b_" + buildingId);
            if (c == null) { Play(Sfx.Click, 0.6f); return; }
            I._buildingSrc.Stop();
            I._buildingSrc.clip = c;
            I._buildingSrc.volume = volume;
            I._buildingSrc.pitch = 1f;
            I._buildingSrc.Play();
        }

        public static void Play(Sfx s, float volume = 1f, float pitchJitter = 0f)
        {
            if (I == null || !I.Enabled) return;
            I._src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            I._src.PlayOneShot(I._clips[(int)s], volume);
        }
    }
}
