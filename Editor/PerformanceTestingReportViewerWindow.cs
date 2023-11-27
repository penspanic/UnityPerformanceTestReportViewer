using System;
using System.IO;
using PerformanceTestReportViewer.Definition;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PerformanceTestReportViewer
{
    public class PerformanceTestingReportViewerWindow : EditorWindow
    {
        private UI.PerformanceTestReportViewer viewer;
        private ViewerOptions viewerOptions;

        [MenuItem("Window/Analysis/PerformanceTestingReportViewer %#&V")]
        public static void ShowExample()
        {
            PerformanceTestingReportViewerWindow wnd = GetWindow<PerformanceTestingReportViewerWindow>();
            wnd.titleContent = new GUIContent("PerformanceTestingReportViewerWindow");
        }

        protected virtual string EditorConfigPath => Path.Combine(Application.persistentDataPath, "PerformanceTestingReportViewerConfig.json");

        public void CreateGUI()
        {
            string path = Constants.LayoutPath + "/PerformanceTestingReportViewerWindow.uxml";
            var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
            asset.CloneTree(rootVisualElement);
            viewer = rootVisualElement.Q<UI.PerformanceTestReportViewer>();

            if (File.Exists(EditorConfigPath))
                viewerOptions = ViewerOptions.DeserializeFromString(File.ReadAllText(EditorConfigPath));
            else
                viewerOptions = new();

            ViewerModule.Instance.Load();

            ViewerModule.Instance.ViewerOptions = viewerOptions;
            
            viewer.Init(viewerOptions);
        }

        private void Update()
        {
            ViewerModule.Instance.ConsumeActions();
        }

        private void OnDestroy()
        {
            string json = viewerOptions.SerializeAsString();
            File.WriteAllText(EditorConfigPath, json);
        }
    }
}