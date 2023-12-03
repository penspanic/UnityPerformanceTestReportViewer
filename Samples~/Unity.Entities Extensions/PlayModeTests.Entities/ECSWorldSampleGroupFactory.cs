using System.Collections.Generic;
using System.Linq;
using PerformanceTestReportViewer.Definition;
using Unity.PerformanceTesting;
using Unity.PerformanceTesting.Data;
using SampleGroup = Unity.PerformanceTesting.Data.SampleGroup;

namespace PerformanceTestReportViewer.Samples_.PlayModeTests.Entities
{
    // ReSharper disable once ClassNeverInstantiated.Global
    [SampleDefinitionContainer]
    public class ECSWorldSampleGroupFactory : ISampleDefinitionFactory
    {
        public string Category => CategoryName;
        public static string CategoryName => "World";
        public ISampleDefinition[] Create(PerformanceTestResultContext context)
        {
            HashSet<ISampleDefinition> sampleGroups = new();
            foreach (PerformanceTestResult performanceTestResult in context.Results)
            {
                foreach (SampleGroup sampleGroup in performanceTestResult.SampleGroups)
                {
                    if (PerformanceTestReportViewerUtility.TryParseFromSampleGroupName(sampleGroup.Name, out string contextName, out string categoryName, out string definitionName) == false)
                        continue;

                    if (categoryName != Category)
                        continue;

                    List<string> tags = new();
                    if (definitionName.EndsWith("Group"))
                        tags.Add("SystemGroup");

                    sampleGroups.Add(new DefaultSampleDefinition(definitionName, Category, SampleUnit.Millisecond, tags: tags.ToArray()));
                }
            }

            return sampleGroups.ToArray();
        }
    }
}