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

        public class LogicalTreeViewItem : ITreeViewItem
        {
            public string Name;
            public List<ITreeViewItem> Children = new();
            public PerformanceTestResult[] GetTestResults() => Children.SelectMany(c => c.GetTestResults()).ToArray();
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
            public string Namespace;
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
            resultsTreeView.ExpandRootItems();
        }

        private VisualElement MakeItem()
        {
            return new Label();
        }

        private void BindItem(VisualElement element, int index)
        {
            var label = element as Label;
            var item = resultsTreeView.GetItemDataForIndex<ITreeViewItem>(index);
            if (item is LogicalTreeViewItem logicalTreeViewItem)
            {
                label.text = logicalTreeViewItem.Name;
            }
            else if (item is GroupedResultItem groupedResultItem)
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
            Dictionary<string, LogicalTreeViewItem> logicalTreeItems = ViewerModule.Instance.PerformanceTestResults.Results.Select(r =>
            {
                string @namespace = r.ClassName.Substring(0, r.ClassName.LastIndexOf('.'));
                return @namespace;
            }).Distinct().ToDictionary(n => n, n => new LogicalTreeViewItem() { Name = n });

            foreach (PerformanceTestResult testResult in ViewerModule.Instance.PerformanceTestResults.Results)
            {
                string @namespace = testResult.ClassName.Substring(0, testResult.ClassName.LastIndexOf('.'));
                string testNameWithoutNamespace = testResult.Name.Substring(@namespace.Length + 1);
                string classNameWithoutNamespace = testResult.ClassName.Substring(@namespace.Length + 1);
                LogicalTreeViewItem parent = logicalTreeItems[@namespace];
                bool isParameterizedTest = PerformanceTestReportViewerUtility.IsParameterizedTest(testResult.Name, out string groupedDefinitionName, out string parameter);
                if (isParameterizedTest || TestInformationGetter.IsComparableTest(testResult.ClassName))
                {
                    if (string.IsNullOrEmpty(groupedDefinitionName))
                        groupedDefinitionName = classNameWithoutNamespace;
                    else
                        groupedDefinitionName = groupedDefinitionName.Substring(@namespace.Length + 1);
                    if (string.IsNullOrEmpty(parameter))
                        parameter = testResult.MethodName;
                    if (groupedResultItems.TryGetValue(groupedDefinitionName, out GroupedResultItem groupedItem) == false)
                    {
                        groupedItem = new GroupedResultItem() { Namespace = @namespace, TestGroupName = groupedDefinitionName };
                        groupedResultItems[groupedDefinitionName] = groupedItem;
                        parent.Children.Add(groupedItem);
                    }

                    var item = new GroupedParameterizedResultItem()
                    {
                        TestName = testNameWithoutNamespace, TestResult = testResult, Parameter = parameter
                    };
                    groupedItem.Children.Add(item);
                    continue;
                }

                ITreeViewItem testResultItem = new SingleResultItem() { TestName = testNameWithoutNamespace, TestResult = testResult };
                parent.Children.Add(testResultItem);
            }

            foreach (LogicalTreeViewItem treeViewItem in logicalTreeItems.Values)
            {
                int parentId = ++id;
                var childList = new List<TreeViewItemData<ITreeViewItem>>();
                foreach (ITreeViewItem child in treeViewItem.Children)
                {
                    if (child is GroupedResultItem groupedResultItem)
                    {
                        var grandChildList = new List<TreeViewItemData<ITreeViewItem>>();
                        foreach (GroupedParameterizedResultItem grandChild in groupedResultItem.Children)
                        {
                            grandChildList.Add(new TreeViewItemData<ITreeViewItem>(++id, grandChild));
                        }
                        childList.Add(new TreeViewItemData<ITreeViewItem>(++id, child, grandChildList));
                    }
                    else
                    {
                        childList.Add(new TreeViewItemData<ITreeViewItem>(++id, child));
                    }
                }
                resultItemDataList.Add(new TreeViewItemData<ITreeViewItem>(parentId, treeViewItem, childList));
            }

            return resultItemDataList;
        }
    }
}