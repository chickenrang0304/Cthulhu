using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TMPro;
using UnityEngine;
using Yarn.Markup;
using Yarn.Unity;

/// <summary>Yarn 3 inline markers such as [happy/] and [romance/].</summary>
[DisallowMultipleComponent]
public sealed class CharacterMotionMarkup : ActionMarkupHandler
{
    [SerializeField] private CharacterMotionController motionController;

    private readonly List<MarkupAttribute> markers = new List<MarkupAttribute>();
    private int nextMarker;
    private string lineText;
    private int[] sourceIndices;
    private CancellationToken hurryToken;
    private bool canceled;

    public override void OnPrepareForLine(MarkupParseResult line, TMP_Text text)
    {
        markers.Clear();
        nextMarker = 0;
        canceled = false;
        hurryToken = default;
        lineText = line.Text ?? string.Empty;

        string speaker = string.Empty;
        if (line.TryGetAttributeWithName("character", out var character) &&
            character.Properties.TryGetValue("name", out var value))
        {
            speaker = value.StringValue;
        }

        markers.AddRange(line.Attributes
            .Where(attribute => CharacterMotionController.IsMotionName(attribute.Name))
            .OrderBy(attribute => attribute.Position));
        motionController?.PrepareLine(speaker, lineText);
    }

    public override void OnLineDisplayBegin(MarkupParseResult line, TMP_Text text)
    {
        // TMP's character index excludes rich-text tags. Yarn marker positions
        // are source-string offsets, so map between the two before dispatching.
        TMP_TextInfo info = text.GetTextInfo(lineText);
        sourceIndices = new int[info.characterCount];
        for (int i = 0; i < info.characterCount; i++) sourceIndices[i] = info.characterInfo[i].index;
        motionController?.BeginDisplaying();
    }

    public override YarnTask OnCharacterWillAppear(int currentCharacterIndex,
        MarkupParseResult line, CancellationToken cancellationToken)
    {
        hurryToken = cancellationToken;
        if (canceled || cancellationToken.IsCancellationRequested)
        {
            canceled = true;
            motionController?.CancelLine();
            return YarnTask.CompletedTask;
        }

        int sourceIndex = sourceIndices != null && currentCharacterIndex < sourceIndices.Length
            ? sourceIndices[currentCharacterIndex] : currentCharacterIndex;
        DispatchThrough(sourceIndex);
        if (sourceIndex < lineText.Length) motionController?.ObserveCharacter(lineText[sourceIndex]);
        return YarnTask.CompletedTask;
    }

    public override void OnLineDisplayComplete()
    {
        if (canceled || hurryToken.IsCancellationRequested)
        {
            motionController?.CancelLine();
        }
        else
        {
            // Self-closing markers at the very end of a line still fire once.
            DispatchThrough(lineText.Length);
            motionController?.FinishDisplaying();
        }
        markers.Clear();
    }

    public override void OnLineWillDismiss()
    {
        canceled = true;
        markers.Clear();
        motionController?.CancelLine();
    }

    private void DispatchThrough(int sourceIndex)
    {
        while (nextMarker < markers.Count && markers[nextMarker].Position <= sourceIndex)
        {
            motionController?.PlayMotion(markers[nextMarker].Name);
            nextMarker++;
        }
    }

    private void OnDisable()
    {
        markers.Clear();
        motionController?.ResetDialogue();
    }
}
