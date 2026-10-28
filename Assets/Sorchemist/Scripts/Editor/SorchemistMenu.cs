// Unity menü çubuğunda "Sorchemist" menüsü: Android/Play Store ayarlarını tek tıkla yapar.
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Sorchemist.EditorTools
{
    public static class SorchemistMenu
    {
        const string PackageName = "com.sorchemist.aqualf";   // Play Store'da benzersiz olmalı; istersen değiştir

        [MenuItem("Sorchemist/1) Android ve Play Store Ayarlarını Uygula")]
        public static void ApplyAndroidSettings()
        {
            PlayerSettings.companyName = "Sorchemist";
            PlayerSettings.productName = "Sorchemist";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.Android.bundleVersionCode = 1;

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, PackageName);
            // Google Play 64-bit zorunlu tutar: IL2CPP + ARM64
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            // Hedef API: yüklü en yüksek seviye (Play şu an yeni uygulamalar için API 36 istiyor)
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            EditorUserBuildSettings.buildAppBundle = true;   // Play Store .aab ister

            Debug.Log("[Sorchemist] Android ayarları uygulandı. Paket adı: " + PackageName +
                      "\nSıradaki adım: Project Settings > Player > Publishing Settings'ten Keystore oluştur.");
            EditorUtility.DisplayDialog("Sorchemist",
                "Android ayarları uygulandı!\n\nPaket adı: " + PackageName +
                "\nÇıktı: .aab (App Bundle)\nMimari: ARM64 + ARMv7, IL2CPP\nYön: Dikey\n\n" +
                "Sıradaki adım: Keystore oluşturmak (KURULUM.md, Adım 7).", "Tamam");
        }

        [MenuItem("Sorchemist/2) NativeShare Paylaşımını Etkinleştir")]
        public static void EnableNativeShare()
        {
            var target = NamedBuildTarget.Android;
            string defs = PlayerSettings.GetScriptingDefineSymbols(target);
            if (!defs.Contains("SORCHEMIST_NATIVESHARE"))
            {
                defs = string.IsNullOrEmpty(defs) ? "SORCHEMIST_NATIVESHARE" : defs + ";SORCHEMIST_NATIVESHARE";
                PlayerSettings.SetScriptingDefineSymbols(target, defs);
            }
            EditorUtility.DisplayDialog("Sorchemist",
                "SORCHEMIST_NATIVESHARE eklendi (Android).\n\nÖnce NativeShare eklentisini kurduğundan emin ol, yoksa derleme hatası alırsın. (KURULUM.md, Adım 8)", "Tamam");
        }

        [MenuItem("Sorchemist/Kayıt Dosyasını Sil (yeni oyun)")]
        public static void DeleteSave()
        {
            string p = Path.Combine(Application.persistentDataPath, "sorchemist_save.json");
            if (File.Exists(p)) File.Delete(p);
            Debug.Log("[Sorchemist] Kayıt silindi: " + p);
        }

        [MenuItem("Sorchemist/Kayıt Klasörünü Aç")]
        public static void OpenSaveFolder()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }
    }
}
