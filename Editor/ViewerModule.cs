using System;
using System.Collections.Concurrent;
using System.IO;
using PerformanceTestReportViewer.Definition;
using Unity.PerformanceTesting.Data;
using UnityEngine;

namespace PerformanceTestReportViewer
{
    public class ViewerModule
    {
        public static readonly ViewerModule Instance = new();
        public Run PerformanceTestResults { get; private set; }
        public PerformanceTestResultContext ResultContext { get; private set; }
        public ViewerOptions ViewerOptions { get; set; }

        private ConcurrentQueue<Action> unityThreadJobs = new();

        private static readonly string performanceTestResultsPath = Path.Combine(Application.persistentDataPath, "PerformanceTestResults.json");

        public void Load()
        {
            TestInformationGetter.ClearCache();
            PerformanceTestResults = null;
            if (File.Exists(performanceTestResultsPath) == false)
                return;

            string json = File.ReadAllText(performanceTestResultsPath);
            PerformanceTestResults = JsonUtility.FromJson<Run>(json);
            ResultContext = new PerformanceTestResultContext(PerformanceTestResults.Results.ToArray());
        }

        public void QueueAction(Action action)
        {
            unityThreadJobs.Enqueue(action);
        }

        public void ConsumeActions()
        {
            while (unityThreadJobs.TryDequeue(out var action))
            {
                action?.Invoke();
            }
        }
    }
}