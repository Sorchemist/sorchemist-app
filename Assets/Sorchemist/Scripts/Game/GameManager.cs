// Oyunun kalbi. Kamera, EventSystem, Canvas ve bütün ekranları kodla kurar.
// Boş bir sahnede Play'e basman yeterli: GameBootstrap bu nesneyi otomatik oluşturur.
using System.Collections.Generic;
using Sorchemist.Core;
using Sorchemist.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sorchemist.Game
{
    public enum ScreenId { Village, Lab, FailDex, Skills }

    public class GameManager : MonoBehaviour
    {
        public static GameManager I;

        public GameSession Session { get; private set; }
        public PlayerState State { get { return Session.State; } }

        public RectTransform Content { get; private set; }   // Ekranların yaşadığı alan
        public RectTransform Overlay { get; private set; }   // Açılır pencereler, minioyunlar
        public RectTransform SafeRoot { get; private set; }

        HudBar _hud;
        readonly Dictionary<ScreenId, GameScreen> _screens = new Dictionary<ScreenId, GameScreen>();
        public ScreenId Current { get; private set; }
        bool _dirty;
        float _saveTimer;
        bool _isNewGame;

        void Awake()
        {
            if (I != null) { Destroy(gameObject); return; }
            I = this;
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            // Oyun yatay (landscape) oynanır; iki yatay yöne de döner
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;

            EnsureCamera();
            EnsureEventSystem();
            gameObject.AddComponent<AudioFX>();
            gameObject.AddComponent<FX>();
            AudioFXHook.OnClick = () => AudioFX.Play(Sfx.Click, 0.5f, 0.1f);

            var loaded = SaveService.Load();
            _isNewGame = loaded == null;
            Session = new GameSession(loaded);
            Session.Changed += () => { _dirty = true; if (_hud != null) _hud.Refresh(); };
            AudioFX.I.Enabled = State.SoundOn;
            Haptics.Enabled = State.HapticsOn;

            BuildCanvas();
            Show(ScreenId.Village);
            // Açılış ekranı (Art: bg_title). Yoksa doğrudan oyuna gir.
            bool newGame = _isNewGame;
            if (!TitleScreen.Show(Overlay, () => { if (newGame) Intro(); }))
                if (_isNewGame) Invoke("Intro", 0.3f);
        }

        void EnsureCamera()
        {
            if (Camera.main != null) { Camera.main.backgroundColor = Theme.Bg; return; }
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Theme.Bg;
            cam.orthographic = true;
            cam.gameObject.AddComponent<AudioListener>();
            DontDestroyOnLoad(cam.gameObject);
        }

        void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            // Yeni Input System (Unity 6 varsayılanı)
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
            DontDestroyOnLoad(es);
        }

        void BuildCanvas()
        {
            var cgo = new GameObject("SorchemistCanvas");
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            cgo.AddComponent<GraphicRaycaster>();

            var bg = UIKit.Img(cgo.transform, "Background", Theme.Bg);
            UIKit.Stretch(bg.rectTransform);

            SafeRoot = UIKit.Rect("SafeArea", cgo.transform);
            UIKit.Stretch(SafeRoot);
            SafeRoot.gameObject.AddComponent<SafeArea>();

            const float top = 110, rail = 220;   // üst bilgi çubuğu + sol menü
            Content = UIKit.Rect("Content", SafeRoot);
            UIKit.Stretch(Content, rail, top, 0, 0);

            _hud = SafeRoot.gameObject.AddComponent<HudBar>();
            _hud.Build(SafeRoot, top, rail);

            Overlay = UIKit.Rect("Overlay", SafeRoot);
            UIKit.Stretch(Overlay);

            AddScreen<VillageScreen>(ScreenId.Village, "Village");
            AddScreen<LabScreen>(ScreenId.Lab, "Lab");
            AddScreen<FailDexScreen>(ScreenId.FailDex, "FailDex");
            AddScreen<SkillScreen>(ScreenId.Skills, "Skills");
        }

        void AddScreen<T>(ScreenId id, string name) where T : GameScreen
        {
            var rt = UIKit.Rect(name, Content);
            UIKit.Stretch(rt);
            var s = rt.gameObject.AddComponent<T>();
            s.Build();
            rt.gameObject.SetActive(false);
            _screens[id] = s;
        }

        public void Show(ScreenId id)
        {
            foreach (var kv in _screens) kv.Value.gameObject.SetActive(kv.Key == id);
            Current = id;
            _screens[id].OnShow();
            if (_hud != null) _hud.SetActiveTab(id);
        }

        public T GetScreen<T>(ScreenId id) where T : GameScreen { return (T)_screens[id]; }

        public void RefreshCurrent() { _screens[Current].Refresh(); if (_hud != null) _hud.Refresh(); }

        // ---------- Ortak yardımcılar ----------
        public float MinigameTime(float baseSeconds)
        {
            return State.HasSkill(SkillDatabase.TimeBend) ? baseSeconds * 0.7f : baseSeconds;
        }

        public void Toast(string msg) { Toaster.Show(Overlay, msg); }

        public void Share(string text)
        {
            StartCoroutine(ShareService.CaptureAndShare(text, Toast));
        }

        public void HandleLevelUps(int ups)
        {
            if (ups <= 0) return;
            AudioFX.Play(Sfx.LevelUp);
            FX.I.CrystalRain(Overlay, Theme.Gold, 40);
            Toast("Seviye " + State.Level + "! +" + ups + " yetenek puanı. Yetenek ağacına göz at!");
        }

        void Intro()
        {
            DialoguePanel.Show(Overlay, "Aqualf", "Fındıkdere'nin Simyacısı", "#3E7CB1", "portrait_aqualf", new[]
            {
                "Hoş geldin! Ben Aqualf, Fındıkdere köyünün tek simyacısıyım.",
                "Köylüler dertlerini kapıma getirir, ben de onlara iksir hazırlarım.",
                "Ama sihrin sırrı kimyadır: haritadan ham madde topla, laboratuvarda elementlere ayır...",
                "...sonra doğru atomları doğru ısıda kazana at. Yanlış olursa? Eh... Fail-Dex'e bir sayfa daha!",
                "Hadi, kapıda biri var gibi. Haritadaki ünlem işaretine dokun!"
            }, null);
        }

        // ---------- Kayıt ----------
        void Update()
        {
            _saveTimer += Time.unscaledDeltaTime;
            if (_dirty && _saveTimer > 5f) SaveNow();
        }

        public void SaveNow()
        {
            _saveTimer = 0; _dirty = false;
            SaveService.Save(State);
        }

        void OnApplicationPause(bool paused) { if (paused && Session != null) SaveNow(); }
        void OnApplicationQuit() { if (Session != null) SaveNow(); }

        public void ResetGame()
        {
            SaveService.Delete();
            _dirty = false;
            Session = null;
            I = null;
            foreach (Transform t in transform) Destroy(t.gameObject);
            var go = new GameObject("Sorchemist");
            go.AddComponent<GameManager>();
            Destroy(gameObject);
        }
    }
}
