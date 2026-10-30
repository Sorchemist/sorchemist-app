// Çevrimiçi hizmetler: hesap (e-posta/şifre ya da Google), bulut kaydı ve sorchemist.com API çağrıları.
// Sunucu kodu: Server/api (PHP + MySQL, Hostinger).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Sorchemist.Core;
using Sorchemist.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace Sorchemist.Game
{
    public class CloudService : MonoBehaviour
    {
        public static CloudService I;

        /// <summary>API adresi. Kendi alan adın değişirse yalnızca burayı değiştir.</summary>
        public const string ApiUrl = "https://sorchemist.com/api/api.php";
        const float AutoUploadSeconds = 120f;

        const string KToken = "sorch_token", KName = "sorch_name", KCode = "sorch_code", KUid = "sorch_uid", KEmail = "sorch_email";

        /// <summary>
        /// Google ile giriş köprüsü. Android'de bir Google Sign-In eklentisi bu alana atanır:
        /// (onIdToken, onError) => { ...eklenti girişi... onIdToken(idToken); }
        /// Atanmamışsa "Google ile Giriş" butonu kurulum gerektiğini söyler.
        /// </summary>
        public static Action<Action<string>, Action<string>> GoogleIdTokenProvider;

        public event Action AccountChanged;

        public bool LoggedIn { get { return !string.IsNullOrEmpty(Token); } }
        public string Token { get { return PlayerPrefs.GetString(KToken, ""); } }
        public string UserName { get { return PlayerPrefs.GetString(KName, ""); } }
        public string FriendCode { get { return PlayerPrefs.GetString(KCode, ""); } }
        public string Email { get { return PlayerPrefs.GetString(KEmail, ""); } }
        public int UserId { get { return PlayerPrefs.GetInt(KUid, 0); } }
        public bool Busy { get; private set; }

        bool _dirty;
        float _uploadTimer;
        bool _uploading;

        GameManager G { get { return GameManager.I; } }

        void Awake() { I = this; }

        public void MarkDirty() { _dirty = true; }

        void Update()
        {
            if (!LoggedIn || !_dirty || _uploading) return;
            _uploadTimer += Time.unscaledDeltaTime;
            if (_uploadTimer >= AutoUploadSeconds) Upload(false, null);
        }

        void OnApplicationPause(bool paused) { if (paused && LoggedIn && _dirty) Upload(false, null); }

        // =================================================================
        // Ham API çağrısı
        // =================================================================
        public void Call(string action, Dictionary<string, object> body, Action<JNode> ok, Action<string> err = null)
        {
            StartCoroutine(Send(action, body ?? new Dictionary<string, object>(), ok, err));
        }

        IEnumerator Send(string action, Dictionary<string, object> body, Action<JNode> ok, Action<string> err)
        {
            body["action"] = action;
            var bytes = Encoding.UTF8.GetBytes(MiniJson.Serialize(body));
            using (var req = new UnityWebRequest(ApiUrl, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(bytes);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                if (LoggedIn) req.SetRequestHeader("Authorization", "Bearer " + Token);
                req.timeout = 20;
                yield return req.SendWebRequest();

                var res = JNode.From(req.downloadHandler != null ? req.downloadHandler.text : null);
                if (!res.IsNull && res["ok"].Bool) { if (ok != null) ok(res); yield break; }

                string code = !res.IsNull && res["error"].Str != null ? res["error"].Str : "network";
                if (code == "session_expired" || code == "not_logged_in") { ClearSession(); }
                if (err != null) err(code);
                else if (code != "conflict") G.Toast(ErrorText(code));
            }
        }

        /// <summary>Sunucu hata kodlarının oyuncuya gösterilecek hali.</summary>
        public static string ErrorText(string code)
        {
            switch (code)
            {
                case "network": return Loc.T("Sunucuya ulaşılamadı. İnternet bağlantını kontrol et.", "Can't reach the server. Check your internet connection.");
                case "wrong_credentials": return Loc.T("E-posta ya da şifre yanlış.", "Wrong email or password.");
                case "email_taken": return Loc.T("Bu e-postayla zaten bir hesap var. Giriş yapmayı dene.", "An account with this email already exists. Try logging in.");
                case "bad_email": return Loc.T("Geçerli bir e-posta yaz.", "Enter a valid email.");
                case "password_too_short": return Loc.T("Şifre en az 8 karakter olmalı.", "Password must be at least 8 characters.");
                case "bad_name": return Loc.T("Ad 2-20 karakter olmalı.", "Name must be 2-20 characters.");
                case "rate_limited": return Loc.T("Çok fazla deneme. Biraz bekle.", "Too many attempts. Please wait a bit.");
                case "session_expired":
                case "not_logged_in": return Loc.T("Oturumun sona erdi, tekrar giriş yap.", "Your session has expired. Please log in again.");
                case "friend_not_found": return Loc.T("Bu kodla bir simyacı bulunamadı.", "No alchemist found with this code.");
                case "cannot_add_self": return Loc.T("Kendini arkadaş ekleyemezsin!", "You can't add yourself!");
                case "too_many_friends": return Loc.T("Arkadaş listen dolu (100).", "Your friend list is full (100).");
                case "daily_gift_limit": return Loc.T("Bugünlük hediye hakkın bitti (5). Yarın tekrar dene!", "You've used today's 5 gifts. Try again tomorrow!");
                case "not_friends": return Loc.T("Önce arkadaş olmalısınız.", "You need to be friends first.");
                case "already_claimed": return Loc.T("Bu zaten alınmış.", "This has already been claimed.");
                case "trade_not_open": return Loc.T("Bu takas artık açık değil.", "This trade is no longer open.");
                case "too_many_trades": return Loc.T("Aynı anda en fazla 10 takas teklifin olabilir.", "You can have at most 10 open trades.");
                case "already_in_guild": return Loc.T("Zaten bir loncadasın.", "You're already in a guild.");
                case "guild_not_found": return Loc.T("Bu kodla bir lonca bulunamadı.", "No guild found with this code.");
                case "guild_full": return Loc.T("Lonca dolu (30 üye).", "The guild is full (30 members).");
                case "guild_name_taken": return Loc.T("Bu lonca adı alınmış.", "That guild name is taken.");
                case "wrong_effect": return Loc.T("Bu hafta loncanın istediği etki bu değil.", "That's not the effect the guild needs this week.");
                case "quest_done": return Loc.T("Haftalık lonca görevi zaten tamam!", "This week's guild quest is already done!");
                case "quest_not_done": return Loc.T("Lonca görevi henüz bitmedi.", "The guild quest isn't finished yet.");
                case "nothing_to_claim": return Loc.T("Alınacak ödül yok (katkı vermiş olmalısın).", "Nothing to claim (you must have contributed).");
                case "google_not_configured": return Loc.T("Google ile giriş sunucuda henüz ayarlanmadı.", "Google sign-in isn't set up on the server yet.");
                case "server_not_configured": return Loc.T("Sunucu henüz kurulmadı.", "The server isn't set up yet.");
                default: return Loc.T("Bir sorun oldu: ", "Something went wrong: ") + code;
            }
        }

        // =================================================================
        // Hesap
        // =================================================================
        public void Register(string email, string password, string name, Action<string> done)
        {
            Busy = true;
            Call("register", new Dictionary<string, object> { { "email", email }, { "password", password }, { "name", name } },
                r => { Busy = false; OnLoggedIn(r, email); done(null); }, e => { Busy = false; done(e); });
        }

        public void Login(string email, string password, Action<string> done)
        {
            Busy = true;
            Call("login", new Dictionary<string, object> { { "email", email }, { "password", password } },
                r => { Busy = false; OnLoggedIn(r, email); done(null); }, e => { Busy = false; done(e); });
        }

        public void LoginWithGoogle(Action<string> done)
        {
            if (GoogleIdTokenProvider == null) { done("google_plugin_missing"); return; }
            Busy = true;
            GoogleIdTokenProvider(idToken =>
            {
                Call("google", new Dictionary<string, object> { { "idToken", idToken } },
                    r => { Busy = false; OnLoggedIn(r, ""); done(null); }, e => { Busy = false; done(e); });
            }, e => { Busy = false; done(e ?? "google_cancelled"); });
        }

        void OnLoggedIn(JNode r, string email)
        {
            PlayerPrefs.SetString(KToken, r["token"].Str);
            StoreUser(r["user"]);
            if (!string.IsNullOrEmpty(email)) PlayerPrefs.SetString(KEmail, email);
            PlayerPrefs.Save();
            if (AccountChanged != null) AccountChanged();
            SyncAfterLogin();
        }

        void StoreUser(JNode u)
        {
            if (u.IsNull) return;
            PlayerPrefs.SetString(KName, u["name"].Str ?? "");
            PlayerPrefs.SetString(KCode, u["friendCode"].Str ?? "");
            PlayerPrefs.SetInt(KUid, u["id"].Int);
        }

        public void RefreshMe(Action done = null)
        {
            if (!LoggedIn) { if (done != null) done(); return; }
            Call("me", null, r => { StoreUser(r["user"]); PlayerPrefs.Save(); if (AccountChanged != null) AccountChanged(); if (done != null) done(); },
                e => { if (done != null) done(); });
        }

        public void SetName(string name, Action<string> done)
        {
            Call("set_name", new Dictionary<string, object> { { "name", name } },
                r => { PlayerPrefs.SetString(KName, r["name"].Str); PlayerPrefs.Save(); if (AccountChanged != null) AccountChanged(); done(null); }, done);
        }

        public void Logout()
        {
            if (LoggedIn)
            {
                if (_dirty) Upload(false, null);
                Call("logout", null, null, e => { });
            }
            ClearSession();
        }

        public void DeleteAccount(string password, Action<string> done)
        {
            var body = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(password)) body["password"] = password;
            Call("delete_account", body, r =>
            {
                ClearSession();
                G.State.CloudUserId = 0; G.State.CloudRev = 0; G.SaveNow();
                done(null);
            }, done);
        }

        void ClearSession()
        {
            foreach (var k in new[] { KToken, KName, KCode, KUid, KEmail }) PlayerPrefs.DeleteKey(k);
            PlayerPrefs.Save();
            if (AccountChanged != null) AccountChanged();
        }

        // =================================================================
        // Bulut kaydı
        // =================================================================
        bool IsFreshLocal(PlayerState s)
        {
            return s.CloudUserId == 0 && s.Level <= 1 && s.CompletedQuests.Count == 0 && s.Discovered.Count == 0;
        }

        /// <summary>Girişten sonra: buluttaki kayıtla cihazdakini karşılaştırır.</summary>
        public void SyncAfterLogin()
        {
            Call("save_get", null, r =>
            {
                var save = r["save"];
                var s = G.State;
                if (save.IsNull) { Upload(true, null); return; }                    // bulutta kayıt yok: bu cihazınkini yükle
                int rev = save["rev"].Int;
                if (s.CloudUserId == UserId && s.CloudRev == rev) { if (_dirty) Upload(false, null); return; }   // zaten güncel
                if (IsFreshLocal(s)) { ApplyCloudSave(save); return; }              // cihazda yeni oyun var: buluttakini al
                AskWhichSave(save);
            }, e => G.Toast(ErrorText(e)));
        }

        void AskWhichSave(JNode save)
        {
            var cloud = TryUnpack(save["data"].Str);
            string cloudInfo = cloud != null
                ? Loc.T("Sv." + cloud.Level + ", " + cloud.CompletedQuests.Count + " görev", "Lv." + cloud.Level + ", " + cloud.CompletedQuests.Count + " quests")
                : "?";
            var local = G.State;
            string localInfo = Loc.T("Sv." + local.Level + ", " + local.CompletedQuests.Count + " görev", "Lv." + local.Level + ", " + local.CompletedQuests.Count + " quests");
            string when = DateTimeOffset.FromUnixTimeSeconds(save["updatedAt"].Long).ToLocalTime().ToString("dd.MM.yyyy HH:mm");
            ChoiceDialog.Show(Loc.T("Hangi kayıt?", "Which save?"),
                Loc.T("Bu hesapta buluta kayıtlı bir oyun var:\n<b>" + cloudInfo + "</b>  (" + when + ")\n\nBu cihazdaki oyun:\n<b>" + localInfo + "</b>\n\nHangisiyle devam edelim? Diğeri silinecek.",
                      "This account has a cloud save:\n<b>" + cloudInfo + "</b>  (" + when + ")\n\nThis device's game:\n<b>" + localInfo + "</b>\n\nWhich one should we keep? The other will be replaced."),
                Loc.T("Buluttakini yükle", "Load cloud save"), () => ApplyCloudSave(save),
                Loc.T("Bu cihazdakini kullan", "Keep this device's"), () => Upload(true, null));
        }

        static PlayerState TryUnpack(string data)
        {
            try { return JsonUtility.FromJson<PlayerState>(CloudData.Unpack(data)); } catch (Exception) { return null; }
        }

        void ApplyCloudSave(JNode save)
        {
            var st = TryUnpack(save["data"].Str);
            if (st == null) { G.Toast(Loc.T("Buluttaki kayıt okunamadı.", "Couldn't read the cloud save.")); return; }
            st.CloudUserId = UserId;
            st.CloudRev = save["rev"].Int;
            st.Language = (int)Loc.Current;
            G.LoadState(st);
            _dirty = false;
            G.Toast(Loc.T("Buluttaki oyunun yüklendi!", "Your cloud save has been loaded!"));
        }

        /// <summary>Kaydı buluta yükler. force: sunucudaki daha yeni sürümün üzerine yaz.</summary>
        public void Upload(bool force, Action<bool> done)
        {
            if (!LoggedIn || _uploading) { if (done != null) done(false); return; }
            _uploading = true;
            _uploadTimer = 0;
            var s = G.State;
            bool sameAccount = s.CloudUserId == UserId;
            var body = new Dictionary<string, object>
            {
                { "data", CloudData.Pack(JsonUtility.ToJson(s)) },
                { "baseRev", sameAccount ? s.CloudRev : 0 },
                { "force", force },
                { "device", SystemInfo.deviceModel },
                { "stats", CloudData.Stats(s) },
                { "village", CloudData.Village(s, UserName) },
            };
            _dirty = false;
            Call("save_put", body, r =>
            {
                _uploading = false;
                s.CloudUserId = UserId;
                s.CloudRev = r["rev"].Int;
                SaveService.Save(s);
                if (done != null) done(true);
            }, e =>
            {
                _uploading = false;
                _dirty = true;
                if (e == "conflict")
                {
                    // Başka bir cihaz daha yeni bir kayıt yüklemiş: oyuncuya sor
                    Call("save_get", null, r2 => { if (!r2["save"].IsNull) AskWhichSave(r2["save"]); }, x => { });
                }
                else if (done != null) G.Toast(ErrorText(e));
                if (done != null) done(false);
            });
        }
    }
}
