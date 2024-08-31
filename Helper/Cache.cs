namespace Helper
{
    public static class Cache
    {
        private const string _cacheFilePath = @"Assets\Cache.txt";
        public static List<string> Data = new List<string>();

        public static async Task<List<string>> GetContent()
        {
            return await StringEngineer.GetLines(_cacheFilePath, true);
        }

        public static async Task SetContent(string line)
        {
            if (!File.Exists(_cacheFilePath))
            {
                File.Create(_cacheFilePath);
            }

            using (var streamReader = new StreamReader(_cacheFilePath))
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

            using (StreamWriter sw = new StreamWriter(_cacheFilePath, append: true))
            {
                await sw.WriteLineAsync(StringCipher.Encode(line.Trim()));
            }
        }
    }
}
