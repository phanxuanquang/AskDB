namespace Helper
{
    public static class Cache
    {
        private const string CacheFilePath = @"Assets\Cache.txt";

        public static async Task<List<string>> GetContent()
        {
            return await StringEngineer.GetWords(CacheFilePath, true);
        }

        public static async Task SetContent(string line)
        {
            using (StreamWriter sw = new StreamWriter(CacheFilePath, append: true))
            {
                if (!File.Exists(CacheFilePath))
                {
                    File.Create(CacheFilePath);
                }

                await sw.WriteLineAsync(StringCipher.Encode(line.Trim()));
            }
        }
    }
}
