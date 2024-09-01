using System.Text.RegularExpressions;

namespace Helper
{
    public static class StringEngineer
    {
        public static async Task<List<string>> GetLines(string path, bool useDecoding = false)
        {
            if (File.Exists(path))
            {
                var lines = await File.ReadAllLinesAsync(path);

                if(lines.Length == 0)
                {
                    return new List<string>();
                }

                if (useDecoding)
                {
                    return lines.Select(StringCipher.Decode).ToList();
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

        public static string GetLastWord(string sentence)
        {
            if (IsNull(sentence))
            {
                return string.Empty;
            }
            string cleanedSentence = Regex.Replace(sentence, @"[\p{P}-[.]]+", " ");

            string[] words = cleanedSentence.Trim().Split(' ');

            return words.Length > 0 ? words[words.Length - 1] : string.Empty;
        }

        public static string ReplaceLastWord(string text, string oldString, string newString)
        {
            int place = text.LastIndexOf(oldString, StringComparison.OrdinalIgnoreCase);

            if (place == -1)
            {
                return text;
            }

            return text.Remove(place, oldString.Length).Insert(place, newString);
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
