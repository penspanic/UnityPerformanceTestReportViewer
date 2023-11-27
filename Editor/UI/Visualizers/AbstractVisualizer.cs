using System;
using System.Linq;
using Unity.PerformanceTesting.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace PerformanceTestReportViewer.UI.Visualizers
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
        }

        public abstract bool MustShown();
        public abstract void Refresh();
        protected abstract void OnRenderAreaGenerateVisualContent(MeshGenerationContext ctx);

        public static Color GetDataColor(int index)
        {
            return (index % 7) switch
            {
                0 => Color.blue,
                1 => Color.red,
                2 => Color.green,
                3 => Color.yellow,
                4 => Color.gray,
                5 => Color.magenta,
                6 => Color.black
            };
        }

        protected void SetHeaderText()
        {
            var names = Item.TestResults.Select(t => t.Name).ToArray();
            if (GroupedItem != null)
            {
                header.text = $"{GroupedItem.GroupedResult.TestGroupName}<br>[{string.Join(", ", GroupedItem.GroupedResult.Children.Select(c => c.Parameter))}]";
            }
            else
            {
                if (Utility.TryExtractCommonStrings(names, out string commonString, out string[] variations))
                {
                    commonString = commonString.TrimEnd('.');
                    header.text = $"{commonString}<br>[{string.Join(", ", variations)}]";
                }
                else
                {
                    header.text = string.Join(", ", names);
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