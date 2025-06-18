using FuzzySharp;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace DatabaseInteractor.Helpers
{
    public class TableNameSearcher(List<string> tableNames, int parallizationActivationThreshold = 1000)
    {
        private static readonly Regex CamelCaseRegex = new("([a-z])([A-Z])", RegexOptions.Compiled);
        private readonly int _parallizationActivationThreshold = parallizationActivationThreshold;

        private readonly List<(string Original, string Preprocessed)> _tables = tableNames.Count < parallizationActivationThreshold
                ? tableNames.Select(name => (Original: name, Preprocessed: Preprocess(name))).ToList()
                : tableNames.AsParallel().Select(name => (Original: name, Preprocessed: Preprocess(name))).ToList();

        public List<string> SearchByTopN(string keyword, int topN = 5)
        {
            var processedKeyword = Preprocess(keyword);

            if (_tables.Count < _parallizationActivationThreshold)
            {
                return _tables
                    .Select(entry => (entry.Original, Score: Fuzz.WeightedRatio(processedKeyword, entry.Preprocessed)))
                    .OrderByDescending(x => x.Score)
                    .Take(topN)
                    .Select(x => x.Original)
                    .ToList();
            }

            return _tables
                .AsParallel()
                .AsOrdered()
                .Select(entry => (entry.Original, Score: Fuzz.WeightedRatio(processedKeyword, entry.Preprocessed)))
                .OrderByDescending(x => x.Score)
                .Take(topN)
                .Select(x => x.Original)
                .ToList();
        }

        public List<string> SearchByThreshold(string keyword, int threshold = 65)
        {
            var processedKeyword = Preprocess(keyword);

            if (_tables.Count < _parallizationActivationThreshold)
            {
                return _tables
                 .Select(entry => (entry.Original, Score: Fuzz.WeightedRatio(processedKeyword, entry.Preprocessed)))
                 .Where(x => x.Score >= threshold)
                 .OrderByDescending(x => x.Score)
                 .Select(x => x.Original)
                 .ToList();
            }

            return _tables
                .AsParallel()
                .AsOrdered()
                .Select(entry => (entry.Original, Score: Fuzz.WeightedRatio(processedKeyword, entry.Preprocessed)))
                .Where(x => x.Score >= threshold)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Original)
                .ToList();
        }

        private static string Preprocess(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            input = RemoveDiacritics(input);
            input = SplitCamelCase(input);
            input = input.Replace("_", " ");
            input = input.ToLowerInvariant();
            input = input.Replace(" ", "");

            return input;
        }

        private static string RemoveDiacritics(string input)
        {
            var normalized = input.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();
            foreach (var c in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                    builder.Append(c);
            }
            return builder.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string SplitCamelCase(string input)
        {
            return CamelCaseRegex.Replace(input, "$1 $2");
        }
    }
}
