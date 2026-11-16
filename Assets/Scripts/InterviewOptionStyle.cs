using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

/// <summary>Restyles pooled Yarn options without replacing their selection or metadata handling.</summary>
[DisallowMultipleComponent]
public sealed class InterviewOptionStyle : MonoBehaviour
{
    private OptionItem item;
    private Image originalBackground;
    private TMP_Text label;
    private LayoutElement layout;
    private InterviewPanelGraphic surface;

    private void Awake()
    {
        item = GetComponent<OptionItem>();
        originalBackground = GetComponent<Image>();
        label = GetComponentInChildren<TMP_Text>(true);
        layout = GetComponent<LayoutElement>();
        if (layout == null) layout = gameObject.AddComponent<LayoutElement>();

        // Keep the original Image as the target for existing answer/highlight scripts.
        // Its colour is mirrored by the procedural panel instead of drawing a second rectangle.
        foreach (var graphic in GetComponentsInChildren<Graphic>(true)) graphic.enabled = false;
        var rect = new GameObject("Interview Choice Surface", typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(transform, false);
        rect.SetAsFirstSibling();
        Stretch(rect);
        surface = rect.gameObject.AddComponent<InterviewPanelGraphic>();
        surface.raycastTarget = true;
        if (label != null)
        {
            label.enabled = true;
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(38, 18);
            label.rectTransform.offsetMax = new Vector2(-38, -18);
            var local = label.rectTransform.localPosition;
            local.z = 0;
            label.rectTransform.localPosition = local;
            label.fontSize = 36;
            label.enableAutoSizing = false;
            label.alignment = TextAlignmentOptions.Center;
            label.fontStyle = FontStyles.Normal;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.raycastTarget = false;
        }
        layout.minWidth = 0;
        layout.preferredWidth = -1;
        layout.flexibleWidth = 1;
        layout.minHeight = 104;
        layout.flexibleHeight = 0;
        Refresh();
    }

    private void LateUpdate() => Refresh();

    public void Refresh()
    {
        if (surface == null || item == null) return;
        bool selected = UnityEngine.EventSystems.EventSystem.current != null && item.IsHighlighted;
        var body = originalBackground != null ? originalBackground.color : new Color(0.025f, 0.03f, 0.035f, 0.82f);
        body.a = 0.82f;
        if (selected) body = Color.Lerp(body, new Color(0.16f, 0.18f, 0.18f, 0.9f), 0.35f);
        surface.color = body;
        var rule = new Color(0.8f, 0.82f, 0.81f, selected ? 0.85f : 0.4f);
        if (surface.ruleColor != rule)
        {
            surface.ruleColor = rule;
            surface.SetVerticesDirty();
        }
        if (label == null) return;
        label.color = item.interactable ? (selected ? Color.white : new Color(0.91f, 0.92f, 0.91f)) : new Color(0.5f, 0.5f, 0.5f);
        float width = Mathf.Max(200, ((RectTransform)transform).rect.width - 76);
        float height = Mathf.Max(104, label.GetPreferredValues(label.text, width, Mathf.Infinity).y + 36);
        if (Mathf.Abs(layout.preferredHeight - height) > 0.1f) layout.preferredHeight = height;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
