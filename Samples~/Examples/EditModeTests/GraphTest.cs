using System;
using NUnit.Framework;
using PerformanceTestReportViewer.Definition;
using PerformanceTestReportViewer.Editor;
using Unity.PerformanceTesting;

namespace PerformanceTestReportViewer.Samples_.EditModeTests
{
    [SampleDefinitionContainer]
    public static class GraphTestDefinitions
    {
        public const string Category = "GraphTest";
        public static readonly ISampleDefinition Value1 = new DefaultSampleDefinition("Value1", Category, SampleUnit.Byte);
        public static readonly ISampleDefinition Value2 = new DefaultSampleDefinition("Value2", Category, SampleUnit.Byte);
        public static readonly ISampleDefinition Value3 = new DefaultSampleDefinition("Value3", Category, SampleUnit.Byte);
        public static readonly ISampleDefinition Value4 = new DefaultSampleDefinition("Value4", Category, SampleUnit.Byte);
        public static readonly ISampleDefinition Value5 = new DefaultSampleDefinition("Value5", Category, SampleUnit.Byte);
        public static readonly ISampleDefinition Value6 = new DefaultSampleDefinition("Value6", Category, SampleUnit.Byte);
        public static readonly ISampleDefinition Value7 = new DefaultSampleDefinition("Value7", Category, SampleUnit.Byte);
        public static readonly ISampleDefinition Value8 = new DefaultSampleDefinition("Value8", Category, SampleUnit.Byte);
        public static readonly ISampleDefinition Value9 = new DefaultSampleDefinition("Value9", Category, SampleUnit.Byte);
        public static readonly ISampleDefinition Value10 = new DefaultSampleDefinition("Value10", Category, SampleUnit.Byte);
        public static readonly ISampleDefinition Value11 = new DefaultSampleDefinition("Value11", Category, SampleUnit.Byte);
        public static readonly ISampleDefinition Value12 = new DefaultSampleDefinition("Value12", Category, SampleUnit.Byte);
    }

    internal static class GraphTestUtil
    {
        public static void AddAllSamples(SampleGroups sampleGroups, Random random)
        {
            sampleGroups.AddSample(GraphTestDefinitions.Value1, random.Next(0, 100));
            sampleGroups.AddSample(GraphTestDefinitions.Value2, random.Next(0, 100));
            sampleGroups.AddSample(GraphTestDefinitions.Value3, random.Next(0, 100));
            sampleGroups.AddSample(GraphTestDefinitions.Value4, random.Next(0, 100));
            sampleGroups.AddSample(GraphTestDefinitions.Value5, random.Next(0, 100));
            sampleGroups.AddSample(GraphTestDefinitions.Value6, random.Next(0, 100));
            sampleGroups.AddSample(GraphTestDefinitions.Value7, random.Next(0, 100));
            sampleGroups.AddSample(GraphTestDefinitions.Value8, random.Next(0, 100));
            sampleGroups.AddSample(GraphTestDefinitions.Value9, random.Next(0, 100));
            sampleGroups.AddSample(GraphTestDefinitions.Value10, random.Next(0, 100));
            sampleGroups.AddSample(GraphTestDefinitions.Value11, random.Next(0, 100));
            sampleGroups.AddSample(GraphTestDefinitions.Value12, random.Next(0, 100));
        }
    }

    public class GraphTest
    {
        [Test, Performance]
        public void ManySamples()
        {
            var random = new Random(Seed: 1);
            using var sampleGroups = new SampleGroups();
            GraphTestUtil.AddAllSamples(sampleGroups, random);
        }

        [Test, Performance]
        public void ManyTargets()
        {
            var random = new Random(Seed: 1);
            using var sampleGroups = new SampleGroups();
            foreach (string sampleTarget in new[] { "Target1", "Target2", "Target3", "Target4", "Target5", "Target6" })
            {
                sampleGroups.AddSample(GraphTestDefinitions.Value1, random.Next(10), sampleTarget);
                sampleGroups.AddSample(GraphTestDefinitions.Value2, random.Next(10), sampleTarget);
                sampleGroups.AddSample(GraphTestDefinitions.Value3, random.Next(10), sampleTarget);
                sampleGroups.AddSample(GraphTestDefinitions.Value4, random.Next(10), sampleTarget);
            }
        }

        [Test, Performance]
        public void ManyTargetsWithParameter([Values(10, 20, 30, 40)] int param)
        {
            var random = new Random(Seed: param);
            using var sampleGroups = new SampleGroups();
            foreach (string sampleTarget in new[] { "Target1", "Target2", "Target3", "Target4", "Target5", "Target6" })
            {
                sampleGroups.AddSample(GraphTestDefinitions.Value1, random.Next(param), sampleTarget);
                sampleGroups.AddSample(GraphTestDefinitions.Value2, random.Next(param), sampleTarget);
                sampleGroups.AddSample(GraphTestDefinitions.Value3, random.Next(param), sampleTarget);
                sampleGroups.AddSample(GraphTestDefinitions.Value4, random.Next(param), sampleTarget);
            }
        }
    }

    [ComparableTest]
    public class GraphComparableTest
    {
        [Test, Performance]
        public void CompareTest1()
        {
            var random = new Random(Seed: 1);
            using var sampleGroups = new SampleGroups();
            GraphTestUtil.AddAllSamples(sampleGroups, random);
        }

        [Test, Performance]
        public void CompareTest2()
        {
            var random = new Random(Seed: 2);
            using var sampleGroups = new SampleGroups();
            GraphTestUtil.AddAllSamples(sampleGroups, random);
        }
    }
}