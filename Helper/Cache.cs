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
            if (!File.Exists(CacheFilePath))
            {
                File.Create(CacheFilePath);
            }

            using (var streamReader = new StreamReader(CacheFilePath))
            {
                string currentLine;
                while ((currentLine = await streamReader.ReadLineAsync()) != null)
                {
                    if (currentLine.Equals(line, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }
            }

            using (StreamWriter sw = new StreamWriter(CacheFilePath, append: true))
            {
                await sw.WriteLineAsync(StringCipher.Encode(line.Trim()));
            }
        }
    }
}
