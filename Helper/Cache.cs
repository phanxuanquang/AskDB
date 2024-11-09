namespace Helper
{
    public static class Cache
    {
        private static string _cacheFilePath = Path.Combine(Path.GetTempPath(), "AskDB.tmp");
        private static HashSet<string> _cachedData = [];

        private static async Task EnsureCacheFileCreated()
        {
            if (!File.Exists(_cacheFilePath))
            {
                await File.Create(_cacheFilePath).DisposeAsync();
            }
        }

        public static async Task Init()
        {
            if (!File.Exists(_cacheFilePath))
            {
                await File.Create(_cacheFilePath).DisposeAsync();
                return;
            }

            var cacheFileData = await StringTool.GetLines(_cacheFilePath, true);
            await Set(cacheFileData);
        }
        public static IEnumerable<string> Get(Func<string, bool> predicate, string keyword = "")
        {
            if (string.IsNullOrEmpty(keyword))
            {
                return _cachedData
                .Where(predicate)
                .Distinct()
                .Take(10)
                .OrderBy(k => k);
            }

            return _cachedData
                .Where(predicate)
                .Distinct()
                .OrderBy(k => StringTool.GetSimilarity(k, keyword))
                .Take(10)
                .OrderBy(k => k);
        }
        public static async Task Set<T>(T input)
        {
            if (object.Equals(input, default(T)))
            {
                return;
            }

            if (input is string data)
            {
                await EnsureCacheFileCreated();

                data = data.Trim();

                if (!_cachedData.Any(d => d.Equals(data, StringComparison.OrdinalIgnoreCase)))
                {
                    using (StreamWriter sw = new(_cacheFilePath, append: true))
                    {
                        await sw.WriteLineAsync(StringCipher.Encode(data));
                    }

                    _cachedData.Add(data);
                }
            }
            else if (input is IEnumerable<string> items)
            {
                _cachedData.UnionWith(items);
            }
        }
        public static async Task Remove<T>(T input)
        {
            if (object.Equals(input, default(T)))
            {
                return;
            }

            if (input is string data)
            {
                await EnsureCacheFileCreated();

                data = data.Trim();

                if (_cachedData.Any(d => d.Equals(data, StringComparison.OrdinalIgnoreCase)))
                {
                    var lines = File.ReadAllLinesAsync(_cacheFilePath).Result
                        .Select(StringCipher.Decode)
                        .ToList();

                    lines.RemoveAll(line => line.Equals(data, StringComparison.OrdinalIgnoreCase));

                    using (StreamWriter sw = new(_cacheFilePath))
                    {
                        foreach (var line in lines)
                        {
                            await sw.WriteLineAsync(StringCipher.Encode(line));
                        }
                    }

                    _cachedData.Remove(data);
                }
            }
            else if (input is IEnumerable<string> items)
            {
                _cachedData.ExceptWith(items);
            }
        }
    }
}