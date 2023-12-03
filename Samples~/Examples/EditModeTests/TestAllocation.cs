using System;
using NUnit.Framework;
using PerformanceTestReportViewer.Definition;
using PerformanceTestReportViewer.Editor;
using Unity.PerformanceTesting;
// ReSharper disable AccessToDisposedClosure

namespace PerformanceTestReportViewer.Samples_.EditModeTests
{
    [SampleDefinitionContainer]
    public static class TestAllocationDefinitions
    {
        public const string Category = "TestAllocation";

        public static readonly ISampleDefinition Value1 = new DefaultSampleDefinition("Value1", Category, SampleUnit.Millisecond);
        public static readonly ISampleDefinition Value2 = new DefaultSampleDefinition("Value2", Category, SampleUnit.Millisecond);
        public static readonly ISampleDefinition Value3 = new DefaultSampleDefinition("Value3", Category, SampleUnit.Millisecond);
    }

    public class SampleClass
    {
        public int intValue1;
        public int intValue2;
        public int intValue3;
        public int intValue4;
        public int intValue5;
    }

    public struct SampleStruct
    {
        public int intValue1;
        public int intValue2;
        public int intValue3;
        public int intValue4;
        public int intValue5;
    }

    [ComparableTest]
    public class TestAllocation
    {
        public const int warmupCount = 1000;
        public const int loopCount = 100000;

        [Test, Performance]
        public void ClassNew()
        {
            using var sampleGroups = new SampleGroups();
            Measure.Method(() =>
            {
                var instance = new SampleClass();
                //sampleGroups.RecordMemory();
                sampleGroups.AddSample(TestAllocationDefinitions.Value1, 1);
                sampleGroups.AddSample(TestAllocationDefinitions.Value2, 1);
                sampleGroups.AddSample(TestAllocationDefinitions.Value3, 1);
                
            }).WarmupCount(warmupCount).MeasurementCount(loopCount).Run();
        }

        [Test, Performance]
        public void StructNew()
        {
            using var sampleGroups = new SampleGroups(); 
            Measure.Method(() =>    
            {
                var instance = new SampleStruct();
                //sampleGroups.RecordMemory();
                sampleGroups.AddSample(TestAllocationDefinitions.Value1, 2);
                sampleGroups.AddSample(TestAllocationDefinitions.Value2, 2);
                sampleGroups.AddSample(TestAllocationDefinitions.Value3, 2);
            }).WarmupCount(warmupCount).MeasurementCount(loopCount).Run();
        }

        [Test, Performance]
        public void StructStackalloc()
        {
            using var sampleGroups = new SampleGroups(); 
            Measure.Method(() =>    
            {
                Span<SampleStruct> instance = stackalloc SampleStruct[1];
                //sampleGroups.RecordMemory();
                sampleGroups.AddSample(TestAllocationDefinitions.Value1, 3);
                sampleGroups.AddSample(TestAllocationDefinitions.Value2, 3);
                sampleGroups.AddSample(TestAllocationDefinitions.Value3, 3);
            }).WarmupCount(warmupCount).MeasurementCount(loopCount).Run();
        }
    }

    public class SampleTargetTest
    {
        public const int warmupCount = 1000;
        public const int loopCount = 100000;

        [Test, Performance]
        public void ClassNew()
        {
            using var sampleGroups = new SampleGroups();
            Measure.Method(() =>
            {
                var instance = new SampleClass();
                sampleGroups.RecordMemory("client");
                sampleGroups.RecordMemory("server");
            }).WarmupCount(warmupCount).MeasurementCount(loopCount).Run();
        }

        [Test, Performance]
        public void StructNew()
        {
            using var sampleGroups = new SampleGroups(); 
            Measure.Method(() =>    
            {
                var instance = new SampleStruct();
                sampleGroups.RecordMemory("client");
                sampleGroups.RecordMemory("server");
            }).WarmupCount(warmupCount).MeasurementCount(loopCount).Run();
        }

        [Test, Performance]
        public void StructStackalloc()
        {
            using var sampleGroups = new SampleGroups(); 
            Measure.Method(() =>
            {
                Span<SampleStruct> instance = stackalloc SampleStruct[1];
                sampleGroups.RecordMemory("client");
                sampleGroups.RecordMemory("server");
            }).WarmupCount(warmupCount).MeasurementCount(loopCount).Run();
        }
    }
}