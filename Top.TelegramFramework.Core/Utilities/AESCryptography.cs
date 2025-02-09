
using System.Security.Cryptography;
using System.Text;

namespace Top.TelegramFramework.Core.Utilities
{
    public class AESCryptography
    {
        public static string Encrypt(string plainText, string key)
        {
            using var aes = Aes.Create();
            using var keyDerivation = new Rfc2898DeriveBytes(key, Encoding.UTF8.GetBytes("MySaltValue"), 10000);
            aes.Key = keyDerivation.GetBytes(32);
            aes.IV = keyDerivation.GetBytes(16);

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                using var sw = new StreamWriter(cs);
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        public static string Decrypt(string cipherText, string key)
        {
            using var aes = Aes.Create();
            using var keyDerivation = new Rfc2898DeriveBytes(key, Encoding.UTF8.GetBytes("MySaltValue"), 10000);
            aes.Key = keyDerivation.GetBytes(32);
            aes.IV = keyDerivation.GetBytes(16);

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }
    }
}
