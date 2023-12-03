using System;
using System.Linq;
using System.Text;
using Unity.PerformanceTesting.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace PerformanceTestReportViewer.Editor.UI.Visualizers
{
    public abstract class ItemData
    {
        public PerformanceTestResult[] TestResults;
        public SampleElement[] Values;

        public bool MustShown(TestResultOptions testResultOptions)
        {
            return Values.Any(e =>
                testResultOptions.MustShown(e.SampleDefinition) && e.MustShown(ViewerModule.Instance.ViewerOptions, testResultOptions));
        }
    }

    public class SingleItemData : ItemData
    {
    }

    public class GroupedItemData : ItemData
    {
        public TestResultListView.GroupedResultItem GroupedResult;
    }

    public abstract class AbstractVisualizer : VisualElement
    {
        public TestResultOptions TestResultOptions
        {
            get => _testResultOptions;
            set
            {
                _testResultOptions = value;
                Refresh();
            }
        }
        private TestResultOptions _testResultOptions;

        

        public ItemData Item
        {
            get => _item;
            set
            {
                _item = value;
                Refresh();
            }
        }
        private ItemData _item;

        public GroupedItemData GroupedItem => Item as GroupedItemData;

        protected Label header;
        protected VisualElement renderArea;
        protected VisualElement descriptionArea;
        protected VisualElement xAxis;
        protected VisualElement yAxis;
        protected VisualElement legends;
        protected Label valueTooltip;

        protected AbstractVisualizer()
        {
        }

        protected void InitCommonVisualElements()
        {
            header = this.Q<Label>(nameof(header));
            renderArea = this.Q<VisualElement>(nameof(renderArea));
            renderArea.generateVisualContent += OnRenderAreaGenerateVisualContent;

            descriptionArea = this.Q<VisualElement>(nameof(descriptionArea));
            xAxis = this.Q<VisualElement>(nameof(xAxis));
            yAxis = this.Q<VisualElement>(nameof(yAxis));
            legends = this.Q<VisualElement>(nameof(legends));
            valueTooltip = this.Q<Label>(nameof(valueTooltip));
        }

        public abstract bool MustShown();
        public abstract void Refresh();
        protected abstract void OnRenderAreaGenerateVisualContent(MeshGenerationContext ctx);

        public static Color GetDataColor(int index)
        {
            return (index % 7) switch
            {
                0 => new Color32(143, 0, 49, 255),
                1 => new Color32(247, 155, 25, 255),
                2 => new Color32(2, 143, 163, 255),
                3 => new Color32(145, 25, 142, 255),
                4 => new Color32(0, 135, 90, 255),
                5 => new Color32(194, 112, 192, 255),
                6 => new Color32(0, 168, 204, 255),
            };
        }

        protected void SetHeaderText()
        {
            var names = Item.TestResults.Select(t => t.Name).ToArray();

            void Do(string @namespace, string testName, string[] parameters)
            {
                var sb = new StringBuilder();
                if (string.IsNullOrEmpty(@namespace) == false)
                {
                    sb.Append(@namespace);
                    sb.Append("<br>");
                }

                if (string.IsNullOrEmpty(testName) == false)
                {
                    sb.Append(testName);
                }
                if (parameters != null && parameters.Length > 0 && parameters.Any(p => string.IsNullOrEmpty(p) == false))
                {
                    sb.Append("<br>");
                    sb.Append($"[{string.Join(", ", parameters)}]");
                }

                header.text = sb.ToString();
            }

            if (GroupedItem != null)
            {
                Do(GroupedItem.GroupedResult.Namespace, GroupedItem.GroupedResult.TestGroupName, GroupedItem.GroupedResult.Children.Select(c => c.Parameter).ToArray());
            }
            else
            {
                if (PerformanceTestReportViewerUtility.TryExtractCommonStrings(names, out string commonString, out string[] variations))
                {
                    commonString = commonString.TrimEnd('.');
                    string @namespace = commonString.Substring(0, commonString.LastIndexOf('.'));
                    string testName = commonString.Substring(@namespace.Length + 1);
                    Do(@namespace, testName, variations);
                }
                else if (names.Length == 1)
                {
                    Do(null, names[0], null);
                }
            }
        }

        protected virtual Rect GetChartRect()
        {
            var rect = renderArea.contentRect;
            if (float.IsNaN(rect.x) || float.IsNaN(rect.y))
                throw new Exception($"Must not access renderArea rect now!");

            int safeAmount = 10;
            rect.xMin += safeAmount;
            rect.xMax -= safeAmount;
            rect.yMin += safeAmount;
            rect.yMax -= safeAmount;
            return rect;
        }
    }
}