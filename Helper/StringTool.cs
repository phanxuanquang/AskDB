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
            return Markdown.ToPlainText(markdown).Trim();
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
    }
}
