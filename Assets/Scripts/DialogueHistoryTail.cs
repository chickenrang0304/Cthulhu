using UnityEngine;
using UnityEngine.UI;

/// <summary>Pixel friendly dialogue tail, facing the speaker's portrait.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class DialogueHistoryTail : MaskableGraphic
{
    public bool PointRight { get; set; }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var rect = rectTransform.rect;
        var edge = PointRight ? rect.xMin : rect.xMax;
        var tip = PointRight ? rect.xMax : rect.xMin;
        vh.AddVert(new Vector3(edge, rect.yMax), color, Vector2.zero);
        vh.AddVert(new Vector3(tip, rect.center.y), color, Vector2.zero);
        vh.AddVert(new Vector3(edge, rect.yMin), color, Vector2.zero);
        vh.AddTriangle(0, 1, 2);
    }
}
