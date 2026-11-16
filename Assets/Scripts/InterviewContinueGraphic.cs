using UnityEngine;
using UnityEngine.UI;

/// <summary>Small double chevron shown only after the current line is fully visible.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class InterviewContinueGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = rectTransform.rect;
        float center = r.center.x;
        float half = r.width * 0.38f;
        foreach (float y in new[] { r.yMax - 5, r.yMax - 16 })
        {
            Segment(vh, new Vector2(center - half, y), new Vector2(center, y - 10), 4);
            Segment(vh, new Vector2(center, y - 10), new Vector2(center + half, y), 4);
        }
    }

    private void Segment(VertexHelper vh, Vector2 a, Vector2 b, float thickness)
    {
        var direction = (b - a).normalized;
        var normal = new Vector2(-direction.y, direction.x) * thickness * 0.5f;
        int start = vh.currentVertCount;
        vh.AddVert(a - normal, color, Vector2.zero);
        vh.AddVert(a + normal, color, Vector2.zero);
        vh.AddVert(b + normal, color, Vector2.zero);
        vh.AddVert(b - normal, color, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }
}
