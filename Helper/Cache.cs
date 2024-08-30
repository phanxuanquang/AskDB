namespace Helper
{
    public static class Cache
    {
        private const string CacheFilePath = @"Assets\Cache.txt";

        public static async Task<List<string>> GetContent()
        {
            return await StringEngineer.GetWords(CacheFilePath);
        }

        public static async Task SetContent(string line)
        {
            using (StreamWriter sw = new StreamWriter(CacheFilePath, append: true))
            {
                await sw.WriteLineAsync(line.Trim());
            }
        }
    }
}
