using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;

/// <summary>Supplies raw metadata before LinePresenter runs and switches the active sentence on TAB.</summary>
[DisallowMultipleComponent]
public sealed class PerceptionDialoguePresenter : DialoguePresenterBase
{
    private LinePresenter linePresenter;
    private DialogueRunner runner;
    private FilterManager filter;
    private FilterableTypewriter typewriter;
    private LocalizedLine currentLine;

    public static void Install(LinePresenter line, DialogueRunner runner)
    {
        if (line.GetComponent<PerceptionDialoguePresenter>() != null) return;
        var instance = line.gameObject.AddComponent<PerceptionDialoguePresenter>();
        instance.linePresenter = line;
        instance.runner = runner;
        instance.typewriter = new FilterableTypewriter(line.Typewriter, instance);
        line.Typewriter = instance.typewriter;
        instance.filter = FilterManager.Instance;
        if (instance.filter != null) instance.filter.FilterChanged += instance.OnFilterChanged;

        // Yarn invokes presenters in list order. Capture LocalizedLine metadata before the line view prepares its typewriter.
        var presenters = runner.DialoguePresenters.ToList();
        presenters.Insert(0, instance);
        runner.DialoguePresenters = presenters;
    }

    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken cancellationToken)
    {
        currentLine = line;
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueStartedAsync()
    {
        currentLine = null;
        typewriter?.Reset();
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        currentLine = null;
        typewriter?.Reset();
        return YarnTask.CompletedTask;
    }

    private bool FilterOn => filter == null || filter.FilterOn;

    private void OnFilterChanged(bool filterOn)
    {
        if (typewriter == null || !typewriter.Active || !typewriter.HasRaw) return;
        if (typewriter.FullyDisplayed) typewriter.RefreshVisibleText();
        else
        {
            // Finish this sentence before replacing it: different raw/normal lengths must not
            // leave half a sentence visible or let the original typewriter overwrite the replacement.
            runner.RequestHurryUpLine();
        }
    }

    private void OnDestroy()
    {
        if (filter != null) filter.FilterChanged -= OnFilterChanged;
        if (linePresenter != null && ReferenceEquals(linePresenter.Typewriter, typewriter))
            linePresenter.Typewriter = typewriter.Original;
    }

    private sealed class FilterableTypewriter : IAsyncTypewriter
    {
        public IAsyncTypewriter Original { get; }
        private readonly PerceptionDialoguePresenter owner;
        private MarkupParseResult normal;
        private MarkupParseResult raw;
        private MarkupParseResult prepared;
        public bool HasRaw { get; private set; }
        public bool Active { get; private set; }
        public bool FullyDisplayed { get; private set; }

        public TMP_Text TextElement { get => Original.TextElement; set => Original.TextElement = value; }
        public List<IActionMarkupHandler> ActionMarkupHandlers => Original.ActionMarkupHandlers;

        public FilterableTypewriter(IAsyncTypewriter original, PerceptionDialoguePresenter owner)
        {
            Original = original;
            this.owner = owner;
        }

        public void PrepareForContent(MarkupParseResult line)
        {
            normal = line;
            HasRaw = PerceptionText.TryGetRaw(owner.currentLine, out var rawText);
            if (HasRaw)
            {
                using (var parser = new LineParser())
                    raw = parser.ParseString(PerceptionText.Colorize(rawText), "ko", true);
                // The alternate is body text, not a new speaker. Preserve character metadata for existing handlers.
                raw.Attributes.RemoveAll(attribute => attribute.Name == "character");
                if (normal.TryGetAttributeWithName("character", out var character)) raw.Attributes.Add(character);
            }
            prepared = Selected;
            Active = true;
            FullyDisplayed = false;
            Original.PrepareForContent(prepared);
        }

        public async YarnTask RunTypewriter(MarkupParseResult line, CancellationToken cancellationToken)
        {
            await Original.RunTypewriter(prepared, cancellationToken);
            FullyDisplayed = true;
            if (Active && HasRaw) RefreshVisibleText();
        }

        private MarkupParseResult Selected => HasRaw && !owner.FilterOn ? raw : normal;

        public void RefreshVisibleText()
        {
            if (!Active || TextElement == null) return;
            var selected = Selected;
            TextElement.text = selected.Text;
            TextElement.maxVisibleCharacters = TextElement.GetTextInfo(selected.Text).characterCount;
        }

        public void ContentWillDismiss()
        {
            Active = false;
            Original.ContentWillDismiss();
        }

        public void ContentDidDismiss()
        {
            Original.ContentDidDismiss();
            Reset();
        }

        public void Reset()
        {
            Active = false;
            FullyDisplayed = false;
            HasRaw = false;
        }
    }
}
