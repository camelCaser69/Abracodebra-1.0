using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasRenderer))]
public class HexGridRenderer : MaskableGraphic
{
    public bool showGrid = true;
    public Color gridColor = Color.white;
    public float lineThickness = 1f;

    private float HexSize {
        get { return (HexGridManager.Instance != null) ? HexGridManager.Instance.hexSize : 50f; }
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (!showGrid)
            return;

        Rect rect = rectTransform.rect;
        Vector2 origin = rect.center;

        int cols = Mathf.CeilToInt(rect.width / (HexSize * Mathf.Sqrt(3))) + 2;
        int rows = Mathf.CeilToInt(rect.height / (HexSize * 1.5f)) + 2;

        List<Vector2> linePoints = new List<Vector2>();

        for (int q = -cols; q <= cols; q++)
        {
            for (int r = -rows; r <= rows; r++)
            {
                HexCoords hex = new HexCoords(q, r);
                Vector2 hexCenter = hex.HexToWorld(HexSize) + origin;
                List<Vector2> corners = new List<Vector2>();
                // For flat top, use corners at angles 0, 60, 120, 180, 240, 300.
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * 60f;
                    float rad = angle * Mathf.Deg2Rad;
                    Vector2 offset = new Vector2(HexSize * Mathf.Cos(rad), HexSize * Mathf.Sin(rad));
                    corners.Add(hexCenter + offset);
                }
                for (int i = 0; i < 6; i++)
                {
                    int next = (i + 1) % 6;
                    linePoints.Add(corners[i]);
                    linePoints.Add(corners[next]);
                }
            }
        }
        for (int i = 0; i < linePoints.Count; i += 2)
        {
            AddLineQuad(vh, linePoints[i], linePoints[i + 1], lineThickness, gridColor);
        }
    }

    private void AddLineQuad(VertexHelper vh, Vector2 start, Vector2 end, float thickness, Color col)
    {
        Vector2 direction = (end - start).normalized;
        Vector2 normal = new Vector2(-direction.y, direction.x);
        Vector2 offset = normal * (thickness * 0.5f);

        int idx = vh.currentVertCount;
        vh.AddVert(start + offset, col, Vector2.zero);
        vh.AddVert(start - offset, col, Vector2.zero);
        vh.AddVert(end - offset, col, Vector2.zero);
        vh.AddVert(end + offset, col, Vector2.zero);
        vh.AddTriangle(idx, idx + 1, idx + 2);
        vh.AddTriangle(idx, idx + 2, idx + 3);
    }
}
