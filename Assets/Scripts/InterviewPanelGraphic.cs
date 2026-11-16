using UnityEngine;
using UnityEngine.UI;

/// <summary>Translucent dialogue panel with fading ends and thin horizontal rules.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class InterviewPanelGraphic : MaskableGraphic
{
    [Range(0, 0.3f)] public float edgeFade = 0.055f;
    public Color ruleColor = new Color(0.78f, 0.8f, 0.79f, 0.55f);

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        var r = rectTransform.rect;
        var clear = color;
        clear.a = 0;
        float fade = r.width * edgeFade;
        Strip(vh, r.xMin, r.xMin + fade, r.yMin, r.yMax, clear, color);
        Strip(vh, r.xMin + fade, r.xMax - fade, r.yMin, r.yMax, color, color);
        Strip(vh, r.xMax - fade, r.xMax, r.yMin, r.yMax, color, clear);
        var transparentRule = ruleColor;
        transparentRule.a = 0;
        float thickness = 1.5f;
        foreach (float y in new[] { r.yMin, r.yMax - thickness })
        {
            Strip(vh, r.xMin, r.xMin + fade, y, y + thickness, transparentRule, ruleColor);
            Strip(vh, r.xMin + fade, r.xMax - fade, y, y + thickness, ruleColor, ruleColor);
            Strip(vh, r.xMax - fade, r.xMax, y, y + thickness, ruleColor, transparentRule);
        }
    }

    private static void Strip(VertexHelper vh, float left, float right, float bottom, float top, Color a, Color b)
    {
        int start = vh.currentVertCount;
        vh.AddVert(new Vector3(left, bottom), a, Vector2.zero);
        vh.AddVert(new Vector3(left, top), a, Vector2.zero);
        vh.AddVert(new Vector3(right, top), b, Vector2.zero);
        vh.AddVert(new Vector3(right, bottom), b, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
        vh.AddTriangle(start, start + 2, start + 3);
    }
}
