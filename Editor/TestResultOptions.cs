using System.Collections.Generic;
using System.Linq;
using PerformanceTestReportViewer.Definition;
using Unity.PerformanceTesting.Data;

namespace PerformanceTestReportViewer
{
    public class TestResultOptions
    {
        private static TestResultOptions defaultOptions = new();
        private static readonly Dictionary<string, TestResultOptions> optionsByName = new();
        public static TestResultOptions Get(params PerformanceTestResult[] testResults)
        {
            if (testResults == null)
                return defaultOptions;

            string key = string.Join(",", testResults.OrderBy(t => t.Name).Select(t => t.Name));
            if (optionsByName.TryGetValue(key, out var options) == false)
            {
                options = new();
                optionsByName[key] = options;
            }

            return options;
        }

        private readonly Dictionary<string, bool> tagToggles = new();
        public bool IsTagOn(string tag)
        {
            if (tagToggles.TryGetValue(tag, out bool value))
                return value;

            return true;
        }

        public void SetIsTagOn(string tag, bool value)
        {
            tagToggles[tag] = value;
        }

        public bool MustShown(ISampleDefinition definition)
        {
            if (definition == null)
                return false;

            foreach (string tag in definition.Tags ?? Enumerable.Empty<string>())
            {
                if (IsTagOn(tag))
                    return true;
            }

            return (definition.Tags?.Length ?? 0) == 0;
        }
    }
}