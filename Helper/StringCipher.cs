using System.Security.Cryptography;
using System.Text;

namespace Helper
{
    public static class StringCipher
    {
        private const string key = "phanxuanquang-AskDB-2024";

        public static string Encode(string plainText)
        {
            using Aes aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(key);
            aes.IV = new byte[16];

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using MemoryStream ms = new();
            using (CryptoStream cs = new(ms, encryptor, CryptoStreamMode.Write))
            {
                using StreamWriter sw = new(cs);
                sw.Write(plainText);
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        public static string Decode(string cipherText)
        {
            using Aes aes = Aes.Create();
            try
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = new byte[16];

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using MemoryStream ms = new(Convert.FromBase64String(cipherText));
                using CryptoStream cs = new(ms, decryptor, CryptoStreamMode.Read);
                using StreamReader sr = new(cs);
                return sr.ReadToEnd();
            }
            catch
            {
                return cipherText;
            }
        }
    }
}
