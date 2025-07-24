using AllMiniLmL6V2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AskDB.SemanticKernel.Helpers
{
    public class SemanticSearcher
    {
        public List<IEnumerable<float>> GetEmbeddings(IEnumerable<string> words)
        {
            using var embedder = new AllMiniLmL6V2Embedder();
            return embedder.GenerateEmbeddings(words).ToList();
        }

        // Compute cosine similarity between two float vectors
        private static float CosineSimilarity(IReadOnlyList<float> v1, IReadOnlyList<float> v2)
        {
            if (v1.Count != v2.Count) throw new ArgumentException("Vectors must be of same length");
            float dot = 0, norm1 = 0, norm2 = 0;
            for (int i = 0; i < v1.Count; i++)
            {
                dot += v1[i] * v2[i];
                norm1 += v1[i] * v1[i];
                norm2 += v2[i] * v2[i];
            }
            return (float)(dot / (Math.Sqrt(norm1) * Math.Sqrt(norm2)));
        }

        // Get top N words with highest semantic similarity to the query
        public List<(string Word, float Similarity)> GetTopNSimilarWords(string query, IEnumerable<string> words, int n)
        {
            var wordList = words.ToList();
            using var embedder = new AllMiniLmL6V2Embedder();
            var queryEmbedding = embedder.GenerateEmbeddings(new[] { query }).First().ToArray();
            var wordEmbeddings = embedder.GenerateEmbeddings(wordList).Select(e => e.ToArray()).ToList();

            var similarities = wordList.Select((word, i) => (Word: word, Similarity: CosineSimilarity(queryEmbedding, wordEmbeddings[i])))
                .OrderByDescending(x => x.Similarity)
                .Take(n)
                .ToList();
            return similarities;
        }

        // Get all words with semantic similarity above a threshold
        public List<(string Word, float Similarity)> GetWordsAboveSimilarity(string query, IEnumerable<string> words, float threshold)
        {
            var wordList = words.ToList();
            using var embedder = new AllMiniLmL6V2Embedder();
            var queryEmbedding = embedder.GenerateEmbeddings(new[] { query }).First().ToArray();
            var wordEmbeddings = embedder.GenerateEmbeddings(wordList).Select(e => e.ToArray()).ToList();

            var similarities = wordList.Select((word, i) => (Word: word, Similarity: CosineSimilarity(queryEmbedding, wordEmbeddings[i])))
                .Where(x => x.Similarity >= threshold)
                .OrderByDescending(x => x.Similarity)
                .ToList();
            return similarities;
        }
    }
}
