using UnityEngine;
using Yarn.Unity;

public class LastLineWatcher : DialoguePresenterBase
{
    [SerializeField] private CharacterMotionController characterMotion;
    private LocalizedLine lastSeenLine;

    public override YarnTask RunLineAsync(LocalizedLine line, LineCancellationToken token)
    {
        lastSeenLine = line; // 대사가 나올 때마다 저장만 해둠 (화면엔 아무것도 안 그림)
        DialogueHistoryUI.Instance?.RecordLine(line);
        return YarnTask.CompletedTask;
    }

    public override YarnTask<DialogueOption?> RunOptionsAsync(DialogueOption[] dialogueOptions, LineCancellationToken cancellationToken)
    {
        characterMotion?.CancelLine();
        // 선택지가 뜨는 순간, 직전 대사를 FilterableLastLine에 넘겨줌
        if (lastSeenLine != null && FilterableLastLine.Instance != null)
        {
            FilterableLastLine.Instance.CacheLine(lastSeenLine);
        }

        return DialogueRunner.NoOptionSelected; // 선택은 기존 OptionsPresenter가 처리
    }

    public override YarnTask OnDialogueStartedAsync()
    {
        lastSeenLine = null;
        DialogueHistoryUI.Instance?.ClearHistory();
        characterMotion?.ResetDialogue();
        return YarnTask.CompletedTask;
    }

    public override YarnTask OnDialogueCompleteAsync()
    {
        characterMotion?.ResetDialogue();
        return YarnTask.CompletedTask;
    }
}
