using System;
using NUnit.Framework;
using PerformanceTestReportViewer.Definition;
using PerformanceTestReportViewer.Editor;
using PerformanceTestReportViewer.Samples_.EditModeTests;
using Unity.PerformanceTesting;

namespace PerformanceTestReportViewer.Samples_.Examples.EditModeTests
{
    [ComparableTest]
    public class GroupedTest
    {
        [Test, Performance]
        public void Test1()
        {
            using var sampleGroups = new SampleGroups();
            using (sampleGroups.CreateScope(SimpleTestDefinitions.Value1))
                Console.WriteLine("Do something");
        }

        [Test, Performance]
        public void Test2()
        {
            using var sampleGroups = new SampleGroups();
            using (sampleGroups.CreateScope(SimpleTestDefinitions.Value1))
                Console.WriteLine("Do something");
        }

        [Test, Performance]
        public void Test3()
        {
            using var sampleGroups = new SampleGroups();
            using (sampleGroups.CreateScope(SimpleTestDefinitions.Value1))
                Console.WriteLine("Do something");
        }
    }

    public class ParameterizedTest
    {
        [Test, Performance]
        public void Test([Values(1, 2, 3, 4, 5)]int parameter)
        {
            using var sampleGroups = new SampleGroups();
            using (sampleGroups.CreateScope(SimpleTestDefinitions.Value1))
                Console.WriteLine("Do something");
        }
    }
}