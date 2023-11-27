using Unity.PerformanceTesting;
using Unity.PerformanceTesting.Data;

namespace PerformanceTestReportViewer.Definition
{

    public class PerformanceTestResultContext
    {
        public PerformanceTestResultContext(PerformanceTestResult[] results)
        {
            Results = results;
        }

        public PerformanceTestResult[] Results { get; }
    }

    public interface ISampleDefinitionFactory
    {
        string Category { get; }
        ISampleDefinition[] Create(PerformanceTestResultContext context);
    }

    public interface ISampleDefinition
    {
        public string Name { get; }
        public string Category { get; }
        public string[] Tags { get; }
        public SampleUnit SampleUnit { get; }
        public bool? IncreaseIsBetter { get; }
    }

    public class DefaultSampleDefinition : ISampleDefinition
    {
        public string Name { get; }
        public string Category { get; }
        public string[] Tags { get; }
        public SampleUnit SampleUnit { get; }
        public bool? IncreaseIsBetter { get; }

        public DefaultSampleDefinition(string name, string category, SampleUnit sampleUnit, bool? increaseIsBetter = null, params string[] tags)
        {
            Name = name;
            Category = category;
            Tags = tags;
            SampleUnit = sampleUnit;
            IncreaseIsBetter = increaseIsBetter;
        }
    }
}