using System.Linq;
using PerformanceTestReportViewer.Definition;

namespace PerformanceTestReportViewer
{
    public class SampleElement
    {
        public SampleElement(ISampleDefinition sampleDefinition, string rawName, double?[] values)
        {
            SampleDefinition = sampleDefinition;
            RawName = rawName;
            Values = values;
            Average = values.Where(v => v.HasValue).Select(v => v.Value).Average();
        }

        public readonly ISampleDefinition SampleDefinition;
        public readonly string RawName;
        public readonly double?[] Values; // shares same index
        public readonly double Average;

        public bool MustShown(ViewerOptions options, TestResultOptions testResultOptions)
        {
            if (testResultOptions != null && testResultOptions.MustShown(SampleDefinition) == false)
                return false;

            if (Utility.TryParseFromSampleGroupName(RawName, out string context, out string category, out string definition) == false)
                return false;

            if (options.IsSampleTargetOn(context) == false)
                return false;

            return options.IsOn(category, definition);
        }
    }
}