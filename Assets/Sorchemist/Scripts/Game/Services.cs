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
        static string SigPath { get { return PathOf + ".sig"; } }
        static string DeviceKey { get { return SystemInfo.deviceUniqueIdentifier; } }

        public static PlayerState Load()
        {
            try
            {
                if (!File.Exists(PathOf)) return null;
                var json = File.ReadAllText(PathOf);
                var s = JsonUtility.FromJson<PlayerState>(json);
                // İmza dosyası varsa ve tutmuyorsa kayıt elle düzenlenmiştir (eski sürümlerde imza yoktu)
                if (s != null && File.Exists(SigPath) && !SaveSignature.Verify(json, DeviceKey, File.ReadAllText(SigPath).Trim()))
                {
                    s.TamperCount++;
                    Debug.LogWarning("[Sorchemist] Kayıt dosyasının imzası tutmuyor.");
                    Save(s);
                }
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
                string json = JsonUtility.ToJson(s);
                string tmp = PathOf + ".tmp", sigTmp = SigPath + ".tmp";
                File.WriteAllText(tmp, json);
                File.WriteAllText(sigTmp, SaveSignature.Compute(json, DeviceKey));
                if (File.Exists(PathOf)) File.Delete(PathOf);
                File.Move(tmp, PathOf);
                if (File.Exists(SigPath)) File.Delete(SigPath);
                File.Move(sigTmp, SigPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Sorchemist] Kayıt yazılamadı: " + e.Message);
            }
        }

        public static void Delete()
        {
            if (File.Exists(PathOf)) File.Delete(PathOf);
            if (File.Exists(SigPath)) File.Delete(SigPath);
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
