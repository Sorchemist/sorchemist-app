// Yerel kayıt dosyasının imzası: kayıt dosyası elle düzenlenirse fark edilir.
// Bu yalnızca caydırıcıdır (gerçek koruma sunucudadır); imza eşleşmezse PlayerState.TamperCount artar
// ve sunucu bu kayıtla bulut yüklemesini reddeder, oyuncu buluttaki son geçerli kayda döner.
using System;
using System.Security.Cryptography;
using System.Text;

namespace Sorchemist.Core
{
    public static class SaveSignature
    {
        const string Pepper = "Sorchemist|Findikdere|Aqualf|7c41e9";

        public static string Compute(string json, string deviceKey)
        {
            using (var h = new HMACSHA256(Encoding.UTF8.GetBytes(Pepper + "|" + (deviceKey ?? ""))))
                return Convert.ToBase64String(h.ComputeHash(Encoding.UTF8.GetBytes(json ?? "")));
        }

        public static bool Verify(string json, string deviceKey, string signature)
        {
            if (string.IsNullOrEmpty(signature)) return false;
            var expected = Compute(json, deviceKey);
            if (expected.Length != signature.Length) return false;
            int diff = 0;
            for (int i = 0; i < expected.Length; i++) diff |= expected[i] ^ signature[i];
            return diff == 0;
        }
    }
}
