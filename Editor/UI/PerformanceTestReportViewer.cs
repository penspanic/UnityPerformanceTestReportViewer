using System;
using System.Collections.Generic;
using System.Linq;
using PerformanceTestReportViewer.Definition;
using PerformanceTestReportViewer.Editor.UI.Visualizers;
using Unity.PerformanceTesting.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using PopupWindow = UnityEditor.PopupWindow;

namespace PerformanceTestReportViewer.Editor.UI
{
    public class PerformanceTestReportViewer : VisualElement
    {
        private new class UxmlTraits : VisualElement.UxmlTraits { }
        private new class UxmlFactory : UxmlFactory<PerformanceTestReportViewer, UxmlTraits> { }

        private static readonly string layoutPath = $"{Constants.LayoutPath}/{nameof(PerformanceTestReportViewer)}.uxml";

        public event Action RequestRefresh;
        
        private ScrollView sampleTypesScrollView;
        private ScrollView contextsScrollView;
        private TestResultListView testResultListView;
        private ScrollView viewPanel;
        private DropdownField dataCountDropdown;
        private DropdownField sortMethodDropdown;
        private DropdownField viewerTypeDropdown;
        private ScrollView tagsScrollView;

        private List<AbstractVisualizer> visualizers = new();
        private TestResultListView.ITreeViewItem showingTreeViewItem;
        private ViewerOptions viewerOptions;

        public PerformanceTestReportViewer()
        {
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(layoutPath).CloneTree(this);

            sampleTypesScrollView = this.Q<ScrollView>(nameof(sampleTypesScrollView));
            contextsScrollView = this.Q<ScrollView>(nameof(contextsScrollView));
            testResultListView = this.Q<TestResultListView>(nameof(testResultListView));
            viewPanel = this.Q<ScrollView>(nameof(viewPanel));
            dataCountDropdown = this.Q<DropdownField>(nameof(dataCountDropdown));
            sortMethodDropdown = this.Q<DropdownField>(nameof(sortMethodDropdown));
            viewerTypeDropdown = this.Q<DropdownField>(nameof(viewerTypeDropdown));
            tagsScrollView = this.Q<ScrollView>(nameof(tagsScrollView));

            RequestRefresh += Refresh;
        }

        public void Init(ViewerOptions viewerOptions)
        {
            this.viewerOptions = viewerOptions;

            InitCategoryToggles();
            InitToggles(contextsScrollView, ViewerModule.Instance.ResultContext.GetSampleTargets(),
                viewerOptions.IsSampleTargetOn, viewerOptions.SetIsSampleTargetOn,
                () => RequestRefresh?.Invoke());
            testResultListView.Init(viewerOptions);

            testResultListView.OnTreeViewItemSelected += OnTreeViewItemSelected;
        }

        private void Refresh()
        {
            OnTreeViewItemSelected(showingTreeViewItem);
        }

        private void OnTreeViewItemSelected(TestResultListView.ITreeViewItem treeViewItem)
        {
            showingTreeViewItem = treeViewItem;
            viewPanel.Clear();
            visualizers.Clear();

            InitVisualizerOptions();

            if (treeViewItem is TestResultListView.GroupedResultItem groupedResultItem)
            {
                PerformanceTestResult[] testResults = groupedResultItem.Children.Select(c => c.TestResult).ToArray();
                if (MultipleTestResultComparison.TryCreate(testResults, out var comparison, viewerOptions.IsOn) == false)
                    return;
                TestResultOptions testResultOptions = TestResultOptions.Get(testResults);

                var item = new GroupedItemData() { GroupedResult = groupedResultItem, Values = comparison.Elements, TestResults = testResults };
                if (viewerOptions.ViewerType == ViewerType.Bars_Single)
                {
                    var visualizer = new BarsVisualizer()
                    {
                        Item = item,
                        TestResultOptions = testResultOptions,
                        XAxisName = comparison.Elements.GetSampleUnit(),
                        GroupNames = groupedResultItem.Children.Select(c => c.Parameter).ToArray()
                    };
                    viewPanel.Add(visualizer);
                    visualizers.Add(visualizer);
                }
                else if (viewerOptions.ViewerType == ViewerType.Lines_Grouped)
                {
                    foreach (SampleElement[] elements in comparison.GroupElementsByContext())
                    {
                        if (item.MustShown(testResultOptions) == false)
                            continue;

                        var visualizer = new MultipleTestResultComparisonVisualizer()
                        {
                            Item = item,
                            TestResultOptions = testResultOptions
                        };
                        viewPanel.Add(visualizer);
                        visualizers.Add(visualizer);
                    }
                }
            }
            else if (treeViewItem is TestResultListView.SingleResultItem singleResultItem)
            {
                var testResults = new[] { singleResultItem.TestResult };
                TestResultOptions testResultOptions = TestResultOptions.Get(testResults);

                if (viewerOptions.ViewerType == ViewerType.Bars_Single)
                {
                    if (MultipleTestResultComparison.TryCreate(testResults, out var comparison, viewerOptions.IsOn) == false)
                        return;

                    var visualizer = new BarsVisualizer()
                    {
                        Item = new SingleItemData() { Values = comparison.Elements, TestResults = testResults },
                        TestResultOptions = testResultOptions,
                        XAxisName = comparison.Elements.GetSampleUnit()
                    };
                    viewPanel.Add(visualizer);
                    visualizers.Add(visualizer);
                }
            }
        }

        #region Category, Context Toggle
        private void InitCategoryToggles()
        {
            foreach (VisualElement child in sampleTypesScrollView.contentContainer.Children().ToArray())
            {
                child.RemoveFromHierarchy();
            }

            ThreeStateToggle.StateType GetState(string category)
            {
                if (viewerOptions.IsAnythingOn(category) && viewerOptions.IsEverythingOn(category) == false)
                    return ThreeStateToggle.StateType.MiddleState;
                else if (viewerOptions.IsEverythingOn(category))
                    return ThreeStateToggle.StateType.Checked;
                else
                    return ThreeStateToggle.StateType.Unchecked;
            }

            var resultContext = ViewerModule.Instance.ResultContext;
            foreach (string category in TestInformationGetter.GetAllSampleGroupCategories(resultContext))
            {
                var categoryToggle = new ThreeStateToggle();
                categoryToggle.OnStateChanged += (state) =>
                {
                    if (state == ThreeStateToggle.StateType.Checked)
                        viewerOptions.SetIsOnCategory(resultContext, category, true);
                    else if (state == ThreeStateToggle.StateType.Unchecked)
                        viewerOptions.SetIsOnCategory(resultContext, category, false);

                    RequestRefresh?.Invoke();
                };
                categoryToggle.Text = category;
                categoryToggle.State = GetState(category);
                categoryToggle.SettingButton.clicked += () =>
                {
                    PopupWindow.Show(categoryToggle.SettingButton.worldBound, new CategoryTogglePopup()
                    {
                        OnValueChanged = ()  =>
                        {
                            categoryToggle.State = GetState(category);
                        },
                        ViewerOptions = viewerOptions,
                        Category = category,
                    });
                };
                sampleTypesScrollView.contentContainer.Add(categoryToggle);
            }
        }

        private static void InitToggles(ScrollView parentScrollView, IEnumerable<string> toggleNames,
            Func<string, bool> toggleValueGetter, Action<string, bool> toggleValueSetter, Action onChanged)
        {
            parentScrollView.Clear();
            static Toggle CreateToggle(string label)
            {
                var toggle = new Toggle();
                toggle.label = label;
                toggle.Q<Label>().style.minWidth = 0;
                toggle.style.marginLeft = 5;
                toggle.style.marginRight = 5;
                toggle.value = true;
                return toggle;
            }

            var allToggle = CreateToggle("All");
            parentScrollView.Add(allToggle);

            List<Toggle> toggles = new();
            foreach (string toggleName in toggleNames)
            {
                    var toggle = CreateToggle(toggleName);
                    toggle.value = toggleValueGetter(toggleName);
                     toggle.RegisterValueChangedCallback((evt) =>
                     {
                         toggleValueSetter(toggleName, evt.newValue);
                         bool isEverythingOn = toggles.All(t => t.value);
                         allToggle.SetValueWithoutNotify(isEverythingOn);
                         onChanged?.Invoke();
                     });
                     toggles.Add(toggle);
                     parentScrollView.Add(toggle);
            }

            allToggle.RegisterValueChangedCallback(evt =>
            {
                foreach (Toggle toggle in toggles)
                {
                    toggle.SetValueWithoutNotify(evt.newValue);
                    toggleValueSetter(toggle.label, evt.newValue);
                }

                onChanged?.Invoke();
            });
        }

        private class CategoryTogglePopup : PopupWindowContent
        {
            public Action OnValueChanged { get; set; }
            public ViewerOptions ViewerOptions { get; set; }
            public string Category { get; set; }
            public override void OnGUI(Rect rect) { }

            public override void OnOpen()
            {
                base.OnOpen();
                var popupRoot = new ScrollView()
                {
                    mode = ScrollViewMode.Vertical
                };
                foreach (ISampleDefinition definition in TestInformationGetter.GetDefinitionsInCategory(ViewerModule.Instance.ResultContext, Category))
                {
                    var toggle = new Toggle() { text = definition.Name, value = ViewerOptions.IsOn(Category, definition)};
                    toggle.RegisterValueChangedCallback((evt) =>
                    {
                        ViewerOptions.SetIsOn(Category, definition, evt.newValue);
                        OnValueChanged?.Invoke();
                    });
                    popupRoot.contentContainer.Add(toggle);
                }

                editorWindow.rootVisualElement.Add(popupRoot);
            }
        }
        #endregion

        #region Visualizer Option

        private bool visualizerOptionsCallbackSet = false;
        private void InitVisualizerOptions()
        {
            dataCountDropdown.choices.Clear();
            dataCountDropdown.choices.AddRange(ViewerOptions.ViewDataCountChoices.Select(i => i.ToString()));
            dataCountDropdown.index = Array.IndexOf(ViewerOptions.ViewDataCountChoices, viewerOptions.ViewDataCount);

            sortMethodDropdown.choices.Clear();
            sortMethodDropdown.choices.AddRange(ViewerOptions.SortMethodChoices.Select(c => c.ToString()));
            sortMethodDropdown.index = Array.IndexOf(ViewerOptions.SortMethodChoices, viewerOptions.SortMethod);

            viewerTypeDropdown.choices.Clear();
            viewerTypeDropdown.choices.AddRange(ViewerOptions.ViewerTypeChoices.Select(c => c.ToString()));
            viewerTypeDropdown.index = Array.IndexOf(ViewerOptions.ViewerTypeChoices, viewerOptions.ViewerType);

            
            IEnumerable<string> tags = new PerformanceTestResultContext(showingTreeViewItem?.GetTestResults()).GetTags() ?? Enumerable.Empty<string>();
            TestResultOptions options = TestResultOptions.Get(showingTreeViewItem?.GetTestResults());
            InitToggles(tagsScrollView, tags,
                (name) => options.IsTagOn(name), (name, value) => options.SetIsTagOn(name, value),
                () => RequestRefresh?.Invoke());

            if (visualizerOptionsCallbackSet == false)
            {
                visualizerOptionsCallbackSet = true;
                dataCountDropdown.RegisterValueChangedCallback(evt =>
                {
                    viewerOptions.ViewDataCount = ViewerOptions.ViewDataCountChoices[dataCountDropdown.index];
                    RequestRefresh?.Invoke();
                });

                sortMethodDropdown.RegisterValueChangedCallback(evt =>
                {
                    viewerOptions.SortMethod = ViewerOptions.SortMethodChoices[sortMethodDropdown.index];
                    RequestRefresh?.Invoke();
                });

                viewerTypeDropdown.RegisterValueChangedCallback(evt =>
                {
                    viewerOptions.ViewerType = ViewerOptions.ViewerTypeChoices[viewerTypeDropdown.index];
                    RequestRefresh?.Invoke();
                });
            }
        }
        #endregion
    }
}