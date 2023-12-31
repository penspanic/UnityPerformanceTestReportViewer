using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using PerformanceTestReportViewer.Definition;

namespace PerformanceTestReportViewer
{
    public static class PerformanceTestReportViewerUtility
    {
        // [prefix][categoryName]Definition
        public static readonly Regex sampleGroupNameWithSampleTargetRegex = new(@"^\[([\w\.]+)\]\[([\w\.]+)\](.+)$");
        public static readonly Regex sampleGroupNameWithoutSampleTargetRegex = new(@"^\[([\w\.]+)\](.+)$");
        public static bool TryParseFromSampleGroupName(string sampleGroupName, out string sampleTarget, out string category, out string definition)
        {
            sampleTarget = string.Empty;
            category = string.Empty;
            definition = string.Empty;

            if (sampleGroupName == "Time")
            {
                definition = sampleGroupName;
                return true;
            }

            var match = sampleGroupNameWithSampleTargetRegex.Match(sampleGroupName);
            if (match.Success == false)
            {
                match = sampleGroupNameWithoutSampleTargetRegex.Match(sampleGroupName);
                if (match.Success == false)
                    return false;

                category = match.Groups[1].Value;
                definition = match.Groups[2].Value;
                return true;
            }

            if (match.Groups.Count != 4)
                return false;

            sampleTarget = match.Groups[1].Value;
            category = match.Groups[2].Value;
            definition = match.Groups[3].Value;
            return true;
        }

        public static string FormatSampleGroupName(string sampleTarget, string category, string definition)
        {
            if (string.IsNullOrEmpty(sampleTarget))
                return $"[{category}]{definition}";

            return $"[{sampleTarget}][{category}]{definition}";
        }

        private static readonly Regex parameterizedTestNameRegex = new Regex(@"(.*)\((.+)\)");
        public static bool IsParameterizedTest(string testName, out string testNameWithoutParameter, out string parameter)
        {
            // TODO: Not working when the parameter value contains '(' or ')' ...
            parameter = string.Empty;
            testNameWithoutParameter = string.Empty;
            var match = parameterizedTestNameRegex.Match(testName);

            if (match.Success == false)
                return false;

            if (match.Groups.Count != 3)
                return false;

            testNameWithoutParameter = match.Groups[1].Value;
            parameter = match.Groups[2].Value;
            return true;
        }

        

        public static IEnumerable<string> GetSampleTargets(this PerformanceTestResultContext resultContext)
        {
            if (resultContext == null || resultContext.Results == null || resultContext.Results.Length == 0)
                return Enumerable.Empty<string>();

            return resultContext.Results
                .SelectMany(t => t.SampleGroups)
                .Select(s =>
                {
                    bool result = TryParseFromSampleGroupName(s.Name, out string sampleTarget, out _, out _);
                    return (result, sampleTarget);
                })
                .Where(p => p.result && string.IsNullOrEmpty(p.sampleTarget) == false)
                .Select(p => p.sampleTarget)
                .Distinct();
        }

        

        public static bool TryExtractCommonStrings(string[] input, out string common, out string[] variations)
        {
            variations = null;
            common = FindCommonPrefix(input);
            if (string.IsNullOrEmpty(common))
                return false;

            variations = new string[input.Length];
            for (int i = 0; i < input.Length; ++i)
            {
                string remaining = input[i].Substring(common.Length);
                variations[i] = remaining;
            }

            return true;
        }

        private static string FindCommonPrefix(string[] strings)
        {
            if (strings == null || strings.Length == 0)
                return "";

            string firstString = strings[0];

            for (int i = 1; i < strings.Length; i++)
            {
                int j = 0;
                while (j < firstString.Length && j < strings[i].Length && firstString[j] == strings[i][j])
                {
                    j++;
                }

                firstString = firstString.Substring(0, j);

                if (string.IsNullOrEmpty(firstString))
                    break;
            }

            return firstString;
        }
    }
}