using System;
using System.Collections.Generic;
using System.Linq;
using PerformanceTestReportViewer.Definition;
using Unity.PerformanceTesting.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PerformanceTestReportViewer.Editor.UI
{
    public class TestResultListView : VisualElement
    {

        public interface ITreeViewItem
        {
            PerformanceTestResult[] GetTestResults();
        }

        public class SingleResultItem : ITreeViewItem
        {
            public string TestName;
            public PerformanceTestResult TestResult;
            public PerformanceTestResult[] GetTestResults() => new[] { TestResult };
        }

        // 1, 2, 10, 100,...
        public class GroupedResultItem : ITreeViewItem
        {
            public string TestGroupName;
            public List<GroupedParameterizedResultItem> Children = new();
            public PerformanceTestResult[] GetTestResults() => Children.Select(c => c.TestResult).ToArray();
        }

        public class GroupedParameterizedResultItem : SingleResultItem
        {
            public string Parameter;
        }

        private new class UxmlTraits : VisualElement.UxmlTraits
        {
        }

        private new class UxmlFactory : UxmlFactory<TestResultListView, UxmlTraits>
        {
        }

        public event Action<ITreeViewItem> OnTreeViewItemSelected;

        private static readonly string layoutPath = $"{Constants.LayoutPath}/{nameof(TestResultListView)}.uxml";

        private TreeView resultsTreeView;

        private ViewerOptions viewerOptions;

        public TestResultListView()
        {
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(layoutPath).CloneTree(this);
            resultsTreeView = this.Q<TreeView>(nameof(resultsTreeView));
        }

        public void Init(ViewerOptions viewerOptions)
        {
            this.viewerOptions = viewerOptions;

            resultsTreeView.Clear();

            if (ViewerModule.Instance.PerformanceTestResults == null)
                return;

            resultsTreeView.SetRootItems(BuildTreeDataList());
            resultsTreeView.makeItem = MakeItem;
            resultsTreeView.bindItem = BindItem;

            resultsTreeView.selectionChanged += (selectedList) =>
            {
                if (selectedList.Count() > 1)
                    return;

                var item = selectedList.Single() as ITreeViewItem;
                OnTreeViewItemSelected?.Invoke(item);
            };
        }

        private VisualElement MakeItem()
        {
            return new Label();
        }

        private void BindItem(VisualElement element, int index)
        {
            var label = element as Label;
            var item = resultsTreeView.GetItemDataForIndex<ITreeViewItem>(index);
            if (item is GroupedResultItem groupedResultItem)
            {
                label.text = $"Group|{groupedResultItem.TestGroupName} {groupedResultItem.Children.Count} Tests";
            }
            else if (item is GroupedParameterizedResultItem groupedParameterizedResultItem)
            {
                label.text = groupedParameterizedResultItem.TestName;
            }
            else if (item is SingleResultItem singleResultItem)
            {
                label.text = singleResultItem.TestName;
            }
            else
            {
                throw new Exception($"Unhandled type : {item.GetType()}");
            }
        }
        
        private IList<TreeViewItemData<ITreeViewItem>> BuildTreeDataList()
        {
            int id = 0;

            List<TreeViewItemData<ITreeViewItem>> resultItemDataList = new();
            Dictionary<string, GroupedResultItem> groupedResultItems = new();
            foreach (PerformanceTestResult testResult in ViewerModule.Instance.PerformanceTestResults.Results)
            {
                bool isParameterizedTest = PerformanceTestReportViewerUtility.IsParameterizedTest(testResult.Name, out string groupedDefinitionName, out string parameter);
                if (isParameterizedTest || TestInformationGetter.IsComparableTest(testResult.ClassName))
                {
                    if (string.IsNullOrEmpty(groupedDefinitionName))
                        groupedDefinitionName = testResult.ClassName;
                    if (string.IsNullOrEmpty(parameter))
                        parameter = testResult.MethodName;
                    if (groupedResultItems.TryGetValue(groupedDefinitionName, out GroupedResultItem groupedItem) == false)
                    {
                        groupedItem = new GroupedResultItem() { TestGroupName = groupedDefinitionName };
                        groupedResultItems[groupedDefinitionName] = groupedItem;
                    }

                    var item = new GroupedParameterizedResultItem()
                    {
                        TestName = testResult.Name, TestResult = testResult, Parameter = parameter
                    };
                    groupedItem.Children.Add(item);
                    continue;
                }

                ITreeViewItem testResultItem = new SingleResultItem() { TestName = testResult.Name, TestResult = testResult };
                var treeItemData = new TreeViewItemData<ITreeViewItem>(++id, testResultItem);
                resultItemDataList.Add(treeItemData);
            }

            foreach (GroupedResultItem groupedResultItem in groupedResultItems.Values)
            {
                var childrenItemData = new List<TreeViewItemData<ITreeViewItem>>();
                foreach (GroupedParameterizedResultItem parameterizedResultItem in groupedResultItem.Children)
                {
                    childrenItemData.Add(new TreeViewItemData<ITreeViewItem>(++id, parameterizedResultItem));
                }
                
                var treeViewItemData = new TreeViewItemData<ITreeViewItem>(++id, groupedResultItem, childrenItemData);
                resultItemDataList.Add(treeViewItemData);
            }

            return resultItemDataList;
        }
    }
}