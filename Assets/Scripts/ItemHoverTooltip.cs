using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ItemHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    private const float FontSize = 18f;
    private const float HorizontalPadding = 12f;
    private const float VerticalPadding = 8f;
    private const float MaxTextWidth = 220f;

    private static readonly Vector2 CursorOffset = new Vector2(14f, -14f);

    private static RectTransform tooltipRect;
    private static TextMeshProUGUI tooltipText;
    private static Canvas tooltipCanvas;
    private static ItemHoverTooltip currentOwner;

    private string description = string.Empty;

    public void SetDescription(string value)
    {
        description = value ?? string.Empty;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (string.IsNullOrWhiteSpace(description)) return;

        EnsureTooltip();
        if (tooltipRect == null) return;

        currentOwner = this;
        tooltipText.text = description;
        ResizeTooltip();
        tooltipRect.gameObject.SetActive(true);
        tooltipRect.SetAsLastSibling();
        MoveTooltip(eventData.position);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (currentOwner == this)
        {
            MoveTooltip(eventData.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideTooltip();
    }

    private void OnDisable()
    {
        HideTooltip();
    }

    private void EnsureTooltip()
    {
        if (tooltipRect != null) return;

        tooltipCanvas = GetComponentInParent<Canvas>();
        if (tooltipCanvas == null)
        {
            Debug.LogWarning("[ItemHoverTooltip] 아이템의 부모 Canvas를 찾을 수 없습니다.", this);
            return;
        }

        var tooltipObject = new GameObject(
            "Item Tooltip",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(CanvasGroup));

        tooltipRect = tooltipObject.GetComponent<RectTransform>();
        tooltipRect.SetParent(tooltipCanvas.transform, false);
        tooltipRect.anchorMin = new Vector2(0.5f, 0.5f);
        tooltipRect.anchorMax = new Vector2(0.5f, 0.5f);
        tooltipRect.pivot = new Vector2(0f, 1f);

        var background = tooltipObject.GetComponent<Image>();
        background.color = new Color(0.06f, 0.06f, 0.08f, 0.9f);
        background.raycastTarget = false;

        var canvasGroup = tooltipObject.GetComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        var textObject = new GameObject(
            "Description",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI));

        var textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(tooltipRect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(HorizontalPadding, VerticalPadding);
        textRect.offsetMax = new Vector2(-HorizontalPadding, -VerticalPadding);

        tooltipText = textObject.GetComponent<TextMeshProUGUI>();
        tooltipText.fontSize = FontSize;
        tooltipText.color = Color.white;
        tooltipText.alignment = TextAlignmentOptions.TopLeft;
        tooltipText.textWrappingMode = TextWrappingModes.Normal;
        tooltipText.raycastTarget = false;

        tooltipObject.SetActive(false);
    }

    private static void ResizeTooltip()
    {
        var preferred = tooltipText.GetPreferredValues(tooltipText.text, MaxTextWidth, 0f);
        var width = Mathf.Min(MaxTextWidth, Mathf.Max(80f, preferred.x));
        var height = Mathf.Max(FontSize, preferred.y);

        tooltipRect.sizeDelta = new Vector2(
            width + HorizontalPadding * 2f,
            height + VerticalPadding * 2f);
    }

    private static void MoveTooltip(Vector2 screenPosition)
    {
        if (tooltipRect == null || tooltipCanvas == null) return;

        var canvasRect = tooltipCanvas.transform as RectTransform;
        if (canvasRect == null) return;

        var camera = tooltipCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : tooltipCanvas.worldCamera;

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPosition,
                camera,
                out var localPosition))
        {
            return;
        }

        var scaleFactor = Mathf.Max(0.01f, tooltipCanvas.scaleFactor);
        localPosition += CursorOffset / scaleFactor;

        var canvasBounds = canvasRect.rect;
        var size = tooltipRect.sizeDelta;
        localPosition.x = Mathf.Clamp(localPosition.x, canvasBounds.xMin, canvasBounds.xMax - size.x);
        localPosition.y = Mathf.Clamp(localPosition.y, canvasBounds.yMin + size.y, canvasBounds.yMax);

        tooltipRect.anchoredPosition = localPosition;
    }

    private void HideTooltip()
    {
        if (currentOwner != this) return;

        currentOwner = null;
        if (tooltipRect != null)
        {
            tooltipRect.gameObject.SetActive(false);
        }
    }
}
