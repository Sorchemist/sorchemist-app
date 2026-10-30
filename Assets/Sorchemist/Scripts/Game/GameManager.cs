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
    public enum ScreenId { Village, Lab, FailDex, Skills, Social }

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
            gameObject.AddComponent<CloudService>();
            AudioFXHook.OnClick = () => AudioFX.Play(Sfx.Click, 0.5f, 0.1f);

            var loaded = SaveService.Load();
            _isNewGame = loaded == null;
            if (loaded != null) Loc.Current = (Lang)loaded.Language;   // kayıtlı dil (yeni oyunda mevcut dil korunur)
            Session = new GameSession(loaded);
            State.Language = (int)Loc.Current;
            Session.Changed += OnSessionChanged;
            AudioFX.I.Enabled = State.SoundOn;
            Haptics.Enabled = State.HapticsOn;

            BuildCanvas();
            Show(ScreenId.Village);
            // Açılış ekranı (Art: bg_title). Yoksa doğrudan oyuna gir.
            bool newGame = _isNewGame;
            if (!TitleScreen.Show(Overlay, () =>
                {
                    if (newGame) Intro();
                    else if (Session.MysteryReady(Now))
                        Toast(Loc.T("Köye bir Gizemli Kutu geldi! Laboratuvarın yanında seni bekliyor.", "A Mystery Box has arrived in the village! It is waiting by the lab."));
                }))
                if (_isNewGame) Invoke("Intro", 0.3f);
            // Hesaba girilmişse başka bir cihazda oynanmış daha yeni bir kayıt var mı bak
            if (CloudService.I.LoggedIn) Invoke("CloudCheck", 2f);
        }

        void CloudCheck() { CloudService.I.RefreshMe(); CloudService.I.SyncAfterLogin(); }

        void OnSessionChanged()
        {
            _dirty = true;
            if (_hud != null) _hud.Refresh();
            if (CloudService.I != null) CloudService.I.MarkDirty();
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
            TopInset = top;
            // Ekranlar bütün alanı kullanır; köy haritası tam ekran, diğer ekranlar üst çubuk ve menü kadar içeride başlar
            Content = UIKit.Rect("Content", SafeRoot);
            UIKit.Stretch(Content);
            _leftInset = rail;

            _hud = SafeRoot.gameObject.AddComponent<HudBar>();
            _hud.Build(SafeRoot, top, rail);

            Overlay = UIKit.Rect("Overlay", SafeRoot);
            UIKit.Stretch(Overlay);

            AddScreen<VillageScreen>(ScreenId.Village, "Village");
            AddScreen<LabScreen>(ScreenId.Lab, "Lab");
            AddScreen<FailDexScreen>(ScreenId.FailDex, "FailDex");
            AddScreen<SkillScreen>(ScreenId.Skills, "Skills");
            AddScreen<SocialScreen>(ScreenId.Social, "Social");
        }

        void AddScreen<T>(ScreenId id, string name) where T : GameScreen
        {
            var rt = UIKit.Rect(name, Content);
            UIKit.Stretch(rt);
            var s = rt.gameObject.AddComponent<T>();
            if (!s.FullBleed) { UIKit.Stretch(rt, _leftInset, TopInset, 0, 0); _insetScreens.Add(rt); }
            s.Build();
            rt.gameObject.SetActive(false);
            _screens[id] = s;
        }

        public float TopInset { get; private set; }
        float _leftInset;
        readonly List<RectTransform> _insetScreens = new List<RectTransform>();

        /// <summary>Sol menü açılıp kapanırken tam ekran olmayan ekranların sol kenarını kaydırır.</summary>
        public void SetLeftInset(float left)
        {
            _leftInset = left;
            foreach (var rt in _insetScreens) rt.offsetMin = new Vector2(left, rt.offsetMin.y);
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
        public static long Now { get { return System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(); } }

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
            Toast(Loc.T("Seviye ", "Level ") + State.Level + Loc.T("! Aqualf'in ünü kasabada yayılıyor.", "! Aqualf's fame is spreading through the town."));
            // Büyülü sis kalkan mahalleler
            int prev = State.Level - ups;
            for (int g = 2; g <= 4; g++)
            {
                int need = BuildingDatabase.GroupUnlockLevel(g);
                if (prev < need && State.Level >= need)
                {
                    string[] names = { "", "", Loc.T("Zanaat ve Araştırma Mahallesi", "Craft & Research District"),
                                       Loc.T("Keşif ve Arıtma Mahallesi", "Exploration & Refining District"), Loc.T("Efsanevi Volkanik Mahalle", "Legendary Volcanic District") };
                    AudioFX.Play(Sfx.Chime);
                    FX.I.Flash(Overlay, new Color(0.75f, 0.55f, 1f, 0.5f), 0.8f);
                    ResultPopup.Show(Loc.T("Büyülü Sis Kalktı!", "The Magic Mist Lifts!"), Theme.Magic,
                        names[g] + Loc.T(" artık açık! Yeni binalar, yeni görevliler ve yeni deneyler seni bekliyor.", " is now open! New buildings, new keepers and new experiments await you."),
                        null, null, Loc.T("Sorchemist'te yeni bir mahallenin sisini kaldırdım!", "I lifted the mist from a new district in Sorchemist!"), RefreshCurrent,
                        g == 2 ? "fog_cloud" : g == 3 ? "fog_runes" : "fog_cosmic");
                }
            }
        }

        void Intro()
        {
            DialoguePanel.Show(Overlay, "Aqualf", Loc.T("Fındıkdere'nin Simyacısı", "Alchemist of Hazelbrook"), "#3E7CB1", "portrait_aqualf", new[]
            {
                Loc.T("Hoş geldin! Ben Aqualf, Fındıkdere kasabasının simyacısıyım.", "Welcome! I'm Aqualf, the alchemist of Hazelbrook town."),
                Loc.T("Kasabanın 23 görevlisi dertlerini kapıma getirir, ben de onlara iksir hazırlarım.", "The town's 23 keepers bring their troubles to my door, and I make potions for them."),
                Loc.T("Ama sihrin sırrı kimyadır: ham maddeleri laboratuvarda elementlere ayır, doğru atomları kazana at ve iksirin demlenmesini bekle.", "But the secret of magic is chemistry: break raw materials down into elements in the lab, put the right atoms in the cauldron and wait for the potion to brew."),
                Loc.T("Kasabanın her binasının bir seviyesi var. Binaları yükselttikçe yeni deneyler açılır. Uzak mahalleler büyülü sisin altında: sen seviye atladıkça sis kalkar.", "Every building in town has a level. Upgrade them to unlock new experiments. The far districts lie under a magical mist that lifts as you level up."),
                Loc.T("Mevsimler de önemli: ilkbahar şifa iksirlerini, sonbahar ateşi, kış asitleri, fırtına da elektrolizi hızlandırır. Sağ üstteki madalyona bak!", "Seasons matter too: spring speeds up healing potions, autumn fire, winter acids and storms electrolysis. Check the medallion at the top right!"),
                Loc.T("Oyunda kaldıkça altın ve Bilgi Puanı biriktirirsin. Bilgiyle kulemdeki yuvalara pasif özellikler takabilirsin.", "While you play you collect gold and Knowledge Points. Use Knowledge to put passives into the slots of my tower."),
                Loc.T("Yanlış karışım mı? Eh... Fail-Dex'e bir sayfa daha! Hadi, haritada gezin ve ünlem işaretli binayı bul!", "Wrong mix? Well... one more page for the Fail-Dex! Come on, explore the map and find the building with the exclamation mark!")
            }, null);
        }

        // ---------- Kayıt ----------
        float _tickTimer;

        void Update()
        {
            if (Session != null)
            {
                // Çevrimiçi gelir: yalnızca oyun açıkken akar
                _tickTimer += Time.unscaledDeltaTime;
                if (_tickTimer >= 1f)
                {
                    float dt = _tickTimer; _tickTimer = 0;
                    HandleLevelUps(Session.TickOnline(dt));
                    var done = Session.CompleteUpgrades(Now);
                    foreach (var id in done)
                    {
                        AudioFX.Play(Sfx.LevelUp, 0.7f);
                        if (FX.I != null) FX.I.CrystalRain(Overlay, Theme.Gold, 25);
                        Toast(Loc.T(BuildingDatabase.NameTr(id) + " Sv." + State.BuildingLevel(id) + " oldu!", BuildingDatabase.NameTr(id) + " reached Lv." + State.BuildingLevel(id) + "!"));
                    }
                    if (_screens.ContainsKey(Current)) _screens[Current].Tick();
                }
            }
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

        /// <summary>Dili değiştirir ve bütün arayüzü yeni dilde yeniden kurar.</summary>
        public void SetLanguage(Lang lang)
        {
            Loc.Current = lang;
            State.Language = (int)lang;
            SaveNow();
            RebuildUI();
            Toast(Loc.T("Dil: Türkçe", "Language: English"));
        }

        void RebuildUI()
        {
            var old = transform.Find("SorchemistCanvas");
            if (old != null) { old.name = "OldCanvas"; Destroy(old.gameObject); }
            _screens.Clear();
            BuildCanvas();
            Show(ScreenId.Village);
        }

        /// <summary>Başka bir kaydı (ör. buluttan gelen) yükler ve arayüzü yeniden kurar.</summary>
        public void LoadState(PlayerState st)
        {
            Session.Changed -= OnSessionChanged;
            Session = new GameSession(st);
            Session.Changed += OnSessionChanged;
            Loc.Current = (Lang)st.Language;
            AudioFX.I.Enabled = State.SoundOn;
            Haptics.Enabled = State.HapticsOn;
            SaveNow();
            RebuildUI();
        }

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
