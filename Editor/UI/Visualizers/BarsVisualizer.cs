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

        public BarsVisualizer()
        {
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(layoutPath).CloneTree(this);
            InitCommonVisualElements();
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

            vertices.Clear();
            indices.Clear();
            if (_groupNames != null)
            {
                showingMin = showingElements.Min(v => v.Values.Min().Value);
                showingMax = showingElements.Max(v => v.Values.Max().Value);
                for (int i = 0; i < _groupNames.Length; ++i)
                {
                    GenerateBars(i, _groupNames.Length, showingMin, showingMax,
                        showingElements.Select(e => e.Values[i].Value).ToArray(), ctx, vertices, indices);
                }
            }
            else
            {
                showingMin = showingElements.Min(v => v.Average);
                showingMax = showingElements.Max(v => v.Average);
                for (var i = 0; i < showingElements.Length; i++)
                {
                    SampleElement sampleElement = showingElements[i];
                    GenerateBars(i, showingElements.Length, showingMin, showingMax, new[] { sampleElement.Average }, ctx, vertices, indices);
                }
            }

            if (vertices.Count == 0)
                return;

            MeshWriteData mwd = ctx.Allocate(vertices.Count, indices.Count);
            mwd.SetAllVertices(vertices.ToArray());
            mwd.SetAllIndices(indices.ToArray());
        }

        private List<Vertex> vertices = new();
        //static readonly ushort[] k_Indices = { 0, 1, 2, 3 };
        private List<ushort> indices = new();

        private void GenerateBars(int index, int totalCount, double min, double max, double[] values,
            MeshGenerationContext ctx, List<Vertex> vertices, List<ushort> indices)
        {
            double gap = max - min;
            Rect chartRect = GetChartRect();
            float yPosStart = ChartUtility.YPosByIndex(chartRect, index, totalCount, out float ySize);
            float ySpacing = 1;
            float eachYSize = (ySize - (values.Length + 1) * ySpacing) / values.Length;
            float realYSize = Math.Min(10, eachYSize);
            float yMargin = (ySize - realYSize * values.Length - ySpacing * (values.Length - 1)) / 2f;
            for (int i = 0; i < values.Length; i++)
            {
                float yPos = yPosStart + yMargin + i * ySpacing + i * realYSize; 
                float xPosMin = Mathf.Lerp(chartRect.xMin, chartRect.xMax, 1f - Mathf.Max((float)(values[i] - min) / (float)gap, 0.03f));
                Rect barRect = default;
                barRect.yMin = yPos;
                barRect.yMax = barRect.yMin + realYSize;
                barRect.xMin = xPosMin;
                barRect.xMax = chartRect.xMax;
                Color barColor = GetDataColor(i);
                AddBarMesh(barRect, barColor, this.vertices, indices);
            }
        }

        private void AddBarMesh(Rect rect, Color color, List<Vertex> vertices, List<ushort> indices)
        {
            int vertexStartIndex = vertices.Count;
            vertices.Add(new Vertex() // 0 : top left
            {
                position = new Vector3(rect.xMin, rect.yMin, Vertex.nearZ),
                tint = color
            });
            vertices.Add(new Vertex() // 1 : top right
            {
                position = new Vector3(rect.xMax, rect.yMin, Vertex.nearZ),
                tint = color
            });
            vertices.Add(new Vertex() // 2 : bottom left
            {
                position = new Vector3(rect.xMin, rect.yMax, Vertex.nearZ),
                tint = color
            });
            vertices.Add(new Vertex() // bottom right
            {
                position = new Vector3(rect.xMax, rect.yMax, Vertex.nearZ),
                tint = color
            });

            indices.Add((ushort)(vertexStartIndex + 0));
            indices.Add((ushort)(vertexStartIndex + 1));
            indices.Add((ushort)(vertexStartIndex + 2));
            indices.Add((ushort)(vertexStartIndex + 1));
            indices.Add((ushort)(vertexStartIndex + 3));
            indices.Add((ushort)(vertexStartIndex + 2));
        }
    }
}