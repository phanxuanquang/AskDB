using AskDB.Commons.Helpers;
using System.Security.Cryptography;
using System.Text;

namespace AskDB.Commons.Extensions
{
    public static class StringExtensions
    {
        private static readonly byte[] IV = new byte[16];

        public static string AesEncrypt(this string content)
        {
            if (string.IsNullOrEmpty(content)) return string.Empty;

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

        public static string AesDecrypt(this string content)
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

        public static string CalculateSHA256Hash(string filePath)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException("File not found", filePath);

            using SHA256 sha256Hash = SHA256.Create();
            using FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            byte[] buffer = new byte[8192];
            int bytesRead;
            while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
            {
                sha256Hash.TransformBlock(buffer, 0, bytesRead, buffer, 0);
            }

            sha256Hash.TransformFinalBlock(buffer, 0, 0);

            byte[] hashBytes = sha256Hash.Hash;

            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}
