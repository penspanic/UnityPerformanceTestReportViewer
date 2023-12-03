using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Entities.PerformanceTestReportViewer.Extensions;
using Unity.PerformanceTesting;

namespace PerformanceTestReportViewer.Samples_.PlayModeTests.Entities
{
    public class WorldSampler : IDisposable
    {
        private Dictionary<string, Dictionary<string, SampleGroup>> frameDatasByContext = new();
        public void RecordFrameData(int tick, in Span<ExposedSystemFrameData> frameDatas, string sampleTarget = null)
        {
            sampleTarget ??= string.Empty;
            if (frameDatasByContext.TryGetValue(sampleTarget, out var frameDataDictionary) == false)
            {
                frameDataDictionary = new();
                frameDatasByContext[sampleTarget] = frameDataDictionary;
            }

            foreach (ExposedSystemFrameData frameData in frameDatas)
            {
                if (frameDataDictionary.TryGetValue(frameData.SystemName, out var frameDataSampleGroup) == false)
                {
                    string sampleGroupName = PerformanceTestReportViewerUtility.FormatSampleGroupName(
                        sampleTarget,
                        ECSWorldSampleGroupFactory.CategoryName,
                        frameData.SystemName
                    );
                    frameDataSampleGroup = new SampleGroup(sampleGroupName, SampleUnit.Millisecond);
                    frameDataDictionary[frameData.SystemName] = frameDataSampleGroup;
                }

                frameDataSampleGroup.Samples.Add(frameData.LastFrameRuntimeMilliseconds);
            }
        }

        public void Dispose()
        {
            foreach (var group in frameDatasByContext.Values.SelectMany(v => v.Values))
            {
                if (group.Samples.Count == 0)
                    continue;
                double avg = group.Samples.Sum() / group.Samples.Count;

                group.Samples.Clear();
                group.Samples.Add(avg);
                PerformanceTest.Active.SampleGroups.Add(group);
            }

            PerformanceTest.Active.CalculateStatisticalValues();
        }
    }
}