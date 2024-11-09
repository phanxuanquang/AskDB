using Markdig;

namespace Helper
{
    public static class StringTool
    {
        public static async Task<List<string>> GetLines(string path, bool useDecoding = false)
        {
            if (File.Exists(path))
            {
                var lines = await File.ReadAllLinesAsync(path);

                if (lines.Length == 0)
                {
                    return [];
                }

                if (useDecoding)
                {
                    return lines.AsParallel().Select(StringCipher.Decode).ToList();
                }
                else
                {
                    return [.. lines];
                }
            }

            return [];
        }

        public static bool IsNull(string text)
        {
            return string.IsNullOrWhiteSpace(text) || string.IsNullOrEmpty(text);
        }

        public static string AsPlainText(string markdown)
        {
            return Markdown.ToPlainText(markdown).Replace("  ", " ").Trim();
        }

        public static string EscapeCsvValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                value = value.Replace("\"", "\"\"");
                return $"\"{value}\"";
            }

            return value;
        }

        public static double GetSimilarity(string a, string b)
        {
            a = a.ToLowerInvariant();
            b = b.ToLowerInvariant();

            int m = a.Length;
            int n = b.Length;

            if (m == 0 || n == 0)
            {
                return 0;
            }

            int[] prev = new int[n + 1];
            int[] curr = new int[n + 1];

            for (int j = 0; j <= n; j++) prev[j] = j;

            bool earlyTermination = false;

            for (int i = 1; i <= m; i++)
            {
                curr[0] = i;

                for (int j = 1; j <= n; j++)
                {
                    int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;

                    curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);

                    if (curr[j] > m / 2)
                    {
                        earlyTermination = true;
                        break;
                    }
                }

                (curr, prev) = (prev, curr);
                if (earlyTermination)
                {
                    break;
                }
            }

            int distance = prev[n];
            int maxLength = Math.Max(m, n);

            return 1.0 - (double)distance / maxLength;
        }
    }
}
