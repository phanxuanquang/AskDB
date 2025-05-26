namespace AskDB.Commons.Helpers
{
    public static class FileHelper
    {
        public static async Task<string> ReadFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"The file '{filePath}' does not exist.");
            }
            using var reader = new StreamReader(filePath);
            return await reader.ReadToEndAsync();
        }
    }
}
