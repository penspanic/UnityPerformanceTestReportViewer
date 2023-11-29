using System;
using System.Collections.Generic;
using System.Linq;
using PerformanceTestReportViewer.Definition;
using PerformanceTestReportViewer.Editor.UI;

namespace PerformanceTestReportViewer.Editor
{
    internal static class Utility
    {
        public static IEnumerable<string> GetTags(this PerformanceTestResultContext resultContext)
        {
            if (resultContext == null || resultContext.Results == null || resultContext.Results.Length == 0)
                return Enumerable.Empty<string>();

            return resultContext.GetSampleGroupDefinitions().SelectMany(g => g.Tags).Distinct();
        }

        public static IEnumerable<T> TrySort<T, T2>(this IEnumerable<T> collection, Func<T, T2> keySelector, SortMethod sortMethod)
        {
            switch (sortMethod)
            {
                case SortMethod.None:
                    return collection;
                case SortMethod.Ascending:
                    return collection.OrderBy(keySelector);
                case SortMethod.Descending:
                    return collection.OrderByDescending(keySelector);
                default:
                    throw new Exception($"Unhandled case : {sortMethod}");
            }
        }

        public static IEnumerable<ISampleDefinition> GetSampleGroupDefinitions(this PerformanceTestResultContext resultContext)
        {
            return resultContext.Results
                .SelectMany(t => t.SampleGroups)
                .Select(s => s.Name)
                .Distinct()
                .Select(name => TestInformationGetter.FindSampleGroupDefinition(name, resultContext))
                .Where(v => v != null).Distinct();
        }

        public static string GetSampleUnit(this SampleElement[] elements)
        {
            if (elements == null || elements.Length == 0)
                return string.Empty;

            var units = elements.Select(e => e.SampleDefinition.SampleUnit).Distinct().ToArray();
            if (units.Length == 1)
                return units.First().ToString();

            return string.Empty;
        }
    }
}