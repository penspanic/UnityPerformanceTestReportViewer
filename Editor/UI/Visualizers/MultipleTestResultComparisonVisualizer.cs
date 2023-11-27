using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.UIElements;

namespace PerformanceTestReportViewer.UI.Visualizers
{
    public class MultipleTestResultComparisonVisualizer : AbstractVisualizer
    {
        private new class UxmlTraits : VisualElement.UxmlTraits { }
        private new class UxmlFactory : UxmlFactory<MultipleTestResultComparisonVisualizer, UxmlTraits> { }

        private static readonly string layoutPath = $"{Constants.LayoutPath}/{nameof(MultipleTestResultComparisonVisualizer)}.uxml";

        public MultipleTestResultComparisonVisualizer()
        {
            AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(layoutPath).CloneTree(this);
            InitCommonVisualElements();
        }

        public override bool MustShown() => Item?.MustShown(TestResultOptions) ?? false;

        public override void Refresh()
        {
            if (Item == null)
                return;

            header.text = string.Join(", ", Item.Values.Select(e => e.SampleDefinition).Distinct().Select(e => e.Name));
            MarkDirtyRepaint();
            renderArea.MarkDirtyRepaint();
        }

        private SampleElement GetDefaultElement()
        {
            return Item?.Values?.FirstOrDefault();
        }

        private void RefreshAxis(Rect chartRect)
        {
            xAxis.Clear();
            yAxis.Clear();
            if (Item == null)
                return;

            var defaultElement = GetDefaultElement();
            ChartUtility.MakeXAxis_DataLabel(xAxis, chartRect, GroupedItem.GroupedResult.Children.Select(c => c.Parameter).ToArray());
            double minValue = Item.Values.SelectMany(e => e.Values).Where(v => v.HasValue).Select(v => v.Value).Min();
            double maxValue = defaultElement.Values.Where(v => v.HasValue).Select(v => v.Value).Max();
            ChartUtility.MakeYAxis_ValueRange(yAxis, chartRect, minValue, maxValue, valueCount: 5, defaultElement.SampleDefinition.SampleUnit.ToString());
        }

        private void RefreshLegend(Rect chartRect)
        {
            legends.Clear();
            if (Item == null)
                return;

            var legendList = new List<(string, Color)>();
            for (var i = 0; i < Item.Values.Length; i++)
            {
                legendList.Add((Item.Values[i].RawName, GetDataColor(i)));
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

            for (var i = 0; i < Item.Values.Length; i++)
            {
                SampleElement sampleElement = Item.Values[i];
                if (sampleElement.MustShown(ViewerModule.Instance.ViewerOptions, TestResultOptions) == false)
                    return;

                GenerateLine(i, sampleElement, ctx);
            }
        }

        private void GenerateLine(int index, SampleElement sampleElement, MeshGenerationContext ctx)
        {
            if (sampleElement.Values.Length == 0)
                return;

            double min = sampleElement.Values.Min().Value;
            double max = sampleElement.Values.Max().Value;

            //Vector2 drawPos = new Vector2(20f, 20f);
            Vector2 drawPos = new Vector2(0f, 0f);

            double gap = max - min;
            ctx.painter2D.BeginPath();

            bool painterMoved = false;
            Rect chartRect = GetChartRect();
            for (int i = 0; i < sampleElement.Values.Length; ++i)
            {
                double? value = sampleElement.Values[i];
                if (value == null)
                    continue;

                float xPos = drawPos.x + ChartUtility.XPosByIndex(chartRect, i, sampleElement.Values.Length);
                float yPos = drawPos.y + Mathf.Lerp(chartRect.yMax, chartRect.yMin, ((float)(value.Value - min) / (float)gap));
                Vector2 pos = new Vector2(xPos, yPos);
                if (painterMoved == false)
                {
                    painterMoved = true;
                    ctx.painter2D.MoveTo(pos);
                }
                else
                {
                    ctx.painter2D.LineTo(pos);
                }
            }

            ctx.painter2D.lineWidth = 2f;
            ctx.painter2D.strokeColor = GetDataColor(index);
            ctx.painter2D.Stroke();
        }
    }
}