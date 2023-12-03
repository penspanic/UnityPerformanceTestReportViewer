using System;
using System.Collections.Generic;
using NUnit.Framework;
using PerformanceTestReportViewer.Definition;
using PerformanceTestReportViewer.Editor;
using Unity.PerformanceTesting;

namespace PerformanceTestReportViewer.Samples_.EditModeTests
{
    [SampleDefinitionContainer]
    public static class TestCollectionDefinitions
    {
        public const string Category = "TestCollection";

        public static readonly ISampleDefinition Add = new DefaultSampleDefinition("Add", Category, SampleUnit.Millisecond);
        public static readonly ISampleDefinition Remove = new DefaultSampleDefinition("Remove", Category, SampleUnit.Millisecond);
        public static readonly ISampleDefinition Contains = new DefaultSampleDefinition("Contains", Category, SampleUnit.Millisecond);
    }

    [ComparableTest]
    public class TestCollection
    {
        private static void Test<T>(int elementCount, Func<T> getElement, Action<T> add, Action<T> remove, Action<T> contains)
        {
            using var sampleGroups = new SampleGroups();
            List<T> added = new List<T>();
            using (sampleGroups.CreateScope(TestCollectionDefinitions.Add))
            {
                for (int i = 0; i < elementCount; ++i)
                {
                    var element = getElement();
                    added.Add(element);
                    add(element);
                }
            }

            var random = new Random(1);
            using (sampleGroups.CreateScope(TestCollectionDefinitions.Contains))
            {
                for (int i = 0; i < elementCount; ++i)
                {
                    int randomIndex = random.Next(added.Count);
                    contains(added[randomIndex]);
                }
            }

            using (sampleGroups.CreateScope(TestCollectionDefinitions.Remove))
            {
                for (int i = 0; i < elementCount; ++i)
                {
                    int randomIndex = random.Next(added.Count);
                    remove(added[randomIndex]);
                }
            }
        }


        private const int elementCount = 5000;
        private const int warmupCount = 10;
        private const int measurementCount = 100;
        [Test, Performance]
        public void List()
        {
            Measure.Method(() =>
            {
                int num = 0;
                List<int> collection = new();
                Test(elementCount, () => ++num,
                    value => collection.Add(value),
                    value => collection.Remove(value),
                    value => collection.Contains(value));
            }).WarmupCount(warmupCount).MeasurementCount(measurementCount).Run();
        }

        [Test, Performance]
        public void LinkedList()
        {
            Measure.Method(() =>
            {
                int num = 0;
                LinkedList<int> collection = new();
                Test(elementCount, () => ++num,
                    value => collection.AddLast(value),
                    value => collection.Remove(value),
                    value => collection.Contains(value));
            }).WarmupCount(warmupCount).MeasurementCount(measurementCount).Run();
        }

        [Test, Performance]
        public void Dictionary()
        {
            Measure.Method(() =>
            {
                int num = 0;
                Dictionary<int, int> collection = new();
                Test(elementCount, () => ++num,
                    value => collection.Add(value, value),
                    value => collection.Remove(value),
                    value => collection.ContainsKey(value));
            }).WarmupCount(warmupCount).MeasurementCount(measurementCount).Run();
        }

        [Test, Performance]
        public void HashSet()
        {
            Measure.Method(() =>
            {
                int num = 0;
                HashSet<int> collection = new();
                Test(elementCount, () => ++num,
                    value => collection.Add(value),
                    value => collection.Remove(value),
                    value => collection.Contains(value));
            }).WarmupCount(warmupCount).MeasurementCount(measurementCount).Run();
        }
    }
}