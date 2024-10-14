namespace Helper
{
    public static class Cache
    {
        private static string _cacheFileName;
        public static HashSet<string> Data = [];
        public const short MaxResults = 10;

        private static async Task EnsureCacheFileCreated()
        {
            if (!File.Exists(_cacheFileName))
            {
                await File.Create(_cacheFileName).DisposeAsync();
            }
        }

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
                await EnsureCacheFileCreated();

                data = data.Trim();

                if (!Data.Any(d => d.Equals(data, StringComparison.OrdinalIgnoreCase)))
                {
                    using (StreamWriter sw = new(_cacheFileName, append: true))
                    {
                        await sw.WriteLineAsync(StringCipher.Encode(data));
                    }

                    Data.Add(data);
                }
            }
            else if (input is IEnumerable<string> items)
            {
                Data.UnionWith(items);
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

                if (Data.Any(d => d.Equals(data, StringComparison.OrdinalIgnoreCase)))
                {
                    var lines = File.ReadAllLinesAsync(_cacheFileName).Result
                        .Select(StringCipher.Decode)
                        .ToList();

                    lines.RemoveAll(line => line.Equals(data, StringComparison.OrdinalIgnoreCase));

                    using (StreamWriter sw = new(_cacheFileName))
                    {
                        foreach (var line in lines)
                        {
                            await sw.WriteLineAsync(StringCipher.Encode(line));
                        }
                    }

                    Data.Remove(data);
                }
            }
            else if (input is IEnumerable<string> items)
            {
                Data.ExceptWith(items);
            }
        }

        public static IEnumerable<string> Get(Func<string, bool> predicate)
        {
            return Data.Where(predicate).Distinct().Take(MaxResults);
        }
    }
}