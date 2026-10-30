// Hesap penceresi: e-posta/şifre ile giriş ve kayıt, Google ile giriş, hesap bilgileri ve bulut kaydı.
using System;
using Sorchemist.Core;
using Sorchemist.Game;
using UnityEngine;
using UnityEngine.UI;

namespace Sorchemist.UI
{
    public static class AccountPanel
    {
        static RectTransform _modal;

        public static void Show(Action afterLogin = null)
        {
            Close();
            var cloud = CloudService.I;
            if (cloud.LoggedIn) ShowAccount(cloud);
            else ShowLogin(cloud, false, afterLogin);
        }

        static void Close() { if (_modal != null) UnityEngine.Object.Destroy(_modal.gameObject); _modal = null; }

        static RectTransform Body(string title, out RectTransform col)
        {
            RectTransform body;
            _modal = UIKit.Modal(GameManager.I.Overlay, title, out body, 940, 1150);
            col = UIKit.Rect("Col", body);
            UIKit.Stretch(col);
            UIKit.VList(col, 14, 4);
            return body;
        }

        // ------------------------------------------------------------ giriş / kayıt
        static void ShowLogin(CloudService cloud, bool register, Action afterLogin)
        {
            RectTransform col;
            Body(register ? Loc.T("Hesap Aç", "Sign Up") : Loc.T("Giriş Yap", "Log In"), out col);

            var info = UIKit.Txt(col, "Info", Loc.T(
                "Hesabınla oyunun buluta kaydedilir: telefon değişse de ilerlemen kaybolmaz. Arkadaş ekleyebilir, hediye gönderebilir, loncalara katılabilirsin.",
                "With an account your game is saved to the cloud, so you never lose progress. Add friends, send gifts and join guilds."), 26, Theme.TextDim);
            UIKit.LE(info, 70);

            var email = UIKit.Input(col, "Email", Loc.T("E-posta", "Email"), InputField.ContentType.EmailAddress, 32, 190);
            UIKit.LE(email, 78);
            if (!string.IsNullOrEmpty(PlayerPrefs.GetString("sorch_last_email", ""))) email.text = PlayerPrefs.GetString("sorch_last_email", "");
            var pass = UIKit.Input(col, "Password", Loc.T("Şifre (en az 8 karakter)", "Password (at least 8 characters)"), InputField.ContentType.Password, 32, 200);
            UIKit.LE(pass, 78);
            InputField name = null;
            if (register)
            {
                name = UIKit.Input(col, "Name", Loc.T("Simyacı adın (arkadaşların görür)", "Your alchemist name (friends see it)"), InputField.ContentType.Standard, 32, 20);
                UIKit.LE(name, 78);
            }

            Action<string> done = err =>
            {
                if (err != null) { GameManager.I.Toast(CloudService.ErrorText(err)); return; }
                PlayerPrefs.SetString("sorch_last_email", email.text.Trim());
                Close();
                AudioFX.Play(Sfx.Chime);
                GameManager.I.Toast(Loc.T("Hoş geldin, ", "Welcome, ") + cloud.UserName + "!");
                if (afterLogin != null) afterLogin();
            };

            var main = UIKit.Wood(UIKit.Btn(col, "Main", register ? Loc.T("Hesap Aç", "Sign Up") : Loc.T("Giriş Yap", "Log In"), Theme.Accent, () =>
            {
                if (cloud.Busy) return;
                string e = email.text.Trim(), p = pass.text;
                if (e.Length < 5 || !e.Contains("@")) { GameManager.I.Toast(CloudService.ErrorText("bad_email")); return; }
                if (p.Length < 8) { GameManager.I.Toast(CloudService.ErrorText("password_too_short")); return; }
                if (register) cloud.Register(e, p, name.text.Trim(), done);
                else cloud.Login(e, p, done);
            }, 34), UIKit.Hex("#0E3B2C"), UIKit.Hex("#B8FFE4"));
            UIKit.LE(main, 86);

            var google = UIKit.Btn(col, "Google", Loc.T("Google ile Giriş", "Sign in with Google"), Color.white, () =>
            {
                if (cloud.Busy) return;
                cloud.LoginWithGoogle(err =>
                {
                    if (err == "google_plugin_missing")
                        GameManager.I.Toast(Loc.T("Google ile giriş bu sürümde henüz etkin değil. Şimdilik e-postayla giriş yap.",
                            "Google sign-in isn't enabled in this build yet. Please use email for now."));
                    else done(err);
                });
            }, 32, UIKit.Hex("#3C4043"));
            UIKit.LE(google, 80);
            var g = UIKit.Txt(google.transform, "G", "<b><color=#4285F4>G</color></b>", 44, Color.white);
            UIKit.Place(g.rectTransform, new Vector2(0, 0.5f), new Vector2(50, 0), new Vector2(60, 60));

            var row = UIKit.Rect("Row", col);
            UIKit.LE(row, 76);
            UIKit.HList(row, 16);
            UIKit.Btn(row, "Switch", register ? Loc.T("Zaten hesabım var", "I have an account") : Loc.T("Yeni hesap aç", "Create an account"),
                Theme.PanelLight, () => { Close(); ShowLogin(cloud, !register, afterLogin); }, 28);
            UIKit.Btn(row, "Close", Loc.T("Kapat", "Close"), Theme.PanelLight, Close, 28);
        }

        // ------------------------------------------------------------ giriş yapılmışken
        static void ShowAccount(CloudService cloud)
        {
            RectTransform col;
            Body(Loc.T("Hesabım", "My Account"), out col);
            var gm = GameManager.I;

            var info = UIKit.Txt(col, "Info",
                "<b>" + cloud.UserName + "</b>" + (string.IsNullOrEmpty(cloud.Email) ? "" : "   <color=#A99FC9>" + cloud.Email + "</color>") +
                Loc.T("\nArkadaş kodun: ", "\nYour friend code: ") + "<color=#FFD166><b>" + cloud.FriendCode + "</b></color>" +
                Loc.T("\n<size=24><color=#A99FC9>Bulut kaydı sürümü: ", "\n<size=24><color=#A99FC9>Cloud save version: ") + gm.State.CloudRev + "</color></size>",
                32, Theme.Text);
            UIKit.LE(info, 150);
            if (cloud.Flagged)
            {
                var warn = UIKit.Txt(col, "Flagged", Loc.T(
                    "<color=#FF8A80>Hesabın şüpheli bulut kayıtları yüzünden işaretlendi. Hediye, takas ve lonca bağışı kapalı; sıralamalarda görünmüyorsun.</color>",
                    "<color=#FF8A80>Your account was flagged for suspicious cloud saves. Gifts, trades and guild donations are disabled and you don't appear on leaderboards.</color>"), 24, Theme.Text);
                UIKit.LE(warn, 70);
            }

            var row1 = UIKit.Rect("Row1", col);
            UIKit.LE(row1, 80);
            UIKit.HList(row1, 16);
            UIKit.Btn(row1, "Copy", Loc.T("Kodu Kopyala", "Copy Code"), Theme.PanelLight, () =>
            {
                GUIUtility.systemCopyBuffer = cloud.FriendCode;
                gm.Toast(Loc.T("Arkadaş kodun kopyalandı: ", "Friend code copied: ") + cloud.FriendCode);
            }, 28);
            UIKit.Btn(row1, "Share", Loc.T("Kodu Paylaş", "Share Code"), Theme.PanelLight, () =>
                gm.Share(Loc.T("Sorchemist'te arkadaşım ol! Arkadaş kodum: ", "Be my friend in Sorchemist! My friend code: ") + cloud.FriendCode), 28);

            var nameRow = UIKit.Rect("NameRow", col);
            UIKit.LE(nameRow, 80);
            UIKit.HList(nameRow, 16);
            var nameField = UIKit.Input(nameRow, "Name", Loc.T("Yeni ad", "New name"), InputField.ContentType.Standard, 30, 20);
            UIKit.LE(nameField, -1, -1, 2);
            var setName = UIKit.Btn(nameRow, "SetName", Loc.T("Adı Değiştir", "Rename"), Theme.PanelLight, () =>
                cloud.SetName(nameField.text.Trim(), e => { if (e != null) gm.Toast(CloudService.ErrorText(e)); else { Close(); ShowAccount(cloud); } }), 28);
            UIKit.LE(setName, -1, -1, 1);

            var save = UIKit.Wood(UIKit.Btn(col, "Save", Loc.T("Şimdi Buluta Kaydet", "Save to Cloud Now"), Theme.Accent, () =>
                cloud.Upload(false, ok => { if (ok) gm.Toast(Loc.T("Oyunun buluta kaydedildi.", "Your game is saved to the cloud.")); }), 32),
                UIKit.Hex("#0E3B2C"), UIKit.Hex("#B8FFE4"));
            UIKit.LE(save, 84);

            var row2 = UIKit.Rect("Row2", col);
            UIKit.LE(row2, 80);
            UIKit.HList(row2, 16);
            UIKit.Btn(row2, "Logout", Loc.T("Çıkış Yap", "Log Out"), Theme.PanelLight, () =>
            {
                cloud.Logout();
                Close();
                gm.Toast(Loc.T("Çıkış yapıldı. Oyunun bu cihazda kalmaya devam ediyor.", "Logged out. Your game stays on this device."));
            }, 28);
            UIKit.Btn(row2, "Delete", Loc.T("Hesabı Sil", "Delete Account"), Theme.Danger, () => ConfirmDelete(cloud), 28);

            var close = UIKit.Btn(col, "Close", Loc.T("Kapat", "Close"), Theme.Magic, Close, 30);
            UIKit.LE(close, 76);
        }

        static void ConfirmDelete(CloudService cloud)
        {
            Close();
            RectTransform col;
            Body(Loc.T("Hesabı Sil", "Delete Account"), out col);
            var t = UIKit.Txt(col, "Warn", Loc.T(
                "Hesabın, bulut kaydın, arkadaşlıkların ve hediyelerin kalıcı olarak silinir. Bu cihazdaki oyunun kalır.\nOnaylamak için şifreni yaz (Google hesaplarında boş bırak).",
                "Your account, cloud save, friendships and gifts will be permanently deleted. The game on this device stays.\nType your password to confirm (leave empty for Google accounts)."), 30, Theme.Text);
            UIKit.LE(t, 200);
            var pass = UIKit.Input(col, "Password", Loc.T("Şifre", "Password"), InputField.ContentType.Password, 32, 200);
            UIKit.LE(pass, 78);
            var del = UIKit.Btn(col, "Delete", Loc.T("Kalıcı Olarak Sil", "Delete Permanently"), Theme.Danger, () =>
                cloud.DeleteAccount(pass.text, e =>
                {
                    if (e != null) { GameManager.I.Toast(CloudService.ErrorText(e)); return; }
                    Close();
                    GameManager.I.Toast(Loc.T("Hesabın silindi.", "Your account has been deleted."));
                }), 32);
            UIKit.LE(del, 84);
            var cancel = UIKit.Btn(col, "Cancel", Loc.T("Vazgeç", "Cancel"), Theme.PanelLight, () => { Close(); ShowAccount(cloud); }, 30);
            UIKit.LE(cancel, 76);
        }
    }
}
