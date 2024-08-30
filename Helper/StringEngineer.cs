﻿using System.Text.RegularExpressions;

namespace Helper
{
    public static class StringEngineer
    {
        public static async Task<List<string>> GetWords(string path)
        {
            var words = new List<string>();

            if (File.Exists(path))
            {
                var sqlKeywords = await File.ReadAllLinesAsync(path);
                words.AddRange(sqlKeywords);
            }

            return words;
        }

        public static string GetLastWord(string sentence)
        {
            if (string.IsNullOrWhiteSpace(sentence))
            {
                return string.Empty;
            }
            string cleanedSentence = Regex.Replace(sentence, @"[\p{P}-[.]]+", " ");

            string[] words = cleanedSentence.Trim().Split(' ');

            return words.Length > 0 ? words[words.Length - 1] : string.Empty;
        }

        public static string ReplaceLastOccurrence(string text, string oldString, string newString)
        {
            int place = text.ToUpper().LastIndexOf(oldString.ToUpper());

            if (place == -1)
            {
                return text;
            }

            return text.Remove(place, oldString.Length).Insert(place, newString);
        }
    }
}