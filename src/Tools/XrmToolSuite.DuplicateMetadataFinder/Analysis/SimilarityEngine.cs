using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace XrmToolSuite.DuplicateMetadataFinder.Analysis
{
    /// <summary>
    /// UI-free, SDK-free similarity primitives: name normalization, edit-distance and token-overlap
    /// ratios, and set overlap. All pure functions so they are exhaustively unit-testable.
    /// </summary>
    public static class TextSimilarity
    {
        /// <summary>Lowercase, strip non-alphanumerics, collapse — so "Phone Number" ~ "phone_number".</summary>
        public static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s.Trim().ToLowerInvariant())
                if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            return sb.ToString();
        }

        /// <summary>Levenshtein edit distance (case-sensitive on the already-normalized input).</summary>
        public static int Levenshtein(string a, string b)
        {
            a = a ?? string.Empty;
            b = b ?? string.Empty;
            if (a.Length == 0) return b.Length;
            if (b.Length == 0) return a.Length;

            var prev = new int[b.Length + 1];
            var curr = new int[b.Length + 1];
            for (var j = 0; j <= b.Length; j++) prev[j] = j;

            for (var i = 1; i <= a.Length; i++)
            {
                curr[0] = i;
                for (var j = 1; j <= b.Length; j++)
                {
                    var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    curr[j] = Math.Min(Math.Min(curr[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
                }
                var tmp = prev; prev = curr; curr = tmp;
            }
            return prev[b.Length];
        }

        /// <summary>Normalized-name closeness in 0..1 (1 = identical after normalization).</summary>
        public static double NameRatio(string a, string b)
        {
            var na = Normalize(a);
            var nb = Normalize(b);
            if (na.Length == 0 && nb.Length == 0) return 0d; // two blanks are not "similar"
            var max = Math.Max(na.Length, nb.Length);
            if (max == 0) return 0d;
            return 1d - (double)Levenshtein(na, nb) / max;
        }

        /// <summary>Split on camelCase, digits, and separators into lowercase tokens.</summary>
        public static IReadOnlyList<string> Tokenize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return Array.Empty<string>();
            var tokens = new List<string>();
            var sb = new StringBuilder();
            char prev = '\0';
            foreach (var ch in s)
            {
                var boundary = !char.IsLetterOrDigit(ch)
                    || (char.IsUpper(ch) && char.IsLower(prev))
                    || (char.IsDigit(ch) != char.IsDigit(prev) && prev != '\0');
                if (boundary && sb.Length > 0) { tokens.Add(sb.ToString().ToLowerInvariant()); sb.Clear(); }
                if (char.IsLetterOrDigit(ch)) sb.Append(ch);
                prev = ch;
            }
            if (sb.Length > 0) tokens.Add(sb.ToString().ToLowerInvariant());
            return tokens;
        }

        /// <summary>Jaccard overlap of the two token sets in 0..1.</summary>
        public static double TokenOverlap(string a, string b) =>
            Jaccard(Tokenize(a), Tokenize(b));

        /// <summary>Jaccard overlap of two string sets (case-insensitive), 0..1.</summary>
        public static double Jaccard(IEnumerable<string> a, IEnumerable<string> b)
        {
            var sa = new HashSet<string>((a ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToLowerInvariant()));
            var sb = new HashSet<string>((b ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim().ToLowerInvariant()));
            if (sa.Count == 0 && sb.Count == 0) return 0d;
            var inter = sa.Count(sb.Contains);
            var union = sa.Count + sb.Count - inter;
            return union == 0 ? 0d : (double)inter / union;
        }

        /// <summary>Stable SHA-256 hex of content, for exact web-resource/JS duplicate detection.</summary>
        public static string ContentHash(string content)
        {
            if (content == null) return null;
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(content));
                var sb = new StringBuilder(bytes.Length * 2);
                foreach (var b in bytes) sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                return sb.ToString();
            }
        }
    }

    /// <summary>
    /// Scores a pair of same-kind components 0..100 from weighted factors and groups scored pairs into
    /// clusters (union-find over pairs at/above the threshold). Read-only: recommends a primary, never merges.
    /// </summary>
    public static class SimilarityEngine
    {
        /// <summary>
        /// Score two same-kind components. Returns 0 (with no factors) for a cross-kind pair so callers
        /// never accidentally group across kinds. An identical, non-empty content hash short-circuits to 100.
        /// </summary>
        public static DuplicatePair Score(MetadataComponent a, MetadataComponent b)
        {
            if (a == null || b == null) throw new ArgumentNullException(a == null ? nameof(a) : nameof(b));
            if (a.Kind != b.Kind)
                return new DuplicatePair(a, b, 0, new List<SimilarityFactor>(), false);

            // Exact content match (web resource / JS) is definitive.
            if (!string.IsNullOrEmpty(a.ContentHash) && a.ContentHash == b.ContentHash)
            {
                var exact = new List<SimilarityFactor> { new SimilarityFactor("content-hash", 1d, 1d) };
                return new DuplicatePair(a, b, 100, exact, isExactContentMatch: true);
            }

            var factors = new List<SimilarityFactor>();
            void Add(string name, double value, double weight)
            {
                if (weight > 0) factors.Add(new SimilarityFactor(name, Clamp01(value), weight));
            }

            // Display + schema name closeness applies to every kind that has them.
            Add("display-name", TextSimilarity.NameRatio(a.DisplayName, b.DisplayName), 0.45);
            Add("schema-name", TextSimilarity.NameRatio(a.SchemaName, b.SchemaName),
                string.IsNullOrWhiteSpace(a.SchemaName) && string.IsNullOrWhiteSpace(b.SchemaName) ? 0d : 0.20);
            Add("token-overlap", TextSimilarity.TokenOverlap(
                a.DisplayName ?? a.SchemaName, b.DisplayName ?? b.SchemaName), 0.15);

            // Data-type agreement matters for columns / option sets / plugin steps.
            if (!string.IsNullOrWhiteSpace(a.DataType) || !string.IsNullOrWhiteSpace(b.DataType))
                Add("data-type", SameType(a.DataType, b.DataType) ? 1d : 0d, 0.15);

            // Option-value overlap for option sets / picklists.
            var hasOptions = (a.OptionValues?.Count ?? 0) > 0 || (b.OptionValues?.Count ?? 0) > 0;
            if (hasOptions)
                Add("option-overlap", TextSimilarity.Jaccard(a.OptionValues, b.OptionValues), 0.25);

            // Description similarity is a light tie-breaker when present.
            if (!string.IsNullOrWhiteSpace(a.Description) && !string.IsNullOrWhiteSpace(b.Description))
                Add("description", TextSimilarity.NameRatio(a.Description, b.Description), 0.10);

            var totalWeight = factors.Sum(f => f.Weight);
            var score = totalWeight <= 0
                ? 0
                : (int)Math.Round(factors.Sum(f => f.Value * f.Weight) / totalWeight * 100d,
                    MidpointRounding.AwayFromZero);

            return new DuplicatePair(a, b, Clamp0100(score), factors, isExactContentMatch: false);
        }

        /// <summary>
        /// Group components into duplicate clusters. Compares only within each kind (blocking), keeps pairs
        /// at/above <paramref name="threshold"/>, then unions transitively-linked members into groups.
        /// </summary>
        public static DuplicateScanResult Group(IEnumerable<MetadataComponent> components, int threshold)
        {
            threshold = Clamp0100(threshold);
            var result = new DuplicateScanResult { Threshold = threshold };
            if (components == null) return result;

            var all = components.Where(c => c != null).ToList();

            foreach (var kindGroup in all.GroupBy(c => c.Kind))
            {
                var items = kindGroup.ToList();
                if (items.Count < 2) continue;

                var uf = new UnionFind(items.Count);
                var pairs = new List<DuplicatePair>();
                for (var i = 0; i < items.Count; i++)
                for (var j = i + 1; j < items.Count; j++)
                {
                    var pair = Score(items[i], items[j]);
                    if (pair.Score >= threshold)
                    {
                        pairs.Add(pair);
                        uf.Union(i, j);
                    }
                }

                foreach (var cluster in uf.Clusters().Where(c => c.Count > 1))
                {
                    var members = new HashSet<int>(cluster);
                    var group = new DuplicateGroup { Kind = kindGroup.Key };
                    group.Members.AddRange(cluster.Select(idx => items[idx]));
                    group.Pairs.AddRange(pairs.Where(p =>
                        members.Contains(items.IndexOf(p.A)) && members.Contains(items.IndexOf(p.B))));
                    result.Groups.Add(group);
                }
            }

            return result;
        }

        private static bool SameType(string a, string b) =>
            string.Equals((a ?? string.Empty).Trim(), (b ?? string.Empty).Trim(),
                StringComparison.OrdinalIgnoreCase);

        private static double Clamp01(double v) => v < 0 ? 0 : v > 1 ? 1 : v;
        private static int Clamp0100(int v) => v < 0 ? 0 : v > 100 ? 100 : v;

        /// <summary>Minimal union-find for clustering scored pairs into groups.</summary>
        private sealed class UnionFind
        {
            private readonly int[] _parent;
            public UnionFind(int n)
            {
                _parent = new int[n];
                for (var i = 0; i < n; i++) _parent[i] = i;
            }
            private int Find(int x) => _parent[x] == x ? x : (_parent[x] = Find(_parent[x]));
            public void Union(int a, int b) => _parent[Find(a)] = Find(b);
            public IEnumerable<List<int>> Clusters()
            {
                var map = new Dictionary<int, List<int>>();
                for (var i = 0; i < _parent.Length; i++)
                {
                    var root = Find(i);
                    if (!map.TryGetValue(root, out var list)) map[root] = list = new List<int>();
                    list.Add(i);
                }
                return map.Values;
            }
        }
    }
}
