using AskDB.Commons.Helpers;
using System.Security.Cryptography;
using System.Text;

namespace AskDB.Commons.Extensions
{
    public static class StringExtensions
    {
        private static readonly byte[] IV = new byte[16];

        public static string AesEncrypt(this string? content)
        {
            if (string.IsNullOrEmpty(content) || string.IsNullOrWhiteSpace(content)) return string.Empty;

            using var aes = Aes.Create();
            aes.Key = CryptographyHelper.GetMachineGuidAesKey();
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            byte[] inputBytes = Encoding.UTF8.GetBytes(content.Trim());
            byte[] encryptedBytes = encryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
            return Convert.ToBase64String(encryptedBytes);
        }

        public static string AesDecrypt(this string? content)
        {
            if (string.IsNullOrEmpty(content)) return string.Empty;

            using var aes = Aes.Create();
            aes.Key = CryptographyHelper.GetMachineGuidAesKey();
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            byte[] inputBytes = Convert.FromBase64String(content);
            byte[] decryptedBytes = decryptor.TransformFinalBlock(inputBytes, 0, inputBytes.Length);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
    }
}
