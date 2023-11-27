using System;
using System.Collections.Generic;
using System.Linq;
using PerformanceTestReportViewer.Definition;
using Unity.PerformanceTesting.Data;

namespace PerformanceTestReportViewer
{
    public class MultipleTestResultComparison
    {
        private MultipleTestResultComparison(SampleElement[] elements, PerformanceTestResult[] testResults)
        {
            Elements = elements;
            TestResults = testResults;
        }

        public readonly SampleElement[] Elements;
        public readonly PerformanceTestResult[] TestResults; // shares same index

        public static bool TryCreate(PerformanceTestResult[] testResults, out MultipleTestResultComparison result, Func<SampleGroup, bool> filter)
        {
            result = null;
            if (testResults == null)
                return false;

            IEnumerable<SampleGroup> validSampleGroups = testResults.SelectMany(tr => tr.SampleGroups);
            if (filter != null)
                validSampleGroups = validSampleGroups.Where(filter);

            string[] uniqueSampleGroupNames = validSampleGroups.Select(g => g.Name).Distinct().ToArray();

            Dictionary<string, List<double?>> valuesByGroupName = new();
            foreach (string uniqueSampleGroupName in uniqueSampleGroupNames)
                valuesByGroupName[uniqueSampleGroupName] = new List<double?>();

            foreach (PerformanceTestResult performanceTestResult in testResults)
            {
                foreach (string groupName in uniqueSampleGroupNames)
                {
                    SampleGroup sampleGroup = performanceTestResult.SampleGroups.Find(g => g.Name == groupName);
                    if (sampleGroup == null)
                        continue;

                    valuesByGroupName[groupName].Add(sampleGroup.Average);
                }
            }

            List<SampleElement> elements = new();
            foreach ((string definition, List<double?> values) in valuesByGroupName)
            {
                var sampleGroupDefinition = TestInformationGetter.FindSampleGroupDefinition(definition, ViewerModule.Instance.ResultContext);
                if (sampleGroupDefinition == null)
                    continue;

                elements.Add(new SampleElement(sampleGroupDefinition, definition, values.ToArray()));
            }

            result = new MultipleTestResultComparison(
                elements.ToArray(),
                testResults
            );
            return true;
        }

        public SampleElement[][] GroupElementsByContext()
        {
            return Elements.GroupBy(e => e.SampleDefinition).Select(g => g.ToArray()).ToArray();
        }
    }
}