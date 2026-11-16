using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Yarn.Unity;

/// <summary>Additive toolbar and session history. Does not move or restyle existing scene UI.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-10000)]
public sealed class DialogueHistoryUI : MonoBehaviour
{
    [SerializeField] private Sprite historyIcon;
    [SerializeField] private Sprite settingsIcon;
    [SerializeField] private Sprite playerPortrait;
    [SerializeField] private Sprite otherPortrait;
    [SerializeField] private TMP_FontAsset font;

    private static readonly Color Gold = new Color32(205, 181, 125, 255);
    private static readonly Color Paper = new Color32(231, 226, 214, 255);
    private readonly List<Entry> entries = new();
    private GameObject overlay;
    private GameObject uiRoot;
    private RectTransform toolbar;
    private bool toolbarPressCaptured;
    private RectTransform content;
    private ScrollRect scroll;
    private TMP_Text countText;
    private readonly List<GameObject> rows = new();
    private float lastWidth;

    public static DialogueHistoryUI Instance { get; private set; }
    public bool IsOpen => overlay != null && overlay.activeSelf;
    public int EntryCount => entries.Count;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        CreateForScene();
    }

    private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        => CreateForScene();

    private static void CreateForScene()
    {
        if (Instance != null || FindFirstObjectByType<LastLineWatcher>() == null) return;
        new GameObject("Dialogue History").AddComponent<DialogueHistoryUI>();
    }

    public sealed class Entry
    {
        public string Speaker { get; }
        public string Text { get; }
        public bool IsPlayer { get; }
        public bool IsNarration { get; }
        public Sprite Portrait { get; }

        public Entry(string speaker, string text, bool isPlayer, bool isNarration, Sprite portrait)
        {
            Speaker = speaker;
            Text = text;
            IsPlayer = isPlayer;
            IsNarration = isNarration;
            Portrait = portrait;
        }
    }

    private void Awake()
    {
        Instance = this;
        var assets = Resources.Load<DialogueHistoryAssets>("DialogueHistoryAssets");
        if (assets != null)
        {
            historyIcon = assets.historyIcon;
            settingsIcon = assets.settingsIcon;
            playerPortrait = assets.playerPortrait;
            otherPortrait = assets.otherPortrait;
            font = assets.font;
        }
        if (font == null) font = TMP_Settings.defaultFontAsset;
        BuildUI();
    }

    private void OnDisable() => Close();

    private void OnDestroy()
    {
        DialogueMenuPause.Release(this);
        if (toolbar != null) DialogueMenuPause.Release(toolbar.gameObject);
        if (uiRoot != null) Destroy(uiRoot);
        if (Instance == this) Instance = null;
    }

    private void LateUpdate()
    {
        if (toolbarPressCaptured && (Mouse.current == null || !Mouse.current.leftButton.isPressed))
        {
            toolbarPressCaptured = false;
            DialogueMenuPause.Release(toolbar.gameObject);
        }
        if (IsOpen && Mathf.Abs(scroll.viewport.rect.width - lastWidth) > 1f) RebuildRows(false);
    }

    private void Update()
    {
        // Yarn's global mouse binding fires on press; Button.onClick fires on release.
        // Capture toolbar presses before Yarn Update so opening a menu cannot skip a line.
        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame || IsOpen) return;
        if (!RectTransformUtility.RectangleContainsScreenPoint(toolbar, Mouse.current.position.ReadValue())) return;
        toolbarPressCaptured = true;
        DialogueMenuPause.Acquire(toolbar.gameObject);
    }

    public void RecordLine(LocalizedLine line)
    {
        if (line == null) return;
        var speaker = line.CharacterName?.Trim() ?? string.Empty;
        var text = line.TextWithoutCharacterName.Text;
        if (string.IsNullOrWhiteSpace(text)) return;
        var player = speaker == "나" || speaker == "상담원" || speaker == "플레이어";
        var narration = string.IsNullOrEmpty(speaker);
        // Keep only the line actually delivered by Yarn. #raw metadata and unchosen options are never logged.
        entries.Add(new Entry(speaker, text, player, narration,
            narration ? null : player ? playerPortrait : otherPortrait));
        if (IsOpen) RebuildRows(true);
    }

    public void ClearHistory()
    {
        entries.Clear();
        if (IsOpen) RebuildRows(true);
    }

    public void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (IsOpen) return;
        // Acquire before closing settings so the pause never briefly resumes between menus.
        DialogueMenuPause.Acquire(this);
        var settings = FindFirstObjectByType<GameSettingsMenu>();
        settings?.Close();
        overlay.SetActive(true);
        Canvas.ForceUpdateCanvases();
        RebuildRows(true);
    }

    public void Close()
    {
        if (overlay != null) overlay.SetActive(false);
        DialogueMenuPause.Release(this);
    }

    private void OpenSettings()
    {
        var settings = FindFirstObjectByType<GameSettingsMenu>();
        if (settings != null) settings.Open();
    }

    private void BuildUI()
    {
        // Keep this a root canvas: the watcher is parented under the existing scaled dialogue canvas.
        var canvasRect = UI("Dialogue History Canvas", null);
        uiRoot = canvasRect.gameObject;
        var canvas = canvasRect.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 31000;
        var scaler = canvasRect.gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRect.gameObject.AddComponent<GraphicRaycaster>();

        toolbar = UI("Top Right Toolbar", canvasRect);
        toolbar.anchorMin = toolbar.anchorMax = toolbar.pivot = Vector2.one;
        toolbar.anchoredPosition = new Vector2(-28, -24);
        toolbar.sizeDelta = new Vector2(136, 60);
        IconButton(toolbar, "Dialogue Log", historyIcon, new Vector2(-74, 0), Toggle);
        IconButton(toolbar, "Settings", settingsIcon, Vector2.zero, OpenSettings);

        var shade = UI("Dialogue History Overlay", canvasRect);
        Stretch(shade);
        Image(shade, new Color(0.015f, 0.02f, 0.025f, 0.93f));
        overlay = shade.gameObject;

        var panel = UI("History Panel", shade);
        panel.anchorMin = new Vector2(0.08f, 0.07f);
        panel.anchorMax = new Vector2(0.92f, 0.91f);
        panel.offsetMin = panel.offsetMax = Vector2.zero;
        Image(panel, new Color32(23, 26, 29, 255));
        var topRule = UI("Gold Rule", panel);
        topRule.anchorMin = new Vector2(0, 1);
        topRule.anchorMax = Vector2.one;
        topRule.pivot = new Vector2(0.5f, 1);
        topRule.sizeDelta = new Vector2(0, 3);
        topRule.anchoredPosition = Vector2.zero;
        Image(topRule, Gold, false);

        var title = Label(panel, "대화 기록", 34, Gold, TextAlignmentOptions.Left);
        TopRect(title.rectTransform, new Vector2(32, -24), new Vector2(400, 48));
        countText = Label(panel, string.Empty, 20, new Color32(152, 152, 144, 255), TextAlignmentOptions.Right);
        TopRightRect(countText.rectTransform, new Vector2(-220, -28), new Vector2(250, 40));
        var close = UI("Close History", panel);
        TopRightRect(close, new Vector2(-26, -22), new Vector2(172, 48));
        Image(close, new Color32(48, 51, 53, 255));
        var closeButton = close.gameObject.AddComponent<Button>();
        closeButton.onClick.AddListener(Close);
        var closeLabel = Label(close, "닫기  [ESC]", 22, Paper, TextAlignmentOptions.Center);
        Stretch(closeLabel.rectTransform);

        var scrollArea = UI("History Scroll", panel);
        Stretch(scrollArea);
        scrollArea.offsetMin = new Vector2(28, 62);
        scrollArea.offsetMax = new Vector2(-28, -94);
        scroll = scrollArea.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 55;
        var viewport = UI("Viewport", scrollArea);
        Stretch(viewport);
        viewport.offsetMax = new Vector2(-20, 0);
        viewport.gameObject.AddComponent<RectMask2D>();
        Image(viewport, new Color(1, 1, 1, 0.005f));
        content = UI("Entries", viewport);
        content.anchorMin = new Vector2(0, 1);
        content.anchorMax = Vector2.one;
        content.pivot = new Vector2(0.5f, 1);
        content.anchoredPosition = Vector2.zero;
        content.sizeDelta = Vector2.zero;
        scroll.viewport = viewport;
        scroll.content = content;
        BuildScrollbar(scrollArea);

        var footer = Label(panel, "마우스 휠로 이전 대화를 확인할 수 있습니다.", 19,
            new Color32(139, 141, 139, 255), TextAlignmentOptions.Center);
        footer.rectTransform.anchorMin = Vector2.zero;
        footer.rectTransform.anchorMax = new Vector2(1, 0);
        footer.rectTransform.pivot = new Vector2(0.5f, 0);
        footer.rectTransform.sizeDelta = new Vector2(0, 32);
        footer.rectTransform.anchoredPosition = new Vector2(0, 16);
        overlay.SetActive(false);
    }

    private void RebuildRows(bool latest)
    {
        var position = scroll.verticalNormalizedPosition;
        foreach (var row in rows)
        {
            row.SetActive(false);
            Destroy(row);
        }
        rows.Clear();
        lastWidth = scroll.viewport.rect.width;
        var width = Mathf.Max(320, lastWidth);
        var y = 14f;
        foreach (var entry in entries) y += BuildRow(entry, width, y) + 24;
        if (entries.Count == 0)
        {
            var empty = Label(content, "아직 기록된 대화가 없습니다.", 26,
                new Color32(160, 160, 150, 255), TextAlignmentOptions.Center);
            TopRect(empty.rectTransform, new Vector2(0, -90), new Vector2(width, 70));
            rows.Add(empty.gameObject);
        }
        content.sizeDelta = new Vector2(0, Mathf.Max(y + 12, scroll.viewport.rect.height));
        countText.text = $"{entries.Count}개의 기록";
        Canvas.ForceUpdateCanvases();
        scroll.StopMovement();
        scroll.verticalNormalizedPosition = latest ? 0 : position;
    }

    private float BuildRow(Entry entry, float width, float y)
    {
        var row = UI(entry.IsNarration ? "Narration Entry" : entry.IsPlayer ? "Player Entry" : "Other Entry", content);
        rows.Add(row.gameObject);
        TopRect(row, new Vector2(0, -y), new Vector2(width, 120));
        if (entry.IsNarration)
        {
            var narration = Label(row, entry.Text, 23, new Color32(159, 161, 153, 255), TextAlignmentOptions.Center);
            var narrationWidth = width - 100;
            var h = narration.GetPreferredValues(entry.Text, narrationWidth, Mathf.Infinity).y + 16;
            TopRect(narration.rectTransform, new Vector2(50, -4), new Vector2(narrationWidth, h));
            row.sizeDelta = new Vector2(width, h + 8);
            return h + 8;
        }

        var player = entry.IsPlayer;
        var bubbleWidth = Mathf.Min(width - 174, width * 0.77f);
        var bubbleX = player ? width - 134 - bubbleWidth : 134;
        var frame = UI("Portrait Frame", row);
        TopRect(frame, new Vector2(player ? width - 112 : 0, 0), new Vector2(112, 112));
        Image(frame, player ? new Color32(106, 117, 119, 255) : new Color32(139, 122, 86, 255), false);
        var portraitBack = UI("Portrait Background", frame);
        Stretch(portraitBack);
        portraitBack.offsetMin = new Vector2(3, 3);
        portraitBack.offsetMax = new Vector2(-3, -3);
        Image(portraitBack, new Color32(35, 41, 43, 255), false);
        var portrait = UI("Portrait", portraitBack);
        Stretch(portrait);
        var portraitImage = Image(portrait, Color.white, false);
        portraitImage.sprite = entry.Portrait;
        portraitImage.preserveAspect = true;
        if (entry.Portrait == null) portraitImage.enabled = false;

        var name = Label(row, entry.Speaker, 24, player ? new Color32(169, 195, 198, 255) : Gold,
            player ? TextAlignmentOptions.Right : TextAlignmentOptions.Left);
        TopRect(name.rectTransform, new Vector2(bubbleX, 0), new Vector2(bubbleWidth, 34));
        var bubble = UI("Dialogue", row);
        var bubbleColor = player ? new Color32(39, 49, 53, 255) : new Color32(46, 44, 38, 255);
        Image(bubble, bubbleColor, false);
        var body = Label(bubble, entry.Text, 27, Paper, TextAlignmentOptions.TopLeft);
        var bodyHeight = body.GetPreferredValues(entry.Text, bubbleWidth - 40, Mathf.Infinity).y;
        var bubbleHeight = Mathf.Max(66, bodyHeight + 32);
        TopRect(bubble, new Vector2(bubbleX, -42), new Vector2(bubbleWidth, bubbleHeight));
        TopRect(body.rectTransform, new Vector2(20, -16), new Vector2(bubbleWidth - 40, bubbleHeight - 32));
        var tail = UI(player ? "Right Facing Tail" : "Left Facing Tail", row);
        TopRect(tail, new Vector2(player ? bubbleX + bubbleWidth : bubbleX - 12, -55), new Vector2(12, 22));
        var graphic = tail.gameObject.AddComponent<DialogueHistoryTail>();
        graphic.PointRight = player;
        graphic.color = bubbleColor;
        graphic.raycastTarget = false;
        var height = Mathf.Max(112, bubbleHeight + 42);
        row.sizeDelta = new Vector2(width, height);
        return height;
    }

    private void IconButton(Transform parent, string name, Sprite sprite, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        var rect = UI(name, parent);
        TopRightRect(rect, position, new Vector2(60, 60));
        var button = rect.gameObject.AddComponent<Button>();
        // The original sprite is the only visible graphic; keep its transparent silhouette.
        button.transition = Selectable.Transition.None;
        var colors = button.colors;
        colors.highlightedColor = new Color32(217, 201, 154, 255);
        colors.pressedColor = new Color32(155, 141, 110, 255);
        button.colors = colors;
        button.onClick.AddListener(action);
        var icon = UI("Icon", rect);
        Stretch(icon);
        icon.offsetMin = new Vector2(8, 8);
        icon.offsetMax = new Vector2(-8, -8);
        var image = Image(icon, Color.white, false);
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = true;
        button.targetGraphic = image;
    }

    private void BuildScrollbar(RectTransform parent)
    {
        var track = UI("Scrollbar", parent);
        track.anchorMin = new Vector2(1, 0);
        track.anchorMax = Vector2.one;
        track.pivot = Vector2.one;
        track.sizeDelta = new Vector2(7, 0);
        track.anchoredPosition = Vector2.zero;
        Image(track, new Color32(48, 50, 49, 255));
        var handle = UI("Handle", track);
        Stretch(handle);
        var handleImage = Image(handle, new Color32(145, 132, 100, 255));
        var bar = track.gameObject.AddComponent<Scrollbar>();
        bar.handleRect = handle;
        bar.targetGraphic = handleImage;
        bar.direction = Scrollbar.Direction.BottomToTop;
        scroll.verticalScrollbar = bar;
    }

    private TMP_Text Label(Transform parent, string value, float size, Color color, TextAlignmentOptions alignment)
    {
        var rect = UI("Text", parent);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.text = value;
        text.color = color;
        text.alignment = alignment;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private static RectTransform UI(string name, Transform parent)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    private static Image Image(RectTransform rect, Color color, bool raycast = true)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycast;
        return image;
    }

    private static void TopRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void TopRightRect(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
