using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Draws a cubic Bézier "S-curve" in a Screen Space - Overlay UI. Also performs a custom
/// hit-test so that pointer events only register if the pointer is near the curve.
/// </summary>
[RequireComponent(typeof(CanvasRenderer))]
public class UICubicBezier : Graphic
{
    [Range(2,100)] public int segments = 30;
    public float lineThickness = 4f;

    [Header("Positions in local space")]
    public Vector2 startPos;
    public Vector2 endPos;

    private List<Vector2> sampledPoints = new List<Vector2>();

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        sampledPoints.Clear();

        if ((startPos - endPos).sqrMagnitude < 1f)
            return;

        // "S-curve" approach: horizontal tangents
        float dx = (endPos.x - startPos.x) * 0.5f;
        Vector2 ctrl1 = startPos + new Vector2(dx, 0f);
        Vector2 ctrl2 = endPos - new Vector2(dx, 0f);

        // Sample the curve
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float) segments;
            Vector2 p = CubicBezier(startPos, ctrl1, ctrl2, endPos, t);
            sampledPoints.Add(p);
        }

        // Create a mesh
        for (int i=0; i<sampledPoints.Count -1; i++)
        {
            Vector2 p0 = sampledPoints[i];
            Vector2 p1 = sampledPoints[i+1];

            Vector2 dir = (p1 - p0).normalized;
            Vector2 normal = new Vector2(-dir.y, dir.x) * (lineThickness*0.5f);

            Vector2 v0 = p0 + normal;
            Vector2 v1 = p0 - normal;
            Vector2 v2 = p1 - normal;
            Vector2 v3 = p1 + normal;

            AddQuad(v0, v1, v2, v3, vh);
        }
    }

    private Vector2 CubicBezier(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
    {
        float u = 1f - t;
        float tt = t*t;
        float uu = u*u;
        float uuu = uu*u;
        float ttt = tt*t;

        Vector2 p = uuu * p0;
        p += 3f * uu * t * p1;
        p += 3f * u * tt * p2;
        p += ttt * p3;
        return p;
    }

    private void AddQuad(Vector2 v0, Vector2 v1, Vector2 v2, Vector2 v3, VertexHelper vh)
    {
        int idx = vh.currentVertCount;
        vh.AddVert(v0, color, Vector2.zero);
        vh.AddVert(v1, color, Vector2.zero);
        vh.AddVert(v2, color, Vector2.zero);
        vh.AddVert(v3, color, Vector2.zero);

        vh.AddTriangle(idx, idx+1, idx+2);
        vh.AddTriangle(idx, idx+2, idx+3);
    }

    /// <summary>
    /// Update the curve’s start/end in local coordinates.
    /// </summary>
    public void UpdateCurve(Vector2 startLocal, Vector2 endLocal)
    {
        startPos = startLocal;
        endPos   = endLocal;
        SetVerticesDirty();
    }

    /// <summary>
    /// Custom Raycast so we only register clicks if pointer is near the line.
    /// </summary>
    public override bool Raycast(Vector2 sp, Camera eventCamera)
    {
        // Convert screen point to local coords
        Vector2 localPos;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, sp, eventCamera, out localPos))
            return false;

        // If we have no sampled points, reject
        if (sampledPoints.Count < 2) return false;

        float maxDist = lineThickness*0.5f + 2f; // small margin
        float minDistance = float.MaxValue;

        // find min distance from localPos to any segment
        for (int i=0; i<sampledPoints.Count-1; i++)
        {
            Vector2 segStart = sampledPoints[i];
            Vector2 segEnd   = sampledPoints[i+1];
            float dist = DistanceToSegment(localPos, segStart, segEnd);
            if (dist < minDistance) 
                minDistance = dist;
            if (minDistance < maxDist) 
                return true; // early exit
        }
        return false;
    }

    // helper for point-line distance
    private float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ap = p - a;
        Vector2 ab = b - a;
        float magAB = ab.sqrMagnitude;
        float dot = Vector2.Dot(ap, ab)/magAB;
        dot = Mathf.Clamp01(dot);
        Vector2 proj = a + ab*dot;
        return (p - proj).magnitude;
    }
}
