using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PerformanceTestReportViewer.Definition;
using PerformanceTestReportViewer.SampleDefinitions;
using Unity.PerformanceTesting;
using UnityEngine.Profiling;

namespace PerformanceTestReportViewer.Editor
{
    public struct SamplingScope : IDisposable
    {
        private readonly SampleGroups target;
        private readonly ISampleDefinition sampleDefinition;
        private readonly string sampleTarget;
        private long startTime;
        internal SamplingScope(SampleGroups target, ISampleDefinition sampleDefinition, string sampleTarget)
        {
            this.target = target;
            this.sampleDefinition = sampleDefinition;
            this.sampleTarget = sampleTarget;
            startTime = DateTime.UtcNow.Ticks;
        }

        public void Dispose()
        {
            if (target == null)
                return;

            long endTime = DateTime.UtcNow.Ticks;
            target.AddSample(sampleDefinition, (endTime - startTime) / 10000f, sampleTarget);
        }
    }

    public class SampleGroups : IDisposable
    {
        public SampleGroups(params (string categoryName, ISampleDefinition, string sampleTarget)[] sampleTypes)
        {
            foreach ((string categoryName, ISampleDefinition sampleGroupDefinition, string sampleTarget) in sampleTypes)
            {
                var sampleTarget2 = string.IsNullOrEmpty(sampleTarget) ? string.Empty : sampleTarget;
                sampleGroups[(sampleGroupDefinition, sampleTarget2)] = CreateSampleGroup(categoryName, sampleGroupDefinition, sampleTarget2);
            }
        }

        public static SampleGroup CreateSampleGroup(
            string categoryName,
            ISampleDefinition sampleDefinition,
            string sampleTarget)
        {
            return new SampleGroup(
                PerformanceTestReportViewerUtility.FormatSampleGroupName(sampleTarget, categoryName, sampleDefinition.Name),
                sampleDefinition.SampleUnit,
                increaseIsBetter: sampleDefinition.IncreaseIsBetter.GetValueOrDefault(false)
            );
        }

        private Dictionary<(ISampleDefinition, string), SampleGroup> sampleGroups = new();

        private static void TryAddSampleGroup(SampleGroup group)
        {
            if (group == null || group.Samples.Count == 0)
                return;

            PerformanceTest.AddSampleGroup(group);
        }

        public void RemoveSampleGroup(ISampleDefinition sampleDefinition, string sampleTarget = null)
        {
            sampleTarget = string.IsNullOrEmpty(sampleTarget) ? string.Empty : sampleTarget;
            sampleGroups.Remove((sampleDefinition, sampleTarget));
        }

        public void Dispose()
        {
            foreach ((ISampleDefinition sampleGroupDefinition, string sampleTarget) in sampleGroups.Keys)
            {
                if (sampleGroups.TryGetValue((sampleGroupDefinition, sampleTarget), out var serverSampleGroup))
                    TryAddSampleGroup(serverSampleGroup);
            }

            PerformanceTest.Active.CalculateStatisticalValues();
        }

        public void AddSample(ISampleDefinition sampleDefinition, double value, string sampleTarget = null)
        {
            sampleTarget = string.IsNullOrEmpty(sampleTarget) ? string.Empty : sampleTarget;
            if (sampleGroups.TryGetValue((sampleDefinition, sampleTarget), out SampleGroup group) == false)
            {
                group = CreateSampleGroup(sampleDefinition.Category, sampleDefinition, sampleTarget);
                sampleGroups.Add((sampleDefinition, sampleTarget), group);
            }

            group.Samples.Add(value);
        }

        public void RecordMemory(string sampleTarget = null)
        {
            AddSample(SystemSampleDefinitions.TotalAllocatedMemorySize,
                Profiler.GetTotalAllocatedMemoryLong() / (float)(1024 * 1024), sampleTarget);
            AddSample(SystemSampleDefinitions.MonoUsedMemorySize,
                Profiler.GetMonoUsedSizeLong() / (float)(1024 * 1024), sampleTarget);
        }

        public SamplingScope CreateScope(ISampleDefinition sampleDefinition, string sampleTarget = null)
        {
            sampleTarget = string.IsNullOrEmpty(sampleTarget) ? string.Empty : sampleTarget;
            return new SamplingScope(this, sampleDefinition, sampleTarget);
        }
    }
}