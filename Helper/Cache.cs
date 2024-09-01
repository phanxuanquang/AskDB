using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Helper
{
    public static class Cache
    {
        private const string _cacheFilePath = @"Assets\Cache.txt";
        public const short MaxResults = 10;
        public static HashSet<string> Data = new HashSet<string>();

        public static async Task Init()
        {
            var cacheFileData = await StringEngineer.GetLines(_cacheFilePath, true);
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
                if (!File.Exists(_cacheFilePath))
                {
                    await File.Create(_cacheFilePath).DisposeAsync();
                }

                data = data.Trim();

                using (var streamReader = new StreamReader(_cacheFilePath))
                {
                    string currentLine;
                    while ((currentLine = await streamReader.ReadLineAsync()) != null)
                    {
                        if (currentLine.Equals(data, StringComparison.OrdinalIgnoreCase) || currentLine.Contains(data, StringComparison.OrdinalIgnoreCase))
                        {
                            return;
                        }
                    }
                }

                using (StreamWriter sw = new StreamWriter(_cacheFilePath, append: true))
                {
                    await sw.WriteLineAsync(StringCipher.Encode(data.Trim()));
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
            return Data.Where(predicate)
                .OrderByDescending(item => item.All(char.IsUpper))
                .ThenBy(item => item)
                .Distinct()
                .Take(MaxResults);
        }
    }
}
