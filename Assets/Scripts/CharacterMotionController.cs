using System;
using DG.Tweening;
using UnityEngine;
using Yarn.Unity;

/// <summary>One owner for the character's position. Motion values are pixels at 1080p.</summary>
[DisallowMultipleComponent]
public sealed class CharacterMotionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform motionTarget;
    [SerializeField] private Camera motionCamera;
    [SerializeField] private FilterManager filterManager;
    [SerializeField] private DialogueRunner dialogueRunner;

    [Header("Dialogue")]
    [SerializeField] private string[] speakerNames = { "크투 양", "크투", "크툴루" };
    // New opt-in setting: do not inherit the old automaticTalk=true value
    // from a scene or a Play-mode hot reload after motion tags are removed.
    [SerializeField, Tooltip("Animate untagged dialogue too. Off by default.")]
    private bool talkOnUntaggedLines = false;

    [Header("Motion size (pixels at 1080p)")]
    [SerializeField, Min(0f)] private float talkHeight = 6f;
    [SerializeField, Min(0f)] private float happyHeight = 18f;
    [SerializeField, Min(0f)] private float nodDepth = 7f;
    [SerializeField, Min(0f)] private float surpriseHeight = 14f;
    [SerializeField, Min(0f)] private float angryWidth = 6f;
    [SerializeField, Min(0f)] private float sadDepth = 14f;
    [SerializeField, Min(0f)] private float romanceWidth = 7f;
    [SerializeField] private bool snapToScreenPixels = true;

    private Vector3 homeLocalPosition;
    private Vector2 pixelOffset;
    private Tween activeTween;
    private string activeMotion;
    private string queuedMotion;
    private string lineMotion;
    private bool initialized;
    private bool speakingLine;
    private bool displayingText;
    private bool suppressLine;
    private bool talkRequestedForLine;
    private float resumeTalkAt;

    public string ActiveMotion => activeMotion;
    public Vector2 PixelOffset => pixelOffset;

    public static bool IsMotionName(string name)
    {
        switch (name)
        {
            case "talk": case "happy": case "nod": case "surprise":
            case "angry": case "sad": case "romance": case "still":
                return true;
            default:
                return false;
        }
    }

    private void Awake()
    {
        if (motionTarget == null)
        {
            Debug.LogError("[CharacterMotion] Assign the character Transform.", this);
            enabled = false;
            return;
        }

        if (motionCamera == null) motionCamera = Camera.main;
        homeLocalPosition = motionTarget.localPosition;
        initialized = true;
    }

    private void OnEnable()
    {
        if (filterManager != null) filterManager.FilterChanged += OnFilterChanged;
        if (dialogueRunner != null) dialogueRunner.AddCommandHandler<string>("motion", QueueMotion);
    }

    private void OnDisable()
    {
        if (filterManager != null) filterManager.FilterChanged -= OnFilterChanged;
        if (dialogueRunner != null) dialogueRunner.RemoveCommandHandler("motion");
        queuedMotion = null;
        CancelLine();
    }

    private void Update()
    {
        if (initialized && (motionTarget == null || !motionTarget.gameObject.activeInHierarchy))
        {
            CancelLine();
            return;
        }
        if (CanAnimate && displayingText && activeTween == null &&
            Time.time >= resumeTalkAt && talkRequestedForLine)
        {
            PlayMotion("talk");
        }
    }

    private bool CanAnimate => initialized && isActiveAndEnabled && speakingLine &&
        !suppressLine && motionTarget != null && motionTarget.gameObject.activeInHierarchy &&
        (filterManager == null || filterManager.FilterOn);

    public void PrepareLine(string speaker, string text)
    {
        lineMotion = queuedMotion;
        queuedMotion = null;
        CancelLine();

        // Whitespace in character names should not change who owns a line.
        string normalized = (speaker ?? string.Empty).Replace(" ", string.Empty).Trim();
        speakingLine = Array.Exists(speakerNames, name =>
            string.Equals((name ?? string.Empty).Replace(" ", string.Empty).Trim(),
                normalized, StringComparison.Ordinal));

        // Silence and punctuation-only lines are still poses, not speech.
        bool containsSpeech = false;
        foreach (char c in text ?? string.Empty)
        {
            if (char.IsLetterOrDigit(c)) { containsSpeech = true; break; }
        }
        speakingLine &= containsSpeech;
    }

    public void BeginDisplaying()
    {
        displayingText = true;
        resumeTalkAt = Time.time;
        talkRequestedForLine = talkOnUntaggedLines;
        if (!string.IsNullOrEmpty(lineMotion)) PlayMotion(lineMotion);
        else if (talkRequestedForLine) PlayMotion("talk");
        lineMotion = null;
    }

    public void ObserveCharacter(char character)
    {
        if (!CanAnimate || !displayingText) return;
        if (character == '.' || character == '…' || character == ',' ||
            character == '!' || character == '?' || character == '\n')
        {
            resumeTalkAt = Time.time + (character == ',' ? 0.12f : 0.22f);
            if (activeMotion == "talk") Settle();
        }
    }

    public void FinishDisplaying()
    {
        displayingText = false;
        // A short line may finish typing before its reaction finishes. Let a
        // finite reaction land, but stop the looping speech movement now.
        if (activeMotion == "talk") Settle();
    }

    public void CancelLine()
    {
        displayingText = false;
        speakingLine = false;
        suppressLine = false;
        talkRequestedForLine = false;
        resumeTalkAt = 0f;
        StopTween();
        ApplyOffset(Vector2.zero);
    }

    public void ResetDialogue()
    {
        queuedMotion = null;
        lineMotion = null;
        CancelLine();
    }

    private void OnFilterChanged(bool filterOn)
    {
        StopTween();
        ApplyOffset(Vector2.zero);
        // Do not replay a reaction after peeking and returning to the same line.
        suppressLine = true;
        talkRequestedForLine = false;
    }

    /// <summary>Used by &lt;&lt;motion happy&gt;&gt;; consumed by the next line.</summary>
    public void QueueMotion(string name)
    {
        name = (name ?? string.Empty).Trim().ToLowerInvariant();
        if (!IsMotionName(name))
        {
            Debug.LogWarning($"[CharacterMotion] Unknown motion '{name}'.", this);
            return;
        }
        queuedMotion = name;
    }

    public void PlayMotion(string name)
    {
        if (!IsMotionName(name) || !CanAnimate) return;
        if (name == "still")
        {
            StopTween();
            ApplyOffset(Vector2.zero);
            suppressLine = true;
            talkRequestedForLine = false;
            return;
        }
        if (name == "talk") talkRequestedForLine = true;
        // Speech must not interrupt an emotional reaction already in flight.
        if (name == "talk" && activeTween != null) return;

        StopTween();
        ApplyOffset(Vector2.zero);
        activeMotion = name;
        Sequence sequence = DOTween.Sequence();
        sequence.SetTarget(this);
        sequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);

        switch (name)
        {
            case "talk":
                Move(sequence, 0f, talkHeight, 0.14f);
                Move(sequence, 0f, 0f, 0.18f);
                Move(sequence, 0f, talkHeight * 0.65f, 0.13f);
                Move(sequence, 0f, 0f, 0.20f);
                sequence.AppendInterval(0.25f).SetLoops(-1);
                break;
            case "happy":
                Move(sequence, 0f, -2f, 0.08f);
                Move(sequence, 0f, happyHeight, 0.16f, Ease.OutQuad);
                Move(sequence, 0f, 0f, 0.18f, Ease.InQuad);
                Move(sequence, 0f, happyHeight * 0.6f, 0.13f, Ease.OutQuad);
                Move(sequence, 0f, 0f, 0.18f, Ease.InQuad);
                break;
            case "nod":
                Move(sequence, 0f, -nodDepth, 0.13f);
                Move(sequence, 0f, 0f, 0.19f);
                break;
            case "surprise":
                Move(sequence, 0f, surpriseHeight, 0.09f, Ease.OutQuad);
                sequence.AppendInterval(0.08f);
                Move(sequence, 0f, 0f, 0.30f);
                break;
            case "angry":
                // Deterministic X-only shake; keep the elbow near the desk.
                Move(sequence, -angryWidth, 0f, 0.055f, Ease.Linear);
                Move(sequence, angryWidth, 0f, 0.075f, Ease.Linear);
                Move(sequence, -angryWidth * 0.7f, 0f, 0.075f, Ease.Linear);
                Move(sequence, angryWidth * 0.45f, 0f, 0.075f, Ease.Linear);
                Move(sequence, 0f, 0f, 0.12f);
                break;
            case "sad":
                Move(sequence, 0f, -sadDepth, 0.55f);
                sequence.AppendInterval(0.7f);
                Move(sequence, 0f, 0f, 0.5f);
                break;
            case "romance":
                Move(sequence, -romanceWidth, -3f, 0.45f);
                Move(sequence, romanceWidth * 0.5f, -2f, 0.55f);
                Move(sequence, -romanceWidth * 0.25f, -1f, 0.45f);
                Move(sequence, 0f, 0f, 0.45f);
                break;
        }

        activeTween = sequence;
        if (name != "talk")
        {
            sequence.OnComplete(() =>
            {
                if (activeTween != sequence) return;
                activeTween = null;
                activeMotion = null;
                ApplyOffset(Vector2.zero);
                // Give the reaction a little space before normal speech resumes.
                resumeTalkAt = Mathf.Max(resumeTalkAt, Time.time + 0.15f);
            });
        }
    }

    private void Move(Sequence sequence, float x, float y, float duration,
        Ease ease = Ease.InOutSine)
    {
        sequence.Append(DOTween.To(() => pixelOffset, ApplyOffset,
            new Vector2(x, y), duration).SetEase(ease));
    }

    private void Settle()
    {
        StopTween();
        if (!initialized || motionTarget == null) return;
        Tween settle = DOTween.To(() => pixelOffset, ApplyOffset, Vector2.zero, 0.12f)
            .SetEase(Ease.OutSine).SetTarget(this)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        activeTween = settle;
        settle.OnComplete(() =>
        {
            if (activeTween != settle) return;
            activeTween = null;
            ApplyOffset(Vector2.zero);
        });
    }

    private void StopTween()
    {
        Tween previous = activeTween;
        activeTween = null;
        activeMotion = null;
        if (previous != null && previous.IsActive()) previous.Kill(false);
    }

    private void ApplyOffset(Vector2 offset)
    {
        pixelOffset = offset;
        if (!initialized || motionTarget == null) return;
        if (offset == Vector2.zero)
        {
            motionTarget.localPosition = homeLocalPosition;
            return;
        }

        Transform parent = motionTarget.parent;
        Vector3 homeWorld = parent != null ? parent.TransformPoint(homeLocalPosition) : homeLocalPosition;
        if (motionCamera == null || motionCamera.pixelHeight <= 0)
        {
            motionTarget.localPosition = homeLocalPosition + new Vector3(offset.x, offset.y, 0f) / 108f;
            return;
        }

        Vector3 screen = motionCamera.WorldToScreenPoint(homeWorld);
        Vector2 scaled = offset * (motionCamera.pixelHeight / 1080f);
        if (snapToScreenPixels)
        {
            scaled.x = Mathf.Round(scaled.x);
            scaled.y = Mathf.Round(scaled.y);
        }
        Vector3 displaced = motionCamera.ScreenToWorldPoint(screen + new Vector3(scaled.x, scaled.y, 0f));
        motionTarget.localPosition = parent != null ? parent.InverseTransformPoint(displaced) : displaced;
    }
}
