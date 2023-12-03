using System;
using System.Collections.Generic;
using System.Linq;
using PerformanceTestReportViewer.Editor.UI.Visualizers;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PerformanceTestReportViewer.Editor.UI
{
    public static class ChartUtility
    {
        private static readonly Vector2 labelSize = new Vector2(50, 15);
        public static float XPosByIndex(Rect rect, int dataIndex, int count)
        {
            if (count < 2)
                return rect.xMax;

            return Mathf.Lerp(rect.xMin, rect.xMax, (float)dataIndex / (count - 1));
        }

        public static float YPosByIndex(Rect rect, int dataIndex, int count, out float ySize)
        {
            ySize = rect.height / count;
            if (count < 2)
                return rect.yMin;

            return rect.yMin + ySize * dataIndex;
        }

        public static void MakeXAxis_DataLabel(VisualElement parent, Rect chartRect, string[] axisTexts)
        {
            parent.Clear();
            int index = 0;
            int totalCount = axisTexts.Length;
            var axisNameLabel = CreateLabelNonLayout("Parameter", new Rect(new Vector2(chartRect.center.x, 20), labelSize));
            axisNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            parent.Add(axisNameLabel);
            foreach (string xAxisText in axisTexts)
            {
                Vector2 pos = new Vector2(XPosByIndex(chartRect, index, totalCount), 5f);
                pos.x -= labelSize.x / 2f;
                var label = CreateLabelNonLayout(xAxisText, new Rect(pos, labelSize));

                parent.Add(label);
                ++index;
            }
        }

        public static void MakeYAxis_DataLabel(VisualElement parent, Rect chartRect, string[] axisTexts)
        {
            parent.Clear();
            int index = 0;
            int totalCount = axisTexts.Length;
            foreach (string yAxisText in axisTexts)
            {
                Vector2 pos = new Vector2(5f, YPosByIndex(chartRect, index, totalCount, out float ySize));
                pos.y += ySize / 2f;
                pos.y -= labelSize.y / 2f;
                var label = CreateLabelNonLayout(yAxisText, new Rect(pos, labelSize));
                label.style.unityTextAlign = TextAnchor.MiddleLeft;

                parent.Add(label);
                ++index;
            }
        }

        public static void MakeXAxis_ValueRange(VisualElement parent, Rect chartRect, double min, double max, int valueCount, string axisName)
        {
            parent.Clear();
            valueCount = Math.Max(valueCount, 2);
            var axisNameLabel = CreateLabelNonLayout(axisName, new Rect(new Vector2(0, 13), labelSize + new Vector2(20, 25)));
            axisNameLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            axisNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            parent.Add(axisNameLabel);
            for (int i = 0; i < valueCount; ++i)
            {
                Vector2 pos = new Vector2(Mathf.Lerp(chartRect.xMin, chartRect.xMax, 1f - (float)i / (valueCount - 1)), 0);
                pos.x -= labelSize.x / 2f;
                string valueText = Mathf.Lerp((float)min, (float)max, (float)i / (valueCount - 1)).ToString("F2"); // TODO: double type lerp
                var label = CreateLabelNonLayout(valueText, new Rect(pos, labelSize));
                parent.Add(label);
            }
        }

        public static void MakeYAxis_ValueRange(VisualElement parent, Rect chartRect, double min, double max, int valueCount, string axisName)
        {
            parent.Clear();
            valueCount = Math.Max(valueCount, 2);
            var axisNameLabel = CreateLabelNonLayout(axisName, new Rect(new Vector2(-20 , -20), labelSize + new Vector2(20, 0)));
            axisNameLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            axisNameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            parent.Add(axisNameLabel);
            for (int i = 0; i < valueCount; ++i)
            {
                Vector2 pos = new Vector2(0, Mathf.Lerp(chartRect.yMax, chartRect.yMin, (float)i / (valueCount - 1)));

                string valueText = Mathf.Lerp((float)min, (float)max, (float)i / (valueCount - 1)).ToString(); // TODO: double type lerp
                var label = CreateLabelNonLayout(valueText, new Rect(pos, labelSize));
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                parent.Add(label);
            }
        }

        public static void MakeLegend(VisualElement parent, IEnumerable<(string name, Color color)> items)
        {
            parent.Clear();
            foreach ((string name, Color color) in items)
            {
                VisualElement itemParent = new();
                itemParent.style.flexDirection = FlexDirection.Row;
                VisualElement colorElement = new();
                colorElement.style.width = 10;
                colorElement.style.height = 10;
                colorElement.style.backgroundColor = color;
                colorElement.style.alignSelf = Align.Center;
                VisualElement label = new Label() { text = name };
                label.style.unityTextAlign = TextAnchor.MiddleLeft;
                label.style.marginRight = 5;
                itemParent.Add(colorElement);
                itemParent.Add(label);

                parent.Add(itemParent);
            }
        }

        public static Label CreateLabelNonLayout(string text, Rect rect)
        {
            var label = new Label() { text = text };
            label.style.position = Position.Absolute;
            //label.style.width = rect.size.x;
            label.style.maxWidth = rect.size.x;
            //label.style.height = rect.size.y;
            label.style.maxHeight = rect.size.y;
            label.style.left = rect.xMin;
            label.style.top = rect.yMin;
            label.style.unityTextAlign = TextAnchor.MiddleCenter;
            return label;
        }

        public static void DrawBar(Rect rect, Color color)
        {
            GL.Begin(GL.TRIANGLE_STRIP);
            GL.Color(color);
            GL.Vertex3(rect.xMin, rect.yMin, 0);
            GL.Vertex3(rect.xMax, rect.yMin, 0);
            GL.Vertex3(rect.xMin, rect.yMax, 0);
            GL.Vertex3(rect.xMax, rect.yMax, 0);
            GL.End();
        }

        public static void GenerateBars(int index, int totalCount, double min, double max, double[] values,
            Rect chartRect, MeshGenerationContext ctx, List<Rect> barRects, List<Vertex> vertices, List<ushort> indices)
        {
            double gap = max - min;

            float yPosStart = YPosByIndex(chartRect, index, totalCount, out float ySize);
            float ySpacing = 1;
            float yMargin = 6;
            float eachYSize = (ySize - (2 * yMargin) - (values.Length * ySpacing)) / values.Length;
            float realYSize = Math.Min(10, eachYSize);
            yMargin = (ySize - (realYSize + ySpacing) * values.Length) / 2f;
            for (int i = 0; i < values.Length; i++)
            {
                float yPos = yPosStart + yMargin + i * ySpacing + i * realYSize; 
                float xPosMin = Mathf.Lerp(chartRect.xMin, chartRect.xMax, 1f - Mathf.Max((float)(values[i] - min) / (float)gap, 0.03f));
                Rect barRect = default;
                barRect.yMin = yPos;
                barRect.yMax = barRect.yMin + realYSize;
                barRect.xMin = xPosMin;
                barRect.xMax = chartRect.xMax;
                barRects.Add(barRect);
                Color barColor = AbstractVisualizer.GetDataColor(i);
                AddBarMesh(barRect, barColor, vertices, indices);
            }
        }

        public static void AddBarMesh(Rect rect, Color color, List<Vertex> vertices, List<ushort> indices)
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

        public static bool TryGetHoveringBarIndex(Vector2 mousePos, Rect chartRect, List<Rect> barRects, out int index)
        {
            index = -1;
            for (int i = 0; i < barRects.Count; i++)
            {
                Rect barRect = barRects[i];
                if (barRect.Contains(mousePos))
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }
    }
}