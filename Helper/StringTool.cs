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
                    return new List<string>();
                }

                if (useDecoding)
                {
                    return lines.AsParallel().Select(StringCipher.Decode).ToList();
                }
                else
                {
                    return lines.ToList();
                }
            }

            return new List<string>();
        }

        public static List<string> GetWords(string sentence)
        {
            if (IsNull(sentence))
            {
                return new List<string>();
            }

            char[] splitChars = { ' ', ',', '.', '!', '?', ';', ':', '-', '_', '(', ')', '[', ']', '{', '}', '\"', '\'', '\\', '/' };
            string[] wordsArray = sentence.Split(splitChars, StringSplitOptions.RemoveEmptyEntries);

            List<string> wordsList = new List<string>(wordsArray);

            return wordsList;
        }

        public static bool IsNull(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrEmpty(text))
            {
                return true;
            }

            return false;
        }
    }
}
