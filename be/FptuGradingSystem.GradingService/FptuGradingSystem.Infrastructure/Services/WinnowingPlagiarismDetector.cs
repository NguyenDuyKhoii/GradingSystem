using FptuGradingSystem.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FptuGradingSystem.Infrastructure.Services
{
    public class WinnowingPlagiarismDetector : IPlagiarismDetectorService
    {
        private const int KGramLength = 15;
        private const int WindowSize = 8;

        public List<SimilarityComparisonResult> AnalyzeSubmissions(
            List<SubmissionCodeContent> submissions,
            decimal thresholdPercentage)
        {
            var results = new List<SimilarityComparisonResult>();

            if (submissions == null || submissions.Count < 2)
            {
                return results;
            }

            // Step 1: Precompute fingerprints for each submission
            var fingerprints = submissions.ToDictionary(
                s => s.SubmissionId,
                s => ExtractFingerprints(NormalizeCode(s.Content))
            );

            // Step 2: Pairwise comparison O(N^2)
            for (int i = 0; i < submissions.Count; i++)
            {
                for (int j = i + 1; j < submissions.Count; j++)
                {
                    var subA = submissions[i];
                    var subB = submissions[j];

                    var fpA = fingerprints[subA.SubmissionId];
                    var fpB = fingerprints[subB.SubmissionId];

                    decimal similarity = CalculateJaccardSimilarity(fpA, fpB);

                    if (similarity >= thresholdPercentage)
                    {
                        var matchCount = fpA.Intersect(fpB).Count();
                        var totalCount = fpA.Union(fpB).Count();

                        results.Add(new SimilarityComparisonResult(
                            subA.SubmissionId,
                            subA.StudentId,
                            subA.StudentName,
                            subB.SubmissionId,
                            subB.StudentId,
                            subB.StudentName,
                            Math.Round(similarity, 2),
                            $"Phát hiện {matchCount}/{totalCount} vân tay mã nguồn (Winnowing fingerprints) trùng khớp."
                        ));
                    }
                }
            }

            return results.OrderByDescending(r => r.SimilarityPercentage).ToList();
        }

        private static string NormalizeCode(string? code)
        {
            if (string.IsNullOrWhiteSpace(code)) return string.Empty;

            // Remove multi-line comments /* ... */
            var noMultiComments = Regex.Replace(code, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);

            // Remove single-line comments //... and #...
            var noComments = Regex.Replace(noMultiComments, @"(//|#).*?$", string.Empty, RegexOptions.Multiline);

            // Remove all whitespace, line breaks, and convert to lower
            var normalized = Regex.Replace(noComments, @"\s+", string.Empty).ToLowerInvariant();

            return normalized;
        }

        private static HashSet<ulong> ExtractFingerprints(string text)
        {
            var fingerprints = new HashSet<ulong>();

            if (text.Length < KGramLength)
            {
                if (text.Length > 0)
                {
                    fingerprints.Add(ComputeFnvHash(text));
                }
                return fingerprints;
            }

            // Generate hashes for all k-grams
            var hashes = new List<ulong>();
            for (int i = 0; i <= text.Length - KGramLength; i++)
            {
                var kgram = text.Substring(i, KGramLength);
                hashes.Add(ComputeFnvHash(kgram));
            }

            // Winnowing algorithm: select minimum hash in each sliding window
            if (hashes.Count <= WindowSize)
            {
                if (hashes.Count > 0)
                {
                    fingerprints.Add(hashes.Min());
                }
                return fingerprints;
            }

            for (int i = 0; i <= hashes.Count - WindowSize; i++)
            {
                ulong minHash = ulong.MaxValue;
                for (int w = 0; w < WindowSize; w++)
                {
                    if (hashes[i + w] < minHash)
                    {
                        minHash = hashes[i + w];
                    }
                }
                fingerprints.Add(minHash);
            }

            return fingerprints;
        }

        private static ulong ComputeFnvHash(string str)
        {
            // 64-bit FNV-1a hash
            ulong hash = 14695981039346656037UL;
            foreach (char c in str)
            {
                hash ^= c;
                hash *= 1099511628211UL;
            }
            return hash;
        }

        private static decimal CalculateJaccardSimilarity(HashSet<ulong> setA, HashSet<ulong> setB)
        {
            if (setA.Count == 0 || setB.Count == 0)
            {
                return 0m;
            }

            int intersectionCount = 0;
            // Iterate over smaller set for performance
            var smaller = setA.Count <= setB.Count ? setA : setB;
            var larger = setA.Count <= setB.Count ? setB : setA;

            foreach (var item in smaller)
            {
                if (larger.Contains(item))
                {
                    intersectionCount++;
                }
            }

            int unionCount = setA.Count + setB.Count - intersectionCount;

            if (unionCount == 0) return 0m;

            return ((decimal)intersectionCount / unionCount) * 100.0m;
        }
    }
}
