using PerformanceTestReportViewer.Definition;
using Unity.PerformanceTesting;

namespace PerformanceTestReportViewer.SampleDefinitions
{
    [SampleDefinitionContainer]
    public static class SystemSampleDefinitions
    {
        private static string Category => "System";

        public static readonly DefaultSampleDefinition TotalAllocatedMemorySize =
            new("Allocated Memory", Category, SampleUnit.Megabyte);

        public static readonly DefaultSampleDefinition MonoUsedMemorySize =
            new("Mono Used Memory", Category, SampleUnit.Megabyte);

        public static readonly DefaultSampleDefinition FPS =
            new("FPS", Category, SampleUnit.Undefined);

        public static readonly DefaultSampleDefinition FrameTime =
            new("FrameTime", Category, SampleUnit.Millisecond);
    }
}