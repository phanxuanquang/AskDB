using AskDB.Commons.Extensions;
using AskDB.SemanticKernel.Models;
using Newtonsoft.Json;

namespace AskDB.SemanticKernel.Helpers
{
    public static class AiServiceProviderCredentialManager
    {
        private const string CREADENTIALS_FOLDER = ".phanxuanquang";
        private const string CREDENTIALS_FILE_NAME = "credentials.json";
        private static readonly string FilePath = GetCredentialFilePath();

        private static string GetCredentialFilePath()
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var folderPath = Path.Combine(userProfile, CREADENTIALS_FOLDER);
            Directory.CreateDirectory(folderPath);

            return Path.Combine(folderPath, CREDENTIALS_FILE_NAME);
        }

        public static async Task SaveCredentialAsync(StandardAiServiceProviderCredential credentialObject)
        {
            var json = JsonConvert.SerializeObject(credentialObject);
            await File.WriteAllTextAsync(FilePath, json.AesEncrypt());
        }

        public static async Task<StandardAiServiceProviderCredential?> LoadCredentialAsync()
        {
            if (!File.Exists(FilePath)) return default;

            var encrypted = await File.ReadAllTextAsync(FilePath);
            return JsonConvert.DeserializeObject<StandardAiServiceProviderCredential?>(encrypted.AesDecrypt());
        }
    }
}
