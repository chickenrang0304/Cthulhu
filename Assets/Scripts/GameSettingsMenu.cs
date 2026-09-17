using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Persistent, scene-independent settings menu. Press Escape to open/close it.
/// The UI is generated at runtime so every scene shares exactly the same menu.
/// </summary>
[DisallowMultipleComponent]
public sealed class GameSettingsMenu : MonoBehaviour
{
    private const string MasterVolumeKey = "Settings.MasterVolume";
    private const string BgmVolumeKey = "Settings.BGMVolume";
    private const string SfxVolumeKey = "Settings.SFXVolume";
    private const string FullscreenKey = "Settings.Fullscreen";

    private static readonly Color PanelColor = new Color32(17, 22, 29, 248);
    private static readonly Color AccentColor = new Color32(91, 207, 183, 255);
    private static readonly Color ButtonColor = new Color32(43, 54, 67, 255);
    private readonly Dictionary<AudioSource, SourceVolumeInfo> sourceVolumes = new();

    private GameObject menuRoot;
    private TMP_FontAsset menuFont;
    private Slider masterSlider;
    private Slider bgmSlider;
    private Slider sfxSlider;
    private TMP_Text masterValue;
    private TMP_Text bgmValue;
    private TMP_Text sfxValue;
    private Button fullscreenButton;
    private Button windowedButton;
    private GameObject quitConfirmationRoot;
    private bool isOpen;
    private float previousTimeScale = 1f;
    private float nextAudioScanTime;

    private float MasterVolume { get; set; }
    private float BgmVolume { get; set; }
    private float SfxVolume { get; set; }
    private bool IsFullscreen { get; set; }

    private readonly struct SourceVolumeInfo
    {
        public SourceVolumeInfo(float originalVolume, GameAudioChannelType channel)
        {
            OriginalVolume = originalVolume;
            Channel = channel;
        }

        public float OriginalVolume { get; }
        public GameAudioChannelType Channel { get; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateInstance()
    {
        if (FindFirstObjectByType<GameSettingsMenu>() != null) return;

        var settingsObject = new GameObject("Game Settings Menu");
        settingsObject.AddComponent<GameSettingsMenu>();
        DontDestroyOnLoad(settingsObject);
    }

    private void Awake()
    {
        MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
        BgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumeKey, 1f));
        SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));
        IsFullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;

        menuFont = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
            .FirstOrDefault(font => font.name == "Galmuri11 SDF") ?? TMP_Settings.defaultFontAsset;

        BuildMenu();
        ApplyDisplayMode(IsFullscreen, false);
        DiscoverAudioSources();
        ApplyAudioVolumes();

        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        if (isOpen) Time.timeScale = previousTimeScale;
    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.Save();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (quitConfirmationRoot != null && quitConfirmationRoot.activeSelf)
            {
                quitConfirmationRoot.SetActive(false);
            }
            else
            {
                SetMenuOpen(!isOpen);
            }
        }

        if (Time.unscaledTime >= nextAudioScanTime)
        {
            nextAudioScanTime = Time.unscaledTime + 1f;
            if (DiscoverAudioSources()) ApplyAudioVolumes();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureEventSystem();
        DiscoverAudioSources();
        ApplyAudioVolumes();

        if (isOpen) SetMenuOpen(false);
    }

    private void BuildMenu()
    {
        var canvasObject = new GameObject("Settings Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        menuRoot = CreateUiObject("Settings Overlay", canvasObject.transform);
        Stretch(menuRoot.GetComponent<RectTransform>());
        var dimmer = menuRoot.AddComponent<Image>();
        dimmer.color = new Color(0f, 0f, 0f, 0.68f);

        var panel = CreateUiObject("Settings Panel", menuRoot.transform);
        SetRect(panel.GetComponent<RectTransform>(), new Vector2(680f, 600f), Vector2.zero);
        panel.AddComponent<Image>().color = PanelColor;

        CreateText(panel.transform, "설정", 38f, FontStyles.Bold, TextAlignmentOptions.Center,
            new Vector2(0f, 246f), new Vector2(560f, 54f));

        var closeButton = CreateButton(panel.transform, "닫기  [ESC]", new Vector2(-115f, -252f), new Vector2(210f, 54f));
        closeButton.onClick.AddListener(() => SetMenuOpen(false));

        var quitButton = CreateButton(panel.transform, "게임 나가기", new Vector2(115f, -252f), new Vector2(210f, 54f));
        var quitColors = quitButton.colors;
        quitColors.normalColor = new Color32(113, 48, 55, 255);
        quitColors.highlightedColor = new Color32(143, 59, 67, 255);
        quitColors.pressedColor = new Color32(86, 35, 41, 255);
        quitButton.colors = quitColors;
        quitButton.image.color = quitColors.normalColor;
        quitButton.onClick.AddListener(() => quitConfirmationRoot.SetActive(true));

        CreateText(panel.transform, "화면 모드", 23f, FontStyles.Bold, TextAlignmentOptions.Left,
            new Vector2(-210f, 165f), new Vector2(180f, 40f));

        fullscreenButton = CreateButton(panel.transform, "전체 화면", new Vector2(70f, 165f), new Vector2(180f, 46f));
        fullscreenButton.onClick.AddListener(() => ApplyDisplayMode(true, true));
        windowedButton = CreateButton(panel.transform, "창 모드", new Vector2(260f, 165f), new Vector2(160f, 46f));
        windowedButton.onClick.AddListener(() => ApplyDisplayMode(false, true));

        masterSlider = CreateVolumeRow(panel.transform, "마스터 볼륨", 82f, out masterValue);
        bgmSlider = CreateVolumeRow(panel.transform, "BGM 볼륨", 6f, out bgmValue);
        sfxSlider = CreateVolumeRow(panel.transform, "SFX 볼륨", -70f, out sfxValue);

        masterSlider.SetValueWithoutNotify(MasterVolume);
        bgmSlider.SetValueWithoutNotify(BgmVolume);
        sfxSlider.SetValueWithoutNotify(SfxVolume);
        RefreshValueLabels();
        RefreshDisplayButtons();

        masterSlider.onValueChanged.AddListener(value =>
        {
            MasterVolume = value;
            PlayerPrefs.SetFloat(MasterVolumeKey, value);
            AudioListener.volume = value;
            RefreshValueLabels();
        });

        bgmSlider.onValueChanged.AddListener(value =>
        {
            BgmVolume = value;
            PlayerPrefs.SetFloat(BgmVolumeKey, value);
            ApplyAudioVolumes();
            RefreshValueLabels();
        });

        sfxSlider.onValueChanged.AddListener(value =>
        {
            SfxVolume = value;
            PlayerPrefs.SetFloat(SfxVolumeKey, value);
            ApplyAudioVolumes();
            RefreshValueLabels();
        });

        BuildQuitConfirmation(menuRoot.transform);

        menuRoot.SetActive(false);
    }

    private void BuildQuitConfirmation(Transform parent)
    {
        quitConfirmationRoot = CreateUiObject("Quit Confirmation", parent);
        Stretch(quitConfirmationRoot.GetComponent<RectTransform>());
        quitConfirmationRoot.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

        var confirmationPanel = CreateUiObject("Confirmation Panel", quitConfirmationRoot.transform);
        SetRect(confirmationPanel.GetComponent<RectTransform>(), new Vector2(520f, 250f), Vector2.zero);
        confirmationPanel.AddComponent<Image>().color = PanelColor;

        CreateText(confirmationPanel.transform, "게임을 종료할까요?", 28f, FontStyles.Bold,
            TextAlignmentOptions.Center, new Vector2(0f, 55f), new Vector2(440f, 54f));

        var cancelButton = CreateButton(confirmationPanel.transform, "취소", new Vector2(-112f, -55f), new Vector2(190f, 52f));
        cancelButton.onClick.AddListener(() => quitConfirmationRoot.SetActive(false));

        var confirmButton = CreateButton(confirmationPanel.transform, "게임 종료", new Vector2(112f, -55f), new Vector2(190f, 52f));
        var confirmColors = confirmButton.colors;
        confirmColors.normalColor = new Color32(135, 48, 55, 255);
        confirmColors.highlightedColor = new Color32(165, 60, 69, 255);
        confirmColors.pressedColor = new Color32(96, 34, 40, 255);
        confirmButton.colors = confirmColors;
        confirmButton.image.color = confirmColors.normalColor;
        confirmButton.onClick.AddListener(QuitGame);

        quitConfirmationRoot.SetActive(false);
    }

    private Slider CreateVolumeRow(Transform parent, string label, float y, out TMP_Text valueText)
    {
        CreateText(parent, label, 22f, FontStyles.Normal, TextAlignmentOptions.Left,
            new Vector2(-200f, y), new Vector2(200f, 38f));

        valueText = CreateText(parent, "100%", 20f, FontStyles.Normal, TextAlignmentOptions.Right,
            new Vector2(257f, y), new Vector2(82f, 38f));

        var sliderObject = CreateUiObject(label + " Slider", parent);
        SetRect(sliderObject.GetComponent<RectTransform>(), new Vector2(330f, 34f), new Vector2(70f, y));
        var slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.direction = Slider.Direction.LeftToRight;

        var background = CreateUiObject("Background", sliderObject.transform);
        var backgroundRect = background.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 0.35f);
        backgroundRect.anchorMax = new Vector2(1f, 0.65f);
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;
        background.AddComponent<Image>().color = new Color32(52, 65, 78, 255);

        var fillArea = CreateUiObject("Fill Area", sliderObject.transform);
        var fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.35f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.65f);
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        var fill = CreateUiObject("Fill", fillArea.transform);
        Stretch(fill.GetComponent<RectTransform>());
        fill.AddComponent<Image>().color = AccentColor;
        slider.fillRect = fill.GetComponent<RectTransform>();

        var handleArea = CreateUiObject("Handle Slide Area", sliderObject.transform);
        Stretch(handleArea.GetComponent<RectTransform>());
        var handle = CreateUiObject("Handle", handleArea.transform);
        var handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(14f, 22f);
        var handleImage = handle.AddComponent<Image>();
        handleImage.color = Color.white;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;

        return slider;
    }

    private Button CreateButton(Transform parent, string text, Vector2 position, Vector2 size)
    {
        var buttonObject = CreateUiObject(text + " Button", parent);
        SetRect(buttonObject.GetComponent<RectTransform>(), size, position);
        var image = buttonObject.AddComponent<Image>();
        image.color = ButtonColor;
        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        var colors = button.colors;
        colors.normalColor = ButtonColor;
        colors.highlightedColor = new Color32(64, 80, 94, 255);
        colors.pressedColor = new Color32(31, 40, 50, 255);
        colors.selectedColor = AccentColor;
        button.colors = colors;

        var label = CreateText(buttonObject.transform, text, 20f, FontStyles.Normal, TextAlignmentOptions.Center,
            Vector2.zero, size);
        Stretch(label.rectTransform);
        return button;
    }

    private TMP_Text CreateText(Transform parent, string value, float fontSize, FontStyles style,
        TextAlignmentOptions alignment, Vector2 position, Vector2 size)
    {
        var textObject = CreateUiObject(value + " Text", parent);
        SetRect(textObject.GetComponent<RectTransform>(), size, position);
        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.font = menuFont;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private void SetMenuOpen(bool open)
    {
        if (isOpen == open) return;

        isOpen = open;
        if (open)
        {
            EnsureEventSystem();
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            menuRoot.SetActive(true);
            masterSlider.SetValueWithoutNotify(MasterVolume);
            bgmSlider.SetValueWithoutNotify(BgmVolume);
            sfxSlider.SetValueWithoutNotify(SfxVolume);
            RefreshValueLabels();
            RefreshDisplayButtons();
        }
        else
        {
            if (quitConfirmationRoot != null) quitConfirmationRoot.SetActive(false);
            menuRoot.SetActive(false);
            Time.timeScale = previousTimeScale;
            PlayerPrefs.Save();
        }
    }

    private static void QuitGame()
    {
        PlayerPrefs.Save();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void ApplyDisplayMode(bool fullscreen, bool save)
    {
        IsFullscreen = fullscreen;

        if (fullscreen)
        {
            var resolution = Screen.currentResolution;
            Screen.SetResolution(resolution.width, resolution.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            var displayWidth = Display.main.systemWidth;
            var displayHeight = Display.main.systemHeight;
            var width = Mathf.Min(1280, Mathf.Max(960, displayWidth - 160));
            var height = Mathf.Min(720, Mathf.Max(540, displayHeight - 120));
            Screen.SetResolution(width, height, FullScreenMode.Windowed);
        }

        if (save)
        {
            PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
            PlayerPrefs.Save();
        }

        RefreshDisplayButtons();
    }

    private void RefreshDisplayButtons()
    {
        if (fullscreenButton == null || windowedButton == null) return;
        fullscreenButton.image.color = IsFullscreen ? AccentColor : ButtonColor;
        windowedButton.image.color = IsFullscreen ? ButtonColor : AccentColor;
    }

    private void RefreshValueLabels()
    {
        if (masterValue != null) masterValue.text = $"{Mathf.RoundToInt(MasterVolume * 100f)}%";
        if (bgmValue != null) bgmValue.text = $"{Mathf.RoundToInt(BgmVolume * 100f)}%";
        if (sfxValue != null) sfxValue.text = $"{Mathf.RoundToInt(SfxVolume * 100f)}%";
    }

    private bool DiscoverAudioSources()
    {
        var changed = false;
        var destroyedSources = sourceVolumes.Keys.Where(source => source == null).ToArray();
        foreach (var source in destroyedSources) sourceVolumes.Remove(source);

        var sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var source in sources)
        {
            if (sourceVolumes.ContainsKey(source)) continue;
            sourceVolumes[source] = new SourceVolumeInfo(source.volume, Classify(source));
            changed = true;
        }

        return changed;
    }

    private static GameAudioChannelType Classify(AudioSource source)
    {
        var explicitChannel = source.GetComponent<GameAudioChannel>();
        if (explicitChannel != null) return explicitChannel.Channel;

        var clipName = source.clip != null ? source.clip.name : string.Empty;
        var searchableName = (source.gameObject.name + " " + clipName).ToLowerInvariant();
        var isBgm = source.loop
                    || searchableName.Contains("bgm")
                    || searchableName.Contains("music")
                    || searchableName.Contains("theme")
                    || searchableName.Contains("ambient");
        return isBgm ? GameAudioChannelType.BGM : GameAudioChannelType.SFX;
    }

    private void ApplyAudioVolumes()
    {
        AudioListener.volume = MasterVolume;
        foreach (var pair in sourceVolumes)
        {
            if (pair.Key == null) continue;
            var channelVolume = pair.Value.Channel == GameAudioChannelType.BGM ? BgmVolume : SfxVolume;
            pair.Key.volume = pair.Value.OriginalVolume * channelVolume;
        }
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;

        var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        eventSystemObject.transform.SetAsLastSibling();
    }

    private static GameObject CreateUiObject(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static void SetRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
