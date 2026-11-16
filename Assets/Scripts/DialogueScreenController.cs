using System.Collections.Generic;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Yarn.Markup;
using Yarn.Unity;

/// <summary>Interview dialogue layout and separate reveal/advance clicks for Yarn.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-9000)]
public sealed class DialogueScreenController : MonoBehaviour, IActionMarkupHandler
{
    private LinePresenter presenter;
    private DialogueRunner runner;
    private Button continueButton;
    private InterviewDialogueLayout interviewLayout;
    private bool lineActive;
    private bool fullyDisplayed;
    private int preparedFrame = -1;
    private int lastClickFrame = -1;
    private readonly List<RaycastResult> hits = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        ConfigureScene();
    }

    private static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
        => ConfigureScene();

    private static void ConfigureScene()
    {
        // Scope the correction to consultation scenes, preserving all other UI.
        if (FindFirstObjectByType<LastLineWatcher>() == null) return;
        foreach (var line in FindObjectsByType<LinePresenter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (line.GetComponent<DialogueScreenController>() == null)
                line.gameObject.AddComponent<DialogueScreenController>();
        }
    }

    private void Awake()
    {
        presenter = GetComponent<LinePresenter>();
        runner = GetComponentInParent<DialogueRunner>();
        if (presenter == null || runner == null || presenter.Typewriter == null) return;

        // The package's default button always requests NextLine, even while typing.
        // Replace that handler through Yarn's public typewriter API, keeping motion/pause handlers.
        var handlers = presenter.Typewriter.ActionMarkupHandlers;
        foreach (var handler in handlers)
        {
            if (handler is LinePresenterButtonHandler original)
            {
                continueButton = original.GetComponentInChildren<Button>(true);
                break;
            }
        }
        handlers.RemoveAll(handler => handler is LinePresenterButtonHandler);
        handlers.Add(this);
        if (continueButton != null) continueButton.onClick.AddListener(ClickCurrentLine);
        interviewLayout = gameObject.AddComponent<InterviewDialogueLayout>();
        interviewLayout.Configure(presenter, runner);
        PerceptionDialoguePresenter.Install(presenter, runner);
    }

    private void Update()
    {
        if (!lineActive || DialogueMenuPause.IsBlockingInput || Mouse.current == null ||
            !Mouse.current.leftButton.wasReleasedThisFrame) return;

        // UI controls (choices, inventory, settings, history) consume their own clicks.
        // The dialogue panel and otherwise empty game background can reveal/advance text.
        if (EventSystem.current != null)
        {
            hits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current)
                { position = Mouse.current.position.ReadValue() }, hits);
            if (hits.Count > 0 && !hits[0].gameObject.transform.IsChildOf(presenter.transform)) return;
        }
        ClickCurrentLine();
    }

    public void ClickCurrentLine()
    {
        if (!lineActive || runner == null || DialogueMenuPause.IsBlockingInput ||
            preparedFrame == Time.frameCount || lastClickFrame == Time.frameCount) return;
        lastClickFrame = Time.frameCount;
        if (fullyDisplayed) runner.RequestNextLine();
        else runner.RequestHurryUpLine();
    }

    public void OnPrepareForLine(MarkupParseResult line, TMP_Text text)
    {
        lineActive = true;
        fullyDisplayed = false;
        preparedFrame = Time.frameCount;
        string speaker = string.Empty;
        if (line.TryGetAttributeWithName("character", out var character) &&
            character.Properties.TryGetValue("name", out var name)) speaker = name.StringValue;
        interviewLayout?.SetSpeaker(speaker);
        interviewLayout?.SetLineReady(false);
        if (continueButton != null)
        {
            continueButton.enabled = true;
            continueButton.interactable = true;
        }
    }

    public void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text) { }

    public YarnTask OnCharacterWillAppear(int currentCharacterIndex, MarkupParseResult line, CancellationToken cancellationToken)
        => YarnTask.CompletedTask;

    public void OnLineDisplayComplete()
    {
        fullyDisplayed = true;
        interviewLayout?.SetLineReady(true);
    }

    public void OnLineWillDismiss()
    {
        lineActive = false;
        fullyDisplayed = false;
        interviewLayout?.SetLineReady(false);
        if (continueButton != null)
        {
            continueButton.interactable = false;
            continueButton.enabled = false;
        }
    }

    public void OnLineDidDismiss() { }

    private void OnDestroy()
    {
        if (continueButton != null) continueButton.onClick.RemoveListener(ClickCurrentLine);
        presenter?.Typewriter?.ActionMarkupHandlers.Remove(this);
    }
}
