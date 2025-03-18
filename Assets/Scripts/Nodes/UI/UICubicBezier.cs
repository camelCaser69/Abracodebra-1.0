using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(CanvasRenderer))]
public class UICubicBezier : MaskableGraphic
{
    [Range(2, 100)] public int segments = 30;
    public float lineThickness = 4f;

    [Header("Positions in local space")]
    public Vector2 startPos;
    public Vector2 endPos;

    private List<Vector2> sampledPoints = new List<Vector2>();

    protected override void Awake()
    {
        base.Awake();
        // With MaskableGraphic, the property "maskable" is available.
        this.maskable = true;
    }

    protected override void OnPopulateMesh(VertexHelper vh)
{
    vh.Clear();
    sampledPoints.Clear();

    if ((startPos - endPos).sqrMagnitude < 1f)
        return;

    // Create control points (S-curve style)
    float dx = (endPos.x - startPos.x) * 0.5f;
    Vector2 ctrl1 = startPos + new Vector2(dx, 0f);
    Vector2 ctrl2 = endPos - new Vector2(dx, 0f);

    // Sample the curve
    for (int i = 0; i <= segments; i++)
    {
        float t = i / (float)segments;
        Vector2 p = CubicBezier(startPos, ctrl1, ctrl2, endPos, t);
        sampledPoints.Add(p);
    }

    // We define an "outer ring" (alpha=0) and an "inner ring" (alpha=1)
    // so we can fade out the edges.
    float outerThickness = lineThickness * 0.5f;
    float innerThickness = outerThickness - 1f; // 1px fade region

    for (int i = 0; i < sampledPoints.Count - 1; i++)
    {
        Vector2 p0 = sampledPoints[i];
        Vector2 p1 = sampledPoints[i + 1];

        Vector2 dir = (p1 - p0).normalized;
        Vector2 normal = new Vector2(-dir.y, dir.x);

        // Outer ring (alpha=0)
        Vector2 v0 = p0 + normal * outerThickness;
        Vector2 v1 = p0 - normal * outerThickness;
        Vector2 v2 = p1 - normal * outerThickness;
        Vector2 v3 = p1 + normal * outerThickness;

        // Inner ring (alpha=1)
        Vector2 v0i = p0 + normal * innerThickness;
        Vector2 v1i = p0 - normal * innerThickness;
        Vector2 v2i = p1 - normal * innerThickness;
        Vector2 v3i = p1 + normal * innerThickness;

        int idx = vh.currentVertCount;

        // Add 8 vertices (outer ring + inner ring)
        vh.AddVert(v0, new Color(color.r, color.g, color.b, 0), Vector2.zero);
        vh.AddVert(v1, new Color(color.r, color.g, color.b, 0), Vector2.zero);
        vh.AddVert(v2, new Color(color.r, color.g, color.b, 0), Vector2.zero);
        vh.AddVert(v3, new Color(color.r, color.g, color.b, 0), Vector2.zero);

        vh.AddVert(v0i, new Color(color.r, color.g, color.b, 1), Vector2.zero);
        vh.AddVert(v1i, new Color(color.r, color.g, color.b, 1), Vector2.zero);
        vh.AddVert(v2i, new Color(color.r, color.g, color.b, 1), Vector2.zero);
        vh.AddVert(v3i, new Color(color.r, color.g, color.b, 1), Vector2.zero);

        // Bridge the outer ring (alpha=0) to the inner ring (alpha=1)
        vh.AddTriangle(idx,   idx+4, idx+5);
        vh.AddTriangle(idx,   idx+5, idx+1);

        vh.AddTriangle(idx+1, idx+5, idx+6);
        vh.AddTriangle(idx+1, idx+6, idx+2);

        vh.AddTriangle(idx+2, idx+6, idx+7);
        vh.AddTriangle(idx+2, idx+7, idx+3);

        vh.AddTriangle(idx+3, idx+7, idx+4);
        vh.AddTriangle(idx+3, idx+4, idx);

        // **Fill the center** (the inner ring) so we don't just get an outline
        vh.AddTriangle(idx+4, idx+5, idx+6);
        vh.AddTriangle(idx+4, idx+6, idx+7);
    }
}



    private Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector2 p = uuu * p0;
        p += 3f * uu * t * p1;
        p += 3f * u * tt * p2;
        p += ttt * p3;
        return p;
    }

    private void AddQuad(Vector2 v0, Vector2 v1, Vector2 v2, Vector2 v3, VertexHelper vh)
    {
        int idx = vh.currentVertCount;

        // Vertices on the "edge" get alpha = 0, center vertices get alpha = 1
        Color edgeColor = new Color(color.r, color.g, color.b, 0);
        Color centerColor = new Color(color.r, color.g, color.b, 1);

        vh.AddVert(v0, edgeColor, Vector2.zero);
        vh.AddVert(v1, edgeColor, Vector2.zero);
        vh.AddVert(v2, edgeColor, Vector2.zero);
        vh.AddVert(v3, edgeColor, Vector2.zero);

        // Two triangles
        vh.AddTriangle(idx, idx + 1, idx + 2);
        vh.AddTriangle(idx, idx + 2, idx + 3);
    }


    /// <summary>
    /// Updates the curve’s start and end positions in local coordinates and forces a redraw.
    /// </summary>
    public void UpdateCurve(Vector2 startLocal, Vector2 endLocal)
    {
        startPos = startLocal;
        endPos = endLocal;
        SetVerticesDirty();
    }

    /// <summary>
    /// Custom Raycast so that pointer events only register if the pointer is near the line.
    /// </summary>
    public override bool Raycast(Vector2 sp, Camera eventCamera)
    {
        Vector2 localPos;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, sp, eventCamera, out localPos))
            return false;

        if (sampledPoints.Count < 2)
            return false;

        float maxDist = lineThickness * 0.5f + 2f;
        float minDistance = float.MaxValue;

        for (int i = 0; i < sampledPoints.Count - 1; i++)
        {
            Vector2 segStart = sampledPoints[i];
            Vector2 segEnd = sampledPoints[i + 1];
            float dist = DistanceToSegment(localPos, segStart, segEnd);
            if (dist < minDistance)
                minDistance = dist;
            if (minDistance < maxDist)
                return true;
        }
        return false;
    }

    private float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ap = p - a;
        Vector2 ab = b - a;
        float abSqr = ab.sqrMagnitude;
        float dot = Vector2.Dot(ap, ab) / abSqr;
        dot = Mathf.Clamp01(dot);
        Vector2 proj = a + ab * dot;
        return (p - proj).magnitude;
    }
}
