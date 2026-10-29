// Kayıt, titreşim ve paylaşım servisleri.
using System;
using System.Collections;
using System.IO;
using Sorchemist.Core;
using UnityEngine;

namespace Sorchemist.Game
{
    public static class SaveService
    {
        static string PathOf { get { return System.IO.Path.Combine(Application.persistentDataPath, "sorchemist_save.json"); } }

        public static PlayerState Load()
        {
            try
            {
                if (!File.Exists(PathOf)) return null;
                var s = JsonUtility.FromJson<PlayerState>(File.ReadAllText(PathOf));
                return s;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Sorchemist] Kayıt okunamadı, yeni oyun başlıyor: " + e.Message);
                return null;
            }
        }

        public static void Save(PlayerState s)
        {
            try
            {
                string tmp = PathOf + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(s));
                if (File.Exists(PathOf)) File.Delete(PathOf);
                File.Move(tmp, PathOf);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Sorchemist] Kayıt yazılamadı: " + e.Message);
            }
        }

        public static void Delete()
        {
            if (File.Exists(PathOf)) File.Delete(PathOf);
        }
    }

    public static class Haptics
    {
        public static bool Enabled = true;

        public static void Bump()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (Enabled) Handheld.Vibrate();
#endif
        }
    }

    /// <summary>
    /// Ekran görüntüsü alıp paylaşır. Gerçek Android paylaşım menüsü için ücretsiz
    /// "NativeShare" eklentisini kur (KURULUM.md, Adım 8) ve Scripting Define Symbols'a
    /// SORCHEMIST_NATIVESHARE ekle. Eklenti yokken görüntüyü kaydeder ve bildirir.
    /// </summary>
    public static class ShareService
    {
        public const string Hashtags = "#Sorchemist #Aqualf #KimyaBüyüsü";

        public static IEnumerator CaptureAndShare(string text, Action<string> onDone)
        {
            yield return new WaitForEndOfFrame();
            Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
            string path = System.IO.Path.Combine(Application.temporaryCachePath,
                "sorchemist_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.Destroy(tex);
            string full = text + "\n" + Hashtags;

#if SORCHEMIST_NATIVESHARE
            new NativeShare().AddFile(path).SetSubject("Sorchemist").SetText(full).Share();
            if (onDone != null) onDone(Loc.T("Paylaşım menüsü açıldı!", "Share menu opened!"));
#else
            GUIUtility.systemCopyBuffer = full;
            Debug.Log("[Sorchemist] Ekran görüntüsü kaydedildi: " + path);
            if (onDone != null) onDone(Loc.T("Görüntü kaydedildi, metin panoya kopyalandı.\n(Paylaşım menüsü için NativeShare'i kur)", "Screenshot saved, text copied to clipboard.\n(Install NativeShare for the share menu)"));
#endif
        }
    }
}
