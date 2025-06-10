namespace DatabaseInteractor.Helpers
{
    public class SimilaritySearchHelper
    {
        private sealed class ProcessedCandidate(string candidate)
        {
            public string Original { get; } = candidate;
            public string Normalized { get; } = NormalizeString(candidate);
        }

        private readonly List<ProcessedCandidate> _processedCandidates;
        private readonly int _topN;

        private static string NormalizeString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            return input.Replace("_", " ").ToLowerInvariant();
        }

        public SimilaritySearchHelper(IEnumerable<string> candidates, int topN = 10)
        {
            if (candidates == null)
                throw new ArgumentNullException(nameof(candidates));
            if (topN <= 0)
                throw new ArgumentOutOfRangeException(nameof(topN), "topN must be positive.");

            _processedCandidates = candidates.Select(c => new ProcessedCandidate(c)).ToList();
            _topN = topN;
        }

        public List<string> LevenshteinSearch(string keyword)
        {
            string normalizedKeyword = NormalizeString(keyword);

            return _processedCandidates
                .AsParallel()
                .AsOrdered()
                .Select(pc =>
                {
                    int distance = LevenshteinDistanceOptimized(normalizedKeyword, pc.Normalized);
                    int maxLen = Math.Max(normalizedKeyword.Length, pc.Normalized.Length);
                    double score = (maxLen == 0) ? 1.0 : (1.0 - (double)distance / maxLen);
                    return new { Candidate = pc.Original, Score = score };
                })
                .OrderByDescending(x => x.Score)
                .Take(_topN)
                .Select(x => x.Candidate)
                .ToList();
        }

        private static int LevenshteinDistanceOptimized(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return string.IsNullOrEmpty(t) ? 0 : t.Length;
            if (string.IsNullOrEmpty(t)) return s.Length;

            if (s.Length > t.Length)
            {
                string temp = s;
                s = t;
                t = temp;
            }

            int sLen = s.Length;
            int tLen = t.Length;
            int[] v0 = new int[sLen + 1];
            int[] v1 = new int[sLen + 1];

            for (int i = 0; i <= sLen; i++)
                v0[i] = i;

            for (int i = 0; i < tLen; i++)
            {
                v1[0] = i + 1;

                for (int j = 0; j < sLen; j++)
                {
                    int cost = (s[j] == t[i]) ? 0 : 1;
                    v1[j + 1] = Math.Min(Math.Min(
                        v1[j] + 1,
                        v0[j + 1] + 1),
                        v0[j] + cost);
                }
                Array.Copy(v1, v0, sLen + 1);
            }
            return v1[sLen];
        }

        public List<string> JaroWinklerSearch(string keyword)
        {
            string normalizedKeyword = NormalizeString(keyword);

            return _processedCandidates
                .AsParallel()
                .AsOrdered()
                .Select(pc => new
                {
                    Candidate = pc.Original,
                    Score = JaroWinklerSimilarity(normalizedKeyword, pc.Normalized)
                })
                .OrderByDescending(x => x.Score)
                .Take(_topN)
                .Select(x => x.Candidate)
                .ToList();
        }

        private static double JaroWinklerSimilarity(string s1, string s2)
        {
            int len1 = s1.Length;
            int len2 = s2.Length;

            if (len1 == 0) return len2 == 0 ? 1.0 : 0.0;

            int matchDistance = Math.Max(len1, len2) / 2 - 1;
            if (matchDistance < 0) matchDistance = 0;

            bool[] s1Matches = new bool[len1];
            bool[] s2Matches = new bool[len2];
            int matches = 0;

            for (int i = 0; i < len1; i++)
            {
                int start = Math.Max(0, i - matchDistance);
                int end = Math.Min(i + matchDistance + 1, len2);

                for (int j = start; j < end; j++)
                {
                    if (s2Matches[j] || s1[i] != s2[j]) continue;
                    s1Matches[i] = true;
                    s2Matches[j] = true;
                    matches++;
                    break;
                }
            }

            if (matches == 0) return 0.0;

            int transpositions = 0;
            int k = 0;
            for (int i = 0; i < len1; i++)
            {
                if (!s1Matches[i]) continue;
                while (!s2Matches[k]) k++;
                if (s1[i] != s2[k]) transpositions++;
                k++;
            }

            double jaro = ((matches / (double)len1) +
                           (matches / (double)len2) +
                           ((matches - transpositions / 2.0) / matches)) / 3.0;

            int prefix = 0;
            for (int i = 0; i < Math.Min(4, Math.Min(len1, len2)); i++)
            {
                if (s1[i] != s2[i]) break;
                prefix++;
            }

            return jaro + (0.1 * prefix * (1.0 - jaro));
        }

        public List<string> NgramSearch(string keyword, int n = 2)
        {
            if (n <= 0)
                throw new ArgumentOutOfRangeException(nameof(n), "n must be positive for N-gram similarity.");

            string normalizedKeyword = NormalizeString(keyword);
            HashSet<string> keywordNgrams = GetNgramsForString(normalizedKeyword, n);

            return _processedCandidates
                .AsParallel()
                .AsOrdered()
                .Select(pc =>
                {
                    HashSet<string> candidateNgrams = GetNgramsForString(pc.Normalized, n);
                    return new
                    {
                        Candidate = pc.Original,
                        Score = CalculateNgramSimilarity(keywordNgrams, candidateNgrams)
                    };
                })
                .OrderByDescending(x => x.Score)
                .Take(_topN)
                .Select(x => x.Candidate)
                .ToList();
        }

        private static double CalculateNgramSimilarity(HashSet<string> ngrams1, HashSet<string> ngrams2)
        {
            if (ngrams1.Count == 0 && ngrams2.Count == 0) return 1.0;
            if (ngrams1.Count == 0 || ngrams2.Count == 0) return 0.0;

            int intersect = ngrams1.Intersect(ngrams2).Count();
            int unionCount = ngrams1.Count + ngrams2.Count - intersect;

            return unionCount == 0 ? 0 : (double)intersect / unionCount;
        }

        private static HashSet<string> GetNgramsForString(string normalizedInput, int n)
        {
            var set = new HashSet<string>();
            if (normalizedInput.Length < n)
            {
                return set;
            }

            for (int i = 0; i <= normalizedInput.Length - n; i++)
            {
                set.Add(normalizedInput.Substring(i, n));
            }
            return set;
        }
    }
}
