namespace Helper
{
    public static class Cache
    {
        private static string _cacheFileName;
        public static HashSet<string> Data = new HashSet<string>();
        public const short MaxResults = 10;

        public static async Task Init()
        {
            _cacheFileName = Path.Combine(Path.GetTempPath(), "AskDB.tmp");

            if (!File.Exists(_cacheFileName))
            {
                await File.Create(_cacheFileName).DisposeAsync();
                return;
            }

            var cacheFileData = await StringTool.GetLines(_cacheFileName, true);
            await Set(cacheFileData);
        }

        public static async Task Set<T>(T input)
        {
            if (object.Equals(input, default(T)))
            {
                return;
            }

            if (input is string data)
            {
                if (!File.Exists(_cacheFileName))
                {
                    await File.Create(_cacheFileName).DisposeAsync();
                }

                data = data.Trim();

                if (Data.Any(d => d.Equals(data, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                using (StreamWriter sw = new StreamWriter(_cacheFileName, append: true))
                {
                    await sw.WriteLineAsync(StringCipher.Encode(data));
                }

                Data.Add(data);
            }
            else if (input is IEnumerable<string> items)
            {
                Data.UnionWith(items);
            }
        }

        public static IEnumerable<string> Get(Func<string, bool> predicate)
        {
            return Data.Where(predicate).OrderBy(k => k).Distinct().Take(MaxResults);
        }
    }
}