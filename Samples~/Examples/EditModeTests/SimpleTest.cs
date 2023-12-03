using System;
using NUnit.Framework;
using PerformanceTestReportViewer.Definition;
using PerformanceTestReportViewer.Editor;
using Unity.PerformanceTesting;

namespace PerformanceTestReportViewer.Samples_.EditModeTests
{
    [SampleDefinitionContainer]
    public static class SimpleTestDefinitions
    {
        public const string Category = "SimpleTest";
        public static readonly ISampleDefinition Value1 = new DefaultSampleDefinition("Value1", Category, SampleUnit.Byte);
        public static readonly ISampleDefinition Value2 = new DefaultSampleDefinition("Value2", Category, SampleUnit.Byte);
        public static readonly ISampleDefinition Value3 = new DefaultSampleDefinition("Value3", Category, SampleUnit.Byte);
    }

    public class SimpleTest
    {
        [Test, Performance]
        public void Test()
        {
            using var sampleGroups = new SampleGroups();
            using (sampleGroups.CreateScope(SimpleTestDefinitions.Value1))
                Console.WriteLine("Do something");

            using (sampleGroups.CreateScope(SimpleTestDefinitions.Value2))
                Console.WriteLine("Do something");

            using (sampleGroups.CreateScope(SimpleTestDefinitions.Value3))
                Console.WriteLine("Do something");
        }
    }
}