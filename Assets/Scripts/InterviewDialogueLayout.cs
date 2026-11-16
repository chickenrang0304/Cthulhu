using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Yarn.Unity;

/// <summary>Speaker-based interview panels and centered choices, using the existing Yarn presenters.</summary>
[DisallowMultipleComponent]
public sealed class InterviewDialogueLayout : MonoBehaviour
{
    private LinePresenter line;
    private RectTransform lineRect;
    private RectTransform canvasRect;
    private GameObject continueIndicator;
    private readonly List<OptionsPresenter> optionsPresenters = new();
    private readonly Dictionary<OptionsPresenter, RectTransform> choiceContainers = new();
    private readonly List<OptionItem> optionItems = new();

    public void Configure(LinePresenter presenter, DialogueRunner runner)
    {
        line = presenter;
        lineRect = (RectTransform)line.transform;
        canvasRect = line.GetComponentInParent<Canvas>().rootCanvas.GetComponent<RectTransform>();

        foreach (var layout in line.GetComponents<LayoutGroup>()) layout.enabled = false;
        var fitter = line.GetComponent<ContentSizeFitter>();
        if (fitter != null) fitter.enabled = false;
        foreach (var graphic in line.GetComponentsInChildren<Graphic>(true)) graphic.enabled = false;
        var panel = UI("Interview Speech Surface", line.transform);
        Stretch(panel);
        panel.SetAsFirstSibling();
        var surface = panel.gameObject.AddComponent<InterviewPanelGraphic>();
        surface.color = new Color(0.018f, 0.022f, 0.025f, 0.78f);
        surface.raycastTarget = true;

        SetSpeaker(string.Empty);
        if (line.lineText != null)
        {
            var text = line.lineText;
            text.enabled = true;
            text.raycastTarget = false;
            text.fontSize = 32;
            text.enableAutoSizing = false;
            text.fontStyle = FontStyles.Normal;
            text.alignment = TextAlignmentOptions.TopLeft;
            text.textWrappingMode = TextWrappingModes.Normal;
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(32, 52);
            text.rectTransform.offsetMax = new Vector2(-32, -62);
            ResetDepth(text.rectTransform);
        }
        if (line.characterNameText != null)
        {
            var name = line.characterNameText;
            name.enabled = true;
            name.raycastTarget = false;
            name.fontSize = 21;
            name.fontStyle = FontStyles.Normal;
            name.color = new Color(0.78f, 0.82f, 0.81f);
            name.alignment = TextAlignmentOptions.Left;
            var rect = name.rectTransform;
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 1);
            rect.anchoredPosition = new Vector2(0, -17);
            rect.sizeDelta = new Vector2(-64, 30);
            ResetDepth(rect);
        }
        var indicator = UI("Interview Continue Indicator", line.transform);
        indicator.anchorMin = indicator.anchorMax = indicator.pivot = new Vector2(1, 0);
        indicator.anchoredPosition = new Vector2(-28, 16);
        indicator.sizeDelta = new Vector2(32, 32);
        indicator.gameObject.AddComponent<InterviewContinueGraphic>().raycastTarget = false;
        continueIndicator = indicator.gameObject;
        SetLineReady(false);

        foreach (var options in runner.GetComponentsInChildren<OptionsPresenter>(true))
        {
            optionsPresenters.Add(options);
            choiceContainers[options] = ConfigureOptions(options);
        }
        RefreshLayout();
    }

    private static RectTransform ConfigureOptions(OptionsPresenter options)
    {
        var root = (RectTransform)options.transform;
        Stretch(root);
        ResetDepth(root);
        foreach (var layout in options.GetComponents<LayoutGroup>()) layout.enabled = false;
        var fitter = options.GetComponent<ContentSizeFitter>();
        if (fitter != null) fitter.enabled = false;

        foreach (var graphic in options.GetComponentsInChildren<Graphic>(true))
            graphic.enabled = false;
        var shade = UI("Interview Choice Dimmer", root);
        Stretch(shade);
        shade.SetAsFirstSibling();
        var dimmer = shade.gameObject.AddComponent<Image>();
        dimmer.color = new Color(0, 0, 0, 0.58f);
        dimmer.raycastTarget = true;

        // A separate visual container avoids conflicting with the prefab's HorizontalLayoutGroup.
        var choices = UI("Interview Choice Container", root);
        var vertical = choices.gameObject.AddComponent<VerticalLayoutGroup>();
        vertical.childAlignment = TextAnchor.MiddleCenter;
        vertical.spacing = 28;
        vertical.childControlWidth = true;
        vertical.childControlHeight = true;
        vertical.childForceExpandWidth = true;
        vertical.childForceExpandHeight = false;
        choices.anchorMin = new Vector2(0.2f, 0.5f);
        choices.anchorMax = new Vector2(0.8f, 0.5f);
        choices.pivot = new Vector2(0.5f, 0.5f);
        choices.anchoredPosition = Vector2.zero;
        choices.sizeDelta = Vector2.zero;
        ResetDepth(choices);
        var content = choices.GetComponent<ContentSizeFitter>();
        if (content == null) content = choices.gameObject.AddComponent<ContentSizeFitter>();
        content.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        content.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        var group = choices.GetComponent<CanvasGroup>();
        if (group != null) group.ignoreParentGroups = false;
        return choices;
    }

    private void LateUpdate() => RefreshLayout();

    private void RefreshLayout()
    {
        if (line == null || canvasRect == null) return;
        float width = canvasRect.rect.width * 0.34f;
        float preferred = line.lineText != null
            ? line.lineText.GetPreferredValues(line.lineText.text, Mathf.Max(200, width - 64), Mathf.Infinity).y + 114 : 288;
        lineRect.sizeDelta = new Vector2(width, Mathf.Min(Mathf.Max(288, preferred), canvasRect.rect.height * 0.7f));
        foreach (var options in optionsPresenters)
        {
            if (options == null) continue;
            optionItems.Clear();
            options.GetComponentsInChildren(true, optionItems);
            foreach (var item in optionItems)
            {
                // Keep compatibility with scenes whose serialized option target points to a legacy row.
                // Yarn still owns the objects and selection; only their visual parent changes.
                var container = choiceContainers[options];
                if (item.transform.parent != container) item.transform.SetParent(container, false);
                var style = item.GetComponent<InterviewOptionStyle>();
                if (style == null) style = item.gameObject.AddComponent<InterviewOptionStyle>();
                style.Refresh();
            }
        }
    }

    public void SetLineReady(bool ready)
    {
        if (continueIndicator != null) continueIndicator.SetActive(ready);
    }

    public void SetSpeaker(string speaker)
    {
        if (lineRect == null) return;
        speaker = speaker?.Trim() ?? string.Empty;
        bool player = speaker == "나" || speaker == "상담원" || speaker == "플레이어" ||
            speaker == "주인공" || speaker == "담당관";
        lineRect.anchorMin = lineRect.anchorMax = player ? new Vector2(0.06f, 0.08f) : new Vector2(0.94f, 0.86f);
        lineRect.pivot = player ? Vector2.zero : Vector2.one;
        lineRect.anchoredPosition = Vector2.zero;
    }

    private static RectTransform UI(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }

    private static void ResetDepth(RectTransform rect)
    {
        var position = rect.localPosition;
        position.z = 0;
        rect.localPosition = position;
    }
}
