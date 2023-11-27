using System;
using System.Collections.Generic;
using System.Linq;
using Unity.PerformanceTesting.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PerformanceTestReportViewer.UI.Visualizers
{
    public class BarsVisualizer : AbstractVisualizer
    {
        private new class UxmlTraits : VisualElement.UxmlTraits { }
        private new class UxmlFactory : UxmlFactory<BarsVisualizer, UxmlTraits> { }

        private static readonly string layoutPath = $"{Constants.LayoutPath}/{nameof(BarsVisualizer)}.uxml";


        public string XAxisName
        {
            get => _xAxisName;
            set
            {
                _xAxisName = value;
                Refresh();
            }
        }
        private string _xAxisName;

        public string[] GroupNames
        {
            get => _groupNames;
            set
            {
                _groupNames = value;
                Refresh();
            }
        }
        private string[] _groupNames;

        private double showingMin;
        private double showingMax;
        private SampleElement[] showingElements;

        private List<Rect> barRects = new();
        private List<Vertex> vertices = new();
        private List<ushort> indices = new();

        public BarsVisualizer()
        {
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(layoutPath).CloneTree(this);
            InitCommonVisualElements();
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
        }

        public override bool MustShown()
        {
            return true;
        }

        public override void Refresh()
        {
            //  TODO: 사용하는 값에 대한 선택지 제공
            if (Item == null)
                return;

            SetHeaderText();
            MarkDirtyRepaint();
            renderArea.MarkDirtyRepaint();
        }

        private void RefreshAxis(Rect chartRect)
        {
            xAxis.Clear();
            yAxis.Clear();
            if (Item == null)
                return;

            ChartUtility.MakeXAxis_ValueRange(xAxis, chartRect, showingMin, showingMax, valueCount: 5, XAxisName);
            if (_groupNames != null)
            {
                ChartUtility.MakeYAxis_DataLabel(yAxis, chartRect, _groupNames);
            }
            else
            {
                ChartUtility.MakeYAxis_DataLabel(yAxis, chartRect, showingElements.Select(e => e.RawName).ToArray());
            }
        }

        private void RefreshLegend(Rect chartRect)
        {
            legends.Clear();
            if (Item == null)
                return;

            if (_groupNames == null)
                return;

            var legendList = new List<(string, Color)>();
            var labels = showingElements.Select(e => e.RawName).ToArray();
            for (var i = 0; i < labels.Length; i++)
            {
                legendList.Add((labels[i], GetDataColor(i)));
            }

            ChartUtility.MakeLegend(legends, legendList);
        }
        
        protected override void OnRenderAreaGenerateVisualContent(MeshGenerationContext ctx)
        {
            Rect chartRect = GetChartRect();
            ViewerModule.Instance.QueueAction(() =>
            {
                RefreshAxis(chartRect);
                RefreshLegend(chartRect);
            });

            IEnumerable<SampleElement> itemsFiltered =
                Item.Values.TrySort(v => v.Average, ViewerModule.Instance.ViewerOptions.SortMethod);

            itemsFiltered = itemsFiltered.Where(e => e.MustShown(ViewerModule.Instance.ViewerOptions, TestResultOptions));
            itemsFiltered = itemsFiltered.Take(ViewerModule.Instance.ViewerOptions.ViewDataCount);

            showingElements = itemsFiltered.ToArray();
            if (showingElements.Length == 0)
                return;

            barRects.Clear();
            vertices.Clear();
            indices.Clear();
            if (_groupNames != null)
            {
                showingMin = showingElements.Min(v => v.Values.Min().Value);
                showingMax = showingElements.Max(v => v.Values.Max().Value);
                for (int i = 0; i < _groupNames.Length; ++i)
                {
                    ChartUtility.GenerateBars(i, _groupNames.Length, showingMin, showingMax,
                        showingElements.Select(e => e.Values[i].Value).ToArray(), GetChartRect(), ctx, barRects, vertices, indices);
                }
            }
            else
            {
                showingMin = showingElements.Min(v => v.Average);
                showingMax = showingElements.Max(v => v.Average);
                for (var i = 0; i < showingElements.Length; i++)
                {
                    SampleElement sampleElement = showingElements[i];
                    ChartUtility.GenerateBars(i, showingElements.Length, showingMin, showingMax, new[] { sampleElement.Average }, GetChartRect(), ctx, barRects, vertices, indices);
                }
            }

            if (vertices.Count == 0)
                return;

            MeshWriteData mwd = ctx.Allocate(vertices.Count, indices.Count);
            mwd.SetAllVertices(vertices.ToArray());
            mwd.SetAllIndices(indices.ToArray());
        }

        #region EventHandlers

        private void OnMouseMove(MouseMoveEvent evt)
        {
            Vector2 relativeMousePos = evt.mousePosition - renderArea.worldBound.position;
            if (ChartUtility.TryGetHoveringBarIndex(relativeMousePos, GetChartRect(), barRects, out int index))
            {
                Rect rectInRenderArea = barRects[index];
                Rect worldRect = renderArea.LocalToWorld(rectInRenderArea);
                Rect localRect = this.WorldToLocal(worldRect);
                valueTooltip.visible = true;
                var worldPos = new Vector3(localRect.center.y, localRect.center.y, 0);
                valueTooltip.style.left = GetChartRect().center.x;
                valueTooltip.style.top = worldPos.y - valueTooltip.resolvedStyle.height / 2f;
                
                if (_groupNames != null)
                {
                    int groupIndex = index % _groupNames.Length;
                    int valueIndex = index / _groupNames.Length;
                    valueTooltip.text = showingElements[valueIndex].Values[groupIndex]!.Value.ToString("F2");
                }
                else
                {
                    valueTooltip.text = showingElements[index].Average.ToString("F2");
                }
            }
            else
            {
                valueTooltip.visible = false;
            }
        }
        #endregion
    }
}